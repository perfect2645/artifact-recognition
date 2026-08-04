
using artifact.desktop.Configurations;
using artifact.desktop.Views;
using Ioc;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using artifact.desktop.Messaging.Signalr;
using System.Windows;
using System.Windows.Threading;
using Utils.Configuration;
using Microsoft.Extensions.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Reflection;

namespace artifact.desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost _host = null!;

    public App()
    {
        DispatcherUnhandledException += UnHandledExceptionHandler;
        InitServices();
    }

    private void InitServices()
    {
        SetupAppConfig();
        ConfigViews();

        // build generic host with Autofac as the service provider factory
        _host = Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((context, services) =>
            {
                // bind configuration sections
                var appsettings = AppConfig.Configuration!.GetSection("AppSettings");
                services.Configure<AppSettings>(appsettings);

                var signalrSection = AppConfig.Configuration!.GetSection("SignalR");
                services.Configure<SignalRSettings>(signalrSection);

                // register SignalR client
                services.AddSingleton<ISignalRClient, SignalRClient>();
                services.AddSingleton<IHostedService>(sp => (IHostedService)sp.GetRequiredService<ISignalRClient>());
            })
            .ConfigureContainer<ContainerBuilder>((context, builder) =>
            {
                // keep auto registration behavior
                var assemblies = new[] { Assembly.GetEntryAssembly() };
                builder.RegisterModule(new Utils.Ioc.AutoRegisterModule(assemblies));
            })
            .Build();
    }
    
    private void SetupAppConfig()
    {
        AppConfig.Init();

        var appsettings = AppConfig.Configuration!.GetSection("AppSettings");
        AppContainer.Instance.Services.Configure<AppSettings>(appsettings);
    }

    private void ConfigViews()
    {
        //AppContainer.Instance.Services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // start host and show main window
        if (_host != null)
        {
            await _host.StartAsync();

            // allow AppContainer to resolve from host when needed
            AppContainer.Instance.SetHostServiceProvider(_host.Services);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
    
    private void UnHandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Log4Logger.Logger.Error($"An unhandled exception occurred: {args.Exception?.Message}");
        args.Handled = true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        catch (Exception ex)
        {
            Log4Logger.Logger.Error(ex.Message);
        }

        base.OnExit(e);
    }
}