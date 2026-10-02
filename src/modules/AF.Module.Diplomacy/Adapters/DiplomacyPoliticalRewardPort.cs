using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal sealed class DiplomacyPoliticalRewardPort : IDiplomacyPoliticalRewardPort
{
    private readonly string giverId, receiverId;
    private Hero giver, receiver;
    private bool resolved;
    internal DiplomacyPoliticalRewardPort(string giverId, string receiverId)
    { this.giverId = giverId; this.receiverId = receiverId; }
    private void Resolve()
    {
        if (resolved) return;
        giver = DiplomacyIdentityResolver.Hero(giverId); receiver = DiplomacyIdentityResolver.Hero(receiverId);
        resolved = true;
    }
    public bool GiverIsPlayer { get { Resolve(); return giver != null && giver == Hero.MainHero; } }
    public bool ReceiverIsPlayer { get { Resolve(); return receiver != null && receiver == Hero.MainHero; } }
    public DiplomacyPoliticalRewardReceipt Apply(DiplomacyPoliticalRewardKind kind, string type, string target)
    {
        Resolve();
        string status = "";
        if (kind == DiplomacyPoliticalRewardKind.Vassalage)
        {
            var owner = VassalageBehavior.Instance;
            bool applied = owner?.TryApplyVassalageAction(giver, "SUBMIT", type, target, out status) ?? false;
            return new DiplomacyPoliticalRewardReceipt(owner != null, applied, status);
        }
        var annexation = KingdomAnnexationBehavior.Instance;
        bool annexed = annexation?.TryApplyKingdomAnnexation(giver, target, out status) ?? false;
        return new DiplomacyPoliticalRewardReceipt(annexation != null, annexed, status);
    }
    public void Record(DiplomacyPoliticalRewardKind kind, string stage, string tag, string type, string target, bool applied, string status)
    {
        Resolve();
        if (kind == DiplomacyPoliticalRewardKind.Vassalage)
        {
            var fields = new Dictionary<string, object> { ["tag"] = tag, ["giver"] = VassalageDiagnosticLog.DescribeHero(giver), ["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver) };
            if (stage == "matched") { fields["receiverIsMainHero"] = ReceiverIsPlayer; fields["giverIsMainHero"] = GiverIsPlayer; }
            if (stage == "matched" || stage == "applied") { fields["typeToken"] = type; fields["kingdomToken"] = target; }
            if (stage == "applied") { fields["ok"] = applied; fields["statusText"] = status; }
            if (stage == "skipped") fields["reason"] = "not_npc_to_main_hero";
            VassalageDiagnosticLog.Event(stage == "unsupported" ? "reward_tags.vassalage_unsupported.matched" : "reward_tags.vassalage_submit." + stage, fields);
            return;
        }
        var diagnostic = new Dictionary<string, object> { ["tag"] = tag, ["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver), ["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver) };
        if (stage != "unsupported")
        {
            diagnostic["targetKingdomId"] = target;
            var observation = new Dictionary<string, object> { ["tag"] = tag, ["giverId"] = giver?.StringId ?? "", ["receiverId"] = receiver?.StringId ?? "", ["targetKingdomId"] = target };
            if (stage == "applied") { diagnostic["ok"] = applied; diagnostic["statusText"] = status; observation["ok"] = applied; observation["statusText"] = status; }
            Logger.Obs("KingdomAnnexation", "reward_tags." + stage, observation);
        }
        KingdomAnnexationDiagnosticLog.Event("reward_tags." + stage, diagnostic);
    }
    public void Show(DiplomacyPoliticalRewardKind kind, bool applied, string status)
    {
        string title = kind == DiplomacyPoliticalRewardKind.Vassalage ? "臣属国条约" : "国家吞并";
        InformationManager.DisplayMessage(new InformationMessage("【" + title + (applied ? "" : "失败") + "】" + status,
            Color.FromUint(applied ? 4278242559u : 4294936661u)));
    }
    public void Unsupported(DiplomacyPoliticalRewardKind kind, string tag) =>
        Logger.Log(kind == DiplomacyPoliticalRewardKind.Vassalage ? "Vassalage" : "KingdomAnnexation", "Unsupported tag ignored: " + tag);
}
