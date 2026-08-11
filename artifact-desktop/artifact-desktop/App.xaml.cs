using artifact.desktop.Messaging.Signalr;
using artifact.desktop.Views;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace artifact.desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    [STAThread]
    public static void Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();

        host.Start();

        var app = new App();
        app.InitializeComponent();

        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        app.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureHostConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                //// bind configuration sections
                //var appsettings = AppConfig.Configuration!.GetSection("AppSettings");
                //services.Configure<AppSettings>(appsettings);

                //var signalrSection = AppConfig.Configuration!.GetSection("SignalR");
                //services.Configure<SignalRSettings>(signalrSection);

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

    public App()
    {
        DispatcherUnhandledException += UnHandledExceptionHandler;
    }

    //public static IServiceProvider GetServiceProvider(IServiceCollection services)
    //{
        
    //}

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
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