using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000240 RID: 576
	public class OutlawSympathyTag : ConversationTag
	{
		// Token: 0x1700088E RID: 2190
		// (get) Token: 0x060022A6 RID: 8870 RVA: 0x0009833B File Offset: 0x0009653B
		public override string StringId
		{
			get
			{
				return "OutlawSympathyTag";
			}
		}

		// Token: 0x060022A7 RID: 8871 RVA: 0x00098342 File Offset: 0x00096542
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.IsWanderer && character.HeroObject.GetTraitLevel(DefaultTraits.RogueSkills) > 0;
		}

		// Token: 0x04000A4C RID: 2636
		public const string Id = "OutlawSympathyTag";
	}
}
