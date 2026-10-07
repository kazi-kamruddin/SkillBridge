using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using SkillBridge.Models;

namespace SkillBridge
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // PostgreSQL tables are installed explicitly with database/postgres/001_initial.sql.
            // Historical SQL Server migrations must not run against Supabase.
            Database.SetInitializer<ApplicationDbContext>(null);
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
