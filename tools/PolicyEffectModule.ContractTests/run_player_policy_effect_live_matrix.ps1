[CmdletBinding()]
param(
    [string]$ApiUrl = 'https://yjapi.manqiaotechnology.com/v1',
    [string]$ModelName = 'gemini-3-flash',
    [ValidateRange(1, 12)]
    [int]$ThrottleLimit = 6,
    [ValidateRange(1, 12000)]
    [int]$MaxTokens = 5000,
    [ValidateRange(0.0, 2.0)]
    [double]$Temperature = 0.25,
    [ValidateRange(0, 200)]
    [int]$StartIndex = 0,
    [ValidateRange(0, 200)]
    [int]$EndIndex = 200,
    [string]$ApiKeyEnvironmentVariable = 'ANIMUSFORGE_POLICY_LIVE_PROBE_API_KEY',
    [switch]$UseClipboardApiKey,
    [switch]$AllModulesStress,
    [switch]$DeepPolicyStress,
    [switch]$SoldierTroopXpStress,
    [ValidateRange(1, 200)]
    [int]$BatchSize = 200,
    [ValidateRange(0, 300)]
    [int]$BatchDelaySeconds = 0,
    [switch]$SummaryOnly,
    [switch]$SyntaxOnly
)

$ErrorActionPreference = 'Stop'

$probePath = Join-Path $PSScriptRoot 'run_player_policy_effect_live_probe.ps1'
$contractExecutable = Join-Path $PSScriptRoot 'bin\Release\net472\PolicyEffectModule.ContractTests.exe'
$caseDefinitions = @(
    [pscustomobject]@{ Index = 0; Name = 'foreign_all_tax_transfer'; Scenario = 'foreign_all'; Count = 6; Category = 'broad-country'; Policy = '从其他王国控制的全部城镇和城堡征收领主税收，并把征得的收益交给发布者一方。' },
    [pscustomobject]@{ Index = 1; Name = 'exclude_player_clan_tax'; Scenario = 'exclude_player_clan'; Count = 5; Category = 'clan-exclusion'; Policy = '在本国全部城镇和城堡征税，但明确排除玩家家族所有的领地；收益交给发布者。' },
    [pscustomobject]@{ Index = 2; Name = 'other_places_precise_exclusion'; Scenario = 'other_places'; Count = 5; Category = 'object-exclusion'; Policy = '只从本国其他城镇和城堡征收领主税收，当前政策发布地必须排除，收益留在发布地。' },
    [pscustomobject]@{ Index = 3; Name = 'enemy_border_castles'; Scenario = 'enemy_border_castles'; Count = 4; Category = 'precise-country-object'; Policy = '只对所有敌国的边境城堡加征领主主税收，不影响城市和非边境城堡，收益交给发布者。' },
    [pscustomobject]@{ Index = 4; Name = 'ally_top2_towns'; Scenario = 'ally_top2'; Count = 6; Category = 'ranked-specific-target'; Policy = '只向盟国中繁荣度最高的两个城镇征收领主税收，其他城镇不受影响，收益交给发布者。' },
    [pscustomobject]@{ Index = 5; Name = 'clan_centralization'; Scenario = 'clan_centralization'; Count = 5; Category = 'multi-clan-linked'; Policy = '推行中央集权：目标王国除发布者家族外的其他家族，一次性和每日影响力都下降；发布者家族的一次性和每日影响力都上升。' },
    [pscustomobject]@{ Index = 6; Name = 'clan_great_deed'; Scenario = 'clan_great_deed'; Count = 1; Category = 'single-specific-clan'; Policy = '发布者完成了名震全国的伟业：只让发布者家族在下一游戏日一次性增加影响力，每日变化必须为零。' },
    [pscustomobject]@{ Index = 7; Name = 'region_owner_relations'; Scenario = 'clan_region_relations'; Count = 2; Category = 'derived-specific-clans'; Policy = '西境城镇和东境城堡获得成功赈济；政策通过后的下一游戏日，发布者与这两个地区当前所有者家族领袖的关系分别提高。' },
    [pscustomobject]@{ Index = 8; Name = 'kingdom_stability_crisis'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'single-specific-country'; Policy = '目标王国发生严重继承危机；政策通过后的下一游戏日，该王国稳定度一次性显著下降。' },
    [pscustomobject]@{ Index = 9; Name = 'direct_prosperity'; Scenario = 'effect_direct'; Count = 6; Category = 'independent-broad'; Policy = '向其他王国的全部城镇和城堡投入商队补贴，使其每日繁荣度显著上升；这是直接投入，不发生资源转移。' },
    [pscustomobject]@{ Index = 10; Name = 'food_flow'; Scenario = 'effect_flow'; Count = 6; Category = 'single-module-linked'; Policy = '从其他王国全部城镇和城堡持续调出粮食，并运到当前发布地：外国目标每日粮食下降，发布地每日粮食上升。' },
    [pscustomobject]@{ Index = 11; Name = 'food_and_loyalty_aid'; Scenario = 'effect_aid'; Count = 6; Category = 'multi-module-linked'; Policy = '当前发布地持续拿出粮食援助其他王国全部城镇和城堡，使受援地的每日粮食和忠诚度都上升。' },
    [pscustomobject]@{ Index = 12; Name = 'tax_acquire_local'; Scenario = 'effect_acquire_local'; Count = 6; Category = 'local-beneficiary'; Policy = '从其他王国的全部城镇和城堡获取领主税收，并把全部收益交给当前发布地。' },
    [pscustomobject]@{ Index = 13; Name = 'tax_acquire_kingdom'; Scenario = 'effect_acquire_kingdom'; Count = 6; Category = 'country-beneficiary'; Policy = '从其他王国的全部城镇和城堡获取领主税收，并把全部收益交给玩家王国。' },
    [pscustomobject]@{ Index = 14; Name = 'tax_for_construction_exchange'; Scenario = 'effect_exchange'; Count = 6; Category = 'cross-module-linked'; Policy = '当前发布地承担持续的领主税收成本，为其他王国全部城镇和城堡提供持续建设投入，使其每日建造力上升。' },
    [pscustomobject]@{ Index = 121; Name = 'target_hearth_low3_food'; Scenario = 'effect_target_hearth_low3'; Count = 3; Category = 'target-metric-hearth'; Policy = '只向本国户数最低的三个一级封地每日补充少量粮食，不改变户数本身。' },
    [pscustomobject]@{ Index = 122; Name = 'target_hearth_gte300_high2_loyalty'; Scenario = 'effect_target_hearth_gte300_high2'; Count = 2; Category = 'target-metric-hearth'; Policy = '在本国户数不低于300的一级封地中，只取户数最高的两处每日提升忠诚，不改变户数本身。' },
    [pscustomobject]@{ Index = 123; Name = 'target_hearth_lt300_all_growth'; Scenario = 'effect_target_hearth_lt300_all'; Count = 6; Category = 'target-metric-hearth'; Policy = '对本国户数低于300的全部一级封地附属村庄实施安置，使每个村庄每日户数小幅增加。' },
    [pscustomobject]@{ Index = 124; Name = 'target_militia_high2_construction'; Scenario = 'effect_target_militia_high2'; Count = 2; Category = 'target-metric-militia'; Policy = '只向本国民兵最高的两座城镇派遣工程队，使其每日建造力提高，不改变民兵。' },
    [pscustomobject]@{ Index = 125; Name = 'target_militia_low2_training'; Scenario = 'effect_target_militia_low2'; Count = 2; Category = 'target-metric-militia'; Policy = '只在本国民兵最低的两座城镇训练乡勇，使其每日民兵小幅增加。' },
    [pscustomobject]@{ Index = 126; Name = 'target_militia_gte200_asc2_security'; Scenario = 'effect_target_militia_gte200_asc2'; Count = 2; Category = 'target-metric-militia'; Policy = '在本国民兵至少200的城镇中，按民兵升序取前两座加强巡逻，使其每日治安提高，不改变民兵。' }
)

if ($AllModulesStress -or $DeepPolicyStress) {
    $caseDefinitions += @(
        [pscustomobject]@{ Index = 15; Name = 'food_direct_broad'; Scenario = 'effect_food_direct'; Count = 6; Category = 'food-direct'; Policy = '为其他王国全部城镇和城堡建设独立粮仓，使每处目标每日粮食储备持续增加；不从发布地调粮。' },
        [pscustomobject]@{ Index = 16; Name = 'loyalty_direct_broad'; Scenario = 'effect_loyalty_direct'; Count = 6; Category = 'loyalty-direct'; Policy = '在其他王国全部城镇和城堡推行公开赦免与自治保障，使每处目标每日忠诚度持续提高；不发生资源转移。' },
        [pscustomobject]@{ Index = 17; Name = 'hearth_growth_broad'; Scenario = 'effect_hearth'; Count = 6; Category = 'hearth-direct'; Policy = '在其他王国全部城镇和城堡覆盖的附属村庄推行移民安置与开垦，使每个附属村庄每日户数持续增加。' },
        [pscustomobject]@{ Index = 18; Name = 'security_patrol_broad'; Scenario = 'effect_security'; Count = 6; Category = 'security-direct'; Policy = '在其他王国全部城镇和城堡建立常态巡逻和反匪体系，使每座目标城镇每日治安持续提高。' },
        [pscustomobject]@{ Index = 19; Name = 'militia_training_broad'; Scenario = 'effect_militia'; Count = 6; Category = 'militia-direct'; Policy = '在其他王国全部城镇和城堡持续训练地方守备，使每座目标定居点每日民兵人数增加。' },
        [pscustomobject]@{ Index = 20; Name = 'construction_direct_broad'; Scenario = 'effect_construction_direct'; Count = 6; Category = 'construction-direct'; Policy = '为其他王国全部城镇和城堡提供独立工程队，使每座目标定居点每日建造力持续提高；发布地不承担机械税收变化。' },
        [pscustomobject]@{ Index = 21; Name = 'prosperity_single_small'; Scenario = 'effect_direct'; Count = 1; Category = 'prosperity-stress'; Policy = '只向一个外国城镇提供小规模商队便利，使该目标每日繁荣小幅提高；不发生资源转移。' },
        [pscustomobject]@{ Index = 22; Name = 'prosperity_twelve_large'; Scenario = 'effect_direct'; Count = 12; Category = 'prosperity-stress'; Policy = '向外国全部十二处城镇和城堡投入大规模商路补贴，使每处目标每日繁荣显著提高；不发生资源转移。' },
        [pscustomobject]@{ Index = 23; Name = 'prosperity_decline'; Scenario = 'effect_direct'; Count = 5; Category = 'prosperity-stress'; Policy = '封锁外国五处城镇和城堡的商路，使每处目标每日繁荣持续下降；发布地不获得机械收益。' },
        [pscustomobject]@{ Index = 24; Name = 'food_direct_single'; Scenario = 'effect_food_direct'; Count = 1; Category = 'food-stress'; Policy = '只为一个外国城镇建设小型独立粮仓，使该目标每日粮食储备小幅增加，不从发布地调粮。' },
        [pscustomobject]@{ Index = 25; Name = 'food_flow_single'; Scenario = 'effect_flow'; Count = 1; Category = 'food-stress'; Policy = '从一个外国城镇持续调出粮食运到当前发布地：外国目标每日粮食下降，发布地每日粮食上升。' },
        [pscustomobject]@{ Index = 26; Name = 'food_flow_twelve'; Scenario = 'effect_flow'; Count = 12; Category = 'food-stress'; Policy = '从外国十二处城镇和城堡持续调出粮食运到当前发布地：每处外国目标每日粮食下降，发布地每日粮食上升。' },
        [pscustomobject]@{ Index = 27; Name = 'aid_single_target'; Scenario = 'effect_aid'; Count = 1; Category = 'aid-stress'; Policy = '当前发布地持续拿出粮食援助一个外国城镇，使受援地每日粮食和忠诚度都上升。' },
        [pscustomobject]@{ Index = 28; Name = 'aid_twelve_targets'; Scenario = 'effect_aid'; Count = 12; Category = 'aid-stress'; Policy = '当前发布地持续拿出粮食援助外国十二处城镇和城堡，使每处受援地每日粮食和忠诚度都上升。' },
        [pscustomobject]@{ Index = 29; Name = 'aid_four_targets'; Scenario = 'effect_aid'; Count = 4; Category = 'aid-stress'; Policy = '当前发布地承担中等粮食援助，持续支援外国四处城镇和城堡，使受援地粮食和忠诚度同步提高。' },
        [pscustomobject]@{ Index = 30; Name = 'hearth_single'; Scenario = 'effect_hearth'; Count = 1; Category = 'hearth-stress'; Policy = '在一个外国城镇覆盖的附属村庄安置移民，使每个附属村庄每日户数小幅增加。' },
        [pscustomobject]@{ Index = 31; Name = 'hearth_twelve'; Scenario = 'effect_hearth'; Count = 12; Category = 'hearth-stress'; Policy = '在外国十二处城镇和城堡覆盖的附属村庄实施大规模开垦，使每个附属村庄每日户数持续增加。' },
        [pscustomobject]@{ Index = 32; Name = 'hearth_decline'; Scenario = 'effect_hearth'; Count = 6; Category = 'hearth-stress'; Policy = '在外国六处城镇和城堡覆盖的附属村庄实施沉重徭役，导致每个附属村庄每日户数持续下降。' },
        [pscustomobject]@{ Index = 33; Name = 'loyalty_single'; Scenario = 'effect_loyalty_direct'; Count = 1; Category = 'loyalty-stress'; Policy = '只在一个外国城镇实行赦免和自治保障，使该目标每日忠诚度小幅提高。' },
        [pscustomobject]@{ Index = 34; Name = 'loyalty_twelve'; Scenario = 'effect_loyalty_direct'; Count = 12; Category = 'loyalty-stress'; Policy = '在外国十二处城镇和城堡推行自治保障，使每处目标每日忠诚度持续提高。' },
        [pscustomobject]@{ Index = 35; Name = 'loyalty_decline'; Scenario = 'effect_loyalty_direct'; Count = 5; Category = 'loyalty-stress'; Policy = '在外国五处城镇和城堡实施公开羞辱与高压统治，使每处目标每日忠诚度持续下降。' },
        [pscustomobject]@{ Index = 36; Name = 'security_single'; Scenario = 'effect_security'; Count = 1; Category = 'security-stress'; Policy = '只在一个外国城镇增加常态巡逻，使该目标每日治安小幅提高。' },
        [pscustomobject]@{ Index = 37; Name = 'security_twelve'; Scenario = 'effect_security'; Count = 12; Category = 'security-stress'; Policy = '在外国十二处城镇和城堡部署强力反匪体系，使每座目标城镇每日治安显著提高。' },
        [pscustomobject]@{ Index = 38; Name = 'security_decline'; Scenario = 'effect_security'; Count = 5; Category = 'security-stress'; Policy = '撤销外国五处城镇的巡逻并纵容盗匪，使每座目标城镇每日治安持续下降。' },
        [pscustomobject]@{ Index = 39; Name = 'militia_single'; Scenario = 'effect_militia'; Count = 1; Category = 'militia-stress'; Policy = '只在一个外国定居点持续训练乡勇，使该目标每日民兵人数小幅增加。' },
        [pscustomobject]@{ Index = 40; Name = 'militia_twelve'; Scenario = 'effect_militia'; Count = 12; Category = 'militia-stress'; Policy = '在外国十二处城镇和城堡实施战争动员，使每座目标定居点每日民兵人数显著增加。' },
        [pscustomobject]@{ Index = 41; Name = 'militia_decline'; Scenario = 'effect_militia'; Count = 5; Category = 'militia-stress'; Policy = '裁撤外国五处城镇和城堡的地方武装，使每座目标定居点每日民兵人数持续下降。' },
        [pscustomobject]@{ Index = 42; Name = 'construction_single'; Scenario = 'effect_construction_direct'; Count = 1; Category = 'construction-stress'; Policy = '只向一个外国城镇派遣独立工程队，使该目标每日建造力小幅提高，不产生发布地税收变化。' },
        [pscustomobject]@{ Index = 43; Name = 'construction_twelve'; Scenario = 'effect_construction_direct'; Count = 12; Category = 'construction-stress'; Policy = '向外国十二处城镇和城堡派遣大型独立工程队，使每座目标每日建造力显著提高，不产生发布地税收变化。' },
        [pscustomobject]@{ Index = 44; Name = 'tax_transfer_single'; Scenario = 'foreign_all'; Count = 1; Category = 'tax-stress'; Policy = '从一个外国城镇征收领主税收，并把所得交给发布者一方。' },
        [pscustomobject]@{ Index = 45; Name = 'tax_transfer_twelve'; Scenario = 'foreign_all'; Count = 12; Category = 'tax-stress'; Policy = '从外国十二处城镇和城堡征收领主税收，并把所得交给发布者一方；每处目标使用独立百分比点。' },
        [pscustomobject]@{ Index = 46; Name = 'tax_acquire_kingdom_twelve'; Scenario = 'effect_acquire_kingdom'; Count = 12; Category = 'tax-stress'; Policy = '从外国十二处城镇和城堡获取领主税收，并把全部收益交给玩家王国；不得按十二处目标累计 payload。' },
        [pscustomobject]@{ Index = 47; Name = 'clan_centralization_ten'; Scenario = 'clan_centralization'; Count = 10; Category = 'clan-influence-stress'; Policy = '推行中央集权：目标王国十个其他家族的一次性和每日影响力下降，发布者家族的一次性和每日影响力上升。' },
        [pscustomobject]@{ Index = 48; Name = 'clan_great_deed_repeat'; Scenario = 'clan_great_deed'; Count = 1; Category = 'clan-influence-stress'; Policy = '发布者完成全国性伟业：只让发布者家族在下一游戏日一次性增加影响力，每日变化必须为零。' },
		[pscustomobject]@{ Index = 49; Name = 'region_owner_relation_penalty'; Scenario = 'clan_region_relations'; Count = 2; Category = 'relation-stress'; Policy = '西境城镇和东境城堡的当前所有者家族领袖公开反对发布者；下一游戏日，发布者与这两个领袖的关系分别下降。' },
        [pscustomobject]@{ Index = 50; Name = 'kingdom_stability_recovery'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'stability-stress'; Policy = '目标王国完成和平继承与权力交接；下一游戏日，该王国稳定度一次性显著提高。' }
    )
}

if ($DeepPolicyStress) {
    $caseDefinitions += @(
        [pscustomobject]@{ Index = 51; Name = 'foreign_tax_two_mild'; Scenario = 'foreign_all'; Count = 2; Category = 'selector-variation'; Policy = '向两个外国领地征收轻微领主税，并将收益交给发布者；每个领地独立使用相同百分比点。' },
        [pscustomobject]@{ Index = 52; Name = 'foreign_tax_twenty_strong'; Scenario = 'foreign_all'; Count = 20; Category = 'selector-variation'; Policy = '向外国二十处城镇和城堡征收重税，全部收益交给发布者；不得把二十处目标累计成单个百分比。' },
        [pscustomobject]@{ Index = 53; Name = 'domestic_tax_exclude_player_formal'; Scenario = 'exclude_player_clan'; Count = 8; Category = 'selector-variation'; Policy = '对本国八处非玩家家族领地统一征收领主税，玩家家族领地明确豁免，所得交由发布者。' },
        [pscustomobject]@{ Index = 54; Name = 'domestic_other_places_collective'; Scenario = 'other_places'; Count = 8; Category = 'selector-variation'; Policy = '当前发布地不纳税，只从本国其余八处城镇和城堡征收领主税，收入用于当前发布地。' },
        [pscustomobject]@{ Index = 55; Name = 'enemy_border_castles_ten'; Scenario = 'enemy_border_castles'; Count = 10; Category = 'selector-variation'; Policy = '仅对敌国十座边境城堡加征领主税；城市、内地城堡和非敌国领地均不受影响。' },
        [pscustomobject]@{ Index = 56; Name = 'ally_top2_twelve_pool'; Scenario = 'ally_top2'; Count = 12; Category = 'selector-variation'; Policy = '在十二个盟国候选城镇中只选择繁荣最高的两个征收领主税，其他候选不得受到效果。' },

        [pscustomobject]@{ Index = 57; Name = 'prosperity_two_small'; Scenario = 'effect_direct'; Count = 2; Category = 'prosperity-angle'; Policy = '为两个外国城镇提供小额独立商路补贴，使每个目标每日繁荣小幅提高，不产生任何发布方机械成本。' },
        [pscustomobject]@{ Index = 58; Name = 'prosperity_twenty_large'; Scenario = 'effect_direct'; Count = 20; Category = 'prosperity-angle'; Policy = '为外国二十处城镇和城堡提供大型独立商队网络，使每个目标每日繁荣显著提高，数值不得按目标数累计。' },
        [pscustomobject]@{ Index = 59; Name = 'prosperity_seven_decline'; Scenario = 'effect_direct'; Count = 7; Category = 'prosperity-angle'; Policy = '切断外国七处领地的商道，使每处目标每日繁荣中度下降；发布者不获得税收或其他机械收益。' },
        [pscustomobject]@{ Index = 60; Name = 'prosperity_ninety_days'; Scenario = 'effect_direct'; Count = 8; Category = 'duration-angle'; Policy = '政策持续九十天：改善外国八处领地的商路，使每处目标每日繁荣小幅提高；每日值不得乘九十。' },

        [pscustomobject]@{ Index = 61; Name = 'food_two_small'; Scenario = 'effect_food_direct'; Count = 2; Category = 'food-direct-angle'; Policy = '在两个外国城镇建设独立小粮仓，使每个目标每日粮食小幅增加，不从任何其他地方调粮。' },
        [pscustomobject]@{ Index = 62; Name = 'food_twenty_large'; Scenario = 'effect_food_direct'; Count = 20; Category = 'food-direct-angle'; Policy = '在外国二十处领地建设大型独立粮仓，使每处目标每日粮食显著增加，发布地不承担粮食或忠诚成本。' },
        [pscustomobject]@{ Index = 63; Name = 'food_seven_decline'; Scenario = 'effect_food_direct'; Count = 7; Category = 'food-direct-angle'; Policy = '焚毁外国七处领地的粮仓，使每处目标每日粮食持续下降；不得虚构发布者获得粮食。' },
        [pscustomobject]@{ Index = 64; Name = 'food_ninety_days'; Scenario = 'effect_food_direct'; Count = 8; Category = 'duration-angle'; Policy = '政策持续九十天：为外国八处领地提供独立粮仓维护，使每处目标每日粮食小幅增加，不能把每日值乘持续天数。' },

        [pscustomobject]@{ Index = 65; Name = 'loyalty_two_small'; Scenario = 'effect_loyalty_direct'; Count = 2; Category = 'loyalty-angle'; Policy = '给予两个外国城镇有限自治，使每个目标每日忠诚小幅提高；没有资源转移或发布者负面效果。' },
        [pscustomobject]@{ Index = 66; Name = 'loyalty_twenty_large'; Scenario = 'effect_loyalty_direct'; Count = 20; Category = 'loyalty-angle'; Policy = '在外国二十处领地实行广泛赦免，使每处目标每日忠诚显著提高，单点值不得累计。' },
        [pscustomobject]@{ Index = 67; Name = 'loyalty_seven_decline'; Scenario = 'effect_loyalty_direct'; Count = 7; Category = 'loyalty-angle'; Policy = '在外国七处领地实施公开羞辱，使每处目标每日忠诚中度下降；不影响发布地忠诚。' },
        [pscustomobject]@{ Index = 68; Name = 'loyalty_ninety_days'; Scenario = 'effect_loyalty_direct'; Count = 8; Category = 'duration-angle'; Policy = '九十天内保障外国八处领地自治，使每处目标每日忠诚小幅提高；payload 是每日单点值而非九十日合计。' },

        [pscustomobject]@{ Index = 69; Name = 'hearth_two_small'; Scenario = 'effect_hearth'; Count = 2; Category = 'hearth-angle'; Policy = '在两个外国领地的附属村庄进行小规模安置，使每个村庄每日户数小幅增加。' },
        [pscustomobject]@{ Index = 70; Name = 'hearth_twenty_large'; Scenario = 'effect_hearth'; Count = 20; Category = 'hearth-angle'; Policy = '在外国二十处领地覆盖的附属村庄大规模开垦，使每个村庄每日户数显著增加。' },
        [pscustomobject]@{ Index = 71; Name = 'hearth_seven_decline'; Scenario = 'effect_hearth'; Count = 7; Category = 'hearth-angle'; Policy = '在外国七处领地的附属村庄强征徭役，使每个村庄每日户数持续下降。' },
        [pscustomobject]@{ Index = 72; Name = 'hearth_ninety_days'; Scenario = 'effect_hearth'; Count = 8; Category = 'duration-angle'; Policy = '政策持续九十天：安置外国八处领地的附属村庄，每个村庄每日户数小幅增加，不得乘目标数或天数。' },

        [pscustomobject]@{ Index = 73; Name = 'security_two_small'; Scenario = 'effect_security'; Count = 2; Category = 'security-angle'; Policy = '在两个外国城镇增加少量巡逻，使每个目标每日治安小幅提高，不产生发布者治安变化。' },
        [pscustomobject]@{ Index = 74; Name = 'security_twenty_large'; Scenario = 'effect_security'; Count = 20; Category = 'security-angle'; Policy = '在外国二十处领地部署全面反匪力量，使每个有效城镇目标每日治安显著提高。' },
        [pscustomobject]@{ Index = 75; Name = 'security_seven_decline'; Scenario = 'effect_security'; Count = 7; Category = 'security-angle'; Policy = '撤销外国七个城镇的巡逻，使每个目标每日治安中度下降；发布方没有对应收益。' },
        [pscustomobject]@{ Index = 76; Name = 'security_ninety_days'; Scenario = 'effect_security'; Count = 8; Category = 'duration-angle'; Policy = '九十天内维持外国八处领地的巡逻，使每个有效城镇目标每日治安小幅提高；每日值不得累计九十次写入 payload。' },

        [pscustomobject]@{ Index = 77; Name = 'militia_two_small'; Scenario = 'effect_militia'; Count = 2; Category = 'militia-angle'; Policy = '在两个外国定居点小规模训练乡勇，使每个目标每日民兵人数小幅增加。' },
        [pscustomobject]@{ Index = 78; Name = 'militia_twenty_large'; Scenario = 'effect_militia'; Count = 20; Category = 'militia-angle'; Policy = '在外国二十处领地全面动员，使每个目标每日民兵人数显著增加，不能把总人数写成单点 payload。' },
        [pscustomobject]@{ Index = 79; Name = 'militia_seven_decline'; Scenario = 'effect_militia'; Count = 7; Category = 'militia-angle'; Policy = '裁撤外国七处领地的地方武装，使每个目标每日民兵人数中度下降。' },
        [pscustomobject]@{ Index = 80; Name = 'militia_ninety_days'; Scenario = 'effect_militia'; Count = 8; Category = 'duration-angle'; Policy = '政策持续九十天：在外国八处领地训练乡勇，使每个目标每日民兵小幅增加，payload 不得乘持续天数。' },

        [pscustomobject]@{ Index = 81; Name = 'construction_two_small'; Scenario = 'effect_construction_direct'; Count = 2; Category = 'construction-angle'; Policy = '向两个外国城镇派遣小型独立工程队，使每个目标每日建造力小幅提高，不产生发布地税收成本。' },
        [pscustomobject]@{ Index = 82; Name = 'construction_twenty_large'; Scenario = 'effect_construction_direct'; Count = 20; Category = 'construction-angle'; Policy = '向外国二十处领地派遣大型独立工程队，使每个目标每日建造力显著提高；没有机械资源来源腿。' },
        [pscustomobject]@{ Index = 83; Name = 'construction_seven_decline'; Scenario = 'effect_construction_direct'; Count = 7; Category = 'construction-angle'; Policy = '破坏外国七处领地的工地，使每个目标每日建造力中度下降；发布者不获得税收。' },
        [pscustomobject]@{ Index = 84; Name = 'construction_ninety_days'; Scenario = 'effect_construction_direct'; Count = 8; Category = 'duration-angle'; Policy = '九十天内支援外国八处领地建设，使每个目标每日建造力小幅提高；不得把九十日总量作为每日 payload。' },

        [pscustomobject]@{ Index = 85; Name = 'food_flow_two'; Scenario = 'effect_flow'; Count = 2; Category = 'flow-angle'; Policy = '从两个外国城镇持续调粮到当前发布地：两个来源各自每日粮食下降，发布地每日粮食上升，属于同一流转事件。' },
        [pscustomobject]@{ Index = 86; Name = 'food_flow_twenty'; Scenario = 'effect_flow'; Count = 20; Category = 'flow-angle'; Policy = '从外国二十处领地持续调粮到当前发布地：每个来源每日粮食下降，发布地每日粮食上升，不得按来源数放大单点值。' },
        [pscustomobject]@{ Index = 87; Name = 'food_flow_ninety_days'; Scenario = 'effect_flow'; Count = 8; Category = 'duration-angle'; Policy = '粮食调运持续九十天：外国八处来源每日粮食下降，当前发布地每日粮食上升；payload 只能表达每日单点值。' },
        [pscustomobject]@{ Index = 88; Name = 'food_flow_thirty_targets'; Scenario = 'effect_flow'; Count = 30; Category = 'payload-scale-angle'; Policy = '从外国三十处领地向当前发布地调粮；每处来源和发布地都使用独立每日值，禁止把三十处总量塞进单目标 payload。' },
        [pscustomobject]@{ Index = 89; Name = 'aid_two_collective'; Scenario = 'effect_aid'; Count = 2; Category = 'causal-grouping-angle'; Policy = '当前发布地拿出粮食共同援助两个外国城镇，使两个受援地每日粮食与忠诚都提高；所有直接腿属于同一援助事件。' },
        [pscustomobject]@{ Index = 90; Name = 'aid_twenty_collective'; Scenario = 'effect_aid'; Count = 20; Category = 'causal-grouping-angle'; Policy = '当前发布地持续拿出粮食援助外国二十处领地，使每个受援地每日粮食和忠诚提高；不得虚构发布地忠诚下降。' },
        [pscustomobject]@{ Index = 91; Name = 'aid_two_respectively'; Scenario = 'effect_aid'; Count = 2; Category = 'causal-grouping-angle'; Policy = '当前发布地提供同一批粮食，分别援助两个外国城镇；两个目标的粮食与忠诚同等提高，不能按目标拆成不同援助事件。' },
        [pscustomobject]@{ Index = 92; Name = 'aid_ninety_days'; Scenario = 'effect_aid'; Count = 8; Category = 'duration-angle'; Policy = '援助持续九十天：发布地每日付出粮食，外国八处受援地每日粮食和忠诚提高；每日 payload 不得乘九十。' },
        [pscustomobject]@{ Index = 93; Name = 'aid_no_fictional_penalty'; Scenario = 'effect_aid'; Count = 3; Category = 'causal-grouping-angle'; Policy = '发布地只付出粮食援助三个外国城镇，使受援地粮食与忠诚提高；正文没有发布地忠诚、安全或税收代价。' },
        [pscustomobject]@{ Index = 94; Name = 'aid_thirty_shared_source'; Scenario = 'effect_aid'; Count = 30; Category = 'payload-scale-angle'; Policy = '一个发布地粮食来源共同支援外国三十处领地，使每处受援地每日粮食和忠诚提高；一个真实成本可以支撑多个直接收益。' },

        [pscustomobject]@{ Index = 95; Name = 'tax_acquire_local_one'; Scenario = 'effect_acquire_local'; Count = 1; Category = 'tax-mapping-angle'; Policy = '从一个外国城镇获取领主税收并交给当前发布地，来源与受益方属于同一获取事件。' },
        [pscustomobject]@{ Index = 96; Name = 'tax_acquire_local_twenty'; Scenario = 'effect_acquire_local'; Count = 20; Category = 'tax-mapping-angle'; Policy = '从外国二十处领地获取领主税收并交给当前发布地；每个来源独立使用百分比点，不得累计。' },
        [pscustomobject]@{ Index = 97; Name = 'tax_acquire_local_ninety_days'; Scenario = 'effect_acquire_local'; Count = 8; Category = 'duration-angle'; Policy = '政策持续九十天：从外国八处领地获取领主税收并交给当前发布地；税率 payload 不得乘九十。' },
        [pscustomobject]@{ Index = 98; Name = 'tax_acquire_kingdom_one'; Scenario = 'effect_acquire_kingdom'; Count = 1; Category = 'tax-mapping-angle'; Policy = '从一个外国城镇获取领主税收并交给玩家王国，不能把王国描述改成当前发布地。' },
        [pscustomobject]@{ Index = 99; Name = 'tax_acquire_kingdom_twenty'; Scenario = 'effect_acquire_kingdom'; Count = 20; Category = 'tax-mapping-angle'; Policy = '从外国二十处领地获取领主税收并交给玩家王国；不得按目标数放大来源或王国受益 payload。' },
        [pscustomobject]@{ Index = 100; Name = 'tax_acquire_kingdom_ninety_days'; Scenario = 'effect_acquire_kingdom'; Count = 8; Category = 'duration-angle'; Policy = '九十天内从外国八处领地获取领主税收并交给玩家王国；payload 是独立百分比点，不是期间总收益。' },
        [pscustomobject]@{ Index = 101; Name = 'tax_construction_exchange_one'; Scenario = 'effect_exchange'; Count = 1; Category = 'exchange-angle'; Policy = '当前发布地承担领主税收成本，为一个外国城镇提供建设投入，使该目标每日建造力提高；这是一个交换事件。' },
        [pscustomobject]@{ Index = 102; Name = 'tax_construction_exchange_twenty'; Scenario = 'effect_exchange'; Count = 20; Category = 'exchange-angle'; Policy = '当前发布地承担税收成本，为外国二十处领地提供建设投入，使每个目标每日建造力提高；所有直接腿保持同一事件。' },
        [pscustomobject]@{ Index = 103; Name = 'tax_construction_exchange_ninety_days'; Scenario = 'effect_exchange'; Count = 8; Category = 'duration-angle'; Policy = '交换持续九十天：发布地承担每日税收成本，外国八处领地获得每日建造力；两种 payload 都不得乘目标数或天数。' },
        [pscustomobject]@{ Index = 104; Name = 'tax_transfer_thirty'; Scenario = 'foreign_all'; Count = 30; Category = 'payload-scale-angle'; Policy = '从外国三十处领地征收领主税并交给发布者；每处目标使用单独百分比点，绝不能把三十份合计写入单点 payload。' },

        [pscustomobject]@{ Index = 105; Name = 'centralization_two_clans'; Scenario = 'clan_centralization'; Count = 2; Category = 'clan-influence-angle'; Policy = '对目标王国两个其他家族推行中央集权：它们的一次性与每日影响力下降，发布者家族对应上升。' },
        [pscustomobject]@{ Index = 106; Name = 'centralization_twenty_clans'; Scenario = 'clan_centralization'; Count = 20; Category = 'clan-influence-angle'; Policy = '对目标王国二十个其他家族推行中央集权：每个家族的一次性与每日影响力下降，发布者家族对应上升。' },
        [pscustomobject]@{ Index = 107; Name = 'centralization_collective_wording'; Scenario = 'clan_centralization'; Count = 8; Category = 'clan-influence-angle'; Policy = '目标王国其余八个家族共同失去政治影响力，发布者家族获得权力；一次性变化和每日变化都必须明确。' },
        [pscustomobject]@{ Index = 108; Name = 'great_deed_small'; Scenario = 'clan_great_deed'; Count = 1; Category = 'clan-influence-angle'; Policy = '发布者完成一项值得嘉奖的小型公共工程，只在下一游戏日为发布者家族增加少量一次性影响力，每日变化为零。' },
        [pscustomobject]@{ Index = 109; Name = 'great_deed_large'; Scenario = 'clan_great_deed'; Count = 1; Category = 'clan-influence-angle'; Policy = '发布者赢得决定王国命运的伟大胜利，只在下一游戏日为发布者家族增加大量一次性影响力，每日变化为零。' },

        [pscustomobject]@{ Index = 110; Name = 'relation_collective_positive'; Scenario = 'clan_region_relations'; Count = 2; Category = 'collective-target-angle'; Policy = '西境城镇与东境城堡共同得到赈济，发布者与这两个地区当前所有者家族领袖的关系在下一日同等提高。' },
        [pscustomobject]@{ Index = 111; Name = 'relation_respectively_positive'; Scenario = 'clan_region_relations'; Count = 2; Category = 'collective-target-angle'; Policy = '西境城镇和东境城堡获得同等援助；发布者与两位当前所有者家族领袖的关系分别提高相同幅度。' },
        [pscustomobject]@{ Index = 112; Name = 'relation_each_positive'; Scenario = 'clan_region_relations'; Count = 2; Category = 'collective-target-angle'; Policy = '发布者同时帮助西境城镇和东境城堡，因此下一日与各自当前所有者家族领袖的关系统一增加。' },
        [pscustomobject]@{ Index = 113; Name = 'relation_collective_negative'; Scenario = 'clan_region_relations'; Count = 2; Category = 'collective-target-angle'; Policy = '发布者同时损害西境城镇与东境城堡，下一日与两个地区当前所有者家族领袖的关系同等下降。' },
        [pscustomobject]@{ Index = 114; Name = 'relation_respectively_negative'; Scenario = 'clan_region_relations'; Count = 2; Category = 'collective-target-angle'; Policy = '西境城镇和东境城堡遭到同等征用，发布者与两位当前所有者家族领袖的关系分别下降相同幅度。' },
        [pscustomobject]@{ Index = 115; Name = 'relation_same_reason_positive'; Scenario = 'clan_region_relations'; Count = 2; Category = 'collective-target-angle'; Policy = '同一项赈济同时惠及西境城镇和东境城堡，使发布者与两地当前所有者家族领袖统一改善关系。' },

        [pscustomobject]@{ Index = 116; Name = 'stability_mild_decline'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'stability-angle'; Policy = '目标王国出现轻微继承争议；下一游戏日王国稳定度一次性小幅下降。' },
        [pscustomobject]@{ Index = 117; Name = 'stability_severe_decline'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'stability-angle'; Policy = '目标王国爆发灾难性的王位内战；下一游戏日王国稳定度一次性大幅下降。' },
        [pscustomobject]@{ Index = 118; Name = 'stability_mild_recovery'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'stability-angle'; Policy = '目标王国达成有限政治和解；下一游戏日王国稳定度一次性小幅提高。' },
        [pscustomobject]@{ Index = 119; Name = 'stability_strong_recovery'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'stability-angle'; Policy = '目标王国完成广泛和平和解与合法继承；下一游戏日王国稳定度一次性显著提高。' },
        [pscustomobject]@{ Index = 120; Name = 'stability_once_during_ninety_days'; Scenario = 'kingdom_stability_crisis'; Count = 1; Category = 'duration-angle'; Policy = '该政策持续九十天，但目标王国只在下一游戏日获得一次性中等稳定度提升，不得把一次性值乘九十。' }
    )
}

if ($SoldierTroopXpStress) {
    if ($AllModulesStress -or $DeepPolicyStress) {
        throw 'SoldierTroopXpStress cannot be combined with AllModulesStress or DeepPolicyStress.'
    }
    $caseDefinitions = @(
        [pscustomobject]@{ Index = 127; Name = 'soldier_xp_explicit_once'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-once'; Policy = '对目标家族全部正式领主队伍和城镇、城堡驻军的普通士兵进行一次集中整编；下一游戏日每名合格士兵获得400点兵种经验，此后没有每日训练收益。'; SoldierXpExpectation = 'once'; ExpectedOnceMinimum = 400; ExpectedOnceMaximum = 400; ExpectedDailyMinimum = 0; ExpectedDailyMaximum = 0 },
        [pscustomobject]@{ Index = 128; Name = 'soldier_xp_explicit_daily'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-daily'; Policy = '为目标家族全部正式领主队伍和城镇、城堡驻军建立常设每日训练制度；政策有效期间每天每名合格士兵获得20点兵种经验，不含一次集中整编。'; SoldierXpExpectation = 'daily'; ExpectedOnceMinimum = 0; ExpectedOnceMaximum = 0; ExpectedDailyMinimum = 20; ExpectedDailyMaximum = 20 },
        [pscustomobject]@{ Index = 129; Name = 'soldier_xp_explicit_two_phase'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-both'; Policy = '先对目标家族领主队伍与驻军集中整编，下一游戏日每名获得350点兵种经验；随后在政策有效期内持续每日操练，每天每名再获得15点兵种经验。'; SoldierXpExpectation = 'both'; ExpectedOnceMinimum = 350; ExpectedOnceMaximum = 350; ExpectedDailyMinimum = 15; ExpectedDailyMaximum = 15 },
        [pscustomobject]@{ Index = 130; Name = 'soldier_xp_vague_defaults_once'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-vague'; Policy = '提高目标家族部队与驻军普通士兵的精锐程度。'; SoldierXpExpectation = 'once'; ExpectedOnceMinimum = 1; ExpectedOnceMaximum = 5000; ExpectedDailyMinimum = 0; ExpectedDailyMaximum = 0 },
        [pscustomobject]@{ Index = 131; Name = 'soldier_xp_vague_with_budget'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-funding'; Policy = '拨款二十万第纳尔，用于提高目标家族全部领主队伍和驻军普通士兵的精锐程度。'; SoldierXpExpectation = 'once'; ExpectedOnceMinimum = 1; ExpectedOnceMaximum = 5000; ExpectedDailyMinimum = 0; ExpectedDailyMaximum = 0 },
        [pscustomobject]@{ Index = 132; Name = 'soldier_xp_daily_sixty'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-daily'; Policy = '为目标家族全部正式领主队伍与驻军建立极强常设训练制度；政策有效期间每天每名合格士兵精确获得60点兵种经验，不含一次性整编。'; SoldierXpExpectation = 'daily'; ExpectedOnceMinimum = 0; ExpectedOnceMaximum = 0; ExpectedDailyMinimum = 60; ExpectedDailyMaximum = 60 },
        [pscustomobject]@{ Index = 133; Name = 'soldier_xp_clan_parties_and_garrisons'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-coverage'; Policy = '对目标家族族长、普通领主、同伴领主和玩家主部队所带普通士兵，以及该家族城镇和城堡驻军普通士兵，一并集中训练；下一游戏日每名获得300点兵种经验。'; SoldierXpExpectation = 'once'; ExpectedOnceMinimum = 300; ExpectedOnceMaximum = 300; ExpectedDailyMinimum = 0; ExpectedDailyMaximum = 0 },
        [pscustomobject]@{ Index = 134; Name = 'soldier_xp_militia_recruit_prisoner_excluded'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-excluded'; Policy = '只增加城镇民兵、直接招募新兵并训练俘虏；明确不训练家族领主队伍或驻军中的普通在编士兵。'; SoldierXpExpectation = 'none'; ExpectedOnceMinimum = 0; ExpectedOnceMaximum = 0; ExpectedDailyMinimum = 0; ExpectedDailyMaximum = 0 },
        [pscustomobject]@{ Index = 135; Name = 'soldier_xp_explicit_none'; Scenario = 'soldier_troop_xp'; Count = 1; Category = 'soldier-xp-none'; Policy = '这是一项纯宣传政策，明确不向任何家族领主队伍或驻军士兵提供兵种经验，也不建立训练制度。'; SoldierXpExpectation = 'none'; ExpectedOnceMinimum = 0; ExpectedOnceMaximum = 0; ExpectedDailyMinimum = 0; ExpectedDailyMaximum = 0 }
    )
}

if ($StartIndex -gt $EndIndex) {
    throw 'StartIndex cannot be greater than EndIndex.'
}
$caseDefinitions = @($caseDefinitions | Where-Object { $_.Index -ge $StartIndex -and $_.Index -le $EndIndex })

if (-not (Test-Path -LiteralPath $probePath -PathType Leaf)) {
    throw "Probe script not found: $probePath"
}
if (-not (Test-Path -LiteralPath $contractExecutable -PathType Leaf)) {
    throw "Contract executable not found: $contractExecutable"
}
if ($ApiKeyEnvironmentVariable -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw 'API key environment-variable name is invalid.'
}
if (@($caseDefinitions.Name | Sort-Object -Unique).Count -ne $caseDefinitions.Count) {
    throw 'Matrix case names must be unique.'
}

if ($SyntaxOnly) {
    [ordered]@{
        status = 'SYNTAX_OK'
        case_count = $caseDefinitions.Count
        maximum_api_requests = $caseDefinitions.Count * 2
        batch_size = $BatchSize
        batch_delay_seconds = $BatchDelaySeconds
        retry_count = 0
    } | ConvertTo-Json
    return
}

$pwshPath = Join-Path $PSHOME 'pwsh.exe'
if (-not (Test-Path -LiteralPath $pwshPath -PathType Leaf)) {
    throw "PowerShell 7 executable not found: $pwshPath"
}

$configuredKey = [Environment]::GetEnvironmentVariable($ApiKeyEnvironmentVariable, [EnvironmentVariableTarget]::Process)
if ([string]::IsNullOrWhiteSpace($configuredKey)) {
    if ($UseClipboardApiKey) {
        $clipboardText = [string](Get-Clipboard -Raw)
        $keyMatches = @([regex]::Matches($clipboardText, '(?<![A-Za-z0-9])sk-[A-Za-z0-9]{20,}(?![A-Za-z0-9])'))
        if ($keyMatches.Count -ne 1) {
            throw 'Clipboard must contain exactly one plausible API key; no API requests were sent.'
        }
        $configuredKey = $keyMatches[0].Value
        $clipboardText = $null
    }
    else {
        $secureKey = Read-Host 'API key (hidden; process memory only)' -AsSecureString
        $keyPointer = [IntPtr]::Zero
        try {
            $keyPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)
            $configuredKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($keyPointer)
        }
        finally {
            if ($keyPointer -ne [IntPtr]::Zero) {
                [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($keyPointer)
            }
            $secureKey.Dispose()
        }
    }
    if ([string]::IsNullOrWhiteSpace($configuredKey)) {
        throw 'API key cannot be empty; no API requests were sent.'
    }
    [Environment]::SetEnvironmentVariable(
        $ApiKeyEnvironmentVariable,
        $configuredKey,
        [EnvironmentVariableTarget]::Process)
}
$configuredKey = $null

$results = @()
$batchCount = [int][Math]::Ceiling($caseDefinitions.Count / [double]$BatchSize)
for ($batchOffset = 0; $batchOffset -lt $caseDefinitions.Count; $batchOffset += $BatchSize) {
    $batch = @($caseDefinitions | Select-Object -Skip $batchOffset -First $BatchSize)
    $batchResults = @($batch | ForEach-Object -Parallel {
        $caseDefinition = $_
        $arguments = @(
            '-NoLogo',
            '-NoProfile',
            '-File', $using:probePath,
            '-Scenario', $caseDefinition.Scenario,
            '-PositiveTargetCount', [string]$caseDefinition.Count,
            '-PolicyText', $caseDefinition.Policy,
            '-ContractExecutable', $using:contractExecutable,
            '-ApiUrl', $using:ApiUrl,
            '-ModelName', $using:ModelName,
            '-ApiKeyEnvironmentVariable', $using:ApiKeyEnvironmentVariable,
            '-MaxTokens', [string]$using:MaxTokens,
            '-Temperature', [string]$using:Temperature
        )
        if ($null -ne $caseDefinition.PSObject.Properties['SoldierXpExpectation']) {
            $arguments += @(
                '-SoldierXpExpectation', [string]$caseDefinition.SoldierXpExpectation,
                '-ExpectedOnceMinimum', [string]$caseDefinition.ExpectedOnceMinimum,
                '-ExpectedOnceMaximum', [string]$caseDefinition.ExpectedOnceMaximum,
                '-ExpectedDailyMinimum', [string]$caseDefinition.ExpectedDailyMinimum,
                '-ExpectedDailyMaximum', [string]$caseDefinition.ExpectedDailyMaximum
            )
        }

        $output = & $using:pwshPath @arguments 2>&1
        $exitCode = $LASTEXITCODE
        $parsed = $null
        $runnerError = ''
        $runnerDiagnosticPrefix = ''
        $outputText = ($output | Out-String).Trim()
        try {
            $parsed = ($outputText | ConvertFrom-Json)
        }
        catch {
            $runnerError = 'Probe process did not return parseable sanitized JSON (' + $_.Exception.GetType().Name + ').'
            $runnerDiagnosticPrefix = ($outputText -replace '(?<![A-Za-z0-9])sk-[A-Za-z0-9]{20,}(?![A-Za-z0-9])', '[REDACTED]')
            if ($runnerDiagnosticPrefix.Length -gt 240) {
                $runnerDiagnosticPrefix = $runnerDiagnosticPrefix.Substring(0, 240)
            }
        }

        [pscustomobject]@{
            index = [int]$caseDefinition.Index
            name = [string]$caseDefinition.Name
            category = [string]$caseDefinition.Category
            scenario = [string]$caseDefinition.Scenario
            soldier_xp_expectation = $(if ($null -eq $caseDefinition.PSObject.Properties['SoldierXpExpectation']) { '' } else { [string]$caseDefinition.SoldierXpExpectation })
            requested_target_count = [int]$caseDefinition.Count
            process_exit_code = [int]$exitCode
            runner_error = $runnerError
            runner_output_chars = $outputText.Length
            runner_diagnostic_prefix = $runnerDiagnosticPrefix
            probe = $parsed
        }
    } -ThrottleLimit $ThrottleLimit)
    $results += $batchResults
    if (($batchOffset + $BatchSize) -lt $caseDefinitions.Count -and $BatchDelaySeconds -gt 0) {
        Start-Sleep -Seconds $BatchDelaySeconds
    }
}

$results = @($results | Sort-Object index)
$passedCount = @($results | Where-Object { $null -ne $_.probe -and [bool]$_.probe.passed }).Count
$requestCount = ($results | ForEach-Object { if ($null -ne $_.probe) { [int]$_.probe.api.request_count } else { 0 } } | Measure-Object -Sum).Sum
$retryCount = ($results | ForEach-Object { if ($null -ne $_.probe) { [int]$_.probe.api.retry_count } else { 0 } } | Measure-Object -Sum).Sum
$mainHttpPassed = @($results | Where-Object { $null -ne $_.probe -and [bool]$_.probe.main.success }).Count
$mainJsonPassed = @($results | Where-Object { $null -ne $_.probe -and [bool]$_.probe.main_json_parsed }).Count
$effectHttpPassed = @($results | Where-Object { $null -ne $_.probe -and [bool]$_.probe.effect.success }).Count
$effectJsonPassed = @($results | Where-Object { $null -ne $_.probe -and [bool]$_.probe.effect_json_parsed }).Count
$mappingContractPassed = @($results | Where-Object {
    $null -ne $_.probe -and [bool]$_.probe.mapping_contract_validation.success
}).Count
$offlineCompilerPassed = @($results | Where-Object {
    $null -ne $_.probe -and [bool]$_.probe.offline_synthetic_compiler_validation.success
}).Count
$offlineCompilerExpected = @($results | Where-Object { [string]$_.soldier_xp_expectation -ne 'none' }).Count
$campaignPassed = @($results | Where-Object {
    $null -ne $_.probe -and [string]$_.probe.campaign_production_validation.status -eq 'passed'
}).Count
$campaignNotEvaluable = @($results | Where-Object {
    $null -ne $_.probe -and [string]$_.probe.campaign_production_validation.status -eq 'not_evaluable_no_campaign'
}).Count
$campaignFailed = @($results | Where-Object {
    $null -ne $_.probe -and [string]$_.probe.campaign_production_validation.status -eq 'failed'
}).Count
$usageTotals = [ordered]@{
    main_prompt_tokens = [long]0
    main_completion_tokens = [long]0
    main_total_tokens = [long]0
    effect_prompt_tokens = [long]0
    effect_completion_tokens = [long]0
    effect_total_tokens = [long]0
    combined_total_tokens = [long]0
}
foreach ($result in $results) {
    if ($null -eq $result.probe) { continue }
    $usageTotals.main_prompt_tokens += [long]$result.probe.main.usage.prompt_tokens
    $usageTotals.main_completion_tokens += [long]$result.probe.main.usage.completion_tokens
    $usageTotals.main_total_tokens += [long]$result.probe.main.usage.total_tokens
    $usageTotals.effect_prompt_tokens += [long]$result.probe.effect.usage.prompt_tokens
    $usageTotals.effect_completion_tokens += [long]$result.probe.effect.usage.completion_tokens
    $usageTotals.effect_total_tokens += [long]$result.probe.effect.usage.total_tokens
}
$usageTotals.combined_total_tokens = $usageTotals.main_total_tokens + $usageTotals.effect_total_tokens
$categoryCoverage = @($results |
    Group-Object category |
    Sort-Object Name |
    ForEach-Object {
        $categoryResults = @($_.Group)
        [ordered]@{
            category = [string]$_.Name
            case_count = $categoryResults.Count
            passed = @($categoryResults | Where-Object { $null -ne $_.probe -and [bool]$_.probe.passed }).Count
            main_http_passed = @($categoryResults | Where-Object { $null -ne $_.probe -and [bool]$_.probe.main.success }).Count
            effect_http_passed = @($categoryResults | Where-Object { $null -ne $_.probe -and [bool]$_.probe.effect.success }).Count
            compiler_passed = @($categoryResults | Where-Object { $null -ne $_.probe -and [bool]$_.probe.offline_synthetic_compiler_validation.success }).Count
        }
    })
$expectedPromptVisibleModules = @(
    'prosperityPerDay',
    'foodPerDay',
    'hearthPerDay',
    'loyaltyPerDay',
    'securityPerDay',
    'militiaPerDay',
    'taxIncomePct',
    'constructionPerDay',
    'clanInfluence',
    'clanLeaderRelationOnce',
    'kingdomStability'
)
$coveredModules = @($results | ForEach-Object {
    if ($null -ne $_.probe -and $null -ne $_.probe.mapping_contract_validation) {
        @($_.probe.mapping_contract_validation.module_ids)
    }
} | Sort-Object -Unique)
$missingModules = @($expectedPromptVisibleModules | Where-Object { $_ -notin $coveredModules })
$moduleCoverage = @($expectedPromptVisibleModules | ForEach-Object {
    $moduleId = $_
    $moduleCases = @($results | Where-Object {
        $null -ne $_.probe -and $moduleId -in @($_.probe.mapping_contract_validation.module_ids)
    })
    [ordered]@{
        module_id = $moduleId
        case_count = $moduleCases.Count
        passed_case_count = @($moduleCases | Where-Object { [bool]$_.probe.passed }).Count
    }
})
$failureSummary = @($results | Where-Object {
    $null -eq $_.probe -or -not [bool]$_.probe.passed
} | ForEach-Object {
    $item = $_
    $probe = $item.probe
    [ordered]@{
        index = [int]$item.index
        name = [string]$item.name
        scenario = [string]$item.scenario
        requested_target_count = [int]$item.requested_target_count
        process_exit_code = [int]$item.process_exit_code
        runner_error = [string]$item.runner_error
        runner_output_chars = [int]$item.runner_output_chars
        runner_diagnostic_prefix = [string]$item.runner_diagnostic_prefix
        request_count = $(if ($null -eq $probe) { 0 } else { [int]$probe.api.request_count })
        main_success = $(if ($null -eq $probe) { $false } else { [bool]$probe.main.success })
        main_http_status = $(if ($null -eq $probe) { 0 } else { [int]$probe.main.http_status })
        main_error = $(if ($null -eq $probe) { '' } else { [string]$probe.main.error })
        main_json_parsed = $(if ($null -eq $probe) { $false } else { [bool]$probe.main_json_parsed })
        main_semantic_intent_count = $(if ($null -eq $probe) { 0 } else { [int]$probe.main_semantic_ledger.intent_count })
        main_semantic_leg_count = $(if ($null -eq $probe) { 0 } else { [int]$probe.main_semantic_ledger.leg_count })
        main_semantic_shape = $(if ($null -eq $probe) { @() } else { @($probe.main_semantic_ledger.shape) })
        effect_success = $(if ($null -eq $probe) { $false } else { [bool]$probe.effect.success })
        effect_http_status = $(if ($null -eq $probe) { 0 } else { [int]$probe.effect.http_status })
        effect_error = $(if ($null -eq $probe) { '' } else { [string]$probe.effect.error })
        effect_json_parsed = $(if ($null -eq $probe) { $false } else { [bool]$probe.effect_json_parsed })
        aid_grouping_valid = $(if ($null -eq $probe) { $false } else { [bool]$probe.main_semantic_ledger.aid_three_leg_grouping_valid })
        tax_payload_per_target_valid = $(if ($null -eq $probe) { $false } else { [bool]$probe.tax_payload_per_canonical_target_valid })
        target_plan_mapping_valid = $(if ($null -eq $probe) { $false } else { [bool]$probe.target_plan_mapping_valid })
        mapping_success = $(if ($null -eq $probe) { $false } else { [bool]$probe.mapping_contract_validation.success })
        mapping_error = $(if ($null -eq $probe) { '' } else { [string]$probe.mapping_contract_validation.error })
        raw_mapping_shape = $(if ($null -eq $probe) { @() } else { @($probe.raw_mapping_shape) })
        offline_compiler_success = $(if ($null -eq $probe) { $false } else { [bool]$probe.offline_synthetic_compiler_validation.success })
        offline_compiler_error = $(if ($null -eq $probe) { '' } else { [string]$probe.offline_synthetic_compiler_validation.error })
        campaign_status = $(if ($null -eq $probe) { '' } else { [string]$probe.campaign_production_validation.status })
        probe_error = $(if ($null -eq $probe) { '' } else { [string]$probe.error })
    }
})
$matrixPassed = (
    $passedCount -eq $caseDefinitions.Count -and
    [int]$requestCount -eq ($caseDefinitions.Count * 2) -and
    [int]$retryCount -eq 0 -and
    $mappingContractPassed -eq $caseDefinitions.Count -and
    $offlineCompilerPassed -eq $offlineCompilerExpected -and
    $campaignFailed -eq 0 -and
    (-not ($AllModulesStress -or $DeepPolicyStress) -or $missingModules.Count -eq 0)
)
$apiHost = try { ([Uri]$ApiUrl).Host } catch { 'invalid' }
$reportedCases = @($results)
if ($SummaryOnly) {
    $reportedCases = [object[]]::new(0)
}

$report = [ordered]@{
    schema_version = 3
    executed_utc = [DateTime]::UtcNow.ToString('o')
    profile = $(if ($SoldierTroopXpStress) { 'soldier_troop_xp_stress' } elseif ($DeepPolicyStress) { 'deep_policy_stress' } elseif ($AllModulesStress) { 'all_modules_stress' } else { 'standard' })
    api = [ordered]@{
        host = $apiHost
        model = $ModelName
        case_count = $caseDefinitions.Count
        throttle_limit = $ThrottleLimit
        batch_size = $BatchSize
        batch_delay_seconds = $BatchDelaySeconds
        batch_count = $batchCount
        maximum_request_count = $caseDefinitions.Count * 2
        actual_request_count = [int]$requestCount
        retry_count = [int]$retryCount
        response_format_sent = $false
        thinking_controls_sent = $false
        api_key_recorded = $false
        prompt_recorded = $false
        response_content_recorded = $false
        usage = $usageTotals
    }
    module_coverage = [ordered]@{
        expected_count = $expectedPromptVisibleModules.Count
        covered_count = $coveredModules.Count
        covered_module_ids = $coveredModules
        missing_module_ids = $missingModules
        modules = $moduleCoverage
    }
    category_coverage = $categoryCoverage
    totals = [ordered]@{
        passed = $passedCount
        failed = $caseDefinitions.Count - $passedCount
        main_http_passed = $mainHttpPassed
        main_json_passed = $mainJsonPassed
        effect_http_passed = $effectHttpPassed
        effect_json_passed = $effectJsonPassed
        mapping_contract_passed = $mappingContractPassed
        offline_synthetic_compiler_passed = $offlineCompilerPassed
        campaign_production_passed = $campaignPassed
        campaign_production_not_evaluable_no_campaign = $campaignNotEvaluable
        campaign_production_failed = $campaignFailed
    }
    routing_covered = $false
    automatic_semantic_repair = $false
    success_case_details_omitted = [bool]$SummaryOnly
    failures = $failureSummary
    cases = $reportedCases
    passed = $matrixPassed
}

$report | ConvertTo-Json -Depth 30
if (-not $matrixPassed) { exit 2 }
