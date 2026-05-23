using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000262 RID: 610
	public class MetBeforeTag : ConversationTag
	{
		// Token: 0x170008B0 RID: 2224
		// (get) Token: 0x0600230C RID: 8972 RVA: 0x00098BB8 File Offset: 0x00096DB8
		public override string StringId
		{
			get
			{
				return "MetBeforeTag";
			}
		}

		// Token: 0x0600230D RID: 8973 RVA: 0x00098BBF File Offset: 0x00096DBF
		public override bool IsApplicableTo(CharacterObject character)
		{
			return !Campaign.Current.ConversationManager.CurrentConversationIsFirst;
		}

		// Token: 0x04000A6E RID: 2670
		public const string Id = "MetBeforeTag";
	}
}
