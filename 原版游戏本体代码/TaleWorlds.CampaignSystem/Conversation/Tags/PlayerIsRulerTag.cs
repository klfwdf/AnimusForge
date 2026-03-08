using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000252 RID: 594
	public class PlayerIsRulerTag : ConversationTag
	{
		// Token: 0x170008A0 RID: 2208
		// (get) Token: 0x060022DC RID: 8924 RVA: 0x00098777 File Offset: 0x00096977
		public override string StringId
		{
			get
			{
				return "PlayerIsRulerTag";
			}
		}

		// Token: 0x060022DD RID: 8925 RVA: 0x0009877E File Offset: 0x0009697E
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Hero.MainHero.Clan.Leader == Hero.MainHero;
		}

		// Token: 0x04000A5E RID: 2654
		public const string Id = "PlayerIsRulerTag";
	}
}
