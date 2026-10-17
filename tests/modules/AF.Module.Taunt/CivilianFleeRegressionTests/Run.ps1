param(
 [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path,
 [string]$OutputDirectory = ''
)
$ErrorActionPreference='Stop'
if (!$OutputDirectory) { $OutputDirectory=Join-Path $SourceRoot 'artifacts\civilian-flee-20261018\replay' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
function Method([string]$text,[string]$name,[switch]$Optional) {
 $m=[regex]::Match($text,'(?m)^\s*(?:private|public|internal)\s+(?:static\s+)?(?:void|bool)\s+'+[regex]::Escape($name)+'\(')
 if (!$m.Success) {if($Optional){return ''};throw "Method missing: $name"}
 $start=$text.IndexOf('{',$m.Index);$level=0
 for($i=$start;$i -lt $text.Length;$i++){
  if($text[$i] -eq '{'){$level++}
  if($text[$i] -eq '}'){$level--;if($level -eq 0){return $text.Substring($m.Index,$i-$m.Index+1)}}
 }
 throw "Unbalanced method: $name"
}
function Read-Source($path){[IO.File]::ReadAllText((Join-Path $SourceRoot $path))}
$taunt=Read-Source 'src\modules\AF.Module.Taunt\Host\SceneTauntBehavior.cs'
$gccz=Read-Source 'src\AF.GameAdapter.Bannerlord\Composition\SiegeAftermath\SiegeAiInterventionBehavior.cs'
$move=Read-Source 'src\AF.GameAdapter.Bannerlord\Composition\SiegeAftermath\SiegeAiInterventionBehavior.NativeMovementAdapter.cs'
$sets=Read-Source 'src\AF.GameAdapter.Bannerlord\SettlementEntry\SettlementEntryTroopSelectionBehavior.cs'
$policy=Read-Source 'AnimusForge.SiegeAftermathIntervention\SiegeLocalCivilianReactionProfile.cs'
$react=Method $taunt 'TryFleeOwnedSettlementPassiveVictim' -Optional
if($react){$react=$react.Replace('TryFleeOwnedSettlementPassiveVictim','React')}
else{$react=(Method $taunt 'TryHoldOwnedSettlementPassiveVictimInHandsUpPose').Replace('TryHoldOwnedSettlementPassiveVictimInHandsUpPose','React')}
$hold=Method $taunt 'TryHoldOwnedSettlementPassiveVictimInHandsUpPose'
$taunt=$taunt.Replace('TryHoldOwnedSettlementPassiveVictimInHandsUpPose','React').Replace('TryFleeOwnedSettlementPassiveVictim','React').Replace('ShouldRefreshOwnedSettlementPassiveHandsUpPose','ShouldRefreshOwnedSettlementPassiveFlee').Replace('_ownedSettlementPassiveHandsUpPoseRefreshTimer','_ownedSettlementPassiveFleeRefreshTimer').Replace('OwnedSettlementPassiveHandsUpPoseRefreshInterval','OwnedSettlementPassiveFleeRefreshInterval')
$refreshConstant=[regex]::Match($taunt,'private const float OwnedSettlementPassiveFleeRefreshInterval = [^;]+;').Value
# Name adaptation only; the selected tick condition and method algorithm are unchanged.
$sets=$sets.Replace('MaintainArmedCoupCivilianPanic','MaintainConflictCivilianPanic').Replace('_nextArmedCoupCivilianPanicTime','_nextConflictCivilianPanicTime').Replace('_armedCoupCivilianRetreatTimes','_conflictCivilianRetreatTimes').Replace('base.Mission','Mission')
$tick=[regex]::Match($sets,'if \([^\r\n]+>= _nextConflictCivilianPanicTime\)[\s\S]*?MaintainConflictCivilianPanic\(\);\s*\}').Value
if(!$tick){throw 'Conflict panic tick gate missing'}
$fixture=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Fixture.cs.template'))
$fixture=$fixture.Replace('/*HOLD*/',$hold).Replace('/*REACT*/',$react).Replace('/*KEEP*/',(Method $gccz 'KeepLocalConflictCivilianFleeing')).Replace('/*ASSIGN*/',(Method $move 'TryAssignNativeMovementFrame')).Replace('/*RESOLVE*/',(Method $move 'TryResolveLocalFleeWorldPosition' -Optional)).Replace('/*PANIC*/',(Method $sets 'MaintainConflictCivilianPanic')).Replace('/*TICK*/',$tick).Replace('/*REFRESH*/',(Method $policy 'ShouldRefreshLocalFleeOrder')).Replace('/*REACHED*/',(Method $policy 'IsLocalFleeTargetReached'))
$fixture=$fixture.Replace('/*MAINTAIN*/',(Method $taunt 'TryMaintainOwnedSettlementPassiveAttackVictims')).Replace('/*TAUNT_REFRESH*/',(Method $taunt 'ShouldRefreshOwnedSettlementPassiveFlee')).Replace('/*REFRESH_CONST*/',$refreshConstant)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'replay.generated.cs'),$fixture)
Add-Type -TypeDefinition $fixture -IgnoreWarnings
$results=[ReplayEntry]::Run()
$results | Set-Content (Join-Path $OutputDirectory 'results.txt') -Encoding utf8
$results
Write-Output ("Assertions={0}; Failures={1}; Engine=FAKE; LiveGame=NOT_RUN" -f $results.Count,[Checks]::Failures)
if([Checks]::Failures -ne 0){exit 1}
