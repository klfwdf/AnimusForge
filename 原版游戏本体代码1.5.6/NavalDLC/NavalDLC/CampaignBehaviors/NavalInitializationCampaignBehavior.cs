using NavalDLC.Storyline;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalInitializationCampaignBehavior : CampaignBehaviorBase
{
	private bool _hasIntroductionPopUpBeenShown;

	public override void RegisterEvents()
	{
		if (Campaign.Current.CurrentGame.GameType is CampaignStoryMode)
		{
			StoryModeEvents.OnStealthTutorialActivatedEvent.AddNonSerializedListener(this, OnStealthTutorialActivated);
		}
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
	}

	private void OnCharacterCreationIsOver(int index)
	{
		if (index == 1)
		{
			if (!(Campaign.Current.CurrentGame.GameType is CampaignStoryMode) && !_hasIntroductionPopUpBeenShown)
			{
				TextObject textObject = new TextObject("{=SJT8Nl5a}Call of the Oceans");
				TextObject textObject2 = new TextObject("{=XcaoQSjv}Often, when you were growing up, you wondered if your destiny might lie upon the sea. You listened closely to old sailors telling their tales of daring and peril: steering longships through the icy storms of the north, outfoxing corsairs along the pirate-infested coasts of the south, or standing in the forecastle of a dromon as it crashed through the enemy battle-line. You wonder what opportunities lie for you to seize on the seas of Calradia: a fortune made in the commerce and intrigues of a bustling port, or glory won on the bloodied deck of a foe's flagship.");
				InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: null, negativeAction: null));
			}
			Hero.MainHero.HeroDeveloper.UnspentFocusPoints += 6;
		}
	}

	private void OnStealthTutorialActivated()
	{
		ShowIntroductionPopUp();
	}

	private void ShowIntroductionPopUp()
	{
		_hasIntroductionPopUpBeenShown = true;
		TextObject textObject = new TextObject("{=F6qA5Mmo}Troubled Waters");
		TextObject textObject2 = new TextObject("{=Iq2YN7o3}Throughout your travels, you overheard hushed conversations about a new menace, a pirate confederacy terrorizing the coasts of Calradia. These northern corsairs have built a reputation for trading in captives from bandits and raiders such as the ones who attacked your family. Do you want to go to Ostican now to try to pick up your sister's trail and embark on an hunt across the seas of Calradia?");
		TextObject textObject3 = new TextObject("{=0aD2pdmB}Take me to Ostican now");
		InformationManager.ShowInquiry(new InquiryData(negativeText: new TextObject("{=fRRkHsZR}I'll go there myself").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, affirmativeText: textObject3.ToString(), affirmativeAction: OnStorylinePopUpAccepted, negativeAction: OnStorylinePopUpDeclined), pauseGameActiveState: true);
	}

	private void OnStorylinePopUpAccepted()
	{
		NavalStorylineData.StartNavalStoryline();
		NavalStorylineData.TeleportMainPartyBackToBase();
	}

	private void OnStorylinePopUpDeclined()
	{
		NavalStorylineData.StartNavalStoryline();
	}

	private void OnAfterSessionLaunched(CampaignGameStarter gameStarter)
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.12")))
		{
			return;
		}
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			allAliveHero.HeroDeveloper.UnspentFocusPoints += 6;
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_hasIntroductionPopUpBeenShown", ref _hasIntroductionPopUpBeenShown);
	}
}
