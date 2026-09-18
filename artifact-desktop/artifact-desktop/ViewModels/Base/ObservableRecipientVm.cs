using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace artifact.desktop.ViewModels.Base
{
    public partial class ObservableRecipientVm(IMessenger messenger) : ObservableRecipient(messenger)
    {
    }
}
