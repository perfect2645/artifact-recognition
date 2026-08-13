using artifact.desktop.Generic;
using artifact.desktop.Views.SingleImage;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Input;
using Utils.Ioc;

namespace artifact.desktop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[Register(ServiceType = typeof(MainWindow), Lifetime = Lifetime.Singleton)]
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(INavigationService navService, ILogger<MainWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
        navService.SetNavigationHost(MainContentHost);
        navService.NavigateTo<SingleImageView>();

        _logger.LogInformation("MainWindow Initialized.");

        //CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
    }
}