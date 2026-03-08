using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000267 RID: 615
	public class AttackingTag : ConversationTag
	{
		// Token: 0x170008B5 RID: 2229
		// (get) Token: 0x0600231B RID: 8987 RVA: 0x00098CF1 File Offset: 0x00096EF1
		public override string StringId
		{
			get
			{
				return "AttackingTag";
			}
		}

		// Token: 0x0600231C RID: 8988 RVA: 0x00098CF8 File Offset: 0x00096EF8
		public override bool IsApplicableTo(CharacterObject character)
		{
			return HeroHelper.WillLordAttack() || (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.SiegeEvent != null && Settlement.CurrentSettlement.Parties.Contains(Hero.MainHero.PartyBelongedTo));
		}

		// Token: 0x04000A74 RID: 2676
		public const string Id = "AttackingTag";
	}
}
