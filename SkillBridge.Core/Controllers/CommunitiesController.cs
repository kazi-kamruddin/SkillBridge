
using SkillBridge.Models;
using SkillBridge.Services;
using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class CommunitiesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly CloudinaryImageService images;

        public CommunitiesController(ApplicationDbContext db, CloudinaryImageService images)
        { this.db = db; this.images = images; }

        [AllowAnonymous]
        public ActionResult Index(string q = "")
        {
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q[..100];
            var currentUserId = User.Identity.GetUserId();

            var userSkills = db.UserSkills
                .Where(us => us.UserId == currentUserId)
                .Include(us => us.Skill)
                .ToList();

            var teachingSkills = userSkills
                .Where(us => us.Status == "Teaching")
                .Select(us => us.Skill)
                .ToList();

            var learningSkills = userSkills
                .Where(us => us.Status == "Learning")
                .Select(us => us.Skill)
                .ToList();

            var allCommunities = db.Communities.Include(c => c.Skill).Include(c => c.Skill.SkillCategory)
                .Where(c => q == "" || EF.Functions.ILike(c.Name, "%" + q + "%") ||
                    EF.Functions.ILike(c.Skill.Name, "%" + q + "%") ||
                    EF.Functions.ILike(c.Skill.SkillCategory.Name, "%" + q + "%"))
                .ToList();

            var model = new CommunityIndexViewModel
            {
                IsGuest = !User.Identity.IsAuthenticated,
                SearchQuery = q,
                SkillsYouKnow = allCommunities
                    .Where(c => teachingSkills.Contains(c.Skill))
                    .Select(c => new CommunityViewModel
                    {
                        CommunityId = c.Id,
                        SkillName = c.Skill.Name,
                        CategoryName = c.Skill.SkillCategory.Name,
                        IsMember = true
                    }).ToList(),

                SkillsYouWantToLearn = allCommunities
                    .Where(c => learningSkills.Contains(c.Skill) && !teachingSkills.Contains(c.Skill))
                    .Select(c => new CommunityViewModel
                    {
                        CommunityId = c.Id,
                        SkillName = c.Skill.Name,
                        CategoryName = c.Skill.SkillCategory.Name,
                        IsMember = true
                    }).ToList(),

                OtherCommunities = allCommunities
                    .Where(c => !teachingSkills.Contains(c.Skill) && !learningSkills.Contains(c.Skill))
                    .Select(c => new CommunityViewModel
                    {
                        CommunityId = c.Id,
                        SkillName = c.Skill.Name,
                        CategoryName = c.Skill.SkillCategory.Name,
                        IsMember = false
                    }).ToList()
            };

            return View(model);
        }


        [AllowAnonymous]
        public ActionResult Landing(int id, string q = "", string sort = "newest")
        {
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q[..100];
            if (sort != "active") sort = "newest";
            var community = db.Communities.Include(c => c.Skill).FirstOrDefault(c => c.Id == id);
            if (community == null) return NotFound();

            var currentUserId = User.Identity.GetUserId();
            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == community.SkillId);

            var posts = db.CommunityPosts
                .Where(p => p.CommunityId == id && !p.IsHidden)
                .Where(p => q == "" || EF.Functions.ILike(p.Title, "%" + q + "%") ||
                    EF.Functions.ILike(p.Content, "%" + q + "%"))
                .Include(p => p.Comments)
                .ToList()
                .OrderByDescending(p => sort == "active"
                    ? p.Comments.Where(c => !c.IsHidden).Select(c => c.CreatedAt)
                        .DefaultIfEmpty(p.CreatedAt).Max()
                    : p.CreatedAt)
                .Select(p => new CommunityPostListItemViewModel
                {
                    PostId = p.Id,
                    CommentCount = p.Comments.Count(c => !c.IsHidden),
                    Title = p.Title,
                    ImageUrl = p.ImageUrl,
                    CreatedByFullName = DisplayName(p.CreatedByUserId),
                    CreatedAt = p.CreatedAt
                })
                .ToList();



            var model = new CommunityLandingViewModel
            {
                CommunityId = community.Id,
                CommunityName = community.Name,
                SkillName = community.Skill.Name,
                IsMember = isMember,
                SearchQuery = q,
                Sort = sort,
                Posts = posts
            };

            return View(model);
        }


        public ActionResult CreatePost(int communityId)
        {
            var currentUserId = User.Identity.GetUserId();
            var community = db.Communities.Find(communityId);
            if (community == null) return NotFound();

            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == community.SkillId);
            if (!isMember) return StatusCode(403);

            var model = new CommunityPostCreateModel
            {
                CommunityId = communityId
            };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult> CreatePost(CommunityPostCreateModel model)
        {
            var currentUserId = User.Identity.GetUserId();
            var community = db.Communities.Find(model.CommunityId);
            if (community == null) return NotFound();

            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == community.SkillId);
            if (!isMember) return StatusCode(403);
            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(model.Title), "Write a title.");
            if (string.IsNullOrWhiteSpace(model.Content) && model.Image?.Length is not > 0)
                ModelState.AddModelError(nameof(model.Content), "Write some content or add an image.");
            var imageError = CloudinaryImageService.Validate(model.Image);
            if (imageError != null) ModelState.AddModelError(nameof(model.Image), imageError);
            if (!ModelState.IsValid) return View(model);

            UploadedImage uploaded = null;
            if (model.Image?.Length > 0)
            {
                try { uploaded = await images.UploadAsync(model.Image, "skillbridge/community", false); }
                catch { ModelState.AddModelError(nameof(model.Image), "The image could not be uploaded. Please try again."); return View(model); }
            }

            var post = new CommunityPost
            {
                CommunityId = model.CommunityId,
                CreatedByUserId = currentUserId,
                Title = model.Title.Trim(),
                Content = model.Content?.Trim() ?? "",
                ImageUrl = uploaded?.Url,
                CreatedAt = DateTime.Now
            };

            db.CommunityPosts.Add(post);
            db.SaveChanges();

            return RedirectToAction("Landing", new { id = model.CommunityId });
        }



        [AllowAnonymous]
        public ActionResult PostDetails(int id)
        {
            var post = db.CommunityPosts
                .Include(p => p.Community)
                .Include(p => p.Comments)
                .FirstOrDefault(p => p.Id == id && !p.IsHidden);

            if (post == null) return NotFound();

            var currentUserId = User.Identity.GetUserId();
            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == post.Community.SkillId);

            var model = new PostDetailsViewModel
            {
                PostId = post.Id,
                CommunityId = post.CommunityId,
                Title = post.Title,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedByUserName = DisplayName(post.CreatedByUserId),
                        CreatedAt = post.CreatedAt,
                        IsMember = isMember,
                        IsAuthor = post.CreatedByUserId == currentUserId,
                        Comments = post.Comments.Where(c => !c.IsHidden).OrderBy(c => c.CreatedAt)
                .Select(c => new CommunityCommentViewModel
                {
                    CommentId = c.Id,
                    Content = c.Content,
                    ImageUrl = c.ImageUrl,
                    CreatedByFullName = DisplayName(c.CreatedByUserId),
                    CreatedAt = c.CreatedAt,
                    IsAuthor = c.CreatedByUserId == currentUserId
                }).ToList(),
                NewComment = new CommunityCommentCreateModel
                {
                    PostId = post.Id
                }
            };


            return View(model);
        }



        //[HttpGet]
        //public ActionResult CreateComment()
        //{
        //    return RedirectToAction("Index");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult> CreateComment(CommunityCommentCreateModel model)
        {
            var currentUserId = User.Identity.GetUserId();

            var post = db.CommunityPosts
                .Include(p => p.Community)
                .FirstOrDefault(p => p.Id == model.PostId);

            if (post == null || post.IsHidden) return NotFound();
            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == post.Community.SkillId);
            if (!isMember) return StatusCode(403);
            if (string.IsNullOrWhiteSpace(model.Content) && model.Image?.Length is not > 0)
                ModelState.AddModelError(nameof(model.Content), "Write a comment or add an image.");
            var imageError = CloudinaryImageService.Validate(model.Image);
            if (imageError != null) ModelState.AddModelError(nameof(model.Image), imageError);

            if (!ModelState.IsValid)
            {
                TempData["CommunityNotice"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Check your comment.";
                return RedirectToAction("PostDetails", new { id = model.PostId });
            }

            UploadedImage uploaded = null;
            if (model.Image?.Length > 0)
            {
                try { uploaded = await images.UploadAsync(model.Image, "skillbridge/community", false); }
                catch { TempData["CommunityNotice"] = "The image could not be uploaded. Please try again.";
                    return RedirectToAction("PostDetails", new { id = model.PostId }); }
            }

            var comment = new CommunityComment
            {
                PostId = model.PostId,
                CreatedByUserId = currentUserId,
                Content = model.Content?.Trim() ?? "",
                ImageUrl = uploaded?.Url,
                CreatedAt = DateTime.Now
            };

            db.CommunityComments.Add(comment);
            db.SaveChanges();

            return RedirectToAction("PostDetails", new { id = model.PostId });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult EditPost(int postId, string title, string content)
        {
            var post = db.CommunityPosts.FirstOrDefault(p => p.Id == postId && !p.IsHidden &&
                p.CreatedByUserId == User.Identity.GetUserId());
            if (post == null) return NotFound();
            title = (title ?? "").Trim();
            content = (content ?? "").Trim();
            if (title.Length is < 1 or > 200 || content.Length > 2000 ||
                (content.Length == 0 && string.IsNullOrWhiteSpace(post.ImageUrl)))
                return BadRequest("Add a title and content or an image, within the field limits.");
            post.Title = title;
            post.Content = content;
            post.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            TempData["CommunityNotice"] = "Post updated.";
            return RedirectToAction(nameof(PostDetails), new { id = postId });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult EditComment(int commentId, string content)
        {
            var comment = db.CommunityComments.Include(c => c.Post).FirstOrDefault(c => c.Id == commentId &&
                !c.IsHidden && !c.Post.IsHidden && c.CreatedByUserId == User.Identity.GetUserId());
            if (comment == null) return NotFound();
            content = (content ?? "").Trim();
            if (content.Length > 1000 || (content.Length == 0 && string.IsNullOrWhiteSpace(comment.ImageUrl)))
                return BadRequest("Keep the comment within 1000 characters and include text or an image.");
            comment.Content = content;
            db.SaveChanges();
            TempData["CommunityNotice"] = "Comment updated.";
            return RedirectToAction(nameof(PostDetails), new { id = comment.PostId });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult RemoveOwnContent(int postId, int? commentId)
        {
            var userId = User.Identity.GetUserId();
            if (commentId.HasValue)
            {
                var comment = db.CommunityComments.FirstOrDefault(c => c.Id == commentId.Value &&
                    c.PostId == postId && c.CreatedByUserId == userId && !c.IsHidden);
                if (comment == null) return NotFound();
                comment.IsHidden = true;
                db.SaveChanges();
                TempData["CommunityNotice"] = "Comment removed.";
                return RedirectToAction(nameof(PostDetails), new { id = postId });
            }
            var post = db.CommunityPosts.FirstOrDefault(p => p.Id == postId &&
                p.CreatedByUserId == userId && !p.IsHidden);
            if (post == null) return NotFound();
            post.IsHidden = true;
            db.SaveChanges();
            TempData["CommunityNotice"] = "Post removed.";
            return RedirectToAction(nameof(Landing), new { id = post.CommunityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult Report(int postId, int? commentId, string reason)
        {
            var post = db.CommunityPosts.FirstOrDefault(p => p.Id == postId && !p.IsHidden);
            if (post == null) return NotFound();
            if (commentId.HasValue && !db.CommunityComments.Any(c =>
                    c.Id == commentId.Value && c.PostId == postId && !c.IsHidden))
                return NotFound();

            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            {
                TempData["CommunityNotice"] = "Please give a reason of at most 500 characters.";
                return RedirectToAction("PostDetails", new { id = postId });
            }

            var reporterId = User.Identity.GetUserId();
            var alreadyReported = db.CommunityReports.Any(r => r.ReporterId == reporterId &&
                (commentId.HasValue ? r.CommentId == commentId.Value : r.PostId == postId));
            if (!alreadyReported)
            {
                db.CommunityReports.Add(new CommunityReport
                {
                    ReporterId = reporterId,
                    PostId = commentId.HasValue ? null : postId,
                    CommentId = commentId,
                    Reason = reason.Trim()
                });
                db.SaveChanges();
            }
            TempData["CommunityNotice"] = "Thanks. Your report has been sent for review.";
            return RedirectToAction("PostDetails", new { id = postId });
        }

        private string DisplayName(string userId)
        {
            var info = db.UserInformations.FirstOrDefault(ui => ui.UserId == userId);
            return !User.Identity.IsAuthenticated && (info?.IsPublic != true || info.IsHidden)
                ? "SkillBridge member" : info?.FullName ?? "SkillBridge member";
        }

    }
}
