using artifact.desktop.Services;
using artifact.desktop.ViewModels.Base;
using artifact.shared.data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using System.IO;
using Utils.Ioc;
using Utils.Tasking;

namespace artifact.desktop.ViewModels;

[Register(ServiceType = typeof(ObservableRecipientVm))]
public partial class SingleImageVm : ObservableRecipientVm, IRecipient<ValueChangedMessage<ArtifactMessage>>
{
    [ObservableProperty] private string? _selectedImagePath;
    [ObservableProperty] private RecognitionStatus _recognitionStatus;
    [ObservableProperty] private string? _recognitionResult;
    [ObservableProperty] private string? _outputPath;

    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger _logger;
    private readonly IRecognitionService _recognitionService;


    public SingleImageVm(IMessenger messenger,
        IDispatcherService dispatcherService,
        IRecognitionService recognitionService,
        ILogger<SingleImageVm> logger) : base(messenger)
    {
        _dispatcherService = dispatcherService;
        _recognitionService = recognitionService;
        _logger = logger;
        ResetRecognitionStatus();
        IsActive = true;
    }

    private void ResetRecognitionStatus()
    {
        RecognitionStatus = RecognitionStatus.Pending;
    }

    #region Recognition process

    private bool CanExecuteStart()
    {
        return true;
    }
    
    [RelayCommand(CanExecute = nameof(CanExecuteStart), IncludeCancelCommand = true)]
    private async Task OnStart(CancellationToken cancellationToken)
    {
        RecognitionStatus = RecognitionStatus.Pending;

        await ProceedRecognitionAsync(cancellationToken);
    }

    private async Task ProceedRecognitionAsync(CancellationToken cancellationToken)
    {
        var folderPath = Path.GetDirectoryName(SelectedImagePath);
        if (folderPath is null)
        {
            _logger.LogError("Folder path is null for selected image path: {SelectedImagePath}", SelectedImagePath);
            return;
        }

        RecognitionStatus = RecognitionStatus.Creating;
        var result = await _recognitionService.CreateArtifactsAsync(folderPath, cancellationToken);
    }

    #endregion Recognition process

    public void Receive(ValueChangedMessage<ArtifactMessage> message)
    {
        _dispatcherService.InvokeOnUIAsync(() =>
        {
            if (message.Value is not { } artifactMessage) return;
            OutputPath = artifactMessage.Message.OutputPath;
            RecognitionStatus = artifactMessage.Message.RecognitionStatus;
        }).SafeFireAndForget(onError: ex => _logger.LogError(ex, "Error Receive ArtifactMessage: {message} ", message));
    }
}