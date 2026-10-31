param([string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
$owner = $PSScriptRoot
$release = Join-Path $owner 'Release'
New-Item -ItemType Directory -Force -Path $release | Out-Null
dotnet build (Join-Path $owner 'CustomBattleReforged.csproj') -c Release -v quiet "-p:BannerlordPath=$GameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Owner Release build failed.' }
dotnet build (Join-Path $owner 'Tests/CustomBattleReforged.Tests.csproj') -c Release -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Owner tests build failed.' }
$dll = Join-Path $owner 'bin/Win64_Shipping_Client/CustomBattleReforged.dll'
$testExe = Join-Path $owner 'Tests/bin/Release/net472/CustomBattleReforged.Tests.exe'
$testOutput = @(& $testExe $GameRoot $dll 2>&1)
$testExit = $LASTEXITCODE
$testOutput | Set-Content -LiteralPath (Join-Path $release 'headless-tests.txt') -Encoding UTF8
$testOutput | ForEach-Object { Write-Output $_ }
if ($testExit -ne 0) { throw 'Owner headless checks failed; package not produced.' }
[xml]$manifest = Get-Content -LiteralPath (Join-Path $owner 'SubModule.xml')
if ($manifest.Module.Id.value -ne 'CustomBattleReforged' -or $manifest.Module.Name.value -ne 'Custom Battle Reforged') { throw 'Incorrect owner identity.' }
if ($manifest.Module.SubModules.SubModule.DLLName.value -ne 'CustomBattleReforged.dll') { throw 'Manifest DLL mismatch.' }
if (@($manifest.Module.DependedModules.DependedModule.Id) -notcontains 'TAOM') { throw 'Missing TAOM dependency.' }
if (@($manifest.Module.Xmls.XmlNode).Where({ $_ }).Count -ne 0) { throw 'No protected data should be packaged.' }
$packageRoot = Join-Path $release 'package/CustomBattleReforged'
New-Item -ItemType Directory -Force -Path (Join-Path $packageRoot 'bin/Win64_Shipping_Client') | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $packageRoot 'bin/Win64_Shipping_Client/CustomBattleReforged.dll') -Force
Copy-Item -LiteralPath (Join-Path $owner 'SubModule.xml') -Destination (Join-Path $packageRoot 'SubModule.xml') -Force
Copy-Item -LiteralPath (Join-Path $owner 'README.md') -Destination (Join-Path $packageRoot 'README.md') -Force
$expected = @('README.md', 'SubModule.xml', 'bin/Win64_Shipping_Client/CustomBattleReforged.dll')
$files = @(Get-ChildItem -LiteralPath $packageRoot -File -Recurse)
if ($files.Count -ne $expected.Count) { throw 'Unexpected package payload.' }
$hashes = @($files | ForEach-Object {
    $relative = $_.FullName.Substring($packageRoot.Length + 1).Replace('\', '/')
    if ($expected -notcontains $relative) { throw "Unexpected package file $relative" }
    [ordered]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$zip = Join-Path $release 'CustomBattleReforged-TAOM-2.0.29.5.zip'
Compress-Archive -LiteralPath $packageRoot -DestinationPath $zip -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entries = @($archive.Entries | Where-Object { !$_.FullName.EndsWith('/') -and !$_.FullName.EndsWith('\') })
    if ($entries.Count -ne $expected.Count) { throw 'Unexpected ZIP file payload.' }
    foreach ($entry in $entries) {
        $relative = $entry.FullName.Replace('\', '/')
        if (!$relative.StartsWith('CustomBattleReforged/')) { throw 'ZIP leaves the owner folder.' }
        if ($expected -notcontains $relative.Substring('CustomBattleReforged/'.Length)) { throw 'Unexpected ZIP entry.' }
    }
} finally { $archive.Dispose() }
$receipt = [ordered]@{
    module = 'CustomBattleReforged'; version = $manifest.Module.Version.value.TrimStart('v'); taom = '2.0.29.5'
    deployment_scope = 'non-suite-owner-only'; full_suite_deploy_required = $false
    proof = 'Release build, metadata/policy/performance checks and headless Harmony installation'
    native_runtime = 'not run; user starts game'; protected_files_changed = @()
    timestamp_utc = [DateTime]::UtcNow.ToString('o'); files = $hashes
    zip = $zip; zip_sha256 = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $release 'validation-receipt.json') -Encoding UTF8
Write-Output "PASS package: $zip"
