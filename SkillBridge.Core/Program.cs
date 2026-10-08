using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using System.Threading.RateLimiting;
using SkillBridge.Hubs;
using SkillBridge.Models;
using SkillBridge.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (!builder.Environment.IsDevelopment())
    builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var rawConnection = Environment.GetEnvironmentVariable("SKILLBRIDGE_DB_CONNECTION");
if (string.IsNullOrWhiteSpace(rawConnection))
    throw new InvalidOperationException("Set SKILLBRIDGE_DB_CONNECTION before starting SkillBridge.");

var connection = new NpgsqlConnectionStringBuilder(rawConnection);
var certificatePath = Environment.GetEnvironmentVariable("SKILLBRIDGE_DB_CA_CERT");
if (string.IsNullOrWhiteSpace(certificatePath))
    certificatePath = Path.Combine(builder.Environment.ContentRootPath, "certs", "prod-ca-2021.crt");
else if (!Path.IsPathRooted(certificatePath))
    certificatePath = Path.Combine(builder.Environment.ContentRootPath, certificatePath);
if (!File.Exists(certificatePath))
    throw new InvalidOperationException("The Supabase CA certificate was not found.");
connection.SslMode = SslMode.VerifyFull;
connection.RootCertificate = certificatePath;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseLazyLoadingProxies().UseNpgsql(connection.ConnectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(1));
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SkillBridge");
if (builder.Environment.IsDevelopment())
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));
else
    dataProtection.PersistKeysToDbContext<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IEmailSender, BrevoEmailSender>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SkillBridge.RateLimiting")
            .LogWarning("Request limit reached for {Endpoint}", httpContext.GetEndpoint()?.DisplayName);
        httpContext.Response.Headers.RetryAfter = "60";
        httpContext.Response.ContentType = "text/plain";
        await httpContext.Response.WriteAsync("Too many requests. Please try again in a minute.", cancellationToken);
    };

    // A shared cap protects account endpoints without trusting forwarded client-IP headers.
    options.AddFixedWindowLimiter(RateLimitPolicies.AccountWrites, limiter =>
    {
        limiter.PermitLimit = 40;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddPolicy(RateLimitPolicies.MemberWrites, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unauthenticated",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Render terminates TLS before forwarding traffic to the container.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok("ok"));
app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
