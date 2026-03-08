using System;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000397 RID: 919
	public class MapState : GameState
	{
		// Token: 0x17000C7D RID: 3197
		// (get) Token: 0x060034A6 RID: 13478 RVA: 0x000D6279 File Offset: 0x000D4479
		// (set) Token: 0x060034A7 RID: 13479 RVA: 0x000D6281 File Offset: 0x000D4481
		public Incident NextIncident
		{
			get
			{
				return this._nextIncident;
			}
			set
			{
				this._nextIncident = value;
			}
		}

		// Token: 0x17000C7E RID: 3198
		// (get) Token: 0x060034A8 RID: 13480 RVA: 0x000D628A File Offset: 0x000D448A
		// (set) Token: 0x060034A9 RID: 13481 RVA: 0x000D6292 File Offset: 0x000D4492
		public MenuContext MenuContext
		{
			get
			{
				return this._menuContext;
			}
			private set
			{
				this._menuContext = value;
			}
		}

		// Token: 0x17000C7F RID: 3199
		// (get) Token: 0x060034AA RID: 13482 RVA: 0x000D629B File Offset: 0x000D449B
		// (set) Token: 0x060034AB RID: 13483 RVA: 0x000D62AC File Offset: 0x000D44AC
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

		// Token: 0x17000C80 RID: 3200
		// (get) Token: 0x060034AC RID: 13484 RVA: 0x000D62BE File Offset: 0x000D44BE
		public bool AtMenu
		{
			get
			{
				return this.MenuContext != null;
			}
		}

		// Token: 0x17000C81 RID: 3201
		// (get) Token: 0x060034AD RID: 13485 RVA: 0x000D62C9 File Offset: 0x000D44C9
		public bool MapConversationActive
		{
			get
			{
				return this._mapConversationActive;
			}
		}

		// Token: 0x17000C82 RID: 3202
		// (get) Token: 0x060034AE RID: 13486 RVA: 0x000D62D1 File Offset: 0x000D44D1
		// (set) Token: 0x060034AF RID: 13487 RVA: 0x000D62D9 File Offset: 0x000D44D9
		public IMapStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x17000C83 RID: 3203
		// (get) Token: 0x060034B0 RID: 13488 RVA: 0x000D62E2 File Offset: 0x000D44E2
		public bool IsSimulationActive
		{
			get
			{
				return this._battleSimulation != null;
			}
		}

		// Token: 0x060034B1 RID: 13489 RVA: 0x000D62ED File Offset: 0x000D44ED
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			IMapStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnIdleTick(dt);
		}

		// Token: 0x060034B2 RID: 13490 RVA: 0x000D6307 File Offset: 0x000D4507
		private void RefreshHandler()
		{
			IMapStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnRefreshState();
		}

		// Token: 0x060034B3 RID: 13491 RVA: 0x000D6319 File Offset: 0x000D4519
		public void OnJoinArmy()
		{
			this.RefreshHandler();
		}

		// Token: 0x060034B4 RID: 13492 RVA: 0x000D6321 File Offset: 0x000D4521
		public void OnLeaveArmy()
		{
			this.RefreshHandler();
		}

		// Token: 0x060034B5 RID: 13493 RVA: 0x000D6329 File Offset: 0x000D4529
		public void OnFadeInAndOut(float fadeOutTime, float blackTime, float fadeInTime)
		{
			IMapStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnFadeInAndOut(fadeOutTime, blackTime, fadeInTime);
		}

		// Token: 0x060034B6 RID: 13494 RVA: 0x000D633E File Offset: 0x000D453E
		public void OnDispersePlayerLeadedArmy()
		{
			this.RefreshHandler();
		}

		// Token: 0x060034B7 RID: 13495 RVA: 0x000D6346 File Offset: 0x000D4546
		public void OnArmyCreated(MobileParty mobileParty)
		{
			this.RefreshHandler();
		}

		// Token: 0x060034B8 RID: 13496 RVA: 0x000D634E File Offset: 0x000D454E
		public void StartIncident(Incident incident)
		{
			IMapStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnIncidentStarted(incident);
		}

		// Token: 0x060034B9 RID: 13497 RVA: 0x000D6361 File Offset: 0x000D4561
		public void OnMainPartyEncounter()
		{
			IMapStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnMainPartyEncounter();
		}

		// Token: 0x060034BA RID: 13498 RVA: 0x000D6374 File Offset: 0x000D4574
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
			MobileParty.NavigationType navigationType;
			if (NavigationHelper.CanPlayerNavigateToPosition(moveTargetPoint, out navigationType))
			{
				MobileParty.MainParty.SetMoveGoToPoint(moveTargetPoint, navigationType);
			}
			if (MobileParty.MainParty.HasNavalNavigationCapability && !embarkDisembarkData.IsTargetingTheDeadZone && navigationType == MobileParty.NavigationType.Naval && MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.IsTransitionInProgress)
			{
				MobileParty.MainParty.CancelNavigationTransition();
			}
		}

		// Token: 0x060034BB RID: 13499 RVA: 0x000D6454 File Offset: 0x000D4654
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			if (Campaign.Current.SaveHandler.IsSaving)
			{
				Campaign.Current.SaveHandler.SaveTick();
				return;
			}
			if (this._battleSimulation != null)
			{
				this._battleSimulation.Tick(dt);
			}
			else if (this.AtMenu)
			{
				this.OnMenuModeTick(dt);
			}
			this.OnMapModeTick(dt);
			if (!Campaign.Current.SaveHandler.IsSaving)
			{
				Campaign.Current.SaveHandler.CampaignTick();
			}
		}

		// Token: 0x060034BC RID: 13500 RVA: 0x000D64D5 File Offset: 0x000D46D5
		private void OnMenuModeTick(float dt)
		{
			this.MenuContext.OnTick(dt);
			IMapStateHandler handler = this.Handler;
			if (handler == null)
			{
				return;
			}
			handler.OnMenuModeTick(dt);
		}

		// Token: 0x060034BD RID: 13501 RVA: 0x000D64F4 File Offset: 0x000D46F4
		private void OnMapModeTick(float dt)
		{
			if (this._closeScreenNextFrame)
			{
				Game.Current.GameStateManager.CleanStates(0);
				return;
			}
			if (this.Handler != null)
			{
				this.Handler.BeforeTick(dt);
			}
			if (Campaign.Current != null && base.GameStateManager.ActiveState == this)
			{
				Campaign.Current.RealTick(dt);
				IMapStateHandler handler = this.Handler;
				if (handler != null)
				{
					handler.Tick(dt);
				}
				IMapStateHandler handler2 = this.Handler;
				if (handler2 != null)
				{
					handler2.AfterTick(dt);
				}
				Campaign.Current.Tick();
				IMapStateHandler handler3 = this.Handler;
				if (handler3 == null)
				{
					return;
				}
				handler3.AfterWaitTick(dt);
			}
		}

		// Token: 0x060034BE RID: 13502 RVA: 0x000D6590 File Offset: 0x000D4790
		public void OnLoadingFinished()
		{
			if (!string.IsNullOrEmpty(this.GameMenuId))
			{
				this.EnterMenuMode();
			}
			this.RefreshHandler();
			if (Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu != null && Campaign.Current.CurrentMenuContext.GameMenu.IsWaitMenu)
			{
				Campaign.Current.CurrentMenuContext.GameMenu.StartWait();
			}
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnGameLoadFinished();
		}

		// Token: 0x060034BF RID: 13503 RVA: 0x000D6618 File Offset: 0x000D4818
		public void OnMapConversationStarts(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
		{
			this._mapConversationActive = true;
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnMapConversationStarts(playerCharacterData, conversationPartnerData);
		}

		// Token: 0x060034C0 RID: 13504 RVA: 0x000D6634 File Offset: 0x000D4834
		public void OnMapConversationOver()
		{
			IMapStateHandler handler = this._handler;
			if (handler != null)
			{
				handler.OnMapConversationOver();
			}
			this._mapConversationActive = false;
			if (Game.Current.GameStateManager.ActiveState is MapState)
			{
				MenuContext menuContext = this.MenuContext;
				if (menuContext != null)
				{
					menuContext.Refresh();
				}
			}
			this.RefreshHandler();
		}

		// Token: 0x060034C1 RID: 13505 RVA: 0x000D6686 File Offset: 0x000D4886
		internal void OnSignalPeriodicEvents()
		{
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnSignalPeriodicEvents();
		}

		// Token: 0x060034C2 RID: 13506 RVA: 0x000D6698 File Offset: 0x000D4898
		internal void OnHourlyTick()
		{
			IMapStateHandler handler = this._handler;
			if (handler != null)
			{
				handler.OnHourlyTick();
			}
			MenuContext menuContext = this.MenuContext;
			if (menuContext == null)
			{
				return;
			}
			menuContext.OnHourlyTick();
		}

		// Token: 0x060034C3 RID: 13507 RVA: 0x000D66BB File Offset: 0x000D48BB
		protected override void OnActivate()
		{
			base.OnActivate();
			if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
			{
				MenuContext menuContext = this.MenuContext;
				if (menuContext != null)
				{
					menuContext.Refresh();
				}
			}
			this.RefreshHandler();
		}

		// Token: 0x060034C4 RID: 13508 RVA: 0x000D66EB File Offset: 0x000D48EB
		public void EnterMenuMode()
		{
			this.MenuContext = MBObjectManager.Instance.CreateObject<MenuContext>();
			IMapStateHandler handler = this._handler;
			if (handler != null)
			{
				handler.OnEnteringMenuMode(this.MenuContext);
			}
			this.MenuContext.Refresh();
		}

		// Token: 0x060034C5 RID: 13509 RVA: 0x000D671F File Offset: 0x000D491F
		public void ExitMenuMode()
		{
			IMapStateHandler handler = this._handler;
			if (handler != null)
			{
				handler.OnExitingMenuMode();
			}
			this.MenuContext.Destroy();
			MBObjectManager.Instance.UnregisterObject(this.MenuContext);
			this.MenuContext = null;
			this.GameMenuId = null;
		}

		// Token: 0x060034C6 RID: 13510 RVA: 0x000D675B File Offset: 0x000D495B
		public void StartBattleSimulation()
		{
			this._battleSimulation = PlayerEncounter.Current.BattleSimulation;
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnBattleSimulationStarted(this._battleSimulation);
		}

		// Token: 0x060034C7 RID: 13511 RVA: 0x000D6783 File Offset: 0x000D4983
		public void EndBattleSimulation()
		{
			this._battleSimulation = null;
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnBattleSimulationEnded();
		}

		// Token: 0x060034C8 RID: 13512 RVA: 0x000D679C File Offset: 0x000D499C
		public void OnPlayerSiegeActivated()
		{
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnPlayerSiegeActivated();
		}

		// Token: 0x060034C9 RID: 13513 RVA: 0x000D67AE File Offset: 0x000D49AE
		public void OnPlayerSiegeDeactivated()
		{
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnPlayerSiegeDeactivated();
		}

		// Token: 0x060034CA RID: 13514 RVA: 0x000D67C0 File Offset: 0x000D49C0
		public void OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
		{
			IMapStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnSiegeEngineClick(siegeEngineFrame);
		}

		// Token: 0x04000EFF RID: 3839
		private Incident _nextIncident;

		// Token: 0x04000F00 RID: 3840
		private MenuContext _menuContext;

		// Token: 0x04000F01 RID: 3841
		private bool _mapConversationActive;

		// Token: 0x04000F02 RID: 3842
		private bool _closeScreenNextFrame;

		// Token: 0x04000F03 RID: 3843
		private IMapStateHandler _handler;

		// Token: 0x04000F04 RID: 3844
		private BattleSimulation _battleSimulation;
	}
}
