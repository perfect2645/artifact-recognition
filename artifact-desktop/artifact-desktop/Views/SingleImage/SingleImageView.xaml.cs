using System.Windows.Controls;
using artifact_automation_desktop.ViewModels.SingleImage;
using Utils.Ioc;

namespace artifact_automation_desktop.Views.SingleImage;

[Register(ServiceType = typeof(SingleImageView), Lifetime = Lifetime.Transient)]
public partial class SingleImageView : UserControl
{
    public SingleImageView(SingleImageVm vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}