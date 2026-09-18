using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Utils.Ioc;

namespace artifact.desktop.Services.Navigation;

public interface INavigationService
{
    object? CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateTo(Type viewModelType);
}

[Register(Lifetime = Lifetime.Singleton)]
public partial class NavigationService(IServiceProvider serviceProvider) : ObservableObject, INavigationService
{
    [ObservableProperty]
    private object? currentViewModel;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        var viewModel = serviceProvider.GetRequiredService(viewModelType);
        CurrentViewModel = viewModel;
    }
}
