using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class AutonomousRecallReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host, IWorldDiplomacyPersonalRecall
    {
        internal int Reads;
        internal bool SelectedHistoryReceived;
        public string PersonalMemory(string ruler, string topic, string counterpart, string history)
        { SelectedHistoryReceived = history.Contains("ONLY_IF_RETURNED"); return PersonalMemory(ruler, topic, counterpart); }
        public override string PersonalMemory(string ruler, string topic, string counterpart)
        { Reads++; return string.Join("\n\n", Enumerable.Range(0, 8).Select(i => "PERSONAL_" + i + ":" + new string('p', 1800) + " CONDITION_END_" + i)); }
    }
    private static object? Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args);
    internal static void Run()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var round = o.EnsureActiveRound("a", "b", false); round.RoundTopic = "trade";
        string body = "trade " + new string('x', 5000) + " ONLY_IF_RETURNED";
        o.CurrentStorage.Documents.Add(new() { DocumentId = "source", RoundId = round.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b", Body = body, IsReadyForPublication = true });
        o.CurrentStorage.EventAnnouncements.Add(new() { Id = "latest", EventId = "unrelated", State = "public", Title = "Latest sports", Body = "UNRELATED_LATEST", Day = 9999 });
        var job = new WorldDiplomacyJob { Kind = "generate", RoundId = round.RoundId, AuthorKingdomId = "a", TargetKingdomId = "b", PersonalMemoryRulerId = "ruler_a", SourceDocumentId = "source", UserPrompt = "CURRENT_AUTHORITY_AND_TERMS", SystemPrompt = "rules" };
        string recall = (string)Invoke(o, "BuildPrivateDiplomacyRecall", job)!;
        Test.True(h.SelectedHistoryReceived, "actual recall port receives the selected sources for exact old-copy deduplication");
        var duplicateBudget = new RequestRecallBudget(new[] { RequestRecallBudget.WrapDiplomacy(Newtonsoft.Json.JsonConvert.SerializeObject(new { source = "old_doc", Body = body })) });
        string wrapper = "I have officially published a diplomatic document, source=old_doc, date=1, publishing kingdom=a, title=T. Original text: " + body + ". This entry only confirms the document was published or received; it does not prove the proposal was accepted or diplomatic actions executed.";
        Test.True(duplicateBudget.IsDuplicateDiplomaticCopy(new() { Role = "system", Content = wrapper }), "known exact old copied document is excluded from personal recall");
        Test.True(!duplicateBudget.IsDuplicateDiplomaticCopy(new() { Role = "user", Content = wrapper })
            && !duplicateBudget.IsDuplicateDiplomaticCopy(new() { Role = "system", Content = wrapper + " A separate fact." }), "player quotations and additional personal evidence survive deduplication");
        Test.True(!duplicateBudget.IsDuplicateDiplomaticCopy(new() { Role = "system", Content = "A separate fact. " + wrapper }), "leading independent evidence cannot be deleted with a copied document");
        Test.True(duplicateBudget.IsDuplicateDiplomaticCopy(new() { Role = "system", Content = PromptComposerTexts.Raw("afef_npc_action") + wrapper }), "exact historical AFEF producer envelope still deduplicates");
        Test.True(recall.Length <= RequestRecallBudget.MaximumChars + 1 && recall.Contains("ONLY_IF_RETURNED"), "actual autonomous owner shares one history budget and preserves a document's trailing condition");
        Test.True(!recall.Contains("UNRELATED_LATEST") && !recall.Contains("<af-recall"), "no unrelated latest event or request marker reaches the private source");
        for (int i = 0; i < 8; i++) Test.True(recall.Contains("PERSONAL_" + i) == recall.Contains("CONDITION_END_" + i), "personal recall retains whole conditions: " + i);
        Test.True(recall.Contains("PERSONAL_0") && !recall.Contains("PERSONAL_7"), "personal recall spends the remaining shared budget");
        h.Reads = 0; Invoke(o, "PrepareDiplomacyPrivacy", job);
        Test.True(job.Kind == "privacy_material" && h.Reads == 1 && job.UserPrompt.Contains("CURRENT_AUTHORITY_AND_TERMS")
            && job.UserPrompt.Contains("ONLY_IF_RETURNED"), "actual privacy stage receives the common history once alongside authoritative current input");
        job.Kind = "generate"; job.PrivacyPublicContext = "publicly safe history and facts";
        o.CaptureCanonicalHistoryForJob(job, false);
        var messages = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _ => throw new Exception("full archive read"));
        Test.True(job.DeclarationHistoryBlock == WorldDiplomacyRequestHistoryApplication.ContextMarker
            && !messages[1]!["content"]!.ToString().Contains(body), "generation does not append raw history a second time after privacy isolation");
        Test.True(o.CurrentStorage.Documents.Single().Body == body, "shared selection never rewrites archived prose");
        job.PrivacyPublicContext = ""; job.SourceDocumentId = ""; job.RoundId = "missing";
        Test.True(!WorldDiplomacyRequestHistoryApplication.Build(o.CurrentStorage, job, h.CurrentDayValue).Contains(body), "no event or subject means no recent bilateral filler");
    }
}
