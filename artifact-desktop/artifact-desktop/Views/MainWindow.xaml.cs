using System.Windows;
using artifact.desktop.Generic;
using artifact.desktop.Views.SingleImage;
using Utils.Ioc;

namespace artifact.desktop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[Register(ServiceType = typeof(MainWindow), Lifetime = Lifetime.Singleton)]
public partial class MainWindow : Window
{
    public MainWindow(INavigationService navService)
    {
        InitializeComponent();
        navService.SetNavigationHost(MainContentHost);
        navService.NavigateTo<SingleImageView>();
    }
}