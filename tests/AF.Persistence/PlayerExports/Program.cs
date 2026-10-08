using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AnimusForge;

// Contract for the J05d PlayerExports owners, compiled from the production files.
internal static class Program
{
    private static int _checks;
    private static void Check(bool c, string m) { _checks++; if (!c) throw new Exception("FAIL: " + m); }
    private sealed class Row { public int Day; public string Text; }

    private static void Main()
    {
        NpcFileNames();
        ImportSources();
        InstalledWorldbooks();
        Store();
        if (Environment.GetEnvironmentVariable("AF_H7_PERSONA_FORMATS")=="1") { PersonaFormats(); MemoryFormats(); OnboardingBaseline(); PackageExportFormats(); PackageImportFormats(); RebellionEditorUi(); OnboardingTerminal(); OnboardingFiveDomainTerminal(); }
        Console.WriteLine("PASS player-exports checks=" + _checks);
    }

    private static void PersonaFormats()
    {
        var heroes = TaleWorlds.CampaignSystem.Hero.AllAliveHeroes;
        heroes.Clear();
        heroes.Add(new TaleWorlds.CampaignSystem.Hero { StringId="h1", Name="Alice" });
        heroes.Add(new TaleWorlds.CampaignSystem.Hero { StringId="h2", Name="Bob" });
        string dir = Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP"), "persona-format");
        Directory.CreateDirectory(dir);
        string personas = Path.Combine(dir, "personality_background");
        Directory.CreateDirectory(personas);
        string valid = Path.Combine(personas,"h1__Alice.json");
        PlayerExportsStore.WriteJson(valid,new MyBehavior.NpcPersonaProfile { Personality="new", Background="bio" });
        Check(NpcDataIdentityFileAdapter.TryResolveNpcDataFileHeroIdForImport(valid,out string id,out _) && id=="h1", "actual identity accepts matching id/name");
        Check(NpcDataIdentityFileAdapter.TryResolveNpcDataFileHeroIdForImport("old__Alice.json",out id,out _) && id=="h1", "actual identity remaps unique name");
        Check(!NpcDataIdentityFileAdapter.TryResolveNpcDataFileHeroIdForImport("CharacterObject_old__NPC.json",out id,out _), "auto id placeholder cannot match");
        heroes.Add(new TaleWorlds.CampaignSystem.Hero { StringId="h3", Name="Alice" });
        Check(!NpcDataIdentityFileAdapter.TryResolveNpcDataFileHeroIdForImport("CharacterObject_old__Alice.json",out id,out _), "duplicate name refuses unstable id");
        heroes.RemoveAt(2);
        Check(NpcDataIdentityFileAdapter.FindNpcJsonByHeroId(personas,"h1")==valid, "actual single locator returns matched file");
        var scope = new NpcDataIdentityFileAdapter.LookupScope();
        Check(scope.TryResolveNpcDataFileHeroIdForImport(valid,out id,out _) && scope.TryResolveNpcDataFileHeroIdForImport(valid,out id,out _) && scope.IdentityFiles==2 && scope.IdLookups==1, "operation identity memoized once per distinct id, files tracked");
        Check(scope.ResolveHeroNameForNpcDataFile("h1")=="Alice" && scope.ResolveHeroNameForNpcDataFile("h1")=="Alice" && scope.FilenameLookups==1, "operation filename memo reused across domains");
        Check(new NpcDataIdentityFileAdapter.LookupScope().ResolveHeroNameForNpcDataFile("h1")=="Alice", "next operation gets fresh lookup scope, not permanent cross-generation cache");
        var state = new PersonaProfileStateOwner();
        var original = new MyBehavior.NpcPersonaProfile { Personality="old" };
        state.Profiles["h1"]=original;
        long generation=1;
        Action overwrite=null,skip=null,cancel=null;
        var adapter = new PersonaProfileImportExportAdapter(state,()=>generation,g=>g==generation,
            (title,text,a,b,c)=>{overwrite=a;skip=b;cancel=c;});
        var prepared=adapter.PreparePersonaDirectory(dir,new NpcDataIdentityFileAdapter.LookupScope(),out int dup,out int total);
        Check(dup==1 && total==1 && prepared["h1"].HeroName=="Alice" && ReferenceEquals(state.Profiles["h1"],original), "actual package profile preparation has original counts and no live write");
        MyBehavior.LegacyImportPersonaFixture = name => adapter.ImportPersonaData(name);
        bool queuedAcknowledgement=LegacyOnboardingReflection.InvokePrivateImport(new MyBehavior(),"ImportPersonaData",dir);
        Check(queuedAcknowledgement && overwrite!=null && ReferenceEquals(state.Profiles["h1"],original), "baseline original reflection acknowledges queued duplicate prompt before actual domain commit");
        adapter.ImportPersonaData(dir);
        Check(overwrite!=null && ReferenceEquals(state.Profiles["h1"],original), "confirmation is prepared without commit");
        skip();
        Check(ReferenceEquals(state.Profiles["h1"],original), "skip preserves live profile reference");
        adapter.ImportPersonaData(dir);
        generation++;
        overwrite();
        Check(ReferenceEquals(state.Profiles["h1"],original), "changed generation rejects prepared overwrite");
        adapter.ImportPersonaData(dir);
        cancel();
        Check(ReferenceEquals(state.Profiles["h1"],original), "cancel has no state write");
        adapter.ImportPersonaData(dir);
        overwrite();
        Check(state.Profiles["h1"].Personality=="new" && state.Profiles["h1"].HeroId=="h1" && state.Profiles["h1"].HeroName=="Alice", "actual domain commit stamps original profile metadata");
        File.WriteAllText(valid,"{bad json");
        adapter.ImportSingleNpcPersonaData(valid,"h1");
        Check(!state.Profiles.ContainsKey("h1"), "legacy malformed single import deletes existing profile (not silently corrected)");
        adapter.ImportPersonaData(Path.Combine(dir,"missing"));
        Check(state.Profiles.Count==0, "missing directory does not mutate authority");
        state.Profiles["h1"]=new MyBehavior.NpcPersonaProfile { Personality="exported" };
        adapter.ExportSingleNpcPersonaData("single-persona-h7","h1");
        string export = Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"single-persona-h7","personality_background","h1__Alice.json");
        Check(PlayerExportsStore.ReadJson<MyBehavior.NpcPersonaProfile>(export)?.Personality=="exported", "actual single export publishes original schema through real DataPaths/atomic package");
        adapter.ExportPersonaData("all-persona-h7");
        export = Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"all-persona-h7","personality_background");
        Check(Directory.GetFiles(export,"*.json").Length==1, "actual persona-only export has exact files");
    }

    private static void OnboardingTerminal()
    {
        string dir=Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP"),"onboarding-baseline");
        var profiles=new PersonaProfileStateOwner(); var old=new MyBehavior.NpcPersonaProfile{Personality="old"};profiles.Profiles["h1"]=old;
        bool current=true;Action overwrite=null,skip=null,cancel=null;int unlocked=0,rejected=0,tail=0;
        var adapter=new PersonaProfileImportExportAdapter(profiles,()=>1,_=>current,(t,m,o,s,c)=>{overwrite=o;skip=s;cancel=c;});
        var coordinator=new OnboardingDatabaseImportController();
        coordinator.Run(()=>current,new Action<OnboardingImportStep>[] {step=>adapter.ImportPersonaDataScoped(dir,step)},()=>{tail++;return true;},()=>unlocked++,r=>rejected++);
        Check(unlocked==0 && tail==0 && overwrite!=null && ReferenceEquals(profiles.Profiles["h1"],old),"actual scoped Persona pending does not continue/unlock");
        overwrite();Check(unlocked==1 && tail==1 && !ReferenceEquals(profiles.Profiles["h1"],old),"actual overwrite terminal continues once after sameauthority commit");
        overwrite();skip();cancel();Check(unlocked==1 && tail==1 && rejected==0,"duplicate terminal callbacks cannot mutate or receive twice");
        var accepted=profiles.Profiles["h1"];coordinator.Run(()=>current,new Action<OnboardingImportStep>[] {step=>adapter.ImportPersonaDataScoped(dir,step)},()=>true,()=>unlocked++,r=>rejected++);skip();Check(unlocked==2&&ReferenceEquals(profiles.Profiles["h1"],accepted),"actual legal skip completes without replacing existing profile");
        coordinator.Run(()=>current,new Action<OnboardingImportStep>[] {step=>adapter.ImportPersonaDataScoped(dir,step)},()=>true,()=>unlocked++,r=>rejected++);cancel();Check(unlocked==2&&rejected==1,"actual confirmation cancel cannot unlock");
        coordinator.Run(()=>current,new Action<OnboardingImportStep>[] {step=>adapter.ImportPersonaDataScoped(dir,step)},()=>true,()=>unlocked++,r=>rejected++);current=false;overwrite();Check(unlocked==2&&rejected==2&&ReferenceEquals(profiles.Profiles["h1"],accepted),"old generation callback retires before mutation/unlock");current=true;
        ShoutUtils.ExistingKeys.Add("villager");
        coordinator.Run(()=>current,new Action<OnboardingImportStep>[] {step=>adapter.ImportUnnamedPersonaDataScoped(dir,step)},()=>true,()=>unlocked++,r=>rejected++);Check(unlocked==2&&rejected==3,"actual unnamed duplicate-key rejection cannot be inferred successful from preloaded voice");
        ShoutUtils.ExistingKeys.Clear();
        var order=new List<int>();coordinator.Run(()=>current,Enumerable.Range(0,5).Select<int,Action<OnboardingImportStep>>(i=>step=>{order.Add(i);step.Finish(OnboardingImportResult.Completed);}).ToArray(),()=>{order.Add(5);return true;},()=>unlocked++,r=>rejected++);Check(string.Join(",",order)=="0,1,2,3,4,5"&&unlocked==3,"event coordinator preserves five step order before kingdom/voice tail");
        var committed=profiles.Profiles["h1"];coordinator.Run(()=>current,new Action<OnboardingImportStep>[] {step=>step.Finish(OnboardingImportResult.Completed),step=>step.Finish(OnboardingImportResult.Failed)},()=>true,()=>unlocked++,r=>rejected++);Check(unlocked==3&&rejected==4&&ReferenceEquals(profiles.Profiles["h1"],committed),"later failure does not rollback previously committed domain");
    }

    private static void OnboardingFiveDomainTerminal()
    {
        string dir=Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP"),"onboarding-baseline");
        Directory.CreateDirectory(Path.Combine(dir,"knowledge"));
        File.WriteAllText(Path.Combine(dir,"knowledge","KnowledgeRules.json"),"{\"Rules\":[{\"Id\":\"rule\",\"Keywords\":[\"key\"]}]}");
        File.WriteAllText(Path.Combine(dir,"voice_mapping","VoiceMapping.json"),"{\"male_young\":[\"voice-new\"],\"fallback\":\"fallback-new\"}");
        File.WriteAllText(Path.Combine(dir,"event_data","WorldOpeningSummary.json"),"{\"Summary\":\"new-world\"}");
        var state=new PersonaProfileStateOwner();state.Profiles["h1"]=new(){Personality="old"};
        Action overwrite=null,skip=null,cancel=null;string pending="";var starts=new List<string>();bool current=true;int unlocked=0,returned=0,kingdom=0,voicePersist=0;
        Action<string,string,Action,Action,Action> inquiry=(title,text,o,s,c)=>{pending=title;overwrite=o;skip=s;cancel=c;};
        var persona=new PersonaProfileImportExportAdapter(state,()=>1,_=>current,inquiry);
        var kb=new KnowledgeLibraryBehavior();kb.ExistingRuleIds.Add("rule");KnowledgeLibraryBehavior.Instance=kb;
        var knowledge=new KnowledgeImportExportAdapter(inquiry);var voice=new VoicePersonaImportExportAdapter(inquiry,_=>voicePersist++);
        var weekly=new WeeklyEventImportExportAdapter(inquiry);weekly._weekly.WorldOpening="old-world";
        VoiceMapper.Ready=true;ShoutUtils.ExistingKeys.Clear();
        var controller=new OnboardingDatabaseImportController();
        Func<string,Action<OnboardingImportStep>[]> steps=folder=>new Action<OnboardingImportStep>[] {
          step=>{starts.Add("persona");persona.ImportPersonaDataScoped(folder,step);},
          step=>{starts.Add("unnamed");persona.ImportUnnamedPersonaDataScoped(folder,step);},
          step=>{starts.Add("knowledge");knowledge.ImportKnowledgeDataScoped(folder,step);},
          step=>{starts.Add("voice");voice.ImportVoiceMappingDataScoped(folder,step);},
          step=>{starts.Add("weekly");weekly.ImportEventDataScoped(folder,step);}};
        Func<string,OnboardingDatabaseImportController.KingdomInspection> inspect=folder=>new(){Available=true,Valid=true,Total=1,Import=()=>{kingdom++;return true;}};
        controller.Begin(dir,()=>current,inspect,steps,()=>VoiceMapper.GetTotalVoiceCount()>0||!string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice()),()=>unlocked++,()=>returned++);
        Check(unlocked==0&&kingdom==0&&starts.SequenceEqual(new[]{"persona"}),"whole actual Begin waits Persona before later domains/card/voice tail");
        overwrite();Check(unlocked==0&&starts.SequenceEqual(new[]{"persona","unnamed","knowledge"}),"actual Persona/Unnamed application reaches real Knowledge duplicate confirmation only");
        skip();Check(unlocked==0&&kb.ImportAttempts==1&&starts.Last()=="voice","actual Knowledge validate/import fallback legal skip completes before Voice prompt");
        skip();Check(unlocked==0&&voicePersist==1&&VoiceMapper.GetFallbackVoice()=="fallback-new"&&starts.Last()=="weekly","actual Voice file/JSON/merge application completes before Weekly prompt");
        overwrite();Check(unlocked==1&&kingdom==1&&returned==0&&weekly._weekly.WorldOpening=="new-world","actual five domain completed callbacks precede kingdom+sameVoice final legal check/unlock");
        overwrite();skip();cancel();Check(unlocked==1&&kingdom==1,"whole actual final duplicate callbacks cannot repeat tail/unlock");
        // A true Voice file parse failure with pre-existing voice must stop, not use presence as success.
        File.WriteAllText(Path.Combine(dir,"voice_mapping","VoiceMapping.json"),"{bad");VoiceMapper.Ready=true;starts.Clear();
        controller.Begin(dir,()=>current,inspect,steps,()=>true,()=>unlocked++,()=>returned++);overwrite();skip();overwrite();
        Check(unlocked==1&&kingdom==1&&returned==1&&starts.Last()=="voice"&&VoiceMapper.GetTotalVoiceCount()>0,"actual bad Voice JSON with preloaded mapping fails terminal without tail/unlock; earlier domains stay committed");
        kb.RejectRules=true;starts.Clear();controller.Begin(dir,()=>current,inspect,steps,()=>true,()=>unlocked++,()=>returned++);overwrite();overwrite();Check(unlocked==1&&returned==2&&starts.Last()=="knowledge","actual Knowledge bulk+single rejection fails terminal before Voice");kb.RejectRules=false;
        File.WriteAllText(Path.Combine(dir,"knowledge","KnowledgeRules.json"),"{\"Rules\":[{\"Id\":\"rule\",\"Keywords\":[\"key\"]},{\"Id\":\"bad-rule\",\"Keywords\":[\"bad-key\"]}]}");
        kb.RejectBulk=true;kb.RejectSingleId="bad-rule";OnboardingImportResult acceptedResult=OnboardingImportResult.PendingConfirmation;
        knowledge.ImportKnowledgeDataScoped(dir,new OnboardingImportStep(()=>true,r=>acceptedResult=r));overwrite();Check(acceptedResult==OnboardingImportResult.Completed,"actual Knowledge bulk-fallback one-rule success remains legal partial completion, no all-files requirement");kb.RejectBulk=false;kb.RejectSingleId=null;
        File.WriteAllText(Path.Combine(dir,"voice_mapping","VoiceMapping.json"),"{}");VoiceMapper.Ready=false;int noVoiceUnlocked=0, noVoiceRejected=0;
        controller.Run(()=>true,new Action<OnboardingImportStep>[] {step=>voice.ImportVoiceMappingDataScoped(dir,step)},()=>VoiceMapper.GetTotalVoiceCount()>0||!string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice()),()=>noVoiceUnlocked++,r=>noVoiceRejected++);
        Check(noVoiceUnlocked==0&&noVoiceRejected==1,"actual valid empty Voice import completes domain but original loaded-voice final legality still refuses unlock");
        // Source/UI entry and a replaced operation cannot accept a callback from the older inquiry.
        controller.Begin(dir,()=>current,inspect,steps,()=>true,()=>unlocked++,()=>returned++);var oldOverwrite=overwrite;
        controller.Begin(Path.Combine(dir,"missing"),()=>current,inspect,steps,()=>true,()=>unlocked++,()=>returned++);oldOverwrite();Check(unlocked==1&&returned==3,"invalid replacement preflight retires old pending callback without old feedback/unlock");
        int liveReads=0;var scoped=new OnboardingImportStep(()=>{liveReads++;return true;},r=>{});var guarded=scoped.Guard(()=>throw new Exception("must never run off UI thread"));var thread=new System.Threading.Thread(()=>{guarded();scoped.Finish(OnboardingImportResult.Completed);});thread.Start();thread.Join();Check(liveReads==0,"off-thread result/apply is not accepted and never reads live owner/game guards");
    }

    private static void OnboardingBaseline()
    {
        string dir=Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP"),"onboarding-baseline");
        foreach(string name in new[]{"personality_background","unnamed_persona","knowledge/rules","voice_mapping","event_data","kingdom_profiles"}) Directory.CreateDirectory(Path.Combine(dir,name));
        PlayerExportsStore.WriteJson(Path.Combine(dir,"personality_background","h1__Alice.json"),new MyBehavior.NpcPersonaProfile{Personality="new"});
        PlayerExportsStore.WriteJson(Path.Combine(dir,"unnamed_persona","villager.json"),new MyBehavior.UnnamedPersonaSingleJson{Key="villager",Personality="new"});
        foreach(string file in new[]{"knowledge/rules/r.json","voice_mapping/VoiceMapping.json","event_data/WorldOpeningSummary.json","event_data/KingdomOpeningSummaries.json","kingdom_profiles/KingdomProfiles.json"}) File.WriteAllText(Path.Combine(dir,file),"{}");
        var profiles=new PersonaProfileStateOwner(); var old=new MyBehavior.NpcPersonaProfile{Personality="old"};profiles.Profiles["h1"]=old;
        Action pending=null;
        var adapter=new PersonaProfileImportExportAdapter(profiles,()=>1,g=>true,(title,text,a,b,c)=>pending=a);
        MyBehavior.LegacyImportPersonaFixture=adapter.ImportPersonaData;
        MyBehavior.LegacyImportUnnamedFixture=adapter.ImportUnnamedPersonaData;
        TaleWorlds.CampaignSystem.Campaign.Current=new TaleWorlds.CampaignSystem.Campaign{My=new MyBehavior(),Kingdom=new KingdomStrategicProfileBehavior()};
        ShoutUtils.ExistingKeys.Clear();VoiceMapper.Ready=true;
        var terminal=new LegacyOnboardingReflection();terminal.TryImportRequiredSetAndUnlock(dir,()=>{});
        if (Environment.GetEnvironmentVariable("AF_ONBOARDING_TDD_RED")=="1") Check(!terminal.Unlocked,"approved terminal contract: pending confirmation must not unlock");
        Check(terminal.Unlocked && pending!=null && ReferenceEquals(profiles.Profiles["h1"],old),"baseline full original Onboarding unlocks while actual Persona confirmation is still pending");
        Console.WriteLine("BASELINE pendingConfirmation=true originalTerminalUnlocked=true personaCommitted=false");
        profiles.Profiles.Clear();pending=null;ShoutUtils.ExistingKeys.Add("villager");TaleWorlds.Library.InformationManager.Messages.Clear();
        terminal=new LegacyOnboardingReflection();terminal.TryImportRequiredSetAndUnlock(dir,()=>{});
        Check(terminal.Unlocked && TaleWorlds.Library.InformationManager.Messages.Exists(x=>x.Contains("Key 冲突")),"baseline full original terminal unlocks after actual unnamed format/validation failure with preloaded Voice");
        Console.WriteLine("BASELINE unnamedDomainFailure=true preloadedVoice=true originalTerminalUnlocked=true");
        ShoutUtils.ExistingKeys.Clear();
    }

    private static void MemoryFormats()
    {
        string dir=Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP"),"memory-format");
        string compressed=Path.Combine(dir,"compressed_memory");Directory.CreateDirectory(compressed);
        var memory=new MemoryBusinessStateOwner();
        long generation=1;Action overwrite=null,skip=null,cancel=null;int dirty=0;
        var adapter=new MemoryHistoryImportExportAdapter(memory,()=>generation,g=>g==generation,
            id=>dirty++, (title,text,a,b,c)=>{overwrite=a;skip=b;cancel=c;});
        string file=Path.Combine(compressed,"h1__Alice.json");
        File.WriteAllText(file,"{bad json");adapter.ImportSingleNpcDialogueHistoryData(file,"h1");
        Check(memory.Drafts==null && dirty==0,"malformed single compressed file does not clear memory");
        adapter.ImportDialogueHistoryData(dir);
        Check(memory.Drafts==null,"all-invalid compressed directory refuses commit");
        var bundle=new CompressedMemoryExportBundle { DailyDrafts=new List<DailyMemoryDraft> { new DailyMemoryDraft { HeroId="h1",GameDayIndex=8 } } };
        PlayerExportsStore.WriteJson(file,bundle);
        File.WriteAllText(Path.Combine(compressed,"bad.json"),"{bad json");
        adapter.ImportDialogueHistoryData(dir);
        Check(memory.Drafts["h1"][0].GameDayIndex==8,"partial compressed directory imports valid old schema through actual owner");
        var prepared=adapter.PrepareCompressedDirectory(dir,out int dup,out int total,out int invalid);
        Check(dup==1 && total==1 && invalid==1 && prepared.ContainsKey("h1"), "actual HeroNpcAll compressed preparation preserves invalid-file and duplicate counts");
        var old=memory.Drafts["h1"];
        adapter.ImportDialogueHistoryData(dir);skip();
        Check(ReferenceEquals(memory.Drafts["h1"],old),"compressed skip preserves original list reference");
        adapter.ImportDialogueHistoryData(dir);generation++;overwrite();
        Check(ReferenceEquals(memory.Drafts["h1"],old),"compressed confirmation after generation change rejects commit");
        adapter.ImportDialogueHistoryData(dir);cancel();
        Check(ReferenceEquals(memory.Drafts["h1"],old),"compressed cancel leaves authority unchanged");
        memory.History=new Dictionary<string,List<MyBehavior.DialogueDay>> { ["h2"]=new List<MyBehavior.DialogueDay> { new MyBehavior.DialogueDay { GameDayIndex=3,Lines=new List<string>{"raw"} } } };
        string raw=Path.Combine(dir,"dialogue_history");Directory.CreateDirectory(raw);
        PlayerExportsStore.WriteJson(Path.Combine(raw,"h2__Bob.json"),memory.History["h2"]);
        var rawPrepared=adapter.PrepareRawHistoryDirectory(dir,out dup,out total);
        Check(dup==1 && total==1 && rawPrepared["h2"][0].Lines[0]=="raw", "All raw preparation is distinct original DialogueDay schema");
        adapter.ExportDialogueHistoryData("compressed-only-h7");
        string exported=Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"compressed-only-h7");
        Check(Directory.GetFiles(Path.Combine(exported,"compressed_memory"),"*.json").Length==1 && !Directory.Exists(Path.Combine(exported,"dialogue_history")),"DialogueHistory export is compressed-only, raw h2 excluded");
        adapter.ExportSingleNpcDialogueHistoryData("compressed-single-h7","h1");
        Check(PlayerExportsStore.ReadJson<CompressedMemoryExportBundle>(Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"compressed-single-h7","compressed_memory","h1__Alice.json"))?.DailyDrafts.Count==1,"single compressed original schema publishes through actual package");
    }

    private static void PackageExportFormats()
    {
        var profiles=new PersonaProfileStateOwner();profiles.Profiles["h1"]=new MyBehavior.NpcPersonaProfile {Personality="package"};
        var persona=new PersonaProfileImportExportAdapter(profiles,()=>1,g=>true,(t,b,x,y,z)=>{});
        var state=new MemoryBusinessStateOwner { History=new() { ["h2"]=new(){new MyBehavior.DialogueDay {GameDayIndex=2,Lines=new(){"RAW"}}} }, Drafts=new() { ["h1"]=new(){new DailyMemoryDraft{HeroId="h1",GameDayIndex=8}} } };
        var memory=new MemoryHistoryImportExportAdapter(state,()=>1,g=>true,x=>{},(t,b,x,y,z)=>{});
        RewardSystemBehavior.Instance=new RewardSystemBehavior();RewardSystemBehavior.Instance.Entries["h1"]=new RewardSystemBehavior.DebtExportEntry {Gold=3};
        KnowledgeLibraryBehavior.Instance=new KnowledgeLibraryBehavior();KingdomStrategicProfileBehavior.Instance=new KingdomStrategicProfileBehavior();
        var weekly=new WeeklyEventImportExportAdapter();weekly._weekly.WorldOpening=" world ";weekly._weekly.KingdomOpenings[" k "]=" opening ";
        var package=new DeveloperPackageExportController(persona,memory,new DebtImportExportAdapter(),new VoicePersonaImportExportAdapter(),new KnowledgeImportExportAdapter(),weekly);
        package.ExportAllData("whole-all-h7");string all=Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"whole-all-h7");
        var expectedFiles=new[]{"debt/h1__Alice.json","dialogue_history/h2__Bob.json","event_data/EventRecords.json","event_data/KingdomOpeningSummaries.json","event_data/WorldOpeningSummary.json","kingdom_profiles/profile.json","knowledge/rules/rule__key.json","personality_background/h1__Alice.json","unnamed_persona/unnamed.json","voice_mapping/VoiceMapping.json"};
        var actualFiles=Array.ConvertAll(Directory.GetFiles(all,"*",SearchOption.AllDirectories),f=>Path.GetRelativePath(all,f).Replace('\\','/'));Array.Sort(actualFiles,StringComparer.Ordinal);Array.Sort(expectedFiles,StringComparer.Ordinal);
        Check(System.Linq.Enumerable.SequenceEqual(actualFiles,expectedFiles)&&!Directory.Exists(Path.Combine(all,"compressed_memory")),"actual All package exact ten files and RAW not compressed");
        Check(PlayerExportsStore.ReadJson<List<MyBehavior.DialogueDay>>(Path.Combine(all,"dialogue_history","h2__Bob.json"))[0].Lines[0]=="RAW","All original RAW DialogueDay schema");
        Check(Directory.Exists(Path.Combine(all,"personality_background"))&&Directory.Exists(Path.Combine(all,"debt"))&&Directory.Exists(Path.Combine(all,"knowledge"))&&Directory.Exists(Path.Combine(all,"unnamed_persona"))&&Directory.Exists(Path.Combine(all,"voice_mapping"))&&Directory.Exists(Path.Combine(all,"event_data"))&&Directory.Exists(Path.Combine(all,"kingdom_profiles")),"All exact original domain collection");
        package.ExportHeroNpcAllData("whole-hero-h7");string hero=Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"whole-hero-h7");
        Check(Directory.GetFiles(hero,"*",SearchOption.AllDirectories).Length==3 && Directory.GetDirectories(hero).Length==3 && !Directory.Exists(Path.Combine(hero,"dialogue_history"))&&!Directory.Exists(Path.Combine(hero,"knowledge"))&&!Directory.Exists(Path.Combine(hero,"event_data")),"actual HeroNpcAll only persona compressed debt, no All domains");
        Check(PlayerExportsStore.ReadJson<CompressedMemoryExportBundle>(Path.Combine(hero,"compressed_memory","h1__Alice.json")).DailyDrafts.Count==1,"HeroNpcAll original compressed schema");
        string oldKnowledge=File.ReadAllText(Directory.GetFiles(Path.Combine(all,"knowledge","rules"))[0]);string oldKingdom=File.ReadAllText(Path.Combine(all,"kingdom_profiles","profile.json"));
        KnowledgeLibraryBehavior.Instance.ExportFails=true;KingdomStrategicProfileBehavior.Instance.ExportFails=true;profiles.Profiles["h1"].Personality="new";package.ExportAllData("whole-all-h7");
        Check(File.ReadAllText(Directory.GetFiles(Path.Combine(all,"knowledge","rules"))[0])==oldKnowledge&&File.ReadAllText(Path.Combine(all,"kingdom_profiles","profile.json"))==oldKingdom,"actual package failure restores original Knowledge and Kingdom subdirectories before Publish");
        Check(PlayerExportsStore.ReadJson<MyBehavior.NpcPersonaProfile>(Path.Combine(all,"personality_background","h1__Alice.json")).Personality=="new","ordinary export partial domain failure still publishes successful domain");
        KnowledgeLibraryBehavior.Instance.ExportFails=false;KingdomStrategicProfileBehavior.Instance.ExportFails=false;
    }

    private static void PackageImportFormats()
    {
        long generation=1;var profiles=new PersonaProfileStateOwner();var memoryState=new MemoryBusinessStateOwner();
        var persona=new PersonaProfileImportExportAdapter(profiles,()=>generation,g=>g==generation,(t,b,x,y,z)=>{});
        var memory=new MemoryHistoryImportExportAdapter(memoryState,()=>generation,g=>g==generation,x=>{},(t,b,x,y,z)=>{});
        var execution=new DeveloperImportController(()=>generation,g=>g==generation);Action overwrite=null,skip=null,cancel=null;var order=new List<string>();
        var port=new DeveloperPackageImportPort {CaptureGeneration=()=>generation,IsCurrent=g=>g==generation,
            ValidateUnnamedPersonaKeysForImport=(string dir,out string error)=>{error="";return true;},
            ValidateKnowledgeKeywordsForImport=(string dir,bool overwriteExisting,out string error)=>{error="";return true;},
            ApplyImportedPersonaProfiles=(data,replace,g)=>{order.Add("persona");return persona.ApplyImportedPersonaProfiles(data,replace,g);},
            ApplyImportedDialogueHistory=(data,replace,g)=>{order.Add("raw");if(g!=generation)return false;MemoryImportExportOwner.ApplyDialogueHistoryImports(memoryState,data,replace);return true;},
            ApplyImportedEventData=(payload,replace)=>order.Add("weekly"),
            RefreshVoiceStorage=()=>order.Add("voice-success"),RefreshUnnamedStorage=()=>order.Add("unnamed"),
            ImportKnowledgeFromDir=(string dir,bool replace,out string detail)=>{order.Add("knowledge-failed");detail="controlled domain reject";return false;},
            ShowDuplicateImportInquiry=(title,text,a,b,c)=>{var guarded=execution.BeginConfirmation(a,b,c);overwrite=guarded[0];skip=guarded[1];cancel=guarded[2];}};
        var import=new DeveloperPackageImportController(port,execution,persona,memory,new DebtImportExportAdapter(),new KnowledgeImportExportAdapter(),new VoicePersonaImportExportAdapter(),new WeeklyEventImportExportAdapter());
        RewardSystemBehavior.Instance=new RewardSystemBehavior();KingdomStrategicProfileBehavior.Instance=new KingdomStrategicProfileBehavior();VoiceMapper.Ready=false;
        string all=Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"whole-all-h7");
        File.WriteAllText(Path.Combine(all,"voice_mapping","VoiceMapping.json"),"{bad");
        TaleWorlds.Library.InformationManager.Messages.Clear();import.ImportAllData(all);
        Check(profiles.Profiles["h1"].Personality=="new"&&memoryState.History["h2"][0].Lines[0]=="RAW"&&RewardSystemBehavior.Instance.Entries["h1"].Gold==3,"whole All real persona/raw/debt commits survive later controlled domain failures");
        Check(order.Count==5&&order[0]=="persona"&&order[1]=="raw"&&order[2]=="weekly"&&order[3]=="unnamed"&&order[4]=="knowledge-failed"&&!order.Contains("voice-success"),"whole All original sequence continues after Voice failure and Knowledge rejection, not transaction");
        Check(TaleWorlds.Library.InformationManager.Messages.Exists(x=>x.Contains("警告：VoiceMapping"))&&TaleWorlds.Library.InformationManager.Messages.Exists(x=>x.Contains("警告：Knowledge"))&&TaleWorlds.Library.InformationManager.Messages.Exists(x=>x.StartsWith("导入完成：")),"ordinary All original partial warnings and final completion feedback unchanged");
        var oldProfile=profiles.Profiles["h1"];var oldHistory=memoryState.History["h2"];order.Clear();import.ImportAllData(all);Check(order.Count==0&&overwrite!=null,"duplicate whole All preflight waits without state write");
        generation++;overwrite();Check(order.Count==0&&ReferenceEquals(profiles.Profiles["h1"],oldProfile),"whole All confirmation after generation change rejects commits");
        import.ImportAllData(all);cancel();overwrite();Check(order.Count==0,"whole All cancellation retires overwrite");
        import.ImportAllData(all);skip();Check(ReferenceEquals(profiles.Profiles["h1"],oldProfile)&&ReferenceEquals(memoryState.History["h2"],oldHistory),"whole All skip preserves original profile and raw list references");
        int before=order.Count;skip();Check(order.Count==before,"whole All duplicate skip click once via actual confirmation owner");
        order.Clear();string hero=Path.Combine(PlayerExportsStore.GetPlayerExportsRootPath(),"whole-hero-h7");import.ImportHeroNpcAllData(hero);overwrite();
        Check(memoryState.Drafts["h1"][0].GameDayIndex==8&&memoryState.History.ContainsKey("h2")&&order.Count==1&&order[0]=="persona","HeroNpcAll real compressed only; does not dispatch RAW Voice Weekly or Knowledge");
        File.WriteAllText(Path.Combine(hero,"compressed_memory","invalid.json"),"{bad");import.ImportHeroNpcAllData(hero);skip();
        Check(TaleWorlds.Library.InformationManager.Messages.Exists(x=>x.Contains("无效压缩记忆")),"whole HeroNpcAll valid plus malformed file keeps partial success feedback");
        order.Clear();import.ImportAllData(Path.Combine(all,"missing"));Check(order.Count==0,"whole All missing folder dispatches no domain write");
    }

    private static void RebellionEditorUi()
    {
        var runtime=new KingdomRebellionRuntimeController();var automatic=new AutomaticKingdomRebellionOwner<MyBehavior.PendingAutomaticKingdomRebellionContext>();int details=0;
        var ui=new KingdomRebellionEditorController(()=>runtime,()=>automatic,()=>1,k=>details++);
        var k=new TaleWorlds.CampaignSystem.Kingdom{StringId="k",Name="Kingdom"};var c=new TaleWorlds.CampaignSystem.Clan{StringId="c",Name="Clan"};MemoryEntityIdentityBannerlordAdapter.CurrentKingdom=k;MemoryEntityIdentityBannerlordAdapter.CurrentClan=c;
        var failed=new MyBehavior.RebelKingdomNamingResult{FailureReason="synthetic failure",AttemptsUsed=3,IsRequestsPerMinuteLimit=true,RetryAfterSeconds=8};var context=new MyBehavior.PendingDevForcedKingdomRebellionContext {KingdomId="k",ClanId="c",NamingResult=failed};
        ui.PendingDevReady=true;ui.PendingDevContext=context;ui.DevForcedInProgress=true;DuelSettings.Enabled=false;ui.ProcessPendingDevForcedKingdomRebellionResult();
        Check(!ui.PendingDevReady&&ui.PendingDevContext==null&&!ui.DevForcedInProgress&&runtime.Executions==0,"actual Rebellion UI consumes pending state before feature-disabled cancellation, no execution");DuelSettings.Enabled=true;
        ui.PendingDevReady=true;ui.PendingDevContext=context;ui.ProcessPendingDevForcedKingdomRebellionResult();
        Check(TaleWorlds.Library.InformationManager.Inquiry.Title=="叛乱建国命名失败"&&runtime.Executions==0&&NonBlockingErrorReport.Errors.Contains("synthetic failure"),"actual naming failure opens repair UI and reports once without game execution");
        TaleWorlds.Library.InformationManager.Inquiry.Confirm();Check(ReferenceEquals(ui.BlockedDevContext,context)&&ui.ReopenAfterApiConfig&&ModOnboardingBehavior.Repairs==1,"actual failure confirmation queues API repair and retains same blocked context");
        ui.ReopenAfterApiConfigUtcTicks=0;ui.ProcessKingdomRebellionApiRepairResume();Check(ui.ReopenAfterApiConfig,"active inquiry prevents premature API repair resume");TaleWorlds.Library.InformationManager.HideInquiry();ui.ReopenAfterApiConfigUtcTicks=long.MaxValue;ui.ProcessKingdomRebellionApiRepairResume();Check(ui.ReopenAfterApiConfig,"original 300ms gate blocks premature resume");
        ui.ReopenAfterApiConfigUtcTicks=0;ui.ProcessKingdomRebellionApiRepairResume();Check(!ui.ReopenAfterApiConfig&&TaleWorlds.Library.InformationManager.Inquiry.Title=="重试叛乱建国命名","repair resumes original blocked-context UI after gate");
        var oldCancel=TaleWorlds.Library.InformationManager.Inquiry.Cancel;var replacement=new MyBehavior.PendingDevForcedKingdomRebellionContext();ui.BlockedDevContext=replacement;oldCancel();Check(ReferenceEquals(ui.BlockedDevContext,replacement)&&details==1,"old context cancel cannot clear replacement blocked state, returns actual detail capability");
        var autoContext=new MyBehavior.PendingAutomaticKingdomRebellionContext{KingdomId="k",ClanId="c",NamingResult=failed};ui.BlockedAutomaticContext=autoContext;ui.ShowAutomaticKingdomRebellionNamingFailurePopup(autoContext,k,c,new(),false);TaleWorlds.Library.InformationManager.Inquiry.Cancel();Check(ui.BlockedAutomaticContext==null&&runtime.Continues==1&&KingdomRebellionRuntimeController.CivilWarFailures==1,"automatic skip clears only same context and dispatches original failed-notify then continue capabilities");
        ui.ShowAutomaticKingdomRebellionNamingFailurePopup(autoContext,k,c,new(),true);TaleWorlds.Library.InformationManager.Inquiry.Confirm();Check(runtime.Retries==1,"automatic post-repair affirmative dispatches retry capability not create effect");
        var naming=new MyBehavior.RebelKingdomNamingResult{Success=true,FormalName="Name",ShortName="Short",EncyclopediaText="Text"};context.NamingResult=naming;ui.PendingDevReady=true;ui.PendingDevContext=context;ui.ProcessPendingDevForcedKingdomRebellionResult();Check(runtime.Executions==1&&TaleWorlds.Library.InformationManager.Inquiry.Title=="强制叛乱执行完成","actual UI source-extracted success predicate dispatches controlled game capability once");
        var text=new System.Text.StringBuilder();KingdomRebellionEditorController.AppendRebelKingdomNamingResultLines(text,failed);Check(text.ToString().Contains("RPM")&&text.ToString().Contains("8 秒")&&!text.ToString().Contains("synthetic failure"),"actual naming failure popup compact detail retains RPM/wait and omits full response");
        MemoryEntityIdentityBannerlordAdapter.CurrentClan=null;ui.BlockedDevContext=context;ui.RetryBlockedDevForcedKingdomRebellionNaming();Check(runtime.Executions==1&&!ui.DevForcedInProgress,"retry resolves current target and refuses removed clan before task");
        DuelSettings.Enabled=false;ui.StartDevForcedKingdomRebellionAsync(k,c,0,0,0,0,new());Check(!ui.DevForcedInProgress,"disabled start returns before detached request/background task");DuelSettings.Enabled=true;
    }

    private static void ImportSources()
    {
        string fixture = Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP") ?? Path.GetTempPath(), "imports-" + Guid.NewGuid().ToString("N"));
        string module = Path.Combine(fixture, "AnimusForge");
        string installed = Path.Combine(module, "PlayerExports");
        string user = Path.Combine(fixture, "UserData", "PlayerExports");
        string builtin = Path.Combine(installed, "same-name");
        Directory.CreateDirectory(builtin);
        File.WriteAllText(Path.Combine(module, "SubModule.xml"), "<Module />");
        File.WriteAllText(Path.Combine(builtin, "rule.json"), "{\"source\":\"installed\"}");
        string exportRoot = PlayerExportsStore.GetPlayerExportsRootPath();
        string cwd = Directory.GetCurrentDirectory();
        string configuredRoot = Environment.GetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable);
        try
        {
            Directory.SetCurrentDirectory(module);
            // Clear the override after resolving the path-only root to exercise the former gate.
            Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, null);
            Check(PlayerExportsStore.GetPlayerExportsRootPath() == exportRoot, "installed data never gates the user export destination on a migration receipt");
            var menuFolders = PlayerExportsStore.GetImportFolders();
            Check(menuFolders.Count == 1 && menuFolders[0].FullPath == builtin, "production menu discovery works without migration credentials");
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
            Environment.SetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable, configuredRoot);
        }
        var folders = PlayerExportsStore.GetImportFolders(module, user);
        Check(folders.Count == 1 && folders[0].FullPath == builtin, "fresh install works with missing user root and no migration receipt");
        Check(!Directory.Exists(user), "listing builtins never creates or migrates user data");
        Check(PlayerExportsStore.ResolveImportFolderPath("same-name", module, user) == builtin, "named import reads installed worldbook first");
        Check(PlayerExportsStore.ResolveImportFolderPath("", module, user) == null, "blank import never falls back to a builtin");
        string player = Path.Combine(user, "same-name");
        string newest = Path.Combine(user, "newest");
        Directory.CreateDirectory(player);
        Directory.CreateDirectory(newest);
        Directory.CreateDirectory(Path.Combine(user, ".af-export-candidate.hidden"));
        Directory.CreateDirectory(Path.Combine(installed, ".hidden"));
        Directory.SetLastWriteTimeUtc(player, new DateTime(2020, 1, 1));
        Directory.SetLastWriteTimeUtc(newest, new DateTime(2021, 1, 1));
        folders = PlayerExportsStore.GetImportFolders(module, user);
        Check(folders.Count == 3, "both roots listed; hidden candidates excluded");
        Check(folders[0].Name == folders[2].Name && folders[0].SourceLabel != folders[2].SourceLabel, "same names retain distinct source labels");
        Check(PlayerExportsStore.ResolveImportFolderPath(folders[2].FullPath, module, user) == player, "selected user path cannot switch to same-name builtin");
        Check(PlayerExportsStore.ResolveImportFolderPath("same-name", module, user) == builtin, "installed source has priority over same-name user export");
        Check(PlayerExportsStore.ResolveImportFolderPath("newest", module, user) == newest, "user-only name still resolves");
        Check(PlayerExportsStore.ResolveImportFolderPath(null, module, user) == newest, "blank input retains newest-user-export semantics");
        Check(PlayerExportsStore.GetImportFolders(module, installed).Count == 1, "identical roots are not listed twice");
        string missing = Path.Combine(fixture, "does-not-exist");
        Check(PlayerExportsStore.ResolveImportFolderPath(missing, module, user) == missing, "missing absolute source is not reinterpreted as a relative package");
        bool rejected = false;
        try { PlayerExportsStore.ResolveImportFolderPath("..", module, user); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "relative traversal cannot select a parent or latest export");
        File.WriteAllText(Path.Combine(builtin, "rule.json"), "{\"source\":\"old-player-copy\"}");
        string selected = PlayerExportsStore.ResolveImportFolderPath("same-name", module, user);
        Check(File.ReadAllText(Path.Combine(selected, "rule.json")).Contains("old-player-copy"), "manual old-worldbook overwrite is read as-is");
        Check(!File.Exists(Path.Combine(module, "UserData", ".player-exports-ready.json")), "import never creates migration credentials");
    }

    private static void InstalledWorldbooks()
    {
        // The runner starts at the repository root; use the actual reviewed shipped files.
        string module = Path.Combine(Directory.GetCurrentDirectory(), "content", "modules", "AF.Module.Onboarding");
        string missingUser = Path.Combine(Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP"), "absent-user-exports");
        var folders = PlayerExportsStore.GetImportFolders(module, missingUser);
        Check(folders.Count == 4, "fresh install lists all four complete real worldbooks");
        foreach (var folder in folders)
        {
            Check(PlayerExportsStore.ResolveImportFolderPath(folder.Name, module, missingUser) == folder.FullPath, "real worldbook name resolves to its shipped path");
            Check(Directory.GetFiles(Path.Combine(folder.FullPath, "knowledge", "rules"), "*.json").Length > 0, "real worldbook has knowledge rules");
            Check(Directory.GetFiles(Path.Combine(folder.FullPath, "personality_background"), "*.json").Length > 0, "real worldbook has Hero personas");
            Check(Directory.GetFiles(Path.Combine(folder.FullPath, "unnamed_persona"), "*.json").Length > 0, "real worldbook has non-Hero personas");
            foreach (string relative in new[] { "kingdom_profiles/KingdomProfiles.json", "voice_mapping/VoiceMapping.json", "event_data/WorldOpeningSummary.json", "event_data/KingdomOpeningSummaries.json" })
                Check(PlayerExportsStore.ReadJson<Newtonsoft.Json.Linq.JToken>(Path.Combine(folder.FullPath, relative)) != null, "real worldbook required JSON is readable: " + relative);
        }
        Check(!Directory.Exists(missingUser), "real library enumeration leaves absent user data untouched");
    }

    private static void NpcFileNames()
    {
        Check(NpcDataFileName.Build("h1", "Al bert") == "h1__Al bert.json" && NpcDataFileName.Build(" ", null) == "unknown__NPC.json" && NpcDataFileName.Build("a/b", "c:d") == "a_b__c_d.json", "build with placeholders and invalid chars");
        Check(NpcDataFileName.TryParseParts(@"x\dir\h1__Al bert.json", out string id, out string name) && id == "h1" && name == "Al bert", "parse id__name");
        Check(NpcDataFileName.TryParseParts("h1__.json", out id, out name) && id == "h1" && name == "", "empty name part");
        Check(!NpcDataFileName.TryParseParts("__name.json", out id, out name) && !NpcDataFileName.TryParseParts("noseparator.json", out id, out name) && !NpcDataFileName.TryParseParts(null, out id, out name), "missing/empty id rejected");
        Check(NpcDataFileName.TryParseHeroId("h2__x.json") == "h2" && NpcDataFileName.TryParseHeroId("bad.json") == null, "hero id shortcut");
        Check(NpcDataFileName.IsAutoGeneratedHeroId(" characterobject_12 ") && !NpcDataFileName.IsAutoGeneratedHeroId("lord_1_1"), "auto id prefix");
        Check(NpcDataFileName.NormalizeDisplayName("  A   B\t") == "A   B".Replace("   ", " ") && NpcDataFileName.NormalizeDisplayName("x?y") == "x_y" && NpcDataFileName.NormalizeDisplayName(" ") == "", "display name normalization");
        Check(!NpcDataFileName.IsDisplayNameSpecified("npc") && !NpcDataFileName.IsDisplayNameSpecified("Unknown") && !NpcDataFileName.IsDisplayNameSpecified("") && NpcDataFileName.IsDisplayNameSpecified("Al"), "placeholder names are unspecified");
        Check(NpcDataFileName.IsDisplayNameCompatible("Al", "al", true, false) && !NpcDataFileName.IsDisplayNameCompatible("Al", "Bob", true, false), "name match is case-insensitive; mismatch fails");
        Check(NpcDataFileName.IsDisplayNameCompatible("NPC", "Bob", true, false) && !NpcDataFileName.IsDisplayNameCompatible("NPC", "Bob", true, true), "unspecified name obeys strict flag");
        Check(NpcDataFileName.IsDisplayNameCompatible("Al", null, false, false) && !NpcDataFileName.IsDisplayNameCompatible("Al", null, false, true), "missing hero obeys strict flag");
        Check(!NpcDataFileName.IsDisplayNameCompatible("Al", "", true, false), "hero with blank name never matches a specified file name");
    }

    private static void Store()
    {
        Check(PlayerExportsStore.SanitizeFolderName(" a?b. ") == "a_b" && PlayerExportsStore.SanitizeFolderName("  ") == "" && PlayerExportsStore.SanitizeFolderName(null) == "", "folder name sanitize");
        Check(PlayerExportsStore.ResolveExportFolderName(" x ", new DateTime(2026, 9, 19, 8, 5, 3)) == "x" && PlayerExportsStore.ResolveExportFolderName("", new DateTime(2026, 9, 19, 8, 5, 3)) == "20260919_080503", "export folder name falls back to timestamp");
        string root = PlayerExportsStore.GetPlayerExportsRootPath();
        Check(Path.IsPathRooted(root) && root.EndsWith(Path.DirectorySeparatorChar + PlayerExportsStore.FolderName) && PlayerExportsStore.GetModuleRootPath().Length > 0, "root path shape: " + root);

        string tempRoot = Environment.GetEnvironmentVariable("AF_PLAYER_EXPORTS_TEST_TEMP") ?? Path.GetTempPath();
        string temp = Path.Combine(tempRoot, "af-player-exports-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(PlayerExportsStore.FindLatestExportFolder(temp) == null, "missing root → null");
            Directory.CreateDirectory(Path.Combine(temp, "old"));
            Directory.SetLastWriteTimeUtc(Path.Combine(temp, "old"), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Directory.CreateDirectory(Path.Combine(temp, "new"));
            Directory.SetLastWriteTimeUtc(Path.Combine(temp, "new"), new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Check(Path.GetFileName(PlayerExportsStore.FindLatestExportFolder(temp)) == "new", "latest export = most recently written");
            Check(PlayerExportsStore.ResolveImportFolderPath(Path.Combine(temp, "old")) == Path.GetFullPath(Path.Combine(temp, "old")), "rooted existing path wins");
            Check(PlayerExportsStore.ResolveImportFolderPath("some name") == Path.Combine(root, "some name"), "relative name under PlayerExports");

            string file = Path.Combine(temp, "sub", "row.json");
            PlayerExportsStore.WriteJson(file, new Row { Day = 3, Text = "t" });
            Check(File.Exists(file) && File.ReadAllText(file).Contains("\"Day\": 3"), "write json indented, creates dir");
            Row back = PlayerExportsStore.ReadJson<Row>(file);
            Check(back != null && back.Day == 3 && back.Text == "t", "read json round-trip");
            PlayerExportsStore.WriteJson(file, new Row { Day = 4 });
            Check(PlayerExportsStore.ReadJson<Row>(file).Day == 4, "write replaces existing");
            File.WriteAllText(Path.Combine(temp, "sub", "blank.json"), "  ");
            File.WriteAllText(Path.Combine(temp, "sub", "bad.json"), "{not json");
            File.WriteAllText(Path.Combine(temp, "sub", "note.txt"), "keep");
            Check(PlayerExportsStore.ReadJson<Row>(Path.Combine(temp, "sub", "blank.json")) == null && PlayerExportsStore.ReadJson<Row>(Path.Combine(temp, "sub", "bad.json")) == null && PlayerExportsStore.ReadJson<Row>(Path.Combine(temp, "nope.json")) == null, "blank/bad/missing → null");
            PlayerExportsStore.ClearJsonFiles(Path.Combine(temp, "sub"));
            Check(Directory.GetFiles(Path.Combine(temp, "sub")).Length == 1 && File.Exists(Path.Combine(temp, "sub", "note.txt")), "clear removes only *.json");
            PlayerExportsStore.ClearJsonFiles(Path.Combine(temp, "missing"));
            PlayerExportsStore.ClearJsonFiles(null);
            Check(true, "clear tolerates missing/null dir");
        }
        finally
        {
            // Keep the uniquely named synthetic fixtures with this run's evidence.
        }
    }
}

// Controlled engine registry and visible feedback only; no format/import/state algorithms replaced.
namespace TaleWorlds.CampaignSystem
{
 internal sealed class Hero
 {
  internal string StringId, Name;
  internal static readonly List<Hero> AllAliveHeroes=new List<Hero>();
  internal static Hero Find(string id)=>AllAliveHeroes.Find(x=>x.StringId==id);
  internal static Hero FindFirst(Func<Hero,bool> predicate)=>AllAliveHeroes.Find(x=>predicate(x));
 }
}
namespace TaleWorlds.Library
{
 internal sealed class InformationMessage { internal readonly string Text; internal InformationMessage(string text){ Text=text; } }
 internal static class InformationManager { internal static readonly List<string> Messages=new List<string>(); internal static InquiryData Inquiry;internal static bool InquiryActive;internal static bool IsAnyInquiryActive()=>InquiryActive;internal static void HideInquiry(){InquiryActive=false;}internal static void ShowInquiry(InquiryData data,bool pauseGameActiveState=false){Inquiry=data;InquiryActive=true;} internal static void DisplayMessage(InformationMessage message){ Messages.Add(message.Text); } }
}
namespace AnimusForge { internal static class Logger { internal static void Log(string category,string message){} } }

// Controlled external unnamed-persona authority only: these methods are not tested as real Shout state acceptance.
namespace AnimusForge
{
 internal static partial class ShoutUtils
 {
  internal static readonly HashSet<string> ExistingKeys=new HashSet<string>();
  internal static bool HasUnnamedPersonaKey(string key)=>ExistingKeys.Contains(key);
  internal static string ExportUnnamedPersonaStateJson(bool pretty)=>"{}";
  internal static void ImportUnnamedPersonaStateJson(string json,bool overwriteExisting) { }
  internal static bool TryGetUnnamedPersonaByKey(string key,out string personality,out string background) { personality="";background="";return false; }
  internal static void SaveUnnamedPersonaByKey(string key,string personality,string background) { }
  internal static void ExportUnnamedPersonaToDir(string path) { string dir=Path.Combine(path,"unnamed_persona");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"unnamed.json"),"{}"); }
  internal static void ImportUnnamedPersonaFromDir(string path) { }
  internal static void ImportUnnamedPersonaFromDir(string path,bool overwriteExisting) { }
 }
}

namespace AnimusForge
{
 public partial class MyBehavior
 {
  internal static Action<string> LegacyImportPersonaFixture, LegacyImportUnnamedFixture;
  private void ImportPersonaData(string folderName) => LegacyImportPersonaFixture(folderName);
  private void ImportUnnamedPersonaData(string folderName) => LegacyImportUnnamedFixture(folderName);
  private void ImportKnowledgeData(string folderName) { }
  private void ImportVoiceMappingData(string folderName) { }
  private void ImportEventData(string folderName) { }
 }
}

namespace TaleWorlds.CampaignSystem
{
 internal sealed class Campaign
 {
  internal static Campaign Current;
  internal AnimusForge.MyBehavior My; internal AnimusForge.KingdomStrategicProfileBehavior Kingdom;
  internal T GetCampaignBehavior<T>() where T:class => (typeof(T)==typeof(AnimusForge.MyBehavior) ? (object)My : Kingdom) as T;
 }
}
namespace AnimusForge
{
 internal sealed class KingdomStrategicProfileBehavior
 {
  internal static KingdomStrategicProfileBehavior Instance;internal bool ExportFails;
  internal bool ExportAllToDirectory(string path,out string detail) {detail="fixture";if(ExportFails)return false;string dir=Path.Combine(path,"kingdom_profiles");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"profile.json"),"{}");return true;}
  internal bool InspectImportDirectory(string dir,out int total,out int duplicate,out int skipped,out string error){total=1;duplicate=skipped=0;error="";return true;}
  internal bool ImportAllFromDirectory(string dir,bool overwriteExisting,out string detail){detail="";return true;}
 }
 internal static partial class VoiceMapper
 {
  internal static bool Ready { get=>GetTotalVoiceCount()>0; set {_voicePools=value?new Dictionary<string,List<string>>{{"male_young",new List<string>{"preloaded"}}}:new Dictionary<string,List<string>>(); _fallbackVoice="";} }
  internal static string ExportMappingJson(bool pretty=false)=>"{}";
  internal static void SetPreferredExportFolder(string path) { }
 }
}

// Controlled domain query/DTO leaves for exact source-extracted export bodies; no game acceptance.
namespace AnimusForge
{
 internal sealed class RewardSystemBehavior {internal static RewardSystemBehavior Instance;internal sealed class DebtExportEntry{public int Gold;}internal Dictionary<string,DebtExportEntry> Entries=new();internal Dictionary<string,DebtExportEntry> ExportDebtEntries()=>Entries;internal void ImportDebtEntries(Dictionary<string,DebtExportEntry> entries){Entries=entries;}}
 internal sealed class KnowledgeLibraryBehavior {internal static KnowledgeLibraryBehavior Instance;internal bool ExportFails;internal sealed class LoreRule{public string Id;public List<string> Keywords;}internal sealed class KnowledgeFile{public List<LoreRule> Rules;}internal bool RejectRules, RejectBulk; internal string RejectSingleId; internal List<string> ExistingRuleIds=new(); internal int ImportAttempts; internal List<string> GetRuleIdsForDev(int max)=>ExistingRuleIds; internal bool ImportRulesJson(string json,bool overwriteExisting){ImportAttempts++;return !RejectRules;} internal bool ImportSingleRuleJson(string json,bool overwriteExisting)=>!RejectRules;internal bool TryValidateKnowledgeExport(out string error){error="fixture";return !ExportFails;}internal string ExportRulesJson()=>"{\"Rules\":[{\"Id\":\"rule\",\"Keywords\":[\"key\"]}]}";}
 internal sealed class WeeklyEventRecordStateOwner{internal long PublishedHistoryRevision;internal string WorldOpening;internal Dictionary<string,string> KingdomOpenings=new();internal List<MyBehavior.EventRecordEntry> Records=new();}
 public partial class MyBehavior {internal sealed class EventRecordEntry{public string EventId;}internal sealed class EventWorldOpeningSummaryJson{public string Summary;}}
}

namespace AnimusForge.Refactor.Runtime { }
namespace AnimusForge { internal static partial class KnowledgeImportSupport {internal static KnowledgeLibraryBehavior.KnowledgeFile TryLoadKnowledgeRulesFromRuleFiles(string dir)=>null;} }

// Controlled game/runtime domain leaves only. Complete UI controller algorithms compile unchanged.
namespace TaleWorlds.Core { }
namespace TaleWorlds.Library
{
 internal sealed class InquiryData {internal string Title,Text;internal Action Confirm,Cancel;internal InquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string affirmativeText,string negativeText,Action affirmativeAction,Action negativeAction){Title=title;Text=text;Confirm=affirmativeAction;Cancel=negativeAction;}}
}
namespace TaleWorlds.CampaignSystem {internal sealed class Clan{internal string StringId,Name;}internal sealed class Kingdom{internal string StringId,Name;}}
namespace AnimusForge
{
 internal static partial class MemoryEntityIdentityBannerlordAdapter
 {internal static TaleWorlds.CampaignSystem.Kingdom CurrentKingdom;internal static TaleWorlds.CampaignSystem.Clan CurrentClan;internal static TaleWorlds.CampaignSystem.Kingdom FindKingdomById(string id)=>CurrentKingdom?.StringId==id?CurrentKingdom:null;internal static TaleWorlds.CampaignSystem.Clan FindClanById(string id)=>CurrentClan?.StringId==id?CurrentClan:null;internal static string GetKingdomDisplayName(TaleWorlds.CampaignSystem.Kingdom k,string fallback)=>k?.Name??fallback;internal static string GetClanDisplayName(TaleWorlds.CampaignSystem.Clan c)=>c?.Name??"Clan";internal static string GetClanId(TaleWorlds.CampaignSystem.Clan c)=>c?.StringId;internal static string GetKingdomId(TaleWorlds.CampaignSystem.Kingdom k)=>k?.StringId;}
 internal static class RebellionNamingOwner {internal const int MaxAttempts=3;}
 internal static class DuelSettings{internal static bool Enabled=true;internal static bool IsKingdomStabilityAndRebellionEnabled()=>Enabled;}
 internal static class PlayerKingdomRebellionImmunity{internal static bool Protected;internal static bool ShouldProtectKingdom(TaleWorlds.CampaignSystem.Kingdom k)=>Protected;}
 internal static class ModOnboardingBehavior{internal static int Repairs;internal static bool OpenEventAndRebellionApiRepairFlow(){Repairs++;return true;}}
 internal static class NonBlockingErrorReport{internal static readonly List<string> Errors=new();internal static void Show(string title,string message)=>Errors.Add(message);}
 internal sealed class AutomaticKingdomRebellionOwner<T>{internal int PendingCount;}
 internal sealed partial class KingdomRebellionRuntimeController
 {
  internal int Executions,Continues,Retries;internal static int CivilWarFailures;internal static void NotifyCivilWarRebellionFailed(MyBehavior.PendingAutomaticKingdomRebellionContext context,string reason){CivilWarFailures++;}internal static string[] CaptureRebellionExistingNames()=>Array.Empty<string>();
  internal void BuildRebelKingdomNamingRequest(TaleWorlds.CampaignSystem.Clan clan,TaleWorlds.CampaignSystem.Kingdom kingdom,int week,IEnumerable<TaleWorlds.CampaignSystem.Clan> followers,out string system,out string user,IReadOnlyCollection<string> names){system="detached";user="detached";}
  internal MyBehavior.RebelKingdomNamingResult GenerateRebelKingdomNamingFromPrompts(string system,string user,string target,int attempts,IReadOnlyCollection<string> names)=>BuildFailedRebelKingdomNamingResult("controlled gateway",attempts);
  internal void EnqueueKingdomRebellionNamingMainThreadAction(long generation,Action apply,string source){ }
  internal bool TryExecuteKingdomRebellionWithNaming(TaleWorlds.CampaignSystem.Clan clan,TaleWorlds.CampaignSystem.Kingdom kingdom,int week,bool forceTrigger,int relation,int towns,int castles,MyBehavior.RebelKingdomNamingResult naming,IEnumerable<TaleWorlds.CampaignSystem.Clan> followers,out string text){Executions++;text="controlled game effect";return true;}
  internal void ContinueAutomaticKingdomRebellionFlow(){Continues++;}internal void RetryAutomaticKingdomRebellionNamingAsync(MyBehavior.PendingAutomaticKingdomRebellionContext context){Retries++;}
 }
}

// Frozen independent pre-fix oracle only; the current accepting controller is separately linked.
/* AF_ONBOARDING_BASELINE_SOURCE_BEGIN
using System; using System.IO; using System.Reflection; using TaleWorlds.CampaignSystem; using TaleWorlds.Library; namespace AnimusForge; internal sealed class LegacyOnboardingReflection {internal static bool InvokePrivateImport(MyBehavior my, string methodName, string folderName)
	{
		try
		{
			if (my == null)
			{
				return false;
			}
			MethodInfo method = typeof(MyBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (method == null)
			{
				return false;
			}
			method.Invoke(my, new object[1] { folderName ?? "" });
			return true;
		}
		catch
		{
			return false;
		}
	}internal void TryImportRequiredSetAndUnlock(string folderName, Action onReturn)
	{
		try
		{
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path = Path.Combine(text, "personality_background");
			if (!Directory.Exists(path) || Directory.GetFiles(path, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 personality_background\\*.json"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path2 = Path.Combine(text, "unnamed_persona");
			if (!Directory.Exists(path2) || Directory.GetFiles(path2, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 unnamed_persona\\*.json"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			bool flag = false;
			try
			{
				string path3 = Path.Combine(text, "knowledge", "rules");
				if (Directory.Exists(path3) && Directory.GetFiles(path3, "*.json").Length != 0)
				{
					flag = true;
				}
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				string path4 = Path.Combine(text, "knowledge", "KnowledgeRules.json");
				if (File.Exists(path4))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 knowledge\\rules\\*.json（或 knowledge\\KnowledgeRules.json）"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path5 = Path.Combine(text, "voice_mapping", "VoiceMapping.json");
			string path6 = Path.Combine(text, "VoiceMapping.json");
			if (!File.Exists(path5) && !File.Exists(path6))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 voice_mapping\\VoiceMapping.json。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path7 = Path.Combine(text, "event_data", "WorldOpeningSummary.json");
			string path8 = Path.Combine(text, "event_data", "KingdomOpeningSummaries.json");
			if (!File.Exists(path7) || !File.Exists(path8))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 event_data\\WorldOpeningSummary.json 或 event_data\\KingdomOpeningSummaries.json。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string kingdomProfilesPath = Path.Combine(text, "kingdom_profiles", "KingdomProfiles.json");
			if (!File.Exists(kingdomProfilesPath))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 kingdom_profiles\\KingdomProfiles.json。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			KingdomStrategicProfileBehavior kingdomProfileBehavior = Campaign.Current?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			if (kingdomProfileBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：国家战略与性格数据行为未初始化。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			if (!kingdomProfileBehavior.InspectImportDirectory(text, out int kingdomProfileTotalCount, out _, out int kingdomProfileSkippedCount, out string kingdomProfileInspectError)
				|| kingdomProfileTotalCount <= 0)
			{
				string reason = string.IsNullOrWhiteSpace(kingdomProfileInspectError)
					? "资料包中没有与当前世界安全匹配的国家卡。"
					: kingdomProfileInspectError;
				InformationManager.DisplayMessage(new InformationMessage("导入失败：国家战略与性格资料无效。原因：" + reason));
				OpenImportFolderPicker(onReturn);
				return;
			}
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：MyBehavior 未初始化。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportPersonaData", text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 Hero 个性/背景导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportUnnamedPersonaData", text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 非Hero 描述导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportKnowledgeData", text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 知识导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportVoiceMappingData", text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 声音映射导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportEventData", text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 事件库导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!kingdomProfileBehavior.ImportAllFromDirectory(text, overwriteExisting: true, out string kingdomProfileImportDetail))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行国家战略与性格导入。原因：" + kingdomProfileImportDetail));
				OpenImportFolderPicker(onReturn);
			}
			else if (!HasLoadedVoiceMapping())
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：声音映射未成功载入到当前存档。"));
				OpenImportFolderPicker(onReturn);
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("国家战略与性格已从资料包导入：匹配 " + kingdomProfileTotalCount + " 条；无法匹配 " + kingdomProfileSkippedCount + " 条。"));
				CompleteOnboardingAndOpenPlayerPersonaSetup(onReturn, importedDatabase: true);
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
			OpenImportFolderPicker(onReturn);
		}
	}private static bool HasLoadedVoiceMapping()
	{
		try
		{
			return VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
		}
		catch
		{
			return false;
		}
	}private static string ResolveImportFolderPath(string folderName)
	{
		return PlayerExportsStore.ResolveImportFolderPath(folderName);
	}internal bool Unlocked; private void OpenImportFolderPicker(Action returned) { } private void CompleteOnboardingAndOpenPlayerPersonaSetup(Action returned,bool importedDatabase) { Unlocked=true; }}
AF_ONBOARDING_BASELINE_SOURCE_END */
