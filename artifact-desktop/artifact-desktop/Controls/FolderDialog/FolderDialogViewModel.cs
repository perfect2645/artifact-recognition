using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;

namespace artifact.desktop.Controls
{
    public partial class FolderDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFolderPath = string.Empty;

        [ObservableProperty]
        private string dialogTitle = string.Empty;

        [ObservableProperty]
        private bool enableGatherFiles = false;

        [ObservableProperty]
        private string? filesSearchPattern;

        [RelayCommand]
        private void OnOpenFolderDialog()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = DialogTitle,
            };

            if (folderDialog.ShowDialog() is not true)
            {
                return;
            }

            SelectedFolderPath = folderDialog.SafeFolderName;

            if (EnableGatherFiles)
            {
                GatherFiles();
            }
        }

        private void GatherFiles()
        {
            try
            {
                IEnumerable<string> dcmFiles = Directory.EnumerateFiles(
                    path: SelectedFolderPath,
                    searchPattern: "*.dcm",
                    searchOption: SearchOption.AllDirectories
                );

                foreach (string dcmFilePath in dcmFiles)
                {

                }
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
