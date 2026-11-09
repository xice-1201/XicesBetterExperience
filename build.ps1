param(
    [string]$GamePath = 'E:\Steam\steamapps\common\Probably Stolen',
    [string]$UnityEditorPath = 'E:\UnityHub\UnityEditor\2022.3.61f1c1\Editor'
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$managedPath = Join-Path $GamePath 'Probably Stolen_Data/Managed'
$compiler = Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll'
$compilerHost = Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe'
if (!(Test-Path -LiteralPath $compiler) -or !(Test-Path -LiteralPath $compilerHost)) {
    throw 'Unity Roslyn compiler not found. Supply -UnityEditorPath pointing to the Editor directory.'
}
$referenceNames = @('mscorlib.dll', 'netstandard.dll', 'System.dll', 'System.Core.dll',
    'UnityEngine.CoreModule.dll', 'UnityEngine.TextRenderingModule.dll', 'UnityEngine.UIModule.dll', 'UnityEngine.UI.dll', 'Unity.TextMeshPro.dll',
    '0Harmony.dll', 'Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll')
$referenceArgs = foreach ($name in $referenceNames) {
    $referencePath = Join-Path $managedPath $name
    if (!(Test-Path -LiteralPath $referencePath)) { throw "Missing game reference: $referencePath" }
    '/reference:' + $referencePath
}
$packagePath = Join-Path $projectRoot 'dist/XicesBetterExperience'
New-Item -ItemType Directory -Path $packagePath -Force | Out-Null
$sourcePaths = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -Recurse | ForEach-Object FullName)
$compilerArgs = @('/nologo', '/noconfig', '/target:library', '/optimize+', '/nostdlib+', '/utf8output',
    ('/out:' + (Join-Path $packagePath 'XicesBetterExperience.dll'))) + $referenceArgs + $sourcePaths
& $compilerHost $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "C# compilation failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $projectRoot 'manifest.xml') -Destination $packagePath
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $packagePath
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $packagePath
Copy-Item -LiteralPath (Join-Path $projectRoot 'Localization') -Destination $packagePath -Recurse -Force
Write-Output "Built package: $packagePath"
