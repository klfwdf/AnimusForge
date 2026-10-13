using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        checks++;
        if (!condition) throw new Exception(name);
    }

    private static WorldBulletinHistoryCandidate Candidate(string publicity = "public", string material = "The player offered terms to Aldric.", string reason = "Announced in an open assembly.")
        => WorldBulletinContextPolicy.CreateHistoryCandidate("memory:1", 12, "Day 12", "Aldric", publicity, material, reason, 15);

    public static async Task<int> Main()
    {
        foreach (string publicity in new[] { "private", "unclear", "leaked_public", "", null, "unknown" })
            Check(Candidate(publicity) == null, "exclude " + publicity);
        Check(Candidate() != null, "public candidate accepted for review, not publication");
        Check(Candidate(reason: "") == null, "missing reason fails closed");
        Check(Candidate(material: "") == null, "missing public excerpt fails closed");
        Check(Candidate(material: new string('x', 2001)) == null, "do not truncate a privacy qualifier");
        Check(WorldBulletinContextPolicy.CreateHistoryCandidate("future", 16, "tomorrow", "A", "public", "Future", "Public", 15) == null, "future memory excluded");
        var candidates = new List<WorldBulletinHistoryCandidate> { Candidate() };
        var id = candidates[0].Id;
        string approved = "{\"public_ids\":[\"" + id + "\"]}";
        Check(WorldBulletinContextPolicy.BuildApprovedHistory(approved, candidates).Contains(candidates[0].Material), "approved source is copied verbatim");
        foreach (string response in new[] { "", "not json", "[]", "{}", "{\"public_ids\":[\"unknown\"]}", "{\"public_ids\":[1]}", "{\"public_ids\":[]}", "{\"public_ids\":[],\"public_ids\":[\"memory:1\"]}", "{\"public_ids\":[\"memory:1\",\"memory:1\"]}", "{\"public_ids\":[\"memory:1\"],\"extra\":\"invented\"}" })
            Check(WorldBulletinContextPolicy.BuildApprovedHistory(response, candidates) == "", "invalid or denied review fails closed: " + response);
        Check(!WorldBulletinContextPolicy.BuildReviewUserPrompt("event", candidates).Contains("Summary"), "only public excerpt and reason, no private summary");
        Check(WorldBulletinContextPolicy.BuildReviewSystemPrompt().Length > 100, "privacy instructions loaded");
        Check(WorldBulletinContextPolicy.BuildWriterContext("lore", "approved").Contains("lore"), "lore reaches writer block");
        Check(WorldBulletinContextPolicy.BuildWriterContext("lore", "").Length > 4, "missing public history has explicit boundary");
        string writerSystem = WorldBulletinPolicy.BuildSystemPrompt(1, 0, "CUSTOM_STYLE");
        Check(writerSystem.Contains("可以虚构私下谈话") || writerSystem.Contains("may invent private conversations"), "fictional private scenes allowed in actual writer system");
        Check(writerSystem.Contains("不得复述、影射、换名改写") || writerSystem.Contains("must not repeat, allude to, rename or paraphrase"), "real private history cannot be disguised as fiction");
        string noHistory = WorldBulletinContextPolicy.BuildWriterContext("lore", "");
        Check(noHistory.Contains("不等于禁止虚构") || noHistory.Contains("does not prohibit fictional embellishment"), "missing public history does not ban independent fiction");
        Check(!writerSystem.Contains("不得补写未提供的私密经历") && !writerSystem.Contains("Do not invent private experiences")
            && !noHistory.Contains("捏造对话、匿名消息源或泄露情节") && !noHistory.Contains("invent conversations, anonymous sources or leaks"), "remove blanket ban without loosening history admission");

        var general = new KnowledgeLibraryBehavior.LoreVariant { Content = "Shared history." };
        var local = new KnowledgeLibraryBehavior.LoreVariant { Content = "Local opinion.", When = new() { Cultures = new() { "nord" }, Roles = new() { "lord" } } };
        var secret = new KnowledgeLibraryBehavior.LoreVariant { Content = "Private knowledge.", When = new() { HeroIds = new() { "secret_owner" } } };
        var rules = new List<KnowledgeLibraryBehavior.LoreRule>
        {
            new() { Id = "unrelated", Keywords = new() { "Far Away" }, Variants = new() { general } },
            new() { Id = "nord", Keywords = new() { "Nord" }, Variants = new() { general, local, secret } },
            new() { Id = "short", Keywords = new() { "A" }, Variants = new() { general } },
            new() { Id = "substring", Keywords = new() { "lord" }, Variants = new() { general } }
        };
        var chosen = BulletinLoreSelectionPolicy.Select(rules, new[] { "Nord" }, "Nord defeated a warlord.", 4);
        Check(chosen.Count == 2 && chosen.All(x => x.Rule.Id == "nord"), "relevant rule only; bounded public viewpoints");
        Check(chosen.All(x => x.Variant != secret), "hero-specific knowledge excluded");
        Check(chosen.Any(x => x.Audience.Contains("nord")), "viewpoint provenance retained");
        Check(!BulletinLoreSelectionPolicy.IsPublicVariant(new() { When = new() { IdentityIds = new() { "hero:1" } } }), "identity-private variant excluded");
        Check(!BulletinLoreSelectionPolicy.IsPublicVariant(new() { When = new() { SkillMin = new() { ["roguery"] = 50 } } }), "restricted knowledge excluded");
        Check(BulletinLoreSelectionPolicy.Select(rules, new[] { "Nord" }, "", 1).Count == 1, "hard excerpt budget");
        Check(BulletinLoreSelectionPolicy.Select(rules, Array.Empty<string>(), "unrelated topic", 4).Count == 0, "no invented background");
        Check(WorldBulletinPolicy.BuildSystemPrompt(2, 1, "CUSTOM_STYLE").Contains(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_ContextSafety")), "binding privacy rules reach actual system prompt with custom style");
        await TestCapture(rules);
        await TestRequests();
        Console.WriteLine("PASS " + checks + " bulletin context checks");
        return 0;
    }

    private static async Task TestCapture(List<KnowledgeLibraryBehavior.LoreRule> rules)
    {
        var library = new KnowledgeLibraryBehavior();
        KnowledgeLibraryBehavior.Instance = library;
        KnowledgeLibraryBehavior._publishedRules = rules;
        var selection = new WorldBulletinSelection();
        var fact = new WorldBulletinEvent { Sentence = "Nord held an assembly.", KingdomIds = new() { "nord" } };
        selection.MajorFacts.Add(fact);
        var host = new MyBehavior();
        for (int i = 0; i < 8; i++)
        {
            string id = "npc" + i;
            fact.Participants.Add(new() { HeroId = id, Name = "Aldric" });
            host._memoryBusinessState.Values[id] = new()
            {
                new() { PlayerPublicity = "public", PlayerHistoryMaterial = "PUBLIC_A", PlayerPublicityReason = "Open assembly." },
                new() { PlayerPublicity = "public", PlayerHistoryMaterial = "PUBLIC_B", PlayerPublicityReason = "Open assembly." },
                new() { PlayerPublicity = "leaked_public", PlayerHistoryMaterial = "DO_NOT_SEND_LEAK", PlayerPublicityReason = "Unfriendly NPC might leak." },
                new() { PlayerPublicity = "private", PlayerHistoryMaterial = "DO_NOT_SEND_PRIVATE", PlayerPublicityReason = "Personal letter." },
                new() { PlayerPublicity = "unclear", PlayerHistoryMaterial = "DO_NOT_SEND_UNKNOWN" }
            };
        }
        var disabled = host.Capture(selection);
        Check(disabled.PlayerHistory.Count == 0 && host._memoryBusinessState.Reads == 0, "disabled history never reads the memory cache");
        Check(disabled.Lore.Contains("Shared history.") && disabled.EventFacts.Contains(fact.Sentence), "disabled history preserves worldbook and event facts");
        var noHistoryOwner = new WorldBulletinStateOwner();
        int requestCount = 0;
        string writerInput = "";
        noHistoryOwner._port.CallApi = (_, user) => { requestCount++; writerInput = user; return Task.FromResult(new ApiCallResult { Success = true, Content = "WRITTEN" }); };
        await noHistoryOwner.RunWorldBulletinRequestAsync(1, 1, selection, new(), "SYSTEM", "FACTS", null, disabled);
        Check(requestCount == 1 && !writerInput.Contains("PUBLIC_A") && !writerInput.Contains("PUBLIC_B"), "disabled capture skips privacy API and keeps player history out of writer");
        Check(writerInput.Contains("Shared history."), "disabled capture still sends worldbook to writer");
        DuelSettings.Current = null;
        Check(host.Capture(selection).PlayerHistory.Count == 0 && host._memoryBusinessState.Reads == 0, "missing settings fails closed");
        DuelSettings.Current = new() { WorldBulletinReadPlayerHistory = true };
        var captured = host.Capture(selection);
        Check(captured.HistoryHeroIds.Count == 4, "capture bounds NPC IDs");
        Check(captured.PlayerHistory.Count == 6 && host._memoryBusinessState.Reads == 3, "six candidates at most; two per NPC; stop early");
        Check(captured.PlayerHistory.Select(c => c.Id).Distinct().Count() == 6, "review IDs unique");
        Check(!WorldBulletinContextPolicy.BuildReviewUserPrompt(captured.EventFacts, captured.PlayerHistory).Contains("DO_NOT_SEND"), "actual capture excludes private/leaked/unclear and full summaries");
        Check(captured.Lore.Contains("Shared history.") && captured.Lore.Contains("Local opinion.") && !captured.Lore.Contains("Private knowledge."), "actual capture connects lore preserving knowledge boundary");
        Check(captured.EventFacts.Contains(fact.Sentence), "review gets selected facts for relevance");
        DuelSettings.Current.WorldBulletinReadPlayerHistory = false;
        Check(host.Capture(selection).PlayerHistory.Count == 0 && host._memoryBusinessState.Reads == 3, "turning off stops history reads on the next capture");
        Check(host._memoryBusinessState.Values["npc0"].Count == 5, "toggle never deletes stored dialogue memory");
        DuelSettings.Current.WorldBulletinReadPlayerHistory = true;
        KnowledgeLibraryBehavior.Enabled = false;
        Check(library.BuildBulletinLoreSource(new[] { "Nord" }, "Nord") == "", "retrieval disabled respected");
        KnowledgeLibraryBehavior.Enabled = true;
        Check(new KnowledgeLibraryBehavior().BuildBulletinLoreSource(new[] { "Nord" }, "Nord") == "", "stale lore owner rejected");
        var mapped = new KnowledgeLibraryBehavior.LoreRule { Id = "mapped", Keywords = new() { "Nord" }, Variants = new() { new() { Content = "TOKEN" } }, TextMappings = new() { new() { SourceText = "TOKEN", TargetId = "missing" } } };
        KnowledgeLibraryBehavior._publishedRules = new[] { mapped };
        Check(library.BuildBulletinLoreSource(new[] { "Nord" }, "Nord") == "", "unresolved mapping not published");
        mapped.TextMappings[0].TargetId = "known";
        Check(library.BuildBulletinLoreSource(new[] { "Nord" }, "Nord").Contains("resolved"), "mapping rendered before background work");
        mapped.TextMappings[0].Kind = "current_npc_name";
        Check(library.BuildBulletinLoreSource(new[] { "Nord" }, "Nord") == "", "newspaper cannot impersonate NPC mappings");
        mapped.TextMappings.Clear();
        mapped.Variants[0].Content = new string('x', 4001);
        Check(library.BuildBulletinLoreSource(new[] { "Nord" }, "Nord") == "", "oversized lore omitted whole rather than cutting qualifiers");
        fact.Participants = new() { new() { HeroId = "npc0" } };
        host._memoryBusinessState.Values["npc0"] = new() { new() { PlayerPublicity = "public", PlayerHistoryMaterial = "TOO_FAR", PlayerPublicityReason = "Open assembly." } };
        host._memoryBusinessState.Values["npc0"].AddRange(Enumerable.Range(0, 64).Select(_ => new CompressedMemoryBlock()));
        Check(host.Capture(selection).PlayerHistory.Count == 0, "memory inspection bounded to 64 blocks per NPC");
    }

    private static void Drain(WorldBulletinStateOwner owner)
    {
        while (owner.MainThreadActions.TryDequeue(out var action)) action();
    }

    private static async Task TestRequests()
    {
        var selection = new WorldBulletinSelection();
        foreach (string mode in new[] { "approved", "denied", "malformed", "exception", "failure", "none" })
        {
            var context = new WorldBulletinPromptFacts { EventFacts = "EVENT", Lore = "LORE_SOURCE" };
            if (mode != "none") context.PlayerHistory.Add(Candidate(material: "PUBLIC_CANDIDATE"));
            var owner = new WorldBulletinStateOwner();
            var prompts = new List<string>();
            var logs = new List<string>();
            owner._port.Log = (_, text) => logs.Add(text);
            owner._port.CallApi = async (system, user) =>
            {
                prompts.Add(user);
                await Task.Yield(); // Exercise the actual post-await continuation, not only sync leaves.
                if (mode != "none" && prompts.Count == 1)
                {
                    Check(system == WorldBulletinContextPolicy.BuildReviewSystemPrompt(), "first request is privacy review");
                    if (mode == "exception") throw new Exception("DO_NOT_LOG_CONTENT");
                    return new() { Success = mode != "failure", Content = mode == "approved" ? "{\"public_ids\":[\"memory:1\"]}" : mode == "denied" ? "{\"public_ids\":[]}" : "invalid" };
                }
                return new() { Success = true, Content = "WRITTEN" };
            };
            await owner.RunWorldBulletinRequestAsync(1, 1, selection, new(), "SYSTEM", "FACTS", null, context);
            Check(prompts.Count == (mode == "none" ? 1 : 2), "bounded requests: " + mode);
            Check(prompts.Last().Contains("LORE_SOURCE"), "lore reaches actual writer request: " + mode);
            Check(prompts.Last().Contains("PUBLIC_CANDIDATE") == (mode == "approved"), "fail closed writer input: " + mode);
            Check(mode == "approved" || prompts.Last().Contains(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_NoPublicHistory")), "explicit no-history rule: " + mode);
            Check(!string.Join("\n", logs).Contains("DO_NOT_LOG_CONTENT") && !string.Join("\n", logs).Contains("PUBLIC_CANDIDATE"), "privacy diagnostic excludes source and exception content");
            Drain(owner);
            Check(owner.Completed == 1, "review failures still complete factual issue: " + mode);
        }
        foreach (bool changeEpoch in new[] { true, false })
        {
            var owner = new WorldBulletinStateOwner();
            int calls = 0, cancelled = 0;
            var gate = new TaskCompletionSource<ApiCallResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            owner._port.CallApi = (_, _) => { calls++; return gate.Task; };
            owner._port.CancelIllustration = _ => cancelled++;
            var task = owner.RunWorldBulletinRequestAsync(1, 1, selection, new(), "SYSTEM", "FACTS", new(), new() { PlayerHistory = new() { Candidate() } });
            if (changeEpoch) owner._collectionEpoch++; else SaveRuntimeGuard.Generation++;
            gate.SetResult(new() { Success = true, Content = "{\"public_ids\":[\"memory:1\"]}" });
            await task;
            Drain(owner);
            Check(calls == 1 && cancelled == 1 && owner.Completed == 0, "reset/load during review never starts writer");
            SaveRuntimeGuard.Generation = 1;
        }
        var stale = new WorldBulletinStateOwner();
        int staleCalls = 0;
        stale._port.CallApi = (_, _) => { staleCalls++; return Task.FromResult(new ApiCallResult()); };
        await stale.RunWorldBulletinRequestAsync(1, 0, selection, new(), "SYSTEM", "FACTS", null, new() { PlayerHistory = new() { Candidate() } });
        Check(staleCalls == 0, "already stale request does not send history to reviewer");
    }
}
