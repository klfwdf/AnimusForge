using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000272 RID: 626
	public class WandererTag : ConversationTag
	{
		// Token: 0x170008C0 RID: 2240
		// (get) Token: 0x0600233C RID: 9020 RVA: 0x00098F25 File Offset: 0x00097125
		public override string StringId
		{
			get
			{
				return "WandererTag";
			}
		}

		// Token: 0x0600233D RID: 9021 RVA: 0x00098F2C File Offset: 0x0009712C
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.IsWanderer;
		}

		// Token: 0x04000A7F RID: 2687
		public const string Id = "WandererTag";
	}
}
