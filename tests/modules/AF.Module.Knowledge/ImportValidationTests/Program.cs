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
Console.WriteLine($"PASS: {n} production import validation / Memory editor / import lifecycle assertions (synthetic fixtures, stubbed game/domain inputs).");
