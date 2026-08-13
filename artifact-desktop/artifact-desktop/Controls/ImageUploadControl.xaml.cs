using Microsoft.Win32;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Utils.Enumerable;

namespace artifact.desktop.Controls;

public partial class ImageUploadControl : UserControl
{
    #region Properties

    private static readonly Type _controlType = typeof(ImageUploadControl);
    
    private readonly string[] _supportExt = [".jpg", ".jpeg", ".png", ".bmp", ".webp"];
    
    public string? SelectedImagePath
    {
        get => (string)GetValue(SelectedImagePathProperty);
        set => SetValue(SelectedImagePathProperty, value);
    }
    public static readonly DependencyProperty SelectedImagePathProperty =
        DependencyProperty.Register(nameof(SelectedImagePath), typeof(string), _controlType,
            new PropertyMetadata(null, OnImagePathChanged));
    
    public event Action<string>? ImageChanged;
    
    #endregion Properties
    
    public ImageUploadControl()
    {
        InitializeComponent();
    }
    
    #region Image Actions
    
    private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageUploadControl ctrl && e.NewValue is string path && File.Exists(path))
        {
            ctrl.LoadImagePreview(path);
        }
    }
    
    private void LoadImagePreview(string filePath)
    {
        if (!File.Exists(filePath)) return;

        SelectedImagePath = filePath;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.EndInit();

        PreviewImage.Source = bitmap;
        PreviewImage.Visibility = Visibility.Visible;
        EmptyTipPanel.Visibility = Visibility.Collapsed;
        BtnClear.Visibility = Visibility.Visible;

        ImageChanged?.Invoke(filePath);
    }
    
    #endregion Image Actions
    
    #region Control Actions
    
    private void MainBorder_MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
        var dialog = new OpenFileDialog
        {
            Filter = "image|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
            Title = "Select an image",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            LoadImagePreview(dialog.FileName);
        }
    }
    
    private void Panel_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            MainBorder.BorderBrush = Resources["DragActiveBorder"] as System.Windows.Media.Brush;
            MainBorder.Background = Resources["BgDrag"] as System.Windows.Media.Brush;
            e.Effects = DragDropEffects.Copy;
        }
        e.Handled = true;
    }

    private void Panel_DragLeave(object sender, DragEventArgs e)
    {
        ResetPanelStyle();
    }
    
    private void Panel_Drop(object sender, DragEventArgs e)
    {
        ResetPanelStyle();
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (!files.HasItem())
        {
            Log.Warning("file drop event failed. got empty files.");
            return;
        }
        
        var filePath = files![0];
        var ext = Path.GetExtension(filePath).ToLower();

        if (!Array.Exists(_supportExt, x => x == ext))
        {
            MessageBox.Show("Only JPG / PNG / BMP / WEBP types are support!", "Format issue", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadImagePreview(filePath);
    }
    
    private void ResetPanelStyle()
    {
        MainBorder.BorderBrush = Resources["NormalBorder"] as System.Windows.Media.Brush;
        MainBorder.Background = Resources["BgNormal"] as System.Windows.Media.Brush;
    }
    
    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        SelectedImagePath = null;
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        EmptyTipPanel.Visibility = Visibility.Visible;
        BtnClear.Visibility = Visibility.Collapsed;
    }
    
    #endregion Control Actions
}