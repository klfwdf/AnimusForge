"""Pure-Python wiring/resource contracts for the camp's nine field reports.
Does not compile or execute C#, simulate native AI/collision, or contact providers.
Run the C# continuation harness and both API builds separately when authorized.
"""
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
CAMP = 'src/modules/AF.Module.FieldCamp/'
SCENE = 'src/modules/AF.Module.Conversation/Channels/Scene/'
REX = 'Vengeance/Source/Scene/'
checks = 0


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


def check(value, name):
    global checks
    assert value, name
    checks += 1
    print('PASS ' + name)


def xml_strings(path):
    nodes = list(ET.fromstring(read(path)).iter('string'))
    values = {n.attrib['id']: n.attrib['text'] for n in nodes}
    check(len(nodes) == len(values), path + ' has unique keys')
    return values


launcher = read(CAMP + 'Host/FieldCampMissionLauncher.cs')
life = read(CAMP + 'Host/FieldCampMissionLogic.Life.cs')
main = read(CAMP + 'Host/FieldCampMissionLogic.cs')
envoys = read(CAMP + 'Host/FieldCampMissionLogic.Envoys.cs')
people = read(CAMP + 'Host/FieldCampMissionLogic.People.cs')
props = read(CAMP + 'Host/FieldCampProps.cs')
site = read(CAMP + 'Host/FieldCampSite.cs')
layout = read(CAMP + 'FieldCampLayout.cs')
execution = read(CAMP + 'Host/FieldCampMissionLogic.Execution.cs')
continuation = read(REX + 'TownExecutionMissionBehavior.Continuation.cs')
town_scene = read(REX + 'TownExecutionMissionBehavior.Scene.cs')
town = read(REX + 'TownExecutionMissionBehavior.cs')
ambient = read(SCENE + 'ShoutBehavior.SceneAmbient.cs')
field_chat = read(SCENE + 'ShoutBehavior.FieldCamp.cs')
shout = read(SCENE + 'ShoutBehavior.cs')
audio = read(SCENE + 'ShoutBehavior.ExecutionSpeech.cs')
director = read(REX + 'ExecutionSpeechDirector.cs')
playback = read(REX + 'Speech/ExecutionSpeechPlayback.cs')
bridge = read('src/bridges/Vengeance/Host/VengeanceRuntimeBridge.cs')

check('new CampaignMissionComponent()' in launcher and 'new FieldCampConversationLogic()' in launcher,
      'camp uses native campaign/conversation lifecycle')
check('using SandBox.View;' in launcher, 'native conversation view factory namespace imported')
native_views = [read('原版游戏本体代码1.3.x/SandBox.View/Missions/SandBoxMissionViews.cs'),
                read('原版游戏本体代码1.4.5/SandBox.View/SandBox/View/Missions/SandBoxMissionViews.cs')]
check('SandBoxMissionViews.OpenTownCenterMission(mission)' in launcher and all(
      all(x in version.split('[ViewMethod("FacialAnimationTest")]', 1)[0] for x in
          ('new MissionConversationCameraView()', 'CreateMissionConversationView(mission)',
           'CreateMissionAgentStatusUIHandler(mission)', 'CreateMissionBarterView()', 'CreateMissionLeaveView()'))
      for version in native_views), 'native town factory supplies conversation HUD barter and manual leave in both versions')
check('Mission.SetMissionMode(MissionMode.StartUp, atStart: true)' in main, 'camp starts in peaceful F-enabled mode')
check('CanStartNativeConversation(otherAgent)' in launcher and 'camp.IsCampPrisoner(otherAgent)' in launcher,
      'native F restricted to camp registry, including mortal hostile captives')
check('NotifyFieldCampMissionStartedForExternal' not in main + field_chat and
      'NotifyFieldCampMissionEndedForExternal' not in main + field_chat, 'manual duplicate campaign events removed')
check('ReferenceEquals(_sceneLifecycleMission, mission)' in shout, 'Shout lifecycle is idempotent by mission identity')
check('ConversationBegin += OnCampConversationBegin' in main and 'ConversationBegin -= OnCampConversationBegin' in main,
      'native camp conversation hooks unsubscribe')
check('IsSceneConversationHeld(agent)' in life.split('private void UpkeepBatchOfAgents()', 1)[1], 'daily upkeep respects conversation ownership')
check('TryAcquireSceneConversation(initiator)' in ambient and 'ReleaseSceneConversation(session.Initiator)' in ambient,
      'ambient borrows and returns camp ownership')
check('TryPrepareCampNavigation(agent)' in life and 'AddBehaviorGroup<DailyBehaviorGroup>()' in life and
      'component.AgentNavigator ?? component.CreateAgentNavigator()' in life,
      'camp movement reuses resident native navigator and attaches daily behavior only on demand')
check('ScriptBehavior.AddAgentTarget(envoy.Agent, Agent.Main)' in envoys and 'private void WalkEnvoy()' not in envoys,
      'envoy follows the player Agent, not repeated coordinate guesses')
check('FieldCampBehavior.Instance?.MarkEnvoyPresented(issue);' in envoys and '_envoyVisits' not in envoys,
      'visits counted by presentation callback in the persistent camp owner, not departure')
check('MarkEnvoyPresented(openingEnvoy, openingIssue)' in envoys and 'shout._ambientSession.OnPresented = onPresented' in field_chat,
      'actual presentation callback reaches envoy accounting')
check('CancelFieldCampEnvoyForExternal(entry.Agent)' in envoys and '!_envoyQueue.Contains(index)' in envoys,
      'envoy cancellation is scoped and pending issues are not duplicated')
check('AmbientStage.Opening' in ambient and 'AmbientStage.Presenting' in ambient and 'TickAmbientOpeningPresentation' in ambient,
      'ambient opening generation and presentation precede reply window')
check('ownerCancellationToken: session.OpeningLifetime.Token' in ambient and 'canStillPublish: () => CanPublishAmbientOpening(session)' in ambient,
      'late opening publication is owner-cancelled and revalidated')
check('new int[Math.Min(12, picked.Count)]' in life and 'if (d <= 144f)' in life and 'DistanceSquared(entry.Home.AsVec2) > 144f' in life,
      'stroll targets bounded near home with finite candidates')
check('TryBuildGroundedRoute' in life and 'stayInsideCamp: true' in life and 'RecoverBlockedCampWalk(entry);' in life and
      'MoveDeadline' in main and 'entry.MoveBlocked = blocked;' in life,
      'blocked daily walkers use camp-bounded routes and the recovery owner')
check('GetHeightAtPoint' not in life and 'TryGetCachedTerrainHeight' in life, 'camp height no longer selects prop tops')
check('TryResolveGroundedPosition(Mission.Scene, desired, 1.5f' in people, 'horses require cached body clearance')
check('ConditionalWeakTable<Scene, GroundCache>' in site and 'MaxGroundCells' in site and 'MaxPassageSteps' in site,
      'ground sampling bounded and scoped to scene cache')
check(main.count('FieldCampSite.InitializeGroundCache(') == 1 and 'FieldCampSite.InitializeGroundCache(' not in props and
      main.index('FieldCampProps.Place(') < main.index('FieldCampSite.InitializeGroundCache('),
      'ground cache initialized once after props, not twice')
check('FieldCampSite.ReleaseCaches(Mission.Scene)' in main and 'Mission.EndMission()' not in main and
      'RunCampSetup("whole_camp_site"' in main and 'RunCampSetup("player"' in main,
      'unsafe camp setup preserves manual leave and mission releases geometry cache')
check('TentAnchor' in layout and 'PlaceTentFurniture' in props and 'DoorIsClear' in props,
      'tent furniture attached to verified tent frame and entrance')
check('TeleportToPosition' not in execution and 'TeleportFlat' not in execution,
      'ceremony spectators and clearance never teleport')
check('TickCeremonyPreparation();' in main and main.index('TickCeremonyPreparation();') < main.index('if (!_ceremonyActive)'),
      'preparation upkeep runs while ceremony owns camp')
check('FieldCampCeremonyReadiness.Pending' in execution and 'FieldCampCeremonyReadiness.Ready' in execution and
      'FailCeremonyPreparation' in execution, 'camp readiness has pending/ready/failure consumers')
check('TryPrepareFieldCampCeremony()' in town_scene and town_scene.index('TryPrepareFieldCampCeremony()') < town_scene.index('BuildRuntimeScene('),
      'runtime scene creation is behind preparation gate')
check('FieldCampCeremonyBorrowedCrowd?.Invoke(Request.SessionId)' in town_scene and '_borrowedCrowd.Add(agent)' in town_scene,
      'ready camp spectators enter borrowed speech registry')
check('GetPreparedSpeechCrowd()' in town and 'AddressOverride?.Invoke(' in town and '_borrowedCrowd' in town,
      'local and streamed speech share borrowed spectator input')
check('ForgetCampPrisonerForCeremony(agent)' in execution and execution.index('ForgetCampPrisonerForCeremony(agent)') < execution.index('agent.FadeOut'),
      'hidden condemned copy relinquishes captive casualty owner before fade')
check('TryCloseFieldCampContinuation(choiceIdentity);' in continuation and
      re.search(r'_ =>\s*\{[^}]*TryCloseFieldCampContinuation\(choiceIdentity\);\s*\}', continuation),
      'inquiry cancel closes only its own choice identity')
check('ReferenceEquals(element, leaveElement)' in continuation and 'selected.Count != 1' in continuation,
      'only explicit original Leave element ends camp ceremony')
check('ReferenceEquals(_continuationChoiceIdentity, choiceIdentity)' in continuation,
      'stale inquiry callback cannot close replacement')
check(continuation.index('_service.Begin(nextRequest)') < continuation.index('FinalizeSession();'),
      'next Begin acceptance precedes removal of old continuation entry')
check('OnCampPrisonerScoreHit(' in main and 'OnCampPrisonerRemoved(' in main and 'CreateCampPrisonerStateDecider()' in launcher,
      'real hit, removal and native death decider consumers wired')
check('MortalityState.Invulnerable' not in people and 'MortalityState.Mortal' in people,
      'registered captives stay mortal after restore')
check('MarkCampaignCasualtyHandledExternally("field_camp_captive_owner")' in people and 'hero.AddDeathMark(killer, KillCharacterAction.KillCharacterActionDetail.Murdered)' in people and
      'KillCharacterAction.ApplyByDeathMarkForced(hero, true)' in people and 'ApplyByExecution(hero, killer' not in people,
      'camp owns actual lethal casualty once without vanilla execution side effects')
check('hero.MakeWounded()' in people and 'HasFreshCampPrisonerFatalHit' in people and 'affected.IsFadingOut()' in people,
      'wound, real death and visual removal are distinguished')
check('MaintainCampPrisonerCombatIsolation();' in main and 'CleanupCampPrisonerCombatIsolation();' in main,
      'prisoner combat isolation has upkeep and teardown consumers')
check('AudioForShownLine = ShoutBehavior.TryPlayExecutionSpeechForExternal' in bridge and
      director.index('ExecutionSpeechBubbleBridge.TryShow') < director.index('AudioForShownLine?.Invoke'),
      'execution audio dispatch follows successful single bubble publication')
check('LineShown.Invoke' in director and 'ISpeechPlaybackAudioSink' in playback,
      'speech memory hook preserved with optional audio receipt')
check('MaxLineAudioExtensionSeconds = 12f' in playback and 'MaxPhaseAudioExtensionSeconds = 30f' in playback,
      'audio can never block ceremony indefinitely')
check('SpeakAsync(text, -1, -1f, -1, voice' in audio and 'TrackTtsPlaybackRequest(request)' in audio,
      'execution reuses configured detached TTS with request ownership')
check('IsCurrentOnMainThread()' in audio and 'SaveRuntimeGuard.IsCurrentGeneration' in audio and 'BeforePlayback' in audio,
      'asynchronous audio has main-thread identity acceptance')
check('Interlocked.CompareExchange(ref _executionLineAudio, null, this)' in audio and
      '_audio.RetireTtsPlaybackRequest(_request)' in audio and 'ResetExecutionSpeechAudioForExternal();' in shout,
      'terminal/mission end release only owned execution audio')

ui_en = xml_strings('content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml')
ui_cn = xml_strings('content/modules/AF.Module.Conversation/ModuleData/Languages/CNs/afconv_ui_strings-zh-CN.xml')
rex_en = xml_strings('AnimusForge/ModuleData/Languages/vengeance_strings-en.xml')
rex_cn = xml_strings('AnimusForge/ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml')
check(ui_en['AF_CONV_UI_Ambient_InPlace'] == '{NAME} would like to talk to you.' and
      ui_cn['AF_CONV_UI_Ambient_InPlace'] == '{NAME}想和你说话。', 'in-place notification has registered CN/EN values')
check(rex_en['REX_FieldCamp_Continue_Cancel'] == 'Cancel' and rex_cn['REX_FieldCamp_Continue_Cancel'] == '取消',
      'camp cancellation has separate bilingual key')
check(rex_en['REX_Continue_Cancel'] == 'Leave' and rex_cn['REX_Continue_Cancel'] == '离开',
      'town shared Leave button meaning unchanged')
prompt = 'content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimePrompts'
cn = json.loads(read(prompt + '.json'))['AF_CONV_Ambient_OpenPlayer']
en = json.loads(read(prompt + '.en.json'))['AF_CONV_Ambient_OpenPlayer']
check('走上前' not in cn and 'step over' not in en, 'opening prompt no longer invents physical approach')
check(set(re.findall(r'\{\w+\}', cn)) == set(re.findall(r'\{\w+\}', en)), 'opening CN/EN placeholders match')
attention = read('src/AF.GameAdapter.Bannerlord/Scene/SceneAttentionController.cs')
identity = read('src/AF.GameAdapter.Bannerlord/Prompt/SceneAgentIdentityPromptCaptureAdapter.cs')
check('FieldCampMissionLogic.Current?.IsCeremonyAgentHeld(agent) == true' in attention,
      'attention checks exact camp ceremony movement owner')
for method in ['HoldAgentInPlaceUnlessSeated', 'ReleaseAgentFromSceneConversationLocks',
               'RestoreAgentAutonomy', 'UpdateStaringBehavior', 'ResetStaringBehavior']:
    body = attention.split(('private static void ' if method == 'HoldAgentInPlaceUnlessSeated' else 'internal void ') + method, 1)[1]
    check('IsCampCeremonyMovementOwned(' in body.split('\n\tinternal ', 1)[0], method + ' preserves ceremony movement')
walk = execution.split('private void IssueCeremonyWalk(', 1)[1].split('private void TickCeremonyPreparation(', 1)[0]
check(walk.index('SetMaximumSpeedLimit(-1f') < walk.index('SetScriptedPosition(ref world'),
      'ceremony clears old conversation speed freeze before issuing walk')
check('request.VenueName?.ToString()' in identity and 'request.IsFieldCamp' in identity and
      '"scene_execution_role_camp_crowd"' in identity, 'camp name and soldier role reach real prompt consumer')
role_cn = json.loads(read('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.zh-CN.json'))['scene_execution_role_camp_crowd']
role_en = json.loads(read('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.en.json'))['scene_execution_role_camp_crowd']
check('士兵' in role_cn and 'soldier' in role_en and
      set(re.findall(r'\{\w+\}', role_cn)) == set(re.findall(r'\{\w+\}', role_en)),
      'camp spectator role has matching CN/EN semantics and variables')
# Companion issue lifecycle: source/resource contracts, not a C# or savegame replay.
issues = read(CAMP + 'FieldCampIssues.cs')
issue_owner = read(CAMP + 'Host/FieldCampBehavior.Issues.cs')
behavior = read(CAMP + 'Host/FieldCampBehavior.cs')
orders = read(CAMP + 'FieldCampOrderTags.cs')
chat = read(SCENE + 'ShoutBehavior.SceneAmbientChat.cs')
chains = read(SCENE + 'ShoutBehavior.SceneConversationChains.cs')
native = read('src/modules/AF.Module.Conversation/Channels/Native/NativeConversationGameEffectsRuntime.cs')
queue = read('src/AF.GameAdapter.Bannerlord/Scene/SceneSpeechOutputQueueController.cs')
messages = read('src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs')
playback = shout.split('internal sealed class SceneSpeechPlaybackInfo', 1)[1].split('internal sealed class PendingSceneDialogueFeedEntry', 1)[0]
check('TraitsClash' not in issues and 'DefaultTraits' not in issues,
      'traits alone no longer manufacture companion feuds')
check('relation <= FeudRelation && (!handledRelation.HasValue || relation <= handledRelation.Value - FeudRenewalRelationLoss)' in issues
      and 'FeudRelation = -10' in issues and 'FeudRenewalRelationLoss = 5' in issues,
      'feud eligibility requires actual bad relation and new deterioration after handling')
check('CompanionPairKey(first.StringId, second.StringId)' in issue_owner and 'string.CompareOrdinal(first, second)' in issues,
      'same unordered hero pair uses one stable handling identity')
for key in ['_afFieldCamp_issues_v1', '_afFieldCamp_moraleGained_v1', '_afFieldCamp_suppressed_v1', '_afFieldCamp_lastDefeatHours_v1',
            '_afFieldCamp_handledCompanionRelations_v1', '_afFieldCamp_handledCompanionStates_v1',
            '_afFieldCamp_issueEnvoyVisits_v1', '_afFieldCamp_companionIssueRulesVersion_v1']:
    check('SyncData("' + key + '"' in issue_owner, 'save identity wired: ' + key)
clear = issue_owner.split('private void ClearIssueState()', 1)[1].split('internal int EnvoyVisits', 1)[0]
check('_issueEnvoyVisits.Clear()' in clear and '_handledCompanionRelations.Clear()' not in clear,
      'striking camp resets visits but retains handled companion pairs')
reset = behavior.split('private void ResetAll()', 1)[1].split('// ---------- campaign ticks', 1)[0]
check('_handledCompanionRelations.Clear()' in reset and '_handledPairsByHero = null' in reset,
      'new campaign clears persistent handling and its prompt index')
restore = issue_owner.split('private void RestoreCompanionIssueState()', 1)[1].split('private void RememberCompanionResolution', 1)[0]
check('if (_companionIssueRulesVersion >= 1) return;' in restore and 'State.Withdrawn' in restore
      and 'ApplyGrantEffect' not in restore and 'ChangeRelationAction' not in restore,
      'legacy compatibility is one-shot and withdraws obsolete requests without replaying effects')
check('RestoreCompanionIssueState();' in read(CAMP + 'Host/FieldCampBehavior.Menu.cs'),
      'legacy migration runs after campaign objects are available')
check('behavior.EnvoyVisits(i) < FieldCampBehavior.MaxEnvoyVisitsPerIssue' in envoys
      and 'FieldCampBehavior.Instance.EnvoyVisits(index) < FieldCampBehavior.MaxEnvoyVisitsPerIssue' in envoys,
      'initial visit and retry use the same persistent limit')
resolve = issue_owner.split('internal string Resolve(', 1)[1].split('private static float LeadershipFactor', 1)[0]
check(resolve.index('ApplyGrantEffect(issue, main)') < resolve.index('RememberCompanionResolution(issue)') < resolve.index('FlushIssues()'),
      'handling baseline is captured after the real effect and before save flush')
check('!FieldCampOrderTags.CanSpeakerMediate(issue, speakerAgentIndex)' in resolve
      and 'allowed.Remove("GRANT")' in orders and 'speaker == first || speaker == second' in orders,
      'bystanders cannot accept companion mediation in either rules or execution')
check('RecordHeroFact(issue.Subject, fact)' in orders and 'RecordHeroFact(issue.Subject2, fact)' in orders,
      'confirmed handling is recorded to both companion memories')
handled = issue_owner.split('internal string BuildHandledCompanionPrompt(', 1)[1].split('/// <summary>Strike or encounter', 1)[0]
check('if (reopened) continue;' in handled and 'issue.IsOpen && issue.IsCompanionIssue' in handled
      and '_handledPairsByHero.TryGetValue(speaker.StringId' in handled,
      'handled context stays until an actual new issue exists and uses cached per-hero index')
check(identity.count('FieldCampOrderTags.BuildIssuePromptBlock(npc?.AgentIndex ?? -1, hero)') == 2,
      'full and compact scene prompts both contain pending and handled camp state')
core = native.split('internal NativeConversationGameActionResult ApplyNativeConversationGameActionsLegacyCore(', 1)[1]
check(core.index('IsNativeConversationResponseTargetAvailableForActionDispatch')
      < core.index('FieldCampOrderTags.Consume(!string.IsNullOrWhiteSpace(playerText), targetAgentIndex, ref result)'),
      'native camp tags execute only after current-target validation and real player input')
action_postprocess = read('src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs')
shared_postprocess = action_postprocess.split('internal static bool TryPrepareCourierActionPostprocessForExternal(', 1)[1]
check('string text = FieldCampOrderTags.StripOnly(replyText ?? "");' in action_postprocess
      and 'string text = FieldCampOrderTags.StripOnly(replyText ?? "");' in shared_postprocess,
      'camp tags in main replies stripped before scene/courier/native postprocess')
check('(resolvedChainName == "native_conversation" || resolvedChainName == "meeting")'
      in shared_postprocess and 'latestReplyHasPlayerInput && !string.IsNullOrWhiteSpace(playerText)' in shared_postprocess,
      'shared postprocess exposes camp rules only to real native/meeting player turns')
check('fieldCampOrderRules, partyTransferRules' in shared_postprocess
      and 'fieldCampOrderTags = FieldCampOrderTags.NormalizePostprocessTags(content, fieldCampOrderRules)' in shared_postprocess
      and 'nobleExecutionOrderTags, fieldCampOrderTags, partyTransferTags' in shared_postprocess,
      'native camp tags require allowlisted normalization before execution')
strip_only = orders.split('internal static string StripOnly(', 1)[1].split('/// <summary>', 1)[0]
check('AssembleTag' in strip_only and 'DismissTag' in strip_only and 'ResolveTagRegex' in strip_only
      and 'Consume(' not in strip_only and 'FieldCampMissionLogic' not in strip_only,
      'camp reply stripping is text-only and never executes actions')
check('PersistAmbientSummaries' not in ambient and 'lastLine.Substring(0, 80)' not in ambient,
      'ambient dialogue no longer replaces the conversation with its last eighty characters')
check('onPresented: (visible, finalSegment)' in chat and 'PrepareSceneHistorySpeechText(visible)' in chat
      and '_ports.PersistNpcSpeechToNamedHeroes' in chat and 'request.OnLinePresented?.Invoke()' in chat,
      'actually presented ambient lines and segments reach shared personal history')
check(queue.index('TryShowNpcBubble(bubble.Agent') < queue.index('bubble.PlaybackInfo?.NotifyPresented')
      and 'segmentIndex <= _lastPresentedSegment' in playback,
      'history callback follows actual bubble dispatch with duplicate-segment protection')
check('SourceMission = Mission.Current' in queue and 'bubble.ConversationEpoch' in queue
      and 'SaveRuntimeGuard.IsCurrentGeneration(bubble.RuntimeGeneration)' in queue,
      'presentation callbacks cannot cross mission save or conversation ownership')
check('onPresented: () => MarkAmbientOpeningPresented(session)' in ambient and 'request.OnPresented?.Invoke()' in chains,
      'actual envoy presentation immediately reaches persistent visit accounting')
check('MyBehavior.AppendExternalSceneDialogueHistory(hero, null, null, witness, session)' in ambient
      and 'PersistAmbientWitness(speakers, heroes, place)' in ambient,
      'player witness is a confirmed context fact rather than invented NPC dialogue')
check('nearbyDatum.AgentIndex == speakerAgentIndex ? ownLine : OverheardLine' in messages
      and 'SceneHistory_AddressedToTarget' in messages,
      'persistent relay speech preserves its actual NPC addressee')
speech_entry = read(SCENE + 'ShoutBehavior.SpeechExecution.cs')
legacy_enqueue = speech_entry.split('private void EnqueueSpeechLineWithOptions(', 1)[1].split(') =>', 1)[0]
check('onPresented' not in legacy_enqueue and 'EnqueueSpeechLineWithOptions = EnqueueSceneSpeechWithPresentation' in read(SCENE + 'ShoutBehavior.SceneSessionComposition.cs'),
      'new presentation callback preserves the exact reflected SceneActions enqueue signature')
cn_values = json.loads(read(prompt + '.json'))
en_values = json.loads(read(prompt + '.en.json'))
check(set(cn_values) == set(en_values), 'runtime prompt CN/EN key parity')
for key in sorted(k for k in cn_values if k.startswith('AF_FC_') or k.startswith('AF_CONV_Ambient_')):
    check(set(re.findall(r'\{\w+\}', cn_values[key])) == set(re.findall(r'\{\w+\}', en_values[key])), key + ' variable parity')
for state in ['Encouraged', 'Granted', 'Refused', 'Disciplined']:
    check('AF_FC_HandledCompanion_' + state in cn_values, state + ' handled context has both languages')
for resolution in ['Encourage', 'Grant', 'Refuse', 'Discipline']:
    check('AF_FC_Fact_Companion_' + resolution in cn_values, resolution + ' confirmed fact has both languages')
check('亲密关系' in cn_values['AF_FC_IssuePromptHeader'] and 'do not invent intimacy' in en_values['AF_FC_IssuePromptHeader'],
      'both languages forbid unsupported specific accusations')
check('以后再谈不输出' in cn_values['AF_FC_Rule_Resolve'] and 'merely putting the discussion off' in en_values['AF_FC_Rule_Resolve'],
      'deferring the discussion is not falsely resolved')
print(f'{checks} source/resource contracts passed. C# execution, builds and native game/provider behavior NOT RUN.')
