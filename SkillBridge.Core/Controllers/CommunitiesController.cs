
using SkillBridge.Models;
using SkillBridge.Helpers;
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
        public async Task<ActionResult> Index(string q = "", string tab = "all", int? skillId = null, int page = 1)
        {
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q[..100];
            if (tab != "my-skills") tab = "all";
            if (page < 1) page = 1;
            var currentUserId = User.Identity.GetUserId();

            var userSkills = !string.IsNullOrEmpty(currentUserId)
                ? await db.UserSkills
                    .AsNoTracking()
                    .Where(us => us.UserId == currentUserId)
                    .Include(us => us.Skill)
                    .ToListAsync()
                : new List<UserSkill>();

            var userSkillIds = userSkills.Select(us => us.SkillId).ToHashSet();

            var teachingSkills = userSkills
                .Where(us => us.Status == "Teaching")
                .Select(us => us.Skill)
                .ToList();

            var learningSkills = userSkills
                .Where(us => us.Status == "Learning")
                .Select(us => us.Skill)
                .ToList();

            // Query community feed posts
            var postQuery = db.CommunityPosts
                .AsNoTracking()
                .Where(p => !p.IsHidden);

            if (tab == "my-skills")
            {
                if (userSkillIds.Count > 0)
                {
                    postQuery = postQuery.Where(p => userSkillIds.Contains(p.Community.SkillId));
                }
                else
                {
                    postQuery = postQuery.Where(p => false);
                }
            }

            if (skillId.HasValue && skillId.Value > 0)
            {
                postQuery = postQuery.Where(p => p.Community.SkillId == skillId.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                postQuery = postQuery.Where(p =>
                    EF.Functions.ILike(p.Title, "%" + q + "%") ||
                    EF.Functions.ILike(p.Content, "%" + q + "%") ||
                    EF.Functions.ILike(p.Community.Skill.Name, "%" + q + "%"));
            }

            var totalPostsCount = await postQuery.CountAsync();
            const int pageSize = 15;
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalPostsCount / pageSize));
            if (page > totalPages && totalPostsCount > 0) page = totalPages;

            var rawPosts = await postQuery
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.CommunityId,
                    SkillId = p.Community.SkillId,
                    SkillName = p.Community.Skill.Name,
                    CategoryName = p.Community.Skill.SkillCategory.Name,
                    p.CreatedByUserId,
                    p.Title,
                    p.Content,
                    p.ImageUrl,
                    p.CreatedAt,
                    CommentCount = p.Comments.Count(c => !c.IsHidden),
                    TopComment = p.Comments
                        .Where(c => !c.IsHidden)
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Id,
                            c.CreatedByUserId,
                            c.Content,
                            c.ImageUrl,
                            c.CreatedAt
                        }).FirstOrDefault()
                })
                .ToListAsync();

            var authorIds = rawPosts.Select(p => p.CreatedByUserId)
                .Concat(rawPosts.Where(p => p.TopComment != null).Select(p => p.TopComment.CreatedByUserId))
                .Distinct()
                .ToList();

            var authors = await db.UserInformations
                .AsNoTracking()
                .Where(info => authorIds.Contains(info.UserId))
                .ToDictionaryAsync(info => info.UserId);

            var authorSkills = await db.UserSkills
                .AsNoTracking()
                .Where(us => authorIds.Contains(us.UserId))
                .ToListAsync();

            var authorSkillsLookup = authorSkills.ToLookup(us => (us.UserId, us.SkillId));

            var feedPosts = rawPosts.Select(p =>
            {
                var authorInfo = authors.GetValueOrDefault(p.CreatedByUserId);
                var authorSkill = authorSkillsLookup[(p.CreatedByUserId, p.SkillId)].FirstOrDefault();
                string roleBadge = authorSkill != null
                    ? (authorSkill.Status == "Teaching" ? $"Teaches {p.SkillName} (Stage {authorSkill.KnownUpToStage ?? 1})" : $"Learns {p.SkillName}")
                    : "Community Member";

                string avatarUrl = (authorInfo != null && !authorInfo.IsHidden && (User.Identity.IsAuthenticated || authorInfo.IsPublic))
                    ? ProfileImageHelper.GetProfileImage(authorInfo.ProfileImageUrl, authorInfo.FullName)
                    : ProfileImageHelper.GetProfileImage(null, "SkillBridge member");

                CommunityCommentViewModel topCommentVm = null;
                if (p.TopComment != null)
                {
                    var commentAuthor = authors.GetValueOrDefault(p.TopComment.CreatedByUserId);
                    var commentAvatar = (commentAuthor != null && !commentAuthor.IsHidden && (User.Identity.IsAuthenticated || commentAuthor.IsPublic))
                        ? ProfileImageHelper.GetProfileImage(commentAuthor.ProfileImageUrl, commentAuthor.FullName)
                        : ProfileImageHelper.GetProfileImage(null, "SkillBridge member");

                    topCommentVm = new CommunityCommentViewModel
                    {
                        CommentId = p.TopComment.Id,
                        Content = p.TopComment.Content,
                        ImageUrl = p.TopComment.ImageUrl,
                        ProfileImageUrl = commentAvatar,
                        CreatedByFullName = DisplayName(commentAuthor),
                        CreatedAt = p.TopComment.CreatedAt,
                        IsAuthor = p.TopComment.CreatedByUserId == currentUserId
                    };
                }

                return new CommunityFeedPostViewModel
                {
                    PostId = p.Id,
                    CommunityId = p.CommunityId,
                    SkillId = p.SkillId,
                    SkillName = p.SkillName,
                    CategoryName = p.CategoryName,
                    AuthorUserId = p.CreatedByUserId,
                    AuthorName = DisplayName(authorInfo),
                    AuthorAvatarUrl = avatarUrl,
                    AuthorRoleBadge = roleBadge,
                    AuthorIsPublic = authorInfo?.IsPublic == true && !authorInfo.IsHidden,
                    Title = p.Title,
                    Content = p.Content,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt,
                    TimeAgo = FormatTimeAgo(p.CreatedAt),
                    CommentCount = p.CommentCount,
                    IsAuthor = p.CreatedByUserId == currentUserId,
                    TopComment = topCommentVm
                };
            }).ToList();

            var skillPills = await db.Communities
                .AsNoTracking()
                .Select(c => new CommunityFilterSkillPill
                {
                    SkillId = c.SkillId,
                    SkillName = c.Skill.Name,
                    PostCount = c.Posts.Count(post => !post.IsHidden),
                    IsUserSkill = userSkillIds.Contains(c.SkillId)
                })
                .OrderByDescending(s => s.PostCount)
                .ThenBy(s => s.SkillName)
                .Take(16)
                .ToListAsync();

            var userPostableSkills = !string.IsNullOrEmpty(currentUserId)
                ? await (from us in db.UserSkills.AsNoTracking()
                         join c in db.Communities.AsNoTracking() on us.SkillId equals c.SkillId
                         where us.UserId == currentUserId
                         orderby c.Skill.Name
                         select new UserSelectableSkill
                         {
                             CommunityId = c.Id,
                             SkillId = c.SkillId,
                             SkillName = c.Skill.Name,
                             Role = us.Status
                         }).ToListAsync()
                : new List<UserSelectableSkill>();

            string selectedSkillName = null;
            if (skillId.HasValue && skillId.Value > 0)
            {
                selectedSkillName = await db.Skills
                    .AsNoTracking()
                    .Where(s => s.Id == skillId.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
            }

            var allCommunities = await db.Communities.AsNoTracking()
                .Include(c => c.Skill).ThenInclude(s => s.SkillCategory)
                .Where(c => q == "" || EF.Functions.ILike(c.Name, "%" + q + "%") ||
                    EF.Functions.ILike(c.Skill.Name, "%" + q + "%") ||
                    EF.Functions.ILike(c.Skill.SkillCategory.Name, "%" + q + "%"))
                .ToListAsync();

            var model = new CommunityIndexViewModel
            {
                IsGuest = !User.Identity.IsAuthenticated,
                SearchQuery = q,
                ActiveTab = tab,
                SelectedSkillId = skillId,
                SelectedSkillName = selectedSkillName,
                Posts = feedPosts,
                AvailableSkillPills = skillPills,
                UserPostableSkills = userPostableSkills,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalPostsCount = totalPostsCount,
                HasMore = page < totalPages,
                UserHasSkills = userSkills.Count > 0,
                SkillsYouKnow = allCommunities
                    .Where(c => teachingSkills.Any(ts => ts.Id == c.SkillId))
                    .Select(c => new CommunityViewModel
                    {
                        CommunityId = c.Id,
                        SkillName = c.Skill.Name,
                        CategoryName = c.Skill.SkillCategory.Name,
                        IsMember = true
                    }).ToList(),
                SkillsYouWantToLearn = allCommunities
                    .Where(c => learningSkills.Any(ls => ls.Id == c.SkillId) && !teachingSkills.Any(ts => ts.Id == c.SkillId))
                    .Select(c => new CommunityViewModel
                    {
                        CommunityId = c.Id,
                        SkillName = c.Skill.Name,
                        CategoryName = c.Skill.SkillCategory.Name,
                        IsMember = true
                    }).ToList(),
                OtherCommunities = allCommunities
                    .Where(c => !teachingSkills.Any(ts => ts.Id == c.SkillId) && !learningSkills.Any(ls => ls.Id == c.SkillId))
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

            var postRows = db.CommunityPosts
                .Where(p => p.CommunityId == id && !p.IsHidden)
                .Where(p => q == "" || EF.Functions.ILike(p.Title, "%" + q + "%") ||
                    EF.Functions.ILike(p.Content, "%" + q + "%"))
                .Select(p => new
                {
                    p.Id,
                    CommentCount = p.Comments.Count(c => !c.IsHidden),
                    p.Title,
                    p.ImageUrl,
                    p.CreatedByUserId,
                    p.CreatedAt,
                    LastActivity = p.Comments.Where(c => !c.IsHidden).Select(c => (DateTime?)c.CreatedAt).Max() ?? p.CreatedAt
                })
                .OrderByDescending(p => sort == "active" ? p.LastActivity : p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .ToList();
            var authorIds = postRows.Select(p => p.CreatedByUserId).Distinct().ToList();
            var authors = db.UserInformations.Where(info => authorIds.Contains(info.UserId)).ToDictionary(info => info.UserId);
            var posts = postRows.Select(p => new CommunityPostListItemViewModel
            {
                PostId = p.Id,
                CommentCount = p.CommentCount,
                Title = p.Title,
                ImageUrl = p.ImageUrl,
                CreatedByFullName = DisplayName(authors.GetValueOrDefault(p.CreatedByUserId)),
                CreatedAt = p.CreatedAt
            }).ToList();



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
        public async Task<ActionResult> CreatePost(CommunityPostCreateModel model, string returnUrl = null)
        {
            var currentUserId = User.Identity.GetUserId();
            var community = await db.Communities.FindAsync(model.CommunityId);
            if (community == null) return NotFound();

            bool isMember = await db.UserSkills.AnyAsync(us => us.UserId == currentUserId && us.SkillId == community.SkillId);
            if (!isMember) return StatusCode(403);
            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(model.Title), "Write a title.");
            if (string.IsNullOrWhiteSpace(model.Content) && model.Image?.Length is not > 0)
                ModelState.AddModelError(nameof(model.Content), "Write some content or add an image.");
            var imageError = CloudinaryImageService.Validate(model.Image);
            if (imageError != null) ModelState.AddModelError(nameof(model.Image), imageError);
            if (!ModelState.IsValid)
            {
                var errorMsg = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Please check your post details.";
                TempData["CommunityError"] = errorMsg;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return View(model);
            }

            UploadedImage uploaded = null;
            if (model.Image?.Length > 0)
            {
                try { uploaded = await images.UploadAsync(model.Image, "skillbridge/community", false); }
                catch
                {
                    TempData["CommunityError"] = "The image could not be uploaded. Please try again.";
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    ModelState.AddModelError(nameof(model.Image), "The image could not be uploaded. Please try again.");
                    return View(model);
                }
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
            await db.SaveChangesAsync();

            TempData["CommunityNotice"] = "Your post has been published to the community feed!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Communities");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public async Task<ActionResult> JoinSkill(int skillId, string returnUrl = null)
        {
            var currentUserId = User.Identity.GetUserId();
            var skill = await db.Skills.FindAsync(skillId);
            if (skill == null) return NotFound();

            bool exists = await db.UserSkills.AnyAsync(us => us.UserId == currentUserId && us.SkillId == skillId);
            if (!exists)
            {
                db.UserSkills.Add(new UserSkill
                {
                    UserId = currentUserId,
                    SkillId = skillId,
                    Status = "Learning",
                    KnownUpToStage = 0
                });
                await db.SaveChangesAsync();
                TempData["CommunityNotice"] = $"You joined the {skill.Name} community! You can now participate in conversations.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Communities", new { skillId });
        }

        [AllowAnonymous]
        public async Task<ActionResult> PostDetails(int id)
        {
            var post = await db.CommunityPosts
                .Include(p => p.Community)
                .ThenInclude(c => c.Skill)
                .ThenInclude(s => s.SkillCategory)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsHidden);

            if (post == null) return NotFound();

            var currentUserId = User.Identity.GetUserId();
            bool isMember = !string.IsNullOrEmpty(currentUserId) &&
                await db.UserSkills.AnyAsync(us => us.UserId == currentUserId && us.SkillId == post.Community.SkillId);

            var commentAuthorIds = post.Comments.Where(c => !c.IsHidden).Select(c => c.CreatedByUserId)
                .Append(post.CreatedByUserId).Distinct().ToList();

            var commentAuthors = await db.UserInformations.AsNoTracking()
                .Where(info => commentAuthorIds.Contains(info.UserId))
                .ToDictionaryAsync(info => info.UserId);

            var authorSkill = await db.UserSkills.AsNoTracking()
                .Where(us => us.UserId == post.CreatedByUserId && us.SkillId == post.Community.SkillId)
                .FirstOrDefaultAsync();

            string authorRole = authorSkill != null
                ? (authorSkill.Status == "Teaching" ? $"Teaches {post.Community.Skill.Name} (Stage {authorSkill.KnownUpToStage ?? 1})" : $"Learns {post.Community.Skill.Name}")
                : "Community Member";

            var authorInfo = commentAuthors.GetValueOrDefault(post.CreatedByUserId);
            string authorAvatar = (authorInfo != null && !authorInfo.IsHidden && (User.Identity.IsAuthenticated || authorInfo.IsPublic))
                ? ProfileImageHelper.GetProfileImage(authorInfo.ProfileImageUrl, authorInfo.FullName)
                : ProfileImageHelper.GetProfileImage(null, "SkillBridge member");

            var model = new PostDetailsViewModel
            {
                PostId = post.Id,
                CommunityId = post.CommunityId,
                SkillId = post.Community.SkillId,
                SkillName = post.Community.Skill.Name,
                CategoryName = post.Community.Skill.SkillCategory?.Name ?? "General",
                Title = post.Title,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedByUserId = post.CreatedByUserId,
                CreatedByUserName = DisplayName(authorInfo),
                AuthorAvatarUrl = authorAvatar,
                AuthorRoleBadge = authorRole,
                CreatedAt = post.CreatedAt,
                IsMember = isMember,
                IsAuthor = post.CreatedByUserId == currentUserId,
                Comments = post.Comments.Where(c => !c.IsHidden).OrderBy(c => c.CreatedAt)
                    .Select(c => new CommunityCommentViewModel
                    {
                        CommentId = c.Id,
                        Content = c.Content,
                        ImageUrl = c.ImageUrl,
                        CreatedByFullName = DisplayName(commentAuthors.GetValueOrDefault(c.CreatedByUserId)),
                        ProfileImageUrl = commentAuthors.TryGetValue(c.CreatedByUserId, out var author) &&
                            !author.IsHidden && (User.Identity.IsAuthenticated || author.IsPublic)
                            ? ProfileImageHelper.GetProfileImage(author.ProfileImageUrl, author.FullName)
                            : ProfileImageHelper.GetProfileImage(null, "SkillBridge member"),
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

        private string DisplayName(UserInformation info)
        {
            return !User.Identity.IsAuthenticated && (info?.IsPublic != true || info.IsHidden)
                ? "SkillBridge member" : info?.FullName ?? "SkillBridge member";
        }

        private static string FormatTimeAgo(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalSeconds < 60) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return dt.ToString("MMM dd, yyyy");
        }

    }
}
