using CommunityToolkit.Mvvm.ComponentModel;
using Utils.Ioc;

namespace artifact.desktop.ViewModels
{
    [Register(ServiceType = typeof(RecognitionViewModel), Lifetime = Lifetime.Singleton)]
    public partial class RecognitionViewModel : ObservableViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasInputFolder))]
        private string _inputFolderPath = string.Empty;

        [ObservableProperty]
        private string[]? _dicomFiles;

        public bool HasInputFolder => !string.IsNullOrEmpty(InputFolderPath);
    }
}
