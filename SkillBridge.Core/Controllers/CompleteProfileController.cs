
using SkillBridge.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class CompleteProfileController : Controller
    {
        private readonly ApplicationDbContext db;

        public CompleteProfileController(ApplicationDbContext db) => this.db = db;


        ////////////////////////////////////////////////////////////////////////////
        // GET: /CompleteProfile/

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            if (db.UserInformations.Any(ui => ui.UserId == userId))
                return RedirectToAction("Index", "Profile");

            var skillData = db.SkillCategories
                .Include("Skills.SkillStages") 
                .ToList();

            var model = new CompleteProfileViewModel
            {
                AllSkillCategories = skillData
            };

            return View(model);
        }



        ////////////////////////////////////////////////////////////////////////////
        // POST: /CompleteProfile/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CompleteProfileViewModel model)
        {
            var userId = User.Identity.GetUserId();
            if (db.UserInformations.Any(ui => ui.UserId == userId))
                return RedirectToAction("Index", "Profile");

            var learningIds = model.SkillsToLearn ?? new List<int>();
            var teachingSkills = model.SkillsIKnow ?? new List<CompleteProfileViewModel.UserKnownSkill>();
            var validSkillIds = new HashSet<int>(db.Skills.Select(s => s.Id).ToList());
            var maxStageBySkill = db.SkillStages.ToList()
                .GroupBy(s => s.SkillId)
                .ToDictionary(g => g.Key, g => g.Max(s => s.StageNumber));

            if (!learningIds.Any() || !teachingSkills.Any())
                ModelState.AddModelError("", "Choose at least one skill to learn and one skill to teach.");
            if (learningIds.Any(id => !validSkillIds.Contains(id)) ||
                teachingSkills.Any(s => !validSkillIds.Contains(s.SkillId) ||
                    !maxStageBySkill.ContainsKey(s.SkillId) ||
                    s.KnownUpToStage < 1 || s.KnownUpToStage > maxStageBySkill[s.SkillId]))
                ModelState.AddModelError("", "Choose valid skills and stages.");
            if (teachingSkills.Select(s => s.SkillId).Distinct().Count() != teachingSkills.Count ||
                teachingSkills.Any(s => learningIds.Contains(s.SkillId)))
                ModelState.AddModelError("", "A skill cannot appear twice or be both taught and learned.");

            if (!ModelState.IsValid)
            {
                model.AllSkillCategories = db.SkillCategories
                    .Include("Skills.SkillStages")
                    .ToList();
                return View(model);
            }

            var userInfo = new UserInformation
            {
                UserId = userId,
                FullName = model.FullName,
                Age = model.Age,
                Profession = model.Profession,
                Location = model.Location,
                Bio = model.Bio
            };
            db.UserInformations.Add(userInfo);

            if (model.SkillsToLearn != null && model.SkillsToLearn.Any())
            {
                foreach (var skillId in model.SkillsToLearn.Distinct())
                {
                    if (validSkillIds.Contains(skillId))
                    {
                        db.UserSkills.Add(new UserSkill
                        {
                            UserId = userId,
                            SkillId = skillId,
                            Status = "Learning",
                            KnownUpToStage = 0
                        });
                    }
                }
            }

            if (model.SkillsIKnow != null && model.SkillsIKnow.Any())
            {
                foreach (var skillKnown in model.SkillsIKnow)
                {
                    if (skillKnown.SkillId > 0 && validSkillIds.Contains(skillKnown.SkillId))
                    {
                        db.UserSkills.Add(new UserSkill
                        {
                            UserId = userId,
                            SkillId = skillKnown.SkillId,
                            Status = "Teaching",
                            KnownUpToStage = skillKnown.KnownUpToStage
                        });
                    }
                }
            }

            db.SaveChanges();

            return RedirectToAction("Index", "Home");
        }




        ////////////////////////////////////////////////////////////////////////////
        
    }
}
