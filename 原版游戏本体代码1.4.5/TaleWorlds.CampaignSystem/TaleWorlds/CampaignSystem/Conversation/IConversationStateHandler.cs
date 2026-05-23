namespace TaleWorlds.CampaignSystem.Conversation;

public interface IConversationStateHandler
{
	void OnConversationInstall();

	void OnConversationUninstall();

	void OnConversationActivate();

	void OnConversationDeactivate();

	void OnConversationContinue();

	void ExecuteConversationContinue();
}
