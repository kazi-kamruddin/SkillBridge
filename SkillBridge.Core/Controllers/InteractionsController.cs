
using SkillBridge.Models;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SkillBridge.Services;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class InteractionsController : Controller
    {
        private readonly ApplicationDbContext db;

        public InteractionsController(ApplicationDbContext db)
        {
            this.db = db;
        }


        ////////////////////////////////////////////////////////////////////////////

        // Interaction Index Page
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var interactions = db.Interactions
                .Where(i => i.User1Id == userId || i.User2Id == userId)
                .Where(i => i.Status != "Completed")
                .Include(i => i.User1)
                .Include(i => i.User2)
                .Include(i => i.SkillFromRequester)
                .Include(i => i.SkillFromTeacher)
                .ToList();

            var model = interactions.Select(i => new InteractionIndexViewModel
            {
                InteractionId = i.Id,
                Status = i.Status,
                EndReason = i.EndReason,
                OtherUserName = i.User1Id == userId ? i.User2.UserName : i.User1.UserName,

                SkillYouLearn = i.User1Id == userId ? i.SkillFromRequester.Name : i.SkillFromTeacher.Name,
                SkillYouTeach = i.User1Id == userId ? i.SkillFromTeacher.Name : i.SkillFromRequester.Name
            }).ToList();

            return View(model);
        }


        ////////////////////////////////////////////////////////////////////////////
        // Interaction Sessions Page

        public ActionResult Sessions(int id)
        {
            var userId = User.Identity.GetUserId();
            var interaction = db.Interactions
                .Include(i => i.Sessions.Select(s => s.Skill))
                .Include(i => i.SkillFromRequester.SkillStages)
                .Include(i => i.SkillFromTeacher.SkillStages)
                .FirstOrDefault(i => i.Id == id && i.Status == "Ongoing" &&
                    (i.User1Id == userId || i.User2Id == userId));

            if (interaction == null) return NotFound();

            var model = new InteractionSessionsViewModel
            {
                InteractionId = interaction.Id,
                UserId = userId,
                SkillBlocks = BuildSkillBlocks(interaction),
                MeetingStartUtc = interaction.MeetingStartUtc,
                MeetingFormat = interaction.MeetingFormat,
                MeetingNote = interaction.MeetingNote,
                MeetingStatus = interaction.MeetingStatus,
                CanRespondToMeeting = interaction.MeetingStatus == "Proposed" && interaction.MeetingProposedByUserId != userId
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult ProposeMeeting(int id, string startsAtUtc, string format, string note)
        {
            var userId = User.Identity.GetUserId();
            var interaction = db.Interactions.FirstOrDefault(i => i.Id == id && i.Status == "Ongoing" &&
                (i.User1Id == userId || i.User2Id == userId));
            if (interaction == null) return NotFound();
            if (!DateTimeOffset.TryParse(startsAtUtc, out var startsAt) ||
                startsAt.UtcDateTime < DateTime.UtcNow.AddMinutes(30) ||
                startsAt.UtcDateTime > DateTime.UtcNow.AddDays(180))
                return BadRequest("Choose a future time within six months.");
            if (format != "Online" && format != "In person") return BadRequest("Choose a meeting format.");
            note = (note ?? "").Trim();
            if (note.Length > 300 || (format == "In person" && note.Length == 0))
                return BadRequest("Add a meeting place for an in-person session, within 300 characters.");
            interaction.MeetingStartUtc = DateTime.SpecifyKind(startsAt.UtcDateTime, DateTimeKind.Unspecified);
            interaction.MeetingFormat = format;
            interaction.MeetingNote = note;
            interaction.MeetingProposedByUserId = userId;
            interaction.MeetingStatus = "Proposed";
            NotifyOther(interaction, userId, "The other member proposed a meeting time. Open your exchange to respond.");
            db.SaveChanges();
            return RedirectToAction(nameof(Sessions), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult RespondToMeeting(int id, string decision)
        {
            var userId = User.Identity.GetUserId();
            var interaction = db.Interactions.FirstOrDefault(i => i.Id == id && i.Status == "Ongoing" &&
                (i.User1Id == userId || i.User2Id == userId));
            if (interaction == null) return NotFound();
            if (interaction.MeetingStatus != "Proposed" || interaction.MeetingProposedByUserId == userId)
                return StatusCode(409);
            if (decision != "Accept" && decision != "Decline") return BadRequest();
            interaction.MeetingStatus = decision == "Accept" ? "Confirmed" : "Declined";
            NotifyOther(interaction, userId, decision == "Accept" ?
                "Your meeting time was accepted." : "Your meeting time was declined. You can propose another.");
            db.SaveChanges();
            return RedirectToAction(nameof(Sessions), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult CancelMeeting(int id)
        {
            var userId = User.Identity.GetUserId();
            var interaction = db.Interactions.FirstOrDefault(i => i.Id == id && i.Status == "Ongoing" &&
                (i.User1Id == userId || i.User2Id == userId));
            if (interaction == null) return NotFound();
            if (interaction.MeetingStatus != "Confirmed" && interaction.MeetingStatus != "Proposed")
                return StatusCode(409);
            interaction.MeetingStatus = "Cancelled";
            NotifyOther(interaction, userId, "The planned meeting was cancelled. Open your exchange to propose a new time.");
            db.SaveChanges();
            return RedirectToAction(nameof(Sessions), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.MemberWrites)]
        public ActionResult SaveStageNote(int sessionId, string whatWeCovered, string nextStep)
        {
            var userId = User.Identity.GetUserId();
            var session = db.InteractionSessions.Include(s => s.Interaction)
                .FirstOrDefault(s => s.Id == sessionId && s.Interaction.Status == "Ongoing" &&
                    (s.Interaction.User1Id == userId || s.Interaction.User2Id == userId));
            if (session == null) return NotFound();
            whatWeCovered = (whatWeCovered ?? "").Trim();
            nextStep = (nextStep ?? "").Trim();
            if (whatWeCovered.Length is < 1 or > 1000 || nextStep.Length > 500)
                return BadRequest("Keep your note within the field limits.");
            var note = db.InteractionSessionNotes.FirstOrDefault(n => n.InteractionSessionId == sessionId && n.UserId == userId);
            if (note == null)
            {
                note = new InteractionSessionNote { InteractionSessionId = sessionId, UserId = userId };
                db.InteractionSessionNotes.Add(note);
            }
            note.WhatWeCovered = whatWeCovered;
            note.NextStep = nextStep;
            note.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            return RedirectToAction(nameof(Sessions), new { id = session.InteractionId });
        }

        private void NotifyOther(Interaction interaction, string userId, string message)
        {
            db.Notifications.Add(new Notification
            {
                UserId = interaction.User1Id == userId ? interaction.User2Id : interaction.User1Id,
                Type = "Info", ReferenceId = interaction.Id, Message = message, CreatedAt = DateTime.Now
            });
        }



        ////////////////////////////////////////////////////////////////////////////

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkStageDone(int stageNumber, int interactionId, int skillId)
        {
            var userId = User.Identity.GetUserId();
            var session = db.InteractionSessions
                .Include(s => s.Interaction)
                .Include(s => s.Skill)
                .FirstOrDefault(s => s.InteractionId == interactionId
                                     && s.SkillId == skillId
                                     && s.StageNumber == stageNumber);

            if (session == null) return NotFound();

            if (session.Interaction.Status != "Ongoing" ||
                (session.Interaction.User1Id != userId && session.Interaction.User2Id != userId))
                return StatusCode(403);

            if (db.InteractionSessions.Any(s => s.InteractionId == interactionId &&
                s.SkillId == skillId && s.StageNumber < stageNumber &&
                (!s.User1Confirmed || !s.User2Confirmed)))
                return StatusCode(409);

            if ((session.Interaction.User1Id == userId && session.User1Confirmed) ||
                (session.Interaction.User2Id == userId && session.User2Confirmed))
                return Json(new { success = true, status = session.Status });

            if (session.Interaction.User1Id == userId)
                session.User1Confirmed = true;
            else if (session.Interaction.User2Id == userId)
                session.User2Confirmed = true;

            if (session.User1Confirmed && session.User2Confirmed && session.Status != "Confirmed")
            {
                session.Status = "Confirmed";

                db.Notifications.Add(new Notification
                {
                    UserId = session.Interaction.User1Id,
                    Type = "Info",
                    Message = $"Stage {session.StageNumber} of {session.Skill.Name} completed!"
                });
                db.Notifications.Add(new Notification
                {
                    UserId = session.Interaction.User2Id,
                    Type = "Info",
                    Message = $"Stage {session.StageNumber} of {session.Skill.Name} completed!"
                });
            }

            db.SaveChanges();

            return Json(new
            {
                success = true,
                status = session.Status
            });
        }



        ////////////////////////////////////////////////////////////////////////////
        // End Interaction

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StopInteraction(int id, string reason)
        {
            var userId = User.Identity.GetUserId();
            var interaction = db.Interactions.FirstOrDefault(i => i.Id == id &&
                (i.User1Id == userId || i.User2Id == userId));
            if (interaction == null) return NotFound();
            if (interaction.Status != "Ongoing") return StatusCode(409);
            reason = (reason ?? "").Trim();
            if (reason.Length is < 1 or > 500) return BadRequest("Give a reason of at most 500 characters.");
            interaction.Status = "Ended";
            interaction.EndReason = reason;
            interaction.EndedByUserId = userId;
            interaction.EndedAt = DateTime.Now;
            db.Notifications.Add(new Notification
            {
                UserId = interaction.User1Id == userId ? interaction.User2Id : interaction.User1Id,
                Type = "Info",
                ReferenceId = id,
                Message = "The other member ended your exchange. Open Interactions to see the reason.",
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EndInteraction(int id)
        {
            var interaction = db.Interactions
                .Include(i => i.Sessions)
                .Include(i => i.User1)
                .Include(i => i.User2)
                .Include(i => i.SkillFromRequester)
                .Include(i => i.SkillFromTeacher)
                .FirstOrDefault(i => i.Id == id);

            if (interaction == null) return NotFound();
            var userId = User.Identity.GetUserId();
            if (interaction.User1Id != userId && interaction.User2Id != userId)
                return StatusCode(403);
            if (interaction.Status != "Ongoing" || !interaction.Sessions.Any() ||
                interaction.Sessions.Any(s => !s.User1Confirmed || !s.User2Confirmed))
                return StatusCode(409);

            UpdateUserSkill(interaction.User1Id, interaction.SkillFromRequesterId, interaction);
            UpdateUserSkill(interaction.User2Id, interaction.SkillFromTeacherId, interaction);

            interaction.Status = "Completed";

            var user1Rating = db.UserRatings.FirstOrDefault(r => r.UserId == interaction.User1Id);
            var user2Rating = db.UserRatings.FirstOrDefault(r => r.UserId == interaction.User2Id);

            if (user1Rating != null) user1Rating.InteractionsCompleted += 1;
            if (user2Rating != null) user2Rating.InteractionsCompleted += 1;

            db.SaveChanges();
            CreateFeedbackNotification(interaction);
            return RedirectToAction("Index");
        }

        private void UpdateUserSkill(string userId, int skillId, Interaction interaction)
        {
            if (skillId == 0) return;

            var userSkill = db.UserSkills.FirstOrDefault(us => us.UserId == userId && us.SkillId == skillId);

            if (userSkill == null)
            {
                userSkill = new UserSkill
                {
                    UserId = userId,
                    SkillId = skillId,
                    Status = "Teaching"
                };
                db.UserSkills.Add(userSkill);
            }

            userSkill.KnownUpToStage = MaxStageCompleted(interaction, skillId);
            userSkill.Status = "Teaching";
        }



        ////////////////////////////////////////////////////////////////////////////

        // Show rating page
        public ActionResult RateInteraction(int interactionId)
        {
            var userId = User.Identity.GetUserId();
            var interaction = db.Interactions
                .Include(i => i.SkillFromRequester)
                .Include(i => i.SkillFromTeacher)
                .Include(i => i.User1)
                .Include(i => i.User2)
                .FirstOrDefault(i => i.Id == interactionId && i.Status == "Completed" &&
                    (i.User1Id == userId || i.User2Id == userId));

            if (interaction == null) return NotFound();

            var ratingModel = new InteractionRatingViewModel
            {
                InteractionId = interaction.Id,
                SkillName = interaction.User1Id == userId ? interaction.SkillFromTeacher.Name : interaction.SkillFromRequester.Name,
                FromUserName = interaction.User1Id == userId ? interaction.User2.UserName : interaction.User1.UserName,
                ToUserId = userId
            };

            var indexModel = new InteractionIndexViewModel
            {
                InteractionId = interaction.Id,
                OtherUserName = ratingModel.FromUserName,
                SkillYouLearn = interaction.User1Id == userId ? interaction.SkillFromRequester.Name : interaction.SkillFromTeacher.Name,
                SkillYouTeach = interaction.User1Id == userId ? interaction.SkillFromTeacher.Name : interaction.SkillFromRequester.Name,
                Status = interaction.Status
            };

            var model = new InteractionFeedbackViewModel
            {
                RatingModel = ratingModel,
                IndexModel = indexModel
            };

            return View(model);
        }




        ////////////////////////////////////////////////////////////////////////////
        // Submit rating via AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitRating(InteractionRatingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return Json(new { success = false, errors });
            }

            var currentUserId = User.Identity.GetUserId();
            var interaction = db.Interactions.Find(model.InteractionId);
            if (interaction == null)
                return Json(new { success = false, errors = new[] { "Interaction not found." } });

            if (interaction.Status != "Completed" ||
                (interaction.User1Id != currentUserId && interaction.User2Id != currentUserId))
                return StatusCode(403);
            if (db.Ratings.Any(r => r.InteractionId == model.InteractionId && r.FromUserId == currentUserId))
                return Json(new { success = false, errors = new[] { "You already rated this interaction." } });

            var recipientId = interaction.User1Id == currentUserId ? interaction.User2Id : interaction.User1Id;

            var rating = new Rating
            {
                InteractionId = model.InteractionId,
                FromUserId = currentUserId,
                ToUserId = recipientId,
                RatingValue = model.RatingValue,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            };

            db.Ratings.Add(rating);

            var recipientStats = db.UserRatings.FirstOrDefault(ur => ur.UserId == recipientId);
            if (recipientStats != null)
            {
                recipientStats.RatingsReceived += 1;
                recipientStats.AccumulatedRating += rating.RatingValue;
            }

            db.Notifications.Add(new Notification
            {
                UserId = recipientId,
                Type = "Info",
                ReferenceId = model.InteractionId,
                Message = $"{User.Identity.Name} rated you {model.RatingValue}/10. Comment: \"{model.Comment}\"",
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            var notif = db.Notifications.FirstOrDefault(n =>
                n.UserId == currentUserId && n.Type == "Feedback" && n.ReferenceId == model.InteractionId);
            if (notif != null) db.Notifications.Remove(notif);

            db.SaveChanges();

            return Json(new { success = true });
        }




        ////////////////////////////////////////////////////////////////////////////
        // Helper Methods

        private List<SkillStageBlock> BuildSkillBlocks(Interaction interaction)
        {
            var userId = User.Identity.GetUserId();
            var blocks = new List<SkillStageBlock>();
            var notes = db.InteractionSessionNotes
                .Where(n => n.InteractionSession.InteractionId == interaction.Id)
                .ToList().GroupBy(n => n.InteractionSessionId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var sessionsBySkill = interaction.Sessions
                .OrderBy(s => s.StageNumber)
                .GroupBy(s => s.SkillId);

            foreach (var skillGroup in sessionsBySkill)
            {
                bool nextStagePending = true;
                foreach (var session in skillGroup)
                {
                    var stageEntity = db.SkillStages
                        .FirstOrDefault(st => st.SkillId == session.SkillId && st.StageNumber == session.StageNumber);

                    string description = stageEntity != null ? stageEntity.Description : "(no description)";

                    bool confirmed = (session.Interaction.User1Id == userId && session.User1Confirmed) ||
                                     (session.Interaction.User2Id == userId && session.User2Confirmed);

                    string status;
                    bool isLocked = false;

                    if (session.User1Confirmed && session.User2Confirmed)
                    {
                        status = "Green";
                        nextStagePending = true;
                    }
                    else if (nextStagePending)
                    {
                        status = "Yellow";
                        nextStagePending = false;
                    }
                    else
                    {
                        status = "Red";
                        isLocked = true;
                    }

                    blocks.Add(new SkillStageBlock
                    {
                        SessionId = session.Id,
                        StageNumber = session.StageNumber,
                        SkillId = session.SkillId,
                        Description = description,
                        Status = status,
                        UserConfirmed = confirmed,
                        IsLocked = isLocked,
                        Notes = notes.TryGetValue(session.Id, out var sessionNotes)
                            ? sessionNotes.Select(n => new StageNoteViewModel
                            {
                                IsMine = n.UserId == userId,
                                WhatWeCovered = n.WhatWeCovered,
                                NextStep = n.NextStep,
                                UpdatedAt = n.UpdatedAt
                            }).ToList() : new List<StageNoteViewModel>()
                    });
                }
            }

            return blocks.OrderBy(b => b.SkillId).ThenBy(b => b.StageNumber).ToList();
        }




        ////////////////////////////////////////////////////////////////////////////
        private int MaxStageCompleted(Interaction interaction, int skillId)
        {
            var sessions = interaction.Sessions.Where(s => s.SkillId == skillId).ToList();
            if (!sessions.Any()) return 0;

            return sessions
                .Where(s => s.User1Confirmed && s.User2Confirmed)
                .Select(s => s.StageNumber)
                .DefaultIfEmpty(0)
                .Max();
        }


        ////////////////////////////////////////////////////////////////////////////

        private void CreateFeedbackNotification(Interaction interaction)
        {
            var notif1 = new Notification
            {
                UserId = interaction.User1Id,
                Type = "Feedback",
                ReferenceId = interaction.Id,
                Message = $"You have successfully completed the interaction with {interaction.User2.UserName}. You taught {interaction.SkillFromTeacher.Name} and learned {interaction.SkillFromRequester.Name}. Click below to rate this interaction."
            };

            var notif2 = new Notification
            {
                UserId = interaction.User2Id,
                Type = "Feedback",
                ReferenceId = interaction.Id,
                Message = $"You have successfully completed the interaction with {interaction.User1.UserName}. You taught {interaction.SkillFromRequester.Name} and learned {interaction.SkillFromTeacher.Name}. Click below to rate this interaction."
            };


            db.Notifications.Add(notif1);
            db.Notifications.Add(notif2);
            db.SaveChanges();
        }




        ////////////////////////////////////////////////////////////////////////////

    }
}
