using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace artifact.desktop.ViewModels.Base
{
    public partial class ObservableRecipientVm: ObservableRecipient
    {
        public ObservableRecipientVm(IMessenger messenger) : base(messenger)
        {
            IsActive = true;
        }
    }
}
