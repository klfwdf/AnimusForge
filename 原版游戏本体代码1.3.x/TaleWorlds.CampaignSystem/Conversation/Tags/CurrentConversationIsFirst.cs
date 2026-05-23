using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000261 RID: 609
	public class CurrentConversationIsFirst : ConversationTag
	{
		// Token: 0x170008AF RID: 2223
		// (get) Token: 0x06002309 RID: 8969 RVA: 0x00098B98 File Offset: 0x00096D98
		public override string StringId
		{
			get
			{
				return "CurrentConversationIsFirst";
			}
		}

		// Token: 0x0600230A RID: 8970 RVA: 0x00098B9F File Offset: 0x00096D9F
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Campaign.Current.ConversationManager.CurrentConversationIsFirst;
		}

		// Token: 0x04000A6D RID: 2669
		public const string Id = "CurrentConversationIsFirst";
	}
}
