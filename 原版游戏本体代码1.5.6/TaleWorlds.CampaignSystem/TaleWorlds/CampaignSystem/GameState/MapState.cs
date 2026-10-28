using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.GameState;

public class MapState : TaleWorlds.Core.GameState
{
	private Incident _nextIncident;

	private MenuContext _menuContext;

	private bool _mapConversationActive;

	private bool _closeScreenNextFrame;

	private IMapStateHandler _handler;

	private BattleSimulation _battleSimulation;

	public Incident NextIncident
	{
		get
		{
			return _nextIncident;
		}
		set
		{
			_nextIncident = value;
		}
	}

	public MenuContext MenuContext
	{
		get
		{
			return _menuContext;
		}
		private set
		{
			_menuContext = value;
		}
	}

	public string GameMenuId
	{
		get
		{
			return Campaign.Current.MapStateData.GameMenuId;
		}
		set
		{
			Campaign.Current.MapStateData.GameMenuId = value;
		}
	}

	public bool AtMenu => MenuContext != null;

	public bool MapConversationActive => _mapConversationActive;

	public IMapStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public bool IsSimulationActive => _battleSimulation != null;

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		Handler?.OnIdleTick(dt);
	}

	private void RefreshHandler()
	{
		Handler?.OnRefreshState();
	}

	public void OnJoinArmy()
	{
		RefreshHandler();
	}

	public void OnLeaveArmy()
	{
		RefreshHandler();
	}

	public void OnDispersePlayerLeadedArmy()
	{
		RefreshHandler();
	}

	public void OnArmyCreated(MobileParty mobileParty)
	{
		RefreshHandler();
	}

	public void StartIncident(Incident incident)
	{
		Handler?.OnIncidentStarted(incident);
	}

	public void OnMainPartyEncounter()
	{
		Handler?.OnMainPartyEncounter();
	}

	public void ProcessTravel(CampaignVec2 moveTargetPoint)
	{
		MobileParty.MainParty.ForceAiNoPathMode = false;
		NavigationHelper.EmbarkDisembarkData embarkDisembarkData = NavigationHelper.EmbarkDisembarkData.Invalid;
		if (MobileParty.MainParty.HasNavalNavigationCapability)
		{
			Vec2 direction = (moveTargetPoint.ToVec2() - MobileParty.MainParty.Position.ToVec2()).Normalized();
			embarkDisembarkData = NavigationHelper.GetEmbarkAndDisembarkDataForPlayer(MobileParty.MainParty.Position, direction, moveTargetPoint, moveTargetPoint.IsOnLand);
			if (embarkDisembarkData.IsTargetingTheDeadZone)
			{
				moveTargetPoint = (MobileParty.MainParty.IsTransitionInProgress ? embarkDisembarkData.TransitionEndPosition : embarkDisembarkData.TransitionStartPosition);
			}
		}
		if (NavigationHelper.CanPlayerNavigateToPosition(moveTargetPoint, out var navigationType))
		{
			MobileParty.MainParty.SetMoveGoToPoint(moveTargetPoint, navigationType);
		}
		if (MobileParty.MainParty.HasNavalNavigationCapability && !embarkDisembarkData.IsTargetingTheDeadZone && navigationType == MobileParty.NavigationType.Naval && MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.IsTransitionInProgress)
		{
			MobileParty.MainParty.CancelNavigationTransition();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (Campaign.Current.SaveHandler.IsSaving)
		{
			Campaign.Current.SaveHandler.SaveTick();
			return;
		}
		if (_battleSimulation != null)
		{
			_battleSimulation.Tick(dt);
		}
		else if (AtMenu)
		{
			OnMenuModeTick(dt);
		}
		OnMapModeTick(dt);
		if (!Campaign.Current.SaveHandler.IsSaving)
		{
			Campaign.Current.SaveHandler.CampaignTick();
		}
	}

	private void OnMenuModeTick(float dt)
	{
		MenuContext.OnTick(dt);
		Handler?.OnMenuModeTick(dt);
	}

	private void OnMapModeTick(float dt)
	{
		if (_closeScreenNextFrame)
		{
			Game.Current.GameStateManager.CleanStates();
			return;
		}
		if (Handler != null)
		{
			Handler.BeforeTick(dt);
		}
		if (Campaign.Current != null && base.GameStateManager.ActiveState == this)
		{
			Campaign.Current.RealTick(dt);
			Handler?.Tick(dt);
			Handler?.AfterTick(dt);
			Campaign.Current.Tick();
			Handler?.AfterWaitTick(dt);
		}
	}

	public void OnLoadingFinished()
	{
		if (!string.IsNullOrEmpty(GameMenuId))
		{
			EnterMenuMode();
		}
		RefreshHandler();
		if (Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu != null && Campaign.Current.CurrentMenuContext.GameMenu.IsWaitMenu)
		{
			Campaign.Current.CurrentMenuContext.GameMenu.StartWait();
		}
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		_handler?.OnGameLoadFinished();
	}

	public void OnMapConversationStarts(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
		_mapConversationActive = true;
		_handler?.OnMapConversationStarts(playerCharacterData, conversationPartnerData);
	}

	public void OnMapConversationOver()
	{
		_handler?.OnMapConversationOver();
		_mapConversationActive = false;
		if (Game.Current.GameStateManager.ActiveState is MapState)
		{
			MenuContext?.Refresh();
		}
		RefreshHandler();
	}

	internal void OnSignalPeriodicEvents()
	{
		_handler?.OnSignalPeriodicEvents();
	}

	internal void OnHourlyTick()
	{
		_handler?.OnHourlyTick();
		MenuContext?.OnHourlyTick();
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			MenuContext?.Refresh();
		}
		RefreshHandler();
	}

	public void EnterMenuMode()
	{
		MenuContext = MBObjectManager.Instance.CreateObject<MenuContext>();
		_handler?.OnEnteringMenuMode(MenuContext);
		MenuContext.Refresh();
	}

	public void ExitMenuMode()
	{
		_handler?.OnExitingMenuMode();
		MenuContext.Destroy();
		MBObjectManager.Instance.UnregisterObject(MenuContext);
		MenuContext = null;
		GameMenuId = null;
	}

	public void StartBattleSimulation()
	{
		_battleSimulation = PlayerEncounter.Current.BattleSimulation;
		_handler?.OnBattleSimulationStarted(_battleSimulation);
	}

	public void EndBattleSimulation()
	{
		_battleSimulation = null;
		_handler?.OnBattleSimulationEnded();
	}

	public void OnPlayerSiegeActivated()
	{
		_handler?.OnPlayerSiegeActivated();
	}

	public void OnPlayerSiegeDeactivated()
	{
		_handler?.OnPlayerSiegeDeactivated();
	}

	public void OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
	{
		_handler?.OnSiegeEngineClick(siegeEngineFrame);
	}
}
