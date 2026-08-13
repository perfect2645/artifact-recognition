using System.Windows;
using System.Windows.Controls;
using Utils.Ioc;

namespace artifact.desktop.Generic;

public interface INavigationService
{
    void SetNavigationHost(ContentControl host);
    void NavigateTo<TView>() where TView : FrameworkElement;
}

[Register(Lifetime = Lifetime.Singleton)]
public class NavigationService : INavigationService
{
    private ContentControl? _hostControl;
    
    public void SetNavigationHost(ContentControl host)
    {
        _hostControl = host;
    }
    
    public void NavigateTo<TView>() where TView : FrameworkElement
    {
        if (_hostControl is null)
            throw new InvalidOperationException("Please call SetNavigationHost to setup they host.");

        var view = App.GetService<TView>();
        _hostControl.Content = view;
    }
}