using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class ReturnToBaseQuest : QuestBase
{
	private const string QuestFinishInvisibleMenuId = "return_to_base_placeholder";

	[SaveableField(0)]
	private bool _popupShown;

	public override string SpecialQuestType => "NavalStoryline";

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=B9l3S9qh}Return to {SETTLEMENT_NAME}");
			textObject.SetTextVariable("SETTLEMENT_NAME", NavalStorylineData.HomeSettlement.Name);
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => true;

	private TextObject _descriptionLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=vmWnfbJb}Sail back to {SETTLEMENT_LINK} and prepare for your next move.");
			textObject.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _successLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=NJcCXXu9}You have returned to {SETTLEMENT_LINK} and agreed to meet with Gunnar in the port after getting some much-needed rest.");
			textObject.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public ReturnToBaseQuest(string questId, Hero questGiver)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		AddLog(_descriptionLogText);
		AddTrackedObject(NavalStorylineData.HomeSettlement);
	}

	protected override void SetDialogs()
	{
	}

	protected override void InitializeQuestOnGameLoad()
	{
		AddGameMenus();
	}

	protected override void OnStartQuest()
	{
		AddGameMenus();
		_popupShown = NavalStorylineData.GetStorylineStage() < NavalStorylineData.NavalStorylineStage.Act2 || GetDistanceToOstican() < Campaign.Current.EstimatedAverageLordPartySpeed * 0.8f * (float)CampaignTime.HoursInDay;
		if (!_popupShown)
		{
			Campaign.Current.GameMenuManager.SetNextMenu("return_to_base_placeholder");
		}
	}

	private void ShowReturnPopUp()
	{
		TextObject textObject = new TextObject("{=VxcduBO7}Return to Ostican");
		TextObject textObject2 = new TextObject("{=g1ZFrb3E}Do you want to go to Ostican right away?");
		TextObject textObject3 = new TextObject("{=7Hj13O18}Yes, take me to Ostican");
		InformationManager.ShowInquiry(new InquiryData(negativeText: new TextObject("{=l3eSbQJM}No, I will go there myself").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, affirmativeText: textObject3.ToString(), affirmativeAction: FinishQuest, negativeAction: null), pauseGameActiveState: true);
		if (Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
		_popupShown = true;
	}

	private void AddGameMenus()
	{
		AddGameMenu("return_to_base_placeholder", new TextObject("{=!}TEMP"), return_to_ostican_menu_on_init, GameMenu.MenuOverlayType.Encounter);
	}

	private void return_to_ostican_menu_on_init(MenuCallbackArgs args)
	{
		ShowReturnPopUp();
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
	}

	private void Tick(float dt)
	{
		if (!_popupShown)
		{
			ShowReturnPopUp();
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.CurrentSettlement == NavalStorylineData.HomeSettlement && base.IsOngoing)
		{
			FinishQuest();
		}
		else if (PlayerEncounter.Current?.EncounterSettlementAux == NavalStorylineData.HomeSettlement && base.IsOngoing)
		{
			FinishQuest();
		}
	}

	private void FinishQuest()
	{
		CompleteQuestWithSuccess();
		NavalStorylineData.DeactivateNavalStoryline();
	}

	protected override void OnCompleteWithSuccess()
	{
		AddLog(_successLogText);
		if (!Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(ScourgeoftheSeasQuest)))
		{
			new ScourgeoftheSeasQuest().StartQuest();
		}
	}

	private float GetDistanceToOstican()
	{
		return MobileParty.MainParty.Position.Distance(NavalStorylineData.HomeSettlement.PortPosition);
	}
}
