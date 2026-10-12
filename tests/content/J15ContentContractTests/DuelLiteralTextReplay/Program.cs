using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using AnimusForge;
using Newtonsoft.Json;
using TaleWorlds.Localization;

internal static class Program
{
    internal static string Package;
    internal static int PathReads;
    private static int checks;
    private static int negatives;
    private static readonly List<object> Evidence = new List<object>();
    private static readonly Regex Slots = new Regex(@"\{([A-Za-z_]+)\}");
    private const string Literal = "名{SPEAKER}/{TARGET_ID}/{player_gold}/{npcName}/{NPC}/{NOTE}/{UNKNOWN}/{}/$1/AFDUELLITERAL0END";
    private static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new Exception(label);
    }
    private static string Expand(string template, (string Name, object Value)[] values)
        => Slots.Replace(template, match =>
        {
            foreach (var value in values)
                if (value.Name == match.Groups[1].Value)
                    return Convert.ToString(value.Value, CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });
    private static string BeforeRaw(string key, (string Name, object Value)[] values)
    {
        var text = BeforeDuelFactTexts.Object(key);
        foreach (var value in values)
            if (value.Value is int number) text.SetTextVariable(value.Name, number);
            else text.SetTextVariable(value.Name, value.Value?.ToString() ?? "");
        return text.ToString();
    }
    private static void Register(string root, string language, bool chinese)
    {
        string[] files = chinese ? new[] {
            "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml",
            "content/modules/AF.Module.Duel/ModuleData/Languages/CNs/afduel_strings-zh-CN.xml"
        } : new[] {
            "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml",
            "content/modules/AF.Module.Duel/ModuleData/Languages/afduel_strings.xml"
        };
        var document = new XmlDocument();
        var element = document.CreateElement("LanguageData");
        document.AppendChild(element);
        element.SetAttribute("id", language); element.SetAttribute("name", language);
        element.SetAttribute("supported_iso", chinese ? "zh-CN" : "en");
        element.SetAttribute("text_processor", "TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor");
        foreach (string file in files)
        {
            var node = document.CreateElement("LanguageFile");
            node.SetAttribute("xml_path", file);
            element.AppendChild(node);
        }
        var type = typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData", true);
        type.GetMethod("LoadFromXml", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { document, root });
    }
    private static Dictionary<string, string> UiTemplates(string root, bool chinese)
    {
        var xml = new XmlDocument();
        xml.Load(Path.Combine(root, chinese ? "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml" : "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml"));
        var result = new Dictionary<string, string>();
        foreach (XmlElement node in xml.SelectNodes("//string")) result[node.GetAttribute("id")] = node.GetAttribute("text");
        return result;
    }
    private static void RawRows(Dictionary<string, string> raw, string language)
    {
        foreach (var row in raw.Where(pair => Slots.IsMatch(pair.Value)))
        {
            var names = Slots.Matches(row.Value).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToArray();
            foreach (string culture in new[] { "en-US", "zh-CN", "de-DE", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var ordinary = names.Select(name => (Name: name, Value: name == "AMOUNT" || name == "COUNT" ? (object)12345 : "ordinary-" + name)).ToArray();
                string previous = BeforeRaw(row.Key, ordinary);
                Check(DuelFactTexts.Format(row.Key, ordinary) == previous, "raw ordinary/native parity " + language + culture + row.Key + " expected=[" + previous + "] actual=[" + DuelFactTexts.Format(row.Key, ordinary) + "]");
                Check(DuelFactTexts.Format(row.Key, ordinary) == Expand(row.Value, ordinary), "raw reference values " + row.Key);
                foreach (string dynamicValue in new[] { Literal, "{}", "$1", "{NOTE}", "", null })
                {
                    var values = names.Select(name => (Name: name, Value: (object)dynamicValue)).ToArray();
                    Check(DuelFactTexts.Format(row.Key, values) == Expand(row.Value, values), "raw literal one pass " + row.Key);
                }
                Check(DuelFactTexts.Format(row.Key) == row.Value, "unknown raw slots retained " + row.Key);
                Check(DuelFactTexts.Format(row.Key, (names[0].ToLowerInvariant(), "unused")) == row.Value, "raw variable case retained " + row.Key);
            }
        }
        foreach (var row in raw.Where(pair => !Slots.IsMatch(pair.Value)))
            Check(DuelFactTexts.Get(row.Key) == row.Value, "raw identity " + row.Key);
        bool threw = false;
        try { DuelFactTexts.Format("missing-key"); } catch (KeyNotFoundException) { threw = true; }
        Check(threw, "unknown resource key keeps missing-key failure");
        foreach (string key in new[] { "", "af_duel_StakeDenars", null })
        {
            Type oldError = null, currentError = null;
            try { BeforeDuelFactTexts.Get(key); } catch (Exception e) { oldError = e.GetType(); }
            try { DuelFactTexts.Format(key); } catch (Exception e) { currentError = e.GetType(); }
            Check(oldError != null && oldError == currentError, "empty/null/case resource keys preserve existing failure");
        }
    }
    private static void UiRows(Dictionary<string, string> ui, string language)
    {
        string[] ids = { "AF_UI_Duel_StakeGold", "AF_UI_Duel_StakeItem", "AF_UI_Duel_StakeDebtIdSuffix", "AF_UI_Duel_StakeDueSuffix", "AF_UI_Duel_StakeNoteSuffix", "AF_UI_Duel_StakeDebtResult", "AF_UI_Duel_RenownSettlement", "AF_UI_Duel_StakeNotSettled", "AF_UI_Duel_StakePaidPartially", "AF_UI_Duel_StakePaid", "AF_UI_Duel_StakeUnpaid" };
        foreach (string id in ids)
        {
            string template = ui[id];
            var names = Slots.Matches(template).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToArray();
            foreach (string culture in new[] { "en-US", "zh-CN", "de-DE", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var values = names.Select(name => (Name: name, Value: name == "AMOUNT" ? (object)12345 : "ordinary-" + name)).ToArray();
                Check(CurrentDuel.Ui(id, template, values) == BeforeDuel.Ui(id, template, values), "UI ordinary/native parity " + language + id + culture);
                if (language == "简体中文")
                    Check(CurrentDuel.Ui(id, "incorrect fallback", values) == Expand(template, values), "real registered CN UI XML lookup " + language + id);
                else
                    Check(CurrentDuel.Ui(id, "English fallback", values) == "English fallback", "English keeps native fallback behavior " + id);
                foreach (string literal in new[] { Literal, "{}", "$1", "{NOTE}", "", null })
                {
                    var literals = names.Select(name => (Name: name, Value: (object)literal)).ToArray();
                    Check(CurrentDuel.Ui(id, template, literals) == Expand(template, literals), "UI inserted literal values " + id);
                }
            }
        }
        Check(CurrentDuel.Ui("unknown-duel-id", "Fallback {VALUE}", ("VALUE", Literal)) == "Fallback " + Literal, "unknown UI id uses native fallback and literal data");
        Check(CurrentDuel.Ui("unknown-duel-id", "Fallback {MISSING}") == BeforeDuel.Ui("unknown-duel-id", "Fallback {MISSING}"), "unbound native template slot keeps previous engine behavior");
        Check(CurrentDuel.Ui("unknown-duel-id", "{SLOT}", ("slot", "x")) == BeforeDuel.Ui("unknown-duel-id", "{SLOT}", ("slot", "x")), "UI variable case retains native behavior");
        foreach (string fallback in new[] { "", null, "plain literal" })
            Check(CurrentDuel.Ui("unknown-duel-id", fallback, ("VALUE", null)) == BeforeDuel.Ui("unknown-duel-id", fallback, ("VALUE", null)), "empty/null/plain UI fallback keeps old native behavior");
    }
    private static Hero Heroes(string dynamicValue)
    {
        Hero.MainHero = new Hero { StringId = "player", Name = new LiteralName(dynamicValue), Clan = new Clan { StringId = "player-clan", Name = new LiteralName(dynamicValue + "-player-clan"), Renown = 12.5f } };
        return new Hero { StringId = "npc", Name = new LiteralName(dynamicValue), Clan = new Clan { StringId = "npc-clan", Name = new LiteralName(dynamicValue + "-npc-clan"), Renown = 24.25f } };
    }
    private static string StateSnapshot(string display, DuelOutcomeEffectState state)
        => JsonConvert.SerializeObject(new { display, state, history = MyBehavior.History.Select(h => new { hero = h.Hero.StringId, h.Fact }).ToArray(), RewardSystemBehavior.LastNote, RewardSystemBehavior.LastAmount, RewardSystemBehavior.LastDueDays, playerRenown = Hero.MainHero.Clan.Renown });
    private static string SettlementCase(bool before, bool won, bool stake, bool debt, string value, string mode, out DuelOutcomeEffectState state)
    {
        var hero = Heroes(value); MyBehavior.History.Clear();
        RewardSystemBehavior.Reset(value, mode);
        if (before) { BeforeDuel.Seed(hero, stake, debt, value, value); return BeforeDuel.Settle(hero, won, out state); }
        CurrentDuel.Seed(hero, stake, debt, value, value); return CurrentDuel.Settle(hero, won, out state);
    }
    private static void SettlementRows(string language)
    {
        foreach (string culture in new[] { "en-US", "de-DE", "fr-FR" })
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            foreach (bool won in new[] { false, true }) foreach (bool stake in new[] { false, true }) foreach (bool debt in new[] { false, true })
            foreach (string mode in new[] { "complete", "partial", "unpaid", "record-failed", "missing", "gold-throws", "item-throws" })
            {
                string oldDisplay = SettlementCase(true, won, stake, debt, "ordinary", mode, out var oldState);
                string old = StateSnapshot(oldDisplay, oldState);
                string newDisplay = SettlementCase(false, won, stake, debt, "ordinary", mode, out var newState);
                Check(StateSnapshot(newDisplay, newState) == old, "exact settlement ordinary output/side-effects/classification parity " + culture + won + stake + debt + mode);
                string display = SettlementCase(false, won, stake, debt, Literal, mode, out newState);
                Check(newState == oldState, "dynamic input does not alter settlement state " + mode);
                if (display.Length > 0) Check(display.Contains(Literal), "whole settlement literal name/item/note " + mode);
                foreach (var entry in MyBehavior.History) Check(entry.Fact.Contains(Literal), "actual history-entry outgoing extraFact preserves data");
                if (debt && !won && RewardSystemBehavior.Instance != null)
                    Check(RewardSystemBehavior.LastNote == Literal && RewardSystemBehavior.LastAmount == 100 && RewardSystemBehavior.LastDueDays == 3, "actual debt writer leaf receives unmodified raw amount/due/note");
            }
            foreach (bool won in new[] { false, true })
            {
                var oldHero = Heroes("ordinary"); MyBehavior.History.Clear();
                string old = BeforeDuel.Renown(oldHero, won, out var oldState); var oldFacts = MyBehavior.History.Select(h => h.Fact).ToArray();
                float oldPlayer = Hero.MainHero.Clan.Renown, oldNpc = oldHero.Clan.Renown;
                var hero = Heroes("ordinary"); MyBehavior.History.Clear();
                string current = CurrentDuel.Renown(hero, won, out var state);
                Check(old == current && oldState == state && oldFacts.SequenceEqual(MyBehavior.History.Select(h => h.Fact)) && oldPlayer == Hero.MainHero.Clan.Renown && oldNpc == hero.Clan.Renown, "exact renown ordinary/culture parity " + culture + won);
                hero = Heroes(Literal); MyBehavior.History.Clear();
                current = CurrentDuel.Renown(hero, won, out state);
                Check(current.Contains(Literal), "renown UI literal clan name");
                Check(MyBehavior.History.Count == 2 && MyBehavior.History.All(h => h.Fact.Contains(Literal)), "renown AFEF fact/history both actors preserve literal data");
                Check(MyBehavior.History.All(h => h.Fact.StartsWith(DuelFactTexts.Get(won ? "AF_DUEL_FactNpcAction" : "AF_DUEL_FactPlayerAction"), StringComparison.Ordinal)), "actual AFEF actor prefix unchanged");
            }
        }
        var items = new Dictionary<string, int> { { " z " + Literal, 2 }, { "A", 1 }, { "skip", 0 }, { "", 3 } };
        Check(CurrentDuel.RawSummary(100, items).Contains(Literal), "raw summary literal item and existing positive filtering");
        Check(CurrentDuel.UiSummary(100, items).Contains(Literal), "UI summary literal item and existing ordering");
        Check(CurrentDuel.Debt(null, null, null, null, null) == BeforeDuel.Debt(null, null, null, null, null), "debt empty/default slots preserve prior behavior");
        Check(CurrentDuel.Debt("lord", "stake", " ", "\t", "\n") == BeforeDuel.Debt("lord", "stake", " ", "\t", "\n"), "whitespace suffix omission preserved");
    }
    private static void AdAndNegativeRows(string language)
    {
        foreach (string note in new[] { "ordinary", "备注{npcName} $1", Literal })
        {
            string tag = "[ACTION:DUEL][AD:100:3:P:" + note + "]";
            var match = CaptureLeaf.Parse(tag);
            Check(match.Success, "exact production AD capture accepts literal note");
            var hero = Heroes("lord");
            CurrentDuel.CachePendingDuelDebtTag(hero, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), (match.Groups[3].Value ?? "").Trim());
            Check(CurrentDuel.TryConsumePendingDuelDebtTag(hero, out var amount, out var due, out var consumed) && amount == 100 && due == 3 && consumed == note, "exact cache/consume data conservation");
            Check(!CurrentDuel.TryConsumePendingDuelDebtTag(hero, out amount, out due, out consumed), "consume remains once-only");
            string label = DuelFactTexts.Format("AF_DUEL_StakeNoteLabel", ("NOTE", note));
            string fact = DuelFactTexts.Format("AF_DUEL_StakeDebtNpc", ("PLAYER", "player"), ("AMOUNT", 100), ("DEBT_ID", ""), ("TERM", ""), ("NOTE", label));
            string display = CurrentDuel.Debt("lord", CurrentDuel.Gold(100), "id", "3 days", note);
            if (note != "ordinary")
            {
                string oldLabel = BeforeRaw("AF_DUEL_StakeNoteLabel", new[] { ("NOTE", (object)note) });
                string oldFact = BeforeRaw("AF_DUEL_StakeDebtNpc", new[] { ("PLAYER", (object)"player"), ("AMOUNT", (object)100), ("DEBT_ID", (object)""), ("TERM", (object)""), ("NOTE", (object)oldLabel) });
                string oldDisplay = BeforeDuel.Debt("lord", BeforeDuel.Gold(100), "id", "3 days", note);
                Check(oldLabel != label && !oldFact.Contains(note) && !oldDisplay.Contains(note), "unaltered before native label+outer debt/UI negative control"); negatives++;
                Check(fact.Contains(note) && display.Contains(note), "after label+outer debt/UI retains exact original note");
                Evidence.Add(new { language, tag, cachedNote = note, oldLabel, label, oldFact, fact, oldDisplay, display });
                // Isolate the second native parse: even an already-safe raw suffix is
                // recursively parsed and damaged by the old outer TextObject.
                string rawSuffix = ", Note: " + note;
                const string debtTemplate = "You lost the duel to {NPC} and now owe {NPC} {STAKE}{DEBT_ID} (duel wager){DUE}{NOTE}.";
                string oldOuter = BeforeDuel.Ui("AF_UI_Duel_StakeDebtResult", debtTemplate, ("NPC", "lord"), ("STAKE", "stake"), ("DEBT_ID", ""), ("DUE", ""), ("NOTE", rawSuffix));
                Check(!oldOuter.Contains(note), "old outer layer alone reparses literal suffix"); negatives++;
                Check(CurrentDuel.Ui("AF_UI_Duel_StakeDebtResult", debtTemplate, ("NPC", "lord"), ("STAKE", "stake"), ("DEBT_ID", ""), ("DUE", ""), ("NOTE", rawSuffix)).Contains(note), "new outer layer does not reparse suffix");
            }
            else Check(fact.Contains(note) && display == BeforeDuel.Debt("lord", BeforeDuel.Gold(100), "id", "3 days", note), "ordinary debt negative-control baseline unchanged");
        }
    }
    private static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });
            Package = args[0]; string root = args[1];
            Register(root, "English", false); Register(root, "简体中文", true);
            var raw = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Package, "DuelFactTexts.json")));
            foreach (string language in new[] { "English", "简体中文", "English" })
            {
                Check(MBTextManager.ChangeLanguage(language), "native language switches successfully");
                var ui = UiTemplates(root, language == "简体中文");
                Check(CurrentDuel.Gold(1) == Expand(ui["AF_UI_Duel_StakeGold"], new[] { ("AMOUNT", (object)1) }), "UI locale uses actual resource, not fallback " + language + " expected=[" + Expand(ui["AF_UI_Duel_StakeGold"], new[] { ("AMOUNT", (object)1) }) + "] actual=[" + CurrentDuel.Gold(1) + "]");
                RawRows(raw, language); UiRows(ui, language); SettlementRows(language); AdAndNegativeRows(language);
            }
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            int readsBeforeWarm = PathReads;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
            {
                CurrentDuel.Debt(Literal, "100 denars", "id", "3 days", Literal);
                DuelFactTexts.Format("AF_DUEL_StakeNoteLabel", ("NOTE", Literal));
            }
            watch.Stop();
            Check(readsBeforeWarm == 2 && PathReads == readsBeforeWarm, "before/current raw resources read once, no warm reader calls");
            File.WriteAllText("result.json", JsonConvert.SerializeObject(new { api = args[2], assertions = checks, negativeControls = negatives, warmDebtCalls = 2000, warmRawCalls = 2000, warmCombinedElapsedMs = watch.Elapsed.TotalMilliseconds, readsBeforeWarm, readsAfterWarm = PathReads, evidence = Evidence }, Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            Console.WriteLine("PASS DuelLiteralTextReplay " + args[2] + ": " + checks + " assertions, " + negatives + " before negative controls; English/CN/English native locales");
        }
        catch (Exception error) { Console.Error.WriteLine(error); Environment.Exit(1); }
    }
}

namespace AnimusForge
{
    // Controlled leaves: names are already captured strings; transfers/ledger writes
    // return explicit outcomes, and history records the exact outgoing extraFact.
    internal sealed class LiteralName { private readonly string value; internal LiteralName(string value) { this.value = value; } public override string ToString() => value; }
    internal sealed class Hero { internal static Hero MainHero; internal string StringId; internal LiteralName Name; internal Clan Clan; }
    internal sealed class Clan { internal string StringId; internal LiteralName Name; internal float Renown; internal int Tier = 2; }
    internal static class GainRenownAction { internal static void Apply(Hero hero, float amount, bool doNotNotify) { hero.Clan.Renown += amount; } }
    internal static class DuelSettings { internal static bool TryGetDuelRenownPenaltySettings(out int minimum, out int percent, out int maximum) { minimum = 2; percent = 10; maximum = 0; return true; } }
    internal static class Logger { internal static void Log(string category, string message) { } }
    internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name) { Program.PathReads++; return Path.Combine(Program.Package, name); } }
    internal sealed class HistoryEntry { internal Hero Hero; internal string Fact; }
    internal static class MyBehavior { internal static readonly List<HistoryEntry> History = new List<HistoryEntry>(); internal static void AppendExternalDialogueHistory(Hero hero, string playerText, string npcText, string extraFact) { History.Add(new HistoryEntry { Hero = hero, Fact = extraFact }); } }
    internal sealed class RewardSystemBehavior
    {
        internal static RewardSystemBehavior Instance;
        internal static string LastNote; internal static int LastAmount; internal static int LastDueDays;
        private string value; private string mode;
        internal static void Reset(string value, string mode) { LastNote = null; LastAmount = 0; LastDueDays = 0; Instance = mode == "missing" ? null : new RewardSystemBehavior { value = value, mode = mode }; }
        internal int TransferGold(Hero source, Hero target, int amount, bool forceComplete) { if (mode == "gold-throws") throw new Exception("controlled transfer failure"); return mode == "unpaid" ? 0 : mode == "partial" ? Math.Min(25, amount) : amount; }
        internal int TransferItemById(Hero source, Hero target, string id, int amount, out string itemName, bool forceComplete) { itemName = value; if (mode == "item-throws") throw new Exception("controlled item failure"); return mode == "unpaid" ? 0 : mode == "partial" ? Math.Min(1, amount) : amount; }
        internal bool RecordDeferredDuelDebtForNpc(Hero hero, int amount, int dueDays, string note, out string debtId, out string rawStatus, out string displayStatus) { LastNote = note; LastAmount = amount; LastDueDays = dueDays; debtId = "id-" + value; rawStatus = "3天期限-" + value; displayStatus = "3 days-" + value; return mode != "record-failed"; }
    }
}
