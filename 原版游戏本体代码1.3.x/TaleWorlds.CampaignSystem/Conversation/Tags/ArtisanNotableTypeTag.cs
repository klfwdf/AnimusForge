using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000270 RID: 624
	public class ArtisanNotableTypeTag : ConversationTag
	{
		// Token: 0x170008BE RID: 2238
		// (get) Token: 0x06002336 RID: 9014 RVA: 0x00098EDA File Offset: 0x000970DA
		public override string StringId
		{
			get
			{
				return "ArtisanNotableTypeTag";
			}
		}

		// Token: 0x06002337 RID: 9015 RVA: 0x00098EE1 File Offset: 0x000970E1
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.Occupation == Occupation.Artisan;
		}

		// Token: 0x04000A7D RID: 2685
		public const string Id = "ArtisanNotableTypeTag";
	}
}
