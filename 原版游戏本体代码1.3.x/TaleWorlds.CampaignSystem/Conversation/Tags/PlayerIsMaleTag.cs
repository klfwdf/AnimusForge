using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000256 RID: 598
	public class PlayerIsMaleTag : ConversationTag
	{
		// Token: 0x170008A4 RID: 2212
		// (get) Token: 0x060022E8 RID: 8936 RVA: 0x000987EA File Offset: 0x000969EA
		public override string StringId
		{
			get
			{
				return "PlayerIsMaleTag";
			}
		}

		// Token: 0x060022E9 RID: 8937 RVA: 0x000987F1 File Offset: 0x000969F1
		public override bool IsApplicableTo(CharacterObject character)
		{
			return !Hero.MainHero.IsFemale;
		}

		// Token: 0x04000A62 RID: 2658
		public const string Id = "PlayerIsMaleTag";
	}
}
