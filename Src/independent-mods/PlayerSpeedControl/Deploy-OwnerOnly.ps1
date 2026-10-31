param([string]$ModulesRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules')
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Bannerlord', 'Bannerlord.Native', 'TaleWorlds.MountAndBlade.Launcher' -ErrorAction SilentlyContinue) {
    throw 'Game or launcher is running. Close it yourself before owner deployment.'
}
$receipt = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Release/build-receipt.json') -Raw | ConvertFrom-Json
foreach ($inputFile in $receipt.Inputs) {
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $inputFile.Path) -Algorithm SHA256).Hash -ne $inputFile.Hash) {
        throw "Source changed since validation: $($inputFile.Path). Rebuild before deployment."
    }
}
$root = (Resolve-Path -LiteralPath $ModulesRoot).Path
$destination = [IO.Path]::GetFullPath((Join-Path $root 'PlayerSpeedControl'))
if ([IO.Path]::GetDirectoryName($destination) -ne $root) { throw 'Invalid owner deployment destination.' }
if (Test-Path -LiteralPath $destination) {
    if ((Get-Item -LiteralPath $destination -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'Owner destination is a managed link; direct deployment requires a physical owner folder.'
    }
}
$targets = @(
    [pscustomobject]@{ Relative = 'SubModule.xml'; Source = (Join-Path $PSScriptRoot 'SubModule.xml'); Expected = $receipt.ManifestHash },
    [pscustomobject]@{ Relative = 'bin/Win64_Shipping_Client/PlayerSpeedControl.dll'; Source = (Join-Path $PSScriptRoot 'bin/Win64_Shipping_Client/PlayerSpeedControl.dll'); Expected = $receipt.DllHash }
)
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../../..')).Path
$evidence = Join-Path $repo ('Artifacts/player-speed-control-20261010/deploy-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $evidence | Out-Null
$rows = @()
foreach ($target in $targets) {
    $live = Join-Path $destination $target.Relative
    $parent = Split-Path -Parent $live
    # Refuse links anywhere beneath the physical owner folder.
    for ($inspect = $live; $inspect -ne $destination; $inspect = Split-Path -Parent $inspect) {
        if ((Test-Path -LiteralPath $inspect) -and ((Get-Item -LiteralPath $inspect -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Linked destination requires separate review: $inspect"
        }
    }
    $before = if (Test-Path -LiteralPath $live) { (Get-FileHash -LiteralPath $live -Algorithm SHA256).Hash } else { $null }
    $backup = $null
    if ($before) {
        $backup = Join-Path $evidence ('backup/' + $target.Relative)
        New-Item -ItemType Directory -Force (Split-Path -Parent $backup) | Out-Null
        Copy-Item -LiteralPath $live -Destination $backup
        if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $before) { throw 'Backup verification failed.' }
    }
    $rows += [pscustomobject]@{ Source=$target.Source; Destination=$live; Expected=$target.Expected; Before=$before; Backup=$backup }
}
# Fresh source/live preflight immediately before the first live write.
foreach ($row in $rows) {
    if ((Get-FileHash -LiteralPath $row.Source -Algorithm SHA256).Hash -ne $row.Expected) { throw 'Prepared binary/manifest changed; rebuild required.' }
    $current = if (Test-Path -LiteralPath $row.Destination) { (Get-FileHash -LiteralPath $row.Destination -Algorithm SHA256).Hash } else { $null }
    if ($current -ne $row.Before) { throw 'Live file changed during preparation; deployment cancelled.' }
}
$rows | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'preflight.json')
foreach ($row in $rows) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $row.Destination) | Out-Null
    Copy-Item -LiteralPath $row.Source -Destination $row.Destination -Force
    $actual = (Get-FileHash -LiteralPath $row.Destination -Algorithm SHA256).Hash
    $sourceTime = (Get-Item -LiteralPath $row.Source).LastWriteTimeUtc
    $liveTime = (Get-Item -LiteralPath $row.Destination).LastWriteTimeUtc
    if ($actual -ne $row.Expected -or $liveTime -lt $sourceTime) { throw "Live verification failed: $($row.Destination)" }
    $row | Add-Member -NotePropertyName LiveHash -NotePropertyValue $actual
    $row | Add-Member -NotePropertyName LiveUtc -NotePropertyValue $liveTime.ToString('o')
    $row | Add-Member -NotePropertyName SourceUtc -NotePropertyValue $sourceTime.ToString('o')
}
$rows | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'receipt.json')
Write-Output "PASS: deployed only PlayerSpeedControl.dll and SubModule.xml. Evidence: $evidence"
