using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200027B RID: 635
	public class MercyTag : ConversationTag
	{
		// Token: 0x170008C9 RID: 2249
		// (get) Token: 0x06002357 RID: 9047 RVA: 0x000990D3 File Offset: 0x000972D3
		public override string StringId
		{
			get
			{
				return "MercyTag";
			}
		}

		// Token: 0x06002358 RID: 9048 RVA: 0x000990DA File Offset: 0x000972DA
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) > 0;
		}

		// Token: 0x04000A88 RID: 2696
		public const string Id = "MercyTag";
	}
}
