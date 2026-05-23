using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200025D RID: 605
	public class ChivalrousTag : ConversationTag
	{
		// Token: 0x170008AB RID: 2219
		// (get) Token: 0x060022FD RID: 8957 RVA: 0x00098A16 File Offset: 0x00096C16
		public override string StringId
		{
			get
			{
				return "ChivalrousTag";
			}
		}

		// Token: 0x060022FE RID: 8958 RVA: 0x00098A1D File Offset: 0x00096C1D
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetTraitLevel(DefaultTraits.Honor) + character.GetTraitLevel(DefaultTraits.Valor) > 0;
		}

		// Token: 0x04000A69 RID: 2665
		public const string Id = "ChivalrousTag";
	}
}
