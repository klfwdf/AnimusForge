using AnimusForge;
internal static class PromptMemoryReplay
{
    private sealed class Prompt : IDiplomacyPromptSource
    {
        internal bool TopicAllowed = true, Independent, PlayerRuler = true;
        internal int Captures, Wars;
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
    internal static void Run()
    {
        var prompt = new Prompt();
        Test.True(DiplomacyPromptApplication.Build(prompt, "").Length == 0 && prompt.Captures == 0 && prompt.Wars == 0, "unselected diplomacy does not capture political or war context");
        string text = DiplomacyPromptApplication.Build(prompt, "【附加规则:diplomacy】");
        Test.True(text.Contains("都是国王") && text.Contains("你方明显占优") && text.Contains("兼并规则") && text.Contains("level_3"), "royal prompt, war position, annexation and trust share one application");
        Test.True(prompt.Captures == 1 && prompt.Wars == 1, "capture world values once per selected prompt");
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
