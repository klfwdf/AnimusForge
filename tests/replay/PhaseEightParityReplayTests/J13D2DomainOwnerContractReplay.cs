using System;
using System.IO;

internal static class J13D2DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13D2 owner contract: " + label);
        }
        string proactive = Read("ProactiveNpcRequestBehavior.cs");
        string qualification = Read("src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs");
        Require(proactive.Contains("_cooldownOwner.TryBeginScan(", StringComparison.Ordinal)
            && proactive.Contains("FindBestRequestCandidate(scan.Settings", StringComparison.Ordinal)
            && qualification.Contains("private ProactiveCandidate FindBestRequestCandidate(", StringComparison.Ordinal)
            && proactive.Contains("_sessionOwner.TryStart(session)", StringComparison.Ordinal)
            && proactive.Contains("_openingOwner.TryConsume(", StringComparison.Ordinal)
            && proactive.Contains("_sessionOwner.Clear();", StringComparison.Ordinal)
            && proactive.Contains("_openingOwner.Clear();", StringComparison.Ordinal), "Social hourly-to-opening consumer chain");

        string bridge = Read("VanillaIssueOfferBridge.cs");
        string state = Read("src/modules/AF.Module.Issue/Runtime/IssueRuntimeStateOwner.cs");
        string action = Read("src/modules/AF.Module.Issue/Actions/IssueActionOwner.cs");
        string completion = Read("VanillaIssuePromptBehavior.cs");
        Require(bridge.Contains("IssueActionOwner.TryAcceptIssueSelf(", StringComparison.Ordinal)
            && bridge.Contains("IssueActionOwner.TryAcceptIssueWithCompanion(", StringComparison.Ordinal)
            && bridge.Contains("IssueActionOwner.TryTurnInIssue(", StringComparison.Ordinal)
            && state.Contains("issue.IsOngoingWithoutQuest", StringComparison.Ordinal)
            && action.Contains("ReferenceEquals(currentIssue, issue)", StringComparison.Ordinal)
            && bridge.Contains("_dispatchOwner.IsCurrent(expected)", StringComparison.Ordinal)
            && completion.Contains("IssueCompletionReceiptOwner.TryBuildCompletionReceipt(", StringComparison.Ordinal),
            "Issue offer/duplicate/stale/turn-in/completion consumer chain");
        int start = action.IndexOf("internal static bool TryAcceptIssueSelf(", StringComparison.Ordinal);
        int end = action.IndexOf("internal static bool TryAcceptIssueWithCompanion(", start, StringComparison.Ordinal);
        string self = action.Substring(start, end - start);
        int questStart = self.IndexOf("StartIssueQuest(giver)", StringComparison.Ordinal);
        int acceptFinalized = self.IndexOf("FinalizeClassicQuestAcceptance(issue", StringComparison.Ordinal);
        int acceptFact = self.IndexOf("MyBehavior.AppendExternalNpcFact(", StringComparison.Ordinal);
        Require(questStart >= 0 && acceptFinalized > questStart && acceptFact > acceptFinalized,
            "acceptance fact after original success");
        start = action.IndexOf("internal static bool TryTurnInIssue(", StringComparison.Ordinal);
        string turnIn = action.Substring(start);
        int questExecuted = turnIn.IndexOf("TryProbeQuestTurnIn(giver, issue, execute: true", StringComparison.Ordinal);
        int turnInFact = turnIn.IndexOf("MyBehavior.AppendExternalNpcFact(", StringComparison.Ordinal);
        Require(questExecuted >= 0 && turnInFact > questExecuted, "turn-in fact after original execution");

        Require(Read("AIConfigHandler.cs").Contains("VanillaIssueOfferBridge.BuildRuntimePromptBlockForExternal(", StringComparison.Ordinal)
            && Read("ShoutBehavior.cs").Contains("VanillaIssueOfferBridge.ApplyIssueOfferTags(", StringComparison.Ordinal)
            && Read("src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs")
                .Contains("VanillaIssueOfferBridge.BuildRuntimePostprocessRulesForExternal(", StringComparison.Ordinal)
            && Read("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs")
                .Contains("VanillaIssueOfferBridge.ApplyIssueOfferTags(", StringComparison.Ordinal), "Native/Scene/Courier shared Issue entries");
        string transport = Read("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs");
        start = transport.IndexOf("if (!session.DeliveryApplied)", StringComparison.Ordinal);
        end = transport.IndexOf("string senderHeroId", start, StringComparison.Ordinal);
        Require(start >= 0 && end > start, "Courier delivery boundary found");
        string delivery = transport.Substring(start, end - start);
        Require(delivery.Contains("ProactiveNpcRequestBehavior.RecordLetterNeedDeliveredForExternal(", StringComparison.Ordinal)
            && !delivery.Contains("VanillaIssueOfferBridge.ApplyIssueOfferTags(", StringComparison.Ordinal),
            "letter delivery is not quest acceptance");
        Console.WriteLine("PASS J13D2DomainOwnerContractReplay socialChain=1 issueChain=1 confirmedFacts=1 threeChannels=1 deliveryNotAcceptance=1; source-wiring-only live=NOT_RUN");
    }
}
