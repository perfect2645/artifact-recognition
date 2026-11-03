using Microsoft.Win32;
using System.Windows;

namespace artifact.desktop.controls;

internal sealed class NativeFolderDialogService : IFolderDialogService
{
    public string? SelectFolder(string dialogTitle, string initialDirectory, Window? owner)
    {
        var dialog = new OpenFolderDialog { Title = dialogTitle, InitialDirectory = initialDirectory };
        var confirmed = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return confirmed == true ? dialog.FolderName : null;
    }
}
