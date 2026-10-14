using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class Program
{
    private static int count;
    private static void Check(bool ok, string name)
    { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }

    private static void Main()
    {
        var state = new MemoryBusinessStateOwner();
        var recent = new RewriteHistoryCommitOwnerProbe();
        var capture = new MemoryDailyAppendCapabilities(id => true, () => 2, () => "day2", () => 7, () => 1, () => "town", () => 2, id => { }, text => { });
        void Append(string id, string speaker, string text, bool fact = false)
        { Check(state.AppendDailyMemoryLineById(id, "NPC", speaker, text, fact, !fact, -1, -1, "", capture), "rewrite actual daily append published"); }
        Append("hero", "player", "repeat"); Append("hero", "NPC", "repeat");
        recent.Append("hero", 2, "repeat", true); recent.Append("hero", 2, "repeat", false);
        var turn = new ConversationRewriteTurn("repeat");
        using (ConversationRewriteTurn.CaptureScope(turn))
        {
            Append("hero", "player", "repeat"); recent.Append("hero", 2, "repeat", true);
            Append("hero", "NPC", "repeat"); recent.Append("hero", 2, "repeat", false);
            Append("hero", "AFEF", "[AFEF NPC行为补充] retained", true);
        }
        // Main-thread sanitize/regroup replaces lists while retaining the source objects.
        var draft = state.Drafts["hero"][0]; var before = state.Drafts["hero"];
        state.SaveDrafts("hero", before);
        Check(!ReferenceEquals(before, state.Drafts["hero"]) && ReferenceEquals(draft, state.Drafts["hero"][0]), "rewrite main-thread sanitize preserves exact draft identity");
        recent._dialogueHistory["hero"] = DialogueHistoryLedger.Regroup(DialogueHistoryLedger.Flatten(recent._dialogueHistory["hero"], d => d.GameDayIndex, d => d.GameDate, d => d.Lines),
            (day, date) => new MyBehavior.DialogueDay { GameDayIndex = day, GameDate = date }, d => d.GameDayIndex, d => d.Lines);
        turn.Complete(); Check(turn.TryRewrite(out _), "rewrite daily plus regrouped durable history accepted");
        Check(draft.Lines.Select(x => x.Text).SequenceEqual(new[] { "repeat", "repeat", "[AFEF NPC行为补充] retained" }) && recent._dialogueHistory["hero"][0].Lines.Count == 2,
            "rewrite removes exact latest duplicate durable and daily rows with facts retained");
        Check(draft.HasLlmDialogue, "rewrite earlier daily dialogue flag remains");

        var changed = new ConversationRewriteTurn("new");
        using (ConversationRewriteTurn.CaptureScope(changed))
        { Append("hero", "player", "new"); recent.Append("hero", 2, "new", true); Append("hero", "NPC", "reply"); recent.Append("hero", 2, "reply", false); }
        changed.Complete(); draft.Lines.Last().Text = "manual edit";
        int countBefore = recent._dialogueHistory["hero"][0].Lines.Count;
        Check(!changed.TryRewrite(out _) && recent._dialogueHistory["hero"][0].Lines.Count == countBefore, "rewrite changed daily source rejects before persistent removal");
        var queued = new ConversationRewriteTurn("queued");
        using (ConversationRewriteTurn.CaptureScope(queued))
        { Append("hero", "player", "queued"); recent.Append("hero", 2, "queued", true); Append("hero", "NPC", "queued-reply"); recent.Append("hero", 2, "queued-reply", false); }
        queued.Complete(); draft.QueuedForSummary = true;
        Check(!queued.TryRewrite(out _), "rewrite queued compression source rejected"); draft.QueuedForSummary = false;
        var last = new ConversationRewriteTurn("only");
        using (ConversationRewriteTurn.CaptureScope(last))
        { Append("only", "player", "only"); recent.Append("only", 2, "only", true); Append("only", "NPC", "reply"); recent.Append("only", 2, "reply", false); Append("only", "AFEF", "fact", true); }
        last.Complete();
        Check(last.TryRewrite(out _) && !state.Drafts["only"][0].HasLlmDialogue && state.Drafts["only"][0].Lines.Single().IsAfef,
            "rewrite last dialogue removes summary-source flag while preserving actual fact");
        Console.WriteLine("CONVERSATION_REWRITE_DAILY pass=" + count + " fail=0 LIVE=NOT_RUN SAVE=NOT_RUN");
    }
}
