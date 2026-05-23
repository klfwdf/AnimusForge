using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200024B RID: 587
	public class PlayerIsMotherTag : ConversationTag
	{
		// Token: 0x17000899 RID: 2201
		// (get) Token: 0x060022C7 RID: 8903 RVA: 0x00098562 File Offset: 0x00096762
		public override string StringId
		{
			get
			{
				return "PlayerIsMotherTag";
			}
		}

		// Token: 0x060022C8 RID: 8904 RVA: 0x00098569 File Offset: 0x00096769
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.Mother == Hero.MainHero;
		}

		// Token: 0x04000A57 RID: 2647
		public const string Id = "PlayerIsMotherTag";
	}
}
