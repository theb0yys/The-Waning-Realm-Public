[CmdletBinding()]
param(
    [string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord',
    [string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project = Join-Path $PSScriptRoot 'TAOM_Standalone_Compat_Retinues.csproj'
$tests = Join-Path $PSScriptRoot 'Tests/Retinues.Regression.csproj'
if (-not $OutputRoot) { $OutputRoot = Join-Path $repo 'Releases/Compatibility/Retinues-0.1.1' }
& dotnet build $tests -c Release --nologo -v minimal "-p:GameRoot=$GameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Retinues build failed.' }
& (Join-Path $PSScriptRoot 'Tests/bin/Release/net472/Retinues.Regression.exe') $GameRoot
if ($LASTEXITCODE -ne 0) { throw 'Retinues regressions failed; no package produced.' }
$entries = Get-Content (Join-Path $repo 'Tools/compatibility/packages.json') -Raw | ConvertFrom-Json
$retinues = @($entries | Where-Object mod -eq 'Retinues')
if ($retinues.Count -ne 1) { throw 'Expected exactly one Retinues catalog entry.' }
[xml]$manifest = Get-Content (Join-Path $PSScriptRoot 'SubModule.xml') -Raw
if ($manifest.Module.Version.value -ne 'v0.1.1') { throw 'Unexpected launcher version.' }
$binary = Join-Path $PSScriptRoot 'bin/Release/net472/TAOM_Standalone_Compat_Retinues.dll'
if ([Reflection.AssemblyName]::GetAssemblyName($binary).Version.ToString() -ne '0.1.1.0') { throw 'Unexpected DLL version.' }
if ((Get-FileHash $binary).Hash -eq '881632BC2F4DEDE01A1AEB58E18B90BB0A1E3D6F625BCA64B7AFAD25E8D85E67') { throw 'Legacy binary cannot be packaged.' }
$evidence = Join-Path $repo 'Artifacts/retinues-colour-fix-20261014'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$catalog = Join-Path $evidence 'package-catalog.json'
ConvertTo-Json -InputObject $retinues[0] -Depth 6 | Set-Content $catalog -Encoding utf8
$entry = $retinues[0]
$hash = (Get-FileHash $binary -Algorithm SHA256).Hash
if ($entry.requiredSha256 -ne $hash) { throw 'Built DLL differs from the reviewed catalog hash.' }
$name = "Retinues-TAOM-$($entry.taomVersion)"
$package = Join-Path $OutputRoot $name
$zipPath = "$package.zip"
if ((Test-Path $package) -or (Test-Path $zipPath)) { throw 'Output exists; preserve it before rebuilding.' }
$bin = Join-Path $package 'bin/Win64_Shipping_Client'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$manifest.Module.Name.value = $name
$manifest.Module.SubModules.SubModule.Name.value = $name
$manifest.Module.SubModules.SubModule.DLLName.value = "$name.dll"
$manifest.Save((Join-Path $package 'SubModule.xml'))
Copy-Item -LiteralPath $binary -Destination (Join-Path $bin "$name.dll")
[ordered]@{version='0.1.1';moduleId=$entry.moduleId;sha256=$hash;managedChecks=16;nativeRuntime='NOT_RUN';validation=$entry.validation} |
    ConvertTo-Json | Set-Content (Join-Path $package 'validation.json') -Encoding utf8
@(
    'Retinues clan-colour bridge 0.1.1 for TAOM 2.0.29.5 / inspected Retinues v2.0.0.13.',
    'Close the game. Back up and remove your old compatibility patch folder:',
    'Modules/Retinues-TAOM-2.0.26 or Modules/TAOM_Standalone_Compat_Retinues.',
    "Extract this ZIP into Modules/$name so SubModule.xml is directly inside it.",
    'Keep only one folder with module ID TAOM_Standalone_Compat_Retinues.',
    'Keep the original Retinues and TAOM mods installed. Load this patch after both.',
    'Launcher version must show v0.1.1. The game must report bridge 0.1.1 enabled.',
    'Release build and 16 managed regression checks passed; native visual acceptance NOT_RUN.',
    'Test custom clothing before/after joining a kingdom, in a new battle and after save/reload.'
) | Set-Content (Join-Path $package 'README.txt') -Encoding utf8
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false)
try {
    foreach ($relative in @('SubModule.xml','README.txt','validation.json',"bin/Win64_Shipping_Client/$name.dll")) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $package $relative), $relative) | Out-Null
    }
} finally { $archive.Dispose(); $stream.Dispose() }
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $dllEntry = $archive.GetEntry("bin/Win64_Shipping_Client/$name.dll")
    if (-not $dllEntry -or -not $archive.GetEntry('SubModule.xml')) { throw 'ZIP layout invalid.' }
    $dllStream = $dllEntry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $zipHash = ([BitConverter]::ToString($sha.ComputeHash($dllStream))).Replace('-','') }
    finally { $dllStream.Dispose(); $sha.Dispose() }
    if ($zipHash -ne $hash) { throw 'ZIP DLL hash mismatch.' }
} finally { $archive.Dispose() }
[pscustomobject]@{Zip=$zipPath; Version='0.1.1'; SHA256=$hash; Tests=16; NativeRuntime='NOT_RUN'}

