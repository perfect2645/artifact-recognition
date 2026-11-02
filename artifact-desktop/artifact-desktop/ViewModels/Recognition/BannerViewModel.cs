using artifact.desktop.Messaging.Local;
using artifact.desktop.Services;
using artifact.desktop.ViewModels.Base;
using artifact.shared.data;
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
        private bool _isLoadingFiles;
        public bool IsLoadingFiles
        {
            get => _isLoadingFiles;
            private set
            {
                if (!SetProperty(ref _isLoadingFiles, value)) return;
                Messenger.Send(new RecognitionFolderLoadingChangedMessage(value));
            }
        }
        private bool _parentIsBusy;

        public string FolderName => string.IsNullOrEmpty(FolderPath) ? "No folder selected" : new DirectoryInfo(FolderPath).Name;
        public string FolderDescription => string.IsNullOrEmpty(FolderPath) ? "Choose a folder containing .dcm files" : FolderPath;
        public string FolderButtonLabel => string.IsNullOrEmpty(FolderPath) ? "Select folder" : "Change folder";
        public bool CanChangeFolder => !_parentIsBusy;
        partial void OnFolderPathChanged(string value)
        {
            OnPropertyChanged(nameof(FolderName)); OnPropertyChanged(nameof(FolderDescription)); OnPropertyChanged(nameof(FolderButtonLabel));
        }
        [RelayCommand(CanExecute = nameof(CanChangeFolder))]
        private async Task SelectFolderAsync(CancellationToken cancellationToken)
        {
            var path = _files.SelectFolder();
            if (path is null) return;

            Messenger.Send(new RecognitionFolderErrorChangedMessage(null));
            IsLoadingFiles = true;
            try
            {
                var items = await _files.LoadFilesAsync(path, cancellationToken);
                FolderPath = path;
                Messenger.Send(new RecognitionFolderLoadedMessage(items));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read the recognition folder.");
                Messenger.Send(new RecognitionFolderErrorChangedMessage(
                    $"Could not read this folder. Check its location and access permissions. {ex.Message}"));
            }
            finally
            {
                IsLoadingFiles = false;
            }
        }

        #region Messener

        public void Receive(RecognitionBusyChangedMessage message)
        {
            if (_parentIsBusy == message.Value) return;
            _parentIsBusy = message.Value;
            OnPropertyChanged(nameof(CanChangeFolder));
            SelectFolderCommand.NotifyCanExecuteChanged();
        }

        #endregion Messenger
    }
}
