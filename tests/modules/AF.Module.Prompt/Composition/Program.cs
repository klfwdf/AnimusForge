using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

// Contract for the J04a Composition owners. These compile the production files directly;
// behaviors below are the legacy MyBehavior semantics that must survive the extraction.
internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception("FAIL: " + message);
    }

    private static GuardrailRuleHit Hit(string id, int priority = 0, float score = 0f)
        => new GuardrailRuleHit { RuleId = id, Priority = priority, Score = score };

    private static void Main()
    {
        RuleIdPolicy();
        StickyCarry();
        TopicRouter();
        PreprocessIdAssembler();
        ExtrasComposer();
        RuntimeTargetBinding();
        RuleBlockText();
        RoutingStage();
        Console.WriteLine("PASS prompt-composition checks=" + _checks);
    }

    private static PromptRoutingInput RoutingInput(bool allow = true, bool useAux = true, IEnumerable<string> forced = null, string input = "我要和你决斗")
        => new PromptRoutingInput
        {
            Input = input, NpcLastUtterance = "", HasAnyHero = true, AllowRulePreprocess = allow, BypassRulePreprocess = !allow && false,
            UseAuxiliaryRuleApi = useAux, AuxiliaryReturnCap = 4, RewardEnabled = true, LoanEnabled = true, SurroundingsEnabled = false,
            ExcludedRuleIds = PromptRuleIdPolicy.BuildRuleIdSet(new[] { "marriage" }),
            PreprocessExcludedRuleIds = PromptRuleIdPolicy.BuildRuleIdSet(new[] { "marriage", "kingdom_agenda" }),
            ForcedPreprocessRuleIds = forced, StickyTargetKey = "hero_a"
        };

    private static void RoutingStage()
    {
        var logs = new List<string>();
        int semanticCalls = 0;
        var carry = new BuiltInRuleStickyCarry();
        var ports = new PromptRoutingPorts
        {
            AuxiliaryHits = (text, secondary, cap, excluded, mentions) =>
            {
                mentions.Entities.Add("城堡");
                return new List<GuardrailRuleHit> { Hit("Duel", 5, 0.9f), Hit("marriage", 9), Hit("party_transfer", 1, 0.5f) };
            },
            SemanticEvaluator = (tag, text, secondary, excluded) => (string ruleTag, out string kw, out float sc) => { semanticCalls++; kw = "kw"; sc = 0.5f; return ruleTag == "loan"; },
            CanInjectGatedRule = (id, hasHero) => id == "kingdom_vassalage",
            StickyCarry = carry,
            Log = (c, m) => logs.Add(c + ":" + m)
        };

        // 1. auxiliary router authoritative
        var r = PromptTopicRoutingStage.Run(RoutingInput(), ports);
        Check(r.UseAuxiliaryRuleHitSet && r.AuxiliaryRuleHitIds.SequenceEqual(new[] { "duel", "party_transfer" }), "aux ids collected and excluded marriage dropped: " + string.Join(",", r.AuxiliaryRuleHitIds));
        Check(r.AuxiliaryMentions.Entities.SequenceEqual(new[] { "城堡" }), "aux mentions delivered");
        Check(r.Duel.Hit && r.Duel.MatchedKeyword == "auxiliary_router" && r.PartyTransfer.Hit && !r.Loan.Hit && !r.Marriage.Hit && semanticCalls == 0, "router authoritative, no semantic evaluation");
        Check(r.LiveDuelSemanticHit && !r.LiveLoanSemanticHit, "live flags mirror router");
        Check(carry.DuelRoundsLeft == 2 && carry.TargetKey == "hero_a" && logs.Any(l => l.Contains("builtin_rule_sticky_prime")), "sticky primed from live hits");

        // 2. short ack with router active → sticky suppressed, carry consumed but not applied
        logs.Clear();
        ports.AuxiliaryHits = (text, secondary, cap, excluded, mentions) => new List<GuardrailRuleHit>();
        r = PromptTopicRoutingStage.Run(RoutingInput(input: "好的"), ports);
        Check(r.StickySuppressed && r.CarryDuel && !r.Duel.Hit && carry.DuelRoundsLeft == 1, "router omission suppresses sticky resurrection");

        // 3. live semantic path (no aux) → sticky fallback applies, loan via evaluator
        r = PromptTopicRoutingStage.Run(RoutingInput(useAux: false, input: "好的"), ports);
        Check(!r.UseAuxiliaryRuleHitSet && r.Duel.Hit && r.Duel.MatchedKeyword == "sticky" && r.Loan.Hit && r.Loan.MatchedKeyword == "kw" && semanticCalls > 0, "sticky fallback + live semantic loan");
        Check(!r.Surroundings.Hit, "disabled topic not evaluated");

        // 4. forced preselection: excluded/gated filtering and merge
        carry.Clear();
        r = PromptTopicRoutingStage.Run(RoutingInput(useAux: false, forced: new[] { "Kingdom_Vassalage", "diplomacy", "marriage", "worldmap_party_command", "duel" }), ports);
        Check(r.ForcedRuleHitIds.SequenceEqual(new[] { "kingdom_vassalage", "worldmap_party_command", "duel" }), "forced: gated diplomacy and excluded marriage dropped: " + string.Join(",", r.ForcedRuleHitIds));
        Check(r.UseAuxiliaryRuleHitSet && r.WorldMapPartyCommand.Hit && r.Duel.Hit && r.Duel.MatchedKeyword == "auxiliary_router", "forced ids become authoritative router set");

        // 5. auxiliary failure falls back to live semantic; PreprocessFormatException propagates
        ports.AuxiliaryHits = (text, secondary, cap, excluded, mentions) => throw new InvalidOperationException("boom");
        r = PromptTopicRoutingStage.Run(RoutingInput(), ports);
        Check(r.AuxiliaryFailure == "boom" && !r.UseAuxiliaryRuleHitSet && r.Loan.Hit, "aux failure recorded, live routing continues");
        ports.AuxiliaryHits = (text, secondary, cap, excluded, mentions) => throw new PreprocessFormatException("bad format");
        bool propagated = false;
        try { PromptTopicRoutingStage.Run(RoutingInput(), ports); } catch (PreprocessFormatException) { propagated = true; }
        Check(propagated, "PreprocessFormatException propagates");

        // 6. preprocess not allowed → nothing routes, sticky untouched
        carry.Prime("hero_a", true, false, false, out _);
        r = PromptTopicRoutingStage.Run(RoutingInput(allow: false), ports);
        Check(!r.Duel.Hit && r.AuxiliaryRuleHitIds == null && carry.DuelRoundsLeft == 2, "suppressed preprocess routes nothing and keeps carry");
        Check(PromptTopicRoutingStage.DescribeHits(null) == "(skip)" && PromptTopicRoutingStage.DescribeHits(new List<string>(), "(none)") == "(none)" && PromptTopicRoutingStage.DescribeHits(new List<string> { "a", "b" }) == "a,b", "describe hits");
    }

    private static void RuleBlockText()
    {
        var sb = new System.Text.StringBuilder();
        PromptRuleBlockText.Append(sb, " duel ", " body ");
        PromptRuleBlockText.Append(sb, "", "x");
        PromptRuleBlockText.Append(sb, "loan", "  ");
        PromptRuleBlockText.Append(null, "loan", "y");
        string text = sb.ToString();
        Check(text.Replace("\r\n", "\n") == "【附加规则:duel】\nbody\n", "append trims id/body and skips blanks: " + text);
        Check(PromptRuleBlockText.Has(text, "DUEL") && !PromptRuleBlockText.Has(text, "due") && !PromptRuleBlockText.Has(null, "duel") && !PromptRuleBlockText.Has(text, " "), "has is case-insensitive and delimited");
        Check(PromptRuleBlockText.Count("【附加规则:a】x【附加规则:b】y") == 2 && PromptRuleBlockText.Count("") == 0 && PromptRuleBlockText.Count("no blocks") == 0, "count");

        string doc = "intro\n【附加规则:reward】\nold reward\n【附加规则:loan】\nloan body";
        string replaced = PromptRuleBlockText.ReplaceBody(doc, "REWARD", "new reward");
        Check(replaced.Replace("\r\n", "\n") == "intro\n【附加规则:REWARD】\nnew reward\n【附加规则:loan】\nloan body", "replace body keeps neighbours: " + replaced);
        Check(PromptRuleBlockText.ReplaceBody(doc, "missing", "z") == doc.Trim() && PromptRuleBlockText.ReplaceBody(doc, "reward", " ") == doc.Trim(), "replace no-ops");
        string removed = PromptRuleBlockText.Remove(doc, "reward");
        Check(removed.Replace("\r\n", "\n") == "intro\n【附加规则:loan】\nloan body", "remove middle block: " + removed);
        Check(PromptRuleBlockText.Remove("【附加规则:only】\nbody", "only") == "" && PromptRuleBlockText.Remove(doc, "loan").Replace("\r\n", "\n") == "intro\n【附加规则:reward】\nold reward", "remove sole/last block");

        Check(PromptRuleBlockText.AppendIfMissing("", "a", "body", 4).Replace("\r\n", "\n") == "【附加规则:a】\nbody", "append to empty");
        Check(PromptRuleBlockText.AppendIfMissing("x", "a", " ", 4) == "x" && PromptRuleBlockText.AppendIfMissing(doc, "reward", "again", 4) == doc, "blank body or existing block is no-op");
        Check(PromptRuleBlockText.AppendIfMissing(doc, "third", "t", 2) == doc && PromptRuleBlockText.Count(PromptRuleBlockText.AppendIfMissing(doc, "third", "t", 3)) == 3, "cap gates append");
        Check(PromptRuleBlockText.PrependDisclaimer("plain") == "plain" && PromptRuleBlockText.PrependDisclaimer("").Length == 0, "disclaimer only with blocks");
        string withDisclaimer = PromptRuleBlockText.PrependDisclaimer(doc);
        Check(withDisclaimer.StartsWith(PromptRuleBlockText.Disclaimer) && PromptRuleBlockText.PrependDisclaimer(withDisclaimer) == withDisclaimer, "disclaimer idempotent");
    }

    private static void RuntimeTargetBinding()
    {
        var hero = PromptRuntimeTargetBinding.Create("K", "hero_1", "char_1", "hero_1", true, true, 7);
        Check(hero.KingdomId == "K" && hero.HeroId == "hero_1" && hero.CharacterId == "char_1" && hero.TroopId == "char_1" && hero.UnnamedRank == "" && hero.AgentIndex == 7, "hero target: troop=character, no unnamed rank");
        var soldier = PromptRuntimeTargetBinding.Create(null, null, "troop_a", null, false, true, -1);
        Check(soldier.KingdomId == "" && soldier.HeroId == "" && soldier.UnnamedRank == "soldier" && soldier.TroopId == "troop_a", "non-hero soldier binding");
        var commoner = PromptRuntimeTargetBinding.Create("", null, "npc_b", "hero_of_b", false, false, 3);
        Check(commoner.HeroId == "hero_of_b" && commoner.UnnamedRank == "commoner", "non-hero commoner falls back to character hero id");
        var nothing = PromptRuntimeTargetBinding.Create("K", null, null, null, false, false, -1);
        Check(nothing.HeroId == "" && nothing.CharacterId == "" && nothing.UnnamedRank == "", "no character yields empty rank");
        var order = new List<string>();
        hero.Apply(v => order.Add("k=" + v), v => order.Add("h=" + v), v => order.Add("c=" + v), v => order.Add("t=" + v), v => order.Add("r=" + v), v => order.Add("a=" + v));
        Check(order.SequenceEqual(new[] { "k=K", "h=hero_1", "c=char_1", "t=char_1", "r=", "a=7" }), "apply order kingdom,hero,character,troop,rank,agent");
        Check(PromptRuntimeTargetBinding.Cleared.AgentIndex == -1 && PromptRuntimeTargetBinding.Cleared.KingdomId == "" && PromptRuntimeTargetBinding.Cleared.HeroId == "", "cleared binding is legacy reset values");
    }

    private static void ExtrasComposer()
    {
        Check(PromptExtrasComposer.Compose(null) == "" && PromptExtrasComposer.Compose(new PromptExtrasSections()) == "", "empty sections compose to empty");
        var sections = new PromptExtrasSections
        {
            LoanDueDateReference = "due",
            LoanDebtHint = "   ",           // legacy IsNullOrEmpty: whitespace-only Reward output still appended
            TrustPrompt = null,
            SettlementMerchantDebtHint = "  ", // legacy IsNullOrWhiteSpace: skipped
            DuelResultLine = "duel",
            FeastAttendanceContext = "feast",
            ClarificationHint = "clarify",
            TriggeredRuleInstructions = "rules",
            WeeklyFullReports = "full",
            LoreContext = "lore",
            EntityMainPromptBlock = "entity",
            AgendaMainPromptBlock = "agenda"
        };
        string extras = PromptExtrasComposer.Compose(sections);
        var lines = extras.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        Check(lines.SequenceEqual(new[] { "due", "   ", "duel", "feast", "clarify", "rules", "full", "lore", "entity", "agenda", "" }), "canonical order, blank policy per section, trailing newline: " + string.Join("|", lines));
        var ordered = new PromptExtrasSections { WeeklyShortReports = "short", ActivePolicyContext = "policy", TriggeredRuleInstructions = "rules", WeeklyFullReports = "full", LoreContext = "lore", ResidentRecentActions = "recent", NearbySettlementsDetail = "nearby", HeroArmyRuntimeFact = "army", PlayerArmyRuntimeFact = "parmy" };
        Check(PromptExtrasComposer.Compose(ordered).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).SequenceEqual(new[] { "army", "parmy", "recent", "nearby", "short", "policy", "rules", "full", "lore" }), "world/weekly/policy/rules/full/lore ordering");

        Check(PromptExtrasComposer.BuildDuelResultLine(true, " ").StartsWith("【战斗结果】你刚刚在一场正式的决斗中输给了玩家。"), "duel loss with default player name");
        Check(PromptExtrasComposer.BuildDuelResultLine(false, "阿尔文").Contains("打败了阿尔文。你可以据此调整对阿尔文的态度，或提醒阿尔文履行"), "duel win uses display name three times");
        Check(PromptExtrasComposer.BuildVanillaBattleDefeatLine(null).StartsWith("【原版战斗结果】你刚刚在一场战斗中被玩家击败了。"), "vanilla defeat line");
        Check(PromptExtrasComposer.BuildReleasedPrisonerLine("X").StartsWith("【释放通知】你之前被X俘虏关押"), "released prisoner line");

        Check(PromptExtrasComposer.MergePostprocessBlock("", "b") == "b" && PromptExtrasComposer.MergePostprocessBlock("a \n", "b") == "a\nb" && PromptExtrasComposer.MergePostprocessBlock("a", " ") == "a" && PromptExtrasComposer.MergePostprocessBlock(null, null) == "", "postprocess block merge");

        var ids = PromptExtrasComposer.BuildEntityRetrievalRuleIds(new[] { " marriage ", "", null }, true, false, true, false);
        Check(ids.SetEquals(new[] { "marriage", "reward", "party_transfer" }), "entity retrieval rule ids");
        Check(PromptExtrasComposer.BuildEntityRetrievalRuleIds(null, false, true, false, true).SetEquals(new[] { "loan", "worldmap_party_command" }), "entity retrieval rule ids without auxiliary");

        var markers = PromptExtrasComposer.DetectMarkers("x【附加规则:DUEL】y【NPC近期行动（近10天，常驻）】【原版任务上下文：z【附加规则:npc_major_actions】");
        Check(markers.Duel && !markers.Reward && !markers.Loan && !markers.WorldMap && markers.NpcMajor && markers.ResidentRecentActions && markers.VanillaIssueRuntimeBlock && !markers.VanillaIssue, "marker detection (rule blocks case-insensitive, resident header ordinal)");
        Check(!PromptExtrasComposer.DetectMarkers(null).Duel && !PromptExtrasComposer.DetectMarkers("").ResidentRecentActions, "markers on empty extras");
        Check(PromptExtrasComposer.HasRuleBlock("【附加规则:loan】", "LOAN") && !PromptExtrasComposer.HasRuleBlock("【附加规则:loan】", "loa"), "HasRuleBlock exact id with delimiters");
    }

    private static void PreprocessIdAssembler()
    {
        var excluded = PromptRuleIdPolicy.BuildRuleIdSet(new[] { "loan", "marriage" });
        var flags = new PromptRoutedTopicFlags { Duel = true, Reward = true, Loan = true, PersistentAdpDebt = true, Surroundings = true, KingdomService = true, Marriage = true, PartyTransfer = true, WorldMapPartyCommand = true };
        var ids = PromptPreprocessRuleIdAssembler.Assemble(new[] { " Kingdom_Vassalage ", "loan", "", null, "custom_topic" }, flags, excluded, "【附加规则:noble_gathering】...");
        Check(ids.Contains("Kingdom_Vassalage") && ids.Contains("custom_topic"), "auxiliary ids kept trimmed");
        Check(!ids.Contains("loan") && !ids.Contains("persistent_adp_debt") && !ids.Contains("marriage"), "excluded topics gate both loan and persistent debt");
        foreach (string id in new[] { "duel", "reward", "surroundings", "kingdom_service", "party_transfer", "worldmap_party_command", "noble_gathering" })
            Check(ids.Contains(id), "routed topic present: " + id);
        Check(ids.Count == 9, "no duplicates and only routed ids: " + string.Join(",", ids));
        var none = PromptPreprocessRuleIdAssembler.Assemble(null, default(PromptRoutedTopicFlags), null, "");
        Check(none.Count == 0, "nothing routed yields empty list");
        // Legacy quirk preserved: a null instruction block evaluates (null?.IndexOf).GetValueOrDefault() >= 0 as true.
        // Production always passes "" or a real block, never null.
        Check(PromptPreprocessRuleIdAssembler.Assemble(null, default(PromptRoutedTopicFlags), null, null).SequenceEqual(new[] { "noble_gathering" }), "null block keeps legacy null-coalescing behavior");
        var debt = PromptPreprocessRuleIdAssembler.Assemble(null, new PromptRoutedTopicFlags { PersistentAdpDebt = true }, new HashSet<string>(StringComparer.OrdinalIgnoreCase), "no block");
        Check(debt.SequenceEqual(new[] { "persistent_adp_debt" }), "persistent debt id emitted without loan hit");
        var dupe = PromptPreprocessRuleIdAssembler.Assemble(new[] { "DUEL" }, new PromptRoutedTopicFlags { Duel = true }, null, "");
        Check(dupe.Count == 1, "auxiliary and routed duel collapse case-insensitively");
        Check(PromptPreprocessRuleIdAssembler.PersistentAdpDebtRuleId == "persistent_adp_debt", "persistent debt id constant is the legacy ShoutBehavior value");
    }

    private static void RuleIdPolicy()
    {
        var set = PromptRuleIdPolicy.BuildRuleIdSet(new[] { " Duel ", "", null, "reward", "DUEL" });
        Check(set.Count == 2 && set.Contains("duel") && set.Contains("REWARD"), "rule-id set trims, drops blanks, case-insensitive");
        Check(PromptRuleIdPolicy.BuildRuleIdSet(null).Count == 0, "null enumerable yields empty set");
        Check(PromptRuleIdPolicy.IsExcluded(set, " duel "), "excluded lookup trims");
        Check(!PromptRuleIdPolicy.IsExcluded(set, "loan") && !PromptRuleIdPolicy.IsExcluded(null, "duel") && !PromptRuleIdPolicy.IsExcluded(set, " "), "excluded negative cases");

        var limited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        PromptRuleIdPolicy.AddPlayerPartyTradeLimitedExclusions(limited);
        Check(limited.SetEquals(new[] { "loan", "kingdom_agenda", "diplomacy", "party_transfer" }), "companion/family exclusions are exactly the legacy four");
        PromptRuleIdPolicy.AddPlayerPartyTradeLimitedExclusions(null);
        var resident = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        PromptRuleIdPolicy.AddPreprocessOnlyResidentRuleExclusions(resident);
        Check(resident.SetEquals(new[] { "noble_deference" }), "preprocess-only resident rule is noble_deference");

        Check(PromptRuleIdPolicy.IsBuiltInRuleIdForExtraInjection(" Reward ") && !PromptRuleIdPolicy.IsBuiltInRuleIdForExtraInjection("marriage"), "built-in ids for extra injection");
        var normalized = PromptRuleIdPolicy.NormalizePreselectedRuleIds(new[] { " Marriage", "duel", "MARRIAGE", null, "  " });
        Check(normalized.SequenceEqual(new[] { "marriage", "duel" }), "normalization lower-cases, dedupes, keeps first order");
        Check(PromptRuleIdPolicy.ShouldIncludeResidentKingdomEntities(true, null), "kingdom_service hit includes resident kingdoms");
        Check(PromptRuleIdPolicy.ShouldIncludeResidentKingdomEntities(false, new[] { "reward", "Kingdom_Vassalage" }), "kingdom_vassalage preselection includes resident kingdoms");
        Check(!PromptRuleIdPolicy.ShouldIncludeResidentKingdomEntities(false, new[] { "reward" }), "unrelated preselection excludes resident kingdoms");
        foreach (string gated in new[] { "kingdom_vassalage", "diplomacy", "world_diplomacy_discussion", "kingdom_agenda" })
            Check(PromptRuleIdPolicy.IsRuntimeGatedPreprocessRuleId(" " + gated.ToUpperInvariant() + " "), "runtime gated: " + gated);
        Check(!PromptRuleIdPolicy.IsRuntimeGatedPreprocessRuleId("kingdom_service"), "kingdom_service is not runtime gated");

        var ordered = PromptRuleIdPolicy.OrderPreprocessHitIds(new[] { Hit("Loan", 1, 0.9f), null, Hit(" ", 9), Hit("duel", 5, 0.1f), Hit("REWARD", 5, 0.7f), Hit("loan", 1, 0.95f) });
        Check(ordered.SequenceEqual(new[] { "reward", "duel", "loan" }), "courier preprocess ordering: priority desc, score desc, distinct lower-case");
        Check(PromptRuleIdPolicy.OrderPreprocessHitIds(null).Count == 0, "null hits order to empty");

        var excluded = PromptRuleIdPolicy.BuildRuleIdSet(new[] { "loan" });
        var aux = PromptRuleIdPolicy.CollectAuxiliaryHitIds(new[] { Hit("Marriage", 9), Hit("loan", 8), Hit("duel", 1), Hit("MARRIAGE", 0), null }, excluded);
        Check(aux.SequenceEqual(new[] { "marriage", "duel" }), "auxiliary hits keep router order, drop excluded and duplicates");
        PromptRuleIdPolicy.MergeForcedHitIds(aux, new[] { "DUEL", "reward" });
        Check(aux.SequenceEqual(new[] { "marriage", "duel", "reward" }), "forced merge appends new ids only");
        PromptRuleIdPolicy.MergeForcedHitIds(null, new[] { "x" });
        PromptRuleIdPolicy.MergeForcedHitIds(aux, null);
        Check(aux.Count == 3, "null merge arguments are no-ops");
    }

    private static void StickyCarry()
    {
        Check(BuiltInRuleStickyCarry.IsShortAck("好的") && BuiltInRuleStickyCarry.IsShortAck("我选雇佣兵") && BuiltInRuleStickyCarry.IsShortAck(" 继续 "), "short acknowledgements");
        Check(!BuiltInRuleStickyCarry.IsShortAck("") && !BuiltInRuleStickyCarry.IsShortAck("我想和你谈谈关于城堡的事情，还有很多其他细节"), "long or empty input is not a short ack");
        Check(BuiltInRuleStickyCarry.TurnLimit("Duel") == 2 && BuiltInRuleStickyCarry.TurnLimit("reward") == 2 && BuiltInRuleStickyCarry.TurnLimit("loan") == 3 && BuiltInRuleStickyCarry.TurnLimit("marriage") == 0, "turn limits");
        Check(BuiltInRuleStickyCarry.ResolveTargetKey(" Hero_A ", "char", "heroB") == "hero_a", "hero id wins");
        Check(BuiltInRuleStickyCarry.ResolveTargetKey("", "Char_1", "heroB") == "char_1", "character id second");
        Check(BuiltInRuleStickyCarry.ResolveTargetKey(null, " ", "HeroB") == "herob", "character hero id third");
        Check(BuiltInRuleStickyCarry.ResolveTargetKey(null, null, null) == "", "no ids yields empty key");

        var carry = new BuiltInRuleStickyCarry();
        Check(!carry.Prime("t1", false, false, false, out _) && carry.TargetKey == null, "no hits leave carry unset");
        Check(carry.Prime("t1", true, false, true, out string primeLog) && primeLog.Contains("duel=2") && primeLog.Contains("loan=3") && primeLog.Contains("reward=0"), "prime sets per-topic rounds");
        Check(!carry.TryConsume("t1", "我想聊点别的很长的话题啊啊啊啊啊啊啊啊啊啊啊", out _, out _, out _, out _) && carry.TargetKey == null, "non-ack input clears carry");
        carry.Prime("t1", true, false, true, out _);
        Check(!carry.TryConsume("t2", "好的", out _, out _, out _, out _) && carry.TargetKey == null, "target mismatch clears carry");
        carry.Prime("t1", true, true, true, out _);
        Check(carry.TryConsume("t1", "好的", out bool d1, out bool r1, out bool l1, out string log1) && d1 && r1 && l1 && log1.Contains("left=(1,1,2)"), "first ack consumes all three");
        Check(carry.TryConsume("t1", "嗯", out bool d2, out bool r2, out bool l2, out _) && d2 && r2 && l2 && carry.DuelRoundsLeft == 0 && carry.LoanRoundsLeft == 1, "second ack consumes remaining");
        Check(carry.TryConsume("t1", "是", out bool d3, out bool r3, out bool l3, out _) && !d3 && !r3 && l3 && carry.TargetKey == null, "third ack only loan, then auto-clear at zero");
        Check(!carry.TryConsume("t1", "是", out _, out _, out _, out _), "exhausted carry returns false");
        carry.Prime("t1", true, false, false, out _);
        Check(!carry.Prime("", true, true, true, out _) && carry.TargetKey == null, "priming with empty key clears");
        carry.Prime("t1", false, true, false, out _);
        Check(!carry.Prime("t1", false, false, false, out _) && carry.RewardRoundsLeft == 2, "prime without hits keeps existing carry");
        carry.Clear();
        Check(carry.TargetKey == null && carry.RewardRoundsLeft == 0, "clear resets");
    }

    private static void TopicRouter()
    {
        var excluded = PromptRuleIdPolicy.BuildRuleIdSet(new[] { "loan" });
        var auxSet = new HashSet<string>(new[] { "duel", "loan" }, StringComparer.OrdinalIgnoreCase);
        int evaluations = 0;
        PromptTopicSemanticEvaluator evaluator = (string tag, out string kw, out float sc) => { evaluations++; kw = "kw:" + tag; sc = 0.42f; return tag == "reward"; };

        var duel = PromptBuiltInTopicRouter.Route("duel", true, true, excluded, true, auxSet, evaluator);
        Check(duel.Hit && duel.MatchedKeyword == "auxiliary_router" && duel.Score == 1f && duel.Describe() == "auxiliary_router@1.00", "auxiliary router hit is authoritative");
        var reward = PromptBuiltInTopicRouter.Route("reward", true, true, excluded, true, auxSet, evaluator);
        Check(!reward.Hit && reward.Describe() == "" && evaluations == 0, "auxiliary miss does not consult semantic evaluator");
        var loan = PromptBuiltInTopicRouter.Route("loan", true, true, excluded, true, auxSet, evaluator);
        Check(!loan.Hit, "excluded topic never hits even when router lists it");
        var live = PromptBuiltInTopicRouter.Route("reward", true, true, excluded, false, null, evaluator);
        Check(live.Hit && live.MatchedKeyword == "kw:reward" && Math.Abs(live.Score - 0.42f) < 1e-6 && live.Describe() == "kw:reward@0.42" && evaluations == 1, "live semantic path");
        var disabled = PromptBuiltInTopicRouter.Route("reward", true, false, excluded, false, null, evaluator);
        Check(!disabled.Hit && evaluations == 1, "disabled topic skips evaluation");
        var suppressed = PromptBuiltInTopicRouter.Route("reward", false, true, excluded, false, null, evaluator);
        Check(!suppressed.Hit && evaluations == 1, "suppressed preprocess skips evaluation");
        var noEval = PromptBuiltInTopicRouter.Route("reward", true, true, excluded, false, null, null);
        Check(!noEval.Hit && noEval.MatchedKeyword == "", "missing evaluator is a miss");

        var route = PromptBuiltInTopicRouter.Route("duel", true, true, excluded, false, null, evaluator);
        Check(!route.Hit && Math.Abs(route.Score - 0.42f) < 1e-6, "duel live miss before sticky keeps its evaluated score");
        PromptBuiltInTopicRouter.ApplyStickyFallback(ref route, true, true, excluded, "duel");
        Check(route.Hit && route.MatchedKeyword == "sticky" && Math.Abs(route.Score - 0.42f) < 1e-6, "sticky fallback promotes carry and keeps max(score, 0.18)");
        var lowRoute = default(PromptTopicRoute);
        PromptBuiltInTopicRouter.ApplyStickyFallback(ref lowRoute, true, true, excluded, "duel");
        Check(lowRoute.Hit && lowRoute.Score == 0.18f && lowRoute.Describe() == "sticky@0.18", "sticky floor score is 0.18");
        var auxRoute = PromptBuiltInTopicRouter.Route("duel", true, true, excluded, false, null, evaluator);
        PromptBuiltInTopicRouter.ApplyStickyFallback(ref auxRoute, false, true, excluded, "duel");
        Check(!auxRoute.Hit, "sticky suppressed when router authoritative");
        var loanRoute = default(PromptTopicRoute);
        PromptBuiltInTopicRouter.ApplyStickyFallback(ref loanRoute, true, true, excluded, "loan");
        Check(!loanRoute.Hit, "sticky never resurrects an excluded topic");
        var already = new PromptTopicRoute { Hit = true, MatchedKeyword = "kw", Score = 0.9f };
        PromptBuiltInTopicRouter.ApplyStickyFallback(ref already, true, true, excluded, "duel");
        Check(already.MatchedKeyword == "kw" && already.Score == 0.9f, "existing hit keeps its own evidence");
    }
}
