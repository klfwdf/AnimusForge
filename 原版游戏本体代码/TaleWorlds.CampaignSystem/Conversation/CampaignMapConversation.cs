using System;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x0200022D RID: 557
	public static class CampaignMapConversation
	{
		// Token: 0x060021D9 RID: 8665 RVA: 0x00094C52 File Offset: 0x00092E52
		public static void OpenConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
			Campaign.Current.ConversationManager.OpenMapConversation(playerCharacterData, conversationPartnerData);
		}
	}
}
