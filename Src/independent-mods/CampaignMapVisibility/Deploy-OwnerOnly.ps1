param([string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
$ownerPath = $PSScriptRoot
$packagePath = Join-Path $ownerPath 'Release/package/CampaignMapVisibility'
$livePath = Join-Path $GameRoot 'Modules/CampaignMapVisibility'
$receiptPath = Join-Path $ownerPath 'Release/receipt.json'
$receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
if ($receipt.release_build -ne 'PASS' -or $receipt.offline_tests -ne 'PASS') { throw 'A passing Release package is required.' }
$provider = Join-Path $GameRoot 'Modules/TAOM.Dependencies/bin/Win64_Shipping_Client'
$mcmPath = Join-Path $provider 'MCMv5.dll'
if (!(Test-Path -LiteralPath $mcmPath -PathType Leaf)) { throw 'TAOM.Dependencies MCM is unavailable.' }
if ([Reflection.AssemblyName]::GetAssemblyName($mcmPath).FullName -ne $receipt.compiled_mcm) { throw 'MCM differs from the tested build reference.' }
$viewModel = Join-Path $GameRoot 'Modules/SandBox/bin/Win64_Shipping_Client/SandBox.ViewModelCollection.dll'
if ((Get-FileHash -LiteralPath $viewModel -Algorithm SHA256).Hash -ne '8C4B2CA448A3BC8789A6F8D7B4427E7A9832A31C45DD79E9034173535EBAEBF4') {
    throw 'Installed game UI differs from the tested target.'
}
$files = @('SubModule.xml', 'bin/Win64_Shipping_Client/CampaignMapVisibility.dll')
if ($receipt.payload.Count -ne 2) { throw 'Unexpected package scope.' }
foreach ($relativePath in $files) {
    $built = Join-Path $ownerPath $relativePath
    $prepared = Join-Path $packagePath $relativePath
    $entry = @($receipt.payload | Where-Object { $_.path.Replace('\', '/') -eq $relativePath })
    if ($entry.Count -ne 1) { throw "Missing payload receipt: $relativePath" }
    $hash = (Get-FileHash -LiteralPath $prepared -Algorithm SHA256).Hash
    if ($hash -ne $entry[0].sha256 -or $hash -ne (Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash) {
        throw "Changed source/prepared payload: $relativePath"
    }
}
if (Get-Process -Name Bannerlord,Bannerlord.Native -ErrorAction SilentlyContinue) {
    throw 'Bannerlord is running. Close it yourself before replacing module files.'
}
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
$backupPath = Join-Path $ownerPath "Release/backups/$stamp"
$deployed = @()
foreach ($relativePath in $files) {
    $source = Join-Path $packagePath $relativePath
    $destination = Join-Path $livePath $relativePath
    $backup = $null
    $previousHash = $null
    if (Test-Path -LiteralPath $destination -PathType Leaf) {
        $backup = Join-Path $backupPath $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item -LiteralPath $destination -Destination $backup -Force
        $previousHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $previousHash) { throw 'Backup hash mismatch.' }
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
    $hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if ($hash -ne (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash) { throw "Live hash mismatch: $relativePath" }
    $deployed += [ordered]@{ path=$destination; sha256=$hash; previous_sha256=$previousHash; backup=$backup; last_write_utc=(Get-Item -LiteralPath $destination).LastWriteTimeUtc.ToString('o') }
}
$deployment = [ordered]@{
    deployed_utc=[DateTime]::UtcNow.ToString('o'); deployment_scope='non-suite-owner-only'
    dependency_provider='TAOM.Dependencies'; dependencies_modified=$false
    native_campaign='NOT_RUN'; game_started=$false; payload=$deployed
}
$deployment | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $ownerPath 'Release/deployment.json') -Encoding UTF8
$receipt.live_deployed = $true
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding UTF8
$deployment | ConvertTo-Json -Depth 6
