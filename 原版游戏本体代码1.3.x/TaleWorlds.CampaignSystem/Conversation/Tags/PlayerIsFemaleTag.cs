using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000255 RID: 597
	public class PlayerIsFemaleTag : ConversationTag
	{
		// Token: 0x170008A3 RID: 2211
		// (get) Token: 0x060022E5 RID: 8933 RVA: 0x000987CF File Offset: 0x000969CF
		public override string StringId
		{
			get
			{
				return "PlayerIsFemaleTag";
			}
		}

		// Token: 0x060022E6 RID: 8934 RVA: 0x000987D6 File Offset: 0x000969D6
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Hero.MainHero.IsFemale;
		}

		// Token: 0x04000A61 RID: 2657
		public const string Id = "PlayerIsFemaleTag";
	}
}
