using artifact.desktop.ViewModels.SingleImage;
using Utils.Ioc;

namespace artifact.desktop.ViewModels;

[Register(Lifetime = Lifetime.Singleton)]
public class MainWindowVm(SingleImageVm singleImageVm)
{
    public SingleImageVm SingleImageVm { get; } = singleImageVm;
}