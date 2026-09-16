using artifact.desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Utils.Ioc;

namespace artifact.desktop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[Register(ServiceType = typeof(MainWindow), Lifetime = Lifetime.Singleton)]
public partial class MainWindow
{
    public MainWindow(MainWindowVm mainWindowVm, ILogger<MainWindow> logger)
    {
        InitializeComponent();

        DataContext = mainWindowVm;

        logger.LogInformation("MainWindow Initialized.");

        //CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
    }
}