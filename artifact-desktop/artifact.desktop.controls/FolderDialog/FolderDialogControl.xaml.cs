using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace artifact.desktop.controls;

/// <summary>
/// Selects a folder and optionally enumerates its files without depending on an application ViewModel.
/// </summary>
public partial class FolderDialogControl : UserControl
{
    private static readonly Type ControlType = typeof(FolderDialogControl);

    public static readonly DependencyProperty SelectedFolderPathProperty = DependencyProperty.Register(
        nameof(SelectedFolderPath), typeof(string), ControlType,
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSelectedFolderPathChanged));

    /// <summary>The last confirmed folder. This property supports two-way binding.</summary>
    public string SelectedFolderPath
    {
        get => (string)GetValue(SelectedFolderPathProperty);
        set => SetValue(SelectedFolderPathProperty, value);
    }

    public static readonly DependencyProperty InitialDirectoryProperty = DependencyProperty.Register(
        nameof(InitialDirectory), typeof(string), ControlType, new PropertyMetadata(string.Empty));

    public string InitialDirectory
    {
        get => (string)GetValue(InitialDirectoryProperty);
        set => SetValue(InitialDirectoryProperty, value);
    }

    public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.Register(
        nameof(DialogTitle), typeof(string), ControlType, new PropertyMetadata("Select a folder"));

    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    public static readonly DependencyProperty DialogServiceProperty = DependencyProperty.Register(
        nameof(DialogService), typeof(IFolderDialogService), ControlType,
        new PropertyMetadata(new NativeFolderDialogService()), value => value is IFolderDialogService);

    /// <summary>Optional host customization. The default service opens the native dialog.</summary>
    public IFolderDialogService DialogService
    {
        get => (IFolderDialogService)GetValue(DialogServiceProperty);
        set => SetValue(DialogServiceProperty, value);
    }

    public static readonly DependencyProperty EnableGatherFilesProperty = DependencyProperty.Register(
        nameof(EnableGatherFiles), typeof(bool), ControlType, new PropertyMetadata(false));

    public bool EnableGatherFiles
    {
        get => (bool)GetValue(EnableGatherFilesProperty);
        set => SetValue(EnableGatherFilesProperty, value);
    }

    public static readonly DependencyProperty FilesSearchPatternProperty = DependencyProperty.Register(
        nameof(FilesSearchPattern), typeof(string), ControlType, new PropertyMetadata("*.*"));

    public string FilesSearchPattern
    {
        get => (string)GetValue(FilesSearchPatternProperty);
        set => SetValue(FilesSearchPatternProperty, value);
    }

    public static readonly DependencyProperty SearchOptionProperty = DependencyProperty.Register(
        nameof(SearchOption), typeof(SearchOption), ControlType,
        new PropertyMetadata(System.IO.SearchOption.AllDirectories),
        value => value is System.IO.SearchOption.TopDirectoryOnly or System.IO.SearchOption.AllDirectories);

    public SearchOption SearchOption
    {
        get => (SearchOption)GetValue(SearchOptionProperty);
        set => SetValue(SearchOptionProperty, value);
    }

    public static readonly DependencyProperty GatheredFilesProperty = DependencyProperty.Register(
        nameof(GatheredFiles), typeof(string[]), ControlType,
        new PropertyMetadata(null, OnGatheredFilesChanged));

    /// <summary>
    /// Enumerated paths, or null when enumeration is disabled. Kept writable for OneWayToSource bindings.
    /// </summary>
    public string[]? GatheredFiles
    {
        get => (string[]?)GetValue(GatheredFilesProperty);
        set => SetValue(GatheredFilesProperty, value);
    }

    private static readonly DependencyPropertyKey FileCountPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FileCount), typeof(int), ControlType, new PropertyMetadata(0));

    public static readonly DependencyProperty FileCountProperty = FileCountPropertyKey.DependencyProperty;
    public int FileCount => (int)GetValue(FileCountProperty);

    private static readonly DependencyPropertyKey LastErrorPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(LastError), typeof(Exception), ControlType, new PropertyMetadata(null));

    public static readonly DependencyProperty LastErrorProperty = LastErrorPropertyKey.DependencyProperty;

    /// <summary>The latest failure, available even when no failure command is supplied.</summary>
    public Exception? LastError => (Exception?)GetValue(LastErrorProperty);

    public static readonly DependencyProperty FolderSelectedCommandProperty = DependencyProperty.Register(
        nameof(FolderSelectedCommand), typeof(ICommand), ControlType);

    /// <summary>Receives the confirmed path after optional enumeration, including repeated selections.</summary>
    public ICommand? FolderSelectedCommand
    {
        get => (ICommand?)GetValue(FolderSelectedCommandProperty);
        set => SetValue(FolderSelectedCommandProperty, value);
    }

    public static readonly DependencyProperty SelectionFailedCommandProperty = DependencyProperty.Register(
        nameof(SelectionFailedCommand), typeof(ICommand), ControlType);

    /// <summary>Receives the original exception if selection, enumeration, or the selected command fails.</summary>
    public ICommand? SelectionFailedCommand
    {
        get => (ICommand?)GetValue(SelectionFailedCommandProperty);
        set => SetValue(SelectionFailedCommandProperty, value);
    }

    public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
        nameof(ButtonContent), typeof(object), ControlType, new PropertyMetadata("Select folder"));

    public object? ButtonContent
    {
        get => GetValue(ButtonContentProperty);
        set => SetValue(ButtonContentProperty, value);
    }

    public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register(
        nameof(ButtonStyle), typeof(Style), ControlType);

    public Style? ButtonStyle
    {
        get => (Style?)GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    public static readonly DependencyProperty ButtonPaddingProperty = DependencyProperty.Register(
        nameof(ButtonPadding), typeof(Thickness), ControlType,
        new PropertyMetadata(new Thickness(12, 4, 12, 4)));

    public Thickness ButtonPadding
    {
        get => (Thickness)GetValue(ButtonPaddingProperty);
        set => SetValue(ButtonPaddingProperty, value);
    }

    public static readonly DependencyProperty ShowSelectedFolderPathProperty = DependencyProperty.Register(
        nameof(ShowSelectedFolderPath), typeof(bool), ControlType, new PropertyMetadata(true));

    public bool ShowSelectedFolderPath
    {
        get => (bool)GetValue(ShowSelectedFolderPathProperty);
        set => SetValue(ShowSelectedFolderPathProperty, value);
    }

    public FolderDialogControl()
    {
        InitializeComponent();
        IsEnabledChanged += (_, _) => OpenFolderDialogCommand.NotifyCanExecuteChanged();
    }

    private static void OnSelectedFolderPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        // Files from the previous folder are no longer applicable, including when the host changes the path.
        sender.SetCurrentValue(GatheredFilesProperty, null);
    }

    private static void OnGatheredFilesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        sender.SetValue(FileCountPropertyKey, (args.NewValue as string[])?.Length ?? 0);

    private bool CanSelectFolder() => IsEnabled;

    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private async Task OnOpenFolderDialog(CancellationToken cancellationToken)
    {
        if (!IsEnabled) return;

        try
        {
            var initialDirectory = string.IsNullOrWhiteSpace(InitialDirectory) ? SelectedFolderPath : InitialDirectory;
            var folderPath = DialogService.SelectFolder(DialogTitle, initialDirectory, Window.GetWindow(this));
            if (folderPath is null) return;
            cancellationToken.ThrowIfCancellationRequested();

            var selectedCommand = FolderSelectedCommand;
            // The predicate needs the actual selection; checking an empty/old path before the dialog is incorrect.
            if (!IsEnabled || selectedCommand?.CanExecute(folderPath) == false) return;

            SetValue(LastErrorPropertyKey, null);
            var gatheredFiles = EnableGatherFiles
                ? await GatherFilesAsync(folderPath, FilesSearchPattern, SearchOption, cancellationToken)
                : null;

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsEnabled || selectedCommand?.CanExecute(folderPath) == false) return;

            // Preserve host bindings. Publish only after enumeration succeeds, so failures keep the previous result.
            SetCurrentValue(SelectedFolderPathProperty, folderPath);
            SetCurrentValue(GatheredFilesProperty, gatheredFiles);
            await ExecuteCommandAsync(selectedCommand, folderPath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            SetValue(LastErrorPropertyKey, exception);
            var failureCommand = SelectionFailedCommand;
            if (failureCommand?.CanExecute(exception) == true)
                await ExecuteCommandAsync(failureCommand, exception, CancellationToken.None);
        }
    }

    private static Task<string[]> GatherFilesAsync(string folder, string pattern, SearchOption searchOption,
        CancellationToken cancellationToken) => Task.Run(() =>
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = searchOption == System.IO.SearchOption.AllDirectories,
                IgnoreInaccessible = false,
                AttributesToSkip = FileAttributes.ReparsePoint
            };
            List<string> paths = [];
            foreach (var path in Directory.EnumerateFiles(folder, pattern, options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                paths.Add(path);
            }
            return paths.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        }, cancellationToken);

    private static async Task ExecuteCommandAsync(ICommand? command, object parameter,
        CancellationToken cancellationToken)
    {
        if (command is null) return;

        if (command is IAsyncRelayCommand asyncCommand)
        {
            var execution = asyncCommand.ExecuteAsync(parameter);
            using var registration = cancellationToken.Register(asyncCommand.Cancel);
            await execution;
        }
        else
        {
            command.Execute(parameter);
        }
    }
}
