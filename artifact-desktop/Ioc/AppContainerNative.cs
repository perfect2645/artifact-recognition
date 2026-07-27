using Logging;
using Microsoft.Extensions.DependencyInjection;
using Utils.Configuration;

namespace Ioc;

public sealed class AppContainerNative : IDisposable
{
    private static readonly Lazy<AppContainerNative> _instance = new(() => new AppContainerNative());
    public static AppContainerNative Instance { get; }

    public static IServiceCollection Services { get; }
    private static readonly Lazy<IServiceProvider> _serviceProvider;
    private static bool _disposed;

    static AppContainerNative()
    {
        Instance = _instance.Value;
        Services = new ServiceCollection();
        Services.AddSingleton(Instance);
        
        _serviceProvider = new Lazy<IServiceProvider>(() => 
                Services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                
            }), LazyThreadSafetyMode.ExecutionAndPublication
        );
        
        _disposed = false;
    }

    private AppContainerNative()
    {
    }

    private IServiceProvider ServiceProvider => _serviceProvider.Value;


    public void Build() => _ = ServiceProvider;

    #region Helper Methods

    public TService? GetService<TService>() where TService : notnull
    {
        try
        {
            return ServiceProvider.GetRequiredService<TService>();
        }
        catch (Exception ex)
        {
            Log4Logger.Logger.Error(ex);
            return default;
        }
    }

    public TService? GetKeyedService<TService>(string key) where TService : notnull
    {
        try
        {
            return ServiceProvider.GetRequiredKeyedService<TService>(key);
        }
        catch (Exception ex)
        {
            Log4Logger.Logger.Error(ex);
            return default;
        }
    }

    public void AddConfiguration<TOptions>(string queryString) where TOptions : class
    {
        Services.Configure<TOptions>(AppConfig.Configuration!.GetSection(queryString));
    }

    #endregion Helper Methods
    
    
    public void Dispose()
    {
        if (_disposed) return;
        (_serviceProvider.Value as IDisposable)?.Dispose();
        _disposed = true;
    }
}