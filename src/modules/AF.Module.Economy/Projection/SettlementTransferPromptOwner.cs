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
			stringBuilder.AppendLine("【固定资产转移规则】固定资产只认本轮清单；只有你最终明确同意现在把你的清单资产转给玩家，才算真的转移。村庄不单独转移,注意，你绝不可以同意转让【你当前可转移固定资产】中不存在的资产，那会导致转让无效！");
			stringBuilder.AppendLine("当前与你谈判的人：" + c.PlayerName);
			PartyTransferProjectionOwner.AppendSettlementTransferPromptSection(stringBuilder, "【你当前可转移固定资产】：", c.NpcAssets, showPromptIndex: true, isValid, assetId);
			string settlementRemainder = c.NpcRemainder;
			if (!string.IsNullOrWhiteSpace(settlementRemainder))
			{
				stringBuilder.AppendLine(settlementRemainder);
			}
			stringBuilder.Append("你全部可转固定资产: ").Append(c.AllNpcAssets.Count).Append(" 项 | 一次结清指导总值: ").Append(PartyTransferProjectionOwner.CalculateSettlementTransferTotalValueForExternal(c.AllNpcAssets, isValid)).AppendLine(" 第纳尔");
			PartyTransferProjectionOwner.AppendSettlementTransferPromptSection(stringBuilder, "【玩家当前可手动交付固定资产（仅供手动交付参考）】：", c.PlayerAssets, showPromptIndex: true, isValid, assetId);
			string playerSettlementRemainder = c.PlayerRemainder;
			if (!string.IsNullOrWhiteSpace(playerSettlementRemainder))
			{
				stringBuilder.AppendLine(playerSettlementRemainder);
			}
			stringBuilder.Append("玩家全部可手动交付固定资产: ").Append(c.AllPlayerAssets.Count).Append(" 项 | 一次结清指导总值: ").Append(PartyTransferProjectionOwner.CalculateSettlementTransferTotalValueForExternal(c.AllPlayerAssets, isValid)).AppendLine(" 第纳尔");
			return stringBuilder.ToString().Trim();
    }
}
