using artifact.desktop.Controls;
using Utils.Ioc;

namespace artifact.desktop.ViewModels
{
    [Register(ServiceType = typeof(RecognitionViewModel), Lifetime = Lifetime.Singleton)]
    public partial class RecognitionViewModel(
        FolderDialogViewModel folderDialogViewModel) : ObservableViewModel
    {
        public FolderDialogViewModel FolderDialogViewModel { get; } = folderDialogViewModel;
    }
}
