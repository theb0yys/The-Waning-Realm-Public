param([string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $inputFiles = @('PlayerJumpHeight.csproj', 'SubModule.xml', 'README.md', 'Build-Package.ps1', 'Tests/PlayerJumpHeight.Tests.csproj', 'Tests/Program.cs') + @(Get-ChildItem Source -Filter '*.cs' | ForEach-Object { 'Source/' + $_.Name })
    $inputs = @($inputFiles | ForEach-Object { [pscustomobject]@{ Path = $_; Hash = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } })
    & dotnet build Tests/PlayerJumpHeight.Tests.csproj -c Release "-p:GameRoot=$GameRoot" --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Owner Release/test build failed.' }
    $testOutput = & ./Tests/bin/Release/net472/PlayerJumpHeight.Tests.exe $GameRoot
    $testOutput | Write-Output
    if ($LASTEXITCODE -ne 0) { throw 'Owner tests failed.' }
    foreach ($inputFile in $inputs) {
        if ((Get-FileHash -LiteralPath $inputFile.Path -Algorithm SHA256).Hash -ne $inputFile.Hash) {
            throw "Build input changed during validation: $($inputFile.Path)"
        }
    }
    [xml]$manifest = Get-Content -LiteralPath SubModule.xml -Raw
    if ($manifest.Module.Id.value -ne 'PlayerJumpHeight' -or
        $manifest.Module.SubModules.SubModule.DLLName.value -ne 'PlayerJumpHeight.dll' -or
        $manifest.Module.SubModules.SubModule.SubModuleClassType.value -ne 'PlayerJumpHeight.SubModule' -or
        $manifest.Module.MultiplayerModule.value -ne 'false' -or
        @($manifest.Module.DependedModules.DependedModule.Id) -notcontains 'TAOM.Dependencies') {
        throw 'Manifest identity/dependency assertions failed.'
    }
    $stage = Join-Path $PSScriptRoot 'Release/PlayerJumpHeight'
    $bin = Join-Path $stage 'bin/Win64_Shipping_Client'
    New-Item -ItemType Directory -Force $bin | Out-Null
    Copy-Item -LiteralPath SubModule.xml, README.md -Destination $stage -Force
    Copy-Item -LiteralPath bin/Win64_Shipping_Client/PlayerJumpHeight.dll -Destination $bin -Force
    if (@(Get-ChildItem -LiteralPath $stage -File -Recurse).Count -ne 3) { throw 'Unexpected package staging files.' }
    $zip = Join-Path $PSScriptRoot 'Release/PlayerJumpHeight-0.1.0.zip'
    Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $files = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName.Replace('\','/') } | Sort-Object)
        $expected = @('PlayerJumpHeight/README.md', 'PlayerJumpHeight/SubModule.xml', 'PlayerJumpHeight/bin/Win64_Shipping_Client/PlayerJumpHeight.dll') | Sort-Object
        if (Compare-Object $expected $files) { throw 'ZIP entries do not match owner-only package.' }
    } finally { $archive.Dispose() }
    $dllHash = (Get-FileHash -LiteralPath bin/Win64_Shipping_Client/PlayerJumpHeight.dll -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath "$bin/PlayerJumpHeight.dll" -Algorithm SHA256).Hash -ne $dllHash) { throw 'Staged DLL mismatch.' }
    [pscustomobject]@{
        ValidatedUtc = [DateTime]::UtcNow.ToString('o'); Inputs = $inputs
        DllHash = $dllHash; ManifestHash = (Get-FileHash SubModule.xml -Algorithm SHA256).Hash
        ZipHash = (Get-FileHash $zip -Algorithm SHA256).Hash; GameRoot = $GameRoot
        Tests = @($testOutput); NativeValidation = 'NOT_RUN'; Deployment = 'NOT_RUN'
    } | ConvertTo-Json -Depth 5 | Set-Content Release/build-receipt.json
    Write-Output "PASS: owner tests, manifest, ZIP and hash checks: $zip"
}
finally { Pop-Location }
