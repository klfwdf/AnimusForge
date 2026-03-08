using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200023C RID: 572
	public class NonCombatantTag : ConversationTag
	{
		// Token: 0x1700088A RID: 2186
		// (get) Token: 0x0600229A RID: 8858 RVA: 0x000981D0 File Offset: 0x000963D0
		public override string StringId
		{
			get
			{
				return "NonCombatantTag";
			}
		}

		// Token: 0x0600229B RID: 8859 RVA: 0x000981D7 File Offset: 0x000963D7
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.IsNoncombatant;
		}

		// Token: 0x04000A48 RID: 2632
		public const string Id = "NonCombatantTag";
	}
}
