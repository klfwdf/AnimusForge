using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class WorldNewsRecallPromptAdapter
{
    // Called only by shared game-thread capture. Permission is evaluated before any
    // source text reaches embedding/reranking, regardless of conversation channel.
    internal static WorldNewsRecallRequest Capture(Hero hero, string kingdomOverride, string query)
    {
        if (!TWParallel.IsMainThread() || !MyBehavior.IsWorldBulletinEnabled() || string.IsNullOrWhiteSpace(query)) return null;
        MyBehavior owner = MyBehavior.Instance;
        if (owner == null) return null;
        string kingdom = MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportNpcKingdomId(hero, null, kingdomOverride);
        string local = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(hero?.CurrentSettlement?.MapFaction);
        if (string.IsNullOrWhiteSpace(local) && hero == null)
            local = MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportSurroundingsKingdomId(null, null, kingdomOverride);
        ISet<string> documents = null;
        try { documents = DiplomacyModuleServices.World.CaptureKnownDocumentIds(hero?.StringId, kingdom); }
        catch (Exception ex) { Logger.Log("WorldNewsRecall", "knowledge_capture_failed type=" + ex.GetType().Name); }
        var observer = new WorldNewsObserver {
            HeroId = hero?.StringId ?? "", KingdomId = kingdom, LocalKingdomId = local,
            Day = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe(),
            KnownDocuments = documents
        };
        var request = owner.CaptureWorldNewsRecall(observer, query, SaveRuntimeGuard.CaptureGeneration());
        foreach (var candidate in request.Candidates)
        {
            candidate.Fact.Sentence = Project(candidate.Fact.Kind, candidate.Fact.Sentence);
            candidate.Fact.Detail = Project(candidate.Fact.Kind, candidate.Fact.Detail);
            candidate.SearchText = WorldNewsFactArchive.SearchText(candidate.Fact);
        }
        return request;
    }

    private static string Project(string kind, string text)
        => kind == "coup_success" || kind == "coup_failure"
            ? AnimusForge.CoupSystem.CoupSourceTexts.ReportForPrompt(text ?? "")
            : CampaignSystemFactProjection.Project(kind, text ?? "");

    internal static WorldNewsRecallResult Retrieve(WorldNewsRecallRequest request)
    {
        if (request == null || !SaveRuntimeGuard.IsCurrentGeneration(request.Generation)) return null;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        // Synchronous compatibility callers retain deterministic retrieval without blocking
        // the game thread on local model initialization/inference.
        bool worker = !TWParallel.IsMainThread();
        float[] Embed(string text)
            => OnnxEmbeddingEngine.Instance.TryGetEmbedding(text, out var vector) ? vector : null;
        float[] Rerank(string query, IReadOnlyList<string> documents)
            => OnnxCrossEncoderReranker.Instance.TryScoreBatch(query, documents, out var scores) ? scores.ToArray() : null;
        var result = WorldNewsRecallPolicy.Retrieve(request, worker ? Embed : null, worker ? Rerank : null);
        if (!SaveRuntimeGuard.IsCurrentGeneration(request.Generation)) return null;
        Logger.Log("WorldNewsRecall", "mode=" + result.Mode + " candidates=" + request.Candidates.Count
            + " selected=" + result.Selected.Count + " worker=" + worker
            + " elapsedMs=" + clock.ElapsedMilliseconds);
        return result;
    }

    internal static string Render(WorldNewsRecallResult result)
    {
        if (result == null || result.Selected.Count == 0 || !SaveRuntimeGuard.IsCurrentGeneration(result.Generation)
            || !TWParallel.IsMainThread() || !MyBehavior.IsWorldBulletinEnabled()) return "";
        WorldNewsObserver observer = result.Observer;
        if (observer == null) return "";
        Hero hero = string.IsNullOrEmpty(observer.HeroId) ? null : Hero.Find(observer.HeroId);
        if (!string.IsNullOrEmpty(observer.HeroId) && hero == null) return "";
        string kingdom = MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportNpcKingdomId(hero, null);
        string local = hero == null
            ? MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportSurroundingsKingdomId(null, null)
            : MemoryEntityIdentityBannerlordAdapter.GetKingdomId(hero.CurrentSettlement?.MapFaction);
        ISet<string> documents = null;
        try { documents = DiplomacyModuleServices.World.CaptureKnownDocumentIds(hero?.StringId, kingdom); }
        catch { } // Failure cannot preserve an earlier, now-unverifiable diplomacy permission.
        var current = new WorldNewsObserver { HeroId=hero?.StringId ?? "", KingdomId=kingdom,
            LocalKingdomId=local, Day=MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe(),
            KnownDocuments=documents };
        var visible = result.Selected.Where(x => WorldNewsFactArchive.Visible(x.Fact, current))
            .Take(WorldNewsRecallPolicy.FinalLimit).ToList();
        if (visible.Count == 0) return "";
        var output = new StringBuilder(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_RecallHeader"));
        foreach (var selected in visible)
        {
            string date = string.IsNullOrWhiteSpace(selected.Fact.Date)
                ? WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_RecallDay", "Recorded game day: {DAY}",
                    ("DAY", selected.Fact.Day.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                : selected.Fact.Date;
            string source = WorldBulletinPolicyTexts.Prompt(selected.Participant
                ? "AF_WBLT_Prompt_RecallParticipant" : "AF_WBLT_Prompt_RecallPublic");
            string body = selected.Fact.Sentence;
            if (!string.IsNullOrWhiteSpace(selected.Fact.Detail)) body += " " + selected.Fact.Detail;
            // Presentation budget only: the complete confirmed source remains in the archive.
            body = WorldBulletinPolicy.Truncate(body.Replace("\r", " ").Replace("\n", " "), 480);
            output.Append("\n- ").Append(date.Replace("\r", " ").Replace("\n", " "))
                .Append(" | ").Append(source).Append(": ").Append(body);
        }
        output.Append("\n").Append(WorldBulletinPolicyTexts.Prompt("AF_WBLT_Prompt_RecallBoundary"));
        return output.ToString();
    }
}
