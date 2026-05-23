using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200023B RID: 571
	public class NpcIsLiegeTag : ConversationTag
	{
		// Token: 0x17000889 RID: 2185
		// (get) Token: 0x06002297 RID: 8855 RVA: 0x000981AA File Offset: 0x000963AA
		public override string StringId
		{
			get
			{
				return "NpcIsLiegeTag";
			}
		}

		// Token: 0x06002298 RID: 8856 RVA: 0x000981B1 File Offset: 0x000963B1
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.IsKingdomLeader;
		}

		// Token: 0x04000A47 RID: 2631
		public const string Id = "NpcIsLiegeTag";
	}
}
