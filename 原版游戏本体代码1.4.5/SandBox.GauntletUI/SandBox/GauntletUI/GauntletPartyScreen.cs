using System;
using SandBox.View;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
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

[GameStateScreen(typeof(PartyState))]
public class GauntletPartyScreen : ScreenBase, IGameStateListener, IChangeableScreen, IPartyScreenLogicHandler, IPartyScreenPrisonHandler, IPartyScreenTroopHandler
{
	private PartyVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _partyscreenCategory;

	private readonly PartyState _partyState;

	public bool IsTroopUpgradesDisabled
	{
		get
		{
			PartyVM dataSource = _dataSource;
			if (dataSource == null)
			{
				return false;
			}
			return dataSource.PartyScreenLogic?.IsTroopUpgradesDisabled == true;
		}
	}

	public GauntletPartyScreen(PartyState partyState)
	{
		partyState.Handler = this;
		_partyState = partyState;
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
		_dataSource.IsFiveStackModifierActive = _gauntletLayer.Input.IsHotKeyDown("FiveStackModifier");
		_dataSource.IsEntireStackModifierActive = _gauntletLayer.Input.IsHotKeyDown("EntireStackModifier");
		if (!_partyState.IsActive || _gauntletLayer.Input.IsHotKeyReleased("Exit") || (!_gauntletLayer.Input.IsControlDown() && _gauntletLayer.Input.IsGameKeyReleased(43)))
		{
			HandleCancelInput();
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
		{
			HandleDoneInput();
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Reset"))
		{
			HandleResetInput();
		}
		else if (!_dataSource.IsAnyPopUpOpen)
		{
			if (_gauntletLayer.Input.IsHotKeyPressed("TakeAllTroops"))
			{
				if (_dataSource.IsOtherTroopsHaveTransferableTroops)
				{
					UISoundsHelper.PlayUISound("event:/ui/inventory/take_all");
					_dataSource.ExecuteTransferAllOtherTroops();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("GiveAllTroops"))
			{
				if (_dataSource.IsMainTroopsHaveTransferableTroops)
				{
					UISoundsHelper.PlayUISound("event:/ui/inventory/take_all");
					_dataSource.ExecuteTransferAllMainTroops();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("TakeAllPrisoners"))
			{
				if (_dataSource.CurrentFocusedCharacter != null && Input.IsGamepadActive)
				{
					if (_dataSource.CurrentFocusedCharacter.IsTroopTransferrable && _dataSource.CurrentFocusedCharacter.Side == PartyScreenLogic.PartyRosterSide.Left)
					{
						_dataSource.CurrentFocusedCharacter.ExecuteTransferSingle();
						UISoundsHelper.PlayUISound("event:/ui/transfer");
					}
				}
				else if (_dataSource.IsOtherPrisonersHaveTransferableTroops)
				{
					UISoundsHelper.PlayUISound("event:/ui/inventory/take_all");
					_dataSource.ExecuteTransferAllOtherPrisoners();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("GiveAllPrisoners"))
			{
				if (_dataSource.CurrentFocusedCharacter != null && Input.IsGamepadActive)
				{
					if (_dataSource.CurrentFocusedCharacter.IsTroopTransferrable && _dataSource.CurrentFocusedCharacter.Side == PartyScreenLogic.PartyRosterSide.Right)
					{
						_dataSource.CurrentFocusedCharacter.ExecuteTransferSingle();
						UISoundsHelper.PlayUISound("event:/ui/transfer");
					}
				}
				else if (_dataSource.IsMainPrisonersHaveTransferableTroops)
				{
					UISoundsHelper.PlayUISound("event:/ui/inventory/take_all");
					_dataSource.ExecuteTransferAllMainPrisoners();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("OpenUpgradePopup"))
			{
				if (!_dataSource.IsUpgradePopUpDisabled)
				{
					_dataSource.ExecuteOpenUpgradePopUp();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("OpenRecruitPopup"))
			{
				if (!_dataSource.IsRecruitPopUpDisabled)
				{
					_dataSource.ExecuteOpenRecruitPopUp();
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
			}
			else if (_gauntletLayer.Input.IsGameKeyReleased(39) && _dataSource.CurrentFocusedCharacter != null && Input.IsGamepadActive)
			{
				_dataSource.CurrentFocusedCharacter.ExecuteOpenTroopEncyclopedia();
			}
		}
		else
		{
			if (!Input.IsGamepadActive)
			{
				return;
			}
			if (_gauntletLayer.Input.IsHotKeyPressed("PopupItemPrimaryAction"))
			{
				if (_dataSource.UpgradePopUp.IsOpen && _dataSource.UpgradePopUp.IsPrimaryActionAvailable)
				{
					UISoundsHelper.PlayUISound("event:/ui/party/upgrade");
					_dataSource.UpgradePopUp.ExecuteItemPrimaryAction();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("PopupItemSecondaryAction"))
			{
				if (_dataSource.UpgradePopUp.IsOpen)
				{
					if (_dataSource.UpgradePopUp.IsSecondaryActionAvailable)
					{
						UISoundsHelper.PlayUISound("event:/ui/party/upgrade");
						_dataSource.UpgradePopUp.ExecuteItemSecondaryAction();
					}
				}
				else if (_dataSource.RecruitPopUp.IsOpen)
				{
					PartyTroopManagerItemVM focusedTroop = _dataSource.RecruitPopUp.FocusedTroop;
					if (focusedTroop != null && focusedTroop.PartyCharacter.IsTroopRecruitable)
					{
						UISoundsHelper.PlayUISound("event:/ui/party/recruit_prisoner");
						_dataSource.RecruitPopUp.ExecuteItemPrimaryAction();
					}
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("GiveAllTroops"))
			{
				if (_dataSource.UpgradePopUp.IsOpen && _dataSource.UpgradePopUp.IsTertiaryActionAvailable)
				{
					UISoundsHelper.PlayUISound("event:/ui/party/upgrade");
					_dataSource.UpgradePopUp.ExecuteItemTertiaryAction();
				}
			}
			else
			{
				if (!_gauntletLayer.Input.IsGameKeyReleased(39))
				{
					return;
				}
				if (_dataSource.RecruitPopUp.IsOpen && _dataSource.RecruitPopUp.FocusedTroop != null)
				{
					_dataSource.RecruitPopUp.FocusedTroop.PartyCharacter.ExecuteOpenTroopEncyclopedia();
				}
				else if (_dataSource.UpgradePopUp.IsOpen)
				{
					if (_dataSource.UpgradePopUp.FocusedTroop != null)
					{
						_dataSource.UpgradePopUp.FocusedTroop.ExecuteOpenTroopEncyclopedia();
					}
					else if (_dataSource.CurrentFocusedUpgrade != null)
					{
						_dataSource.CurrentFocusedUpgrade.ExecuteUpgradeEncyclopediaLink();
					}
				}
			}
		}
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_partyscreenCategory = UIResourceManager.LoadSpriteCategory("ui_partyscreen");
		_gauntletLayer = new GauntletLayer("PartyScreen", 1, shouldClear: true);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("PartyHotKeyCategory"));
		_dataSource = new PartyVM(_partyState.PartyScreenLogic);
		_dataSource.SetGetKeyTextFromKeyIDFunc(Game.Current.GameTextManager.GetHotKeyGameTextFromKeyID);
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetTakeAllTroopsInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("TakeAllTroops"));
		_dataSource.SetDismissAllTroopsInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("GiveAllTroops"));
		_dataSource.SetTakeAllPrisonersInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("TakeAllPrisoners"));
		_dataSource.SetDismissAllPrisonersInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("GiveAllPrisoners"));
		_dataSource.SetOpenUpgradePanelInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("OpenUpgradePopup"));
		_dataSource.SetOpenRecruitPanelInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("OpenRecruitPopup"));
		_dataSource.UpgradePopUp.SetPrimaryActionInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("PopupItemPrimaryAction"));
		_dataSource.UpgradePopUp.SetSecondaryActionInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("PopupItemSecondaryAction"));
		_dataSource.UpgradePopUp.SetTertiaryActionInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("GiveAllTroops"));
		_dataSource.RecruitPopUp.SetPrimaryActionInputKey(HotKeyManager.GetCategory("PartyHotKeyCategory").GetHotKey("PopupItemSecondaryAction"));
		_gauntletLayer.LoadMovie("PartyScreen", _dataSource);
		AddLayer(_gauntletLayer);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.PartyScreen));
		UISoundsHelper.PlayUISound("event:/ui/panels/panel_party_open");
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		_gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		PartyBase.MainParty.SetVisualAsDirty();
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		_gauntletLayer.IsFocusLayer = false;
		_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		RemoveLayer(_gauntletLayer);
		ScreenManager.TryLoseFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
		if (Campaign.Current.ConversationManager.IsConversationInProgress && !Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			Campaign.Current.ConversationManager.OnConversationActivate();
		}
	}

	void IGameStateListener.OnInitialize()
	{
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
	}

	void IGameStateListener.OnFinalize()
	{
		CampaignEvents.CompanionRemoved.ClearListeners(this);
		_dataSource.OnFinalize();
		_partyscreenCategory.Unload();
		_dataSource = null;
		_gauntletLayer = null;
	}

	void IPartyScreenPrisonHandler.ExecuteTakeAllPrisonersScript()
	{
		_dataSource.ExecuteTransferAllOtherPrisoners();
	}

	void IPartyScreenPrisonHandler.ExecuteDoneScript()
	{
		_dataSource.ExecuteDone();
	}

	void IPartyScreenPrisonHandler.ExecuteResetScript()
	{
		_dataSource.ExecuteReset();
	}

	void IPartyScreenPrisonHandler.ExecuteSellAllPrisoners()
	{
		_dataSource.ExecuteTransferAllMainPrisoners();
	}

	void IPartyScreenTroopHandler.PartyTroopTransfer()
	{
		_dataSource.ExecuteTransferAllMainTroops();
	}

	protected override void OnResume()
	{
		base.OnResume();
		PartyVM dataSource = _dataSource;
		if (dataSource != null && dataSource.IsInConversation)
		{
			_dataSource.IsInConversation = false;
			if (_dataSource.PartyScreenLogic.IsDoneActive())
			{
				_dataSource.PartyScreenLogic.DoneLogic(isForced: false);
			}
		}
	}

	public void RequestUserInput(string text, Action accept, Action cancel)
	{
	}

	private void HandleResetInput()
	{
		if (!_dataSource.IsAnyPopUpOpen)
		{
			_dataSource.ExecuteReset();
			UISoundsHelper.PlayUISound("event:/ui/default");
		}
	}

	private void HandleCancelInput()
	{
		if (_dataSource.UpgradePopUp.IsOpen)
		{
			_dataSource.UpgradePopUp.ExecuteCancel();
		}
		else if (_dataSource.RecruitPopUp.IsOpen)
		{
			_dataSource.RecruitPopUp.ExecuteCancel();
		}
		else
		{
			_dataSource.ExecuteCancel(showCancelInquiry: true);
		}
		UISoundsHelper.PlayUISound("event:/ui/default");
	}

	void IPartyScreenTroopHandler.ExecuteDoneScript()
	{
		_dataSource.ExecuteDone();
	}

	private void HandleDoneInput()
	{
		if (_dataSource.UpgradePopUp.IsOpen)
		{
			_dataSource.UpgradePopUp.ExecuteDone();
		}
		else if (_dataSource.RecruitPopUp.IsOpen)
		{
			_dataSource.RecruitPopUp.ExecuteDone();
		}
		else
		{
			_dataSource.ExecuteDone();
		}
		UISoundsHelper.PlayUISound("event:/ui/default");
	}

	private void OnCompanionRemoved(Hero arg1, RemoveCompanionAction.RemoveCompanionDetail arg2)
	{
		((IChangeableScreen)this).ApplyChanges();
	}

	bool IChangeableScreen.AnyUnsavedChanges()
	{
		return _partyState.PartyScreenLogic.IsThereAnyChanges();
	}

	bool IChangeableScreen.CanChangesBeApplied()
	{
		return _partyState.PartyScreenLogic.IsDoneActive();
	}

	void IChangeableScreen.ApplyChanges()
	{
		_partyState.PartyScreenLogic.DoneLogic(isForced: true);
	}

	void IChangeableScreen.ResetChanges()
	{
		_partyState.PartyScreenLogic.Reset(fromCancel: true);
	}
}
