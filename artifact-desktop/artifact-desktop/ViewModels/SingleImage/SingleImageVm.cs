using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Utils.Ioc;

namespace artifact.desktop.ViewModels.SingleImage;

[Register(ServiceType = typeof(SingleImageVm))]
public partial class SingleImageVm : ObservableObject
{
    [ObservableProperty] private string? _selectedImagePath;
    [ObservableProperty] private string? _recognitionStatus;
    [ObservableProperty] private string? _recognitionResult;
    [ObservableProperty] private string? _outputPath;

    public SingleImageVm()
    {
        ResetRecognitionStatus();
    }

    private void ResetRecognitionStatus()
    {
        RecognitionStatus = "Pending";
    }

    private bool CanExecuteStart()
    {
        return true;
    }
    
    [RelayCommand(CanExecute = nameof(CanExecuteStart))]
    private async Task OnStart()
    {
        RecognitionStatus = "Proceeding";

        await Task.Delay(2000);
        
        RecognitionStatus = "Completed";

        RecognitionResult = "Ok";
    }

}