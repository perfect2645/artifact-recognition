# artifact-automation-desktop

Artifact could be automatically inspected, and the results can be automatically generated.

This is the WPF ui repo for artifact-automation project.

## Infrastructure

### .net version

- .net 10.0

## Recognition workspace

The English batch-recognition page uses Material Design in XAML and CommunityToolkit.Mvvm.
Select a folder to load its DICOM files, including subfolders. Generated `output` subfolders
are excluded so worker-created copies are not submitted again. The file queue supports
relative-path search, result filters, and virtualized rows. Its order stays fixed while
SignalR updates each file and the overall progress.

The details panel previews the PNG image returned by the worker. The output path must be
accessible from the desktop computer. Raw DICOM files open in the system's associated
viewer; no additional DICOM rendering library is required. Failed files can be retried
individually or together without rerunning successful files.

## Build and regression checks

Run from the repository root on Windows with the .NET 10 SDK:

```powershell
dotnet build artifact-desktop/artifact-desktop/artifact.desktop.csproj
dotnet run --project artifact-desktop/artifact.desktop.tests/artifact.desktop.tests.csproj
```

The regression runner uses no additional test packages and does not need a running backend.
It checks message correlation, stale/duplicate updates, result filtering, retries, submission
failures, preview races, DICOM file discovery, and the recognition HTTP endpoint.
To also render the real WPF page at normal and compact sizes, pass an output directory:

```powershell
dotnet run --project artifact-desktop/artifact.desktop.tests/artifact.desktop.tests.csproj -- ./output/wpf-preview
```
