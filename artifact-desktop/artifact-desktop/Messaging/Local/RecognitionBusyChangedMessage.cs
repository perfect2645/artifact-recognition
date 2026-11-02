using CommunityToolkit.Mvvm.Messaging.Messages;

namespace artifact.desktop.Messaging.Local;

public sealed class RecognitionBusyChangedMessage(bool isBusy) : ValueChangedMessage<bool>(isBusy);
