using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models
{
    public class Community
    {
        public int Id { get; set; }
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual Skill Skill { get; set; }
        public virtual ICollection<CommunityPost> Posts { get; set; }
    }

    public class CommunityPost
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public string CreatedByUserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        [StringLength(500)] public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsHidden { get; set; }

        public virtual Community Community { get; set; }
        public virtual ApplicationUser CreatedByUser { get; set; }
        public virtual ICollection<CommunityComment> Comments { get; set; }
    }

    public class CommunityComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string CreatedByUserId { get; set; }
        public string Content { get; set; }
        [StringLength(500)] public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsHidden { get; set; }

        public virtual CommunityPost Post { get; set; }
        public virtual ApplicationUser CreatedByUser { get; set; }
    }

    // =====================================
    // VIEWMODELS
    // =====================================

    public class CommunityFeedPostViewModel
    {
        public int PostId { get; set; }
        public int CommunityId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string CategoryName { get; set; }
        public string AuthorUserId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorAvatarUrl { get; set; }
        public string AuthorRoleBadge { get; set; }
        public bool AuthorIsPublic { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public int CommentCount { get; set; }
        public bool IsAuthor { get; set; }
        public CommunityCommentViewModel TopComment { get; set; }
    }

    public class CommunityFilterSkillPill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int PostCount { get; set; }
        public bool IsUserSkill { get; set; }
    }

    public class UserSelectableSkill
    {
        public int CommunityId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string Role { get; set; }
    }

    public class CommunityIndexViewModel
    {
        public bool IsGuest { get; set; }
        public string SearchQuery { get; set; } = "";
        public string ActiveTab { get; set; } = "all";
        public int? SelectedSkillId { get; set; }
        public string SelectedSkillName { get; set; }
        public List<CommunityFeedPostViewModel> Posts { get; set; } = new List<CommunityFeedPostViewModel>();
        public List<CommunityFilterSkillPill> AvailableSkillPills { get; set; } = new List<CommunityFilterSkillPill>();
        public List<UserSelectableSkill> UserPostableSkills { get; set; } = new List<UserSelectableSkill>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalPostsCount { get; set; }
        public bool HasMore { get; set; }
        public bool UserHasSkills { get; set; }
        public List<CommunityViewModel> SkillsYouKnow { get; set; } = new List<CommunityViewModel>();
        public List<CommunityViewModel> SkillsYouWantToLearn { get; set; } = new List<CommunityViewModel>();
        public List<CommunityViewModel> OtherCommunities { get; set; } = new List<CommunityViewModel>();
    }

    public class CommunityViewModel
    {
        public int CommunityId { get; set; }
        public string SkillName { get; set; }
        public string CategoryName { get; set; }
        public bool IsMember { get; set; }
    }

    public class CommunityLandingViewModel
    {
        public int CommunityId { get; set; }
        public string CommunityName { get; set; }
        public string SkillName { get; set; }
        public bool IsMember { get; set; }
        public string SearchQuery { get; set; } = "";
        public string Sort { get; set; } = "newest";
        public List<CommunityPostListItemViewModel> Posts { get; set; } = new List<CommunityPostListItemViewModel>();
    }

    public class CommunityPostListItemViewModel
    {
        public int PostId { get; set; }
        public int CommentCount { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string CreatedByFullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PostDetailsViewModel
    {
        public int PostId { get; set; }
        public int CommunityId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string CategoryName { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public string AuthorAvatarUrl { get; set; }
        public string AuthorRoleBadge { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMember { get; set; }
        public bool IsAuthor { get; set; }

        public List<CommunityCommentViewModel> Comments { get; set; } = new List<CommunityCommentViewModel>();

        public CommunityCommentCreateModel NewComment { get; set; } = new CommunityCommentCreateModel();
    }


    public class CommunityCommentViewModel
    {
        public int CommentId { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string ProfileImageUrl { get; set; }
        public string CreatedByFullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAuthor { get; set; }
    }

    public class CommunityReport
    {
        public int Id { get; set; }
        public string ReporterId { get; set; }
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
        [Required, StringLength(500)]
        public string Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ModerationReportViewModel
    {
        public int Id { get; set; }
        public int? PostId { get; set; }
        public string Reason { get; set; }
        public string ContentType { get; set; }
        public string ContentPreview { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommunityCommentCreateModel
    {
        [Required]
        public int PostId { get; set; }

        [StringLength(1000)]
        public string Content { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Image { get; set; }
    }

    public class CommunityPostCreateModel
    {
        [Required]
        public int CommunityId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Content { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Image { get; set; }
    }
}
