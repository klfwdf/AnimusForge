using System;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000273 RID: 627
	public class InHomeSettlementTag : ConversationTag
	{
		// Token: 0x170008C1 RID: 2241
		// (get) Token: 0x0600233F RID: 9023 RVA: 0x00098F4B File Offset: 0x0009714B
		public override string StringId
		{
			get
			{
				return "InHomeSettlementTag";
			}
		}

		// Token: 0x06002340 RID: 9024 RVA: 0x00098F54 File Offset: 0x00097154
		public override bool IsApplicableTo(CharacterObject character)
		{
			return (character.IsHero && Settlement.CurrentSettlement != null && character.HeroObject.HomeSettlement == Settlement.CurrentSettlement) || (character.IsHero && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.OwnerClan.Leader == character.HeroObject);
		}

		// Token: 0x04000A80 RID: 2688
		public const string Id = "InHomeSettlementTag";
	}
}
