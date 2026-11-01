[CmdletBinding()]
param([string]$GameRoot='A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord', [string]$OutputRoot)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if (-not $OutputRoot) { $OutputRoot=Join-Path $repo 'Releases/Compatibility/Xorberax-0.1.1' }
& dotnet build (Join-Path $PSScriptRoot 'Tests/Xorberax.Regression.csproj') -c Release --nologo -v minimal "-p:GameRoot=$GameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
& (Join-Path $PSScriptRoot 'Tests/bin/Release/net472/Xorberax.Regression.exe') $GameRoot
if ($LASTEXITCODE -ne 0) { throw 'Regression failed' }
$entries=Get-Content (Join-Path $repo 'Tools/compatibility/packages.json') -Raw | ConvertFrom-Json
$entry=@($entries | Where-Object mod -eq 'Xorberax')
if ($entry.Count -ne 1) { throw 'Expected one Xorberax catalog entry' }
$entry=$entry[0]
$name="Xorberax-TAOM-$($entry.taomVersion)"
$package=Join-Path $OutputRoot $name
$zipPath="$package.zip"
if ((Test-Path $package) -or (Test-Path $zipPath)) { throw 'Output exists; preserve it before rebuilding' }
$binary=Join-Path $PSScriptRoot 'bin/Release/net472/TAOM_XorberaxCompatibility.dll'
$hash=(Get-FileHash $binary -Algorithm SHA256).Hash
if ($hash -ne $entry.requiredSha256) { throw 'Built DLL differs from reviewed catalog hash' }
if ([Reflection.AssemblyName]::GetAssemblyName($binary).Version.ToString() -ne '0.1.1.0') { throw 'Wrong assembly version' }
[xml]$manifest=Get-Content (Join-Path $PSScriptRoot 'SubModule.xml') -Raw
if ($manifest.Module.Version.value -ne 'v0.1.1' -or $manifest.Module.Id.value -ne 'TAOM_XorberaxCompatibility') { throw 'Wrong manifest identity/version' }
$manifest.Module.SubModules.SubModule.DLLName.value="$name.dll"
$bin=Join-Path $package 'bin/Win64_Shipping_Client'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
Copy-Item -LiteralPath $binary -Destination (Join-Path $bin "$name.dll")
$manifest.Save((Join-Path $package 'SubModule.xml'))
[ordered]@{version='0.1.1';sha256=$hash;nativeCombat='NOT_RUN';validation=$entry.validation} | ConvertTo-Json | Set-Content (Join-Path $package 'validation.json') -Encoding utf8
@('Xorberax / TAOM compatibility patch 0.1.1.',
'Close Bannerlord. Back up and replace the old compatibility patch only.',
"Extract this ZIP into Modules/$name, with SubModule.xml directly in that folder.",
'Keep only one folder with module ID TAOM_XorberaxCompatibility.',
'Leave XorberaxLegacy and TAOM installed. Load this compatibility patch after both.',
'Launcher version must show v0.1.1; startup should report creature cleave compatibility 0.1.1 active.',
'TAOM 2.0.29.5 build identity is updated; Bannerlord, Sandbox, Harmony and Xorberax guards remain.',
'Managed regression checks passed. Native combat acceptance is NOT_RUN.') |
Set-Content (Join-Path $package 'README.txt') -Encoding utf8
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream=[IO.File]::Open($zipPath,[IO.FileMode]::CreateNew)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try {
    foreach($relative in @('SubModule.xml','README.txt','validation.json',"bin/Win64_Shipping_Client/$name.dll")) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $package $relative),$relative) | Out-Null
    }
} finally { $zip.Dispose(); $stream.Dispose() }
$zip=[IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entry=$zip.GetEntry("bin/Win64_Shipping_Client/$name.dll")
    if (-not $entry) { throw 'Missing DLL entry' }
    $stream=$entry.Open(); $sha=[Security.Cryptography.SHA256]::Create()
    try { $archiveHash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','') }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($archiveHash -ne $hash) { throw 'ZIP DLL hash mismatch' }
    $stream=$zip.GetEntry('SubModule.xml').Open()
    try { $check=[xml]::new();$check.Load($stream) } finally { $stream.Dispose() }
    if ($check.Module.Version.value -ne 'v0.1.1' -or $check.Module.SubModules.SubModule.DLLName.value -ne "$name.dll") {throw 'ZIP manifest mismatch'}
} finally { $zip.Dispose() }
[pscustomobject]@{Zip=$zipPath;SHA256=$hash;Version='0.1.1';NativeCombat='NOT_RUN'}

