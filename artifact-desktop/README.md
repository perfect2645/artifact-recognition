记录一下readme.md

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

## Reusable folder picker

`artifact.controls` is a separate .NET 10 WPF library containing
`artifact.desktop.Controls.FolderDialogControl`. Other WPF projects can reference
`artifact.controls/artifact.controls.csproj` without referencing the recognition
application. The library uses WPF and CommunityToolkit.Mvvm; it has no Material
Design, application service, or Messenger dependency.

Add the control's assembly-qualified namespace to the consuming view:

```
xmlns:controls="clr-namespace:artifact.desktop.Controls;assembly=artifact.controls"
```

A typical host binds the selected directory and handles explicit confirmations:

```
<controls:FolderDialogControl
    DialogTitle="Select a data folder"
    SelectedFolderPath="{Binding InputFolder}"
    FolderSelectedCommand="{Binding FolderSelectedCommand}"
    SelectionFailedCommand="{Binding FolderSelectionFailedCommand}" />
```

The public contract is:

| Member                                                                    | Contract                                                                                                                                                                                                             |
| ------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SelectedFolderPath`                                                      | Confirmed picker selection. The default binding mode is `TwoWay`; ordinary `OneWay` and `OneWayToSource` bindings are also supported.                                                                                |
| `InitialDirectory`, `DialogTitle`                                         | Inputs for the native folder dialog.                                                                                                                                                                                 |
| `DialogService`                                                           | An `IFolderDialogService` implementation. The library provides the native dialog by default; hosts can replace it without an application DI container.                                                               |
| `EnableGatherFiles`                                                       | Optional generic file enumeration, disabled by default.                                                                                                                                                              |
| `FilesSearchPattern`, `SearchOption`                                      | Enumeration inputs; defaults are `*.*` and `AllDirectories`. Directory reparse points are skipped.                                                                                                                   |
| `GatheredFiles`                                                           | Enumeration output as `string[]?`. Use `Mode=OneWayToSource` to send it to a host ViewModel; `null` means enumeration was not performed.                                                                             |
| `FileCount`, `LastError`                                                  | Read-only dependency-property outputs. Read them as binding sources, for example with `ElementName`; they cannot be binding targets.                                                                                 |
| `FolderSelectedCommand`                                                   | Receives the confirmed folder path as a `string`, including when the same directory is selected again. Its `CanExecute` is checked with that path before outputs change; asynchronous Toolkit commands are awaited.  |
| `SelectionFailedCommand`                                                  | Receives an `Exception` when selection, enumeration, or an unhandled confirmation callback fails. `LastError` also exposes the failure.                                                                              |
| `OpenFolderDialogCommand`                                                 | Opens the dialog and runs the selection workflow. It prevents concurrent executions and supports cooperative enumeration cancellation.                                                                               |
| `ButtonContent`, `ButtonStyle`, `ButtonPadding`, `ShowSelectedFolderPath` | Presentation inputs. `ButtonPadding` applies inside the button; inherited `Padding` applies around the control's content. The host can supply its own button style without adding a theme dependency to the library. |

To enable generic file enumeration, configure these additional properties:

```
EnableGatherFiles="True"
FilesSearchPattern="*.txt"
GatheredFiles="{Binding InputFiles, Mode=OneWayToSource}"
```

Cancelling the dialog or optional enumeration leaves the existing selection intact.
Dialog or enumeration failures retain the previous successful selection and file
list instead of reporting an empty successful scan. Updating picker outputs preserves
their bindings. The picker publishes its successful result before invoking the host
command; a host-command failure or cancellation does not roll that picker result
back. The host controls when its own business operation is committed. A
confirmed selection invokes `FolderSelectedCommand` even when its path has not
changed; use that command for workflows that must support reloading the same folder.
Hosts should avoid starting the same operation from both a path-change handler and
the confirmation command.

Set the control's inherited `IsEnabled` property when the host must prevent opening
the dialog, for example during a batch operation. The confirmation command's
`CanExecute` receives the newly chosen path, not an empty or previous path before
the dialog opens. If it rejects that path, the existing selection and file outputs
are left intact.

For a custom dialog or an automated check, assign a different `DialogService`.
`IFolderDialogService.SelectFolder(string dialogTitle, string initialDirectory, Window? owner)` returns the selected path, or `null` when the user cancels. The
control supplies its containing window as the owner when available. Dialog-service
exceptions use the same `LastError` and `SelectionFailedCommand` outputs.

`RecognitionBanner` uses the picker with enumeration disabled. Its ViewModel receives
the chosen directory through `LoadFolderCommand`, then calls the recognition file
service to discover DICOM files and exclude generated output folders. The Banner's
`FolderPath` represents the last successfully loaded queue, so it is committed only
after that scan succeeds. The picker's `InitialDirectory` is bound to that path;
the picker selection is not bound back to it before validation. Dialog failures
flow through `ReportFolderSelectionErrorCommand` into the page's existing error
message path. The parent Recognition ViewModel remains responsible for overall
Busy state and sends changes to the Banner through Messenger.

The picker's `LastError` reports failures handled by the picker itself. DICOM scan
errors handled inside `LoadFolderCommand` are reported by the Banner's error-message
flow; they are not copied into the picker's `LastError`.

## Build and regression checks

After restoring dependencies, run from the repository root on Windows with the
.NET 10 SDK. These commands use the restored dependencies without contacting a
package feed:

```
dotnet build artifact-desktop/artifact-desktop/artifact.desktop.csproj --no-restore -p:GeneratePackageOnBuild=false
dotnet run --project artifact-desktop/artifact.desktop.tests/artifact.desktop.tests.csproj --no-restore -p:GeneratePackageOnBuild=false
```

The regression runner uses no additional test packages and does not need a running backend.
It checks the folder picker's binding contract, optional enumeration, repeated
selection, cancellation and failure outputs, plus the Banner's loading, error and
Busy coordination. The real control uses a replacement `IFolderDialogService`
during these checks; the runner does not open a native dialog or submit recognition
requests.
