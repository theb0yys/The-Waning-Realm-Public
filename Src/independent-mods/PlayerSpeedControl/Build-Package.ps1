param([string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $inputFiles = @('PlayerSpeedControl.csproj', 'SubModule.xml') + @(Get-ChildItem Source -Filter '*.cs' | ForEach-Object { 'Source/' + $_.Name })
    $inputs = @($inputFiles | ForEach-Object { [pscustomobject]@{ Path = $_; Hash = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } })
    & dotnet build PlayerSpeedControl.csproj -c Release "-p:GameRoot=$GameRoot" --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Owner Release build failed.' }
    & dotnet build Tests/PlayerSpeedControl.Tests.csproj -c Release "-p:GameRoot=$GameRoot" --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Owner test build failed.' }
    & ./Tests/bin/Release/net472/PlayerSpeedControl.Tests.exe $GameRoot
    if ($LASTEXITCODE -ne 0) { throw 'Owner tests failed.' }
    foreach ($inputFile in $inputs) {
        if ((Get-FileHash -LiteralPath $inputFile.Path -Algorithm SHA256).Hash -ne $inputFile.Hash) {
            throw "Build input changed while validating: $($inputFile.Path)"
        }
    }

    [xml]$manifest = Get-Content -LiteralPath SubModule.xml -Raw
    if ($manifest.Module.Id.value -ne 'PlayerSpeedControl' -or
        $manifest.Module.SubModules.SubModule.DLLName.value -ne 'PlayerSpeedControl.dll' -or
        $manifest.Module.SubModules.SubModule.SubModuleClassType.value -ne 'PlayerSpeedControl.SubModule') {
        throw 'Manifest identity does not match the owner assembly.'
    }
    $stage = Join-Path $PSScriptRoot 'Release/PlayerSpeedControl'
    $bin = Join-Path $stage 'bin/Win64_Shipping_Client'
    New-Item -ItemType Directory -Force $bin | Out-Null
    Copy-Item -LiteralPath SubModule.xml -Destination $stage -Force
    Copy-Item -LiteralPath bin/Win64_Shipping_Client/PlayerSpeedControl.dll -Destination $bin -Force
    Copy-Item -LiteralPath README.md -Destination $stage -Force
    if (@(Get-ChildItem -LiteralPath $stage -File -Recurse).Count -ne 3) {
        throw 'Unexpected files in package staging directory; inspect before packaging.'
    }
    $zip = Join-Path $PSScriptRoot 'Release/PlayerSpeedControl-0.1.0.zip'
    Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force
    Get-FileHash -Algorithm SHA256 -LiteralPath "$bin/PlayerSpeedControl.dll", "$stage/SubModule.xml", $zip |
        Select-Object Path, Hash | ConvertTo-Json | Set-Content Release/package-hashes.json
    [pscustomobject]@{
        ValidatedUtc = [DateTime]::UtcNow.ToString('o')
        Inputs = $inputs
        DllHash = (Get-FileHash -LiteralPath bin/Win64_Shipping_Client/PlayerSpeedControl.dll -Algorithm SHA256).Hash
        ManifestHash = (Get-FileHash -LiteralPath SubModule.xml -Algorithm SHA256).Hash
        GameRoot = $GameRoot
        Checks = 126
    } | ConvertTo-Json -Depth 5 | Set-Content Release/build-receipt.json
    Write-Output "PASS: owner tests, manifest assertions and package: $zip"
}
finally { Pop-Location }
