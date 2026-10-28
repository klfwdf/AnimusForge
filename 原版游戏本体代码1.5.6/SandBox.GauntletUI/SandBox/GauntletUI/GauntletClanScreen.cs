using Helpers;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(ClanState))]
public class GauntletClanScreen : ScreenBase, IGameStateListener
{
	protected GauntletLayer _gauntletLayer;

	protected SpriteCategory _clanCategory;

	protected readonly ClanState _clanState;

	protected bool _isCreatingPartyWithMembers;

	public ClanManagementVM _dataSource { get; private set; }

	public GauntletClanScreen(ClanState clanState)
	{
		_clanState = clanState;
	}

	protected virtual ClanManagementVM CreateDataSource()
	{
		return new ClanManagementVM(CloseClanScreen, ShowHeroOnMap, OpenPartyScreenForNewClanParty, OpenBannerEditorWithPlayerClan);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		InformationManager.HideAllMessages();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
		ClanManagementVM dataSource = _dataSource;
		ClanCardSelectionPopupVM cardSelectionPopup = _dataSource.CardSelectionPopup;
		dataSource.CanSwitchTabs = (cardSelectionPopup == null || !cardSelectionPopup.IsVisible) && (!Input.IsGamepadActive || (!InformationManager.GetIsAnyTooltipActiveAndExtended() && _gauntletLayer.IsHitThisFrame));
		ClanManagementVM dataSource2 = _dataSource;
		if (dataSource2 != null && dataSource2.CardSelectionPopup?.IsVisible == true)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				if (_dataSource.CardSelectionPopup.IsDoneEnabled)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					_dataSource.CardSelectionPopup.ExecuteDone();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.CardSelectionPopup.ExecuteCancel();
			}
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			if (IsRoleSelectionPopupActive())
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ClanParties.CurrentSelectedParty.IsRoleSelectionPopupVisible = false;
			}
			else
			{
				CloseClanScreen();
			}
		}
		else if (_gauntletLayer.Input.IsGameKeyPressed(41) || _gauntletLayer.Input.IsHotKeyReleased("Confirm"))
		{
			CloseClanScreen();
		}
		else if (_dataSource.CanSwitchTabs)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
			{
				UISoundsHelper.PlayUISound("event:/ui/tab");
				_dataSource.SelectPreviousCategory();
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
			{
				UISoundsHelper.PlayUISound("event:/ui/tab");
				_dataSource.SelectNextCategory();
			}
		}
	}

	protected bool IsRoleSelectionPopupActive()
	{
		ClanPartiesVM clanParties = _dataSource.ClanParties;
		if (clanParties.IsSelected && clanParties.IsAnyValidPartySelected)
		{
			return clanParties.CurrentSelectedParty.IsRoleSelectionPopupVisible;
		}
		return false;
	}

	protected void OpenPartyScreenForNewClanParty(Hero hero)
	{
		_isCreatingPartyWithMembers = true;
		PartyScreenHelper.OpenScreenAsCreateClanPartyForHero(hero);
	}

	protected void OpenBannerEditorWithPlayerClan()
	{
		Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>());
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
		_gauntletLayer = new GauntletLayer("ClanScreen", 1, shouldClear: true);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		AddLayer(_gauntletLayer);
		_dataSource = CreateDataSource();
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		_dataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		if (_isCreatingPartyWithMembers)
		{
			_dataSource.SelectParty(PartyBase.MainParty);
			_isCreatingPartyWithMembers = false;
		}
		else if (_clanState.InitialSelectedHero != null)
		{
			_dataSource.SelectHero(_clanState.InitialSelectedHero);
		}
		else if (_clanState.InitialSelectedParty != null)
		{
			_dataSource.SelectParty(_clanState.InitialSelectedParty);
			if (_clanState.InitialSelectedParty.LeaderHero == null)
			{
				ClanPartiesVM clanParties = _dataSource.ClanParties;
				if (clanParties != null && clanParties.CurrentSelectedParty?.IsChangeLeaderEnabled == true)
				{
					_dataSource.ClanParties.OnShowChangeLeaderPopup();
				}
			}
		}
		else if (_clanState.InitialSelectedSettlement != null)
		{
			_dataSource.SelectSettlement(_clanState.InitialSelectedSettlement);
		}
		else if (_clanState.InitialSelectedWorkshop != null)
		{
			_dataSource.SelectWorkshop(_clanState.InitialSelectedWorkshop);
		}
		else if (_clanState.InitialSelectedAlley != null)
		{
			_dataSource.SelectAlley(_clanState.InitialSelectedAlley);
		}
		_gauntletLayer.LoadMovie("ClanScreen", _dataSource);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.ClanScreen));
		UISoundsHelper.PlayUISound("event:/ui/panels/panel_clan_open");
		_gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
	}

	protected void ShowHeroOnMap(Hero hero)
	{
		CloseClanScreen();
		MapScreen.Instance.FastMoveCameraToPosition(hero.GetCampaignPosition());
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		RemoveLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
		_clanCategory.Unload();
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_dataSource?.RefreshCategoryValues();
		_dataSource?.UpdateBannerVisuals();
	}

	protected void CloseClanScreen()
	{
		Game.Current.GameStateManager.PopState();
		UISoundsHelper.PlayUISound("event:/ui/default");
	}
}
