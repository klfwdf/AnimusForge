using AnimusForge;
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

            return new(true, false, true, DiplomacyMiscPromptTexts.Get("Cession_Invalid", "; Settlement cession failed: target, ownership, or recipient lord is invalid"));

        var result = AnimusForge.Refactor.Adapters.DiplomacyEffectReadback.Execute(

            () => ChangeOwnerOfSettlementAction.ApplyByBarter(recipient, settlement),

            () => settlement.OwnerClan == recipient.Clan && settlement.OwnerClan?.Kingdom == to);

        if (!string.IsNullOrWhiteSpace(result.Diagnostic)) log?.Invoke("peace cession readback settlement=" + settlement.StringId + " error=" + result.Diagnostic);

        return new(true, result.Applied, result.IsKnown, result.Applied
            ? string.Format(DiplomacyMiscPromptTexts.Get("Cession_Applied", "; {0} has ceded {1} to {2}"), from.Name, settlement.Name, to.Name)
            : result.IsKnown ? DiplomacyMiscPromptTexts.Get("Cession_Unchanged", "; Settlement cession failed: ownership did not change") : DiplomacyMiscPromptTexts.Get("Cession_Unknown", "; Settlement cession outcome cannot be confirmed"));

    }

}

