"""Bind the complete current live adapter to approved leaf-capability substitutions.

This is not a refreshed historical algorithm hash. Any unlisted statement/order
change fails, and executable effect-order assertions separately cover the move.
"""
from pathlib import Path
import importlib.util,re,subprocess
R=Path(__file__).resolve().parents[4]
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
path='src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PromptContextCapture.cs'
prior=subprocess.check_output(['git','show','cb15e847:'+path],cwd=R).decode('utf-8-sig').replace('\r\n','\n')
old=ex.declaration(prior,'private void CapturePromptSections(')
expected=old.replace('private void CapturePromptSections(','internal static void CapturePromptSections(PromptContextCaptureBannerlordPorts ports, ',1).replace('Stopwatch promptContextTotalSw, Stopwatch promptContextStageSw,\n\t\t','')
expected=expected.replace('IsPartyTransferRuleEligible(targetHero, targetCharacter, targetAgentIndex)','ports.IsPartyTransferEligible()').replace('HasDuelRuntimeTarget(targetHero, targetCharacter, targetAgentIndex)','ports.HasDuelRuntimeTarget()')
expected=re.sub(r'LogShoutPromptContextStage\(("[^"]+"), promptContextTotalSw, promptContextStageSw, targetHero, targetCharacter, targetAgentIndex(?:, (.*?))?\);',lambda m:'ports.LogStage('+m[1]+', '+(m[2] or 'null')+', true);' if 'immediate:' not in (m[2] or '') else 'ports.LogStage('+m[1]+', '+m[2].replace('immediate: false','false')+');',expected)
replacements={
 '_recentlyDefeatedByPlayer.Contains(targetHero.StringId)':'ports.WasRecentlyDefeated(targetHero.StringId)',
 '_recentlyReleasedPrisoners.Contains(targetHero.StringId)':'ports.WasRecentlyReleased(targetHero.StringId)',
 'BuildPlayerPublicDisplayNameForPrompt(targetHero)':'ports.BuildPlayerDisplayName(targetHero, null, -1)',
 'BuildPlayerPublicDisplayNameForPrompt(entityContextHero, targetCharacter, targetAgentIndex)':'ports.BuildPlayerDisplayName(entityContextHero, targetCharacter, targetAgentIndex)',
 'BuildHeroPrisonerStatusPromptLineForExternal(targetHero)':'ports.BuildPrisonerStatus(targetHero)',
 'BuildHeroArmyRuntimeFactForPrompt(targetHero)':'ports.BuildHeroArmyFact()',
 'BuildPlayerArmyRuntimeFactForPrompt(targetHero, targetCharacter, targetAgentIndex)':'ports.BuildPlayerArmyFact()',
 'BuildResidentRecentActionsPrompt(targetHero, targetCharacter, targetAgentIndex)':'ports.BuildResidentRecentActions()',
 'IsWorldBulletinPublishingEnabled()':'ports.IsWorldBulletinEnabled()',
 'CaptureWorldBulletinNpcSnapshot(targetHero, targetCharacter, kingdomIdOverride)':'ports.CaptureWorldBulletinSnapshot()',
 'ShouldExcludeNpcShortReportFromWeeklyShortLayer(value8, targetHero, targetCharacter, kingdomIdOverride, weeklyPromptSnapshot)':'ports.ShouldExcludeWeeklyShortReport(value8, weeklyPromptSnapshot)',
 'BuildWeeklyShortReportsPromptBlock(targetHero, targetCharacter, kingdomIdOverride, excludeNpcShortReport2, weeklyPromptSnapshot)':'ports.BuildWeeklyShortReports(excludeNpcShortReport2, weeklyPromptSnapshot)',
 'BuildTriggeredWeeklyFullReportsPromptBlock(value8, targetHero, targetCharacter, kingdomIdOverride, weeklyPromptSnapshot)':'ports.BuildWeeklyFullReports(value8, weeklyPromptSnapshot)',
 'DoesPlayerNotorietyObserverKnowPlayer(targetHero, targetCharacter, targetAgentIndex)':'ports.ObserverKnowsPlayer()'}
for before,after in replacements.items():
 assert before in expected,before
 expected=expected.replace(before,after)
rule_call=re.search(r'BuildTriggeredRuleInstructions\(input, targetHero,.*?retrieval\?\.FallbackExtraRuleHits\)',expected).group()
expected=expected.replace(rule_call,'ports.BuildTriggeredRules(contextFlags)')
adapter=(R/'src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
current=ex.declaration(adapter,'internal static void CapturePromptSections(')
assert current==expected,'unlisted complete-context algorithm/order delta'
# Verify actual host rule bindings, rather than merely trusting a detached fixture port.
host=(R/path).read_text(encoding='utf-8-sig')
factory=ex.declaration(host,'private PromptContextCaptureBannerlordPorts CreatePromptContextCapturePorts(')
call=re.search(r'PromptRuleCaptureBannerlordAdapter\.BuildTriggeredRuleInstructions\(NpcMajorRuleCapture, request.Input,.*?retrieval\?\.FallbackExtraRuleHits\)',factory,re.S).group()
expected_call=rule_call.replace('BuildTriggeredRuleInstructions(', 'PromptRuleCaptureBannerlordAdapter.BuildTriggeredRuleInstructions(NpcMajorRuleCapture, ', 1)
aliases={'input':'request.Input','flag2':'flags.UseDuelContext','isQualified':'request.IsQualified','flag7':'flags.UseRewardContext','flag8':'flags.IsLoanContext','flag5':'routing.Surroundings.Hit','hasAnyHero':'request.HasAnyHero','kingdomIdOverride':'request.KingdomIdOverride','targetAgentIndex':'request.TargetAgentIndex','npcLastUtterance':'request.NpcLastUtterance','includeDuelStakeContext':'flags.IncludeDuelStakeContext','playerWonLastDuelForRule':'flags.PlayerWonLastDuel','worldMapPartyCommandHit':'routing.WorldMapPartyCommand.Hit','auxiliaryRuleHitIds':'routing.AuxiliaryRuleHitIds'}
for name,value in aliases.items():expected_call=re.sub(r'\b'+name+r'\b',value,expected_call)
assert re.sub(r'\s+','',call)==re.sub(r'\s+','',expected_call),'host rule argument/flag binding drift'
ports=adapter[adapter.index('internal sealed class PromptContextCaptureBannerlordPorts'):]
assert 'MyBehavior' not in ports and 'CapturePromptSections' not in ports,'whole host/capture callback retained'
entry=ex.declaration(host,'private void CapturePromptSections(')
assert entry.count('PromptContextCaptureBannerlordAdapter.CapturePromptSections(')==1 and 'TryConsumeLastDuelResult' not in entry
print('PASS complete capture approved leaf substitutions exact; actual host rule args; no whole-host callback')
