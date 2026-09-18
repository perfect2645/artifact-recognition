using Utils.Ioc;

namespace artifact.desktop.ViewModels
{
    [Register(ServiceType = typeof(ObservableViewModel), Lifetime = Lifetime.Singleton)]
    public partial class RecognitionViewModel : ObservableViewModel
    {
    }
}
