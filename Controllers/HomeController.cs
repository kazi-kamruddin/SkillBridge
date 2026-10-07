using Microsoft.AspNet.Identity;
using SkillBridge.Helpers;
using System;
using SkillBridge.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SkillBridge.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();



        ////////////////////////////////////////////////////////////////////////////

        public ActionResult Index()
        {
            var vm = new HomePageViewModel();

            var id = User.Identity.GetUserId();
            var user = db.Users.FirstOrDefault(u => u.Id == id);
            var userInfo = db.UserInformations.FirstOrDefault(ui => ui.UserId == id);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                vm.FullName = userInfo?.FullName ?? "";
                vm.IsLoggedIn = true;
                vm.MotivationalQuote = HomePageViewModel.GetRandomQuote();
                vm.MySkills = db.UserSkills
                    .Include("Skill.SkillStages")
                    .Where(us => us.UserId == userId)
                    .ToList();

                vm.MyLatestPost = db.CommunityPosts
                    .Where(p => p.CreatedByUserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

                vm.OtherLatestPost = db.CommunityPosts
                    .Where(p => p.CreatedByUserId != userId)
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
                    vm.LatestInteractionOtherUserProfileImage = ProfileImageHelper.GetRandomProfileImage();

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
    }
}
