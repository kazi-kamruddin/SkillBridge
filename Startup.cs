using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SkillBridge.Startup))]
namespace SkillBridge
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            app.MapSignalR();
        }
    }
}
