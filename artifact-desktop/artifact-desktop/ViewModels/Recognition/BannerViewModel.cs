
using artifact.desktop.Messaging.Local;
using artifact.desktop.Services;
using artifact.desktop.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.IO;
using Utils.Ioc;

namespace artifact.desktop.ViewModels.Recognition
{

    [Register(ServiceType = typeof(BannerViewModel), Lifetime = Lifetime.Singleton)]
    public partial class BannerViewModel(
        IMessenger messenger,
        IRecognitionFileService files,
        ILogger<BannerViewModel> logger) : ObservableRecipientVm(messenger), IRecipient<RecognitionBusyChangedMessage>
    {
        private readonly IRecognitionFileService _files = files;
        private readonly ILogger<BannerViewModel> _logger = logger;

        [ObservableProperty] public partial string FolderPath { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isLoadingFiles;
        private bool parentIsBusy;

        public string FolderName => string.IsNullOrEmpty(FolderPath) ? "No folder selected" : new DirectoryInfo(FolderPath).Name;
        public string FolderDescription => string.IsNullOrEmpty(FolderPath) ? "Choose a folder containing .dcm files" : FolderPath;
        public string FolderButtonLabel => string.IsNullOrEmpty(FolderPath) ? "Select folder" : "Change folder";
        public bool CanChangeFolder => !parentIsBusy;
        partial void OnFolderPathChanged(string value)
        {
            OnPropertyChanged(nameof(FolderName)); OnPropertyChanged(nameof(FolderDescription)); OnPropertyChanged(nameof(FolderButtonLabel));
        }
        partial void OnIsLoadingFilesChanged(bool value) =>
            Messenger.Send(new RecognitionFolderLoadingChangedMessage(value));

        [RelayCommand(CanExecute = nameof(CanChangeFolder))]
        private async Task LoadFolderAsync(string folderPath, CancellationToken cancellationToken)
        {
            if (!CanChangeFolder || string.IsNullOrWhiteSpace(folderPath)) return;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Messenger.Send(new RecognitionFolderErrorChangedMessage(null));
                IsLoadingFiles = true;
                var items = await _files.LoadFilesAsync(folderPath, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                FolderPath = folderPath;
                Messenger.Send(new RecognitionFolderLoadedMessage(items));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                ReportFolderSelectionError(ex);
            }
            finally
            {
                IsLoadingFiles = false;
            }
        }

        [RelayCommand]
        private void ReportFolderSelectionError(Exception exception)
        {
            _logger.LogError(exception, "Could not select or read the recognition folder.");
            Messenger.Send(new RecognitionFolderErrorChangedMessage(
                $"Could not read this folder. Check its location and access permissions. {exception.Message}"));
        }

        #region Messener

        public void Receive(RecognitionBusyChangedMessage message)
        {
            if (parentIsBusy == message.Value) return;
            parentIsBusy = message.Value;
            LoadFolderCommand.NotifyCanExecuteChanged();
        }

        #endregion Messenger
    }
}
