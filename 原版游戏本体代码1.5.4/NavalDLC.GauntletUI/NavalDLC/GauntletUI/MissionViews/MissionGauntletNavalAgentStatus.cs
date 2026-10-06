using System;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(MissionAgentStatusUIHandler))]
internal class MissionGauntletNavalAgentStatus : MissionGauntletAgentStatus
{
	private NavalShipsLogic _navalShipsLogic;

	private TextObject _selectShipText;

	private TextObject _attemptBoardingText;

	private TextObject _cancelBoardingText;

	private IShipOrigin _focusedShipOrigin;

	private bool _focusedShipIsEnemy;

	private bool _canSelectShip;

	private bool _canAttemptBoarding;

	private bool _isBoardingBlocked;

	private bool _canCancelBoarding;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(RefreshTexts));
		RefreshTexts();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(RefreshTexts));
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.IsAgentStatusPrioritized = _navalShipsLogic?.PlayerControlledShip == null;
	}

	private void RefreshTexts()
	{
		_selectShipText = GameTexts.FindText("str_key_action").SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 113))).SetTextVariable("ACTION", new TextObject("{=QVlyuUu6}Select Ship"));
		_attemptBoardingText = GameTexts.FindText("str_key_action").SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 114))).SetTextVariable("ACTION", new TextObject("{=DJA4aQ8n}Attempt Boarding"));
		_cancelBoardingText = GameTexts.FindText("str_key_action").SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 114))).SetTextVariable("ACTION", new TextObject("{=0bSBXtCi}Cancel Boarding"));
		SetShipInteractionTexts();
	}

	public void UpdateShipInteractionTexts(IShipOrigin origin, bool isEnemy = false, bool canSelectShip = false, bool canAttemptBoarding = false, bool isBoardingBlocked = false, bool canCancelBoarding = false)
	{
		if (origin != _focusedShipOrigin || isEnemy != _focusedShipIsEnemy || canSelectShip != _canSelectShip || canAttemptBoarding != _canAttemptBoarding || isBoardingBlocked != _isBoardingBlocked || canCancelBoarding != _canCancelBoarding)
		{
			_focusedShipOrigin = origin;
			_focusedShipIsEnemy = isEnemy;
			_canSelectShip = canSelectShip;
			_canAttemptBoarding = canAttemptBoarding;
			_isBoardingBlocked = isBoardingBlocked;
			_canCancelBoarding = canCancelBoarding;
			SetShipInteractionTexts();
		}
	}

	private void SetShipInteractionTexts()
	{
		_dataSource.InteractionInterface.ClearForcedInteractionTexts();
		if (_focusedShipOrigin != null)
		{
			TextObject text = (_focusedShipIsEnemy ? new TextObject("{=PFqAEWSt}Enemy {SHIP_NAME}").SetTextVariable("SHIP_NAME", _focusedShipOrigin.Hull.Name) : _focusedShipOrigin.Name);
			TextObject text2 = null;
			bool isDisabled = false;
			if (_canSelectShip)
			{
				text2 = _selectShipText;
			}
			else if (_canAttemptBoarding)
			{
				if (_canCancelBoarding)
				{
					text2 = _cancelBoardingText;
				}
				else
				{
					text2 = _attemptBoardingText;
					isDisabled = _isBoardingBlocked;
				}
			}
			_dataSource.InteractionInterface.SetForcedInteractionTexts(text, isDisabled1: false, text2, isDisabled);
		}
		else if (_navalShipsLogic?.PlayerControlledShip != null)
		{
			_dataSource.InteractionInterface.SetForcedInteractionTexts(TextObject.GetEmpty(), isDisabled1: false, TextObject.GetEmpty(), isDisabled2: false);
		}
	}
}
