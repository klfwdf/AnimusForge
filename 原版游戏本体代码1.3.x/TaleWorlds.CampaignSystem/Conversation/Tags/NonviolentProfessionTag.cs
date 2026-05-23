using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000274 RID: 628
	public class NonviolentProfessionTag : ConversationTag
	{
		// Token: 0x170008C2 RID: 2242
		// (get) Token: 0x06002342 RID: 9026 RVA: 0x00098FB4 File Offset: 0x000971B4
		public override string StringId
		{
			get
			{
				return "NonviolentProfessionTag";
			}
		}

		// Token: 0x06002343 RID: 9027 RVA: 0x00098FBB File Offset: 0x000971BB
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && (character.Occupation == Occupation.Artisan || character.Occupation == Occupation.Merchant || character.Occupation == Occupation.Headman);
		}

		// Token: 0x04000A81 RID: 2689
		public const string Id = "NonviolentProfessionTag";
	}
}
