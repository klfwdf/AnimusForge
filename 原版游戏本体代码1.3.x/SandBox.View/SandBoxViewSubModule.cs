using System;
using System.Collections.Generic;
using SandBox.View.Conversation;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.View.Missions.NameMarkers;
using SandBox.View.OrderProviders;
using SandBox.View.Overlay;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.ScreenSystem;

namespace SandBox.View
{
	// Token: 0x0200000A RID: 10
	public class SandBoxViewSubModule : MBSubModuleBase
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000036 RID: 54 RVA: 0x000034EF File Offset: 0x000016EF
		public static SandBoxViewVisualManager SandBoxViewVisualManager
		{
			get
			{
				return SandBoxViewSubModule._instance._sandBoxViewVisualManager;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000037 RID: 55 RVA: 0x000034FB File Offset: 0x000016FB
		public static ConversationViewManager ConversationViewManager
		{
			get
			{
				return SandBoxViewSubModule._instance._conversationViewManager;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000038 RID: 56 RVA: 0x00003507 File Offset: 0x00001707
		public static IMapConversationDataProvider MapConversationDataProvider
		{
			get
			{
				return SandBoxViewSubModule._instance._mapConversationDataProvider;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000039 RID: 57 RVA: 0x00003513 File Offset: 0x00001713
		internal static Dictionary<UIntPtr, MapEntityVisual> VisualsOfEntities
		{
			get
			{
				return SandBoxViewSubModule._instance._visualsOfEntities;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600003A RID: 58 RVA: 0x0000351F File Offset: 0x0000171F
		internal static Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>> FrameAndVisualOfEngines
		{
			get
			{
				return SandBoxViewSubModule._instance._frameAndVisualOfEngines;
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x0000352C File Offset: 0x0000172C
		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			SandBoxViewSubModule._instance = this;
			this.RegisterTooltipTypes();
			Module.CurrentModule.AddInitialStateOption(new InitialStateOption("CampaignResumeGame", new TextObject("{=6mN03uTP}Saved Games", null), 0, delegate()
			{
				ScreenManager.PushScreen(SandBoxViewCreator.CreateSaveLoadScreen(false));
			}, () => this.IsSavedGamesDisabled(), null, null));
			Module.CurrentModule.AddInitialStateOption(new InitialStateOption("ContinueCampaign", new TextObject("{=0tJ1oarX}Continue Campaign", null), 1, delegate()
			{
				this.ContinueCampaign(BannerlordConfig.LatestSaveGameName);
			}, () => this.IsContinueCampaignDisabled(BannerlordConfig.LatestSaveGameName), null, null));
			Module.CurrentModule.AddInitialStateOption(new InitialStateOption("SandBoxNewGame", new TextObject("{=171fTtIN}SandBox", null), 3, delegate()
			{
				this.StartGame();
			}, () => this.IsSandboxDisabled(), this._sandBoxAchievementsHint, null));
			SandBoxSaveHelper.OnStateChange += SandBoxViewSubModule.OnSaveHelperStateChange;
			Module.CurrentModule.ImguiProfilerTick += this.OnImguiProfilerTick;
			this._gameMenuOverlayProvider = new DefaultGameMenuOverlayProvider();
			GameMenuOverlayFactory.RegisterProvider(this._gameMenuOverlayProvider);
			MissionNameMarkerFactory.DefaultContext.AddProvider<DefaultMissionNameMarkerHandler>();
			MissionNameMarkerFactory.DefaultContext.AddProvider<StealthNameMarkerProvider>();
			this._mapConversationDataProvider = new DefaultMapConversationDataProvider();
			this._hideoutVisualOrderProvider = new HideoutVisualOrderProvider();
			VisualOrderFactory.RegisterProvider(this._hideoutVisualOrderProvider);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003684 File Offset: 0x00001884
		protected override void OnSubModuleUnloaded()
		{
			Module.CurrentModule.ImguiProfilerTick -= this.OnImguiProfilerTick;
			SandBoxSaveHelper.OnStateChange -= SandBoxViewSubModule.OnSaveHelperStateChange;
			GameMenuOverlayFactory.UnregisterProvider(this._gameMenuOverlayProvider);
			VisualOrderFactory.UnregisterProvider(this._hideoutVisualOrderProvider);
			this.UnregisterTooltipTypes();
			SandBoxViewSubModule._instance = null;
			base.OnSubModuleUnloaded();
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000036E0 File Offset: 0x000018E0
		protected override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			if (!this._isInitialized)
			{
				CampaignOptionsManager.Initialize();
				this._isInitialized = true;
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000036FD File Offset: 0x000018FD
		public override void OnCampaignStart(Game game, object starterObject)
		{
			base.OnCampaignStart(game, starterObject);
			if (Campaign.Current != null)
			{
				this._conversationViewManager = new ConversationViewManager();
				this._sandBoxViewVisualManager = new SandBoxViewVisualManager();
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003724 File Offset: 0x00001924
		public override void OnGameLoaded(Game game, object initializerObject)
		{
			this._conversationViewManager = new ConversationViewManager();
			this._sandBoxViewVisualManager = new SandBoxViewVisualManager();
		}

		// Token: 0x06000040 RID: 64 RVA: 0x0000373C File Offset: 0x0000193C
		public override void OnAfterGameInitializationFinished(Game game, object starterObject)
		{
			base.OnAfterGameInitializationFinished(game, starterObject);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003746 File Offset: 0x00001946
		public override void BeginGameStart(Game game)
		{
			base.BeginGameStart(game);
			if (Campaign.Current != null)
			{
				this._visualsOfEntities = new Dictionary<UIntPtr, MapEntityVisual>();
				this._frameAndVisualOfEngines = new Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>>();
				Campaign.Current.SaveHandler.MainHeroVisualSupplier = new MainHeroSaveVisualSupplier();
				ThumbnailCacheManager.InitializeSandboxValues();
			}
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003788 File Offset: 0x00001988
		public override void OnGameEnd(Game game)
		{
			if (this._visualsOfEntities != null)
			{
				foreach (MapEntityVisual mapEntityVisual in this._visualsOfEntities.Values)
				{
					mapEntityVisual.ReleaseResources();
				}
			}
			this._visualsOfEntities = null;
			this._frameAndVisualOfEngines = null;
			this._conversationViewManager = null;
			this._sandBoxViewVisualManager = null;
			if (Campaign.Current != null)
			{
				Campaign.Current.SaveHandler.MainHeroVisualSupplier = null;
				ThumbnailCacheManager.ReleaseSandboxValues();
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003820 File Offset: 0x00001A20
		private ValueTuple<bool, TextObject> IsSavedGamesDisabled()
		{
			if (Module.CurrentModule.IsOnlyCoreContentEnabled)
			{
				return new ValueTuple<bool, TextObject>(true, new TextObject("{=V8BXjyYq}Disabled during installation.", null));
			}
			if (MBSaveLoad.NumberOfCurrentSaves == 0)
			{
				return new ValueTuple<bool, TextObject>(true, new TextObject("{=XcVVE1mp}No saved games found.", null));
			}
			return new ValueTuple<bool, TextObject>(false, null);
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003860 File Offset: 0x00001A60
		private ValueTuple<bool, TextObject> IsContinueCampaignDisabled(string saveName)
		{
			if (Module.CurrentModule.IsOnlyCoreContentEnabled)
			{
				return new ValueTuple<bool, TextObject>(true, new TextObject("{=V8BXjyYq}Disabled during installation.", null));
			}
			if (string.IsNullOrEmpty(saveName))
			{
				return new ValueTuple<bool, TextObject>(true, new TextObject("{=aWMZQKXZ}Save the game at least once to continue", null));
			}
			SaveGameFileInfo saveFileWithName = MBSaveLoad.GetSaveFileWithName(saveName);
			if (saveFileWithName == null)
			{
				return new ValueTuple<bool, TextObject>(true, new TextObject("{=60LTq0tQ}Can't find the save file for the latest save game.", null));
			}
			TextObject item;
			return new ValueTuple<bool, TextObject>(SandBoxSaveHelper.GetIsDisabledWithReason(saveFileWithName, out item), item);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000038CF File Offset: 0x00001ACF
		private ValueTuple<bool, TextObject> IsSandboxDisabled()
		{
			if (Module.CurrentModule.IsOnlyCoreContentEnabled)
			{
				return new ValueTuple<bool, TextObject>(true, new TextObject("{=V8BXjyYq}Disabled during installation.", null));
			}
			return new ValueTuple<bool, TextObject>(false, null);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000038F6 File Offset: 0x00001AF6
		private void ContinueCampaign(string saveName)
		{
			SandBoxSaveHelper.TryLoadSave(MBSaveLoad.GetSaveFileWithName(saveName), new Action<LoadResult>(this.StartGame), null);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003910 File Offset: 0x00001B10
		public override void OnInitialState()
		{
			base.OnInitialState();
			if (Module.CurrentModule.StartupInfo.IsContinueGame && !this._latestSaveLoaded)
			{
				this._latestSaveLoaded = true;
				SaveGameFileInfo[] saveFiles = MBSaveLoad.GetSaveFiles(null);
				if (!saveFiles.IsEmpty<SaveGameFileInfo>())
				{
					string name = saveFiles.MaxBy((SaveGameFileInfo s) => s.MetaData.GetCreationTime()).Name;
					ValueTuple<bool, TextObject> valueTuple = this.IsContinueCampaignDisabled(name);
					if (!valueTuple.Item1)
					{
						this.ContinueCampaign(name);
						return;
					}
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=oZrVNUOk}Error", null).ToString(), valueTuple.Item2.ToString(), true, false, new TextObject("{=yS7PvrTD}OK", null).ToString(), string.Empty, null, null, "", 0f, null, null, null), false, false);
				}
			}
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000039EB File Offset: 0x00001BEB
		private void StartGame(LoadResult loadResult)
		{
			MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000039F8 File Offset: 0x00001BF8
		private void StartGame()
		{
			MBGameManager.StartNewGame(new SandBoxGameManager(() => new Campaign(CampaignGameMode.Campaign)));
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003A24 File Offset: 0x00001C24
		private void OnImguiProfilerTick()
		{
			if (Campaign.Current == null)
			{
				return;
			}
			List<MobileParty> all = MobileParty.All;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			List<EntityVisualManagerBase<PartyBase>> components = SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>();
			foreach (MobileParty mobileParty in all)
			{
				if (!mobileParty.IsMilitia && !mobileParty.IsGarrison)
				{
					if (mobileParty.IsVisible)
					{
						num++;
					}
					MapEntityVisual<PartyBase> mapEntityVisual = null;
					foreach (EntityVisualManagerBase<PartyBase> entityVisualManagerBase in components)
					{
						MapEntityVisual<PartyBase> visualOfEntity = entityVisualManagerBase.GetVisualOfEntity(PartyBase.MainParty);
						if (visualOfEntity != null)
						{
							mapEntityVisual = visualOfEntity;
						}
					}
					if (mapEntityVisual != null)
					{
						MobilePartyVisual mobilePartyVisual;
						if ((mobilePartyVisual = mapEntityVisual as MobilePartyVisual) != null)
						{
							if (mobilePartyVisual.HumanAgentVisuals != null)
							{
								num2++;
							}
							if (mobilePartyVisual.MountAgentVisuals != null)
							{
								num2++;
							}
							if (mobilePartyVisual.CaravanMountAgentVisuals != null)
							{
								num2++;
							}
						}
						num3++;
					}
				}
			}
			Imgui.BeginMainThreadScope();
			Imgui.Begin("Bannerlord Campaign Statistics");
			Imgui.Columns(2, "", true);
			Imgui.Text("Name");
			Imgui.NextColumn();
			Imgui.Text("Count");
			Imgui.NextColumn();
			Imgui.Separator();
			Imgui.Text("Total Mobile Party");
			Imgui.NextColumn();
			Imgui.Text(num3.ToString());
			Imgui.NextColumn();
			Imgui.Text("Visible Mobile Party");
			Imgui.NextColumn();
			Imgui.Text(num.ToString());
			Imgui.NextColumn();
			Imgui.Text("Total Agent Visuals");
			Imgui.NextColumn();
			Imgui.Text(num2.ToString());
			Imgui.NextColumn();
			Imgui.End();
			Imgui.EndMainThreadScope();
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003BE8 File Offset: 0x00001DE8
		private void RegisterTooltipTypes()
		{
			InformationManager.RegisterTooltip<List<MobileParty>, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshEncounterTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Track, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshTrackTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<MapEvent, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshMapEventTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<SiegeEvent, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshSiegeEventTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Army, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshArmyTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<MobileParty, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshMobilePartyTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Hero, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshHeroTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Settlement, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshSettlementTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<CharacterObject, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshCharacterTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<WeaponDesignElement, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshCraftingPartTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<InventoryLogic, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshInventoryTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<ItemObject, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshItemTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Building, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshBuildingTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Workshop, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshWorkshopTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Clan, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshClanTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<Kingdom, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshKingdomTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<MapMarker, PropertyBasedTooltipVM>(new Action<PropertyBasedTooltipVM, object[]>(TooltipRefresherCollection.RefreshMapMarkerTooltip), "PropertyBasedTooltip");
			InformationManager.RegisterTooltip<ExplainedNumber, RundownTooltipVM>(new Action<RundownTooltipVM, object[]>(TooltipRefresherCollection.RefreshExplainedNumberTooltip), "RundownTooltip");
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003D84 File Offset: 0x00001F84
		private void UnregisterTooltipTypes()
		{
			InformationManager.UnregisterTooltip<List<MobileParty>>();
			InformationManager.UnregisterTooltip<Track>();
			InformationManager.UnregisterTooltip<MapEvent>();
			InformationManager.UnregisterTooltip<Army>();
			InformationManager.UnregisterTooltip<MobileParty>();
			InformationManager.UnregisterTooltip<Hero>();
			InformationManager.UnregisterTooltip<Settlement>();
			InformationManager.UnregisterTooltip<CharacterObject>();
			InformationManager.UnregisterTooltip<WeaponDesignElement>();
			InformationManager.UnregisterTooltip<InventoryLogic>();
			InformationManager.UnregisterTooltip<ItemObject>();
			InformationManager.UnregisterTooltip<Building>();
			InformationManager.UnregisterTooltip<Workshop>();
			InformationManager.UnregisterTooltip<Clan>();
			InformationManager.UnregisterTooltip<Kingdom>();
			InformationManager.UnregisterTooltip<ExplainedNumber>();
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003DE1 File Offset: 0x00001FE1
		public static void SetMapConversationDataProvider(IMapConversationDataProvider mapConversationDataProvider)
		{
			SandBoxViewSubModule._instance._mapConversationDataProvider = mapConversationDataProvider;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003DEE File Offset: 0x00001FEE
		private static void OnSaveHelperStateChange(SandBoxSaveHelper.SaveHelperState currentState)
		{
			switch (currentState)
			{
			case SandBoxSaveHelper.SaveHelperState.Start:
			case SandBoxSaveHelper.SaveHelperState.LoadGame:
				LoadingWindow.EnableGlobalLoadingWindow();
				return;
			case SandBoxSaveHelper.SaveHelperState.Inquiry:
				LoadingWindow.DisableGlobalLoadingWindow();
				return;
			default:
				Debug.FailedAssert("Undefined save state for listener!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\SandBoxViewSubModule.cs", "OnSaveHelperStateChange", 679);
				return;
			}
		}

		// Token: 0x04000007 RID: 7
		private bool _latestSaveLoaded;

		// Token: 0x04000008 RID: 8
		private TextObject _sandBoxAchievementsHint = new TextObject("{=j09m7S2E}Achievements are disabled in SandBox mode!", null);

		// Token: 0x04000009 RID: 9
		private bool _isInitialized;

		// Token: 0x0400000A RID: 10
		private HideoutVisualOrderProvider _hideoutVisualOrderProvider;

		// Token: 0x0400000B RID: 11
		private ConversationViewManager _conversationViewManager;

		// Token: 0x0400000C RID: 12
		private SandBoxViewVisualManager _sandBoxViewVisualManager;

		// Token: 0x0400000D RID: 13
		private IMapConversationDataProvider _mapConversationDataProvider;

		// Token: 0x0400000E RID: 14
		private IGameMenuOverlayProvider _gameMenuOverlayProvider;

		// Token: 0x0400000F RID: 15
		private Dictionary<UIntPtr, MapEntityVisual> _visualsOfEntities;

		// Token: 0x04000010 RID: 16
		private Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>> _frameAndVisualOfEngines;

		// Token: 0x04000011 RID: 17
		private static SandBoxViewSubModule _instance;
	}
}
