using AnimusForge;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
int assertions = 0;
void Require(bool condition, string name) { if (!condition) throw new Exception("FAIL: "+name); assertions++; }
Mission mission = new(); Mission.Current = mission;
Agent.Main = new Agent { Index=0, Mission=mission };
Agent first = new() { Index=1, Mission=mission };
Agent second = new() { Index=2, Mission=mission };
mission.Agents.AddRange(new[] { Agent.Main, first, second });
int activated=-1, deactivated=0, ended=0; long fingerprint=1;
var panel = new ScenePresentationController(a => a?.IsActive()==true, () => 10, m=>null, ()=>5, ()=>fingerprint, i=>activated=i, ()=>deactivated++, ()=>ended++);
Require(panel.EnsurePresentationSession(new[] { first,second },1), "session begins");
Require(panel.GetParticipants().Count==2 && activated==1, "members and movement wired");
panel.CycleParticipant(2); Require(panel.FindPresentationMember(2).State==ScenePresentationParticipantState.Excluded,"seal excludes");
panel.CycleParticipant(2); second.Position=new Vec3 { X=100 }; panel.TickPresentationSession(.1f);
Require(panel.FindPresentationMember(2).InRange,"locked ignores distance");
Agent.Main.Health=99; panel.TickPresentationSession(.05f); Require(!ScenePresentationController._presentationCollapsed,"not before 10Hz");
panel.TickPresentationSession(.05f); Require(ScenePresentationController._presentationCollapsed,"damage collapses panel");
first.Active=false; panel.TickPresentationSession(.1f); Require(panel._presentationAddresseeIndex==2,"removed addressee falls to locked");
panel.BeginRound(0); Require(panel.IsRoundActive(7,1,true,()=>false),"pending group blocks");
Require(!panel.IsRoundActive(8,1,true,()=>false),"unclaimed line expires");
panel.BeginRound(10); var task=new TaskCompletionSource<bool>(); panel.NoteRoundGroup(task.Task,1);
Require(panel.IsRoundActive(11,1,true,()=>false),"running group blocks"); task.SetResult(true);
Require(panel.IsRoundActive(12,1,false,()=>false),"postprocess gate blocks");
Require(panel.IsRoundActive(13,1,true,()=>true),"speech remains busy");
Require(!panel.IsRoundActive(58,1,true,()=>true),"ambient speech grace bounded");
panel.BeginRound(60); panel.NoteRoundGroup(Task.CompletedTask,1); Require(!panel.IsRoundActive(60,2,true,()=>true),"retired epoch ends completed round");

string content,status;
Require(!panel.PrepareTextSubmission("",()=>true,()=>false,()=>false,out content,out status) && status=="", "blank line not sent");
Require(!panel.PrepareTextSubmission("line",()=>false,()=>false,()=>false,out content,out status), "off-thread submission refused");
Require(!panel.PrepareTextSubmission("line",()=>true,()=>true,()=>true,out content,out status) && status.Contains("打断"), "busy round preserves interrupt hint");
Require(panel.PrepareTextSubmission("  a\r\n ",()=>true,()=>false,()=>false,out content,out status) && content=="a", "valid line normalized without alternate pipeline");
var records = Enumerable.Range(5,8).Select(i => new ScenePresentationHistoryRecord { Exists=true,EventSequence=i,Role="assistant",Content="line"+i,SpeakerName="NPC" }).ToList();
bool NormalizeFact(string text,out string fact) { fact=text.StartsWith("[AFEF") ? text : null; return fact!=null; }
Require(panel.BuildHistory(records.Count,i=>records[i],NormalizeFact,40).Count==6,"history keeps two current and four context lines");
records.Add(new ScenePresentationHistoryRecord { Exists=true,EventSequence=13,Role="system",Content="internal prompt" });
records.Add(new ScenePresentationHistoryRecord { Exists=true,EventSequence=14,Role="system",Content="[AFEF NPC行为补充] done" });
var history=panel.BuildHistory(records.Count,i=>records[i],NormalizeFact,40);
Require(history.Last().Kind=="fact" && !history.Any(x=>x.Text=="internal prompt"),"only confirmed AFEF system lines visible");
Require(panel.BuildHistory(records.Count,i=>records[i],NormalizeFact,1).Count==1,"max display lines bounded");
List<(int,int)> staged=new(); int resets=0;
int effects=0,accepted=0; bool targetValid=true;string lastAcceptedFact="";Action effectLeaf=()=>effects++;
var liveResources=new List<ShoutBehavior.ShoutTradeResourceOption> { new() {IsGold=true,Name="coin",AvailableAmount=4} };
var trade=SceneInteractionContract.Create(panel,()=>new List<ShoutBehavior.ShoutTradeResourceOption>(liveResources),()=>effectLeaf(),(text,fact,target)=>{accepted++;lastAcceptedFact=fact;},()=>targetValid);
ShoutBehavior.CurrentInstance=new ShoutBehavior {_j17SceneTradeController=trade};
trade.StartFlowIdentity();
var options=new[] { new ScenePresentationTradeOption {Name="coin",Available=4},new ScenePresentationTradeOption {Name="town",Available=1,IsSettlement=true} };
Require(!trade.StageTrade(options,new[]{0},new[]{1},"给予","NPC",staged.Clear,(i,n)=>staged.Add((i,n)),()=>{},out status),"trade cannot stage without owned target");
trade.TradeOwnsState=true;
Require(trade.StageTrade(options,new[]{0,0,1,-1},new[]{2,2,99,2},"给予","NPC",staged.Clear,(i,n)=>staged.Add((i,n)),()=>{},out status) && staged.SequenceEqual(new[]{(0,2),(1,1)}),"selection deduplicated and settlement singleton");
Require(trade.TradeStaged && trade.TradeSummary=="给予 NPC：coin ×2、town ×1","staged summary preserves ordered UI names");
Require(!trade.StageTrade(options,new[]{0,1},new[]{5,1},"给予","NPC",staged.Clear,(i,n)=>staged.Add((i,n)),()=>{},out status) && staged.Count==0,"bad amount clears transient selection");
Require(!trade.ValidateStagedTrade(true,false,()=>resets++,out status) && resets==1 && !trade.TradeStaged,"departed staged target resets and refuses line");
// The actual controller drives popup selection -> amount -> chat -> irreversible commit; game effects remain leaves.
var presentationMission=Mission.Current;long presentationGeneration=SaveRuntimeGuard.Generation;
Mission.Current=new Mission(); var tradeNpc=new NpcDataPacket {AgentIndex=7,Name="NPC"};
Mission.Current.Agents.Add(new Agent {Index=7,Mission=Mission.Current});
void OpenTradeAndSelect() {
 trade.BeginShoutTradeFlow(tradeNpc,ShoutBehavior.ShoutChatMode.Give);
 TaleWorlds.Core.MultiSelectionInquiryData.Latest.Confirm(new List<TaleWorlds.Core.InquiryElement>{new(0,"coin",null,true,"")});
}
OpenTradeAndSelect(); var amountConfirm=TaleWorlds.Core.TextInquiryData.Latest.Confirm;
Require(effects==0,"menu and quantity planning have no effects");
amountConfirm("2"); var chatConfirm=ShoutTextInputPopup.Confirm;
amountConfirm("2"); Require(trade._shoutPendingTradeItems.Count==1&&trade._shoutPendingTradeItems[0].Amount==2,"duplicate quantity callback cannot advance or overrun successor step");
chatConfirm("hello"); chatConfirm("again");
Require(effects==1&&accepted==1,"duplicate confirmation commits exactly once");
trade.BeginShoutTradeFlow(tradeNpc,ShoutBehavior.ShoutChatMode.Give);var oldMenu=TaleWorlds.Core.MultiSelectionInquiryData.Latest;
trade.BeginShoutTradeFlow(tradeNpc,ShoutBehavior.ShoutChatMode.Show);oldMenu.Cancel();oldMenu.Confirm(new List<TaleWorlds.Core.InquiryElement>{new(0,"coin",null,true,"")});
Require(trade._shoutTradeMode==ShoutBehavior.ShoutChatMode.Show&&trade._shoutPendingTradeItems.Count==0,"old menu cancel/reentry cannot erase or stage newer flow");
OpenTradeAndSelect(); amountConfirm=TaleWorlds.Core.TextInquiryData.Latest.Confirm;amountConfirm("2");chatConfirm=ShoutTextInputPopup.Confirm;
liveResources[0].AvailableAmount=1;chatConfirm("insufficient");Require(effects==1&&accepted==1,"confirm rechecks current resource quantity");
liveResources[0].AvailableAmount=4;OpenTradeAndSelect();TaleWorlds.Core.TextInquiryData.Latest.Confirm("1");chatConfirm=ShoutTextInputPopup.Confirm;
targetValid=false;chatConfirm("stale target");Require(effects==1&&accepted==1,"illegal target cannot commit");targetValid=true;
OpenTradeAndSelect();TaleWorlds.Core.TextInquiryData.Latest.Confirm("1");chatConfirm=ShoutTextInputPopup.Confirm;Mission.Current=new Mission();chatConfirm("old mission");Require(effects==1,"old Mission UI cannot commit");
OpenTradeAndSelect();TaleWorlds.Core.TextInquiryData.Latest.Confirm("1");chatConfirm=ShoutTextInputPopup.Confirm;SaveRuntimeGuard.Generation++;chatConfirm("old save");Require(effects==1,"old save UI cannot commit");
OpenTradeAndSelect();TaleWorlds.Core.TextInquiryData.Latest.Cancel();Require(effects==1,"quantity cancel has no transfer effect");
OpenTradeAndSelect();TaleWorlds.Core.TextInquiryData.Latest.Confirm("1");ShoutTextInputPopup.Cancel();Require(effects==1,"chat cancel has no transfer effect");
trade.BeginShoutTradeFlow(tradeNpc,ShoutBehavior.ShoutChatMode.Give);trade.TradeOwnsState=true;long panelRevision=trade.FlowRevision;
Require(SceneTradeController.StageScenePresentationTradeForExternal(panelRevision,new[]{0},new[]{1},out status)&&trade.TradeStaged&&effects==1,"strict panel ABI plans on sole owner without effects");
SceneTradeController.CancelScenePresentationTradeForExternal(panelRevision-1);Require(trade.TradeStaged,"old panel cancel cannot clear newer sole stage");
trade.BeginShoutTradeFlow(tradeNpc,ShoutBehavior.ShoutChatMode.Show);
Require(!trade.TradeStaged&&!SceneTradeController.StageScenePresentationTradeForExternal(panelRevision,new[]{0},new[]{1},out status),"popup reentry retires stage and rejects old panel index revision");
int actionOnlyFinished=0;
trade.OpenNativeConversationGiveShowMenu(tradeNpc,null,null,()=>actionOnlyFinished++);
TaleWorlds.Core.MultiSelectionInquiryData.Latest.Confirm(new List<TaleWorlds.Core.InquiryElement>{new("give","give",null,true,"")});
TaleWorlds.Core.MultiSelectionInquiryData.Latest.Confirm(new List<TaleWorlds.Core.InquiryElement>{new(0,"coin",null,true,"")});
amountConfirm=TaleWorlds.Core.TextInquiryData.Latest.Confirm;amountConfirm("1");amountConfirm("1");
Require(effects==2&&accepted==1&&actionOnlyFinished==1,"Native action-only delivers once and finishes once without AI reply");
trade.OpenNativeConversationGiveShowMenu(tradeNpc,null,null,()=>actionOnlyFinished++);var nativeCancel=TaleWorlds.Core.MultiSelectionInquiryData.Latest.Cancel;nativeCancel();nativeCancel();
Require(effects==2&&accepted==1&&actionOnlyFinished==2,"Native action-only cancel any initial step finishes once without effects/reply");
// Even a game leaf throwing after a real partial mutation cannot make an old confirmation retryable.
effectLeaf=()=>{effects++;throw new Exception("synthetic effect threw after source mutation");};
OpenTradeAndSelect();TaleWorlds.Core.TextInquiryData.Latest.Confirm("1");var partialConfirm=ShoutTextInputPopup.Confirm;partialConfirm("partial");partialConfirm("duplicate partial");
Require(effects==3&&accepted==2&&lastAcceptedFact.Contains("未确认"),"partial effect exception consumes confirmation and publishes unknown, never success or retry");
trade.OpenNativeConversationGiveShowMenu(tradeNpc,null,null,()=>actionOnlyFinished++);TaleWorlds.Core.MultiSelectionInquiryData.Latest.Confirm(new List<TaleWorlds.Core.InquiryElement>{new("give","give",null,true,"")});
TaleWorlds.Core.MultiSelectionInquiryData.Latest.Confirm(new List<TaleWorlds.Core.InquiryElement>{new(0,"coin",null,true,"")});var partialNativeConfirm=TaleWorlds.Core.TextInquiryData.Latest.Confirm;partialNativeConfirm("1");partialNativeConfirm("1");
Require(effects==4&&accepted==2&&actionOnlyFinished==3,"Native partial effect exception finishes consumed lifecycle once without reply/retry");
effectLeaf=()=>effects++;
// Full current TradeAdapter executes with synthetic game mutation leaves; original effects/facts code is linked unchanged as a class.
Mission transferMission=new();Mission.Current=transferMission;var recipientAgent=new Agent{Index=31,Mission=transferMission};transferMission.Agents.Add(recipientAgent);
var recipientNpc=new NpcDataPacket{AgentIndex=31,Name="recipient"};
TaleWorlds.CampaignSystem.Hero.MainHero=new(){Gold=10,PartyBelongedTo=new()};
SceneTradeBannerlordAdapter transferAdapter=null;SceneTradeController transferController=null;string actualFact="";int actualAccepts=0;
transferController=SceneInteractionContract.Create(panel,()=>transferAdapter.BuildShoutTradeOptions(),()=>transferAdapter.ApplyShoutGiveTransfer(),
 (text,fact,target)=>{actualFact=fact;actualAccepts++;},()=>transferAdapter.EnsureShoutTradePrimaryTargetValidForCommit(true),give=>transferAdapter.BuildShoutTradeFactText(give));
transferAdapter=SceneInteractionContract.CreateTradeAdapter(transferController,panel);ShoutBehavior.CurrentInstance=new(){_j17SceneTradeController=transferController};
Action<string> OpenActualTransfer(int amount) {
 transferController.BeginShoutTradeFlow(recipientNpc,ShoutBehavior.ShoutChatMode.Give);
 TaleWorlds.Core.MultiSelectionInquiryData.Latest.Confirm(new List<TaleWorlds.Core.InquiryElement>{new(0,"resource",null,true,"")});
 TaleWorlds.Core.TextInquiryData.Latest.Confirm(amount.ToString());return ShoutTextInputPopup.Confirm;
}
var currentGoldConfirm=OpenActualTransfer(3);currentGoldConfirm("give");
Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==7&&actualFact.Contains("来源减少 3")&&actualFact.Contains("目标是否收到未知"),"actual non-Hero gold source-only effect emits unknown recipient instead of success");
currentGoldConfirm("repeat");Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==7&&actualAccepts==1,"actual source-only delivery is consumed and never repeated");
var recipientHero=new TaleWorlds.CampaignSystem.Hero{PartyBelongedTo=new()};recipientAgent.Character.HeroObject=recipientHero;recipientNpc.IsHero=true;
TaleWorlds.CampaignSystem.Hero.MainHero.Gold=10;OpenActualTransfer(4)("to hero");
Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==6&&recipientHero.Gold==4&&!actualFact.Contains("未知"),"actual Hero gold receipt observes both source and recipient deltas");
var recycledConfirm=OpenActualTransfer(1);transferMission.Agents.Clear();transferMission.Agents.Add(new Agent{Index=31,Mission=transferMission,Character=recipientAgent.Character});recycledConfirm("recycled");
Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==6&&recipientHero.Gold==4,"actual adapter rejects recycled Agent index before effect");transferMission.Agents.Clear();transferMission.Agents.Add(recipientAgent);
TaleWorlds.CampaignSystem.Hero.MainHero.Gold=0;var inventoryItem=new TaleWorlds.Core.ItemObject{StringId="letter",Name=new("letter")};
var sourceRoster=TaleWorlds.CampaignSystem.Hero.MainHero.PartyBelongedTo.ItemRoster;sourceRoster.Items[inventoryItem]=3;recipientHero.PartyBelongedTo.ItemRoster.MaximumAdd=1;
OpenActualTransfer(3)("partial item");
Require(sourceRoster.GetItemNumber(inventoryItem)==0&&recipientHero.PartyBelongedTo.ItemRoster.GetItemNumber(inventoryItem)==1&&actualFact.Contains("来源另减少 2"),"actual item partial target delta retains extra source-only remainder");
sourceRoster.Items[inventoryItem]=3;recipientHero.PartyBelongedTo.ItemRoster.Items.Clear();recipientHero.PartyBelongedTo.ItemRoster.BeforeAdd=()=>throw new Exception("synthetic recipient add rejection");
var partialItemConfirm=OpenActualTransfer(3);partialItemConfirm("source only item");partialItemConfirm("retry");
Require(sourceRoster.GetItemNumber(inventoryItem)==0&&recipientHero.PartyBelongedTo.ItemRoster.GetItemNumber(inventoryItem)==0&&actualFact.Contains("来源减少 3")&&actualFact.Contains("未知"),"actual source-first item exception preserves source-only fact and blocks repeated confirmation");
recipientHero.PartyBelongedTo.ItemRoster.BeforeAdd=null;sourceRoster.Items.Clear();TaleWorlds.CampaignSystem.Hero.MainHero.Gold=5;recipientHero.Gold=0;
TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ThrowAfterDebit=true;OpenActualTransfer(2)("debit throws");TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ThrowAfterDebit=false;
Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==3&&recipientHero.Gold==0&&actualFact.Contains("来源减少 2")&&actualFact.Contains("未知"),"actual gold debit-then-throw never claims recipient success");
recipientAgent.Character.HeroObject=null;recipientNpc.IsHero=false;TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement=new();
var merchantEffects=new RewardSystemBehavior{Merchant=true};RewardSystemBehavior.Instance=merchantEffects;TaleWorlds.CampaignSystem.Hero.MainHero.Gold=5;
OpenActualTransfer(2)("merchant source only");
Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==3&&merchantEffects.RecordedGold==2&&actualFact.Contains("目标是否收到未知")&&merchantEffects.MerchantFacts.Single().Contains("目标到账尚未确认"),"actual merchant nullable-market effect retains prepaid record but both history consumers report unknown recipient");
TaleWorlds.CampaignSystem.Hero.MainHero.Gold=0;sourceRoster.Items[inventoryItem]=3;TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement.ItemRoster.MaximumAdd=1;merchantEffects.MerchantFacts.Clear();
OpenActualTransfer(3)("merchant partial item");
Require(sourceRoster.GetItemNumber(inventoryItem)==0&&TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement.ItemRoster.GetItemNumber(inventoryItem)==1&&actualFact.Contains("来源另减少 2")&&merchantEffects.MerchantFacts.Single().Contains("1 个"),"actual merchant item history uses observed recipient quantity and retains extra source-only fact");
sourceRoster.Items.Clear();TaleWorlds.CampaignSystem.Hero.MainHero.Gold=3;merchantEffects.ThrowOnMerchantCapture=true;OpenActualTransfer(1)("capture throws");merchantEffects.ThrowOnMerchantCapture=false;
Require(TaleWorlds.CampaignSystem.Hero.MainHero.Gold==3&&actualFact.Contains("无法确认剩余资源与目标的变化")&&!transferController.TradeStaged,"actual pre-effect capture exception retires confirmation with unknown fact and no mutation/retry");
RewardSystemBehavior.Instance=null;TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement=null;
// Restore only the synthetic fixture clock/context; the unrelated panel cases own their original fixture.
Mission.Current=presentationMission;SaveRuntimeGuard.Generation=presentationGeneration;
int cancels=0,previews=0,merges=0; bool charge=false,released=false,down=true; float heldSeconds=0;
var hotkeys=new ScenePresentationHotkeyPort { IsCharging=()=>charge,IsReleased=()=>released,IsDown=()=>down,HeldSeconds=()=>heldSeconds,
 BeginIfPressed=(a,b)=>{charge=true;return true;},Cancel=r=>{charge=false;cancels++;},CaptureMerge=()=>()=>merges++,DrawPreview=()=>previews++ };
Require(panel.UpdateHotkey(hotkeys,TaleWorlds.InputSystem.InputKey.T,TaleWorlds.InputSystem.InputKey.Y) && panel.MergesHotkeyCharge,"session hotkey starts merge charge");
released=true;heldSeconds=.1f;Require(panel.UpdateHotkey(hotkeys,default,default) && !ScenePresentationController._presentationCollapsed && merges==0,"tap expands without merging");
released=false;panel.UpdateHotkey(hotkeys,default,default);heldSeconds=.3f;panel.UpdateHotkey(hotkeys,default,default);Require(previews==1,"held key previews framed audience");
released=true;panel.UpdateHotkey(hotkeys,default,default);Require(merges==1 && cancels==2,"hold release merges once and cancels charge");

Mission.Current = new Mission(); panel.TickPresentationSession(.1f);
Require(!panel.IsPresentationSessionLive() && panel.GetParticipants().Count==0 && deactivated==1,"mission retirement clears state");
int version=ScenePresentationController._presentationVersion; panel.EndPresentationSession("repeat"); Require(ScenePresentationController._presentationVersion==version && deactivated==1,"repeat end idempotent");
Mission.Current=mission; first.Active=true;
int session=1,epoch=1,finished=0,failed=0,cancelled=0,timeoutArms=0;
SceneAudioFailureOutput failureOutput=new(); Queue<Action> dispatched=new(); HashSet<int> started=new();
var port = new SceneAudioLipSyncPort {
 SceneSessionId=()=>session, ConversationEpoch=()=>epoch, Dispatch=a=>dispatched.Enqueue(a),
 Finished=i=>finished++, Cancelled=r=>cancelled++, CompleteNativeWait=(r,e)=>false,
 IsNativeWait=r=>false, QueueMapPlayback=(r,w,x,d)=>false, UseMapPlayback=()=>false,
 CanParticipate=i=>true, CanLipSync=(Agent a,out string r)=>{r="";return true;}, CanLipSyncIndex=(int i,out string r)=>{r="";return true;},
 FailureOutput=i=>failureOutput, UnmarkPlaybackStarted=i=>started.Remove(i), ConvertDialogueFeed=(i,d)=>{}, FlushFailedSpeechEffects=(i,d)=>{}, ShowBubble=(a,t,d)=>failed++, ScheduleInteractionTimeout=(i,t,d)=>timeoutArms++, EstimateTypingDuration=t=>1,
 AudioDuration=(i,d)=>{}, DispatchBubble=(i,f)=>true, ClearOrphanDuration=i=>{}, ScheduleBubbleFallback=r=>{},
 HasPlaybackStarted=i=>started.Contains(i), MarkPlaybackStarted=i=>started.Add(i), ClearAgentPending=i=>{}, ClearAgentInteraction=i=>{},
 PauseTyping=p=>{}, StopTyping=()=>{}, ClearPending=()=>{}, SpeechBusy=()=>false, ClearSpeechQueue=()=>{}, ClearMeetingControl=()=>{}, SuppressMeetingControl=()=>false, Report=(s,i,e)=>{}
};
var audio = new SceneAudioLipSyncController(port); audio.SubscribeTtsPlaybackEvents();
void Drain() { while(dispatched.Count>0) dispatched.Dequeue()(); }
var old=new TtsEngine.PlaybackRequest { RequestId=1, AgentIndex=1 }; var next=new TtsEngine.PlaybackRequest { RequestId=2, AgentIndex=1 };
audio.TrackTtsPlaybackRequest(old); TtsEngine.Instance.Start(old); Drain(); Require(audio.IsActiveTtsPlaybackRequest(old),"first request active");
audio.TrackTtsPlaybackRequest(next); TtsEngine.Instance.Start(next); Drain();
SoundEvent held=new(); audio._agentSoundEvents[1]=held;
TtsEngine.Instance.Finish(old); Drain(); Require(finished==0 && audio._agentSoundEvents[1]==held,"old finish cannot clean new audio");
TtsEngine.Instance.Finish(next); Drain(); Require(finished==1 && !audio.IsTtsPlaybackRequestCurrent(next),"natural finish retires exactly once");
TtsEngine.Instance.Finish(next); Drain(); Require(finished==1,"duplicate finish ignored");
var stale=new TtsEngine.PlaybackRequest { RequestId=3, AgentIndex=1 };audio.TrackTtsPlaybackRequest(stale);SaveRuntimeGuard.Generation++;
TtsEngine.Instance.Start(stale);Drain();Require(!audio.IsTtsPlaybackRequestCurrent(stale),"load generation rejects late callback");
audio.ResetRequestOwnership();Require(audio._ttsPlaybackOwners.Count==0,"load clears resource request identities");
var active=new TtsEngine.PlaybackRequest { RequestId=4, AgentIndex=1 }; audio.TrackTtsPlaybackRequest(active);epoch++;Require(!audio.IsTtsPlaybackRequestCurrent(active),"retired epoch rejected");
epoch--;session++;Require(!audio.IsTtsPlaybackRequestCurrent(active),"new history session rejects old callback");
failureOutput=new SceneAudioFailureOutput { HasBubble=true,Agent=second,UiContent="fallback",TypingDuration=2,HasInteractionToken=true,InteractionToken=9 };
var failure=new TtsEngine.PlaybackRequest { RequestId=6,AgentIndex=2 };audio.TrackTtsPlaybackRequest(failure);
TtsEngine.Instance.Fail(failure);Drain();TtsEngine.Instance.Fail(failure);Drain();
Require(failed==1 && timeoutArms==1 && !audio.IsTtsPlaybackRequestCurrent(failure),"failure produces one text fallback and retires ownership");
var queued=new TtsEngine.PlaybackRequest { RequestId=5, AgentIndex=1 };audio.TrackTtsPlaybackRequest(queued);TtsEngine.Instance.Start(queued);
int startedBefore=started.Count; audio.UnsubscribeTtsPlaybackEvents();Drain();Require(started.Count==startedBefore && !audio.IsTtsPlaybackRequestCurrent(queued),"queued completion retired on unsubscribe");
TtsEngine.Instance.Cancel(active);Require(cancelled==0,"unsubscribe releases all listeners");
audio.CleanupSceneLipSyncAfterPlaybackFinished(1);int deferred=audio._deferredCleanupQueue.Count;audio.CleanupSceneLipSyncAfterPlaybackFinished(1);Require(audio._deferredCleanupQueue.Count==deferred,"duplicate resource finish queues once");
Require(first.AgentVisuals.Ends==1,"native detach once");
audio.StopAllLipSyncPlaybackAndCleanup();Require(audio._agentSoundEvents.Count==0 && audio._deferredCleanupQueue.Count==0,"stop all clears held resources");
// Full production interaction owner; movement, speech dispatch and Agent engine leaves are synthetic.
Mission interactionMission=new();Mission.Current=interactionMission;
Agent.Main=new Agent{Index=0,Mission=interactionMission};
Agent ia=new(){Index=11,Mission=interactionMission},ib=new(){Index=12,Mission=interactionMission};
interactionMission.Agents.AddRange(new[]{Agent.Main,ia,ib});
var movement=new SceneMovementController();var releaseTrace=new List<string>();
SceneInteractionLifecycleController interaction=null;var outputDispatcher=new System.Collections.Concurrent.ConcurrentQueue<Action>();int shownBubbles=0;
var currentOutput=new SceneSpeechOutputQueueController(new SceneSpeechOutputQueueControllerPorts {
 Get_mainThreadActions=()=>outputDispatcher,Set_mainThreadActions=v=>{},Get_sceneMovement=()=>movement,
 IsSceneConversationEpochCurrent_L223=e=>e==1,CaptureConversationEpoch=()=>epoch,ClearInteractionTimeoutArms=()=>interaction.ClearInteractionTimeoutArms(),
 ResetSceneAudioRequestOwnership_L67=()=>audio.ResetRequestOwnership(),IsTtsPlaybackRequestCurrent_L73=(r,c)=>audio.IsTtsPlaybackRequestCurrent(r,c),
 LogTtsReport_L208=(stage,i,extra)=>{},TryShowNpcBubble_L2127=(agent,text,duration)=>{shownBubbles++;return true;}
});
interaction=new SceneInteractionLifecycleController(new SceneInteractionLifecycleControllerPorts {
 Get_sceneMovement=()=>movement,GetSceneAudio=()=>audio,CaptureMovementSuppressionAgents=indices=>{},
 ReleaseStareAgent=i=>releaseTrace.Add("stare:"+i),RemoveSceneMovementSuppressionAgents_L18679=indices=>{foreach(int i in indices)releaseTrace.Add("suppression:"+i);},
 RestoreAgentAutonomy_L20524=a=>releaseTrace.Add("autonomy:"+a.Index),ReleaseAgentFromSceneConversationLocks_L19099=a=>releaseTrace.Add("meeting:"+a.Index),
 CancelAutonomyRestore=i=>releaseTrace.Add("cancelrestore:"+i),PrepareAutonomyRestore=i=>releaseTrace.Add("prepare:"+i),
 ClearPendingSpeechCompletionTokens=currentOutput.ClearPendingSpeechCompletionTokens,UnmarkPlaybackStarted=currentOutput.UnmarkPlaybackStarted,
 RunSceneTtsPlaybackFinishedStep_L176=(step,i,action)=>action(),LogTtsReport_L208=(step,i,extra)=>{},
 TryDequeuePendingSpeechCompletionToken_L2832=currentOutput.TryDequeuePendingSpeechCompletionToken,
 AppendTargetedSceneNpcFact_L18284=(fact,i,persist)=>releaseTrace.Add("fact:"+i),
 TriggerImmediateSceneBehaviorReaction_L147=(fact,i,persist,stare,seconds,skip,returns,noSpeech,siege,current,done)=>{releaseTrace.Add("speech:"+i);noSpeech?.Invoke();return false;}
});
NpcDataPacket ina=new(){AgentIndex=11,Name="A"},inb=new(){AgentIndex=12,Name="B"};
interaction.TrackPlayerInteraction(ina);long oldInteraction=interaction.CaptureSpeechInteractionToken(11);
interaction.ScheduleInteractionTimeoutArm(11,oldInteraction,2);interaction.TrackPlayerInteraction(ina);
long currentInteraction=interaction.CaptureSpeechInteractionToken(11);
interaction.ArmActiveInteractionTimeoutNow(11,oldInteraction);
Require(!interaction.CaptureInteractionDiagnostic(11).TimeoutArmed,"old completion token cannot arm reentered interaction");
interaction.ScheduleInteractionTimeoutArm(11,currentInteraction,2);interactionMission.CurrentTime=1;interaction.ProcessPendingInteractionTimeoutArms();
Require(!interaction.CaptureInteractionDiagnostic(11).TimeoutArmed,"arm waits for actual speech duration");
interactionMission.CurrentTime=2;interaction.ProcessPendingInteractionTimeoutArms();
Require(interaction.CaptureInteractionDiagnostic(11).TimeoutArmed&&!interaction.CaptureInteractionDiagnostic(11).HasPendingArm,"matching arm consumes scheduled token");
currentOutput.EnqueuePendingSpeechCompletionToken(11,currentInteraction);interactionMission.CurrentTime=3;interaction.PrepareInteractionCompletion(11);
float armedActivity=interaction._activeInteractionSessions[11].LastActivityTime;interactionMission.CurrentTime=4;interaction.PrepareInteractionCompletion(11);
Require(interaction._activeInteractionSessions[11].LastActivityTime==armedActivity,"duplicate speech ended cannot rearm timeout");
interaction.ResetInteractionsForLoadedSave();interaction.TrackPlayerInteraction(ina,2,5);interaction.TrackPlayerInteraction(inb,2,5);
interaction.ArmActiveInteractionTimeoutNow(11,interaction.CaptureSpeechInteractionToken(11));interaction.ArmActiveInteractionTimeoutNow(12,interaction.CaptureSpeechInteractionToken(12));
interactionMission.CurrentTime=10;interaction.UpdateActiveInteractionTimeouts();
Require(interaction._activeInteractionSessions.Count==0&&releaseTrace.Count(t=>t.StartsWith("speech:"))==1,"group expiry releases others then one representative speech");
Require(releaseTrace.IndexOf("autonomy:12")<releaseTrace.IndexOf("speech:11"),"group release preserves other-before-representative ordering");
Require(interaction.TimeoutUpdateCalls==1&&interaction.LastTimeoutActiveCount==2&&interaction.LastTimeoutAgentCount==3&&interaction.LastTimeoutWorkItems==2&&interaction.LastTimeoutAgentInspections==5&&interaction.LastTimeoutGroupAgentInspections>0&&interaction.LastTimeoutTotalAgentInspections==interaction.LastTimeoutAgentInspections+interaction.LastTimeoutGroupAgentInspections,"timeout frequency and real active/Agent/scan metrics recorded");
releaseTrace.Clear();movement.Following.Add(11);interaction.TrackPlayerInteraction(ina);
Require(interaction._activeInteractionSessions.Count==0&&releaseTrace.Count==0,"command follow does not get interaction lifecycle ownership");
movement.Following.Clear();movement.Summon.Add(11);interaction.TrackPlayerInteraction(ina);
Require(interaction._activeInteractionSessions.Count==0,"active summon excludes idle interaction");movement.Summon.Clear();
interaction.TrackPlayerInteraction(ina,1,1);interaction.ArmActiveInteractionTimeoutNow(11,interaction.CaptureSpeechInteractionToken(11));movement.Following.Add(11);interactionMission.CurrentTime+=2;interaction.UpdateActiveInteractionTimeouts();
Require(interaction._activeInteractionSessions.Count==0&&!releaseTrace.Any(t=>t.StartsWith("autonomy:")),"follow begun after arm is removed without autonomy release");movement.Following.Clear();
interaction.TrackPlayerInteraction(ina,1,1);interaction.ArmActiveInteractionTimeoutNow(11,interaction.CaptureSpeechInteractionToken(11));ia.Active=false;interaction.UpdateActiveInteractionTimeouts();
Require(interaction._activeInteractionSessions.Count==0&&releaseTrace.Contains("suppression:11")&&!releaseTrace.Contains("autonomy:11"),"downed Agent releases suppression without live autonomy commands");ia.Active=true;
releaseTrace.Clear();ShoutBehavior.PreserveMeeting=true;interaction.TrackPlayerInteraction(ina,1,1);interaction.ArmActiveInteractionTimeoutNow(11,interaction.CaptureSpeechInteractionToken(11));ia.Position=new Vec3{X=100};interaction.UpdateActiveInteractionTimeouts();
Require(releaseTrace.Contains("meeting:11")&&!releaseTrace.Contains("autonomy:11"),"meeting autonomy preserves original lock-release branch");ShoutBehavior.PreserveMeeting=false;ia.Position=default;
interaction.TrackPlayerInteraction(ina);interaction.ScheduleInteractionTimeoutArm(11,interaction.CaptureSpeechInteractionToken(11),1);interaction.ResetInteractionsForLoadedSave();interactionMission.CurrentTime+=100;interaction.ProcessPendingInteractionTimeoutArms();
Require(interaction._activeInteractionSessions.Count==0&&interaction._pendingInteractionTimeoutArms.Count==0,"Mission/save retirement clears sessions and arm tokens");
// Output FIFO, duration pairing, token cancellation and feed timers use the actual sole-lock owner.
currentOutput.EnqueuePendingNpcBubble(11,ia,"first","A",2);currentOutput.EnqueuePendingNpcBubble(11,ia,"second","A",3);
currentOutput.EnqueuePendingAudioDuration(11,4);currentOutput.EnqueuePendingSpeechCompletionToken(11,77);
Require(currentOutput.TryDequeuePendingNpcBubble(11,out var firstBubble,out float firstDuration)&&firstBubble.UiContent=="first"&&firstDuration==4,"actual output FIFO pairs oldest bubble with oldest real audio duration");
Require(currentOutput.CaptureQueueDiagnostic(11).PendingBubbleCount==1&&currentOutput.CaptureQueueDiagnostic(11).PendingSpeechTokenCount==1,"actual queue diagnostic sees same sole state");
currentOutput.ClearPendingTtsBubbleSyncForAgent(11,false);Require(currentOutput.TryDequeuePendingSpeechCompletionToken(11,out long preservedToken)&&preservedToken==77,"ordinary bubble clear preserves completion token");
currentOutput.EnqueuePendingSpeechCompletionToken(11,88);currentOutput.EnqueuePendingNpcBubble(11,ia,"cancelled","A",1);currentOutput.ClearPendingTtsBubbleSyncForAgent(11,true);
Require(!currentOutput.TryDequeuePendingSpeechCompletionToken(11,out _)&&currentOutput.CaptureQueueDiagnostic(11).PendingBubbleCount==0,"actual Native cancellation clear removes associated output and token together");
currentOutput.EnqueuePendingNpcBubble(11,ia,"fallback","A",2);
Require(!currentOutput.TryDispatchPendingNpcBubbleForTts(11,false)&&shownBubbles==0,"actual bubble waits for duration before fallback policy");
Require(currentOutput.TryDispatchPendingNpcBubbleForTts(11,true)&&shownBubbles==1&&!currentOutput.TryDispatchPendingNpcBubbleForTts(11,true),"actual fallback dispatch consumes one bubble and duplicate cannot publish");
int feedMessages=TaleWorlds.Library.InformationManager.Messages;currentOutput.EnqueuePendingSceneDialogueFeed(11,"A","feed",default,true);
currentOutput.UpdatePendingSceneDialogueFeeds();Require(TaleWorlds.Library.InformationManager.Messages==feedMessages,"actual feed waits for audio completion");
currentOutput.ConvertPendingSceneDialogueFeedToTimedFlush(11,2);interactionMission.CurrentTime+=1;currentOutput.UpdatePendingSceneDialogueFeeds();Require(TaleWorlds.Library.InformationManager.Messages==feedMessages,"actual fallback feed respects Mission timer");
interactionMission.CurrentTime+=1;currentOutput.UpdatePendingSceneDialogueFeeds();currentOutput.UpdatePendingSceneDialogueFeeds();Require(TaleWorlds.Library.InformationManager.Messages==feedMessages+1,"actual feed flushes once at deadline");
currentOutput.EnqueuePendingSpeechCompletionToken(11,99);interaction.TrackPlayerInteraction(ina);interaction.ScheduleInteractionTimeoutArm(11,interaction.CaptureSpeechInteractionToken(11),5);currentOutput.ClearPendingTtsBubbleSyncQueues();
Require(currentOutput.CaptureQueueDiagnostic(11).PendingSpeechTokenCount==0&&!interaction.CaptureInteractionDiagnostic(11).HasPendingArm&&audio._ttsPlaybackOwners.Count==0,"actual queue retirement clears Audio ownership and interaction arms through named capabilities");
// Actual normal presentation -> accepted audio -> sole output queue -> interaction completion.
TtsEngine.Instance=new TtsEngine();MeetingBattleLockMissionBehavior.Trace=()=>{};
port.Dispatch=a=>outputDispatcher.Enqueue(a);
port.SceneSessionId=()=>session;port.ConversationEpoch=()=>epoch;
port.AudioDuration=currentOutput.EnqueuePendingAudioDuration;
port.HasPlaybackStarted=currentOutput.HasPlaybackStarted;
port.MarkPlaybackStarted=currentOutput.MarkPlaybackStarted;
port.UnmarkPlaybackStarted=currentOutput.UnmarkPlaybackStarted;
port.DispatchBubble=currentOutput.TryDispatchPendingNpcBubbleForTts;
var completionTrace=new List<string>();bool enterHall=false,throwFollow=false;
movement.CompletionTrace=step=>{completionTrace.Add(step);if(throwFollow&&step=="follow")throw new Exception("synthetic movement leaf failure");};
var actualCompletion=new SceneSpeechCompletionController(new SceneSpeechCompletionPorts {
 PrepareInteractionCompletion=i=>{completionTrace.Add("prepare");interaction.PrepareInteractionCompletion(i);},
 FlushDialogueFeed=i=>{completionTrace.Add("feed");currentOutput.FlushPendingSceneDialogueFeedAfterSpeech(i);},
 TryFlushLordsHallEntry=i=>{completionTrace.Add("hall");return enterHall;},
 FlushMeetingRelease=i=>completionTrace.Add("meeting"),FlushWorldMapExit=i=>completionTrace.Add("worldmap"),
 FlushAutonomyRestore=i=>completionTrace.Add("autonomy"),CleanupLipSync=i=>{completionTrace.Add("cleanup");audio.CleanupSceneLipSyncAfterPlaybackFinished(i);},
 RunStep=(step,i,action)=>{try{action();}catch{completionTrace.Add("caught:"+step);}},MainThreadQueueCount=()=>outputDispatcher.Count
},movement);
port.Finished=actualCompletion.Complete;
port.ScheduleBubbleFallback=r=>currentOutput.SchedulePendingNpcBubbleFallbackDispatch(r);
audio=new SceneAudioLipSyncController(port);audio.SubscribeTtsPlaybackEvents();
void PumpOutput(){while(outputDispatcher.TryDequeue(out var action))action();}
var actualShowPort=new SceneSpeechOutputPort {
 SanitizeUiText=t=>t,SanitizeTtsText=t=>t,BuildPatienceBadge=(n,a)=>"",NpcDisplayName=n=>n.Name,
 IsHostile=a=>false,IsTtsEnabled=()=>true,CanLipSync=(Agent a,out string reason)=>{reason="";return true;},
 ResolveHero=i=>null,ExternalHeroVoice=h=>null,EstimateTypingDuration=t=>1,Audio=()=>audio,
 RemoveHostileInteraction=i=>{},CaptureInteractionToken=interaction.CaptureSpeechInteractionToken,Report=(s,i,e)=>{},
 ClearPendingBubble=currentOutput.ClearPendingTtsBubbleSyncForAgent,ClearPendingFeed=currentOutput.ClearPendingSceneDialogueFeedForAgent,
 EnqueueCompletionToken=currentOutput.EnqueuePendingSpeechCompletionToken,EnqueueBubble=currentOutput.EnqueuePendingNpcBubble,
 ScheduleFeed=currentOutput.ScheduleNpcSpeechToMessageFeed,PublishFeedImmediately=currentOutput.PublishNpcSpeechToMessageFeedImmediately,ShowBubble=(a,t,d)=>{shownBubbles++;return true;},
 ArmInteractionTimeout=interaction.ScheduleInteractionTimeoutArm
};
interaction.TrackPlayerInteraction(ina);int beforeNormalBubble=shownBubbles,beforeNormalFeed=InformationManager.Messages;
var normalInfo=panel.ShowNpcSpeechOutput(actualShowPort,ina,ia,"actual normal reply");var normalRequest=TtsEngine.Instance.LastRequest;
Require(normalInfo.TtsAccepted&&currentOutput.CaptureQueueDiagnostic(11).PendingBubbleCount==0,"normal Show accepts without prematurely publishing prepared output");
TtsEngine.Instance.Ready(normalRequest);PumpOutput();TtsEngine.Instance.Start(normalRequest);PumpOutput();
Require(shownBubbles==beforeNormalBubble+1&&currentOutput.CaptureQueueDiagnostic(11).PendingSpeechTokenCount==1,"actual Ready and Start prepare and publish normal output once with original interaction token");
TtsEngine.Instance.Finish(normalRequest);PumpOutput();TtsEngine.Instance.Finish(normalRequest);PumpOutput();
Require(interaction.CaptureInteractionDiagnostic(11).TimeoutArmed&&InformationManager.Messages==beforeNormalFeed+1&&!audio.IsTtsPlaybackRequestCurrent(normalRequest),"normal finish closes actual output feed and interaction once and retires request");
Require(string.Join(",",completionTrace)=="prepare,feed,follow,hall,meeting,worldmap,summon,guide,autonomy,summon-launch,guide-launch,cleanup","actual completion retains follow, meeting, summon, guide, autonomy and cleanup ordering");
completionTrace.Clear();enterHall=true;throwFollow=true;
interaction.TrackPlayerInteraction(ina);panel.ShowNpcSpeechOutput(actualShowPort,ina,ia,"old interaction reply");var oldNormalRequest=TtsEngine.Instance.LastRequest;
interaction.TrackPlayerInteraction(ina);TtsEngine.Instance.Ready(oldNormalRequest);PumpOutput();TtsEngine.Instance.Start(oldNormalRequest);PumpOutput();TtsEngine.Instance.Finish(oldNormalRequest);PumpOutput();
Require(!interaction.CaptureInteractionDiagnostic(11).TimeoutArmed,"actual normal old speech cannot arm reentered interaction");
Require(string.Join(",",completionTrace)=="prepare,feed,follow,caught:follow_command,hall,summon,guide,autonomy,summon-launch,guide-launch,cleanup","actual hall early return suppresses only meeting/worldmap and isolated follow failure preserves later completion steps");
enterHall=false;throwFollow=false;

TtsEngine.Instance.SpeakAccepted=false;int beforeRejectedBubble=shownBubbles;
var rejectedInfo=panel.ShowNpcSpeechOutput(actualShowPort,ina,ia,"text fallback");
Require(!rejectedInfo.TtsAccepted&&shownBubbles==beforeRejectedBubble+1&&interaction.CaptureInteractionDiagnostic(11).HasPendingArm,"actual rejected normal Show falls back and schedules matching interaction arm");
interactionMission.CurrentTime+=1;interaction.ProcessPendingInteractionTimeoutArms();currentOutput.UpdatePendingSceneDialogueFeeds();
Require(interaction.CaptureInteractionDiagnostic(11).TimeoutArmed,"actual rejected fallback arms only after real visual duration");
audio.UnsubscribeTtsPlaybackEvents();currentOutput.ClearPendingTtsBubbleSyncQueues();
Mission.Current=null;interaction.UpdateActiveInteractionTimeouts();Require(interaction.LastTimeoutAgentCount==0&&interaction.LastTimeoutWorkItems==0,"no Mission early return retains empty scan diagnostics");
// Incoming native target identity guard runs on the real adapter and the sole local trade state.
var previousNativeCampaign=TaleWorlds.CampaignSystem.Campaign.Current;
var nativeOwner=SceneInteractionContract.Create(panel,()=>new(),()=>{},(a,b,c)=>{},()=>true);
var nativeAdapter=SceneInteractionContract.CreateTradeAdapter(nativeOwner,panel);
var nativeMission=new Mission();var nativeCharacter=new TaleWorlds.CampaignSystem.CharacterObject();
var nativeAgent=new Agent{Index=77,Mission=nativeMission,Character=nativeCharacter};
var nativeManager=new TaleWorlds.CampaignSystem.ConversationManager{IsConversationInProgress=true,OneToOneConversationCharacter=nativeCharacter,OneToOneConversationAgent=nativeAgent};
TaleWorlds.CampaignSystem.Campaign.Current=new(){ConversationManager=nativeManager};Mission.Current=nativeMission;
nativeOwner._shoutTradeActionOnly=true;nativeOwner._shoutTradeNativeManager=nativeManager;nativeOwner._shoutTradeNativeMission=nativeMission;nativeOwner._shoutTradeNativeAgent=nativeAgent;nativeOwner._shoutTradeTargetCharacterOverride=nativeCharacter;
Require(nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native participant accepts captured manager/mission/agent without scene shout frame");
TaleWorlds.CampaignSystem.Campaign.Current.ConversationManager=new();Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native switched manager rejects");TaleWorlds.CampaignSystem.Campaign.Current.ConversationManager=nativeManager;
Mission.Current=new();Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native switched mission rejects");Mission.Current=nativeMission;
nativeManager.IsConversationInProgress=false;Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"ended native conversation rejects");nativeManager.IsConversationInProgress=true;
nativeManager.OneToOneConversationCharacter=new();Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native switched participant rejects");nativeManager.OneToOneConversationCharacter=nativeCharacter;
nativeManager.OneToOneConversationAgent=new(){Index=77,Character=nativeCharacter};Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native recycled agent index rejects");nativeManager.OneToOneConversationAgent=nativeAgent;
nativeAgent.Active=false;Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native inactive agent rejects");nativeAgent.Active=true;
var formerMain=Agent.Main;Agent.Main=nativeAgent;Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native main agent rejects");Agent.Main=formerMain;
var nativeHero=new TaleWorlds.CampaignSystem.Hero();nativeCharacter.HeroObject=nativeHero;nativeOwner._shoutTradeTargetHeroOverride=nativeHero;
Require(nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native captured live hero accepts");nativeHero.IsAlive=false;Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native dead hero rejects");nativeHero.IsAlive=true;
Mission.Current=null;nativeOwner._shoutTradeNativeMission=null;nativeOwner._shoutTradeNativeAgent=null;nativeManager.OneToOneConversationAgent=null;ConversationActionBoundaryBannerlordAdapter.NativeMapContext=true;
Require(nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native truly agentless map participant accepts");ConversationActionBoundaryBannerlordAdapter.NativeMapContext=false;Require(!nativeAdapter.IsShoutTradePrimaryTargetValidForCommit(),"native missing scene participant does not become map target");
Mission.Current=presentationMission;TaleWorlds.CampaignSystem.Campaign.Current=previousNativeCampaign;
Console.WriteLine($"PASS: {assertions} production trade/presentation/audio lifecycle assertions (stubbed game/native, no files).");

SceneSpeechOutputContract.Run();

SceneAudienceToggleCases.Run();
