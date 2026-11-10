param([string]$UnityEditorPath = 'E:\UnityHub\UnityEditor\2022.3.61f1c1\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot 'dist/tests'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$testExe = Join-Path $outputPath 'GuideRegression.exe'
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$arguments = @('/define:REGRESSION_TESTS', '/nologo', '/noconfig', '/nostdlib+', '/target:exe', ('/out:' + $testExe),
    ('/reference:' + (Join-Path $frameworkPath 'mscorlib.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.Core.dll')),
    (Join-Path $projectRoot 'src/ModEntry.cs'),
    (Join-Path $projectRoot 'src/ExperienceSettings.cs'),
    (Join-Path $projectRoot 'src/InjectorInspectionAutomation.cs'),
    (Join-Path $PSScriptRoot 'InjectorInspectionRegression.cs'),
    (Join-Path $projectRoot 'src/AutoOpenAppraisalGuide.cs'),
    (Join-Path $projectRoot 'src/StorageNameOverlay.cs'),
    (Join-Path $projectRoot 'src/StorageCapacityOverlay.cs'),
    (Join-Path $PSScriptRoot 'StorageCapacityRegression.cs'),
    (Join-Path $projectRoot 'src/DocumentPageMemory.cs'),
    (Join-Path $projectRoot 'src/AppraisalAutomation.cs'),
    (Join-Path $projectRoot 'src/StolenHeatMarker.cs'),
    (Join-Path $projectRoot 'src/ContrabandMarker.cs'),
    (Join-Path $projectRoot 'src/ExchangeAutoPickup.cs'),
    (Join-Path $projectRoot 'src/AutoDiscardAppraisalReceipt.cs'),
    (Join-Path $projectRoot 'src/PowerWarningMarker.cs'),
    (Join-Path $projectRoot 'src/MachineWarningCoordinator.cs'),
    (Join-Path $projectRoot 'src/OverflowWarningMarker.cs'),
    (Join-Path $PSScriptRoot 'OverflowWarningRegression.cs'),
    (Join-Path $projectRoot 'src/MarketChangeMarker.cs'),
    (Join-Path $projectRoot 'src/MarkerGraphic.cs'),
    (Join-Path $projectRoot 'src/ItemStatusBackground.cs'),
    (Join-Path $PSScriptRoot 'AppraisalAutomationRegression.cs'),
    (Join-Path $PSScriptRoot 'DocumentPageRegression.cs'),
    (Join-Path $PSScriptRoot 'StorageNamingRegression.cs'),
    (Join-Path $PSScriptRoot 'StolenHeatRegression.cs'),
    (Join-Path $PSScriptRoot 'ContrabandMarkerRegression.cs'),
    (Join-Path $PSScriptRoot 'ExchangeAutoPickupRegression.cs'),
    (Join-Path $PSScriptRoot 'AutoDiscardAppraisalReceiptRegression.cs'),
    (Join-Path $PSScriptRoot 'PowerWarningRegression.cs'),
    (Join-Path $PSScriptRoot 'MarketChangeRegression.cs'),
    (Join-Path $PSScriptRoot 'GuideRegression.cs'))
& (Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Regression harness compilation failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Guide regression failed.' }

$layoutExe = Join-Path $outputPath 'WindowLayoutRegression.exe'
$layoutArguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', ('/out:' + $layoutExe),
    ('/reference:' + (Join-Path $frameworkPath 'mscorlib.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.Core.dll')),
    (Join-Path $projectRoot 'src/WindowLayoutMemory.cs'),
    (Join-Path $PSScriptRoot 'WindowLayoutRegression.cs'))
& (Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll') @layoutArguments
if ($LASTEXITCODE -ne 0) { throw 'Window layout regression compilation failed.' }
& $layoutExe
if ($LASTEXITCODE -ne 0) { throw 'Window layout regression failed.' }

$augExe = Join-Path $outputPath 'AugIdentificationRegression.exe'
$augArguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', ('/out:' + $augExe),
    ('/reference:' + (Join-Path $frameworkPath 'mscorlib.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.Core.dll')),
    (Join-Path $projectRoot 'src/AugIdentificationRules.cs'),
    (Join-Path $projectRoot 'src/AugIdentificationAutomation.cs'),
    (Join-Path $PSScriptRoot 'AugIdentificationRegression.cs'))
& (Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll') @augArguments
if ($LASTEXITCODE -ne 0) { throw 'Aug identification regression compilation failed.' }
& $augExe
if ($LASTEXITCODE -ne 0) { throw 'Aug identification regression failed.' }

$valueExe = Join-Path $outputPath 'SingleCellValueRegression.exe'
$valueArguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', ('/out:' + $valueExe),
    ('/reference:' + (Join-Path $frameworkPath 'mscorlib.dll')),
    ('/reference:' + (Join-Path $frameworkPath 'System.dll')),
    (Join-Path $projectRoot 'src/SingleCellValue.cs'),
    (Join-Path $PSScriptRoot 'SingleCellValueRegression.cs'))
& (Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll') @valueArguments
if ($LASTEXITCODE -ne 0) { throw 'Single cell value regression compilation failed.' }
& $valueExe
if ($LASTEXITCODE -ne 0) { throw 'Single cell value regression failed.' }
