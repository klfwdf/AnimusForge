using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200027D RID: 637
	public class GenerosityTag : ConversationTag
	{
		// Token: 0x170008CB RID: 2251
		// (get) Token: 0x0600235D RID: 9053 RVA: 0x0009912F File Offset: 0x0009732F
		public override string StringId
		{
			get
			{
				return "GenerosityTag";
			}
		}

		// Token: 0x0600235E RID: 9054 RVA: 0x00099136 File Offset: 0x00097336
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) > 0;
		}

		// Token: 0x04000A8A RID: 2698
		public const string Id = "GenerosityTag";
	}
}
