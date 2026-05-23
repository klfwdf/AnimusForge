using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200027C RID: 636
	public class CruelTag : ConversationTag
	{
		// Token: 0x170008CA RID: 2250
		// (get) Token: 0x0600235A RID: 9050 RVA: 0x00099101 File Offset: 0x00097301
		public override string StringId
		{
			get
			{
				return "CruelTag";
			}
		}

		// Token: 0x0600235B RID: 9051 RVA: 0x00099108 File Offset: 0x00097308
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0;
		}

		// Token: 0x04000A89 RID: 2697
		public const string Id = "CruelTag";
	}
}
