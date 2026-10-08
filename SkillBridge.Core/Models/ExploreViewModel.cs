using System.Collections.Generic;

namespace SkillBridge.Models
{
    public class ExploreViewModel
    {
        public List<string> LearningSkillNames { get; set; } = new List<string>();
        public List<PublicProfileViewModel> BestMatches { get; set; } = new List<PublicProfileViewModel>();
        public List<PublicProfileViewModel> PartialMatches { get; set; } = new List<PublicProfileViewModel>();
    }

}
