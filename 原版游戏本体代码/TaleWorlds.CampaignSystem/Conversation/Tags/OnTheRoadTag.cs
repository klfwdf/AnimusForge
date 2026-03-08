using System;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000268 RID: 616
	public class OnTheRoadTag : ConversationTag
	{
		// Token: 0x170008B6 RID: 2230
		// (get) Token: 0x0600231E RID: 8990 RVA: 0x00098D39 File Offset: 0x00096F39
		public override string StringId
		{
			get
			{
				return "OnTheRoadTag";
			}
		}

		// Token: 0x0600231F RID: 8991 RVA: 0x00098D40 File Offset: 0x00096F40
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Settlement.CurrentSettlement == null;
		}

		// Token: 0x04000A75 RID: 2677
		public const string Id = "OnTheRoadTag";
	}
}
