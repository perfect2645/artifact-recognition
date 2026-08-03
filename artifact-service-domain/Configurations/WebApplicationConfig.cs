using artifact.service.domain.Hubs;
using NetUtils.Aspnet.Configurations;

namespace artifact.service.domain.Configurations
{
    public static class WebApplicationConfig
    {
        extension(WebApplication app)
        {
            public void ConfigApplication()
            {
                app.ConfigApp();
                app.MapHub<SignalRHub>("/signalrHub");
            }
        }
    }
}
