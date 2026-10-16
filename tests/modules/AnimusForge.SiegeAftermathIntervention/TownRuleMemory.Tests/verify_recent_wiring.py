"""Focused source contracts complement executable core/host replays; no game/provider claim."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[4]
ADAPTER = ROOT / 'src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath'
def read(path): return path.read_text(encoding='utf-8-sig')
checks = 0
def check(ok, name):
    global checks
    assert ok, name
    checks += 1
    print('PASS', name)

host = read(ADAPTER/'SiegeAiInterventionBehavior.cs')
rule = read(ADAPTER/'GcczTownRuleMemoryRuntimeBridge.cs')
recent = read(ADAPTER/'GcczTownRecentMemoryBridge.cs')
check('GcczTownRecentMemoryBridge.SyncData(dataStore);' in host and 'GcczTownRecentMemoryBridge.Reset();' in host, 'existing campaign owner saves and resets recent memory')
check(rule.count('GcczTownRecentMemoryBridge.Build(settlement,') == 3, 'dedicated dialogue, ordinary dialogue and encyclopedia all project recent records')
local = rule[rule.index('internal static string BuildLocalDialoguePromptContext'):rule.index('internal static string BuildEncyclopediaText')]
check(local.index('GcczTownRuleMemorySpeakerResolver.IsEligible') < local.index('GcczTownRecentMemoryBridge.Build'), 'existing local speaker eligibility still gates new context')
shared = read(ROOT/'src/bridges/Siege/Host/AfGcczShoutBridge.cs')
check('GcczTownRuleMemoryRuntimeBridge.BuildLocalDialoguePromptContext(targetHero, targetCharacter, targetAgentIndex)' in shared, 'shared conversation bridge still owns local injection')
check('MaximumRulerMemories = 3' in read(ROOT/'AnimusForge.SiegeAftermathIntervention/SettlementRuleMemoryStore.cs'), 'recent memory does not change three-tenure limit')
core = read(ROOT/'AnimusForge.SiegeAftermathIntervention/TownRecentMemoryStore.cs')
check('VillageDispositionActionKind' not in recent + core and 'VillageDispositionActionCatalog' not in recent + core, 'AF receipt seam has no GCCZ village implementation dependency')
check('string.IsNullOrWhiteSpace(receipt)' in recent and 'TownRecentMemoryStore.IsRecordedVillageAction(actionId)' in recent, 'future village integration requires a completed negative action receipt')

action = host[host.index('private static void RecordTownActionMemory('):host.index('private static void ShowOutcomeMessageOnce(')]
check('if (!repeat && key != TownActionPresentationKeys.CulturalRepopulation)' in action, 'town repeated acknowledgement and colonization request excluded')
colonization = host[host.index('private static bool ApplyCulturalRepopulationNow('):host.index('private static void ReplaceTownNotablesForCulturalRepopulation(')]
check(colonization.index('ActiveTownColonization.TryCommit()') < colonization.index('GcczTownRecentMemoryBridge.TownAction(settlement, "cultural_repopulation")'), 'colonization recorded only after durable commit')
check('Task.Run' not in recent and 'AIClient' not in recent and 'CampaignEvents.TickEvent.Add' not in recent, 'recent memory adds no frame polling, background game access or LLM calls')
check('private static readonly FieldInfo DecisionMaker' in recent and 'GameActionLogs' in recent, 'legacy actor access cached and history read at session launch')
resource_dir = ROOT/'content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData'
catalogs = [json.loads(read(resource_dir/f'GcczTownPrompt.{lang}.json'))['RecentMemoryTexts'] for lang in ['zh-CN','en','en-US']]
check(set(catalogs[0]) == set(catalogs[1]) == set(catalogs[2]), 'all shipping CN/EN resource variants have identical keys')
check(catalogs[1] == catalogs[2], 'English compatibility variant has identical semantics')
for path in ['AnimusForge.csproj','AnimusForge.SiegeAftermathIntervention/AnimusForge.SiegeAftermathIntervention.csproj']:
    project = read(ROOT/path)
    check('GcczTownPrompt.zh-CN.json' in project and 'GcczTownPrompt.en.json' in project, 'both catalogs embedded: '+path)
check('GcczTownPrompt' in read(ROOT/'content/content-map.json'), 'existing package content map distributes the changed catalogs')
print('RESULT', checks, 'recent-memory wiring checks passed')
