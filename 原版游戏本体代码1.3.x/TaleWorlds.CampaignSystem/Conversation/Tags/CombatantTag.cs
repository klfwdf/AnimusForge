using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200023D RID: 573
	public class CombatantTag : ConversationTag
	{
		// Token: 0x1700088B RID: 2187
		// (get) Token: 0x0600229D RID: 8861 RVA: 0x000981F6 File Offset: 0x000963F6
		public override string StringId
		{
			get
			{
				return "CombatantTag";
			}
		}

		// Token: 0x0600229E RID: 8862 RVA: 0x000981FD File Offset: 0x000963FD
		public override bool IsApplicableTo(CharacterObject character)
		{
			return !character.IsHero || !character.HeroObject.IsNoncombatant;
		}

		// Token: 0x04000A49 RID: 2633
		public const string Id = "CombatantTag";
	}
}
