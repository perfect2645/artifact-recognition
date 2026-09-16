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

        [ObservableProperty]
        private string[]? gatheredFiles = [];

        [ObservableProperty]
        private int fileCount;

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
                var (files, count) = GatherFiles();
                GatheredFiles = files;
                FileCount = count;
            }
        }

        private (string[], int) GatherFiles()
        {
            try
            {
                var files = Directory.EnumerateFiles(
                    path: SelectedFolderPath,
                    searchPattern: FilesSearchPattern ?? "*.*",
                    searchOption: SearchOption.AllDirectories
                ).ToArray();

                return (files, files.Length);
            }
            catch (DirectoryNotFoundException)
            {
                return (Array.Empty<string>(), 0);
            }
            catch (UnauthorizedAccessException)
            {
                return (Array.Empty<string>(), 0);
            }
            catch (IOException)
            {
                return (Array.Empty<string>(), 0);
            }
        }
    }
}
