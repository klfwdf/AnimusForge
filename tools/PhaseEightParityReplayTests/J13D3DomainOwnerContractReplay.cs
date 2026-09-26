using System;
using System.IO;

internal static class J13D3DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13D3 owner contract: " + label);
        }

        string composition = Read("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string host = Read("WorldEvents/WorldEventInbox.cs");
        string owner = Read("src/modules/AF.Module.WorldEvents/WorldEventInboxOwner.cs");
        string policy = Read("PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs");
        string policyUi = Read("PolicySystem/UI/PolicySystemUi.cs");
        string diplomacy = Read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs");
        string terminal = Read("AnimusForgeTerminalBehavior.cs");

        Require(composition.Contains("AddBehavior(new AnimusForgeWorldEventBehavior())", StringComparison.Ordinal)
            && host.Contains("_inbox.Import(CampaignSaveChunkHelper.RestoreStringDictionary(stored, \"WorldEventInbox\"), unreadIds)", StringComparison.Ordinal)
            && host.Contains("_inbox.ExportRecords()", StringComparison.Ordinal)
            && host.Contains("_inbox.ExportUnread()", StringComparison.Ordinal)
            && host.Contains("_afWorldEventInboxRecords_v1", StringComparison.Ordinal)
            && host.Contains("_afWorldEventInboxUnread_v1", StringComparison.Ordinal),
            "registered behavior retains both save keys and delegates import/export");
        Require(host.Contains("Instance?._inbox.Upsert(entry, markUnread)", StringComparison.Ordinal)
            && host.Contains("Instance?._inbox.Snapshot(maxCount)", StringComparison.Ordinal)
            && host.Contains("Instance?._inbox.MarkRead(eventId)", StringComparison.Ordinal)
            && owner.Contains("_eventIdByStableKey", StringComparison.Ordinal)
            && !host.Contains("private readonly Dictionary<string, string> _records", StringComparison.Ordinal),
            "single inbox owner feeds original public publication/read routes");
        Require(policy.Contains("AnimusForgeWorldEventBehavior.UpsertWorldEventForExternal(context.PublicFeedbackEntry, markUnread: true)", StringComparison.Ordinal)
            && policy.Contains("AnimusForgeWorldEventBehavior.GetInboxVersionForExternal() <= inboxVersion", StringComparison.Ordinal)
            && policyUi.Contains("AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(EventInboxDisplayLimit)", StringComparison.Ordinal)
            && diplomacy.Contains("AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(160)", StringComparison.Ordinal)
            && host.Contains("AnimusForgeWorldEventBehavior.MarkEventReadForExternal(selected.EventId)", StringComparison.Ordinal)
            && terminal.Contains("WorldDiplomacyBehavior.ShowRoyalAnnouncementArchive(OpenCustomPolicyManagementView)", StringComparison.Ordinal),
            "policy acknowledgement and both archive/UI consumers remain wired");

        Console.WriteLine("PASS J13D3DomainOwnerContractReplay saveFacade=1 policyAck=1 archiveConsumers=2 readRoute=1; source-wiring-only live=NOT_RUN");
    }
}
