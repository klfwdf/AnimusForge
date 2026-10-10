param([string]$ProjectRoot = '')
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrEmpty($ProjectRoot)) { $ProjectRoot = Join-Path $PSScriptRoot '../../..' }
$root = [IO.Path]::GetFullPath($ProjectRoot)
$source = [IO.File]::ReadAllText((Join-Path $root 'src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.DisplayLocalization.cs'))
$rules = @{}
foreach ($row in [regex]::Matches($source, 'new WorldMapDisplayRule\("([^"]+)", @"([^"]+)", "([^"]*)"\)')) {
    $rules[$row.Groups[1].Value] = New-Object regex $row.Groups[2].Value
}
if ($rules.Count -ne 55) { throw "Expected 55 prepared rules, got $($rules.Count)" }
# These inputs follow real producers, including optional prisoners, multiple-digit days, and appended ship state.
$cases = @(
    @('HoldUntilCompleted', 'Aldric已经完成在Pravend停留至指定期限的命令。', 'SETTLEMENT', 'Pravend'),
    @('HoldDaysCompleted', 'Aldric已经完成在Pravend停留15天的命令。', 'DAYS', '15'),
    @('PatrolCompleted', 'Aldric已经完成在Pravend附近巡逻21天的命令。', 'DAYS', '21'),
    @('FollowCompleted', 'Aldric已经完成跟随Caladog28天的命令。', 'DAYS', '28'),
    @('PartyCreated', 'Aldric已经创建同伴部队，并接收了42名士兵。', 'PRISONERS', ''),
    @('PartyCreated', 'Aldric已经创建同伴部队，并接收了42名士兵、12名俘虏。', 'PRISONERS', '12'),
    @('QueueSafeExit', 'Aldric的大地图命令队列已经结束；当前原版战斗结算后将安全退出攻城并回归原版行动状态。', 'ACTOR', 'Aldric'),
    @('TimeoutTravel', 'Aldric未能在时限内抵达Pravend，已跳过当前前往命令。', 'SETTLEMENT', 'Pravend'),
    @('TimeoutPatrol', 'Aldric未能在时限内抵达Pravend附近，已跳过当前巡逻命令。', 'SETTLEMENT', 'Pravend'),
    @('TimeoutFollow', 'Aldric未能在时限内追上Caladog，已跳过当前跟随命令。', 'TARGET', 'Caladog'),
    @('ArmyMaintained', 'Aldric消耗20影响力强行维持军团，继续围攻Pravend（军团长影响力现为80）。', 'INFLUENCE', '80'),
    @('GovernorReturned', 'Aldric的远征队已返抵Pravend，普通兵、俘虏与物资已经完成安置，临时部队已回收。3艘随队船只已转交该定居点。该人物已恢复原总督职务。', 'SHIPS', '3艘随队船只已转交该定居点。'),
    @('GovernorReturned', 'Aldric的远征队已返抵Pravend，普通兵、俘虏与物资已经完成安置，临时部队已回收。未驱逐现任总督。', 'RESTORATION', '未驱逐现任总督。'),
    @('SettlementCaptured', 'Pravend已经被攻下。', 'SETTLEMENT', 'Pravend'),
    @('PartyDefeated', 'Caladog的部队被击败。', 'PARTY', 'Caladog'),
    @('HideoutCleared', '原版战斗获胜，驻扎土匪已被清除。', '', ''),
    @('HideoutDefeated', '原版战斗判定清剿部队战败。', '', ''),
    @('HideoutMissingResult', '原版战斗已结束，但没有可确认的清剿结算；未重复发动攻击。', '', ''),
    @('GovernorTransferFailure', '总督远征队兵员转移失败 troop=imperial_recruit step=2', 'TROOP', 'imperial_recruit')
)
foreach ($case in $cases) {
    $match = $rules[$case[0]].Match($case[1])
    if (-not $match.Success) { throw "Producer does not match: $($case[0])" }
    if ($case[2] -and $match.Groups[$case[2]].Value -cne $case[3]) {
        throw "Incorrect variable split: $($case[0])/$($case[2])"
    }
}
foreach ($input in @('Aldric对Caladog的攻击成功：Caladog的部队被击败。', 'Aldric围攻Pravend成功：Pravend已经被攻下。')) {
    foreach ($name in @('PartyDefeated', 'PartyRepelled', 'SettlementCaptured')) {
        if ($rules[$name].IsMatch($input)) { throw 'Detail rule swallowed the enclosing command result.' }
    }
}
$main = [IO.File]::ReadAllText((Join-Path $root 'src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.cs'))
$casualtyRule = [regex]::Match($main, 'Match casualty = Regex.Match\(d, @"([^"]+)"\)').Groups[1].Value
$battle = [regex]::Match('原版战斗获胜，驻扎土匪已被清除。 战斗损失：己方12人，敌方41人。', $casualtyRule)
if (-not $battle.Success -or $battle.Groups['actor'].Value -ne '12' -or $battle.Groups['target'].Value -ne '41') { throw 'Casualty detail split failed.' }
if (-not $rules['HideoutCleared'].IsMatch($battle.Groups['detail'].Value)) { throw 'Hideout detail plus casualties cannot be projected.' }
foreach ($unknown in @('Caladog said hello.', 'Aldric未知自由文本。')) {
    foreach ($rule in $rules.Values) { if ($rule.IsMatch($unknown)) { throw 'Unrecognized fact was unexpectedly classified.' } }
}
Write-Output "PASS WorldMap display producer fixtures=$($cases.Count), hideout casualties, unknown text preservation; source-derived regex checks only."
