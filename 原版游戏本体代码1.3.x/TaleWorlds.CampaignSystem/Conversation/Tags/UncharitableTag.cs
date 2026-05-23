using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200025B RID: 603
	public class UncharitableTag : ConversationTag
	{
		// Token: 0x170008A9 RID: 2217
		// (get) Token: 0x060022F7 RID: 8951 RVA: 0x000989C0 File Offset: 0x00096BC0
		public override string StringId
		{
			get
			{
				return "UncharitableTag";
			}
		}

		// Token: 0x060022F8 RID: 8952 RVA: 0x000989C7 File Offset: 0x00096BC7
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetTraitLevel(DefaultTraits.Generosity) + character.GetTraitLevel(DefaultTraits.Mercy) < 0;
		}

		// Token: 0x04000A67 RID: 2663
		public const string Id = "UncharitableTag";
	}
}
