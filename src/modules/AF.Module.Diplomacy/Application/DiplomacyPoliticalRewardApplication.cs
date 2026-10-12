using AnimusForge;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal enum DiplomacyPoliticalRewardKind { Vassalage, Annexation }
internal readonly struct DiplomacyPoliticalRewardReceipt
{
    internal DiplomacyPoliticalRewardReceipt(bool available, bool applied, string status)
    { Available = available; Applied = applied; Status = status; }
    internal bool Available { get; }
    internal bool Applied { get; }
    internal string Status { get; }
}
internal interface IDiplomacyPoliticalRewardPort
{
    bool GiverIsPlayer { get; }
    bool ReceiverIsPlayer { get; }
    DiplomacyPoliticalRewardReceipt Apply(DiplomacyPoliticalRewardKind kind, string type, string target);
    void Record(DiplomacyPoliticalRewardKind kind, string stage, string tag, string type, string target, bool applied, string status);
    void Show(DiplomacyPoliticalRewardKind kind, bool applied, string status);
    void Unsupported(DiplomacyPoliticalRewardKind kind, string tag);
}

// Runs only when a committed channel response contains reward tags. AF retains its one facts/history writer.
internal static class DiplomacyPoliticalRewardApplication
{
    private static readonly Regex Vassalage = new Regex("\\[ACTION:VASSALAGE:SUBMIT:(TRIBUTARY|GARRISON|VASSAL|MILITARY|PROTECTORATE):([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AnyVassalage = new Regex("\\[ACTION:VASSALAGE:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Annexation = new Regex("\\[ACTION:KINGDOM_ANNEX:target_kingdom_id=([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AnyAnnexation = new Regex("\\[ACTION:KINGDOM_ANNEX:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static bool Apply(IDiplomacyPoliticalRewardPort port, DiplomacyPoliticalRewardKind kind,
        ref string text, List<string> giverFacts, List<string> receiverFacts)
    {
        bool any = false;
        bool vassalage = kind == DiplomacyPoliticalRewardKind.Vassalage;
        text = (vassalage ? Vassalage : Annexation).Replace(text, match =>
        {
            string type = vassalage ? match.Groups[1].Value.Trim() : "";
            string target = match.Groups[vassalage ? 2 : 1].Value.Trim();
            port.Record(kind, "matched", match.Value, type, target, false, "");
            if (port.ReceiverIsPlayer && !port.GiverIsPlayer)
            {
                var receipt = port.Apply(kind, type, target);
                string status = receipt.Status;
                if (vassalage && !receipt.Available) status = DiplomacyMiscPromptTexts.Get("Political_VassalageNotInitialized", "Vassalage terms not executed: vassalage system is not yet initialized.");
                port.Record(kind, "applied", match.Value, type, target, receipt.Applied, status);
                if (!vassalage && string.IsNullOrWhiteSpace(status) && !receipt.Available)
                    status = DiplomacyMiscPromptTexts.Get("Political_AnnexNotInitialized", "Kingdom annexation not executed: annexation system is not yet initialized.");
                if (!string.IsNullOrWhiteSpace(status))
                {
                    any |= receipt.Applied;
                    giverFacts.Add(status); receiverFacts.Add(status);
                    port.Show(kind, receipt.Applied, status);
                }
            }
            else if (vassalage) port.Record(kind, "skipped", match.Value, type, target, false, "");
            return string.Empty;
        });
        text = (vassalage ? AnyVassalage : AnyAnnexation).Replace(text, match =>
        {
            if (!vassalage) port.Unsupported(kind, match.Value);
            port.Record(kind, "unsupported", match.Value, "", "", false, "");
            if (port.ReceiverIsPlayer && !port.GiverIsPlayer)
            {
                string status = vassalage ? DiplomacyMiscPromptTexts.Get("Political_VassalageActionUnsupported", "Vassalage terms not executed: unsupported VASSALAGE action.") : DiplomacyMiscPromptTexts.Get("Political_AnnexTagUnsupported", "Kingdom annexation not executed: unsupported KINGDOM_ANNEX tag format.");
                giverFacts.Add(status); receiverFacts.Add(status);
                port.Show(kind, false, status);
                if (vassalage) port.Unsupported(kind, match.Value);
            }
            return string.Empty;
        });
        return any;
    }
}
