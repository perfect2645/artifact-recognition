using artifact.desktop.ViewModels.SingleImage;
using CommunityToolkit.Mvvm.ComponentModel;
using Utils.Ioc;

namespace artifact.desktop.ViewModels;

[Register(ServiceType = typeof(MainWindowVm), Lifetime = Lifetime.Singleton)]
public class MainWindowVm(SingleImageVm singleImageVm) : ObservableObject
{
    public SingleImageVm SingleImageVm { get; } = singleImageVm;
}