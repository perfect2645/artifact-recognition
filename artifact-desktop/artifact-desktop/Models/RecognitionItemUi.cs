using artifact.shared.data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace artifact.desktop.Models
{
    public partial class RecognitionItemUi : ObservableObject
    {
        public Guid? Id { get; set; }

        [ObservableProperty]
        public required partial string Name { get; set; }
        [ObservableProperty]
        public required partial string InputPath { get; set; }
        [ObservableProperty]
        public partial string? OutputPath { get; set; }
        [ObservableProperty]
        public partial DateTime? UpdateTime { get; set; }
        [ObservableProperty]
        public partial ArtifactStatus ArtifactStatus { get; set; }
        [ObservableProperty]
        public partial RecognitionStatus RecognitionStatus { get; set; }
        [ObservableProperty]
        public partial string? Comments { get; set; }
    }
}
