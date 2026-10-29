using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

// Actual DLL composers and wire projection. Synthetic detached context only; no provider/game.
internal static class RequestPromptAuditChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static object Invoke(Type type, string name, params object[] args) => type.GetMethods(All)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(null, args);
    static object New(Assembly product, string name) => Activator.CreateInstance(product.GetType(name, true), true);
    static void Set(object obj, string name, object value)
    {
        var field = obj.GetType().GetField(name, All);
        if (field != null) field.SetValue(obj, value);
        else obj.GetType().GetProperty(name, All).SetValue(obj, value, null);
    }
    static object Get(object obj, string name) => obj.GetType().GetField(name, All)?.GetValue(obj)
        ?? obj.GetType().GetProperty(name, All)?.GetValue(obj, null);
    internal static void Run(Assembly product)
    {
        var failures = new List<string>(); int checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }
        bool Han(string value) => Regex.IsMatch(value ?? "", @"[\u3400-\u9fff]");
        string root = Environment.GetEnvironmentVariable("AF_TEST_MODULE_ROOT");
        bool english = (string)Invoke(product.GetType("AnimusForge.LlmPromptTexts", true), "Get", "AF_LLM_PROMPT_ContentLanguage", "zh-CN") == "en";
        var texts = product.GetType("AnimusForge.DiplomacyPromptTexts", true);
        Func<string,string,string> localize = (key, fallback) => (string)Invoke(texts,"Get",key,fallback);
        string date = "1084年夏季11天";
        object entry = New(product,"AnimusForge.WorldDiplomacyCanonicalHistoryEntry");
        Set(entry,"GameDate",date); Set(entry,"Text","original quoted content");
        var render = product.GetType("AnimusForge.Refactor.Domain.WorldDiplomacyCanonicalRenderRules",true);
        string canonical = (string)Invoke(render,"RenderCanonicalHistoryEntry",entry,localize);
        Check(english ? !Han(canonical) : canonical.Contains(date),"canonical metadata date follows package");
        Check((string)Get(entry,"GameDate") == date,"saved canonical date unchanged");
        Check(((string)Invoke(render,"RenderCanonicalHistoryEntry",entry)).Contains(date),"compatibility overload without package callback preserves raw date");

        object storage = New(product,"AnimusForge.WorldDiplomacyStorage");
        object doc = New(product,"AnimusForge.WorldDiplomacyDocument");
        Set(doc,"DocumentId","audit-doc"); Set(doc,"AuthorKingdomId","battania"); Set(doc,"TargetKingdomId","empire");
        Set(doc,"GameDate",date); Set(doc,"Title","Original title"); Set(doc,"Body","quoted 中文正文 {NAME} $1"); Set(doc,"IsReadyForPublication",true);
        ((IList)Get(storage,"Documents")).Add(doc);
        object job = New(product,"AnimusForge.WorldDiplomacyJob");
        Set(job,"AuthorKingdomId","battania"); Set(job,"SourceDocumentId","audit-doc");
        var history = product.GetType("AnimusForge.WorldDiplomacyRequestHistoryApplication",true);
        // Real declaration call supplies no localization callback; exercise that exact default path.
        string window = (string)Invoke(history,"Build",storage,job,0,16000L,null,null);
        Check(english ? !window.Contains(date) : window.Contains(date),"declaration reading-window date follows package without caller callback");
        Check(window.Contains("quoted 中文正文 {NAME} $1"),"original generated prose is kept verbatim");
        Check((string)Get(doc,"GameDate") == date && (string)Get(doc,"Body") == "quoted 中文正文 {NAME} $1","document source is not rewritten");

        var context = New(product,"AnimusForge.NpcRulerPolicyBehavior+NpcRulerPolicyBatchContext");
        var target = New(product,"AnimusForge.NpcRulerPolicyBehavior+NpcRulerPolicyKingdomContext");
        Set(context,"BatchSize",1); Set(target,"KingdomId","battania"); Set(target,"KingdomName","Battania");
        Set(target,"RulerHeroId","lord_5_1"); Set(target,"RulerName","Caladog");
        ((IList)Get(context,"Kingdoms")).Add(target);
        Set(context,"CompactWorldContext","CurrentWorldFacts: peace; ruler Caladog; kingdom Battania.");
        string editable = JObject.Parse(File.ReadAllText(Path.Combine(root,"ModuleData/Defaults/CustomPrompts/Policy/NpcRulerPolicyPrompt.json")))["Text"].Value<string>();
        var policy = product.GetType("AnimusForge.NpcRulerPolicyBehavior",true);
        string Draft() => (string)Get(Invoke(policy,"ComposeNpcPolicyDraftPrompt",context,editable),"SystemPrompt");
        string cleanDraft = Draft();
        Check(english ? !Han(cleanDraft) : Han(cleanDraft),"actual NPC policy draft fixed instructions use content-package language");
        Check(!cleanDraft.Contains("30}}。"),"technical policy JSON example has no Chinese trailing full stop");
        Check(cleanDraft.Contains("Caladog") && cleanDraft.Contains("durationDays") && cleanDraft.Contains("votePersonality"),"policy identity/duration/personality contract retained");
        Set(context,"CompactWorldContext","CurrentWorldFacts: preserved 旧政策正文 {NAME} $1");
        string dynamicDraft = Draft();
        Check(dynamicDraft.Contains("旧政策正文 {NAME} $1"),"policy dynamic original context is not silently translated");

        object compat = product.GetType("AnimusForge.LlmApiCompat",true);
        var settings = product.GetType("AnimusForge.DuelSettings", true);
        string diplomacyPreference = JObject.Parse(File.ReadAllText(Path.Combine(root,"ModuleData/Defaults/CustomPrompts/WorldDiplomacyPrompt.json")))["Text"].Value<string>();
        string common = (string)Invoke(settings,"BuildWorldDiplomacyCommonContract",diplomacyPreference);
        common = (string)Invoke(settings,"LocalizeWorldDiplomacyContractForPrompt",common);
        var contract = product.GetType("AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules",true);
        Set(job,"Kind","generate");
        Set(job,"SystemPrompt",Invoke(contract,"BuildGenerationSystemPrompt",common,300,1200,localize));
        Set(doc,"Body","The trading agreement was proposed. No action has executed.");
        Set(job,"DeclarationHistoryBlock",Invoke(history,"Build",storage,job,0,16000L,null,null));
        Set(job,"UserPrompt",Invoke(contract,"BuildDeclareModePrompt","Current kingdom: Battania. Target: Empire.",localize));
        var diplomacyMessages = (JArray)Invoke(product.GetType("AnimusForge.WorldDiplomacyLlmMessageApplication",true),"BuildLlmMessageArray",job,new Func<long,string>(_=>canonical));
        Check(diplomacyMessages.Count==3 && diplomacyMessages.Select(m=>(string)m["role"]).SequenceEqual(new[]{"system","system","user"}),"real declaration assembly retains contract/history/task roles");
        Set(context,"CompactWorldContext","CurrentWorldFacts: [AFEF NPC行为补充] [AFEF内战事实] Recorded uprising; no inferred outcome.");
        string markedPolicyDraft=Draft();
        string output = Path.Combine(root,"request-prompt-audit"); Directory.CreateDirectory(output);
        // These exact compatibility markers are consumed by the new diplomacy
        // contract validator. They retain their persisted/protocol identities;
        // all surrounding fixed prose still has to follow the package language.
        JObject compatibility;
        using (var stream = product.GetManifestResourceStream("AnimusForge.Diplomacy.PromptCompatibility.json"))
        using (var reader = new StreamReader(stream)) compatibility = JObject.Parse(reader.ReadToEnd());
        string[] stableDiplomacyMarkers = Enumerable.Range(0,5)
            .Select(i => (string)compatibility["AF_DIP_Compatibility_" + i.ToString("000")]).ToArray();
        foreach (string endpoint in new[]{"https://audit.invalid/v1/chat/completions","https://audit.invalid/anthropic/v1/messages"})
        {
            foreach (var pair in new[]{("policy",new JArray(new JObject{["role"]="system",["content"]=markedPolicyDraft})),("diplomacy",diplomacyMessages)})
            {
                var input = new JObject{["messages"]=pair.Item2}; string saved=input.ToString();
                string wire=(string)Invoke((Type)compat,"PrepareChatRequestJson",endpoint,input);
                string auditedWire = wire;
                if (pair.Item1 == "diplomacy")
                    foreach (string marker in stableDiplomacyMarkers)
                    {
                        Check(!string.IsNullOrEmpty(marker) && wire.Contains(marker), "new diplomacy retains exact contract marker identity");
                        auditedWire = auditedWire.Replace(marker, "");
                    }
                Check(!english || !Han(auditedWire),"actual "+pair.Item1+" fixed prose and AFEF prefixes follow package; exact protocol markers retained");
                Check(input.ToString()==saved,"actual "+pair.Item1+" wire projection preserves request snapshot");
                Check(!wire.Contains("AF_OUTPUT_LANGUAGE") && !wire.Contains("must reply in English"),"actual "+pair.Item1+" wire adds no reply-language lock");
                if(pair.Item1=="policy") Check(wire.Contains("[AFEF NPC_ACTION]") && wire.Contains("[Civil war fact]") && wire.Contains("no inferred outcome"),"actual policy assembly preserves actor and fact semantics after fixed marker projection");
                File.WriteAllText(Path.Combine(output,pair.Item1+(endpoint.Contains("anthropic")?"-anthropic":"-openai")+"-wire.json"),wire);
            }
        }
        var payload = new JObject { ["messages"]=new JArray(new JObject { ["role"]="system", ["content"]="[AFEF PLAYER_ACTION] [AFEF内战事实] original 中文正文 {NAME} $1" }) };
        string before = payload.ToString();
        foreach(string endpoint in new[]{"https://audit.invalid/v1/chat/completions","https://audit.invalid/anthropic/v1/messages"})
        {
            var wire = (JObject)Invoke((Type)compat,"PrepareChatRequestPayload",endpoint,payload);
            string text = wire.ToString();
            Check(!text.Contains("内战事实") && text.Contains("[Civil war fact]"),"fixed civil-war annotation is projected on both provider protocols");
            Check(text.Contains("[AFEF PLAYER_ACTION]") && text.Contains("original 中文正文 {NAME} $1"),"wire keeps fact actor and opaque body");
            Check(payload.ToString()==before,"wire leaves player/history input object unchanged");
            Check(!text.Contains("must reply in English") && !text.Contains("AF_OUTPUT_LANGUAGE"),"no reply-language lock added");
        }
        File.WriteAllText(Path.Combine(output,"policy-fixed-draft.txt"),cleanDraft);
        File.WriteAllText(Path.Combine(output,"policy-dynamic-draft.txt"),dynamicDraft);
        File.WriteAllText(Path.Combine(output,"diplomacy-history-window.txt"),window);
        Console.WriteLine("Request prompt audit: " + checks + " actual DLL checks; failures=" + failures.Count + "; language=" + (english?"EN":"CN") + "; no model request.");
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        if (failures.Count > 0) throw new Exception("Request prompt audit failed.");
    }
}
