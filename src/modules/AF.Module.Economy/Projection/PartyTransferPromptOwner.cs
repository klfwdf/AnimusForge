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
				stringBuilder.AppendLine("【当前招募限制】你当前对" + c.PlayerName + "的信任不足，任何士兵都不可开放招募。本轮不要展示【你当前可转移部队】清单；正文只口头拒绝或解释。");
			}
			else if (c.CanUseCounterpartyRoster && c.MaximumTier == int.MaxValue)
			{
				stringBuilder.AppendLine("【当前招募限制】你当前对" + c.PlayerName + "的信任已足够，所有已编号的士兵都可正常谈招募。若你最终明确同意放人，才算成交；正文只口头答应、拒绝或谈条件。");
			}
			else if (c.CanUseCounterpartyRoster)
			{
				stringBuilder.AppendLine("【当前招募限制】你当前只可向" + c.PlayerName + "开放 " + c.MaximumTier + " 阶及以下士兵的招募。只有本轮仍带编号的这些士兵才可供讨论；正文只口头答应、拒绝或谈条件。");
			}
			if (c.Notable)
			{
				int maximumRecruitableVolunteerIndex = c.MaximumRecruitableVolunteerIndex;
				if (maximumRecruitableVolunteerIndex >= 0)
				{
					stringBuilder.AppendLine("【原版要人募兵】你这里按原版城镇/村庄要人募兵规则实时计算；当前只开放原版允许的招募槽位（最高槽位索引 " + maximumRecruitableVolunteerIndex + "）。成交后对应槽位会立刻清空，之后仍由原版系统自然刷新。");
				}
				else
				{
					stringBuilder.AppendLine("【原版要人募兵】你这里按原版城镇/村庄要人募兵规则实时计算；由于关系、阵营或战况等原版条件，本轮没有向" + c.PlayerName + "开放任何招募槽位。正文只口头拒绝或解释。");
				}
			}
			if (!c.Wilderness)
			{
				stringBuilder.AppendLine("若你明确同意移交全部士兵，全量转移不受本段信任阶级和展示上限限制，但所有Hero都会留下；正文仍只自然答复。");
			}
			stringBuilder.AppendLine("日薪表示每名士兵每天需要支付多少第纳尔；雇佣价与购买价表示当前谈判指导单价。");
			stringBuilder.AppendLine("当前与你交易的人：" + c.PlayerName);
			if (c.MaximumTier > 0 || c.VolunteerCount > 0)
			{
				PartyTransferProjectionOwner.AppendPartyTransferPromptSection(stringBuilder, "【你当前可转移或可招募部队】：", c.Troops, isPrisoner: false, showPromptIndex: true, sourceLabel, typeLabel);
				string troopRemainder = c.TroopRemainder;
				if (!string.IsNullOrWhiteSpace(troopRemainder))
				{
					stringBuilder.AppendLine(troopRemainder);
				}
			}
			if (c.CanUseCounterpartyRoster && c.MaximumTier > 0 && c.MaximumTier < int.MaxValue)
			{
				PartyTransferProjectionOwner.AppendPartyTransferHiddenTroopSection(stringBuilder, "【由于你对" + c.PlayerName + "不够信任，你当前不可向" + c.PlayerName + "开放招募的更高阶部队】：", c.HiddenTroops, typeLabel, tier);
			}
			string troopTotalLabel = c.Wilderness ? "本轮可移交的全部非Hero士兵: " : "全部非Hero士兵: ";
			stringBuilder.Append(troopTotalLabel).Append(c.AllTroops.Sum((PartyTransferPromptEntry x) => Math.Max(0, x?.Count ?? 0))).Append(" 人 | 雇佣指导总值: ").Append(PartyTransferProjectionOwner.CalculatePartyTransferTotalValueForExternal(c.AllTroops, isPrisoner: false)).AppendLine(" 第纳尔");
			PartyTransferProjectionOwner.AppendPartyTransferPromptSection(stringBuilder, "【你当前可转移俘虏】：", c.Prisoners, isPrisoner: true, showPromptIndex: true, sourceLabel, typeLabel);
			string prisonerRemainder = c.PrisonerRemainder;
			if (!string.IsNullOrWhiteSpace(prisonerRemainder))
			{
				stringBuilder.AppendLine(prisonerRemainder);
			}
			stringBuilder.Append("全部俘虏: ").Append(c.AllPrisoners.Sum((PartyTransferPromptEntry x) => Math.Max(0, x?.Count ?? 0))).Append(" 人 | 购买指导总值: ").Append(PartyTransferProjectionOwner.CalculatePartyTransferTotalValueForExternal(c.AllPrisoners, isPrisoner: true)).AppendLine(" 第纳尔");
			// This c.PlayerName is injected into the NPC's main reply prompt. Action syntax belongs only
			// to the isolated postprocess prompt, so runtime/configuration values are sanitized here.
			return PartyTransferTagCodec.Strip(stringBuilder.ToString());
    }
}
