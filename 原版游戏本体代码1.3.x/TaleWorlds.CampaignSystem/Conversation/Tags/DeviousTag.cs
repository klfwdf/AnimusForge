using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000280 RID: 640
	public class DeviousTag : ConversationTag
	{
		// Token: 0x170008CE RID: 2254
		// (get) Token: 0x06002366 RID: 9062 RVA: 0x000991B9 File Offset: 0x000973B9
		public override string StringId
		{
			get
			{
				return "DeviousTag";
			}
		}

		// Token: 0x06002367 RID: 9063 RVA: 0x000991C0 File Offset: 0x000973C0
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Honor) < 0;
		}

		// Token: 0x04000A8D RID: 2701
		public const string Id = "DeviousTag";
	}
}
