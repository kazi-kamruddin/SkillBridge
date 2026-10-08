using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillBridge.Hubs;
using SkillBridge.Models;
using SkillBridge.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

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
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok("ok"));
app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
