using artifact.desktop.Messaging.Signalr;
using artifact.desktop.Views;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace artifact.desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    public static IHost AppHost { get; private set; } = null!;

    [STAThread]
    private static void Main(string[] args)
    {
        InitBootstrapperLogger();

        try
        {
            AppHost = CreateHostBuilder(args).Build();

            AppHost.Start();

        }
        catch (Exception ex)
        {
            Log.Logger.Error("An error occurred while starting AppHost: {ex.Message}", ex.Message);
            if (AppHost != null)
            {
                AppHost.StopAsync().GetAwaiter().GetResult();
                AppHost.Dispose();
            }
            return;
        }

        Log.Information("AppHost started successfully.");

        App app = new();
        app.InitializeComponent();

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        app.Run(mainWindow);
    }

    private static void InitBootstrapperLogger()
    {
        SeriLogger.CreateBootstrapLogger(rollingInterval: RollingInterval.Month);
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        try
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .AddSerilogger()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureHostConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    //// bind configuration sections
                    //var appsettings = AppConfig.Configuration!.GetSection("AppSettings");
                    //services.Configure<AppSettings>(appsettings);

                    //var signalrSection = AppConfig.Configuration!.GetSection("SignalR");
                    //services.Configure<SignalRSettings>(signalrSection);

                    services.AddSingleton(_ => Current.Dispatcher);
                    services.AddSingleton<WeakReferenceMessenger>();
                    services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider => provider.GetRequiredService<WeakReferenceMessenger>());

                    // register SignalR client
                    services.AddSingleton<ISignalRClient<string>, SignalRClient<string>>();
                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    // keep auto registration behavior
                    var assemblies = new[]
                    {
                        Assembly.GetEntryAssembly()!
                    };
                    builder.RegisterModule(new Utils.Ioc.AutoRegisterModule(assemblies));
                });

            return hostBuilder;
        }
        catch (Exception ex)
        {
            Log.Logger.Error("An error occurred while creating the host builder: {ex.Message}", ex.Message);
            throw;
        }
    }

    public App()
    {
        DispatcherUnhandledException += UnHandledExceptionHandler;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }
    
    private void UnHandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
       Log.Error($"An unhandled exception occurred: {args.Exception?.Message}");
        args.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            //await _host.StopAsync();
            //_host.Dispose();
        }
        catch (Exception ex)
        {
            Log4Logger.Logger.Error(ex.Message);
        }

        base.OnExit(e);

        DispatcherUnhandledException -= UnHandledExceptionHandler;
    }
}