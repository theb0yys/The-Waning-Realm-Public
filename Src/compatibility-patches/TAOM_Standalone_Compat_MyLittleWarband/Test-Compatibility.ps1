[CmdletBinding()]
param([string]$GameDir = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
$originalFiles = @(
    'MyLittleWarband/bin/Win64_Shipping_Client/MyLittleWarband.dll',
    'MyLittleWarband/SubModule.xml',
    'MyLittleWarband/GUI/Prefabs/CustomUnits.xml',
    'TAOM/bin/Win64_Shipping_Client/TAOM.dll',
    'TAOM/SubModule.xml'
)
$before = @{}
foreach ($relative in $originalFiles) {
    $before[$relative] = (Get-FileHash -LiteralPath (Join-Path "$GameDir/Modules" $relative) -Algorithm SHA256).Hash
}
$project = Join-Path $PSScriptRoot 'tests/CultureCompatibility.Tests.csproj'
& dotnet build $project -c Release --nologo -v minimal "-p:BannerlordDir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw 'Owner Release build failed.' }
[xml]$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SubModule.xml') -Raw
$id = 'TAOM_Standalone_Compat_MyLittleWarband'
if ($manifest.Module.Id.value -ne $id) { throw 'Wrong module ID.' }
foreach ($dependency in @('Native','TAOM.Dependencies','TAOM')) {
    if ($manifest.Module.DependedModules.DependedModule.Id -notcontains $dependency) {
        throw "Missing dependency: $dependency"
    }
}
if ($manifest.Module.ModulesToLoadAfterThis.Module.Id -notcontains 'MyLittleWarband') { throw 'Adapter must load before original MLW startup.' }
$sub = $manifest.Module.SubModules.SubModule
if ($sub.DLLName.value -ne "$id.dll" -or $sub.SubModuleClassType.value -ne "$id.SubModule") {
    throw 'Wrong loader identity.'
}
$binary = Join-Path $PSScriptRoot "bin/Release/net472/$id.dll"
if ([Reflection.AssemblyName]::GetAssemblyName($binary).Name -ne $id) { throw 'Wrong assembly identity.' }
Write-Output 'PASS: static module/dependency/loader assertions.'
$testExe = Join-Path $PSScriptRoot 'tests/bin/Release/net472/CultureCompatibility.Tests.exe'
$testOutput = @(& $testExe $GameDir)
$testOutput | ForEach-Object { Write-Output $_ }
if ($LASTEXITCODE -ne 0) { throw 'Owner managed tests or real MLW binding failed; module is not ready to install.' }
foreach ($relative in $originalFiles) {
    $after = (Get-FileHash -LiteralPath (Join-Path "$GameDir/Modules" $relative) -Algorithm SHA256).Hash
    if ($after -ne $before[$relative]) { throw "Original file changed: $relative" }
}
Write-Output 'PASS: original MLW/TAOM DLL/manifest and MLW editor prefab hashes unchanged.'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$evidenceDir = Join-Path $repo 'Artifacts/compatibility/MyLittleWarband'
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
[ordered]@{
    status = 'MANAGED_PASS_NATIVE_PENDING'
    moduleId = $id
    targets = @{ myLittleWarband='1.2.7'; bannerlord='1.4.8'; taom='2.0.29.5' }
    testedDllSha256 = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash
    originalFilesSha256 = $before
    managedOutput = $testOutput
    localCommand = 'powershell -ExecutionPolicy Bypass -File Src/compatibility-patches/TAOM_Standalone_Compat_MyLittleWarband/Test-Compatibility.ps1'
    nativeRenderingVerified = $false
    campaignSaveLoadVerified = $false
    deployed = $false
    protectedFilesTouched = @()
    originalModFilesModified = @()
    testCatalog = 'Owner-local tests added; no central catalog/runtime lane changed.'
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceDir 'validation.json') -Encoding utf8
Write-Output 'PASS: owner managed tests and static module/dependency/loader assertions. Native campaign validation remains pending.'
