using System;
using System.Collections.Generic;
using System.Reflection;
using SandBox.View.Map;
using SandBox.ViewModelCollection.MapSiege;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Tutorial;

public class GauntletTutorialSystem : GlobalLayer
{
	public static GauntletTutorialSystem Current;

	private readonly Dictionary<string, TutorialItemBase> _mappedTutorialItems;

	private readonly Dictionary<TutorialItemBase, string> _tutorialItemIdentifiers;

	private CampaignTutorial _currentTutorial;

	private TutorialItemBase _currentTutorialVisualItem;

	private List<TutorialItemBase> _currentlyAvailableTutorialItems;

	private TutorialItemBase[] _currentlyAvailableTutorialItemsCopy;

	private TutorialVM _dataSource;

	private bool _isInitialized;

	private List<CampaignTutorial> _currentCampaignTutorials;

	private GauntletMovieIdentifier _movie;

	public EncyclopediaPages CurrentEncyclopediaPageContext { get; private set; }

	public bool IsCharacterPortraitPopupOpen { get; private set; }

	public TutorialContexts CurrentContext { get; private set; }

	public GauntletTutorialSystem()
	{
		_isInitialized = true;
		_dataSource = new TutorialVM(DisableTutorialStep);
		base.Layer = new GauntletLayer("TutorialScreen", 15300);
		GauntletLayer gauntletLayer = (GauntletLayer)base.Layer;
		_movie = gauntletLayer.LoadMovie("TutorialScreen", _dataSource);
		gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		ScreenManager.AddGlobalLayer(this, isFocusable: true);
		_mappedTutorialItems = new Dictionary<string, TutorialItemBase>();
		_tutorialItemIdentifiers = new Dictionary<TutorialItemBase, string>();
		_currentlyAvailableTutorialItems = new List<TutorialItemBase>();
		_currentlyAvailableTutorialItemsCopy = new TutorialItemBase[0];
		RegisterEvents();
		RegisterTutorialTypes();
		UpdateKeytexts();
		_currentCampaignTutorials = new List<CampaignTutorial>();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!_isInitialized)
		{
			return;
		}
		if (_currentlyAvailableTutorialItemsCopy.Length != _currentlyAvailableTutorialItems.Capacity)
		{
			_currentlyAvailableTutorialItemsCopy = new TutorialItemBase[_currentlyAvailableTutorialItems.Capacity];
		}
		_currentlyAvailableTutorialItems.CopyTo(_currentlyAvailableTutorialItemsCopy);
		int count = _currentlyAvailableTutorialItems.Count;
		if (_currentTutorial == null)
		{
			_currentCampaignTutorials.Clear();
			_currentlyAvailableTutorialItems.Clear();
			if (CampaignEventDispatcher.Instance != null)
			{
				CampaignEventDispatcher.Instance.CollectAvailableTutorials(ref _currentCampaignTutorials);
				foreach (CampaignTutorial currentCampaignTutorial in _currentCampaignTutorials)
				{
					if (_mappedTutorialItems.TryGetValue(currentCampaignTutorial.TutorialTypeId, out var value))
					{
						if (value.GetTutorialsRelevantContext() == CurrentContext)
						{
							_currentlyAvailableTutorialItems.Add(value);
						}
						if (_currentTutorial == null && value.GetTutorialsRelevantContext() == CurrentContext && value.IsConditionsMetForActivation())
						{
							SetCurrentTutorial(currentCampaignTutorial, value);
						}
					}
				}
			}
		}
		for (int i = 0; i < count; i++)
		{
			if (_currentlyAvailableTutorialItems.IndexOf(_currentlyAvailableTutorialItemsCopy[i]) < 0)
			{
				_currentlyAvailableTutorialItemsCopy[i].OnDeactivate();
			}
		}
		if (_currentlyAvailableTutorialItemsCopy.Length != _currentlyAvailableTutorialItems.Capacity)
		{
			_currentlyAvailableTutorialItemsCopy = new TutorialItemBase[_currentlyAvailableTutorialItems.Capacity];
		}
		else
		{
			_currentlyAvailableTutorialItemsCopy.Initialize();
		}
		_currentlyAvailableTutorialItems.CopyTo(_currentlyAvailableTutorialItemsCopy);
		for (int j = 0; j < _currentlyAvailableTutorialItems.Count; j++)
		{
			TutorialItemBase tutorialItemBase = _currentlyAvailableTutorialItemsCopy[j];
			if (!tutorialItemBase.IsConditionsMetForCompletion())
			{
				continue;
			}
			string text = _tutorialItemIdentifiers[tutorialItemBase];
			CampaignEventDispatcher.Instance.OnTutorialCompleted(text);
			_currentlyAvailableTutorialItems.Remove(tutorialItemBase);
			if (Mission.Current != null)
			{
				List<MissionBehavior> missionBehaviors = Mission.Current.MissionBehaviors;
				if (missionBehaviors != null)
				{
					for (int k = 0; k < missionBehaviors.Count; k++)
					{
						missionBehaviors[k]?.OnTutorialCompleted(text);
					}
				}
			}
			if (tutorialItemBase == _currentTutorialVisualItem)
			{
				ResetCurrentTutorial();
			}
			else
			{
				Debug.Print("Completed a non-active tutorial: " + text);
			}
		}
		_currentlyAvailableTutorialItemsCopy.Initialize();
		TutorialItemBase currentTutorialVisualItem = _currentTutorialVisualItem;
		if ((currentTutorialVisualItem != null && !currentTutorialVisualItem.IsConditionsMetForActivation()) || _currentTutorialVisualItem?.GetTutorialsRelevantContext() != CurrentContext)
		{
			ResetCurrentTutorial();
		}
		if (_currentTutorialVisualItem != null && _currentlyAvailableTutorialItems.IndexOf(_currentTutorialVisualItem) < 0)
		{
			ResetCurrentTutorial();
		}
		_dataSource.IsVisible = _currentTutorialVisualItem?.IsConditionsMetForVisibility() ?? false;
		_dataSource.Tick(dt);
	}

	private void SetCurrentTutorial(CampaignTutorial tutorial, TutorialItemBase tutorialItem)
	{
		_currentTutorial = tutorial;
		_currentTutorialVisualItem = tutorialItem;
		Game.Current.EventManager.TriggerEvent(new TutorialNotificationElementChangeEvent(_currentTutorialVisualItem.HighlightedVisualElementID));
		_dataSource.SetCurrentTutorial(tutorialItem.Placement, tutorial.TutorialTypeId, tutorialItem.MouseRequired);
		if (tutorialItem.MouseRequired)
		{
			base.Layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.MouseButtons);
		}
	}

	private void ResetCurrentTutorial()
	{
		_currentTutorial = null;
		_currentTutorialVisualItem = null;
		_dataSource?.CloseTutorialStep();
		Game.Current.EventManager.TriggerEvent(new TutorialNotificationElementChangeEvent(string.Empty));
		base.Layer?.InputRestrictions.ResetInputRestrictions();
	}

	private void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		CurrentContext = obj.NewContext;
		IsCharacterPortraitPopupOpen = false;
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnTutorialContextChanged(obj);
		});
	}

	private void DisableTutorialStep()
	{
		CampaignEventDispatcher.Instance.OnTutorialCompleted(_currentTutorial.TutorialTypeId);
		ResetCurrentTutorial();
	}

	public static void OnInitialize()
	{
		if (Current == null)
		{
			Current = new GauntletTutorialSystem();
		}
		_ = Current._isInitialized;
	}

	public static void OnUnload()
	{
		if (Current != null)
		{
			if (Current._isInitialized)
			{
				Current.UnregisterEvents();
				Current._isInitialized = false;
				TutorialVM.Instance = null;
				Current._dataSource = null;
				ScreenManager.RemoveGlobalLayer(Current);
				(Current.Layer as GauntletLayer).ReleaseMovie(Current._movie);
			}
			Current = null;
		}
	}

	private void OnEncyclopediaPageChanged(EncyclopediaPageChangedEvent obj)
	{
		CurrentEncyclopediaPageContext = obj.NewPage;
	}

	private void OnPerkSelectionToggle(PerkSelectionToggleEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPerkSelectionToggle(obj);
		});
	}

	private void OnInventoryTransferItem(InventoryTransferItemEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnInventoryTransferItem(obj);
		});
	}

	private void OnInventoryEquipmentTypeChange(InventoryEquipmentTypeChangedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnInventoryEquipmentTypeChange(obj);
		});
	}

	private void OnFocusAddedByPlayer(FocusAddedByPlayerEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnFocusAddedByPlayer(obj);
		});
	}

	private void OnPerkSelectedByPlayer(PerkSelectedByPlayerEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPerkSelectedByPlayer(obj);
		});
	}

	private void OnPartyAddedToArmyByPlayer(PartyAddedToArmyByPlayerEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPartyAddedToArmyByPlayer(obj);
		});
	}

	private void OnArmyCohesionByPlayerBoosted(ArmyCohesionBoostedByPlayerEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnArmyCohesionByPlayerBoosted(obj);
		});
	}

	private void OnInventoryFilterChanged(InventoryFilterChangedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnInventoryFilterChanged(obj);
		});
	}

	private void OnPlayerToggleTrackSettlementFromEncyclopedia(PlayerToggleTrackSettlementFromEncyclopediaEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerToggleTrackSettlementFromEncyclopedia(obj);
		});
	}

	private void OnMissionNameMarkerToggled(MissionNameMarkerToggleEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnMissionNameMarkerToggled(obj);
		});
	}

	private void OnPlayerStartEngineConstruction(PlayerStartEngineConstructionEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerStartEngineConstruction(obj);
		});
	}

	private void OnPlayerInspectedPartySpeed(PlayerInspectedPartySpeedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerInspectedPartySpeed(obj);
		});
	}

	private void OnGameMenuOpened(MenuCallbackArgs obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnGameMenuOpened(obj);
		});
	}

	private void OnCharacterPortraitPopUpOpened(CharacterObject obj)
	{
		IsCharacterPortraitPopupOpen = true;
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnCharacterPortraitPopUpOpened(obj);
		});
	}

	private void OnCharacterPortraitPopUpClosed()
	{
		IsCharacterPortraitPopupOpen = false;
	}

	private void OnPlayerStartTalkFromMenuOverlay(Hero obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerStartTalkFromMenuOverlay(obj);
		});
	}

	private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnGameMenuOptionSelected(gameMenuOption);
		});
	}

	private void OnPlayerStartRecruitment(CharacterObject obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerStartRecruitment(obj);
		});
	}

	private void OnNewCompanionAdded(Hero obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnNewCompanionAdded(obj);
		});
	}

	private void OnPlayerRecruitUnit(CharacterObject obj, int count)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerRecruitedUnit(obj, count);
		});
	}

	private void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerInventoryExchange(purchasedItems, soldItems, isTrading);
		});
	}

	private void OnPlayerUpgradeTroop(PlayerRequestUpgradeTroopEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerUpgradeTroop(obj.SourceTroop, obj.TargetTroop, obj.Number);
		});
	}

	private void OnPlayerMoveTroop(PlayerMoveTroopEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerMoveTroop(obj);
		});
	}

	private void OnPlayerToggledUpgradePopup(PlayerToggledUpgradePopupEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerToggledUpgradePopup(obj);
		});
	}

	private void OnOrderOfBattleHeroAssignedToFormation(OrderOfBattleHeroAssignedToFormationEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnOrderOfBattleHeroAssignedToFormation(obj);
		});
	}

	private void OnPlayerMovementFlagsChanged(MissionPlayerMovementFlagsChangeEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerMovementFlagChanged(obj);
		});
	}

	private void OnOrderOfBattleFormationClassChanged(OrderOfBattleFormationClassChangedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnOrderOfBattleFormationClassChanged(obj);
		});
	}

	private void OnOrderOfBattleFormationWeightChanged(OrderOfBattleFormationWeightChangedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnOrderOfBattleFormationWeightChanged(obj);
		});
	}

	private void OnCraftingWeaponClassSelectionOpened(CraftingWeaponClassSelectionOpenedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnCraftingWeaponClassSelectionOpened(obj);
		});
	}

	private void OnCraftingOnWeaponResultPopupOpened(CraftingWeaponResultPopupToggledEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnCraftingOnWeaponResultPopupOpened(obj);
		});
	}

	private void OnCraftingOrderTabOpened(CraftingOrderTabOpenedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnCraftingOrderTabOpened(obj);
		});
	}

	private void OnCraftingOrderSelectionOpened(CraftingOrderSelectionOpenedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnCraftingOrderSelectionOpened(obj);
		});
	}

	private void OnInventoryItemInspected(InventoryItemInspectedEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnInventoryItemInspected(obj);
		});
	}

	private void OnCrimeValueInspectedInSettlementOverlay(CrimeValueInspectedInSettlementOverlayEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnCrimeValueInspectedInSettlementOverlay(obj);
		});
	}

	private void OnClanRoleAssignedThroughClanScreen(ClanRoleAssignedThroughClanScreenEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnClanRoleAssignedThroughClanScreen(obj);
		});
	}

	private void OnMainMapCameraMove(MapScreen.MainMapCameraMoveEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnMainMapCameraMove(obj);
		});
	}

	private void OnPlayerSelectedAKingdomDecisionOption(PlayerSelectedAKingdomDecisionOptionEvent obj)
	{
		_currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
		{
			t.OnPlayerSelectedAKingdomDecisionOption(obj);
		});
	}

	private void OnResetAllTutorials(ResetAllTutorialsEvent obj)
	{
		_mappedTutorialItems.Clear();
		_tutorialItemIdentifiers.Clear();
		RegisterTutorialTypes();
	}

	private void OnGamepadActiveStateChanged()
	{
		UpdateKeytexts();
	}

	private void OnKeybindsChanged()
	{
		UpdateKeytexts();
	}

	private void RegisterTutorialTypes()
	{
		foreach (Assembly activeGameAssembly in ModuleHelper.GetActiveGameAssemblies())
		{
			Type[] types = activeGameAssembly.GetTypes();
			foreach (Type type in types)
			{
				if (!typeof(TutorialItemBase).IsAssignableFrom(type) || type.IsAbstract)
				{
					continue;
				}
				TutorialAttribute customAttribute = type.GetCustomAttribute<TutorialAttribute>();
				if (customAttribute == null)
				{
					Debug.FailedAssert("Tutorial: " + type.Name + " does not have a Tutorial attribute", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Tutorial\\GauntletTutorialSystem.cs", "RegisterTutorialTypes", 508);
					continue;
				}
				ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
				if (constructor == null)
				{
					Debug.FailedAssert("Tutorial: " + type.Name + " does not have a parameterless constructor", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Tutorial\\GauntletTutorialSystem.cs", "RegisterTutorialTypes", 516);
					continue;
				}
				TutorialItemBase tutorialItemBase = (TutorialItemBase)constructor.Invoke(new object[0]);
				string tutorialIdentifier = customAttribute.TutorialIdentifier;
				if (string.IsNullOrEmpty(tutorialIdentifier))
				{
					Debug.FailedAssert("Tutorial: " + type.Name + " does not have a valid identifier", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Tutorial\\GauntletTutorialSystem.cs", "RegisterTutorialTypes", 526);
					continue;
				}
				_mappedTutorialItems[tutorialIdentifier] = tutorialItemBase;
				_tutorialItemIdentifiers[tutorialItemBase] = tutorialIdentifier;
			}
		}
	}

	private void RegisterEvents()
	{
		Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		Game.Current.EventManager.RegisterEvent<InventoryTransferItemEvent>(OnInventoryTransferItem);
		Game.Current.EventManager.RegisterEvent<InventoryEquipmentTypeChangedEvent>(OnInventoryEquipmentTypeChange);
		Game.Current.EventManager.RegisterEvent<FocusAddedByPlayerEvent>(OnFocusAddedByPlayer);
		Game.Current.EventManager.RegisterEvent<PerkSelectedByPlayerEvent>(OnPerkSelectedByPlayer);
		Game.Current.EventManager.RegisterEvent<ArmyCohesionBoostedByPlayerEvent>(OnArmyCohesionByPlayerBoosted);
		Game.Current.EventManager.RegisterEvent<PartyAddedToArmyByPlayerEvent>(OnPartyAddedToArmyByPlayer);
		Game.Current.EventManager.RegisterEvent<InventoryFilterChangedEvent>(OnInventoryFilterChanged);
		Game.Current.EventManager.RegisterEvent<EncyclopediaPageChangedEvent>(OnEncyclopediaPageChanged);
		Game.Current.EventManager.RegisterEvent<PerkSelectionToggleEvent>(OnPerkSelectionToggle);
		Game.Current.EventManager.RegisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(OnPlayerToggleTrackSettlementFromEncyclopedia);
		Game.Current.EventManager.RegisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
		Game.Current.EventManager.RegisterEvent<MissionNameMarkerToggleEvent>(OnMissionNameMarkerToggled);
		Game.Current.EventManager.RegisterEvent<PlayerRequestUpgradeTroopEvent>(OnPlayerUpgradeTroop);
		Game.Current.EventManager.RegisterEvent<PlayerStartEngineConstructionEvent>(OnPlayerStartEngineConstruction);
		Game.Current.EventManager.RegisterEvent<PlayerInspectedPartySpeedEvent>(OnPlayerInspectedPartySpeed);
		Game.Current.EventManager.RegisterEvent<MapScreen.MainMapCameraMoveEvent>(OnMainMapCameraMove);
		Game.Current.EventManager.RegisterEvent<PlayerMoveTroopEvent>(OnPlayerMoveTroop);
		Game.Current.EventManager.RegisterEvent<MissionPlayerMovementFlagsChangeEvent>(OnPlayerMovementFlagsChanged);
		Game.Current.EventManager.RegisterEvent<ResetAllTutorialsEvent>(OnResetAllTutorials);
		Game.Current.EventManager.RegisterEvent<PlayerToggledUpgradePopupEvent>(OnPlayerToggledUpgradePopup);
		Game.Current.EventManager.RegisterEvent<OrderOfBattleHeroAssignedToFormationEvent>(OnOrderOfBattleHeroAssignedToFormation);
		Game.Current.EventManager.RegisterEvent<OrderOfBattleFormationClassChangedEvent>(OnOrderOfBattleFormationClassChanged);
		Game.Current.EventManager.RegisterEvent<OrderOfBattleFormationWeightChangedEvent>(OnOrderOfBattleFormationWeightChanged);
		Game.Current.EventManager.RegisterEvent<CraftingWeaponClassSelectionOpenedEvent>(OnCraftingWeaponClassSelectionOpened);
		Game.Current.EventManager.RegisterEvent<CraftingOrderTabOpenedEvent>(OnCraftingOrderTabOpened);
		Game.Current.EventManager.RegisterEvent<CraftingOrderSelectionOpenedEvent>(OnCraftingOrderSelectionOpened);
		Game.Current.EventManager.RegisterEvent<CraftingWeaponResultPopupToggledEvent>(OnCraftingOnWeaponResultPopupOpened);
		Game.Current.EventManager.RegisterEvent<InventoryItemInspectedEvent>(OnInventoryItemInspected);
		Game.Current.EventManager.RegisterEvent<CrimeValueInspectedInSettlementOverlayEvent>(OnCrimeValueInspectedInSettlementOverlay);
		Game.Current.EventManager.RegisterEvent<ClanRoleAssignedThroughClanScreenEvent>(OnClanRoleAssignedThroughClanScreen);
		Game.Current.EventManager.RegisterEvent<PlayerSelectedAKingdomDecisionOptionEvent>(OnPlayerSelectedAKingdomDecisionOption);
		HotKeyManager.OnKeybindsChanged += OnKeybindsChanged;
		if (Campaign.Current != null && CampaignEventDispatcher.Instance != null)
		{
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.CharacterPortraitPopUpOpenedEvent.AddNonSerializedListener(this, OnCharacterPortraitPopUpOpened);
			CampaignEvents.CharacterPortraitPopUpClosedEvent.AddNonSerializedListener(this, OnCharacterPortraitPopUpClosed);
			CampaignEvents.PlayerStartTalkFromMenu.AddNonSerializedListener(this, OnPlayerStartTalkFromMenuOverlay);
			CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, OnGameMenuOptionSelected);
			CampaignEvents.PlayerStartRecruitmentEvent.AddNonSerializedListener(this, OnPlayerStartRecruitment);
			CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
			CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, OnPlayerRecruitUnit);
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, OnPlayerInventoryExchange);
		}
	}

	private void UnregisterEvents()
	{
		Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		Game.Current?.EventManager.UnregisterEvent<InventoryTransferItemEvent>(OnInventoryTransferItem);
		Game.Current?.EventManager.UnregisterEvent<InventoryEquipmentTypeChangedEvent>(OnInventoryEquipmentTypeChange);
		Game.Current?.EventManager.UnregisterEvent<FocusAddedByPlayerEvent>(OnFocusAddedByPlayer);
		Game.Current?.EventManager.UnregisterEvent<PerkSelectedByPlayerEvent>(OnPerkSelectedByPlayer);
		Game.Current?.EventManager.UnregisterEvent<ArmyCohesionBoostedByPlayerEvent>(OnArmyCohesionByPlayerBoosted);
		Game.Current?.EventManager.UnregisterEvent<PartyAddedToArmyByPlayerEvent>(OnPartyAddedToArmyByPlayer);
		Game.Current?.EventManager.UnregisterEvent<InventoryFilterChangedEvent>(OnInventoryFilterChanged);
		Game.Current?.EventManager.UnregisterEvent<EncyclopediaPageChangedEvent>(OnEncyclopediaPageChanged);
		Game.Current?.EventManager.UnregisterEvent<PerkSelectionToggleEvent>(OnPerkSelectionToggle);
		Game.Current?.EventManager.UnregisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(OnPlayerToggleTrackSettlementFromEncyclopedia);
		Game.Current?.EventManager.UnregisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
		Game.Current?.EventManager.UnregisterEvent<MissionNameMarkerToggleEvent>(OnMissionNameMarkerToggled);
		Game.Current?.EventManager.UnregisterEvent<PlayerRequestUpgradeTroopEvent>(OnPlayerUpgradeTroop);
		Game.Current?.EventManager.UnregisterEvent<PlayerStartEngineConstructionEvent>(OnPlayerStartEngineConstruction);
		Game.Current?.EventManager.UnregisterEvent<PlayerInspectedPartySpeedEvent>(OnPlayerInspectedPartySpeed);
		Game.Current?.EventManager.UnregisterEvent<MapScreen.MainMapCameraMoveEvent>(OnMainMapCameraMove);
		Game.Current?.EventManager.UnregisterEvent<PlayerMoveTroopEvent>(OnPlayerMoveTroop);
		Game.Current?.EventManager.UnregisterEvent<MissionPlayerMovementFlagsChangeEvent>(OnPlayerMovementFlagsChanged);
		Game.Current?.EventManager.UnregisterEvent<ResetAllTutorialsEvent>(OnResetAllTutorials);
		Game.Current?.EventManager.UnregisterEvent<PlayerToggledUpgradePopupEvent>(OnPlayerToggledUpgradePopup);
		Game.Current?.EventManager.UnregisterEvent<OrderOfBattleHeroAssignedToFormationEvent>(OnOrderOfBattleHeroAssignedToFormation);
		Game.Current.EventManager.UnregisterEvent<OrderOfBattleFormationClassChangedEvent>(OnOrderOfBattleFormationClassChanged);
		Game.Current.EventManager.UnregisterEvent<OrderOfBattleFormationWeightChangedEvent>(OnOrderOfBattleFormationWeightChanged);
		Game.Current.EventManager.UnregisterEvent<CraftingWeaponClassSelectionOpenedEvent>(OnCraftingWeaponClassSelectionOpened);
		Game.Current.EventManager.UnregisterEvent<CraftingWeaponResultPopupToggledEvent>(OnCraftingOnWeaponResultPopupOpened);
		Game.Current.EventManager.UnregisterEvent<CraftingOrderTabOpenedEvent>(OnCraftingOrderTabOpened);
		Game.Current.EventManager.UnregisterEvent<CraftingOrderSelectionOpenedEvent>(OnCraftingOrderSelectionOpened);
		Game.Current.EventManager.UnregisterEvent<InventoryItemInspectedEvent>(OnInventoryItemInspected);
		Game.Current.EventManager.UnregisterEvent<CrimeValueInspectedInSettlementOverlayEvent>(OnCrimeValueInspectedInSettlementOverlay);
		Game.Current.EventManager.UnregisterEvent<ClanRoleAssignedThroughClanScreenEvent>(OnClanRoleAssignedThroughClanScreen);
		Game.Current.EventManager.UnregisterEvent<PlayerSelectedAKingdomDecisionOptionEvent>(OnPlayerSelectedAKingdomDecisionOption);
		HotKeyManager.OnKeybindsChanged -= OnKeybindsChanged;
		if (Campaign.Current != null && CampaignEventDispatcher.Instance != null)
		{
			CampaignEvents.GameMenuOpened.ClearListeners(this);
			CampaignEvents.CharacterPortraitPopUpOpenedEvent.ClearListeners(this);
			CampaignEvents.CharacterPortraitPopUpClosedEvent.ClearListeners(this);
			CampaignEvents.PlayerStartTalkFromMenu.ClearListeners(this);
			CampaignEvents.GameMenuOptionSelectedEvent.ClearListeners(this);
			CampaignEvents.PlayerStartRecruitmentEvent.ClearListeners(this);
			CampaignEvents.NewCompanionAdded.ClearListeners(this);
			CampaignEvents.OnUnitRecruitedEvent.ClearListeners(this);
			CampaignEvents.PlayerInventoryExchangeEvent.ClearListeners(this);
		}
	}

	private void UpdateKeytexts()
	{
		string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 5));
		GameTexts.SetVariable("MISSION_INDICATORS_KEY", keyHyperlinkText);
		string keyHyperlinkText2 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4));
		GameTexts.SetVariable("LEAVE_MISSION_KEY", keyHyperlinkText2);
		string keyHyperlinkText3 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 87));
		GameTexts.SetVariable("HOLD_OPEN_ORDER_KEY", keyHyperlinkText3);
		string keyHyperlinkText4 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 69));
		GameTexts.SetVariable("FIRST_ORDER_CATEGORY_KEY", keyHyperlinkText4);
		string keyHyperlinkText5 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 70));
		GameTexts.SetVariable("SECOND_ORDER_CATEGORY_KEY", keyHyperlinkText5);
		string keyHyperlinkText6 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 71));
		GameTexts.SetVariable("THIRD_ORDER_CATEGORY_KEY", keyHyperlinkText6);
		string keyHyperlinkText7 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 72));
		GameTexts.SetVariable("FOURTH_ORDER_CATEGORY_KEY", keyHyperlinkText7);
		string keyHyperlinkText8 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 79));
		GameTexts.SetVariable("FIRST_GROUP_HEAR_KEY", keyHyperlinkText8);
		string keyHyperlinkText9 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 80));
		GameTexts.SetVariable("SECOND_GROUP_HEAR_KEY", keyHyperlinkText9);
		string keyHyperlinkText10 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 88));
		GameTexts.SetVariable("SELECT_LEFT_FORMATION_KEY", keyHyperlinkText10);
		string keyHyperlinkText11 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 89));
		GameTexts.SetVariable("SELECT_RIGHT_FORMATION_KEY", keyHyperlinkText11);
		string keyHyperlinkText12 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 0));
		GameTexts.SetVariable("FORWARD_KEY", keyHyperlinkText12);
		string keyHyperlinkText13 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 1));
		GameTexts.SetVariable("BACKWARDS_KEY", keyHyperlinkText13);
		string keyHyperlinkText14 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 2));
		GameTexts.SetVariable("LEFT_KEY", keyHyperlinkText14);
		string keyHyperlinkText15 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 3));
		GameTexts.SetVariable("RIGHT_KEY", keyHyperlinkText15);
		string keyHyperlinkText16 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
		GameTexts.SetVariable("INTERACTION_KEY", keyHyperlinkText16);
		string keyHyperlinkText17 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", 57));
		GameTexts.SetVariable("MAP_ZOOM_OUT_KEY", keyHyperlinkText17);
		string keyHyperlinkText18 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", 56));
		GameTexts.SetVariable("MAP_ZOOM_IN_KEY", keyHyperlinkText18);
		string keyHyperlinkText19 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", "MapClick"));
		GameTexts.SetVariable("CONSOLE_ACTION_KEY", keyHyperlinkText19);
		string keyHyperlinkText20 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 30));
		GameTexts.SetVariable("WALK_MODE_KEY", keyHyperlinkText20);
		string keyHyperlinkText21 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 15));
		GameTexts.SetVariable("CROUCH_KEY", keyHyperlinkText21);
		string keyHyperlinkText22 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 90));
		GameTexts.SetVariable("APPLY_SELECTION_KEY", keyHyperlinkText22);
		GameTexts.SetVariable("CONSOLE_MOVEMENT_KEY", HyperlinkTexts.GetKeyHyperlinkText("ControllerLStick"));
		GameTexts.SetVariable("CONSOLE_CAMERA_KEY", HyperlinkTexts.GetKeyHyperlinkText("ControllerRStick"));
		GameTexts.SetVariable("UPGRADE_ICON", "{=!}<img src=\"PartyScreen\\upgrade_icon\" extend=\"5\">");
	}
}
