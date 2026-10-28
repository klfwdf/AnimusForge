using SandBox.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.Quests;

public class SaveTheCrewmenQuest : NavalStorylineQuestBase
{
	private const string QuestFinishMenuId = "save_the_crewmen_placeholder_menu";

	private bool _willProgressStoryline;

	public override string SpecialQuestType => "NavalStoryline";

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act1;

	public override bool WillProgressStoryline => _willProgressStoryline;

	protected override string MainPartyTemplateStringId => string.Empty;

	public override TextObject Title => new TextObject("{=tvGCC1BF}Save the Crewmen");

	private TextObject DescriptionLogText => new TextObject("{=PSjYdlCe}Rescue the merchant sailors who jumped overboard to escape the pirates.");

	public SaveTheCrewmenQuest(string questId, Hero questGiver)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		AddLog(DescriptionLogText);
		SetDialogs();
	}

	protected override void SetDialogs()
	{
		AddPlayerSavedCrewDialog();
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		AddGameMenus();
	}

	protected override void OnStartQuestInternal()
	{
		_willProgressStoryline = true;
		AddGameMenus();
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEventsInternal()
	{
	}

	private void AddPlayerSavedCrewDialog()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=kPB4vvTD}[ib:weary]Thank you. Heaven be praised. We thought we'd escaped the arrows only to be drowned by the waves. Heaven protect us all.", IsSavedCrew, IsMainAgent).Condition(() => IsSavedCrew(ConversationMission.OneToOneConversationAgent))
			.NpcLine("{=GVBtIsvA}Think nothing of it, lads. You'd have done the same for any of us, one sailor for another.", IsGunnar, IsSavedCrew)
			.NpcLine("{=zQRrXKQH}[ib:hip]So look, lads… Purig is still around, but I suspect he's overladen and undermanned. I doubt he can find us before nightfall, which is good, because I don't think we can outfight him. By my reckoning, we're still not far from Ostican. So row, my boys, for Ostican and safety!", IsGunnar, IsSavedCrew)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnCrewSaved;
			})
			.CloseDialog(), this);
	}

	private void OnCrewSaved()
	{
		(Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>()).FinalizeMission();
		Campaign.Current.GameMenuManager.SetNextMenu("save_the_crewmen_placeholder_menu");
	}

	private void CompleteQuest()
	{
		PlayerEncounter.Finish();
		CompleteQuestWithSuccess();
		for (int num = MobileParty.MainParty.Ships.Count - 1; num >= 0; num--)
		{
			MobileParty.MainParty.Ships[num].Owner = null;
		}
		Ship ship = new Ship(MBObjectManager.Instance.GetObject<ShipHull>("northern_trade_ship"));
		ChangeShipOwnerAction.ApplyByTransferring(PartyBase.MainParty, ship);
		if (GameStateManager.Current.ActiveState is MapState mapState)
		{
			mapState.Handler.TeleportCameraToMainParty();
		}
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act1CaptivitySucceeded);
	}

	private void AddGameMenus()
	{
		AddGameMenu("save_the_crewmen_placeholder_menu", new TextObject("{=!}TEMP"), naval_storyline_act_3_quest_1_setpiece_menu_on_init, GameMenu.MenuOverlayType.Encounter);
	}

	private void naval_storyline_act_3_quest_1_setpiece_menu_on_init(MenuCallbackArgs args)
	{
		CompleteQuest();
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool IsMainAgent(IAgent agent)
	{
		return agent == Agent.Main;
	}

	private bool IsSavedCrew(IAgent agent)
	{
		return (Mission.Current?.GetMissionBehavior<NavalStorylineCaptivityMissionController>())?.IsSavedCrew(agent) ?? false;
	}
}
