from pathlib import Path
import re
root=Path(__file__).resolve().parents[3]
life=(root/'src/modules/AF.Module.FieldCamp/Host/FieldCampMissionLogic.Life.cs').read_text(encoding='utf-8')
poses=(root/'src/modules/AF.Module.FieldCamp/Host/FieldCampMissionLogic.Poses.cs').read_text(encoding='utf-8')
audio=(root/'src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs').read_text(encoding='utf-8')
def body(text,symbol):
 start=text.index(symbol); start=text.index('{',start); depth=1; end=start+1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
checks=0
def check(ok,name):
 global checks
 checks+=1
 assert ok,name
move=body(life,'private void MoveTo(')
check('if (!heldGroundPose) ReleasePose(entry);' in move,'non-rest workers release training/tools before route deferral')
check(move.index('entry.Route = route;') < move.index('TryBeginCampPoseExit(entry)'), 'only successfully prepared route authorizes stand-up')
check(move.index('if (heldGroundPose) ReleasePose(entry);') > move.index('TryBeginCampPoseExit(entry)'), 'sitting loop never cleared before natural stand-up')
check(move.index('SettleCampAgent(entry, blocked: false);') < move.index('TryBeginCampPoseExit(entry)'), 'same-position request cannot force stand-up')
refresh=body(life,'private void Refresh(')
check('RefreshCampGroundRest(entry)' in refresh and 'EatStandingActions' not in life+poses,'rest refresh bypasses standing eating and talking')
walk=body(life,'private void AdvanceCampWalks(')
check(walk.index('IsSceneConversationHeld(agent)') < walk.index('AdvanceCampPoseTransition(entry)'),'frame pose transition respects conversation owner')
check('Math.Min(UpkeepBatch, count)' in walk,'transition shares existing bounded frame batch')
upkeep=body(life,'private void UpkeepBatchOfAgents(')
check('entry.PoseTransition != CampPoseTransition.None' in upkeep,'upkeep cannot refresh an unfinished transition')
check('!IsCampPosePlaying(entry)' in upkeep,'upkeep respects native animation alternatives')
finished=body(life,'private bool HasFinishedCampWork(')
check('!IsCampPosePlaying(entry)' in finished,'native work alternative is not falsely declared complete')
release=body(life,'private static void ReleasePose(')
check('entry.PoseTransition = CampPoseTransition.None' in release and 'entry.RestPose = CampRestPose.None' in release,'external follow/ceremony/conversation release cancels pending transition')
check('SetActionChannel(0' in release and 'SetActionChannel(1' in release and 'blendInPeriod: 0f' in release,'instant channel clearing remains at explicit ownership transfer')
face=body(audio,'internal static void PrepareAgentForTrueLipSyncIfPossible(')
check('global::Helpers.CharacterHelper.GetDefaultFaceIdle(character)' in face and 'GetAssemblies' not in face and '.Invoke(' not in face,'per-audio-chunk facial idle uses verified direct API without assembly scan')
check('agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", false)' in face,'high-channel lip sync suppression remains in place')
acquire=body(life,'internal bool TryAcquireSceneConversation(')
check('HoldCampRestForConversation(campAgent)' in acquire and 'campAgent != null && !keepRestPose' in acquire,'conversation acquisition conditionally preserves ground resting pose')
attention=(root/'src/AF.GameAdapter.Bannerlord/Scene/SceneAttentionController.cs').read_text(encoding='utf-8')
check('FieldCampMissionLogic.Current?.IsCampAgentSeated(agent)' in body(attention,'private static void HoldAgentInPlaceUnlessSeated('),'stare hold protects camp ground seats, not only native usable chairs')
seated=body(life,'internal bool IsCampAgentSeated(')
check('entry.RestPose == CampRestPose.None' in seated and 'CampPoseTransition.Exit' in seated,'camp seated predicate uses real posture and excludes departing residents')
ambient=(root/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneAmbient.cs').read_text(encoding='utf-8')
check('FieldCampMissionLogic.Current?.IsCampAgentSeated(agent)' in body(ambient,'private static bool IsAgentUsingObject('),'ambient approach leaves seated residents in place rather than issuing agent movement')
print(f'PASS {checks} source wiring/ownership/performance contracts; static checks, not live engine acceptance')
