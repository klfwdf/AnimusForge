namespace TaleWorlds.CampaignSystem.Conversation;

public static class CampaignMapConversation
{
	public static void OpenConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
		Campaign.Current.ConversationManager.OpenMapConversation(playerCharacterData, conversationPartnerData);
	}
}
