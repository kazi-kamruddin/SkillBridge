
using SkillBridge.Helpers;
using System;
using SkillBridge.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SkillBridge.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db;

        public HomeController(ApplicationDbContext db) => this.db = db;



        ////////////////////////////////////////////////////////////////////////////

        public ActionResult Index()
        {
            var vm = new HomePageViewModel();

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var userInfo = db.UserInformations.FirstOrDefault(ui => ui.UserId == userId);
                vm.FullName = userInfo?.FullName ?? "";
                vm.IsLoggedIn = true;
                vm.MotivationalQuote = HomePageViewModel.GetRandomQuote();
                vm.MySkills = db.UserSkills
                    .Include("Skill.SkillStages")
                    .Where(us => us.UserId == userId)
                    .ToList();

                if (db.SkillRequests.Any(r => r.ReceiverId == userId && r.Status == "Pending"))
                {
                    vm.NextActionTitle = "A member is waiting for your answer";
                    vm.NextActionDescription = "Review the exchange proposal and decide whether to connect.";
                    vm.NextActionUrl = Url.Action("Requests", "Profile");
                }
                else if (db.Messages.Any(m => m.ToUserId == userId && !m.IsRead))
                {
                    vm.NextActionTitle = "You have an unread message";
                    vm.NextActionDescription = "Pick up the conversation with your learning partner.";
                    vm.NextActionUrl = Url.Action("Index", "Messages");
                }
                else if (!vm.MySkills.Any(s => s.Status == "Teaching") || !vm.MySkills.Any(s => s.Status == "Learning"))
                {
                    vm.NextActionTitle = "Add skills to start exchanging";
                    vm.NextActionDescription = "Choose something you can teach and something you want to learn.";
                    vm.NextActionUrl = Url.Action("UpdateProfile", "Profile");
                }
                else if (db.Interactions.Any(i => i.Status == "Ongoing" && (i.User1Id == userId || i.User2Id == userId)))
                {
                    vm.NextActionTitle = "Continue your exchange";
                    vm.NextActionDescription = "Check your plan, meeting time, and next milestone.";
                    vm.NextActionUrl = Url.Action("Index", "Interactions");
                }
                else
                {
                    vm.NextActionTitle = "Find your next learning partner";
                    vm.NextActionDescription = "Explore people whose teaching skills match what you want to learn.";
                    vm.NextActionUrl = Url.Action("Index", "Explore");
                }

                vm.MyLatestPost = db.CommunityPosts
                    .Where(p => p.CreatedByUserId == userId && !p.IsHidden)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

                vm.OtherLatestPost = db.CommunityPosts
                    .Where(p => p.CreatedByUserId != userId && !p.IsHidden)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

                var latestInteraction = db.Interactions
                    .Include(i => i.User1)
                    .Include(i => i.User2)
                    .Include(i => i.SkillFromRequester)
                    .Include(i => i.SkillFromTeacher)
                    .Where(i => i.User1Id == userId || i.User2Id == userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .FirstOrDefault();

                if (latestInteraction != null)
                {
                    vm.LatestInteractionId = latestInteraction.Id;
                    vm.LatestInteractionStatus = latestInteraction.Status;

                    var otherUser = latestInteraction.User1Id == userId
                        ? latestInteraction.User2
                        : latestInteraction.User1;

                    var otherUserInfo = db.UserInformations.FirstOrDefault(ui => ui.UserId == otherUser.Id);

                    vm.LatestInteractionOtherUser = otherUser.UserName;
                    vm.LatestInteractionOtherUserFullName = otherUserInfo?.FullName ?? otherUser.UserName;
                    vm.LatestInteractionOtherUserProfileImage = ProfileImageHelper.GetProfileImage(otherUserInfo?.ProfileImageUrl, otherUserInfo?.FullName);

                    vm.LatestInteractionSkillYouLearn =
                        latestInteraction.User1Id == userId
                            ? latestInteraction.SkillFromRequester.Name
                            : latestInteraction.SkillFromTeacher.Name;

                    vm.LatestInteractionSkillYouTeach =
                        latestInteraction.User1Id == userId
                            ? latestInteraction.SkillFromTeacher.Name
                            : latestInteraction.SkillFromRequester.Name;
                }
            }
            else
            {
                vm.IsLoggedIn = false;
            }

            return View(vm);
        }




        ////////////////////////////////////////////////////////////////////////////
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }



        ////////////////////////////////////////////////////////////////////////////
        public ActionResult Contact()
        {
            ViewBag.SupportEmail = Environment.GetEnvironmentVariable("SKILLBRIDGE_SUPPORT_EMAIL");
            return View();
        }

        public ActionResult HowItWorks() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Error() => View("~/Views/Shared/Error.cshtml");
    }
}
