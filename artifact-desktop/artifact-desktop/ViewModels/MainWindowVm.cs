using artifact.desktop.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using Utils.Ioc;

namespace artifact.desktop.ViewModels;

[Register(ServiceType = typeof(MainWindowVm), Lifetime = Lifetime.Singleton)]
public partial class MainWindowVm : ObservableObject
{
    public INavigationService NavigationService { get; }

    public IReadOnlyList<NavigationItem> NavigationItems { get; } =
        [
            new ("Recognition", PackIconKind.Image, typeof(RecognitionViewModel)),
            new ("Single Image", PackIconKind.Camera, typeof(SingleImageVm)),
            new ("Settings", PackIconKind.Settings, typeof(SettingsViewModel))
        ];

    [ObservableProperty]
    public partial NavigationItem? SelectedNavigationItem { get; set; }

    public MainWindowVm(INavigationService navigationService)
    {
        NavigationService = navigationService;
        SelectedNavigationItem = NavigationItems.First();
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is null)
        {
            return;
        }

        NavigationService.NavigateTo(value.ViewModelType);
    }
}