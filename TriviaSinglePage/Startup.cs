using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TriviaSinglePage.Startup))]
namespace TriviaSinglePage
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
