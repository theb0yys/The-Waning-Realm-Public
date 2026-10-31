param(
    [string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord',
    [string]$HarmonyDllPath = "$env:USERPROFILE\.nuget\packages\lib.harmony\2.3.3\lib\net472\0Harmony.dll"
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$outputRoot = Join-Path $repoRoot 'Artifacts/outputs/custom-battle-return'
if (!(Test-Path -LiteralPath $HarmonyDllPath -PathType Leaf)) {
    $HarmonyDllPath = Join-Path $GameRoot 'Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client/0Harmony.dll'
}
if (!(Test-Path -LiteralPath $HarmonyDllPath -PathType Leaf)) { throw 'Install Harmony or supply -HarmonyDllPath.' }
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

function Build-Project([string]$Project, [string]$Lane) {
    $laneRoot = Join-Path $outputRoot "build/$Lane"
    & dotnet build $Project -c Release --ignore-failed-sources -v minimal `
        "-p:GameRoot=$GameRoot" "-p:HarmonyDllPath=$HarmonyDllPath" `
        "-p:BaseOutputPath=$laneRoot/bin/" "-p:BaseIntermediateOutputPath=$laneRoot/obj/"
    if ($LASTEXITCODE -ne 0) { throw "$Lane Release build failed." }
}

# Keep dotnet's diagnostic output visible without capturing it as the function return value.
$moduleBuild = Join-Path $outputRoot 'build/module'
$testBuild = Join-Path $outputRoot 'build/tests'
Build-Project (Join-Path $PSScriptRoot 'CustomBattleReturn.csproj') 'module' | Out-Host
Build-Project (Join-Path $PSScriptRoot 'tests/CustomBattleReturn.Tests.csproj') 'tests' | Out-Host
$testExe = Join-Path $testBuild 'bin/Release/net472/CustomBattleReturn.Tests.exe'
$testResult = & $testExe 2>&1
$testResult | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Owner tests failed; no package produced.' }
$nativeApiResult = & $testExe $GameRoot 2>&1
$nativeApiResult | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Installed native API probe failed; no package produced.' }

$sourceManifest = Join-Path $PSScriptRoot 'module/CustomBattleReturn/SubModule.xml'
[xml]$manifest = Get-Content -LiteralPath $sourceManifest -Raw
if ($manifest.Module.Id.value -ne 'CustomBattleReturn' -or
    $manifest.Module.SubModules.SubModule.DLLName.value -ne 'CustomBattleReturn.dll' -or
    $manifest.Module.SubModules.SubModule.SubModuleClassType.value -ne 'CustomBattleReturn.SubModule') {
    throw 'Module identity does not match the built project.'
}
$dependencies = @($manifest.Module.DependedModules.DependedModule | ForEach-Object { $_.Id })
if (($dependencies -join ',') -ne 'Bannerlord.Harmony,Native,CustomBattle') { throw 'Unexpected dependencies.' }
$builtDll = Join-Path $moduleBuild 'bin/Release/net472/CustomBattleReturn.dll'
$package = Join-Path $outputRoot 'package/CustomBattleReturn'
$packageBin = Join-Path $package 'bin/Win64_Shipping_Client'
New-Item -ItemType Directory -Force -Path $packageBin | Out-Null
Copy-Item -LiteralPath $builtDll -Destination (Join-Path $packageBin 'CustomBattleReturn.dll') -Force
Copy-Item -LiteralPath $sourceManifest -Destination (Join-Path $package 'SubModule.xml') -Force
$packageFiles = @(Get-ChildItem -LiteralPath $package -Recurse -File)
$expected = @('SubModule.xml', 'bin/Win64_Shipping_Client/CustomBattleReturn.dll') | Sort-Object
$actual = @($packageFiles | ForEach-Object { $_.FullName.Substring($package.Length + 1).Replace('\', '/') }) | Sort-Object
if (@(Compare-Object $expected $actual).Count -ne 0) { throw 'Package contains missing or unexpected files.' }
$dllHash = (Get-FileHash -LiteralPath $builtDll -Algorithm SHA256).Hash
if ((Get-FileHash -LiteralPath (Join-Path $packageBin 'CustomBattleReturn.dll')).Hash -ne $dllHash) {
    throw 'Packaged DLL differs from the Release output.'
}
$zipPath = Join-Path $outputRoot 'CustomBattleReturn-v1.0.0.zip'
Write-Output 'Package assertions passed; creating install archive.'
Compress-Archive -LiteralPath $package -DestinationPath $zipPath -Force

$assemblyPaths = @(
    'bin/Win64_Shipping_Client/TaleWorlds.Core.dll',
    'bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.dll',
    'Modules/CustomBattle/bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.CustomBattle.dll',
    'Modules/Native/bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.GauntletUI.dll',
    'Modules/Native/bin/Win64_Shipping_Client/TaleWorlds.MountAndBlade.View.dll'
)
Write-Output 'Recording inspected assembly hashes.'
$assemblyEvidence = @($assemblyPaths | ForEach-Object {
    $assemblyPath = Join-Path $GameRoot $_
    @{ path = $assemblyPath; sha256 = (Get-FileHash -LiteralPath $assemblyPath).Hash;
       fileVersion = (Get-Item -LiteralPath $assemblyPath).VersionInfo.FileVersion }
})
Write-Output 'Recording owner source hashes.'
$sourceEvidence = @(Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -File | ForEach-Object {
    @{ path = $_.FullName; sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
})
Write-Output 'Recording decompiler identity.'
$decompilerVersion = @(& ilspycmd --version)
$receipt = [ordered]@{
    owner = 'CustomBattleReturn'; generatedUtc = [DateTime]::UtcNow.ToString('o')
    build = 'PASSED: Release net472 against installed game assemblies'
    tests = ($testResult -join "`n"); nativeApiProbe = ($nativeApiResult -join "`n")
    packageAssertions = 'PASSED: dependency, class, DLL identity; exactly two owner files; matching DLL hashes'
    # Read a plain .NET string: Get-Content strings carry provider metadata that
    # Windows PowerShell 5.1 recursively expands during deep JSON serialization.
    gamePackageInfo = [System.IO.File]::ReadAllText((Join-Path $GameRoot 'package_info.txt'))
    researchTool = 'ilspycmd'; researchToolVersion = $decompilerVersion[0]
    assemblies = $assemblyEvidence; sources = $sourceEvidence
    harmonyReference = @{ path = $HarmonyDllPath; sha256 = (Get-FileHash -LiteralPath $HarmonyDllPath).Hash }
    package = $package; dllSha256 = $dllHash; zip = $zipPath; zipSha256 = (Get-FileHash -LiteralPath $zipPath).Hash
    scope = 'Standalone mod only. Existing mods, suite, protected external data and Git refs unchanged.'
    performance = 'Low risk: O(state-stack depth) on pause menu creation/availability/click only; no simulation tick hook or hot-path reflection/IO.'
    centralTestCatalog = 'NOT_REGISTERED; owner test runner supplied. No suite coverage claimed.'
    nativeAcceptance = 'NOT_RUN'; deployment = 'NOT_RUN'; compatibility = 'Only v1.3.4 compiled; other versions unqualified. No save/config changes.'
    nextGate = 'User-started native field/siege return, retained selections, repeated battles and pause/input acceptance.'
}
Write-Output 'Writing validation receipt.'
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputRoot 'validation.json') -Encoding UTF8
Write-Output "Package: $zipPath"
Write-Output "Evidence: $(Join-Path $outputRoot 'validation.json')"
