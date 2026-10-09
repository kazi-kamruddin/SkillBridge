using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;
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
        ViewBag.GoogleEnabled = GoogleEnabled;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
    {
        if (!ModelState.IsValid) return LoginView(model, returnUrl);

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password,
            model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        if (result.IsLockedOut)
        {
            logger.LogWarning("An account was locked out after failed sign-in attempts");
            return View("Lockout");
        }
        if (result.IsNotAllowed)
        {
            ModelState.AddModelError("", "Confirm your email before signing in. You can request a new link below.");
            return LoginView(model, returnUrl);
        }

        logger.LogWarning("A sign-in attempt failed");
        ModelState.AddModelError("", "Invalid login attempt.");
        return LoginView(model, returnUrl);
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        ViewBag.GoogleEnabled = GoogleEnabled;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewBag.GoogleEnabled = GoogleEnabled;
        if (!ModelState.IsValid) return View(model);
        if (!emailSender.IsConfigured || !TryGetPublicUrl(out _))
        {
            ModelState.AddModelError("", "Registration is temporarily unavailable while email verification is being configured.");
            return View(model);
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        db.UserRatings.Add(new UserRating { UserId = user.Id });
        await db.SaveChangesAsync();
        ViewBag.EmailDeliveryFailed = !await SendConfirmationAsync(user);
        return View("CheckEmail");
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)) return BadRequest();
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return BadRequest();
        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (FormatException) { return BadRequest(); }
        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded ? View("EmailConfirmed") : BadRequest("This confirmation link is invalid or expired.");
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> ResendConfirmation(string email)
    {
        if (emailSender.IsConfigured && TryGetPublicUrl(out _) &&
            !string.IsNullOrWhiteSpace(email) && email.Length <= 254)
        {
            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user != null && !await userManager.IsEmailConfirmedAsync(user))
                await SendConfirmationAsync(user);
        }
        return View("CheckEmail");
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public IActionResult GoogleSignIn(string returnUrl)
    {
        if (!GoogleEnabled) return NotFound();
        var callback = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        return Challenge(signInManager.ConfigureExternalAuthenticationProperties("Google", callback), "Google");
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string returnUrl, string remoteError)
    {
        if (!string.IsNullOrEmpty(remoteError)) return RedirectToAction(nameof(Login));
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null || info.LoginProvider != "Google") return RedirectToAction(nameof(Login));

        var signedIn = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: false);
        if (signedIn.Succeeded)
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
        if (signedIn.IsLockedOut) return View("Lockout");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var verified = string.Equals(info.Principal.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        if (!verified || string.IsNullOrWhiteSpace(email))
        {
            TempData["LoginNotice"] = "Google did not provide a verified email address.";
            return RedirectToAction(nameof(Login));
        }
        if (await userManager.FindByEmailAsync(email) != null)
        {
            TempData["LoginNotice"] = "An account already uses this email. Sign in with its password, then link Google from your profile.";
            return RedirectToAction(nameof(Login));
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded) return RedirectToAction(nameof(Login));
        var linked = await userManager.AddLoginAsync(user, info);
        if (!linked.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Login));
        }
        db.UserRatings.Add(new UserRating { UserId = user.Id });
        await db.SaveChangesAsync();
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "CompleteProfile");
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(RateLimitPolicies.AccountWrites)]
    public async Task<IActionResult> LinkGoogle()
    {
        if (!GoogleEnabled) return NotFound();
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var callback = Url.Action(nameof(LinkGoogleCallback), "Account");
        return Challenge(signInManager.ConfigureExternalAuthenticationProperties("Google", callback, user.Id), "Google");
    }

    [HttpGet]
    public async Task<IActionResult> LinkGoogleCallback(string remoteError)
    {
        if (!string.IsNullOrEmpty(remoteError)) return RedirectToAction("Index", "Profile");
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
        if (info?.LoginProvider != "Google" ||
            !string.Equals(info.Principal.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(info.Principal.FindFirstValue(ClaimTypes.Email), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AccountNotice"] = "Choose a Google account with the same verified email as your SkillBridge account.";
            return RedirectToAction("Index", "Profile");
        }
        var result = await userManager.AddLoginAsync(user, info);
        TempData["AccountNotice"] = result.Succeeded ? "Google sign-in is linked." : "This Google account could not be linked.";
        return RedirectToAction("Index", "Profile");
    }

    private bool GoogleEnabled => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKILLBRIDGE_GOOGLE_CLIENT_ID")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKILLBRIDGE_GOOGLE_CLIENT_SECRET"));

    private IActionResult LoginView(LoginViewModel model, string returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.GoogleEnabled = GoogleEnabled;
        return View("Login", model);
    }

    private async Task<bool> SendConfirmationAsync(ApplicationUser user)
    {
        try
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            TryGetPublicUrl(out var publicUrl);
            var callback = new Uri(publicUrl, Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }));
            await emailSender.SendAsync(user.Email, "Confirm your SkillBridge email",
                "Confirm your email to use SkillBridge:\n\n" + callback + "\n\nIf you did not register, ignore this message.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email confirmation could not be sent");
            return false;
        }
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
