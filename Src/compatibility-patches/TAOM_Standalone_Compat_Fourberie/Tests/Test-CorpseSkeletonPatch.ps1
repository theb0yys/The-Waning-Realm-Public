[CmdletBinding()]
param([string]$GameRoot = 'A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Assert-Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "ASSERTION FAILED: $Message" }
    Write-Output "PASS $Message"
}
$owner = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $GameRoot 'bin\Win64_Shipping_Client'
# Assembly paths are explicit; keep the working directory in the workspace.
foreach ($folder in @($bin, (Join-Path $GameRoot 'Modules\Native\bin\Win64_Shipping_Client'), (Join-Path $GameRoot 'Modules\SandBox\bin\Win64_Shipping_Client'), (Join-Path $GameRoot 'Modules\TAOM.Dependencies\bin\Win64_Shipping_Client'))) {
    Get-ChildItem -LiteralPath $folder -Filter '*.dll' -File | ForEach-Object {
        try { [void][Reflection.Assembly]::LoadFrom($_.FullName) } catch {}
    }
}
[void][Reflection.Assembly]::LoadFrom((Join-Path $GameRoot 'Modules\Fourberie\bin\Win64_Shipping_Client\Fourberie.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $owner 'bin\x64\Release\Fourberie_CompatPatch.dll'))
Assert-Check ([Fourberie_CompatPatch.FourberieCorpseSkeletonPatch]::Prepare()) 'exact installed dragging and Fourberie types resolve'
$target = [Fourberie_CompatPatch.FourberieCorpseSkeletonPatch]::TargetMethod()
Assert-Check ($target.ToString() -eq 'Void OnAgentInteraction(TaleWorlds.MountAndBlade.Agent, TaleWorlds.MountAndBlade.Agent, SByte)') 'native interaction signature matches'
$reader = [HarmonyLib.PatchProcessor].GetMethod('GetOriginalInstructions', [type[]]@([Reflection.MethodBase], [Reflection.Emit.ILGenerator]))
$original = $reader.Invoke($null, @($target, $null))
$rewritten = @([Fourberie_CompatPatch.FourberieCorpseSkeletonPatch]::Transpiler($original))
Assert-Check ($rewritten.Count -eq $original.Count + 3) 'installed IL accepts precisely three inserted instructions'
$helper = [HarmonyLib.AccessTools]::Method([Fourberie_CompatPatch.FourberieCorpseSkeletonPatch], 'ResolveSkeletonAgent')
$helperIndices = @(for ($i = 0; $i -lt $rewritten.Count; $i++) { if ($rewritten[$i].operand -eq $helper) { $i } })
Assert-Check ($helperIndices.Count -eq 1) 'exactly one skeleton selector is inserted'
$at = $helperIndices[0]
Assert-Check ($rewritten[$at-3].opcode -eq [Reflection.Emit.OpCodes]::Ldarg_1 -and $rewritten[$at-2].opcode -eq [Reflection.Emit.OpCodes]::Ldarg_2 -and $rewritten[$at-1].opcode -eq [Reflection.Emit.OpCodes]::Ldarg_0) 'selector receives carrier corpse and mission logic in order'
Assert-Check ($rewritten[$at+1].operand.Name -eq 'get_AgentVisuals') 'selected agent feeds the original skeleton lookup'
$withoutInsertion = @($rewritten[0..($at-3)]) + @($rewritten[($at+1)..($rewritten.Count-1)])
Assert-Check ($withoutInsertion.Count -eq $original.Count) 'original instruction count is preserved'
for ($i = 0; $i -lt $original.Count; $i++) {
    if (-not [object]::ReferenceEquals($original[$i], $withoutInsertion[$i])) { throw "Original instruction $i changed" }
}
Write-Output 'PASS all original instructions labels and exception blocks preserved'
function Assert-Rejected($Code, [string]$Name) {
    $rejected = $false
    try { [void]@([Fourberie_CompatPatch.FourberieCorpseSkeletonPatch]::Transpiler($Code)) }
    catch { $rejected = $_.Exception.ToString().Contains('no mapping change applied') }
    Assert-Check $rejected $Name
}
$missing = New-Object 'Collections.Generic.List[HarmonyLib.CodeInstruction]'
$missing.Add([HarmonyLib.CodeInstruction]::new([Reflection.Emit.OpCodes]::Ret))
Assert-Rejected $missing 'missing skeleton lookup fails closed'
$duplicate = New-Object 'Collections.Generic.List[HarmonyLib.CodeInstruction]'
$duplicate.AddRange($original)
$duplicate.AddRange($original)
Assert-Rejected $duplicate 'ambiguous skeleton lookups fail closed'
Assert-Check ($null -eq [Fourberie_CompatPatch.FourberieCorpseSkeletonPatch]::ResolveSkeletonAgent($null,$null,$null)) 'null input remains native fallback'
$source = Get-Content -LiteralPath (Join-Path $owner 'Patches\FourberieCorpseSkeletonPatch.cs') -Raw
$runtime = $source.Substring($source.IndexOf('public static Agent ResolveSkeletonAgent'))
foreach ($token in @('AccessTools.', 'File.', 'Log(', 'ToList(', 'Race', 'UpdateSpawnEquipment')) {
    Assert-Check (-not $runtime.Contains($token)) "interaction path excludes $token"
}
Write-Output 'STATIC_SCOPE=exact Fourberie stealth type and humanoid pair required; manual review, not native proof'
Write-Output ('PATCH_SHA256=' + (Get-FileHash -LiteralPath (Join-Path $owner 'bin\x64\Release\Fourberie_CompatPatch.dll') -Algorithm SHA256).Hash)
Write-Output 'NATIVE_DRAG_DISGUISE_RESCUE_LOYALTY=NOT_RUN'
