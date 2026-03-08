using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200026B RID: 619
	public class FirstMeetingTag : ConversationTag
	{
		// Token: 0x170008B9 RID: 2233
		// (get) Token: 0x06002327 RID: 8999 RVA: 0x00098E26 File Offset: 0x00097026
		public override string StringId
		{
			get
			{
				return "FirstMeetingTag";
			}
		}

		// Token: 0x06002328 RID: 9000 RVA: 0x00098E2D File Offset: 0x0009702D
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Campaign.Current.ConversationManager.CurrentConversationIsFirst;
		}

		// Token: 0x04000A78 RID: 2680
		public const string Id = "FirstMeetingTag";
	}
}
