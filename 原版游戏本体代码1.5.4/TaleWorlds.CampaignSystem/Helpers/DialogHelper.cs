using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Helpers;

public static class DialogHelper
{
	public static void SetDialogString(string stringVariable, string gameTextId)
	{
		MBTextManager.SetTextVariable(stringVariable, Campaign.Current.ConversationManager.FindMatchingTextOrNull(gameTextId, CharacterObject.OneToOneConversationCharacter));
	}
}
