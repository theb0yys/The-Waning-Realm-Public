[CmdletBinding()]
param([string]$GameDir = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord', [switch]$Replace)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test-Compatibility.ps1') -GameDir $GameDir
if (-not $?) { throw 'Validation failed; no package will be created.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$id = 'TAOM_Standalone_Compat_MyLittleWarband'
$evidencePath = Join-Path $repo 'Artifacts/compatibility/MyLittleWarband/validation.json'
$evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
$dll = Join-Path $PSScriptRoot "bin/Release/net472/$id.dll"
if ($evidence.status -ne 'MANAGED_PASS_NATIVE_PENDING' -or
    $evidence.testedDllSha256 -ne (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash) {
    throw 'Missing or stale tested-binary evidence.'
}
$output = Join-Path $repo 'Releases/Compatibility'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$zip = Join-Path $output 'MyLittleWarband-TAOM-2.0.29.5.zip'
if (Test-Path -LiteralPath $zip) {
    if (-not $Replace) { throw "Package already exists; use -Replace to update the current build output: $zip" }
    Remove-Item -LiteralPath $zip
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$files = [ordered]@{
    'SubModule.xml' = (Join-Path $PSScriptRoot 'SubModule.xml')
    "bin/Win64_Shipping_Client/$id.dll" = $dll
    'README.md' = (Join-Path $PSScriptRoot 'README.md')
    'PACKAGE_DESIGN.md' = (Join-Path $PSScriptRoot 'PACKAGE_DESIGN.md')
    'validation.json' = $evidencePath
}
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $files.GetEnumerator()) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $entry.Value, "$id/$($entry.Key)") | Out-Null
    }
} finally { $archive.Dispose() }
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    if ($archive.Entries.Count -ne $files.Count) { throw 'Unexpected package contents.' }
    foreach ($name in $files.Keys) {
        if (-not $archive.GetEntry("$id/$name")) { throw "Missing package entry: $name" }
    }
    $stream = $archive.GetEntry("$id/bin/Win64_Shipping_Client/$id.dll").Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $sha.Dispose(); $stream.Dispose() }
    if ($hash -ne $evidence.testedDllSha256) { throw 'Packaged DLL differs from tested binary.' }
} finally { $archive.Dispose() }
Write-Output "PASS: package structure and tested-DLL hash: $zip"
Write-Output 'Original mod binaries are not bundled. No deployment or native campaign sign-off.'
