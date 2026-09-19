using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Data;

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
            internal set => throw new InvalidOperationException("SelectedFolderPath is read-only and cannot be set externally.");
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
            get => (string[]?)GetValue(GatheredFilesProperty);
            internal set => throw new InvalidOperationException("GatheredFiles is read-only and cannot be set externally.");
        }

        public static readonly DependencyProperty GatheredFilesProperty =
            DependencyProperty.Register(nameof(GatheredFiles), typeof(string[]),
                ControlType, new PropertyMetadata(null));

        public int FileCount
        {
            get => (int)GetValue(FileCountProperty);
        }

        private static readonly DependencyPropertyKey FileCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(FileCount), typeof(int), ControlType, new PropertyMetadata(0));

        public static readonly DependencyProperty FileCountProperty =
            FileCountPropertyKey.DependencyProperty;


        #endregion Properties

        #region Constructor

        public FolderDialogControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        #endregion Constructor

        #region Methods

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ValidateOutputBinding(SelectedFolderPathProperty);
            ValidateOutputBinding(GatheredFilesProperty);
        }

        private void ValidateOutputBinding(DependencyProperty property)
        {
            var binding = BindingOperations.GetBinding(this, property);
            if (binding is null)
            {
                if (ReadLocalValue(property) == DependencyProperty.UnsetValue)
                {
                    return;
                }
            }
            else if (binding.Mode == BindingMode.OneWayToSource )
            {
                return;
            }

            throw new InvalidOperationException($"The property '{property.Name}' only supports a OneWayToSource binding.");
        }

        private void ClearGatheredFiles()
        {
            SetCurrentValue(GatheredFilesProperty, null);
            SetValue(FileCountPropertyKey, 0);
        }

        [RelayCommand]
        private async Task OnOpenFolderDialog()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = DialogTitle,
            };

            if (folderDialog.ShowDialog() is not true)
            {
                return;
            }

            ClearGatheredFiles();

            SetCurrentValue(SelectedFolderPathProperty, folderDialog.FolderName);

            if (EnableGatherFiles)
            {
                var (files, count) = await GatherFiles();
                SetCurrentValue(GatheredFilesProperty, files);
                SetValue(FileCountPropertyKey, count);

                // Ensure the UI has time to update before the next operation, if needed.
                // Adjust the delay as necessary based on your application's needs.
                await Task.Delay(1000); 
            }
        }

        private async Task<(string[], int)> GatherFiles()
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
