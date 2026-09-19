using artifact.desktop.Messaging.Local;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Utils.Ioc;

namespace artifact.desktop.ViewModels.BulkImage
{
    [Register(ServiceType = typeof(FolderDialogViewModel), Lifetime = Lifetime.Singleton)]
    public partial class FolderDialogViewModel(IMessenger messenger) : ObservableObject
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
            messenger.Send(new ValueChangedMessage<SelectedFiles>(new (value)));
        }
    }
}
