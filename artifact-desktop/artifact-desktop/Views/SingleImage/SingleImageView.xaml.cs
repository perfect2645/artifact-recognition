using System.Windows.Controls;
using artifact.desktop.ViewModels.SingleImage;
using Utils.Ioc;

namespace artifact.desktop.Views.SingleImage;

[Register(ServiceType = typeof(SingleImageView), Lifetime = Lifetime.Transient)]
public partial class SingleImageView : UserControl
{
    public SingleImageView(SingleImageVm vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}