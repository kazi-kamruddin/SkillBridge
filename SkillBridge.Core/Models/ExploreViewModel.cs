using System.Collections.Generic;

namespace SkillBridge.Models
{
    public class ExploreViewModel
    {
        public string SearchQuery { get; set; } = "";
        public List<string> LearningSkillNames { get; set; } = new List<string>();
        public List<string> AllCatalogSkillNames { get; set; } = new List<string>();
        public int MaxCatalogStage { get; set; } = 7;
        public List<string> TimeZones { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public List<PublicProfileViewModel> BestMatches { get; set; } = new List<PublicProfileViewModel>();
        public List<PublicProfileViewModel> PartialMatches { get; set; } = new List<PublicProfileViewModel>();
        public List<PublicProfileViewModel> AdditionalMembers { get; set; } = new List<PublicProfileViewModel>();
        public bool HasOwnSkills { get; set; }
    }
}
