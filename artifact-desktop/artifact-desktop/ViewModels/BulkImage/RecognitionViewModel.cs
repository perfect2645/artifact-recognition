using artifact.desktop.Messaging.Local;
using artifact.desktop.Models;
using artifact.desktop.ViewModels.Base;
using artifact.desktop.ViewModels.BulkImage;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.IO;
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

        public void Receive(ValueChangedMessage<SelectedFiles>? message)
        {
            if (message?.Value.GatheredFiles is null)
            {
                Clear();
                return;
            }

            _ = BuildRecognitions(message.Value.GatheredFiles);
        }

        private IEnumerable<RecognitionItemUi> BuildRecognitions(string[] selectedFiles)
        {
            var recognitions = selectedFiles.Select(filePath => new RecognitionItemUi{
                InputPath = filePath,
                Name = Path.GetFileName(filePath),
                Id = Guid.NewGuid(),
            });
            return recognitions;
        }

        private void Clear()
        {
        }
    }
}
