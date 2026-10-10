using Microsoft.EntityFrameworkCore;
using SkillBridge.Models;

// Finalize the exact EF model used by the app without connecting to Supabase.
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseLazyLoadingProxies()
    .UseNpgsql("Host=localhost;Database=skillbridge_model_check;Username=unused;Password=unused")
    .Options;
using var db = new ApplicationDbContext(options);
_ = db.Model.GetEntityTypes().Count();
Console.WriteLine("Core EF model checks passed.");
