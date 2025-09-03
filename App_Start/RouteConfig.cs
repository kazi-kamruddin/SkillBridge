using System.Web.Mvc;
using System.Web.Routing;

public class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

        // Route for Communities Index page
        routes.MapRoute(
            name: "CommunitiesIndex",
            url: "Communities",
            defaults: new { controller = "Communities", action = "Index" }
        );

        // Route for individual skill community
        routes.MapRoute(
            name: "CommunitySkill",
            url: "Communities/{id}",
            defaults: new { controller = "Communities", action = "Skill" }
        );

        // Default fallback route
        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
        );
    }
}
