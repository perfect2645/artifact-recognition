using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace artifact.desktop.Controls
{
    /// <summary>
    /// FolderDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FolderDialogControl
    {

        #region Properties

        private static readonly Type ControlType = typeof(FolderDialogControl);
        private const string DefaultFilesSearchPattern = "*.*";

        public string SelectedFolderPath
        {
            get { return (string)GetValue(SelectedFolderPathProperty); }
            set { SetValue(SelectedFolderPathProperty, value); }
        }

        public static readonly DependencyProperty SelectedFolderPathProperty =
            DependencyProperty.Register(nameof(SelectedFolderPath), typeof(string), ControlType, 
                new PropertyMetadata(string.Empty, OnSelectedFolderPathChanged));

        private static void OnSelectedFolderPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FolderDialogControl)d;
            control.ClearGatheredFiles();
        }

        public string DialogTitle
        {
            get { return (string)GetValue(DialogTitleProperty); }
            set { SetValue(DialogTitleProperty, value); }
        }

        public static readonly DependencyProperty DialogTitleProperty =
            DependencyProperty.Register(nameof(DialogTitle), typeof(string), ControlType, new PropertyMetadata(string.Empty));

        public bool EnableGatherFiles
        {
            get { return (bool)GetValue(EnableGatherFilesProperty); }
            set { SetValue(EnableGatherFilesProperty, value); }
        }

        public static readonly DependencyProperty EnableGatherFilesProperty =
            DependencyProperty.Register(nameof(EnableGatherFiles), typeof(bool), ControlType);

        public string FilesSearchPattern
        {
            get { return (string)GetValue(FilesSearchPatternProperty); }
            set { SetValue(FilesSearchPatternProperty, value); }
        }

        public static readonly DependencyProperty FilesSearchPatternProperty =
            DependencyProperty.Register(nameof(FilesSearchPattern), typeof(string), ControlType, new PropertyMetadata(DefaultFilesSearchPattern));

        public string[]? GatheredFiles
        {
            get { return (string[]?)GetValue(GatheredFilesProperty); }
            set { SetValue(GatheredFilesProperty, value); }
        }

        public static readonly DependencyProperty GatheredFilesProperty =
            DependencyProperty.Register(nameof(GatheredFiles), typeof(string[]), ControlType);


        public int FileCount
        {
            get { return (int)GetValue(FileCountProperty); }
            set { SetValue(FileCountProperty, value); }
        }

        public static readonly DependencyProperty FileCountProperty =
            DependencyProperty.Register(nameof(FileCount), typeof(int), ControlType, new PropertyMetadata(0));


        #endregion Properties

        #region Constructor

        public FolderDialogControl()
        {
            InitializeComponent();
        }

        #endregion Constructor

        #region Methods

        private void ClearGatheredFiles()
        {
            GatheredFiles = null;
            FilesSearchPattern = DefaultFilesSearchPattern;
            FileCount = 0;
        }

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

            SelectedFolderPath = folderDialog.FolderName;

            ClearGatheredFiles();

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
                    searchPattern: FilesSearchPattern,
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

        #endregion Methods
    }
}
