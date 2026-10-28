using artifact.shared.data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace artifact.desktop.Models;

public partial class RecognitionItemUi : ObservableObject
{
    public Guid? Id { get; set; }
    [ObservableProperty] public required partial string Name { get; set; }
    [ObservableProperty] public required partial string InputPath { get; set; }
    public string RelativePath { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string FileSize => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024d:N1} KB" : $"{SizeBytes / (1024d * 1024):N1} MB";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput))]
    public partial string? OutputPath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdatedAt))]
    public partial DateTime? UpdateTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultLabel), nameof(HasArtifact), nameof(HasNoArtifact))]
    public partial ArtifactStatus ArtifactStatus { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel), nameof(ResultLabel), nameof(IsTerminal),
        nameof(IsCompleted), nameof(IsFailed), nameof(IsProcessing), nameof(HasArtifact),
        nameof(HasNoArtifact), nameof(HasOutput), nameof(DetailTitle), nameof(DetailDescription))]
    public partial RecognitionStatus RecognitionStatus { get; set; }

    [ObservableProperty] public partial string? Comments { get; set; }

    public bool IsCompleted => RecognitionStatus == RecognitionStatus.Completed;
    public bool IsFailed => RecognitionStatus == RecognitionStatus.Failed;
    public bool IsTerminal => IsCompleted || IsFailed || RecognitionStatus == RecognitionStatus.Cancelled;
    public bool IsProcessing => RecognitionStatus == RecognitionStatus.Processing;
    public bool HasArtifact => IsCompleted && ArtifactStatus == ArtifactStatus.ArtifactExists;
    public bool HasNoArtifact => IsCompleted && ArtifactStatus == ArtifactStatus.NoArtifact;
    public bool HasOutput => IsCompleted && !string.IsNullOrWhiteSpace(OutputPath);
    public string UpdatedAt => UpdateTime?.ToLocalTime().ToString("HH:mm:ss") ?? "—";
    public string StatusLabel => RecognitionStatus switch
    {
        RecognitionStatus.Creating => "Submitting",
        RecognitionStatus.Created => "Queued",
        RecognitionStatus.Processing => "Recognizing",
        RecognitionStatus.Completed => "Completed",
        RecognitionStatus.Failed => "Failed",
        RecognitionStatus.Cancelled => "Cancelled",
        _ => "Pending"
    };
    // An unsuccessful recognition must never be displayed as a negative classification.
    public string ResultLabel => !IsCompleted ? "—" : ArtifactStatus switch
    {
        ArtifactStatus.ArtifactExists => "Artifact detected",
        ArtifactStatus.NoArtifact => "No artifact",
        _ => "Unknown"
    };
    public string DetailTitle => IsCompleted ? "Recognition complete" : StatusLabel;
    public string DetailDescription => RecognitionStatus switch
    {
        RecognitionStatus.Completed => "Review the classification and output image below.",
        RecognitionStatus.Failed => "No classification was produced. Check the error details and retry this file.",
        RecognitionStatus.Cancelled => "This file was cancelled and has no classification.",
        RecognitionStatus.Processing => "The result will appear here as soon as it arrives.",
        RecognitionStatus.Creating or RecognitionStatus.Created => "This file is waiting for the recognition service.",
        _ => "Start recognition to see the classification, output image, and model notes."
    };
    public static RecognitionItemUi FromFile(string path, string folder) => new()
    {
        Id = Guid.NewGuid(),
        Name = Path.GetFileName(path),
        InputPath = path,
        RelativePath = Path.GetRelativePath(folder, path),
        SizeBytes = new FileInfo(path).Length
    };
}