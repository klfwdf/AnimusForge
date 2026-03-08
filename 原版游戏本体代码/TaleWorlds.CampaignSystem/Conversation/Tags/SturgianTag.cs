using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200027A RID: 634
	public class SturgianTag : ConversationTag
	{
		// Token: 0x170008C8 RID: 2248
		// (get) Token: 0x06002354 RID: 9044 RVA: 0x000990AD File Offset: 0x000972AD
		public override string StringId
		{
			get
			{
				return "SturgianTag";
			}
		}

		// Token: 0x06002355 RID: 9045 RVA: 0x000990B4 File Offset: 0x000972B4
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "sturgia";
		}

		// Token: 0x04000A87 RID: 2695
		public const string Id = "SturgianTag";
	}
}
