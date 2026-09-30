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
                if (vassalage && !receipt.Available) status = "臣属条款未执行：臣属国系统尚未初始化。";
                port.Record(kind, "applied", match.Value, type, target, receipt.Applied, status);
                if (!vassalage && string.IsNullOrWhiteSpace(status) && !receipt.Available)
                    status = "国家吞并未执行：吞并系统尚未初始化。";
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
                string status = vassalage ? "臣属条款未执行：不支持该 VASSALAGE 动作。" : "国家吞并未执行：不支持该 KINGDOM_ANNEX 标签格式。";
                giverFacts.Add(status); receiverFacts.Add(status);
                port.Show(kind, false, status);
                if (vassalage) port.Unsupported(kind, match.Value);
            }
            return string.Empty;
        });
        return any;
    }
}
