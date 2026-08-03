
using artifact.desktop.Configurations;
using artifact.desktop.Views;
using Ioc;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;
using Utils.Configuration;

namespace artifact.desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    public App()
    {
        DispatcherUnhandledException += UnHandledExceptionHandler;
        InitServices();
    }

    private void InitServices()
    {
        SetupAppConfig();
        ConfigViews();
        AppContainer.Instance.Build();
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

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = AppContainer.Instance.Resolve<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
        
        base.OnStartup(e);
    }
    
    private void UnHandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Log4Logger.Logger.Error($"An unhandled exception occurred: {args.Exception?.Message}");
        args.Handled = true;
    }
}