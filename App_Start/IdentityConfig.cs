using System;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using SkillBridge.Models;

namespace SkillBridge
{
    // SMTP settings are supplied by the host, never stored in Web.config.
    public class EmailService : IIdentityMessageService
    {
        public static bool IsConfigured
        {
            get
            {
                var host = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_HOST");
                var from = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_FROM");
                var username = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_USERNAME");
                var password = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_PASSWORD");
                var ssl = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_SSL");
                int port;
                if (string.IsNullOrWhiteSpace(host) ||
                    !int.TryParse(Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_PORT"), out port) ||
                    port < 1 || port > 65535 ||
                    (!string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password)) ||
                    (string.Equals(ssl, "false", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) && host != "127.0.0.1"))
                    return false;
                try { return !string.IsNullOrWhiteSpace(from) && new MailAddress(from) != null; }
                catch (FormatException) { return false; }
            }
        }

        public async Task SendAsync(IdentityMessage message)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("SMTP is not configured.");

            var host = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_HOST");
            var from = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_FROM");
            var username = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_USERNAME");
            var password = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_PASSWORD");
            var portText = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_PORT");
            var sslText = Environment.GetEnvironmentVariable("SKILLBRIDGE_SMTP_SSL");
            int port;
            if (!int.TryParse(portText, out port) || port < 1 || port > 65535)
                throw new InvalidOperationException("SKILLBRIDGE_SMTP_PORT must be a valid TCP port.");

            var useSsl = !string.Equals(sslText, "false", StringComparison.OrdinalIgnoreCase);
            if (!useSsl && !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) && host != "127.0.0.1")
                throw new InvalidOperationException("SMTP encryption may only be disabled for a local test server.");

            using (var mail = new MailMessage(new MailAddress(from), new MailAddress(message.Destination)))
            using (var client = new SmtpClient(host, port))
            {
                mail.Subject = message.Subject;
                mail.Body = message.Body;
                mail.IsBodyHtml = false;
                client.EnableSsl = useSsl;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                if (!string.IsNullOrWhiteSpace(username))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }
                await client.SendMailAsync(mail);
            }
        }
    }

    // Application user manager (handles registration, login, password reset)
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store) { }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));

            // Username validation
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Password validation
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true
            };

            // Lockout settings
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Email service (needed for password reset)
            manager.EmailService = new EmailService();

            // Token provider for password reset
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(
                    dataProtectionProvider.Create("ASP.NET Identity"))
                {
                    TokenLifespan = TimeSpan.FromHours(1)
                };
            }

            return manager;
        }
    }


    // Sign-in manager (handles login, remember me, logout)
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager) { }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
