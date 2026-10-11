using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using SkillBridge.Helpers;
using SkillBridge.Models;

// Finalize the exact EF model used by the app without connecting to Supabase.
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseLazyLoadingProxies()
    .UseNpgsql("Host=localhost;Database=skillbridge_model_check;Username=unused;Password=unused")
    .Options;
using var db = new ApplicationDbContext(options);
_ = db.Model.GetEntityTypes().Count();
Console.WriteLine("Core EF model checks passed.");

using var writer = new StringWriter();
PostTextFormatter.Render("## Hello\n**bold** and *italic* <script>alert(1)</script>").WriteTo(writer, HtmlEncoder.Default);
var formatted = writer.ToString();
if (!formatted.Contains("<h3>Hello</h3>") ||
    !formatted.Contains("<strong>bold</strong>") ||
    !formatted.Contains("<em>italic</em>") ||
    formatted.Contains("<script>") ||
    !formatted.Contains("&lt;script&gt;"))
    throw new Exception("Post formatting must preserve basic styles while encoding HTML.");
Console.WriteLine("Post formatting checks passed.");
