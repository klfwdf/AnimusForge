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
check(all(x in launcher for x in ('new MissionConversationCameraView()', 'CreateMissionConversationView(mission)',
      'CreateMissionAgentStatusUIHandler(mission)', 'CreateMissionBarterView()')), 'native conversation views and HUD wired')
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
check('TryPrepareCampNavigation(agent)' in life and 'AddBehaviorGroup<DailyBehaviorGroup>()' in life,
      'camp creates navigator only on demand')
check('ScriptBehavior.AddAgentTarget(envoy.Agent, Agent.Main)' in envoys and 'private void WalkEnvoy()' not in envoys,
      'envoy follows the player Agent, not repeated coordinate guesses')
check('_envoyVisits[issue]' in envoys and '_envoyVisits[index] =' not in envoys,
      'visits counted by presentation callback, not departure')
check('MarkEnvoyPresented(openingEnvoy, openingIssue)' in envoys and 'shout._ambientSession.OnPresented = onPresented' in field_chat,
      'actual presentation callback reaches envoy accounting')
check('CancelFieldCampEnvoyForExternal(entry.Agent)' in envoys and '!_envoyQueue.Contains(index)' in envoys,
      'envoy cancellation is scoped and pending issues are not duplicated')
check('AmbientStage.Opening' in ambient and 'AmbientStage.Presenting' in ambient and 'TickAmbientOpeningPresentation' in ambient,
      'ambient opening generation and presentation precede reply window')
check('ownerCancellationToken: session.OpeningLifetime.Token' in ambient and 'canStillPublish: () => CanPublishAmbientOpening(session)' in ambient,
      'late opening publication is owner-cancelled and revalidated')
check('Math.Min(count, 12)' in life and 'DistanceSquared(entry.Home.AsVec2) > 64f' in life,
      'stroll targets bounded near home with finite candidates')
check('TryValidateGroundedPassage' in life and 'MoveDeadline' in main and 'entry.MoveBlocked = true' in life,
      'blocked daily walkers stop after bounded return attempt')
check('GetHeightAtPoint' not in life and 'TryGetCachedTerrainHeight' in life, 'camp height no longer selects prop tops')
check('TryResolveGroundedPosition(Mission.Scene, desired, 1.5f' in people, 'horses require cached body clearance')
check('ConditionalWeakTable<Scene, GroundCache>' in site and 'MaxGroundCells' in site and 'MaxPassageSteps' in site,
      'ground sampling bounded and scoped to scene cache')
check(props.count('FieldCampSite.InitializeGroundCache(') == 1 and 'FieldCampSite.InitializeGroundCache(' not in main,
      'ground cache initialized once after props, not twice')
check('FieldCampSite.ReleaseCaches(Mission.Scene)' in main and 'Mission.EndMission()' in main,
      'unsafe camp setup exits and mission releases geometry cache')
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
check('MarkCampaignCasualtyHandledExternally("field_camp_captive_owner")' in people and 'ApplyByExecution(hero, killer' in people,
      'camp owns casualty once and attributes hero execution to actual killer')
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
check(walk.index('SetMaximumSpeedLimit(-1f') < walk.index('SetScriptedPositionAndDirection'),
      'ceremony clears old conversation speed freeze before issuing walk')
check('request.VenueName?.ToString()' in identity and 'request.IsFieldCamp' in identity and
      '"scene_execution_role_camp_crowd"' in identity, 'camp name and soldier role reach real prompt consumer')
role_cn = json.loads(read('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.zh-CN.json'))['scene_execution_role_camp_crowd']
role_en = json.loads(read('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.en.json'))['scene_execution_role_camp_crowd']
check('士兵' in role_cn and 'soldier' in role_en and
      set(re.findall(r'\{\w+\}', role_cn)) == set(re.findall(r'\{\w+\}', role_en)),
      'camp spectator role has matching CN/EN semantics and variables')
print(f'{checks} source/resource contracts passed. C# execution, builds and native game/provider behavior NOT RUN.')
