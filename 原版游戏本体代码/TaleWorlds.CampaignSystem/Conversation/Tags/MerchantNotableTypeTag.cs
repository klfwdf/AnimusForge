using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200026F RID: 623
	public class MerchantNotableTypeTag : ConversationTag
	{
		// Token: 0x170008BD RID: 2237
		// (get) Token: 0x06002333 RID: 9011 RVA: 0x00098EB5 File Offset: 0x000970B5
		public override string StringId
		{
			get
			{
				return "MerchantNotableTypeTag";
			}
		}

		// Token: 0x06002334 RID: 9012 RVA: 0x00098EBC File Offset: 0x000970BC
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.Occupation == Occupation.Merchant;
		}

		// Token: 0x04000A7C RID: 2684
		public const string Id = "MerchantNotableTypeTag";
	}
}
