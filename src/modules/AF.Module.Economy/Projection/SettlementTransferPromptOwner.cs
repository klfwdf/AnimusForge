using System;
using System.Collections.Generic;
using System.Text;
using SettlementTransferPromptEntry = AnimusForge.MyBehavior.SettlementTransferPromptEntry;
namespace AnimusForge;
internal sealed class SettlementTransferPromptCapture
{
    internal string PlayerName, Guidance, NpcRemainder, PlayerRemainder;
    internal List<SettlementTransferPromptEntry> NpcAssets, PlayerAssets, AllNpcAssets, AllPlayerAssets;
}
internal static class SettlementTransferPromptOwner
{
    internal static string Build(SettlementTransferPromptCapture c, Func<SettlementTransferPromptEntry,bool> isValid,
        Func<SettlementTransferPromptEntry,string> assetId)
    {
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = c.Guidance;
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine(text2.Trim());
			}
			stringBuilder.AppendLine(EconomyPromptTexts.Get("S687"));
			stringBuilder.AppendLine(EconomyPromptTexts.Get("S688") + c.PlayerName);
			PartyTransferProjectionOwner.AppendSettlementTransferPromptSection(stringBuilder, EconomyPromptTexts.Get("S689"), c.NpcAssets, showPromptIndex: true, isValid, assetId);
			string settlementRemainder = c.NpcRemainder;
			if (!string.IsNullOrWhiteSpace(settlementRemainder))
			{
				stringBuilder.AppendLine(settlementRemainder);
			}
			stringBuilder.Append(EconomyPromptTexts.Get("S690")).Append(c.AllNpcAssets.Count).Append(EconomyPromptTexts.Get("S691")).Append(PartyTransferProjectionOwner.CalculateSettlementTransferTotalValueForExternal(c.AllNpcAssets, isValid)).AppendLine(EconomyPromptTexts.Get("S027"));
			PartyTransferProjectionOwner.AppendSettlementTransferPromptSection(stringBuilder, EconomyPromptTexts.Get("S692"), c.PlayerAssets, showPromptIndex: true, isValid, assetId);
			string playerSettlementRemainder = c.PlayerRemainder;
			if (!string.IsNullOrWhiteSpace(playerSettlementRemainder))
			{
				stringBuilder.AppendLine(playerSettlementRemainder);
			}
			stringBuilder.Append(EconomyPromptTexts.Get("S693")).Append(c.AllPlayerAssets.Count).Append(EconomyPromptTexts.Get("S691")).Append(PartyTransferProjectionOwner.CalculateSettlementTransferTotalValueForExternal(c.AllPlayerAssets, isValid)).AppendLine(EconomyPromptTexts.Get("S027"));
			return stringBuilder.ToString().Trim();
    }
}
