using artifact.desktop.Messaging.Local;
using artifact.desktop.ViewModels.Base;
using artifact.desktop.ViewModels.BulkImage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Utils.Ioc;

namespace artifact.desktop.ViewModels
{
    [Register(ServiceType = typeof(RecognitionViewModel), Lifetime = Lifetime.Singleton)]
    public partial class RecognitionViewModel(
        IMessenger messenger,
        FolderDialogViewModel folderDialogViewModel) 
        : ObservableRecipientVm(messenger), IRecipient<ValueChangedMessage<SelectedFiles>>
    {
        public FolderDialogViewModel FolderDialogViewModel { get; } = folderDialogViewModel;

        public void Receive(ValueChangedMessage<SelectedFiles> message)
        {
            if (message is null || message.Value is null)
            {
                Clear();
                return;
            }
        }

        private void Clear()
        {
        }
    }
}
