using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillBridge.Models;
using SkillBridge.Services;

namespace SkillBridge.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ApplicationDbContext db;
    private readonly IEmailSender emailSender;
    private readonly ILogger<AccountController> logger;

    public AccountController(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager, ApplicationDbContext db,
        IEmailSender emailSender, ILogger<AccountController> logger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.db = db;
        this.emailSender = emailSender;
        this.logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Login(string returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password,
            model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        if (result.IsLockedOut)
        {
            logger.LogWarning("An account was locked out after failed sign-in attempts");
            return View("Lockout");
        }

        logger.LogWarning("A sign-in attempt failed");
        ModelState.AddModelError("", "Invalid login attempt.");
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        db.UserRatings.Add(new UserRating { UserId = user.Id });
        await db.SaveChangesAsync();
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "CompleteProfile");
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!emailSender.IsConfigured || !TryGetPublicUrl(out var publicUrl))
        {
            ModelState.AddModelError("", "Password reset is unavailable right now. Please contact support.");
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            try
            {
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                var callback = new Uri(publicUrl, Url.Action("ResetPassword", "Account", new { code }));
                await emailSender.SendAsync(user.Email, "Reset your SkillBridge password",
                    "To reset your SkillBridge password, open this link within one hour:\n\n" + callback +
                    "\n\nIf you did not request this, you can ignore this email.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Password reset email could not be sent");
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

    [AllowAnonymous]
    public IActionResult ResetPassword(string code) => string.IsNullOrWhiteSpace(code)
        ? View("Error") : View(new ResetPasswordViewModel { Code = code });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction("ResetPasswordConfirmation");

        var result = await userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");
        AddErrors(result);
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOff()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
    }
}
