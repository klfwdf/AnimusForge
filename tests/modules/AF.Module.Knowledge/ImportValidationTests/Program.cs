using AnimusForge;
int n=0; void Check(bool ok,string label){if(!ok)throw new Exception(label);n++;}
KnowledgeLibraryBehavior.LoreRule Rule(string id,params string[] keys)=>new(){Id=id,Keywords=keys.ToList()};
var kb=new KnowledgeLibraryBehavior(); string error;
Check(KnowledgeImportValidationOwner.NormalizeKeywordForCompare("  a\n\t b  ")=="a b","normalize");
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(null,Rule("a"),false,out error),"unavailable");
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb,null,false,out error),"null rule");
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb,Rule(" "),false,out error),"blank id");
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb,Rule("a","KEY","key"),false,out error),"within rule duplicate");
kb.Data.Rules.Add(Rule("a","key"));
Check(KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb,Rule("a","key"),true,out error),"same id overwrite");
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb,Rule("b","key"),true,out error),"other id conflict");
KnowledgeImportSupport.Imported=new(){Rule("a","key")};
Check(KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForImport(kb,"fixture",false,out error),"skip existing id");
Check(KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForImport(kb,"fixture",true,out error),"replace existing id");
KnowledgeImportSupport.Imported=new(){Rule("b","key")};
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForImport(kb,"fixture",false,out error),"import conflicts existing");
KnowledgeImportSupport.Imported=new(){Rule("b","new"),Rule("c","NEW")};
Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForImport(kb,"fixture",true,out error),"batch duplicate keywords");
KnowledgeImportSupport.Imported=new();Check(!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForImport(kb,"fixture",false,out error),"empty batch");
Check(UnnamedPersonaImportValidationOwner.TryGetUnnamedPersonaKeyFromImportFile("/fixture/NPC__export.json",_=>throw new Exception("badJSON"))=="npc","bad JSON filename fallback");
Check(UnnamedPersonaImportValidationOwner.TryGetUnnamedPersonaKeyFromImportFile("/fixture/ignored.json",_=>" KEY ")=="key","valid key wins");
var root=Path.GetFullPath("artifacts/af2-host-terminal-closeout/line-d/importvalidation/fixtures/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);Directory.CreateDirectory(Path.Combine(root,"unnamed_persona"));
File.WriteAllText(Path.Combine(root,"one.json"),"{}");
Check(UnnamedPersonaImportValidationOwner.ValidateUnnamedPersonaKeysForImport(root,_=>"one",_=>false,out error),"single unique key");
Check(!UnnamedPersonaImportValidationOwner.ValidateUnnamedPersonaKeysForImport(root,_=>"one",_=>true,out error),"existing key prohibits overwrite");
File.WriteAllText(Path.Combine(root,"unnamed_persona","two.json"),"{}");
Check(!UnnamedPersonaImportValidationOwner.ValidateUnnamedPersonaKeysForImport(root,_=>"one",_=>false,out error),"duplicate across root and subdir");
Check(!UnnamedPersonaImportValidationOwner.ValidateUnnamedPersonaKeysForImport(root+"missing",_=>"one",_=>false,out error),"missing directory");
var display=new MemoryEditorDisplayPort {
 BuildDailyMemoryLineForPrompt=line=>line.Text,
 SanitizeWeeklyMemoryMaterialTriggers=triggers=>triggers?.ToList()??new(),
 BuildWeeklyMemoryMaterialTagLabel=tag=>tag,
 BuildCompressedMemoryBlockId=(hero,day)=>hero+":"+day,
 FormatMemoryHourRange=(start,end)=>start+"-"+end,
 BuildDevHistoryPreview=(text,max)=>text??"",
 GetCurrentGameDayIndexSafe=()=>7, GetCurrentHourOfDaySafeForPrompt=()=>9,
 ResolveCurrentMemorySceneLabel=()=>"current scene"
};
var editor=new MemoryEditorController();editor.SynchronizeGeneration(1);
var drafts=Enumerable.Range(0,81).Select(i=>new DailyMemoryDraft {GameDayIndex=i,HeroName="Hero",Lines=new(){new(){Text=i==0?"AFEF gold":"text"}}}).ToList();
var view=editor.OpenDailyPage(drafts,100,"",display);
Check(view.Page==2&&view.PageCount==3&&view.Visible.Count()==1,"daily page clamp");
Check(ReferenceEquals(view.Visible.Single(),drafts[80]),"record identity remains domain-owned");
view=editor.OpenDailyPage(drafts,0," AFEF gold ",display);Check(view.Matches.Count==1&&editor.DailyQuery=="AFEF gold","AND search and trim");
view=editor.OpenDailyPage(drafts,-9,"missing",display);Check(view.Page==0&&view.PageCount==1&&view.Matches.Count==0,"empty results");
var blocks=new List<CompressedMemoryBlock>{new(){HeroId="h",GameDayIndex=1,RichTitle="first"},new(){Id="second",RichTitle="AFEF"}};
var blockView=editor.OpenCompressedPage(blocks,0,"AFEF",display);Check(blockView.Visible.Single().Item1==2&&ReferenceEquals(blockView.Visible.Single().Item2,blocks[1]),"block display index retained");
Check(MemoryEditorProjection.GetDevCompressedMemoryBlockId(display,blocks[0])=="h:1","fallback id");
Check(MemoryEditorProjection.GetDefaultDevDailyMemoryLineHour(display,new(){GameDayIndex=7})==9,"current day default hour");
Check(MemoryEditorProjection.GetDefaultDevDailyMemoryLineHour(display,new(){GameDayIndex=6})==12,"historical default hour");
Check(MemoryEditorProjection.GetDefaultDevDailyMemoryLineScene(display,new())=="current scene","scene default");
Check(MemoryEditorProjection.BuildDevDailyMemoryLineListLabel(display,new(){GameHour=99,IsAfef=true,Text="fact"}).Contains("23"),"hour display clamp");
editor.HistoryQuery="old";editor.SynchronizeGeneration(1);Check(editor.HistoryQuery=="old","same generation retains UI state");
editor.SynchronizeGeneration(2);Check(editor.HistoryQuery==""&&editor.DailyQuery==""&&editor.CompressedQuery==""&&editor.DailyPage==0&&editor.CompressedPage==0,"load resets UI state");
long generation=1;var imports=new DeveloperImportController(()=>generation,g=>g==generation);int commits=0,cancels=0;
var choice=imports.BeginConfirmation(()=>commits++,()=>commits++,()=>cancels++);
Check(commits==0,"no preoverwrite commit");choice[2]();choice[0]();Check(commits==0&&cancels==1,"cancel keeps data");
choice=imports.BeginConfirmation(()=>commits++,()=>commits++,()=>cancels++);choice[0]();choice[0]();Check(commits==1,"one shot confirmation");
choice=imports.BeginConfirmation(()=>commits++,()=>commits++,()=>cancels++);generation++;choice[0]();Check(commits==1,"late load confirmation rejected");
var late=imports.BeginConfirmation(()=>commits++,()=>commits++,()=>cancels++);imports.BeginConfirmation(()=>{},()=>{},()=>{});late[0]();Check(commits==1,"replaced dialog rejected");
var order=new List<string>();var plan=new DeveloperImportPlan {Persona=o=>{order.Add("persona");return true;},Memory=o=>{order.Add("memory");return true;},Debt=o=>order.Add("debt"),Voice=o=>order.Add("voice-failed-skip"),Weekly=o=>order.Add("weekly"),Kingdom=o=>order.Add("kingdom"),UnnamedPersona=o=>order.Add("unnamed"),Knowledge=o=>order.Add("knowledge"),Completed=o=>order.Add(o?"complete-overwrite":"complete-skip")};
imports.Execute(plan,false,generation);Check(string.Join(",",order)=="persona,memory,debt,voice-failed-skip,weekly,kingdom,unnamed,knowledge,complete-skip","original order and partial success");
order.Clear();imports.Execute(plan,true,generation-1);Check(order.Count==0,"late prepared batch rejected");
plan.Memory=o=>false;imports.Execute(plan,true,generation);Check(order.SequenceEqual(new[]{"persona"}),"retired domain guard stops downstream");
var oldProfile=new object();var replacement=new object();var profiles=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase){{"hero",oldProfile}};var authority=profiles;
PersonaImportOwner.ApplyProfiles(ref profiles,new(){{"HERO",replacement},{"new",replacement},{"null",null},{"",replacement}},false);
Check(ReferenceEquals(authority,profiles)&&ReferenceEquals(profiles["hero"],oldProfile)&&ReferenceEquals(profiles["new"],replacement)&&!profiles.ContainsKey("null")&&!profiles.ContainsKey(""),"skip comparer and authority identity");
PersonaImportOwner.ApplyProfiles(ref profiles,new(){{"HERO",replacement}},true);Check(ReferenceEquals(profiles["hero"],replacement)&&profiles.Keys.Contains("HERO"),"overwrite removes then assigns key identity");
Dictionary<string,object> absent=null;PersonaImportOwner.ApplyProfiles(ref absent,null,true);Check(absent==null,"absent source does not create authority");
PersonaImportOwner.ApplyProfiles(ref absent,new(),true);Check(absent!=null&&absent.Comparer.Equals(EqualityComparer<string>.Default),"original default comparer on creation");
var hero=new TaleWorlds.CampaignSystem.Hero();TaleWorlds.CampaignSystem.Hero selectedHero=hero;int personaWrites=0,personaGenerate=0;
var personaPort=new PersonaEditorPort {CaptureGeneration=()=>AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation,IsCurrent=g=>g==AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation,GetSelectedHero=()=>selectedHero,SetSelectedHero=h=>selectedHero=h,GetNpcPersonaStrings=(TaleWorlds.CampaignSystem.Hero h,out string p,out string b)=>{p="old personality";b="old background";},GetNpcVoiceId=h=>"voice",ShowDevEditInquiry=h=>{},SavePersonaText=(h,p,b)=>personaWrites++,SaveVoice=(h,v)=>personaWrites++,ClearPersona=h=>personaWrites++,GeneratePersona=h=>{personaGenerate++;return Task.FromResult("generation failed");},CompleteOnMainThread=(g,operation)=>Task.FromResult(operation())};
var persona=new PersonaEditorController(personaPort);persona.SynchronizeGeneration(1);
persona.OpenDevSetPersonality(hero);var oldSubmit=DevTextEditorHelper.Confirm;Check(personaWrites==0,"persona editor waits for save");DevTextEditorHelper.Cancel();Check(personaWrites==0,"persona cancel does not write");
persona.OpenDevSetBackground(hero);AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation++;DevTextEditorHelper.Confirm("new");Check(personaWrites==0,"late persona input rejected");
persona.OpenDevSetVoiceId(hero);TaleWorlds.Library.InformationManager.Text.Confirm("new voice");Check(personaWrites==1,"voice editor invokes true port once");
persona.OpenHeroPersonaRerollConfirmation(hero,null);Check(personaGenerate==0,"reroll preconfirm no generation");TaleWorlds.Library.InformationManager.Inquiry.Cancel();Check(personaGenerate==0,"reroll cancel no generation");
persona.OpenHeroPersonaRerollConfirmation(hero,null);TaleWorlds.Library.InformationManager.Inquiry.Confirm();Check(personaGenerate==1&&personaWrites==1,"failed reroll preserves existing profile");
persona.PersonaReturnAction=()=>{};persona.SingleNpcQuery="query";persona.KnowledgeImportPicked=true;persona.SynchronizeGeneration(9);Check(persona.PersonaReturnAction==null&&persona.SingleNpcQuery==""&&!persona.KnowledgeImportPicked,"persona load resets callback and navigation");
Action pendingMemoryConfirm=null;int memoryWrites=0;
var memoryPort=new MemoryEditorPort {GetSelectedHero=()=>hero,SetSelectedHero=h=>{},LoadDialogueHistory=h=>new(){new(){GameDayIndex=1,Lines=new(){"marker-only"}}},IsMemorySourceEditorCurrent=g=>g==AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation,ClearDevDialogueHistoryData=(h,g)=>{if(g!=AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation)return false;memoryWrites++;return true;},ShowDevEditInquiry=h=>{}};
var memoryUi=new MemoryEditorController(memoryPort,display);memoryUi.ConfirmDevClearAllDialogueHistory(hero);pendingMemoryConfirm=TaleWorlds.Library.InformationManager.Inquiry.Confirm;Check(memoryWrites==0,"history delete waits for explicit confirmation");AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation++;pendingMemoryConfirm();Check(memoryWrites==0,"load retires destructive memory callback");memoryUi.ConfirmDevClearAllDialogueHistory(hero);TaleWorlds.Library.InformationManager.Inquiry.Confirm();Check(memoryWrites==1,"history explicit clear invokes true data capability");
var sharedUi=new DeveloperEditorSession();sharedUi.SynchronizeGeneration(1);sharedUi.SelectedHero=hero;sharedUi.EditableHeroes.Add(hero);sharedUi.SynchronizeGeneration(1);Check(ReferenceEquals(sharedUi.SelectedHero,hero)&&sharedUi.EditableHeroes.Count==1,"shared UI selection persists only within generation");sharedUi.SynchronizeGeneration(2);Check(sharedUi.SelectedHero==null&&sharedUi.EditableHeroes.Count==0,"shared selection/list retired on load");
var legacy=new MyBehavior.DialogueDay {GameDayIndex=1,Lines=new(){"original"}};
memoryPort.LoadDialogueHistory=h=>new(){legacy};memoryPort.TryApplyDevDialogueHistoryLineDataMutation=(h,day,index,input,g)=>{if(g!=AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation)return false;memoryWrites++;return true;};
memoryUi.OpenDevEditLine(hero,1,0);var legacySave=DevTextEditorHelper.Confirm;AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation++;legacySave("new");Check(memoryWrites==1,"legacy long text late input rejected before domain call");
memoryUi.OpenDevEditLineInput(hero,1,0,"original","day1");legacySave=TaleWorlds.Library.InformationManager.Text.Confirm;AnimusForge.Refactor.Runtime.SaveRuntimeGuard.Generation++;legacySave("new");Check(memoryWrites==1,"legacy fallback text late input rejected");

PersonaImportOwner.ApplySingleProfile(ref profiles,"hero",null);Check(!profiles.ContainsKey("hero")&&ReferenceEquals(profiles,authority),"single null import deletes within same authority");
PersonaImportOwner.ApplySingleProfile(ref profiles,"HERO",replacement);Check(ReferenceEquals(profiles["hero"],replacement),"single replacement profile identity");
int transfers=0,returns=0;long transferGeneration=1;
var transferPort=new DeveloperImportUiPort {CaptureGeneration=()=>transferGeneration,IsCurrent=g=>g==transferGeneration,ReturnToDevRootMenu=()=>returns++,ImportPersonaData=f=>transfers++,ImportSingleNpcDebtData=(f,id)=>{Check(id=="hero","single route trims hero id");transfers++;}};
var transfer=new DeveloperImportUiController(transferPort,new DeveloperImportController(()=>transferGeneration,g=>g==transferGeneration));
transfer.OpenFolderPicker("import",false,MyBehavior.ExportImportScope.PersonalityBackground,null,null);
var folderSelection=TaleWorlds.Core.MBInformationManager.Selection;folderSelection.Confirm(new(){new("__input__","",null)});var pendingFolder=TaleWorlds.Library.InformationManager.Text.Confirm;transferGeneration++;pendingFolder("fixture");Check(transfers==0&&returns==0,"late folder text does not dispatch or navigate");
transfer.ResolveAndRunExportImport(false,MyBehavior.ExportImportScope.PersonalityBackground,"fixture");Check(transfers==1,"scope routes to exact domain import capability");
transfer.ResolveAndRunExportImportForHero(false,MyBehavior.ExportImportScope.Debt,"fixture"," hero ");Check(transfers==2,"single route consumes debt capability");
transfer.ShowOverwriteExportInquiry("export","existing",()=>transfers++,()=>transfers++,()=>returns++);Check(transfers==2,"export waits preoverwrite confirmation");folderSelection=TaleWorlds.Core.MBInformationManager.Selection;folderSelection.Cancel();folderSelection.Confirm(new(){new("__overwrite__","",null)});Check(transfers==2&&returns==1,"export cancel retires overwrite callback");
transfer.ShowOverwriteExportInquiry("export","existing",()=>transfers++,()=>{},()=>{});folderSelection=TaleWorlds.Core.MBInformationManager.Selection;folderSelection.Confirm(new(){new("__overwrite__","",null)});folderSelection.Confirm(new(){new("__overwrite__","",null)});Check(transfers==3,"export duplicate confirm once");
transfer.ShowDuplicateImportInquiry("import","existing",()=>transfers++,()=>transfers++,()=>{});folderSelection=TaleWorlds.Core.MBInformationManager.Selection;transferGeneration++;folderSelection.Confirm(new(){new("__overwrite__","",null)});Check(transfers==3,"late import overwrite rejected");
int reloadApplies=0;transferPort.TryBuildDatabaseReloadPlan=(string f,out MyBehavior.DatabaseReloadPlan p,out string error)=>{p=new(){ImportDirectory="fixture"};error="";return true;};transferPort.ApplyDatabaseReloadPlan=(MyBehavior.DatabaseReloadPlan p,out string detail)=>{reloadApplies++;detail="fixture";return true;};
transfer.BeginDatabaseReloadPreflight("fixture",()=>{});var reloadConfirm=TaleWorlds.Library.InformationManager.Inquiry;Check(reloadApplies==0,"reload preflight no commit before confirm");reloadConfirm.Cancel();reloadConfirm.Confirm();Check(reloadApplies==0,"reload cancel retires prepared plan");transfer.BeginDatabaseReloadPreflight("fixture",()=>{});reloadConfirm=TaleWorlds.Library.InformationManager.Inquiry;transferGeneration++;reloadConfirm.Confirm();Check(reloadApplies==0,"reload load retires prepared plan");transfer.BeginDatabaseReloadPreflight("fixture",()=>{});reloadConfirm=TaleWorlds.Library.InformationManager.Inquiry;reloadConfirm.Confirm();reloadConfirm.Confirm();Check(reloadApplies==1,"reload UI duplicate confirm one commit");
Console.WriteLine($"PASS: {n} production import validation / complete Memory and Persona editor lifecycle assertions (synthetic fixtures, stubbed game/domain inputs).");
