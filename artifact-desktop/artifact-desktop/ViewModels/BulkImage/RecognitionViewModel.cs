using artifact.desktop.Messaging.Local;
using artifact.desktop.Models;
using artifact.desktop.Services;
using artifact.desktop.ViewModels.Base;
using artifact.desktop.ViewModels.BulkImage;
using artifact.shared.data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Messaging.Http.Exceptions;
using System.Collections.ObjectModel;
using System.IO;
using Utils.Ioc;

namespace artifact.desktop.ViewModels
{
    [Register(ServiceType = typeof(RecognitionViewModel), Lifetime = Lifetime.Singleton)]
    public partial class RecognitionViewModel(
        IMessenger messenger,
        FolderDialogViewModel folderDialogViewModel,
        IRecognitionService recognitionService) 
        : ObservableRecipientVm(messenger), IRecipient<ValueChangedMessage<SelectedFiles>>
    {
        public FolderDialogViewModel FolderDialogViewModel { get; } = folderDialogViewModel;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartRecognitionCommand))]
        public partial ObservableCollection<RecognitionItemUi> RecognitionItems { get; set; } = [];

        public void Receive(ValueChangedMessage<SelectedFiles>? message)
        {
            if (message?.Value.GatheredFiles is null)
            {
                Clear();
                return;
            }

            RecognitionItems = BuildRecognitions(message.Value.GatheredFiles);
        }

        private ObservableCollection<RecognitionItemUi> BuildRecognitions(string[] selectedFiles)
        {
            var recognitions = selectedFiles.Select(filePath => new RecognitionItemUi{
                InputPath = filePath,
                Name = Path.GetFileName(filePath),
                Id = Guid.NewGuid(),
            });
            return new ObservableCollection<RecognitionItemUi>(recognitions);
        }

        #region Recognition actions

        [RelayCommand(CanExecute = nameof(CanStartRecognition))]
        private async Task StartRecognitionAsync(CancellationToken cancellationToken)
        {
            var recognitionTaskId = Guid.NewGuid();
            var recognitionRequest = new RecognitionHttpRequest
            {
                TaskId = recognitionTaskId,
                FolderPath = FolderDialogViewModel.SelectedFolderPath,
                Artifacts = [.. RecognitionItems.Select(item => item.ToArtifact())]
            };

            try
            {
                await recognitionService.SubmitRecognitionAsync(recognitionRequest, cancellationToken);
            }
            catch (HttpException ex)
            {
                // show error message to user
            }
        }

        private bool CanStartRecognition()
        {
            return RecognitionItems.Any();
        }

        #endregion Recognition actions

        private void Clear()
        {
            RecognitionItems = [];
        }
    }
}
