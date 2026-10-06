using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class AddHeroToPartyAction
{
	private static void ApplyInternal(Hero hero, MobileParty newParty, bool showNotification = true)
	{
		hero.PartyBelongedTo?.MemberRoster.AddToCounts(hero.CharacterObject, -1);
		hero.StayingInSettlement = null;
		_ = hero.IsNotable;
		if (hero.GovernorOf != null)
		{
			ChangeGovernorAction.RemoveGovernorOf(hero);
		}
		newParty.AddElementToMemberRoster(hero.CharacterObject, 1);
		CampaignEventDispatcher.Instance.OnHeroJoinedParty(hero, newParty);
		if (showNotification && newParty == MobileParty.MainParty && hero.IsPlayerCompanion)
		{
			TextObject textObject = GameTexts.FindText("str_companion_added");
			StringHelpers.SetCharacterProperties("COMPANION", hero.CharacterObject, textObject);
			MBInformationManager.AddQuickInformation(textObject);
		}
	}

	public static void Apply(Hero hero, MobileParty party, bool showNotification = true)
	{
		ApplyInternal(hero, party, showNotification);
	}
}
