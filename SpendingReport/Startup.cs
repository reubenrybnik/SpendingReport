using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SpendingReport.Startup))]
namespace SpendingReport
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
