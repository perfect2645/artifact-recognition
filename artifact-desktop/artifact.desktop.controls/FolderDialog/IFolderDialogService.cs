using System.Windows;

namespace artifact.desktop.controls;

/// <summary>Supplies a native or host-defined folder picker without requiring an application container.</summary>
public interface IFolderDialogService
{
    /// <returns>The confirmed folder path, or null when the user cancels.</returns>
    string? SelectFolder(string dialogTitle, string initialDirectory, Window? owner);
}
