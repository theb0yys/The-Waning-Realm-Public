param(
    [string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord',
    [string]$McmAssembly = "$GameRoot\Modules\TAOM.Dependencies\bin\Win64_Shipping_Client\MCMv5.dll"
)
$ErrorActionPreference = 'Stop'
$ownerPath = $PSScriptRoot
$releasePath = Join-Path $ownerPath 'Release'
$env:DOTNET_CLI_HOME = Join-Path $releasePath 'dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'

if (!(Test-Path -LiteralPath $McmAssembly -PathType Leaf)) { throw "MCM build reference missing: $McmAssembly" }
& dotnet build (Join-Path $ownerPath 'Tests/CampaignMapVisibility.Tests.csproj') -c Release --nologo -v minimal --ignore-failed-sources "-p:GameRoot=$GameRoot" "-p:McmAssembly=$McmAssembly"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
$testExe = Join-Path $ownerPath 'Tests/bin/Release/net472/CampaignMapVisibility.Tests.exe'
$testOutput = & $testExe $GameRoot $McmAssembly 2>&1
$testExit = $LASTEXITCODE
$testOutput | ForEach-Object { Write-Output $_ }
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
$testOutput | Set-Content -LiteralPath (Join-Path $releasePath 'offline-tests.txt') -Encoding UTF8
if ($testExit -ne 0) { throw 'Offline checks failed; package not generated.' }

$modulePath = Join-Path $releasePath 'package/CampaignMapVisibility'
$binPath = Join-Path $modulePath 'bin/Win64_Shipping_Client'
New-Item -ItemType Directory -Path $binPath -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $ownerPath 'SubModule.xml') -Destination $modulePath -Force
Copy-Item -LiteralPath (Join-Path $ownerPath 'bin/Win64_Shipping_Client/CampaignMapVisibility.dll') -Destination $binPath -Force
[xml]$manifest = Get-Content -LiteralPath (Join-Path $modulePath 'SubModule.xml') -Raw
if ($manifest.Module.Id.value -ne 'CampaignMapVisibility' -or
    $manifest.Module.SubModules.SubModule.DLLName.value -ne 'CampaignMapVisibility.dll') { throw 'Package manifest identity mismatch.' }
$dependencyIds = @($manifest.Module.DependedModules.DependedModule | ForEach-Object { $_.Id })
if ($dependencyIds -notcontains 'TAOM.Dependencies' -or
    $dependencyIds -contains 'Bannerlord.Harmony' -or $dependencyIds -contains 'Bannerlord.MBOptionScreen') {
    throw 'Manifest must use the TAOM dependency provider, not require standalone alias modules.'
}
$files = @(Get-ChildItem -LiteralPath $modulePath -Recurse -File)
if ($files.Count -ne 2) { throw 'Package contains unexpected files; review before packaging.' }
$payload = @($files | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($modulePath.Length + 1); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
})
$mcmLive = Join-Path $GameRoot 'Modules/TAOM.Dependencies/bin/Win64_Shipping_Client/MCMv5.dll'
$receipt = [ordered]@{
    version = '0.1.0'; created_utc = [DateTime]::UtcNow.ToString('o')
    deployment_scope = 'non-suite-owner-only'; full_suite_deploy_required = $false
    release_build = 'PASS'; offline_tests = 'PASS'; native_campaign = 'NOT_RUN'
    live_deployed = $false; dependency_provider = 'TAOM.Dependencies'
    live_mcm_present = (Test-Path -LiteralPath $mcmLive -PathType Leaf)
    compiled_mcm = [Reflection.AssemblyName]::GetAssemblyName($McmAssembly).FullName
    payload = $payload
}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $releasePath 'receipt.json') -Encoding UTF8
$zipPath = Join-Path $releasePath 'CampaignMapVisibility-0.1.0.zip'
Compress-Archive -LiteralPath $modulePath -DestinationPath $zipPath -Force
Get-FileHash -LiteralPath $zipPath -Algorithm SHA256 | Format-List
Write-Output "Package: $zipPath"
if (!$receipt.live_mcm_present) { Write-Output 'Live deployment blocked: MCMv5.dll is missing from TAOM.Dependencies. No dependencies were deployed.' }
