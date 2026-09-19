using CommunityToolkit.Mvvm.ComponentModel;
using Utils.Ioc;

namespace artifact.desktop.ViewModels.BulkImage
{
    [Register(ServiceType = typeof(FolderDialogViewModel), Lifetime = Lifetime.Singleton)]
    public partial class FolderDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFolderPath = string.Empty;

        [ObservableProperty]
        private string dialogTitle = string.Empty;

        [ObservableProperty]
        private string[]? gatheredFiles = [];

        [ObservableProperty]
        private int fileCount;

        partial void OnGatheredFilesChanged(string[]? value)
        {
        }
    }
}
