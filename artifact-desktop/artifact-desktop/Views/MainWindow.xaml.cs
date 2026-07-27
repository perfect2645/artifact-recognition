using System.Windows;
using artifact_automation_desktop.Generic;
using artifact_automation_desktop.Views.SingleImage;

namespace artifact_automation_desktop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(INavigationService navService)
    {
        InitializeComponent();
        navService.SetNavigationHost(MainContentHost);
        navService.NavigateTo<SingleImageView>();
    }
}