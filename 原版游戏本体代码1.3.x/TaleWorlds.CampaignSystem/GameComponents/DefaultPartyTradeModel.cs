using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000138 RID: 312
	public class DefaultPartyTradeModel : PartyTradeModel
	{
		// Token: 0x1700068E RID: 1678
		// (get) Token: 0x0600193D RID: 6461 RVA: 0x0007D0A5 File Offset: 0x0007B2A5
		public override int CaravanTransactionHighestValueItemCount
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x0600193E RID: 6462 RVA: 0x0007D0A8 File Offset: 0x0007B2A8
		public override float GetTradePenaltyFactor(MobileParty party)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TradePenaltyReduction, party, ref explainedNumber);
			return 1f / explainedNumber.ResultNumber;
		}
	}
}
