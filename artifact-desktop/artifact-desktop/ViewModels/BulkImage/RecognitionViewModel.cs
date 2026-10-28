using artifact.desktop.Generic;
using artifact.desktop.Messaging.Signalr;
using artifact.desktop.Models;
using artifact.desktop.Services;
using artifact.desktop.ViewModels.Base;
using artifact.shared;
using artifact.shared.data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Utils.Ioc;

namespace artifact.desktop.ViewModels;

[Register(ServiceType = typeof(RecognitionViewModel), Lifetime = Lifetime.Singleton)]
public partial class RecognitionViewModel : ObservableRecipientVm, IRecipient<ValueChangedMessage<ArtifactMessage>>
{
    private readonly IRecognitionService _recognitionService;
    private readonly IRecognitionFileService _files;
    private readonly IDispatcherService _dispatcher;
    private readonly ISignalRClient<ArtifactMessage> _connection;
    private readonly ILogger _logger;
    private readonly Dictionary<Guid, RecognitionItemUi> _attempts = [];
    private CancellationTokenSource? _previewCancellation;
    public ObservableCollection<RecognitionItemUi> RecognitionItems { get; } = [];
    public ICollectionView FilesView { get; }
    public IReadOnlyList<RecognitionFilterOption> Filters { get; } =
    [
        new(RecognitionFilter.All, "All"), new(RecognitionFilter.Artifacts, "Artifacts"),
        new(RecognitionFilter.NoArtifacts, "No artifact"), new(RecognitionFilter.Failed, "Failed")
    ];

    [ObservableProperty] public partial RecognitionFilterOption SelectedFilter { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial RecognitionItemUi? SelectedItem { get; set; }
    [ObservableProperty] public partial string FolderPath { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsLoadingFiles { get; set; }
    [ObservableProperty] public partial bool IsSubmitting { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial BitmapSource? PreviewImage { get; set; }
    [ObservableProperty] public partial bool IsLoadingPreview { get; set; }
    [ObservableProperty] public partial string PreviewMessage { get; set; } = "Select a file to view its details.";
    [ObservableProperty] public partial HubConnectionState ConnectionState { get; set; }

    public RecognitionViewModel(IMessenger messenger, IRecognitionService recognitionService,
        IRecognitionFileService files, IDispatcherService dispatcher,
        ISignalRClient<ArtifactMessage> connection, ILogger<RecognitionViewModel> logger) : base(messenger)
    {
        _recognitionService = recognitionService;
        _files = files;
        _dispatcher = dispatcher;
        _connection = connection;
        _logger = logger;
        SelectedFilter = Filters[0];
        FilesView = CollectionViewSource.GetDefaultView(RecognitionItems);
        FilesView.Filter = MatchesFilter;
        ConnectionState = connection.CurrentState;
        connection.StateChanged += HandleConnectionStateChanged;
    }

    public bool HasFiles => RecognitionItems.Count > 0;
    public bool HasSelection => SelectedItem is not null;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasPreview => PreviewImage is not null;
    public bool HasNoPreview => PreviewImage is null;
    public bool IsBatchActive => _attempts.Values.Any(item => !item.IsTerminal);
    public bool IsBusy => IsLoadingFiles || IsSubmitting || IsBatchActive;
    public bool CanChangeFolder => !IsBusy;
    public int TotalCount => RecognitionItems.Count;
    public int CompletedCount => RecognitionItems.Count(item => item.IsTerminal);
    public int ArtifactCount => RecognitionItems.Count(item => item.HasArtifact);
    public int NoArtifactCount => RecognitionItems.Count(item => item.HasNoArtifact);
    public int FailedCount => RecognitionItems.Count(item => item.IsFailed);
    public int PendingCount => RecognitionItems.Count(item => item.RecognitionStatus == RecognitionStatus.Pending);
    public int ActiveCount => RecognitionItems.Count(item => !item.IsTerminal && item.RecognitionStatus != RecognitionStatus.Pending);
    public int CancelledCount => RecognitionItems.Count(item => item.RecognitionStatus == RecognitionStatus.Cancelled);
    public int UnknownCount => RecognitionItems.Count(item => item.IsCompleted && item.ArtifactStatus == ArtifactStatus.Unknown);
    public double ProgressPercent => TotalCount == 0 ? 0 : 100d * CompletedCount / TotalCount;
    public string ProgressLabel => $"{CompletedCount} / {TotalCount} processed";
    public string FileCountLabel => $"{TotalCount} {(TotalCount == 1 ? "file" : "files")}";
    public string FolderName => string.IsNullOrEmpty(FolderPath) ? "No folder selected" : new DirectoryInfo(FolderPath).Name;
    public string FolderDescription => string.IsNullOrEmpty(FolderPath) ? "Choose a folder containing .dcm files" : FolderPath;
    public string FolderButtonLabel => string.IsNullOrEmpty(FolderPath) ? "Select folder" : "Change folder";
    public string StartButtonLabel => IsSubmitting ? "Submitting…" : IsBatchActive ? "Recognizing…"
        : PendingCount > 0 ? $"Start recognition · {PendingCount}"
        : FailedCount > 0 ? $"Retry failed · {FailedCount}"
        : HasFiles && CompletedCount == TotalCount ? "Batch complete" : "Start recognition";
    public string BatchTitle => IsLoadingFiles ? "Reading folder…" : IsSubmitting ? "Submitting files…"
        : IsBatchActive ? "Recognition in progress" : !HasFiles ? "Select a folder to begin"
        : CompletedCount == TotalCount ? "Batch complete" : "Files ready";
    public string BatchDescription => IsLoadingFiles ? "Finding DICOM files and reading file information."
        : IsSubmitting ? "Connecting to live updates and submitting the selected files."
        : IsBatchActive ? "Results update automatically. Completed files are ready to review."
        : !HasFiles ? (string.IsNullOrEmpty(FolderPath) ? "Your files and recognition results will appear below." : "No DICOM files were found in this folder.")
        : CompletedCount == TotalCount ? $"{ArtifactCount + NoArtifactCount + UnknownCount} succeeded · {FailedCount} failed · {CancelledCount} cancelled"
        : "Review the file queue, then start recognition.";
    public bool IsConnected => ConnectionState == HubConnectionState.Connected;
    public string ConnectionLabel => ConnectionState switch
    {
        HubConnectionState.Connected => "Live updates connected",
        HubConnectionState.Connecting => "Connecting to live updates…",
        HubConnectionState.Reconnecting => "Reconnecting · updates may be delayed",
        _ => "Live updates disconnected"
    };
    public bool ShowEmptyQueue => FilesView.IsEmpty;
    public string EmptyQueueTitle => HasFiles ? "No matching files" : "Your file queue is empty";
    public string EmptyQueueDescription => HasFiles ? "Try another filter or search term." : "Select a folder to list its DICOM files.";
    public bool CanOpenOutput => SelectedItem?.HasOutput == true;

    [RelayCommand(CanExecute = nameof(CanChangeFolder))]
    private async Task SelectFolderAsync(CancellationToken cancellationToken)
    {
        try
        {
            var path = _files.SelectFolder();
            if (path is null) return;
            ErrorMessage = null;
            IsLoadingFiles = true;
            var items = await _files.LoadFilesAsync(path, cancellationToken);
            SelectedItem = null;
            _attempts.Clear();
            RecognitionItems.Clear();
            foreach (var item in items) RecognitionItems.Add(item);
            FolderPath = path;
            SearchText = string.Empty;
            SelectedFilter = Filters[0];
            SelectedItem = RecognitionItems.FirstOrDefault();
            RefreshState();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read the recognition folder.");
            ErrorMessage = $"Could not read this folder. Check its location and access permissions. {ex.Message}";
        }
        finally { IsLoadingFiles = false; }
    }

    private bool CanStartRecognition() => !IsBusy && (PendingCount > 0 || FailedCount > 0);

    [RelayCommand(CanExecute = nameof(CanStartRecognition))]
    private Task StartRecognitionAsync(CancellationToken cancellationToken)
    {
        var hasPending = PendingCount > 0;
        return SubmitAsync(RecognitionItems.Where(item => hasPending
            ? item.RecognitionStatus == RecognitionStatus.Pending : item.IsFailed).ToArray(), cancellationToken);
    }

    private bool CanRetrySelected() => !IsBusy && SelectedItem?.IsFailed == true;
    [RelayCommand(CanExecute = nameof(CanRetrySelected))]
    private Task RetrySelectedAsync(CancellationToken cancellationToken) =>
        SelectedItem is { } item ? SubmitAsync([item], cancellationToken) : Task.CompletedTask;

    private async Task SubmitAsync(IReadOnlyList<RecognitionItemUi> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0 || IsBusy) return;
        ErrorMessage = null;
        IsSubmitting = true;
        foreach (var item in items)
        {
            if (item.Id is { } oldId) _attempts.Remove(oldId);
            // ArtifactMessage has no task ID; fresh IDs isolate delayed messages from previous attempts.
            item.Id = Guid.NewGuid();
            item.OutputPath = null;
            item.ArtifactStatus = ArtifactStatus.Unknown;
            item.Comments = null;
            item.UpdateTime = null;
            item.RecognitionStatus = RecognitionStatus.Creating;
            _attempts.Add(item.Id.Value, item);
        }
        RefreshState();
        try
        {
            // Subscribe before submitting so a fast worker cannot finish before live updates are ready.
            await _connection.JoinGroupAsync(SharedConstants.SignalrClientGroup, cancellationToken);
            var response = await _recognitionService.SubmitRecognitionAsync(new RecognitionHttpRequest
            {
                RelativePath = Constants.RecognitionRequestPath,
                TaskId = Guid.NewGuid(),
                FolderPath = FolderPath,
                Artifacts = items.Select(item => item.ToArtifact()).ToArray()
            }, cancellationToken);
            if (!response.IsSuccess) throw new InvalidOperationException(response.Message ?? "The service rejected this batch.");
            foreach (var item in items.Where(item => item.RecognitionStatus == RecognitionStatus.Creating))
                item.RecognitionStatus = RecognitionStatus.Created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not submit the recognition batch.");
            ErrorMessage = $"Submission could not be confirmed. {ex.Message}";
            // Preserve results that arrived while HTTP submission was in flight.
            foreach (var item in items.Where(item => !item.IsTerminal))
            {
                item.Comments = ErrorMessage;
                item.UpdateTime = DateTime.Now;
                item.RecognitionStatus = RecognitionStatus.Failed;
            }
        }
        finally { IsSubmitting = false; RefreshState(); }
    }

    public void Receive(ValueChangedMessage<ArtifactMessage> message) => _dispatcher.InvokeOnUI(() =>
    {
        var artifact = message.Value.Message;
        if (!_attempts.TryGetValue(artifact.ArtifactId, out var item) || item.IsTerminal) return;
        if (artifact.UpdateTime is { } incoming && item.UpdateTime is { } previous && incoming < previous) return;
        // Do not let a delayed queue update regress a file already being processed.
        if ((int)artifact.RecognitionStatus < (int)item.RecognitionStatus) return;
        item.ArtifactStatus = artifact.ArtifactStatus;
        item.Comments = artifact.Comments;
        item.OutputPath = artifact.OutputPath;
        item.UpdateTime = artifact.UpdateTime;
        item.RecognitionStatus = artifact.RecognitionStatus;
        RefreshState();
    });

    private bool MatchesFilter(object value) => value is RecognitionItemUi item
        && (string.IsNullOrWhiteSpace(SearchText)
            || item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.RelativePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        && ((SelectedFilter?.Filter ?? RecognitionFilter.All) switch
        {
            RecognitionFilter.Artifacts => item.HasArtifact,
            RecognitionFilter.NoArtifacts => item.HasNoArtifact,
            RecognitionFilter.Failed => item.IsFailed,
            _ => true
        });

    private void RefreshFiles()
    {
        var selected = SelectedItem;
        FilesView.Refresh();
        if (SelectedItem is null && selected is not null && FilesView.Contains(selected)) SelectedItem = selected;
        NotifyEmptyQueue();
    }

    private void NotifyEmptyQueue()
    {
        OnPropertyChanged(nameof(ShowEmptyQueue));
        OnPropertyChanged(nameof(EmptyQueueTitle));
        OnPropertyChanged(nameof(EmptyQueueDescription));
    }

    private void RefreshState()
    {
        foreach (var filter in Filters) filter.Count = filter.Filter switch
        {
            RecognitionFilter.Artifacts => ArtifactCount,
            RecognitionFilter.NoArtifacts => NoArtifactCount,
            RecognitionFilter.Failed => FailedCount,
            _ => TotalCount
        };
        foreach (var name in new[] { nameof(HasFiles), nameof(IsBatchActive), nameof(IsBusy), nameof(CanChangeFolder),
        nameof(TotalCount), nameof(CompletedCount), nameof(ArtifactCount), nameof(NoArtifactCount), nameof(FailedCount),
        nameof(PendingCount), nameof(ActiveCount), nameof(UnknownCount), nameof(CancelledCount), nameof(ProgressPercent),
        nameof(ProgressLabel), nameof(FileCountLabel), nameof(StartButtonLabel), nameof(BatchTitle), nameof(BatchDescription) })
            OnPropertyChanged(name);
        StartRecognitionCommand.NotifyCanExecuteChanged();
        RetrySelectedCommand.NotifyCanExecuteChanged();
        SelectFolderCommand.NotifyCanExecuteChanged();
        OpenOutputCommand.NotifyCanExecuteChanged();
        ShowOutputInFolderCommand.NotifyCanExecuteChanged();
        // Bound row values already update in place. Refreshing the unfiltered view would reset scroll/selection.
        if (SelectedFilter.Filter != RecognitionFilter.All) RefreshFiles();
        else NotifyEmptyQueue();
    }

    partial void OnSearchTextChanged(string value) { if (FilesView is not null) RefreshFiles(); }
    partial void OnSelectedFilterChanged(RecognitionFilterOption value) { if (FilesView is not null) RefreshFiles(); }
    partial void OnIsSubmittingChanged(bool value) => RefreshState();
    partial void OnIsLoadingFilesChanged(bool value) => RefreshState();
    partial void OnFolderPathChanged(string value)
    {
        OnPropertyChanged(nameof(FolderName)); OnPropertyChanged(nameof(FolderDescription)); OnPropertyChanged(nameof(FolderButtonLabel));
    }
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnPreviewImageChanged(BitmapSource? value)
    {
        OnPropertyChanged(nameof(HasPreview)); OnPropertyChanged(nameof(HasNoPreview));
    }
    partial void OnConnectionStateChanged(HubConnectionState value)
    {
        OnPropertyChanged(nameof(ConnectionLabel)); OnPropertyChanged(nameof(IsConnected));
    }
    private void HandleConnectionStateChanged(HubConnectionState state) => _dispatcher.InvokeOnUI(() => ConnectionState = state);

    partial void OnSelectedItemChanged(RecognitionItemUi? oldValue, RecognitionItemUi? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= OnSelectedFileChanged;
        if (newValue is not null) newValue.PropertyChanged += OnSelectedFileChanged;
        OnPropertyChanged(nameof(HasSelection));
        RefreshSelectedActions();
        _ = RefreshPreviewCommand.ExecuteAsync(null);
    }
    private void OnSelectedFileChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RecognitionItemUi.RecognitionStatus) or nameof(RecognitionItemUi.OutputPath))
        {
            RefreshSelectedActions();
            _ = RefreshPreviewCommand.ExecuteAsync(null);
        }
    }
    private void RefreshSelectedActions()
    {
        OnPropertyChanged(nameof(CanOpenOutput));
        RetrySelectedCommand.NotifyCanExecuteChanged(); OpenOutputCommand.NotifyCanExecuteChanged();
        ShowOutputInFolderCommand.NotifyCanExecuteChanged(); OpenSourceCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task RefreshPreviewAsync()
    {
        _previewCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _previewCancellation = cancellation;
        PreviewImage = null;
        IsLoadingPreview = false;
        var item = SelectedItem;
        if (item?.HasOutput != true)
        {
            PreviewMessage = item is null ? "Select a file to view its details."
                : item.IsCompleted ? "The service did not return an output image path for this file."
                : item.IsFailed ? "No output image was produced. Review the error details below."
                : "The output image will be available after recognition. Open the source in your DICOM viewer.";
            cancellation.Dispose();
            if (_previewCancellation == cancellation) _previewCancellation = null;
            return;
        }
        IsLoadingPreview = true;
        PreviewMessage = "Loading output image…";
        try
        {
            var image = await _files.LoadPreviewAsync(item.OutputPath!, cancellation.Token);
            if (cancellation.IsCancellationRequested || item != SelectedItem) return;
            PreviewImage = image;
            PreviewMessage = image is null ? "No preview is available for this output file." : string.Empty;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!cancellation.IsCancellationRequested && item == SelectedItem)
            {
                PreviewMessage = "Preview unavailable. Check that the output path is accessible on this computer, then reload.";
                _logger.LogWarning(ex, "Could not load the output preview: {Path}", item.OutputPath);
            }
        }
        finally
        {
            if (_previewCancellation == cancellation) { IsLoadingPreview = false; _previewCancellation = null; }
            cancellation.Dispose();
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenOutput))]
    private void OpenOutput() => OpenFileAction(() => _files.OpenFile(SelectedItem!.OutputPath!));
    [RelayCommand(CanExecute = nameof(CanOpenOutput))]
    private void ShowOutputInFolder() => OpenFileAction(() => _files.ShowInFolder(SelectedItem!.OutputPath!));
    private bool CanOpenSource() => SelectedItem is not null;
    [RelayCommand(CanExecute = nameof(CanOpenSource))]
    private void OpenSource() => OpenFileAction(() => _files.OpenFile(SelectedItem!.InputPath));
    private void OpenFileAction(Action action)
    {
        try { action(); }
        catch (Exception ex) { ErrorMessage = $"Could not open this file. {ex.Message}"; }
    }
    [RelayCommand] private void DismissError() => ErrorMessage = null;

}