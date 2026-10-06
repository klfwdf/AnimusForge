using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionSingleplayerEscapeMenu))]
public class MissionGauntletSingleplayerEscapeMenu : MissionGauntletEscapeMenuBase
{
	private MissionOptionsComponent _missionOptionsComponent;

	private bool _isIronmanMode;

	public MissionGauntletSingleplayerEscapeMenu(bool isIronmanMode)
		: base("EscapeMenu")
	{
		_isIronmanMode = isIronmanMode;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_missionOptionsComponent = base.Mission.GetMissionBehavior<MissionOptionsComponent>();
		DataSource = new EscapeMenuVM(null);
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.HideBattleUI)
		{
			DataSource?.RefreshItems(GetEscapeMenuItems());
		}
	}

	public override void OnFocusChangeOnGameWindow(bool focusGained)
	{
		base.OnFocusChangeOnGameWindow(focusGained);
		if (!focusGained && BannerlordConfig.StopGameOnFocusLost && base.MissionScreen.IsOpeningEscapeMenuOnFocusChangeAllowed() && !GameStateManager.Current.ActiveStateDisabledByUser && !LoadingWindow.IsLoadingWindowActive && !base.IsActive)
		{
			OnEscape();
		}
	}

	public override void OnSceneRenderingStarted()
	{
		base.OnSceneRenderingStarted();
		if (base.MissionScreen.IsFocusLost)
		{
			OnFocusChangeOnGameWindow(focusGained: false);
		}
	}

	protected override List<EscapeMenuItemVM> GetEscapeMenuItems()
	{
		TextObject ironmanDisabledReason = GameTexts.FindText("str_pause_menu_disabled_hint", "IronmanMode");
		List<EscapeMenuItemVM> list = new List<EscapeMenuItemVM>();
		list.Add(new EscapeMenuItemVM(new TextObject("{=e139gKZc}Return to the Game"), delegate
		{
			OnEscapeMenuToggled(isOpened: false);
		}, null, () => new Tuple<bool, TextObject>(item1: false, null), isPositiveBehaviored: true));
		list.Add(new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options"), delegate
		{
			OnEscapeMenuToggled(isOpened: false);
			_missionOptionsComponent?.OnAddOptionsUIHandler();
		}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
		if (BannerlordConfig.HideBattleUI)
		{
			list.Add(new EscapeMenuItemVM(new TextObject("{=asCeKZXx}Re-enable Battle UI"), delegate
			{
				ManagedOptions.SetConfig(ManagedOptions.ManagedOptionsType.HideBattleUI, 0f);
				ManagedOptions.SaveConfig();
				DataSource.RefreshItems(GetEscapeMenuItems());
			}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
		}
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			MissionCheatView missionBehavior = base.Mission.GetMissionBehavior<MissionCheatView>();
			if (missionBehavior != null && missionBehavior.GetIsCheatsAvailable())
			{
				list.Add(new EscapeMenuItemVM(new TextObject("{=WA6Sk6cH}Cheat Menu"), delegate
				{
					base.MissionScreen.Mission.GetMissionBehavior<MissionCheatView>().InitializeScreen();
				}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
			}
		}
		list.Add(new EscapeMenuItemVM(new TextObject("{=VklN5Wm6}Photo Mode"), delegate
		{
			OnEscapeMenuToggled(isOpened: false);
			base.MissionScreen.SetPhotoModeEnabled(isEnabled: true);
			base.Mission.IsInPhotoMode = true;
			InformationManager.HideAllMessages();
		}, null, () => GetIsPhotoModeDisabled()));
		list.Add(new EscapeMenuItemVM(new TextObject("{=RamV6yLM}Exit to Main Menu"), delegate
		{
			if (Game.Current?.GameType is EditorGame || Game.Current?.GameType.GetType().Name == "CustomGame")
			{
				OnExitToMainMenu();
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_exit").ToString(), GameTexts.FindText("str_mission_exit_query").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), OnExitToMainMenu, delegate
				{
					OnEscapeMenuToggled(isOpened: false);
				}));
			}
		}, null, () => new Tuple<bool, TextObject>(_isIronmanMode, ironmanDisabledReason)));
		return list;
	}

	private Tuple<bool, TextObject> GetIsPhotoModeDisabled()
	{
		if (base.MissionScreen.IsDeploymentActive)
		{
			return new Tuple<bool, TextObject>(item1: true, new TextObject("{=rZSjkCpw}Cannot use photo mode during deployment."));
		}
		if (base.MissionScreen.IsConversationActive)
		{
			return new Tuple<bool, TextObject>(item1: true, new TextObject("{=ImQnhIQ5}Cannot use photo mode during conversation."));
		}
		if (base.MissionScreen.IsPhotoModeEnabled)
		{
			return new Tuple<bool, TextObject>(item1: true, new TextObject("{=79bODbwZ}Photo mode is already active."));
		}
		if (Module.CurrentModule.IsOnlyCoreContentEnabled)
		{
			return new Tuple<bool, TextObject>(item1: true, new TextObject("{=V8BXjyYq}Disabled during installation."));
		}
		if (base.MissionScreen.Mission.Mode == MissionMode.CutScene)
		{
			return new Tuple<bool, TextObject>(item1: true, new TextObject("{=WazbgBDJ}Disabled during cutscenes."));
		}
		if (base.MissionScreen.CustomCamera != null || !base.MissionScreen.IsPhotoModeAllowed())
		{
			return new Tuple<bool, TextObject>(item1: true, new TextObject("{=7xU2wu7M}Photo mode isn't allowed."));
		}
		return new Tuple<bool, TextObject>(item1: false, null);
	}

	private void OnExitToMainMenu()
	{
		OnEscapeMenuToggled(isOpened: false);
		InformationManager.HideInquiry();
		MBGameManager.EndGame();
	}
}
