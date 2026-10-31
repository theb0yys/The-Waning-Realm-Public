param([string]$LiveModulesDir = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules')
$ErrorActionPreference = 'Stop'
$owner = $PSScriptRoot
$receiptPath = Join-Path $owner 'Release/validation-receipt.json'
if (!(Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw 'Run Validate.ps1 before deployment.' }
$validation = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
$liveRoot = (Resolve-Path -LiteralPath $LiveModulesDir).Path.TrimEnd('\', '/')
$destination = [IO.Path]::GetFullPath((Join-Path $liveRoot 'CustomBattleReforged'))
if ([IO.Path]::GetDirectoryName($destination) -ne $liveRoot -or [IO.Path]::GetFileName($destination) -ne 'CustomBattleReforged') { throw 'Owner destination boundary failed.' }
$manifestPath = Join-Path $destination 'SubModule.xml'
if (Test-Path -LiteralPath $manifestPath) {
    [xml]$existing = Get-Content -LiteralPath $manifestPath
    if ($existing.Module.Id.value -ne 'CustomBattleReforged') { throw 'Destination belongs to another module.' }
}
$payload = @(
    @{ Source = (Join-Path $owner 'SubModule.xml'); Relative = 'SubModule.xml' },
    @{ Source = (Join-Path $owner 'bin/Win64_Shipping_Client/CustomBattleReforged.dll'); Relative = 'bin/Win64_Shipping_Client/CustomBattleReforged.dll' }
)
# Preflight every source before any live mutation.
foreach ($item in $payload) {
    $pin = @($validation.files | Where-Object { $_.path -eq $item.Relative })
    if ($pin.Count -ne 1 -or (Get-FileHash -LiteralPath $item.Source -Algorithm SHA256).Hash -ne $pin[0].sha256) { throw "Unvalidated source: $($item.Relative)" }
}
$backup = Join-Path $owner ('Release/backups/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$records = @()
foreach ($item in $payload) {
    $target = [IO.Path]::GetFullPath((Join-Path $destination $item.Relative))
    if (!$target.StartsWith($destination + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target leaves the owner module.' }
    $oldHash = $null
    $backupPath = $null
    if (Test-Path -LiteralPath $target) {
        $oldHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $backupPath = Join-Path $backup $item.Relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupPath) | Out-Null
        Copy-Item -LiteralPath $target -Destination $backupPath
        if ((Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash -ne $oldHash) { throw 'Backup hash mismatch.' }
    }
    $records += [ordered]@{ source = $item.Source; destination = $target; backup = $backupPath; previous_sha256 = $oldHash }
}
foreach ($record in $records) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $record.destination) | Out-Null
    Copy-Item -LiteralPath $record.source -Destination $record.destination -Force
    $hash = (Get-FileHash -LiteralPath $record.source -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath $record.destination -Algorithm SHA256).Hash -ne $hash) { throw 'Live SHA256 verification failed.' }
    $record.sha256 = $hash
    $record.live_timestamp_utc = (Get-Item -LiteralPath $record.destination).LastWriteTimeUtc.ToString('o')
}
[ordered]@{ deployment_scope = 'non-suite-owner-only'; full_suite_deploy_required = $false; game_started = $false; files = $records } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $owner 'Release/owner-deployment-receipt.json') -Encoding UTF8
Write-Output "PASS owner-only deployment: $destination"
