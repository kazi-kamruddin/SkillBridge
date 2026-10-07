using System.Threading.Tasks;
using System.Web;
using System;
using System.Diagnostics;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using SkillBridge.Models;

namespace SkillBridge.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController() { }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get => _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            private set => _signInManager = value;
        }

        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }



        ////////////////////////////////////////////////////////////////////////////
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }



        ////////////////////////////////////////////////////////////////////////////
        // POST: /Account/Login


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: true);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }



        ////////////////////////////////////////////////////////////////////////////
        // GET: /Account/Register


        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }



        ////////////////////////////////////////////////////////////////////////////
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await UserManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                using (var db = new ApplicationDbContext())
                {
                    var userRating = new UserRating
                    {
                        UserId = user.Id,
                        InteractionsCompleted = 0,
                        RatingsReceived = 0,
                        AccumulatedRating = 0
                    };

                    db.UserRatings.Add(userRating);
                    db.SaveChanges();
                }

                return RedirectToAction("Index", "CompleteProfile");
            }

            AddErrors(result);
            return View(model);
        }




        ////////////////////////////////////////////////////////////////////////////
        // GET: /Account/ForgotPassword


        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }




        ////////////////////////////////////////////////////////////////////////////
        // POST: /Account/ForgotPassword

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            Uri publicUrl;
            if (!EmailService.IsConfigured || !TryGetPublicUrl(out publicUrl))
            {
                ModelState.AddModelError("", "Password reset is unavailable right now. Please contact support.");
                return View(model);
            }

            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                try
                {
                    var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                    var callback = new Uri(publicUrl, Url.Action("ResetPassword", "Account", new { code }));
                    await UserManager.SendEmailAsync(user.Id, "Reset your SkillBridge password",
                        "To reset your SkillBridge password, open this link within one hour:\n\n" + callback +
                        "\n\nIf you did not request this, you can ignore this email.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Password reset email could not be sent: {0}", ex);
                }
            }

            return View("ForgotPasswordConfirmation");
        }

        private static bool TryGetPublicUrl(out Uri publicUrl)
        {
            var configured = Environment.GetEnvironmentVariable("SKILLBRIDGE_PUBLIC_URL");
            if (!Uri.TryCreate(configured, UriKind.Absolute, out publicUrl) ||
                (publicUrl.Scheme != Uri.UriSchemeHttps &&
                 !(publicUrl.Scheme == Uri.UriSchemeHttp && publicUrl.IsLoopback)) ||
                !string.IsNullOrEmpty(publicUrl.UserInfo) ||
                !string.IsNullOrEmpty(publicUrl.Query) ||
                !string.IsNullOrEmpty(publicUrl.Fragment))
                return false;
            return true;
        }




        ////////////////////////////////////////////////////////////////////////////
        // GET: /Account/ResetPassword

        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return string.IsNullOrWhiteSpace(code)
                ? View("Error")
                : View(new ResetPasswordViewModel { Code = code });
        }




        ////////////////////////////////////////////////////////////////////////////
        // POST: /Account/ResetPassword

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");

            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");

            AddErrors(result);
            return View(model);
        }



        ////////////////////////////////////////////////////////////////////////////
        // GET: /Account/ResetPasswordConfirmation

        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }



        ////////////////////////////////////////////////////////////////////////////
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        #region Helpers

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
