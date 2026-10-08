
using SkillBridge.Models;
using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class CommunitiesController : Controller
    {
        private readonly ApplicationDbContext db;

        public CommunitiesController(ApplicationDbContext db) => this.db = db;

        [AllowAnonymous]
        public ActionResult Index()
        {
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

            var allCommunities = db.Communities.Include(c => c.Skill).Include(c => c.Skill.SkillCategory).ToList();

            var model = new CommunityIndexViewModel
            {
                IsGuest = !User.Identity.IsAuthenticated,
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
        public ActionResult Landing(int id)
        {
            var community = db.Communities.Include(c => c.Skill).FirstOrDefault(c => c.Id == id);
            if (community == null) return NotFound();

            var currentUserId = User.Identity.GetUserId();
            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == community.SkillId);

            var posts = db.CommunityPosts
                .Where(p => p.CommunityId == id && !p.IsHidden)
                .OrderByDescending(p => p.CreatedAt)
                .ToList()
                .Select(p => new CommunityPostListItemViewModel
                {
                    PostId = p.Id,
                    Title = p.Title,
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
        public ActionResult CreatePost(CommunityPostCreateModel model)
        {
            var currentUserId = User.Identity.GetUserId();
            var community = db.Communities.Find(model.CommunityId);
            if (community == null) return NotFound();

            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == community.SkillId);
            if (!isMember) return StatusCode(403);
            if (!ModelState.IsValid) return View(model);

            var post = new CommunityPost
            {
                CommunityId = model.CommunityId,
                CreatedByUserId = currentUserId,
                Title = model.Title,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
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
                CreatedByUserName = DisplayName(post.CreatedByUserId),
                        CreatedAt = post.CreatedAt,
                        IsMember = isMember,
                        Comments = post.Comments.Where(c => !c.IsHidden).OrderBy(c => c.CreatedAt)
                .Select(c => new CommunityCommentViewModel
                {
                    CommentId = c.Id,
                    Content = c.Content,
                    CreatedByFullName = DisplayName(c.CreatedByUserId),
                    CreatedAt = c.CreatedAt
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
        public ActionResult CreateComment(CommunityCommentCreateModel model)
        {
            var currentUserId = User.Identity.GetUserId();

            var post = db.CommunityPosts
                .Include(p => p.Community)
                .FirstOrDefault(p => p.Id == model.PostId);

            if (post == null || post.IsHidden) return NotFound();
            bool isMember = db.UserSkills.Any(us => us.UserId == currentUserId && us.SkillId == post.Community.SkillId);
            if (!isMember) return StatusCode(403);

            if (!ModelState.IsValid)
            {
                TempData["CommunityNotice"] = "Please write a comment of at most 1,000 characters.";
                return RedirectToAction("PostDetails", new { id = model.PostId });
            }

            var comment = new CommunityComment
            {
                PostId = model.PostId,
                CreatedByUserId = currentUserId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };

            db.CommunityComments.Add(comment);
            db.SaveChanges();

            return RedirectToAction("PostDetails", new { id = model.PostId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            return !User.Identity.IsAuthenticated && info?.IsPublic != true
                ? "SkillBridge member" : info?.FullName ?? "SkillBridge member";
        }

    }
}
