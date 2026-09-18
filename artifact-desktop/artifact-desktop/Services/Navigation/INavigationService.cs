using artifact.desktop.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Utils.Ioc;

namespace artifact.desktop.Services.Navigation;

public interface INavigationService
{
    object? CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateTo<TViewModel>(TViewModel viewModel) where TViewModel : class;
}

[Register(Lifetime = Lifetime.Singleton)]
public partial class NavigationService(IServiceProvider serviceProvider) : ObservableObject, INavigationService
{
    [ObservableProperty]
    private object? currentViewModel;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
    }

    public void NavigateTo<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        NavigateTo<TViewModel>();
    }
}
