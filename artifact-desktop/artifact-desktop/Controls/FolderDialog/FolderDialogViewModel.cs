using CommunityToolkit.Mvvm.ComponentModel;
using Utils.Ioc;

namespace artifact.desktop.Controls
{
    [Register(ServiceType = typeof(FolderDialogViewModel), Lifetime = Lifetime.Singleton)]
    public partial class FolderDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFolderPath = string.Empty;

        [ObservableProperty]
        private string dialogTitle = string.Empty;

        [ObservableProperty]
        private bool enableGatherFiles = false;

        [ObservableProperty]
        private string filesSearchPattern = "*.*";

        [ObservableProperty]
        private string[]? gatheredFiles = [];

        [ObservableProperty]
        private int fileCount;
    }
}
