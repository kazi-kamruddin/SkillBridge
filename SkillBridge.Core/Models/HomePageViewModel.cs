using System;
using System.Collections.Generic;

namespace SkillBridge.Models
{
    public class HomePageViewModel
    {
        public string FullName { get; set; }
        public List<SkillCategory> SkillCategories { get; set; }

        public List<UserSkill> MySkills { get; set; }
        public CommunityPost MyLatestPost { get; set; }
        public CommunityPost OtherLatestPost { get; set; }


        public string MotivationalQuote { get; set; }
        public string NextActionTitle { get; set; }
        public string NextActionDescription { get; set; }
        public string NextActionUrl { get; set; }




        public int? LatestInteractionId { get; set; }
        public string LatestInteractionOtherUser { get; set; }
        public string LatestInteractionSkillYouTeach { get; set; }
        public string LatestInteractionSkillYouLearn { get; set; }
        public string LatestInteractionStatus { get; set; }
        public string LatestInteractionOtherUserFullName { get; set; } 
        public string LatestInteractionOtherUserProfileImage { get; set; }

        public bool IsLoggedIn { get; set; }


        public static string GetRandomQuote()
        {
            var quotes = new List<string>
            {
                "Believe you can and you're halfway there.",
                "Small progress each day adds up to big results.",
                "Your limitation—it’s only your imagination.",
                "Push yourself, because no one else is going to do it for you.",
                "Dream it. Wish it. Do it.",
                "Great things never come from comfort zones.",
                "Don’t watch the clock; do what it does. Keep going.",
                "Success is what happens after you’ve survived all your mistakes."
            };

            var random = new Random();
            return quotes[random.Next(quotes.Count)];
        }
    }
}
