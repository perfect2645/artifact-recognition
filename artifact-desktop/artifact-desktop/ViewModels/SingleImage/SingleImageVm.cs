using artifact.desktop.Services;
using artifact.shared.data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Utils.Ioc;
using Utils.Tasking;

namespace artifact.desktop.ViewModels.SingleImage;

[Register(ServiceType = typeof(SingleImageVm))]
public partial class SingleImageVm : ObservableRecipient, IRecipient<ValueChangedMessage<ArtifactMessage>>
{
    [ObservableProperty] private string? _selectedImagePath;
    [ObservableProperty] private RecognitionStatus _recognitionStatus;
    [ObservableProperty] private string? _recognitionResult;
    [ObservableProperty] private string? _outputPath;

    private readonly IDispatcherService _dispatcherService;
    private readonly ILogger _logger;


    public SingleImageVm(IMessenger messenger,
        IDispatcherService dispatcherService,
        ILogger<SingleImageVm> logger) : base(messenger)
    {
        _dispatcherService = dispatcherService;
        _logger = logger;
        ResetRecognitionStatus();
        IsActive = true;
    }

    private void ResetRecognitionStatus()
    {
        RecognitionStatus = RecognitionStatus.Pending;
    }

    private bool CanExecuteStart()
    {
        return true;
    }
    
    [RelayCommand(CanExecute = nameof(CanExecuteStart))]
    private async Task OnStart()
    {
        RecognitionStatus = RecognitionStatus.Proceeding;

        await Task.Delay(2000);
        
        RecognitionStatus = RecognitionStatus.Completed;

        RecognitionResult = "Ok";
    }

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