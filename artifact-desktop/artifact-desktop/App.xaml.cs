using artifact.desktop.Configurations;
using artifact.desktop.Generic;
using artifact.desktop.Messaging.Signalr;
using artifact.desktop.Views;
using artifact.shared.data;
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
using Utils.Tasking;

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
                    // bind configuration sections
                    services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));
                    services.Configure<SignalRSettings>(context.Configuration.GetSection("AppSettings:SignalrSettings"));

                    services.AddSingleton(_ => Current.Dispatcher);
                    services.AddSingleton<IMessenger, WeakReferenceMessenger>();
                    services.AddSingleton<ISignalRClient<ArtifactMessage>, SignalRClient<ArtifactMessage>>();

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
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }

        base.OnExit(e);

        DispatcherUnhandledException -= UnHandledExceptionHandler;
    }

    private void UnHandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Log.Error($"An unhandled exception occurred: {args.Exception?.Message}");
        args.Handled = true;
    }

    private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        var ex = args.ExceptionObject as Exception;
        if (ex != null)
        {
            Log.Error(ex, "An unhandled AppDomain exception occurred: {Message}", ex.Message);
        }
        else
        {
            Log.Error("An unhandled AppDomain exception occurred. ExceptionObject: {Obj}", args.ExceptionObject);
        }
    }
}