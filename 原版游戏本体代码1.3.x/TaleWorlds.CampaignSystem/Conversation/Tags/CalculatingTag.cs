using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000281 RID: 641
	public class CalculatingTag : ConversationTag
	{
		// Token: 0x170008CF RID: 2255
		// (get) Token: 0x06002369 RID: 9065 RVA: 0x000991E7 File Offset: 0x000973E7
		public override string StringId
		{
			get
			{
				return "CalculatingTag";
			}
		}

		// Token: 0x0600236A RID: 9066 RVA: 0x000991EE File Offset: 0x000973EE
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) > 0;
		}

		// Token: 0x04000A8E RID: 2702
		public const string Id = "CalculatingTag";
	}
}
