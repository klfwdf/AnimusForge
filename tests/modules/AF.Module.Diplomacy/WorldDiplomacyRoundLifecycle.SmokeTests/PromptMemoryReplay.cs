using AnimusForge;
internal static class PromptMemoryReplay
{
    private sealed class Prompt : IDiplomacyPromptSource, IDiplomacyOralPromptSource
    {
        internal bool TopicAllowed = true, Independent, PlayerRuler = true;
        internal int Captures, Wars;
        internal string Oral = "";
        internal int OralReads;
        internal bool Native;
        public bool UseFormalCommitments => !Native;
        public string OralArrangementContext() { OralReads++; return Oral; }
        internal List<string> Templates = new();
        public DiplomacyConversationEligibilitySnapshot CaptureEligibility() => new(true, false, false, true, false, TopicAllowed, true, false, true, PlayerRuler);
        public DiplomacyPromptSnapshot Capture() { Captures++; return new(true, true, !Independent, false, true, Independent, false, "主角", "家族", "城堡", "", 3); }
        public IReadOnlyList<DiplomacyPromptWar> CaptureWars() { Wars++; return new[] { new DiplomacyPromptWar("b", "敌国", 10, 300, 100, 30, 10, 2, 1, 2000, 1000, 10000, true) }; }
        public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot) { snapshot = default; return false; }
        public string Template(string key, Dictionary<string, string> tokens) { Templates.Add(key); return key == "player_independent_settlement_clan" ? "" : key; }
        public string AnnexationInstruction() => "兼并规则";
        public void Log(string message) => throw new Exception(message);
    }
    private sealed class Memory : IWorldDiplomacyMemorySource
    {
        internal int Captures;
        internal WorldDiplomacyStorage Storage = new();
        internal HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase);
        public bool TryCapture(string hero, string kingdom, out WorldDiplomacyMemorySnapshot snapshot)
        { Captures++; snapshot = new(Storage, kingdom, Known); return true; }
        public string FormatDate(int day) => "日期" + day;
    }
    private struct Postprocess : IDiplomacyPostprocessContextSource, IDiplomacyOralPostprocessSource
    {
        internal bool Native;
        public bool UseFormalCommitments => !Native;
        public string NativeActionInstruction() => "AI外交已关闭 [ACTION:DIPLOMACY:DECLARE_WAR:a:b] 原版执行";
        public bool HasSpeaker => true;
        public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot) { snapshot = default; return false; }
        public DiplomacyConversationEligibilitySnapshot CaptureEligibility() => new(true, false, false, true, false, true, true, false, true, true);
        public DiplomacyPostprocessKingdomSnapshot CaptureKingdoms() => new(true, "a", "甲", true, false, "p", "玩家国", true, true, Array.Empty<DiplomacyKingdomSummary>());
        public string GetAnnexationHint() => "";
        public bool ArePlayerAndNpcAtWar() => false;
        public int CalculateDailyTribute(bool npcPays) => 0;
        public void LogFailure(string message) => throw new Exception(message);
        public string OralArrangementContext() => "arrangement=owned;source_document=original;version=2";
    }
    internal static void Run()
    {
        var prompt = new Prompt();
        Test.True(DiplomacyPromptApplication.Build(prompt, "").Length == 0 && prompt.Captures == 0 && prompt.Wars == 0, "unselected diplomacy does not capture political or war context");
        string text = DiplomacyPromptApplication.Build(prompt, "【附加规则:diplomacy】");
        Test.True(text.Contains(AnimusForge.DiplomacyDialogue.DialoguePeaceClarificationRules.MainReplyInstruction),
            "shared main reply asks for missing tribute duration before promising publication");
        var peacePostprocess = new Postprocess();
        Test.True(DiplomacyPostprocessContextApplication.Build(ref peacePostprocess).Contains(
            AnimusForge.DiplomacyDialogue.DialoguePeaceClarificationRules.PostprocessInstruction),
            "shared postprocess checks complete peace terms without inventing duration");
        Test.True(text.Contains("都是国王") && text.Contains("你方明显占优") && text.Contains("兼并规则") && text.Contains("level_3"), "royal prompt, war position, annexation and trust share one application");
        Test.True(prompt.Captures == 1 && prompt.Wars == 1, "capture world values once per selected prompt");
        prompt = new Prompt { Oral = "arrangement=owned;source_document=original;version=2" };
        Test.True(DiplomacyPromptApplication.Build(prompt, "").Length == 0 && prompt.OralReads == 0,
            "unselected topic never reads private oral arrangements");
        Test.True(DiplomacyPromptApplication.Build(prompt, "【附加规则:diplomacy】").Contains(prompt.Oral) && prompt.OralReads == 1,
            "selected diplomacy includes exact arrangement/source identity through the module read port");
        var postprocess = new Postprocess();
        string post = DiplomacyPostprocessContextApplication.Build(ref postprocess);
        Test.True(post.Contains(postprocess.OralArrangementContext()) && post.Contains("DIPLOMACY:COMMIT:action=") && post.Contains("DIPLOMACY:COMMITMENT:arrangement="),
            "postprocessing receives arrangement identity and the previously approved oral tags");
        Test.True(!post.Contains("DIPLOMACY:MAKE_TRADE:") && post.Contains("DIPLOMACY:DECLARE_WAR:p:a"),
            "NPC consent submits formal documents while the existing player's explicit-war exception remains available");
        var nativePost = new Postprocess { Native=true };
        string nativeTags = DiplomacyPostprocessContextApplication.Build(ref nativePost);
        Test.True(nativeTags.Contains(nativePost.NativeActionInstruction()) && !nativeTags.Contains("DIPLOMACY:COMMIT") && !nativeTags.Contains("arrangement=owned"),
            "disabled diplomacy uses native postprocessing template without formal tags or private arrangements");
        var nativePrompt = new Prompt {Native=true,Oral="AI外交已关闭，可按原版外交规则直接达成"};
        string nativeMain = DiplomacyPromptApplication.Build(nativePrompt,"【附加规则:diplomacy】");
        Test.True(nativeMain.Contains(nativePrompt.Oral) && !nativeMain.Contains(AnimusForge.DiplomacyDialogue.DialoguePeaceClarificationRules.MainReplyInstruction) && !nativeMain.Contains("兼并规则"),
            "disabled main reply does not force formal peace publication or annexation flow");
        prompt = new Prompt { Independent = true, PlayerRuler = false };
        text = DiplomacyPromptApplication.Build(prompt, "【附加规则:diplomacy】");
        Test.True(text.Contains("独立有城家族：家族") && prompt.Templates.SequenceEqual(new[] { "player_independent_settlement_clan", "level_3" }), "missing independent-clan template retains trust fallback");
        prompt = new Prompt { PlayerRuler = false };
        Test.True(DiplomacyPromptApplication.Build(prompt, "【附加规则:diplomacy】").Contains("player_not_king") && prompt.Templates.Count == 1, "non-ruler state stops before trust template");
        prompt = new Prompt { TopicAllowed = false };
        Test.True(DiplomacyPromptApplication.Build(prompt, "【附加规则:diplomacy】").Length == 0 && prompt.Wars == 0, "ineligible NPC never scans wars");
        var memory = new Memory();
        Test.True(WorldDiplomacyMemoryApplication.Build(memory, "hero", "a", "", null, false) == "" && memory.Captures == 0, "idle memory query does no capture");
        for (int i = 0; i < 10; i++)
        {
            var doc = new WorldDiplomacyDocument { DocumentId = "d" + i, AuthorKingdomId = "a", AuthorKingdomName = "甲国", Title = "可见公文" + i, Body = "正文" + i, Day = i };
            memory.Storage.Documents.Add(doc); memory.Known.Add(doc.DocumentId);
        }
        memory.Storage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "secret", Title = "秘密", Body = "不可见内容", Day = 100 });
        text = WorldDiplomacyMemoryApplication.Build(memory, "hero", "a", "外交", null, false);
        Test.True(memory.Captures == 1 && !text.Contains("不可见内容") && text.Split("[直接相关]").Length - 1 == 3, "one captured knowledge set preserves visibility and three direct-document slots");
        text = WorldDiplomacyMemoryApplication.Build(memory, "hero", "a", "可见公文9", null, false);
        Test.True(text.Contains("[当前问题命中]") && text.Contains("正文9"), "known title triggers query-aware memory without a topic hit");
        Test.True(WorldDiplomacyMemoryApplication.Build(memory, "hero", "a", "秘密", null, false) == "", "unknown titles cannot trigger memory");
    }
}
