param([Parameter(Mandatory=$true)][string]$AssemblyPath,[Parameter(Mandatory=$true)][string]$ReferenceDir,[Parameter(Mandatory=$true)][string]$HarmonyPath)
$ErrorActionPreference='Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$referenceRoot=(Resolve-Path $ReferenceDir).Path
$harmonyRoot=Split-Path (Resolve-Path $HarmonyPath).Path
$resolver = [System.ResolveEventHandler] {
    param($sender,$eventArgs)
    $name=([System.Reflection.AssemblyName]::new($eventArgs.Name)).Name+'.dll'
    $candidate=Join-Path $referenceRoot $name
    if (Test-Path -LiteralPath $candidate) { return [System.Reflection.Assembly]::LoadFrom($candidate) }
    $candidate=Join-Path $harmonyRoot $name
    if (Test-Path -LiteralPath $candidate) { return [System.Reflection.Assembly]::LoadFrom($candidate) }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    [void][System.Reflection.Assembly]::LoadFrom((Resolve-Path $HarmonyPath).Path)
    [void][System.Reflection.Assembly]::LoadFrom((Join-Path $referenceRoot 'Newtonsoft.Json.dll'))
    $assembly=[System.Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
    $catalogType=$assembly.GetType('AnimusForge.SiegeAftermathIntervention.TownPromptTextCatalog',$true)
    $factsType=$assembly.GetType('AnimusForge.SiegeAftermathIntervention.SiegeCastleRuntimePromptFacts',$true)
    $profile=$assembly.GetType('AnimusForge.SiegeAftermathIntervention.SiegeCastleRuntimePromptProfile',$true)
    $build=$profile.GetMethod('Build',[type[]]@($factsType,$catalogType,[bool]))
    $ctor=$factsType.GetConstructors()[0]
    $arguments=New-Object object[] $ctor.GetParameters().Length
    foreach($parameter in $ctor.GetParameters()) { if($parameter.HasDefaultValue) { $arguments[$parameter.Position]=$parameter.DefaultValue } }
    $arguments[0]='Test Castle';$arguments[1]='Player';$arguments[2]=$false;$arguments[3]=$true;$arguments[4]=$false;$arguments[5]='';$arguments[6]=''
    $facts=$ctor.Invoke($arguments)
    $checks=0
    foreach($language in @('zh-CN','en')) {
        $path=Join-Path $root "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownPrompt.$language.json"
        $catalog=[Newtonsoft.Json.JsonConvert]::DeserializeObject([IO.File]::ReadAllText($path),$catalogType)
        $peace=$build.Invoke($null,[object[]]@($facts,$catalog,$true))
        $battle=$build.Invoke($null,[object[]]@($facts,$catalog,$false))
        if(!$peace.Contains($catalog.RuntimeText['CastleRuntime_PeacefulPrisoner']) -or $peace.Contains($catalog.RuntimeText['CastleRuntime_015'])) { throw "$language peaceful prisoner used conquest role" };$checks++
        if($peace.Contains($catalog.RuntimeText['CastleRuntime_001']) -or !$peace.Contains('Test Castle')) { throw "$language peaceful introduction leaked conquest" };$checks++
        if(!$battle.Contains($catalog.RuntimeText['CastleRuntime_001']) -or !$battle.Contains($catalog.RuntimeText['CastleRuntime_015'])) { throw "$language battle introduction regressed" };$checks++
        if($peace.Contains('{settlement}') -or $peace.Contains('{player}')) { throw "$language unresolved template variables" };$checks++
    }
    Write-Output "PASS actual DLL castle prompts: $checks checks (CN/EN peaceful prisoner and unchanged combat)."
    Write-Output 'SCOPE managed prompt execution; no native mission, campaign, save, provider or configuration writes.'
} catch { Write-Output $_.Exception.ToString(); throw } finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }
