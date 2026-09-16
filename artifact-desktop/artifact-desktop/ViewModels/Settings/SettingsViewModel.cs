using Utils.Ioc;

namespace artifact.desktop.ViewModels
{
    [Register(ServiceType = typeof(SettingsViewModel), Lifetime = Lifetime.Singleton)]
    public partial class SettingsViewModel
    {
    }
}
