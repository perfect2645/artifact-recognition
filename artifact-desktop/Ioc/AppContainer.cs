using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Utils.Configuration;

namespace Ioc;

public sealed class AppContainer
{
    private static readonly Lazy<AppContainer> _instance = new(() => new AppContainer());
    public static AppContainer Instance => _instance.Value;

    private readonly ContainerBuilder _autofacBuilder;
    private IContainer? _autofacContainer;
    
    public IServiceCollection Services { get; }

    private AppContainer()
    {
        Services = new ServiceCollection();
        _autofacBuilder = new ContainerBuilder();
    }
    
    public void Build()
    {
        _autofacBuilder.Populate(Services);
        _autofacContainer =  _autofacBuilder.Build();
    }
    
    public T Resolve<T>() where T : notnull
    {
        if (_autofacContainer == null)
        {
            throw new InvalidOperationException("Container not built yet.");
        }
        return _autofacContainer.Resolve<T>();
    }
    
    public T ResolveKeyed<T>(object key) where T : notnull
    {
        if (_autofacContainer == null)
        {
            throw new InvalidOperationException("Container not built yet.");
        }
        return _autofacContainer.ResolveKeyed<T>(key);
    }

    public void AddConfiguration<TOptions>(string queryString) where TOptions : class
    {
        Services.Configure<TOptions>(AppConfig.Configuration!.GetSection(queryString));
    }
}