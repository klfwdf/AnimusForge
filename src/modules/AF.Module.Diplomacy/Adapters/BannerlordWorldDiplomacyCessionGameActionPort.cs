using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.Refactor.Adapters;

internal static class BannerlordWorldDiplomacyCessionGameActionPort
{
    internal static WorldDiplomacyCessionReceipt Apply(WorldDiplomacyPeaceTerms terms,
        Kingdom first, Kingdom second, Kingdom from, Kingdom to, Settlement settlement, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(terms?.CessionSettlementId)) return new(false, false, true, "");
        Hero recipient = to?.RulingClan?.Leader;
        if (from == null || to == null || from == to || from.IsEliminated || to.IsEliminated
            || (from != first && from != second) || (to != first && to != second)
            || settlement == null || settlement.OwnerClan?.Kingdom != from || recipient == null)
            return new(true, false, true, "；领地交割失败：目标、归属或受让领主已失效");
        var result = AnimusForge.Refactor.Adapters.DiplomacyEffectReadback.Execute(
            () => ChangeOwnerOfSettlementAction.ApplyByBarter(recipient, settlement),
            () => settlement.OwnerClan == recipient.Clan && settlement.OwnerClan?.Kingdom == to);
        if (!string.IsNullOrWhiteSpace(result.Diagnostic)) log?.Invoke("peace cession readback settlement=" + settlement.StringId + " error=" + result.Diagnostic);
        return new(true, result.Applied, result.IsKnown, result.Applied
            ? "；已将" + from.Name + "割让" + settlement.Name + "给" + to.Name
            : result.IsKnown ? "；领地交割失败：归属未改变" : "；领地交割结果无法确认");
    }
}
