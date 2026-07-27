using artifact_automation_desktop.ViewModels.SingleImage;
using Utils.Ioc;

namespace artifact_automation_desktop.ViewModels;

[Register(Lifetime = Lifetime.Singleton)]
public class MainWindowVm(SingleImageVm singleImageVm)
{
    public SingleImageVm SingleImageVm { get; } = singleImageVm;
}