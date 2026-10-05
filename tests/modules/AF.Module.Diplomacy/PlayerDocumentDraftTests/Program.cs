using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge;

internal static class Test
{
    internal static readonly int UiThread = Environment.CurrentManagedThreadId;
    internal static int Count;
    internal static void That(bool value, string message)
    { Count++; if (!value) throw new InvalidOperationException(message); }
    internal static void Until(Func<bool> ready, string reason)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!ready()) { if (timer.ElapsedMilliseconds > 5000) throw new Exception("Timed out: " + reason); Thread.Sleep(2); }
    }
}

internal sealed class ControlledHandler : HttpMessageHandler
{
    internal sealed class Call
    {
        internal JObject Body;
        internal string Url, Auth;
        internal bool Cancelled;
        internal readonly TaskCompletionSource<HttpResponseMessage> Reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    internal readonly List<Call> Calls = new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var call = new Call { Body = JObject.Parse(await request.Content.ReadAsStringAsync()), Url = request.RequestUri.ToString(), Auth = request.Headers.Authorization?.Parameter };
        token.Register(() => call.Cancelled = true);
        lock (Calls) Calls.Add(call);
        // Intentionally do not complete on cancellation: exercise a late provider.
        return await call.Reply.Task;
    }
    internal Call Get(int index) { lock (Calls) return Calls.Count > index ? Calls[index] : null; }
    internal void Finish(int index, string body, string finish = "stop", HttpStatusCode status = HttpStatusCode.OK)
    {
        string content = new JObject { ["choices"] = new JArray(new JObject { ["finish_reason"] = finish,
            ["message"] = new JObject { ["content"] = body } }) }.ToString();
        Get(index).Reply.SetResult(new HttpResponseMessage(status) { Content = new StringContent(content) });
    }
}

internal static class Program
{
    private static string Json(string body) => new JObject { ["body"] = body }.ToString();
    private static string Draft = "瓦兰迪亚，我方愿就和平展开商议。双方可就停战条件继续磋商，望贵国慎重考虑，并给予明确答复。";
    private static void Drain(WorldDiplomacyComposePopupVM vm)
    { Test.Until(() => { vm.ProcessAutoDraftCompletion(); return vm.AutoDraftText == "书记官代笔"; }, "main thread completion"); }
    private static (ControlledHandler handler, WorldDiplomacyComposePopupVM vm) Window(Action<string> submit = null)
    {
        DuelSettings.Current = new DuelSettings();
        var handler = new ControlledHandler();
        DuelSettings.GlobalClient = new HttpClient(handler);
        return (handler, new WorldDiplomacyComposePopupVM("宣言", "", "", submit, null));
    }

    private static void Main()
    {
        Rules();
        SnapshotAndRewrite();
        Editing();
        ClosedAndLoaded();
        FailuresAndFallback();
        CompatibilityAndLegacy();
        PrefabBindings();
        Console.WriteLine($"Player diplomatic drafting: PASS ({Test.Count} assertions). Actual production VM/application/client/protocol/transport; game/MCM host substituted.");
    }

    private static void Rules()
    {
        var input = new WorldDiplomacyPlayerDraftInput("与瓦兰迪亚议和", 40, 200, 1);
        var messages = WorldDiplomacyPlayerDraftRules.BuildMessages(input, "简明冷峻");
        Test.That(messages.Count == 3 && (string)messages[2]["content"] == "MODE=PLAYER_DRAFT\n【当前纸面内容】\n与瓦兰迪亚议和", "Only current player material");
        Test.That((string)messages[1]["content"] == "【沿用外交MCM写作偏好，仅在忠实于玩家原意时适用】\n简明冷峻", "Shared preference verbatim");
        Test.That(messages[0]["content"].ToString().Contains("40—200") && messages[0]["content"].ToString().Contains("禁止自行添加"), "Length and authorship guard");
        Test.That(WorldDiplomacyPlayerDraftRules.BuildMessages(input, "").Count == 2, "Blank preference not replaced by autonomous contract");
        Test.That(WorldDiplomacyPlayerDraftRules.Parse(Json(Draft), input).Body == Draft, "Valid draft preserved");
        foreach (var invalid in new[] { "", "{}", "null", "[]", "{\"body\":null}", "{\"body\":42}", "{\"body\":\"甲\",\"actions\":[]}", "{\"body\":\"甲\",\"body\":\"乙\"}", "```json\n" + Json(Draft) + "\n```", Json(Draft) + " trailing", Json("太短"), Json(new string('字', 201)) })
            Test.That(!WorldDiplomacyPlayerDraftRules.Parse(invalid, input).Success, "Reject malformed, duplicate, hidden fields or invalid length: " + invalid.Substring(0, Math.Min(25, invalid.Length)));
        Test.That(!WorldDiplomacyPlayerDraftRules.Parse(Json(new string('字', 6001)), input).Success, "No silent truncation");
        Test.That(WorldDiplomacyPlayerDraftRules.CountVisibleCharacters("甲， \t\n乙😀") == 4, "Punctuation counts, formatting whitespace does not, surrogate pair counts once");
        var fixedLength = new WorldDiplomacyPlayerDraftInput("提纲", 100, 40, 1);
        Test.That(fixedLength.MaximumCharacters == 100, "max below min normalized");
        Test.That(WorldDiplomacyPlayerDraftRules.Parse(Json(new string('字', 100)), fixedLength).Success, "Exact endpoints allowed");
    }

    private static void SnapshotAndRewrite()
    {
        int publishes = 0;
        var (handler, vm) = Window(_ => publishes++);
        vm.ExecuteAutoDraft();
        Test.That(handler.Get(0) == null && vm.HintText.Contains("要点"), "Empty input sends nothing");
        vm.BodyText = "与瓦兰迪亚议和";
        Test.That(vm.CanPublish && vm.CanAutoDraft, "Both actions ready");
        vm.ExecuteAutoDraft(); vm.ExecuteAutoDraft(); vm.ExecutePublish();
        Test.That(!vm.CanPublish && !vm.CanAutoDraft && publishes == 0, "Busy blocks publish and duplicate");
        DuelSettings.Current.WorldDiplomacyPrompt = "新文风";
        DuelSettings.Current.EventAndRebellionModelName = "new-model";
        DuelSettings.Current.EventAndRebellionApiKey = "new-key";
        DuelSettings.Current.EventAndRebellionApiMaxTokens = 512;
        DuelSettings.Current.MinChars = 100;
        int reads = DuelSettings.Reads;
        Test.Until(() => handler.Get(0) != null, "request sent");
        var first = handler.Get(0);
        Test.That(first.Auth == "fixture-event" && (string)first.Body["model"] == "event-model", "Main-thread route and credentials frozen");
        Test.That((int)first.Body["max_tokens"] == 1800 && (float)first.Body["temperature"] == 0.4f, "Diplomatic token and temperature profile");
        Test.That(first.Body["messages"].ToString().Contains("简明冷峻") && !first.Body["messages"].ToString().Contains("新文风"), "Preference frozen");
        Test.That(DuelSettings.Reads == reads && handler.Get(1) == null, "No worker settings read or duplicate send");
        handler.Finish(0, Json(Draft)); Drain(vm);
        Test.That(vm.BodyText == Draft && vm.CanPublish && publishes == 0, "Main-thread fill without publication");
        DuelSettings.Current.MinChars = 40;
        vm.ExecuteAutoDraft(); Test.Until(() => handler.Get(1) != null, "rewrite");
        Test.That(handler.Get(1).Body["messages"].Last["content"].ToString().EndsWith(Draft), "Unedited text becomes fresh rewrite input");
        Test.That(handler.Get(1).Body["messages"].ToString().Contains("新文风"), "Next request gets changed preference");
        string rewritten = Draft.Replace("慎重考虑", "认真斟酌");
        handler.Finish(1, Json(rewritten)); Drain(vm);
        Test.That(vm.BodyText == rewritten, "Rewrite replaces current text");
        vm.ExecutePublish(); Test.That(publishes == 1, "Only explicit player publication"); vm.OnFinalize();
    }

    private static void Editing()
    {
        var (handler, vm) = Window(); vm.BodyText = "提纲"; vm.ExecuteAutoDraft();
        Test.Until(() => handler.Get(0) != null, "edit request");
        vm.BodyText = "玩家新条件";
        handler.Finish(0, Json(Draft)); Drain(vm);
        Test.That(vm.BodyText == "玩家新条件" && vm.HintText.Contains("已修改"), "In-flight edit protected");
        vm.ExecuteAutoDraft(); Test.Until(() => handler.Get(1) != null, "edit then undo request");
        vm.BodyText = "另一稿"; vm.BodyText = "玩家新条件";
        handler.Finish(1, Json(Draft)); Drain(vm);
        Test.That(vm.BodyText == "玩家新条件", "Revision change rejected even after text restored"); vm.OnFinalize();
    }

    private static void ClosedAndLoaded()
    {
        var (handler, old) = Window(); old.BodyText = "旧窗口"; old.ExecuteAutoDraft();
        Test.Until(() => handler.Get(0) != null, "close request"); old.OnFinalize(); old.OnFinalize();
        Test.Until(() => handler.Get(0).Cancelled, "close cancels sender");
        var fresh = new WorldDiplomacyComposePopupVM("回应", "", "", null, null) { BodyText = "新窗口" };
        handler.Finish(0, Json(Draft)); Thread.Sleep(30); old.ProcessAutoDraftCompletion();
        Test.That(old.BodyText == "旧窗口" && !old.CanPublish && fresh.BodyText == "新窗口", "Closed late result cannot affect either window");
        fresh.ExecuteAutoDraft(); Test.Until(() => handler.Get(1) != null, "loaded request");
        SaveRuntimeGuard.AdvanceGeneration("test-load"); handler.Finish(1, Json(Draft)); Thread.Sleep(30);
        fresh.ProcessAutoDraftCompletion();
        Test.That(!fresh.IsCurrentDraftWindow && fresh.BodyText == "新窗口", "Load generation protects source"); fresh.OnFinalize();
    }

    private static void FailuresAndFallback()
    {
        var (handler, vm) = Window(); vm.BodyText = "保持原文"; vm.ExecuteAutoDraft();
        Test.Until(() => handler.Get(0) != null, "failure request"); handler.Finish(0, "failure", status: HttpStatusCode.InternalServerError); Drain(vm);
        Test.That(vm.BodyText == "保持原文" && vm.CanAutoDraft && handler.Get(1) == null, "Failure keeps input, no domain retry");
        vm.ExecuteAutoDraft(); Test.Until(() => handler.Get(1) != null, "truncated"); handler.Finish(1, Json(Draft), "length"); Drain(vm);
        Test.That(vm.BodyText == "保持原文" && vm.HintText.Contains("截断"), "Truncation rejected");
        vm.ExecuteAutoDraft(); Test.Until(() => handler.Get(2) != null, "bad length"); handler.Finish(2, Json("太短")); Drain(vm);
        Test.That(vm.BodyText == "保持原文" && vm.HintText.Contains("字数"), "Invalid length keeps input"); vm.OnFinalize();
        var (fallback, reply) = Window(); DuelSettings.Current.EventAndRebellionApiKey = "";
        reply.BodyText = "我方接受原案，请对方安排后续交涉。"; reply.ExecuteAutoDraft(); Test.Until(() => fallback.Get(0) != null, "fallback");
        Test.That(fallback.Get(0).Url.Contains("main.example") && fallback.Get(0).Auth == "fixture-main", "Partial event config falls back to main");
        Test.That(fallback.Get(0).Body["messages"].Last["content"].ToString().EndsWith(reply.BodyText), "Reply has only player-supplied intent");
        fallback.Finish(0, Json(Draft)); Drain(reply); reply.OnFinalize();
        var (missing, invalid) = Window(); DuelSettings.Current.EventAndRebellionApiKey = ""; DuelSettings.Current.ApiKey = "";
        invalid.BodyText = "提纲"; invalid.ExecuteAutoDraft();
        Test.That(missing.Get(0) == null && invalid.CanPublish && invalid.HintText.Contains("API"), "Bad configuration does not erase or lock form"); invalid.OnFinalize();
    }

    private static void CompatibilityAndLegacy()
    {
        var (handler, vm) = Window(); vm.BodyText = "提纲"; vm.ExecuteAutoDraft(); Test.Until(() => handler.Get(0) != null, "compatibility");
        handler.Get(0).Reply.SetResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("unsupported reasoning_effort") });
        Test.Until(() => handler.Get(1) != null, "thinking fallback");
        Test.That(handler.Get(1).Body["reasoning_effort"] == null && handler.Get(1).Body["messages"].ToString() == handler.Get(0).Body["messages"].ToString(), "Protocol downgrade preserves frozen prompt");
        handler.Finish(1, Json(Draft)); Drain(vm); Test.That(vm.BodyText == Draft, "Compatibility fallback succeeds"); vm.OnFinalize();
        // Existing diplomatic requests retain their regular one-attempt send path.
        DuelSettings.StrictThread = false;
        var legacy = WorldDiplomacyLlmClient.CallMessagesWithRetriesAsync(new JArray(new JObject { ["role"] = "user", ["content"] = "legacy" }), 900, 90000, "legacy-test", SaveRuntimeGuard.CaptureGeneration(), maxAttempts: 1);
        Test.Until(() => handler.Get(2) != null, "legacy"); handler.Finish(2, "legacy-output");
        Test.That(legacy.GetAwaiter().GetResult().Content == "legacy-output", "Legacy client behavior preserved"); DuelSettings.StrictThread = true;
    }

    private static void PrefabBindings()
    {
        var xml = System.Xml.Linq.XDocument.Load("content/modules/AF.Module.Diplomacy/GUI/Prefabs/WorldDiplomacyComposePopup.xml");
        Type vm = typeof(WorldDiplomacyComposePopupVM);
        foreach (var attribute in xml.Descendants().Attributes())
        {
            if (attribute.Name.LocalName.StartsWith("Command."))
                Test.That(vm.GetMethod(attribute.Value) != null, "Prefab command bound: " + attribute.Value);
            if (attribute.Value.StartsWith("@"))
            {
                var property = vm.GetProperty(attribute.Value.Substring(1));
                Test.That(property != null && property.IsDefined(typeof(TaleWorlds.Library.DataSourceProperty), true), "Prefab data source bound: " + attribute.Value);
            }
        }
    }
}
