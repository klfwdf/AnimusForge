using Helpers;
using NavalDLC.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.Quests;

public class DefeatTheCaptorsQuest : NavalStorylineQuestBase
{
	private const string EnemyCharacterStringId = "sea_hound_captivity";

	private const string CrewCharacterStringId = "captivity_troops";

	private const float EncounterPositionX = 188f;

	private const float EncounterPositionY = 600f;

	private bool _willProgressStoryline = true;

	public override TextObject Title => new TextObject("{=pyPqiRwR}Break Free of Captivity");

	private TextObject _descriptionLogText => new TextObject("{=l315rexF}Defeat your captors, then free Gunnar and the others.");

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act1;

	public override bool WillProgressStoryline => _willProgressStoryline;

	protected override string MainPartyTemplateStringId => "storyline_act1_captivity_template";

	public DefeatTheCaptorsQuest(string questId)
		: base(questId, Hero.MainHero, CampaignTime.Never, 0)
	{
		SetDialogs();
		AddLog(_descriptionLogText);
	}

	protected override void SetDialogs()
	{
		AddAllyDialog();
		AddPlayerUnconsciousAllyDialog();
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		base.InitializeQuestOnGameLoadInternal();
		SetDialogs();
	}

	protected override void OnStartQuestInternal()
	{
		base.OnStartQuestInternal();
		_willProgressStoryline = false;
		TextObject name = new TextObject("{=ATA1PShK}Purig's Party");
		Clan randomElementInefficiently = Clan.BanditFactions.GetRandomElementInefficiently();
		MobileParty mobileParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(NavalStorylineData.HomeSettlement.GatePosition, 4f, NavalStorylineData.HomeSettlement, name, randomElementInefficiently, TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), null);
		ChangeShipOwnerAction.ApplyByMobilePartyCreation(ship: new Ship(MBObjectManager.Instance.GetObject<ShipHull>("nord_medium_ship")), newOwner: mobileParty.Party);
		CampaignVec2 sailAtPosition = new CampaignVec2(new Vec2(188f, 600f), isOnLand: false);
		mobileParty.SetSailAtPosition(sailAtPosition);
		MobileParty.MainParty.SetSailAtPosition(sailAtPosition);
		PlayerEncounter.RestartPlayerEncounter(mobileParty.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
		PlayerEncounter.StartBattle();
		GameMenu.ActivateGameMenu("defeat_the_captors_after_fight");
		StartMission();
	}

	public void StartMission()
	{
		CharacterObject enemyCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("sea_hound_captivity");
		CharacterObject crewCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("captivity_troops");
		NavalMissions.OpenNavalStorylineCaptivityMission(NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_1_phase_03"), NavalStorylineData.Gunnar.CharacterObject, enemyCharacter, crewCharacter);
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEventsInternal()
	{
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		GainRenownAction.Apply(Hero.MainHero, 5f);
	}

	private void AddAllyDialog()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=qtQXIguv}[ib:warrior][if:convo_happy]Well done, {PLAYER.NAME}! That's twice now you've gotten me out of a bad spot.").Condition(delegate
		{
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			NavalStorylineCaptivityMissionController navalStorylineCaptivityMissionController = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
			return Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && NavalStorylineData.Gunnar.HasMet && navalStorylineCaptivityMissionController != null && !navalStorylineCaptivityMissionController.WasPlayerKnockedOut;
		})
			.NpcLine("{=utFgkzhx}[ib:normal][if:convo_calm_friendly]Well… Normally I'd say we put as much distance between us and Purig as quickly as we can, but those merchants are still out there floundering in the waves. We can't leave them there. I can get the sail up. Take the steering oar. Let's see if we can  get them out of the water.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnDialogueEnded;
			})
			.CloseDialog(), this);
	}

	private void AddPlayerUnconsciousAllyDialog()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=nQJohWdO}[ib:warrior][if:convo_happy]Are you all right, {PLAYER.NAME}? Don't worry, the rest of us managed to break free and took care of those bastards.").Condition(delegate
		{
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			NavalStorylineCaptivityMissionController navalStorylineCaptivityMissionController = Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
			return Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && NavalStorylineData.Gunnar.HasMet && navalStorylineCaptivityMissionController != null && navalStorylineCaptivityMissionController.WasPlayerKnockedOut;
		})
			.NpcLine("{=evfMsY6h}[ib:normal][if:convo_calm_friendly]Well… Normally I'd say we put as much distance between us and Purig as quickly as we can, but those merchants are still out there floundering in the waves. We can't leave them there. I can get the sail up. Take the steering oar. Let's see if we can't get them out of the water.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnDialogueEnded;
			})
			.CloseDialog(), this);
	}

	private void OnDialogueEnded()
	{
		Mission.Current.GetMissionBehavior<NavalStorylineCaptivityMissionController>().OnShipCaptured();
		CompleteQuestWithSuccess();
		MobileParty.MainParty.MemberRoster.Clear();
		CharacterObject element = MBObjectManager.Instance.GetObject<CharacterObject>("captivity_troops");
		MobileParty.MainParty.AddElementToMemberRoster(element, 7);
		MobileParty.MainParty.AddElementToMemberRoster(Hero.MainHero.CharacterObject, 1, insertAtFront: true);
		MobileParty.MainParty.AddElementToMemberRoster(NavalStorylineData.Gunnar.CharacterObject, 1);
		MobileParty.MainParty.PartyComponent.ChangePartyLeader(Hero.MainHero);
		MobileParty.MainParty.IgnoreForHours(16f);
		new SaveTheCrewmenQuest("naval_storyline_save_the_crewmen_quest", NavalStorylineData.Gunnar).StartQuest();
	}
}
