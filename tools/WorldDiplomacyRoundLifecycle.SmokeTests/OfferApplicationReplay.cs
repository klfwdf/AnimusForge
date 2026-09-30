using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;
using Newtonsoft.Json;

internal static class OfferApplicationReplay
{
    internal static void Run()
    {
        var proposal = NewHarness("propose_peace");
        proposal.Response.TargetKingdomId = "other";
        proposal.Run();
        Test.True(proposal.Round.PendingOffers.Count == 2
                && proposal.Round.PendingOffers.Last().SourceDocumentId == "response"
                && proposal.Round.PendingOffers.Last().Status == "open"
                && proposal.Trace.SequenceEqual(new[] { "prune", "proposal-check" }),
            "legal proposal registers one canonical offer after pruning and main-thread validation");

        var blocked = NewHarness("propose_trade");
        blocked.Response.TargetKingdomId = "other";
        blocked.ProposalBlockReason = "invalid state";
        blocked.Run();
        Test.True(blocked.Round.PendingOffers.Count == 1
                && blocked.Response.MechanicalResult == "提议未登记：invalid state",
            "invalid proposal changes only its recorded mechanical result");

        var missingSource = NewHarness("accept_peace");
        missingSource.Response.RespondingToOfferDocumentId = "";
        missingSource.Run();
        Test.True(missingSource.Response.MechanicalResult == "答复未执行：缺少唯一来源提议"
                && missingSource.Trace.SequenceEqual(new[] { "prune" }),
            "unbound response never reaches a game action");

        var wrongVersion = NewHarness("accept_peace");
        wrongVersion.Response.RespondingToOfferActionId = "late-action";
        wrongVersion.Run();
        Test.True(wrongVersion.Response.MechanicalResult == "答复未执行：来源提议已关闭、失效或不唯一"
                && wrongVersion.Round.PendingOffers[0].Status == "open",
            "late or mismatched source action cannot settle a newer offer");

        var rejected = NewHarness("reject_peace");
        rejected.Run();
        Test.True(rejected.Round.PendingOffers[0].Status == "rejected"
                && rejected.Trace.SequenceEqual(new[] { "prune" }),
            "rejection closes the exact offer without resolving parties or executing effects");

        var missingDocument = NewHarness("accept_peace");
        missingDocument.SourceAvailable = false;
        missingDocument.Run();
        Test.True(missingDocument.Round.PendingOffers[0].Status == "invalidated"
                && missingDocument.Response.MechanicalResult == "接受未执行：原提议或当事国已失效"
                && !missingDocument.Trace.Contains("parties"),
            "missing canonical source invalidates acceptance before world resolution");

        var missingParties = NewHarness("accept_peace");
        missingParties.PartiesAvailable = false;
        missingParties.Run();
        Test.True(missingParties.Round.PendingOffers[0].Status == "invalidated"
                && !missingParties.Trace.Contains("execute"),
            "lost game parties cannot execute an acceptance");

        var invalidTerms = NewHarness("accept_peace");
        invalidTerms.TermsExecutable = false;
        invalidTerms.Run();
        Test.True(invalidTerms.Round.PendingOffers[0].Status == "invalidated"
                && invalidTerms.Response.MechanicalResult == "接受未执行：和平原案条款已无法原样履行",
            "peace acceptance revalidates the original terms immediately before the effect");

        var accepted = NewHarness("accept_peace");
        accepted.Run();
        Test.True(accepted.Round.PendingOffers[0].Status == "accepted"
                && accepted.Response.ChangedDiplomaticState
                && accepted.Trace.SequenceEqual(new[] { "prune", "source", "parties", "execute" }),
            "successful acceptance settles exactly once after source and party resolution");
        string saved = JsonConvert.SerializeObject(accepted.Round);
        Test.True(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<WorldDiplomacyRound>(saved)) == saved,
            "accepted offer identity and status survive a real JSON round trip");

        var partial = NewHarness("accept_peace");
        partial.PartialExecution = true;
        partial.Run();
        Test.True(partial.Round.PendingOffers[0].Status == "partially_executed",
            "game-confirmed transfer failure retains the original partial execution status");

        var failed = NewHarness("accept_peace");
        failed.ThrowOnExecute = true;
        failed.Run();
        Test.True(failed.Round.PendingOffers[0].Status == "execution_failed"
                && failed.Response.MechanicalResult.StartsWith("接受未执行：", StringComparison.Ordinal)
                && failed.Trace.Last().StartsWith("log:", StringComparison.Ordinal),
            "mechanical exception without effect records failure and logs it");

        var effectedThenThrew = NewHarness("accept_peace");
        effectedThenThrew.ThrowOnExecute = true;
        effectedThenThrew.EffectAfterException = true;
        effectedThenThrew.Run();
        Test.True(effectedThenThrew.Round.PendingOffers[0].Status == "partially_executed"
                && effectedThenThrew.Response.ChangedDiplomaticState
                && effectedThenThrew.Trace.Contains("effect-probe"),
            "post-effect exception checks the real game outcome before recording success");

        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        Test.True(root != null, "repository located for offer source boundary");
        string owner = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOfferApplication.cs"));
        Test.True(!owner.Contains("TaleWorlds", StringComparison.Ordinal)
                && !owner.Contains("Kingdom proposer", StringComparison.Ordinal)
                && !owner.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "offer application owns ledger transitions without retaining live game objects");
    }

    private static Harness NewHarness(string intent)
    {
        var result = new Harness();
        result.Round.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "other", TargetKingdomId = "player",
            Status = "open", SourceDocumentId = "source", SourceActionId = "action"
        });
        result.Response.Intent = intent;
        return result;
    }

    private sealed class Harness
    {
        internal readonly WorldDiplomacyRound Round = new WorldDiplomacyRound { RoundId = "round" };
        internal readonly WorldDiplomacyDocument Response = new WorldDiplomacyDocument
        {
            DocumentId = "response", RoundId = "round", AuthorKingdomId = "player",
            TargetKingdomId = "other", RespondingToOfferDocumentId = "source",
            RespondingToOfferActionId = "action"
        };
        internal readonly WorldDiplomacyDocument Source = new WorldDiplomacyDocument { DocumentId = "source" };
        internal readonly List<string> Trace = new List<string>();
        internal string ProposalBlockReason;
        internal bool SourceAvailable = true;
        internal bool PartiesAvailable = true;
        internal bool TermsExecutable = true;
        internal bool PartialExecution;
        internal bool ThrowOnExecute;
        internal bool EffectAfterException;

        internal void Run()
        {
            WorldDiplomacyOfferApplication.Settle(
                Round, Response,
                _ => Trace.Add("prune"),
                (_, __) => { Trace.Add("proposal-check"); return (ProposalBlockReason != null, ProposalBlockReason); },
                _ => { Trace.Add("source"); return SourceAvailable ? Source : null; },
                _ => { Trace.Add("parties"); return PartiesAvailable; },
                (_, __, ___, document) =>
                {
                    Trace.Add("execute");
                    if (ThrowOnExecute) throw new InvalidOperationException("game effect failed");
                    if (!TermsExecutable) return WorldDiplomacyOfferOutcome.Invalidated;
                    document.ChangedDiplomaticState = true;
                    document.MechanicalResult = PartialExecution ? "交割失败" : "执行成功";
                    return PartialExecution ? WorldDiplomacyOfferOutcome.Partial : WorldDiplomacyOfferOutcome.Applied;
                },
                (_, __) => { Trace.Add("effect-probe"); return EffectAfterException; },
                message => Trace.Add("log:" + message));
        }
    }
}
