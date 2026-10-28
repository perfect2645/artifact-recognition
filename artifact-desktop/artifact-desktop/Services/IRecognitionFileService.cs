using artifact.desktop.Models;
using System.Windows.Media.Imaging;

namespace artifact.desktop.Services
{
    public interface IRecognitionFileService
    {
        string? SelectFolder();
        Task<IReadOnlyList<RecognitionItemUi>> LoadFilesAsync(string folder, CancellationToken cancellationToken);
        Task<BitmapSource?> LoadPreviewAsync(string path, CancellationToken cancellationToken);
        void OpenFile(string path);
        void ShowInFolder(string path);
    }
}
