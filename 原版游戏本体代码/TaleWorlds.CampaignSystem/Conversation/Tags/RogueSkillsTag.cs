using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000288 RID: 648
	public class RogueSkillsTag : ConversationTag
	{
		// Token: 0x170008D6 RID: 2262
		// (get) Token: 0x0600237E RID: 9086 RVA: 0x000993A0 File Offset: 0x000975A0
		public override string StringId
		{
			get
			{
				return "RogueSkillsTag";
			}
		}

		// Token: 0x0600237F RID: 9087 RVA: 0x000993A7 File Offset: 0x000975A7
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.RogueSkills) > 0;
		}

		// Token: 0x04000A95 RID: 2709
		public const string Id = "RogueSkillsTag";
	}
}
