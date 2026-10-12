using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PartyTransferPromptEntry = AnimusForge.MyBehavior.PartyTransferPromptEntry;
namespace AnimusForge;
internal sealed class PartyTransferPromptCapture
{
    internal bool Wilderness, CanUseCounterpartyRoster, Notable;
    internal int MaximumTier, MaximumRecruitableVolunteerIndex, VolunteerCount;
    internal string PlayerName, WildernessInstruction, RuntimeHint, TroopRemainder, PrisonerRemainder;
    internal List<PartyTransferPromptEntry> Troops, HiddenTroops, AllTroops, Prisoners, AllPrisoners;
}
internal static class PartyTransferPromptOwner
{
    internal static string Build(PartyTransferPromptCapture c, Func<PartyTransferPromptEntry,string> sourceLabel,
        Func<PartyTransferPromptEntry,string> typeLabel, Func<PartyTransferPromptEntry,int> tier)
    {
			StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(c.WildernessInstruction)) stringBuilder.AppendLine(c.WildernessInstruction.Trim());
			if (!string.IsNullOrWhiteSpace(c.RuntimeHint))
			{
				stringBuilder.AppendLine(c.RuntimeHint.Trim());
			}
			if (c.MaximumTier <= 0 && c.VolunteerCount == 0)
			{
				stringBuilder.AppendLine(EconomyPromptTexts.Get("S659") + c.PlayerName + EconomyPromptTexts.Get("S660"));
			}
			else if (c.CanUseCounterpartyRoster && c.MaximumTier == int.MaxValue)
			{
				stringBuilder.AppendLine(EconomyPromptTexts.Get("S659") + c.PlayerName + EconomyPromptTexts.Get("S661"));
			}
			else if (c.CanUseCounterpartyRoster)
			{
				stringBuilder.AppendLine(EconomyPromptTexts.Get("S662") + c.PlayerName + EconomyPromptTexts.Get("S663") + c.MaximumTier + EconomyPromptTexts.Get("S664"));
			}
			if (c.Notable)
			{
				int maximumRecruitableVolunteerIndex = c.MaximumRecruitableVolunteerIndex;
				if (maximumRecruitableVolunteerIndex >= 0)
				{
					stringBuilder.AppendLine(EconomyPromptTexts.Get("S665") + maximumRecruitableVolunteerIndex + EconomyPromptTexts.Get("S666"));
				}
				else
				{
					stringBuilder.AppendLine(EconomyPromptTexts.Get("S667") + c.PlayerName + EconomyPromptTexts.Get("S668"));
				}
			}
			if (!c.Wilderness)
			{
				stringBuilder.AppendLine(EconomyPromptTexts.Get("S669"));
			}
			stringBuilder.AppendLine(EconomyPromptTexts.Get("S670"));
			stringBuilder.AppendLine(EconomyPromptTexts.Get("S671") + c.PlayerName);
			if (c.MaximumTier > 0 || c.VolunteerCount > 0)
			{
				PartyTransferProjectionOwner.AppendPartyTransferPromptSection(stringBuilder, EconomyPromptTexts.Get("S672"), c.Troops, isPrisoner: false, showPromptIndex: true, sourceLabel, typeLabel);
				string troopRemainder = c.TroopRemainder;
				if (!string.IsNullOrWhiteSpace(troopRemainder))
				{
					stringBuilder.AppendLine(troopRemainder);
				}
			}
			if (c.CanUseCounterpartyRoster && c.MaximumTier > 0 && c.MaximumTier < int.MaxValue)
			{
				PartyTransferProjectionOwner.AppendPartyTransferHiddenTroopSection(stringBuilder, EconomyPromptTexts.Get("S673") + c.PlayerName + EconomyPromptTexts.Get("S674") + c.PlayerName + EconomyPromptTexts.Get("S675"), c.HiddenTroops, typeLabel, tier);
			}
			string troopTotalLabel = c.Wilderness ? EconomyPromptTexts.Get("S676") : EconomyPromptTexts.Get("S677");
			stringBuilder.Append(troopTotalLabel).Append(c.AllTroops.Sum((PartyTransferPromptEntry x) => Math.Max(0, x?.Count ?? 0))).Append(EconomyPromptTexts.Get("S678")).Append(PartyTransferProjectionOwner.CalculatePartyTransferTotalValueForExternal(c.AllTroops, isPrisoner: false)).AppendLine(EconomyPromptTexts.Get("S027"));
			PartyTransferProjectionOwner.AppendPartyTransferPromptSection(stringBuilder, EconomyPromptTexts.Get("S679"), c.Prisoners, isPrisoner: true, showPromptIndex: true, sourceLabel, typeLabel);
			string prisonerRemainder = c.PrisonerRemainder;
			if (!string.IsNullOrWhiteSpace(prisonerRemainder))
			{
				stringBuilder.AppendLine(prisonerRemainder);
			}
			stringBuilder.Append(EconomyPromptTexts.Get("S680")).Append(c.AllPrisoners.Sum((PartyTransferPromptEntry x) => Math.Max(0, x?.Count ?? 0))).Append(EconomyPromptTexts.Get("S681")).Append(PartyTransferProjectionOwner.CalculatePartyTransferTotalValueForExternal(c.AllPrisoners, isPrisoner: true)).AppendLine(EconomyPromptTexts.Get("S027"));
			// This c.PlayerName is injected into the NPC's main reply prompt. Action syntax belongs only
			// to the isolated postprocess prompt, so runtime/configuration values are sanitized here.
			return PartyTransferTagCodec.Strip(stringBuilder.ToString());
    }
}
