using AnimusForge;using TaleWorlds.CampaignSystem;using TaleWorlds.Core;using TaleWorlds.Library;using static AnimusForge.MyBehavior;
int assertions=0;void Check(bool value,string label){if(!value)throw new Exception(label);assertions++;}
long generation=1;int writes=0,returns=0;string world="initial",submitted=null;
var kingdom=new Kingdom {StringId="k",Name="Kingdom"};Kingdom.All.Add(kingdom);
var records=Enumerable.Range(0,30).Select(i=>new EventRecordEntry {EventId="e"+i,Title="title"+i,EventKind="world",Materials=new()}).ToList();
var port=new EventEditorPort {
 CaptureGeneration=()=>generation,IsCurrent=g=>g==generation,WorldOpeningSummary=()=>world,PromptProfileLabel=()=>"profile",CountConfiguredOpeningSummaries=()=>0,StabilityDefault=50,EventRecords=()=>records,
 GetKingdomDisplayName=(k,f)=>k?.Name??f,GetClanDisplayName=c=>c?.Name??"clan",FormatKingdomRebellionChance=f=>f.ToString(),TranslateEventKindForDev=s=>s,ResolveKingdomDisplay=s=>s,TranslateEventMaterialTypeForDev=s=>s,
 GetKingdomStabilityValue=k=>50,SanitizeEventRecordEntries=s=>s,EnsureWeekZeroOpeningSummaryEvents=b=>{},FindKingdomById=id=>kingdom,GetKingdomOpeningSummary=k=>"opening",SaveKingdomOpeningSummary=(k,s)=>{writes++;submitted=s;},AppendDevNpcActionField=(b,l,v)=>b.AppendLine(l+":"+v),
 SetDeveloperWorldOpeningSummary=(s,g)=>{writes++;submitted=s;world=s.Trim();return true;},ClearDeveloperOpeningSummaries=g=>{writes++;world="";return true;},
 ApplyDeveloperEventTitle=(e,s,g)=>{writes++;submitted=s;return e;},ApplyDeveloperEventReport=(e,s,g)=>{writes++;submitted=s;return e;},OpenDevKingdomStabilityLabMenu=()=>returns++
};
var ui=new EventEditorController(port);
Check(EventEditorProjection.BuildDevSummaryPreview("  a\n b\t c  ",40)=="a b c","preview whitespace projection");Check(EventEditorProjection.BuildDevSummaryPreview("abcdef",3)=="abc...","preview truncation preserved");
Check(EventEditorProjection.BuildDevEventRecordMenuLabel(port,records[0])=="世界周报","world label unchanged");Check(EventEditorProjection.BuildDevEventMaterialItemLabel(port,new(){MaterialType="type",SnapshotText="fallback"})=="[type] fallback","material snapshot fallback");
ui.OpenDevEventViewerMenu(99);var page=MBInformationManager.Last;Check(ui.RecordPage==2&&page.Options.Count(o=>o.Identifier is EventRecordEntry)==2,"14 record page clamp");page.Confirm(new(){new(records[29],"",null)});Check(ReferenceEquals(ui.RecordSelection,records[29]),"selection remains original record identity");
ui.SynchronizeGeneration(1);ui.RecordSelection=records[0];ui.RecordPage=2;ui.MaterialPage=1;ui.SynchronizeGeneration(2);Check(ui.RecordSelection==null&&ui.RecordPage==0&&ui.MaterialPage==0,"load resets UI selection/pages");
ui.OpenDevEventEditorMenu();var oldSelection=MBInformationManager.Last;generation++;oldSelection.Confirm(new(){new("kingdom_stability_lab","",null)});Check(returns==0,"late menu callback rejected");
ui.OpenDevEditEventRecordTitle(records[0],0);var oldText=DevTextEditorHelper.Confirm;generation++;oldText("new");Check(writes==0,"late record title cannot write");
ui.OpenDevEditEventRecordReport(records[0],0);DevTextEditorHelper.Confirm("  verbatim  ");Check(writes==1&&submitted=="  verbatim  ","record edit passes input to true domain once");
ui.OpenDevEditWorldOpeningSummary();DevTextEditorHelper.Cancel();Check(writes==1,"world editor cancel no data commit");ui.OpenDevEditWorldOpeningSummary();DevTextEditorHelper.Confirm(" world ");Check(writes==2&&world=="world","world edit canonical capability");
ui.OpenDevEditKingdomOpeningSummary(kingdom);oldText=DevTextEditorHelper.Confirm;generation++;oldText("late");Check(writes==2,"late kingdom opening edit rejected");
ui.ConfirmClearAllEventOpeningSummaries();var confirm=InformationManager.Inquiry;Check(writes==2,"clear no preconfirmation write");confirm.Cancel();confirm.Confirm();Check(writes==2,"cancel retires clear ticket");
ui.ConfirmClearAllEventOpeningSummaries();confirm=InformationManager.Inquiry;generation++;confirm.Confirm();Check(writes==2,"load retires clear ticket");ui.ConfirmClearAllEventOpeningSummaries();confirm=InformationManager.Inquiry;confirm.Confirm();confirm.Confirm();Check(writes==3,"duplicate clear commits once");
ui.ConfirmClearAllEventOpeningSummaries();confirm=InformationManager.Inquiry;ui.ConfirmClearAllEventOpeningSummaries();confirm.Confirm();Check(writes==3,"replacement clear retires previous confirmation");
int stabilityWrites=0,forced=0,evaluations=0,resolutions=0,lastStability=-1;bool forceFlag=false,executeFlag=true;
var clan=new Clan{Name="Clan"};var candidate=new KingdomRebellionCandidateInfo{Clan=clan,Eligible=true,ClanName="Clan"};
var lp=new KingdomStabilityLabPort {
 CaptureGeneration=()=>generation,IsCurrent=g=>g==generation,GetKingdomDisplayName=(k,f)=>k?.Name??f,GetClanDisplayName=c=>c?.Name??"clan",GetHeroDisplayName=h=>"hero",FormatKingdomRebellionChance=f=>f.ToString(),GetKingdomStabilityValue=k=>50,GetKingdomStabilityTierText=v=>"tier",GetKingdomRebellionWeeklyChance=v=>.1f,GetKingdomStabilityRelationTargetOffset=v=>1,GetKingdomStabilityWeeklyBalancingDelta=v=>1,CountActiveKingdomClansForLowClanCountRule=k=>2,GetLowClanCountRoyalDomainLoyaltyAdjustment=(v,n)=>1,FormatKingdomStabilityRelationOffsetText=n=>n.ToString(),EvaluateKingdomRebellionCandidates=(k,f)=>{evaluations++;return new(){candidate};},ClampKingdomStabilityValue=v=>Math.Clamp(v,0,100),SetKingdomStabilityValue=(k,v)=>{stabilityWrites++;lastStability=v;},FindKingdomById=id=>kingdom,GetCurrentGameDayIndexSafe=()=>15,OpenDevEventEditorMenu=()=>{},
 ResolveKingdomRebellion=(k,w,executeAction,forceTrigger)=>{resolutions++;executeFlag=executeAction;forceFlag=forceTrigger;return new(){Kingdom=k,SelectedClan=clan,Candidates=new(){candidate}};},StartDevForcedKingdomRebellionAsync=(k,c,w,r,t,castle,followers)=>forced++
};
var lab=new KingdomStabilityLabController(lp);
lab.OpenDevEditKingdomStability(kingdom);InformationManager.Text.Confirm("bad");Check(stabilityWrites==0,"invalid stability text does not mutate");InformationManager.Text.Confirm("999");Check(stabilityWrites==1&&lastStability==100,"stability clamp retains core policy");
lab.OpenDevEditKingdomStability(kingdom);var oldStability=InformationManager.Text.Confirm;generation++;oldStability("20");Check(stabilityWrites==1,"late stability text rejected");
lab.RunDevKingdomRebellionTest(kingdom);Check(resolutions==1&&!executeFlag&&!forceFlag&&forced==0,"test remains nonexecuting normal probability mode");
lab.ConfirmForceDevKingdomRebellion(kingdom);confirm=InformationManager.Inquiry;Check(forced==0,"forced action requires explicit confirmation");confirm.Cancel();confirm.Confirm();Check(forced==0&&resolutions==1,"forced cancel retires ticket without evaluation");
lab.ConfirmForceDevKingdomRebellion(kingdom);confirm=InformationManager.Inquiry;generation++;confirm.Confirm();Check(forced==0,"load retires forced action ticket");lab.ConfirmForceDevKingdomRebellion(kingdom);confirm=InformationManager.Inquiry;confirm.Confirm();confirm.Confirm();Check(forced==1&&resolutions==2&&!executeFlag&&forceFlag,"forced dispatch once via true domain with original flags");
PlayerKingdomRebellionImmunity.Protected=true;evaluations=0;Check(lab.BuildDevKingdomStabilityDetailText(kingdom).Contains("免疫")&&evaluations==0,"immune display does not evaluate candidates");
Console.WriteLine($"PASS: {assertions} actual Event / Stability Lab UI lifecycle assertions (stubbed game and domain capabilities, no live data writes).");
