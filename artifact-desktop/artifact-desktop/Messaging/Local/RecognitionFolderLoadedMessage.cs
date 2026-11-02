using artifact.desktop.Models;

namespace artifact.desktop.Messaging.Local;

public sealed record RecognitionFolderLoadedMessage(IReadOnlyList<RecognitionItemUi> Items);
