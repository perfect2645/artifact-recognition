using CommunityToolkit.Mvvm.ComponentModel;

namespace artifact.desktop.Models
{
    public enum RecognitionFilter
    {
        All,
        Artifacts,
        NoArtifacts,
        Failed
    }

    public partial class RecognitionFilterOption(RecognitionFilter filter, string label) : ObservableObject
    {
        public RecognitionFilter Filter { get; } = filter;
        public string Label { get; } = label;

        [ObservableProperty]
        private int count;
    }
}
