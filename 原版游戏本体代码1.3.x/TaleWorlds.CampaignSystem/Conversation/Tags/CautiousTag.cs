using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000284 RID: 644
	public class CautiousTag : ConversationTag
	{
		// Token: 0x170008D2 RID: 2258
		// (get) Token: 0x06002372 RID: 9074 RVA: 0x00099271 File Offset: 0x00097471
		public override string StringId
		{
			get
			{
				return "CautiousTag";
			}
		}

		// Token: 0x06002373 RID: 9075 RVA: 0x00099278 File Offset: 0x00097478
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Valor) < 0;
		}

		// Token: 0x04000A91 RID: 2705
		public const string Id = "CautiousTag";
	}
}
