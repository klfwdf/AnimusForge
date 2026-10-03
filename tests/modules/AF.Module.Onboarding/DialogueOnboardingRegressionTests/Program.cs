using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AnimusForge;
using AnimusForge.DialogueUI;

static class Program
{
    static int checks;
    static void Check(bool value,string label) { checks++; if(!value) throw new Exception("FAIL: "+label); }
    static void Pump(AnimusForgeApiOnboardingVM vm,Func<bool> done) {
        for(int i=0;i<300 && !done();i++){vm.OnTick();Thread.Sleep(10);}
        Check(done(),"bounded asynchronous dispatch completed");
    }
    static string Read(string root,string path)=>File.ReadAllText(Path.Combine(root,path));
    static int Main(string[] args)
    {
        try { Run(args); return 0; }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static void Run(string[] args)
    {
        var guard=new OpeningInteractionInputGuard();
        Check(guard.IsPending,"new conversation drains its opening frame");
        guard.Tick(true);Check(guard.IsPending,"F press blocked");
        for(int i=0;i<20;i++)guard.Tick(true);
        Check(guard.IsPending,"held F stays blocked independent of duration");
        guard.Tick(false);Check(guard.IsPending,"release-frame text event drained");
        guard.Tick(false);Check(!guard.IsPending,"editor activated after clean release");
        guard.Tick(true);Check(!guard.IsPending,"legitimate later F preserved");

        var mode=new ConversationModeTextOwner();
        Check(mode.EnterAi("native opening")==null,"first AI entry preserves native opening");
        Check(mode.EnterAi("must not overwrite native")==null,"repeated AI entry is idempotent");
        Check(mode.LeaveAi("AI response",false)=="native opening","ordinary mode restores its own text");
        Check(mode.LeaveAi("ordinary text",false)==null,"repeated ordinary entry cannot corrupt AI cache");
        Check(mode.EnterAi("next native line")=="AI response","AI reentry restores last AI response");
        Check(mode.LeaveAi("..",true)=="next native line","waiting dots do not replace cached AI response");
        Check(mode.EnterAi("native again")=="AI response","timeout reentry restores text not dots");
        Check(mode.LeaveAi("partial streamed AI response",false)=="native again","partial response preserved on manual mode exit");
        Check(mode.EnterAi("")=="partial streamed AI response","partial AI body survives reentry");
        Check(mode.LeaveAi("",false)=="","empty native text remains a valid restore");
        Check(mode.EnterAi("new native text")=="","empty AI text is not mistaken for absent cache");
        mode.Reset();Check(mode.EnterAi("new NPC opening")==null,"changed context retires old NPC body");
        var ret=new AnimusForge.Refactor.Modules.EncounterPendingReturnOwner<object,object>();
        object encounter=new(),party=new();
        Check(ret.Mark(encounter,party,1)&&ret.IsCurrent(encounter,party,1),"native handoff bound to actual encounter/party/save");
        Check(!ret.IsCurrent(new(),party,1)&&!ret.IsCurrent(encounter,new(),1)&&!ret.IsCurrent(encounter,party,2),"later encounter/party/save cannot return old menu");
        bool Return(bool intercepted,bool current,bool release,bool combat,bool native)=>AnimusForge.Refactor.Modules.NativeDialogueReturnPolicy.ShouldReturn(intercepted,current,release,combat,native);
        Check(Return(true,true,false,false,false),"intercepted dialogue exit without release returns options");
        Check(!Return(false,true,false,false,false),"player-initiated native conversation remains vanilla");
        Check(!Return(true,false,false,false,false),"stale handoff cannot reopen menu");
        Check(!Return(true,true,true,false,false),"approved release/surrender is not blocked");
        Check(!Return(true,true,false,true,false),"attack/battle/result/captivity stays native");
        Check(!Return(true,true,false,false,true),"siege/raid/naval activity stays native");
        ret.Clear();Check(!ret.IsPending,"return request clears exactly once");
        var wait=new ConversationReplyWaitOwner();const long minute=600000000;
        wait.Start(1,1);Check(!wait.TryOfferEscape(1,minute,minute),"no early escape notice");
        Check(wait.TryOfferEscape(1,minute+1,minute)&&wait.CanEscape,"first wait unlocks");
        Check(!wait.TryOfferEscape(1,minute*2,minute),"single notice per wait");
        wait.Stop();Check(!wait.CanEscape,"leave disarms old UI wait");
        wait.Start(2,minute*2);Check(!wait.CanEscape,"new generation resets notice");
        Check(!wait.TryOfferEscape(1,minute*4,minute),"old callback cannot arm new wait");
        Check(wait.TryOfferEscape(2,minute*3,minute),"second wait offers escape again");
        // Partial reply changes display only; watchdog remains active until completion.
        wait.Start(3,minute*4);Check(wait.TryOfferEscape(3,minute*5,minute),"stalled streaming reply unlocks");
        wait.Stop();Check(!wait.TryOfferEscape(3,minute*6,minute),"completed wait never emits late notice");

        var options=new DialogueUiSettings();DialogueUiSettings.Instance=options;
        Check(options.AutoEnterAiMode&&DialogueUiOptions.AutoEnterAiMode,"AI-first default preserved");
        options.AutoEnterAiMode=false;Check(!DialogueUiOptions.AutoEnterAiMode,"MCM false reaches actual option reader");
        options.AutoEnterAiMode=true;Check(DialogueUiOptions.AutoEnterAiMode,"MCM change requires no restart");
        Check(options.AutoEnterAiModeHeroOnly && DialogueUiOptions.AutoEnterAiModeHeroOnly,"Hero-only filter is a separate enabled-by-default setting");
        options.AutoEnterAiModeHeroOnly=false;Check(!DialogueUiOptions.AutoEnterAiModeHeroOnly && DialogueUiOptions.AutoEnterAiMode,"Hero filter can be disabled without disabling original auto entry");
        options.AutoEnterAiMode=false;options.AutoEnterAiModeHeroOnly=true;
        Check(!DialogueUiOptions.AutoEnterAiMode && DialogueUiOptions.AutoEnterAiModeHeroOnly,"Hero filter does not implicitly enable master auto entry");
        options.AutoEnterAiMode=true;

        DuelSettings.Current=new();var settings=DuelSettings.Current;int completed=0,cancelled=0;
        var vm=new AnimusForgeApiOnboardingVM(false,()=>completed++,()=>cancelled++);
        Check(vm.PrimaryModel=="persisted-main"&&vm.AuxiliaryModel=="persisted-aux","custom persisted models preserved by selector callback");
        Check(vm.PrimaryModelSelector.Options[vm.PrimaryModelSelector.SelectedIndex]=="persisted-main","selected item matches model sent to API");
        vm.ExecuteSaveAndFinish();Check(completed==0&&ModOnboardingBehavior.Saves==0,"cannot bypass unsuccessful test");
        vm.ExecuteEditPrimaryUrl();Check(vm.IsKeyPromptVisible&&vm.PromptKeyInput=="primary","URL edit on same-layer modal with existing value");
        vm.PromptKeyInput="not-saved";vm.ExecuteCancelPromptKey();Check(vm.PrimaryUrl=="primary"&&!vm.IsKeyPromptVisible,"cancel preserves URL");
        vm.ExecuteEditPrimaryUrl();vm.PromptKeyInput="  edited-url  ";vm.ExecuteConfirmPromptKey();Check(vm.PrimaryUrl=="edited-url"&&!vm.IsKeyPromptVisible,"confirm saves trimmed URL");
        vm.ExecuteSelectDeepSeekFlash();vm.ExecuteEditPromptKey();
        Check(vm.IsKeyPromptVisible,"paste helper cannot replace outer key-confirm callback");
        vm.ExecuteCancelPromptKey();vm.ExecuteBackToMain();
        settings.EventAndRebellionApiKey="";
        vm.ExecuteUseExistingConfig();Pump(vm,()=>vm.IsSuccessViewVisible);
        Check(ModOnboardingBehavior.Calls.Count==3,"incomplete optional event API skipped");
        Check(ModOnboardingBehavior.Calls.All(x=>x.ModelName.StartsWith("persisted-")),"existing models actually passed unchanged to gateway");
        vm.ExecuteSaveAndFinish();Check(completed==1&&settings.ApiUrl=="primary"&&ModOnboardingBehavior.Saves==0,"use existing config completes without rewriting settings");
        vm.OnFinalize();

        DuelSettings.Current=new();var failVm=new AnimusForgeApiOnboardingVM(true,()=>completed++,()=>cancelled++);
        ModOnboardingBehavior.Validate=(t,c)=>Task.FromResult(new ModOnboardingBehavior.ApiValidationTargetResult{Success=false,FailureHint="synthetic failure"});
        failVm.ExecuteStartCombinedTest();Pump(failVm,()=>failVm.PrimaryStatusText.StartsWith("❌"));
        failVm.ExecuteSaveAndFinish();Check(completed==1,"failed tests cannot save or finish");
        failVm.ExecuteClose();Check(cancelled==1,"explicit close requests cancellation");failVm.OnFinalize();

        var existingRetry=new AnimusForgeApiOnboardingVM(true,()=>completed++,()=>cancelled++);
        existingRetry.ExecuteUseExistingConfig();existingRetry.ExecuteCancelTest();
        Check(existingRetry.IsMainViewVisible,"cancel existing config returns to route choice, not non-saving custom edits");
        existingRetry.OnFinalize();

        var delayed=new TaskCompletionSource<ModOnboardingBehavior.ApiValidationTargetResult>();
        ModOnboardingBehavior.Validate=(t,c)=>delayed.Task;
        var lateVm=new AnimusForgeApiOnboardingVM(true,()=>completed++,()=>cancelled++);
        lateVm.ExecuteStartCombinedTest();Check(lateVm.IsTestingViewVisible,"test opens testing state");
        lateVm.ExecuteCancelTest();Check(!lateVm.IsTestingViewVisible,"test cancel returns editable view");
        delayed.SetResult(new(){Success=true});Thread.Sleep(20);lateVm.OnTick();
        Check(!lateVm.IsSuccessViewVisible,"late success after cancellation cannot reopen success");lateVm.OnFinalize();

        string root=args.Length==0?Path.GetFullPath("../../../.."):Path.GetFullPath(args[0]);
        var fixtures=Path.Combine(root,"artifacts/dialogue-onboarding-fixes-20261002/model-fixtures-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixtures);
        bool RejectModel(){try{AnimusForgeModelStore.ResolveEmbedding(fixtures);return false;}catch(InvalidOperationException){return true;}}
        Check(RejectModel(),"missing module ONNX directory rejected");
        var onnx=Path.Combine(fixtures,"ONNX");Directory.CreateDirectory(onnx);
        Check(RejectModel(),"missing model rejected");
        var model=Path.Combine(onnx,"model_quantized.onnx");File.WriteAllText(model,"synthetic model file; no inference asserted");
        Check(RejectModel(),"missing tokenizer/config rejected");
        var tokenizer=Path.Combine(onnx,"tokenizer.json");File.WriteAllText(tokenizer,"{}");
        Check(RejectModel(),"missing config rejected");
        var config=Path.Combine(onnx,"config.json");File.WriteAllText(config,"bad-json");
        Check(RejectModel(),"broken config rejected");
        File.WriteAllText(config,"{}");AnimusForgeModulePaths.Root=fixtures;
        Check(AnimusForgeModelStore.ResolveEmbedding().ModelPath==model,"file resolver uses actual active module root");
        File.WriteAllText(model,"");Check(RejectModel(),"empty model rejected");
        File.Delete(model);var full=Path.Combine(onnx,"model.onnx");File.WriteAllText(full,"synthetic");
        Check(RejectModel(),"non-quantized external data required");File.WriteAllText(full+"_data","synthetic");
        Check(AnimusForgeModelStore.ResolveEmbedding().ModelPath==full,"non-quantized path with sidecar accepted at file layer");
        File.Delete(tokenizer);Check(RejectModel(),"removed tokenizer detected even after earlier successful file resolve");
        var xml=XDocument.Load(Path.Combine(root,"extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueConversation.xml"));
        XElement Id(string id)=>xml.Descendants().Single(x=>(string)x.Attribute("Id")==id);
        XElement Resolve(XElement node,string path){foreach(string part in path.Split('\\')) node=node.Element("Children").Elements().Single(x=>(string)x.Attribute("Id")==part);return node;}
        var rootNode=Id("AFDialogueConversationRoot");Check(Resolve(rootNode,(string)rootNode.Attribute("AnswerList"))==Id("AnswerList"),"native AnswerList binding resolves through actual scroll/clip hierarchy");
        Check((string)Id("AnswerListContainer").Attribute("HeightSizePolicy")=="Fixed"&&(int)Id("AnswerListContainer").Attribute("SuggestedHeight")==190,"answers bounded inside console parent");
        Check((string)Id("AFDialogueAnswerClip").Attribute("ClipContents")=="true","overflow clipped not unclickable outside ancestors");
        Check(Resolve(Id("AFDialogueAnswerScroll"),(string)Id("AFDialogueAnswerScroll").Attribute("InnerPanel"))==Id("AnswerList"),"scroll inner panel resolves");
        Check(Id("AnswerList").Element("ItemTemplate").Elements().Single().Name.LocalName=="AFDialogueConversationItem","scoped answer template consumed");
        var item=XDocument.Load(Path.Combine(root,"extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueConversationItem.xml"));
        Check(item.Descendants().Single(x=>(string)x.Attribute("Id")=="OptionButtonParent").Attribute("WidthSizePolicy").Value=="StretchToParent","long options use actual panel width");
        Check(item.Descendants().Any(x=>(string)x.Attribute("Command.Click")=="ExecuteAction")&&item.Descendants().Any(x=>(string)x.Attribute("Text")=="@ItemText"),"native text/click contract retained");
        foreach(string path in new[]{"extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml","content/modules/AF.Module.Conversation/GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml"})
            Check(XDocument.Load(Path.Combine(root,path)).Descendants().Any(x=>(string)x.Attribute("SuppressOpeningInteractionKey")=="true"),"opening-key guard bound in "+path);
        var onboarding=XDocument.Load(Path.Combine(root,"content/modules/AF.Module.Onboarding/GUI/Prefabs/AnimusForgeApiOnboardingPopup.xml"));
        Check(onboarding.Descendants().Any(x=>(string)x.Attribute("Command.Click")=="ExecuteUseExistingConfig"),"existing API action visible");
        var overlay=Read(root,"src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs");
        var set=overlay.Substring(overlay.IndexOf("private void SetInputVisible("));set=set.Substring(0,set.IndexOf("private void SetLayerForButtonsOnly("));
        Check(set.Contains("_isSubmitting = false;")&&set.Contains("_dataSource.SetBusy(false);")&&set.Contains("_submitGeneration++;"),"escape route clears busy and invalidates stale callbacks");
        Check(set.Contains("_modeText.EnterAi")&&set.Contains("_modeText.LeaveAi")&&set.Contains("ConversationHelper.UpdateDialogText(restoreText)"),"production mode switch consumes text owner output");
        Check(set.Contains("HasCurrentConversationContext")&&!set.Contains("_modeTextScope.IsCurrent()"),"mode text survives later request revision while retaining conversation identity");
        var admission=Read(root,"src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs");
        Check(admission.Contains("HasCurrentConversationContext()")&&admission.Contains("HasCurrentContext() && _owner.IsNativeConversationContextCurrent"),"display scope seam does not weaken existing submission guard");
        var encounterHost=Read(root,"src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs");
        Check(encounterHost.Contains("RegisterNativeDialogueHandoff(target);")&&encounterHost.Contains("manager.ConversationEndOneShot += OnNativeDialogueHandoffEnded"),"real native conversation entry wires end hook");
        Check(encounterHost.Contains("_nativeDialogueHandoffManager.ConversationEndOneShot -= OnNativeDialogueHandoffEnded"),"captured manager hook cleaned after close/session change");
        Check(encounterHost.Contains("_nativeDialogueHandoffOwner.IsCurrent(PlayerEncounter.Current")&&encounterHost.Contains("NativeDialogueReturnPolicy.ShouldReturn"),"real end callback consumes encounter identity and return policy");
        Check(encounterHost.Contains("CanReturnFromNativeDialogueHandoff(_nativeDialogueReturnHero, true)"),"queued return rechecks release/combat/native activity before menu mutation");
        Check(encounterHost.Contains("string.Equals(_suppressCustomEncounterMenuReason, \"native_dialogue_handoff\""),"handoff never clears another mechanism's suppression");
        var host=Read(root,"src/modules/AF.Module.Onboarding/Host/ModOnboardingBehavior.cs");
        var popup=host.Substring(host.IndexOf("if (AnimusForgeApiOnboardingPopup.Show("));popup=popup.Substring(0,popup.IndexOf("List<InquiryElement> list"));
        Check(!popup.Contains("_setupDone = true;")&&popup.Contains("ShowImportSetupPopup"),"completion enters import before setup-done gate");
        Check(popup.Contains("_onboardingSession.CancelWelcome();")&&popup.Contains("_activeOnboardingStage = OnboardingUiStage.None;"),"explicit dismissal not resumed next tick");
        Check(host.Contains("_apiOnlySetupFlowActive = _setupDone;"),"terminal new-save route remains full onboarding");
        var my=Read(root,"src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs");
        var newGame=my.Substring(my.IndexOf("private void OnNewGameCreated("));newGame=newGame.Substring(0,newGame.IndexOf("private void OnGameLoaded("));
        Check(newGame.Contains("QueueMissingOnnxGateCheck(TimeSpan.Zero);"),"new campaign actually queues ONNX gate");
        Check(my.Contains("AnimusForgeApiOnboardingPopup.IsOpen")&&my.Contains("IsSetupUiActive == true"),"ONNX gate waits for foreground owner without losing pending check");
        using(var map=System.Text.Json.JsonDocument.Parse(Read(root,"content/content-map.json")))
            Check(map.RootElement.GetProperty("entries").EnumerateArray().Any(x=>x.GetProperty("target").GetString()=="GUI/Prefabs/AFDialogueConversationItem.xml"),"new template included in unified content map");
        var adapter=Read(root,"extensions/AnimusForge.DialogueUI/src/Native/NativeUiAdapter.cs");
        Check(adapter.Contains("vm.AutoEnterAiMode && !af.IsCustomAnswerVisible") && adapter.Contains("nameof(NpcOpeningPrefix)")&&!adapter.Contains("_defaultModeApplied"),"one consumer applies option with no later forced switch");
        if(args.Length>1) VerifyProductionMetadata(args[1]);
        Console.WriteLine("PASS: "+checks+" production-linked behavioral and XML/source contract checks; engine/network are stubbed, not live-game acceptance.");
    }
    static void VerifyProductionMetadata(string candidate)
    {
        // Do not trigger module initializers: offline game dependencies/Harmony are not a live host.
        var assembly=Assembly.LoadFile(Path.GetFullPath(candidate));
        const BindingFlags all=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        foreach(string name in new[]{"OpeningInteractionInputGuard","ConversationReplyWaitOwner"})
        {
            var type=assembly.GetType("AnimusForge."+name,true);
            Check(type.IsSealed&&type.GetConstructor(all,null,Type.EmptyTypes,null)!=null,"actual DLL owner type/constructor: "+name);
            foreach(var method in type.GetMethods(all|BindingFlags.DeclaredOnly))
                Check(method.GetMethodBody()?.GetILAsByteArray()?.Length>0,"actual DLL executable method body: "+name+"."+method.Name);
        }
        Console.WriteLine("Production DLL metadata (not module-init/runtime replay): "+Path.GetFileName(Path.GetDirectoryName(candidate))+" MVID="+assembly.ManifestModule.ModuleVersionId);
    }
}
