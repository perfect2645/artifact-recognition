using Microsoft.Extensions.DependencyInjection;

namespace artifact.desktop.Generic
{
    public static class IocExtensions
    {
        extension(App app)
        {
            public static T GetService<T>() where T : class
            {
                return App.AppHost.Services.GetRequiredService<T>();
            }

            public static T GetKeyedService<T>(string key) where T : class
            {
                return App.AppHost.Services.GetRequiredKeyedService<T>(key);
            }
        }
    }
}
