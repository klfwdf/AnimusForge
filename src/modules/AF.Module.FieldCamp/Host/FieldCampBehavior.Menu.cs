using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Camp game menus. Two menus keep the native wait semantics simple:
/// <see cref="MenuId"/> is a normal menu (time stopped), <see cref="WaitMenuId"/>
/// is a wait menu that fast-forwards while pitching, resting or striking.
/// </summary>
public sealed partial class FieldCampBehavior
{
	internal const string MenuId = "af_field_camp";
	internal const string WaitMenuId = "af_field_camp_wait";

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		starter.AddGameMenu(MenuId, "{=!}{AF_FC_STATUS}", OnCampMenuInit);
		starter.AddGameMenuOption(MenuId, "af_fc_patrol", "{=AF_FC_Menu_Patrol}Walk the camp", OnPatrolCondition, OnPatrolConsequence);
		starter.AddGameMenuOption(MenuId, "af_fc_rest", "{=AF_FC_Menu_Rest}Rest and wait", OnRestCondition, OnRestConsequence);
		starter.AddGameMenuOption(MenuId, "af_fc_strike", "{=AF_FC_Menu_Strike}Strike camp", OnStrikeCondition, OnStrikeConsequence);
		starter.AddGameMenuOption(MenuId, "af_fc_leave", "{=AF_FC_Menu_Leave}Return to the map", OnLeaveCondition, OnLeaveConsequence, isLeave: true);

		starter.AddWaitGameMenu(WaitMenuId, "{=!}{AF_FC_STATUS}", OnWaitMenuInit, OnWaitMenuCondition, null, OnWaitMenuTick,
			GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption);
		starter.AddGameMenuOption(WaitMenuId, "af_fc_cancel_pitch", "{=AF_FC_Menu_CancelPitch}Cancel pitching camp", OnCancelPitchCondition, OnCancelPitchConsequence, isLeave: true);
		starter.AddGameMenuOption(WaitMenuId, "af_fc_stop_wait", "{=AF_FC_Menu_StopWait}Stop resting", OnStopWaitCondition, OnStopWaitConsequence, isLeave: true);
	}

	private bool _resting;

	private void OpenCampMenu()
	{
		string target = _state == (int)CampState.Camped && !_resting ? MenuId : WaitMenuId;
		try
		{
			if (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.SwitchToMenu(target);
			}
			else
			{
				GameMenu.ActivateGameMenu(target);
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "open menu failed menu=" + target + " error=" + ex.Message);
		}
	}

	private void LeaveCampMenusIfOpen()
	{
		_resting = false;
		MenuContext context = Campaign.Current?.CurrentMenuContext;
		string id = context?.GameMenu?.StringId;
		if (id != MenuId && id != WaitMenuId)
		{
			return;
		}
		context.GameMenu.EndWait();
		GameMenu.ExitToLast();
	}

	private void SetBackground(MenuCallbackArgs args)
	{
		try
		{
			string mesh = Hero.MainHero?.MapFaction?.Culture?.EncounterBackgroundMesh;
			args.MenuContext.SetBackgroundMeshName(string.IsNullOrEmpty(mesh) ? "wait_fallback" : mesh);
		}
		catch
		{
		}
	}

	private void RefreshStatusText(MenuCallbackArgs args)
	{
		args.MenuContext.GameMenu.GetText().SetTextVariable("AF_FC_STATUS", FieldCampText.Status(State, _resting, _parties.Count, NowHours - _startHours, Math.Max(0.0, _phaseEndHours - NowHours)));
	}

	// ----- normal camp menu -----

	private void OnCampMenuInit(MenuCallbackArgs args)
	{
		args.MenuTitle = FieldCampText.MenuTitle();
		SetBackground(args);
		RefreshStatusText(args);
	}

	private bool OnPatrolCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		args.Tooltip = FieldCampText.PatrolTooltip(FieldCampSettings.PatrolHours);
		if (Hero.MainHero?.IsWounded == true)
		{
			args.IsEnabled = false;
			args.Tooltip = FieldCampText.PatrolWounded();
		}
		return _state == (int)CampState.Camped;
	}

	private void OnPatrolConsequence(MenuCallbackArgs args)
	{
		EnsureParties();
		_resting = false;
		if (!FieldCampMissionLauncher.TryOpen(_parties, out string failure))
		{
			Logger.Log(LogSource, "camp mission open failed: " + failure);
			AnimusForgeQuickInfo.Show(FieldCampText.PatrolFailed());
			return;
		}
		BeginPatrol();
	}

	private bool OnRestCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		return _state == (int)CampState.Camped;
	}

	private void OnRestConsequence(MenuCallbackArgs args)
	{
		_resting = true;
		GameMenu.SwitchToMenu(WaitMenuId);
	}

	private bool OnStrikeCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		args.Tooltip = FieldCampText.StrikeTooltip(FieldCampSettings.StrikeHours);
		return _state == (int)CampState.Camped;
	}

	private void OnStrikeConsequence(MenuCallbackArgs args)
	{
		_resting = false;
		BeginStrike();
		GameMenu.SwitchToMenu(WaitMenuId);
	}

	private bool OnLeaveCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void OnLeaveConsequence(MenuCallbackArgs args)
	{
		// The camp stays pitched; the hotkey or terminal reopens this menu.
		_resting = false;
		GameMenu.ExitToLast();
	}

	// ----- wait menu (time runs) -----

	private void OnWaitMenuInit(MenuCallbackArgs args)
	{
		args.MenuTitle = FieldCampText.MenuTitle();
		SetBackground(args);
		RefreshStatusText(args);
		UpdateProgress(args);
	}

	private bool OnWaitMenuCondition(MenuCallbackArgs args)
	{
		return IsActive;
	}

	private void OnWaitMenuTick(MenuCallbackArgs args, CampaignTime dt)
	{
		// An encounter takes precedence over leaving: hand over to the native state menu first.
		string generic = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (!string.IsNullOrEmpty(generic))
		{
			args.MenuContext.GameMenu.EndWait();
			GameMenu.SwitchToMenu(generic);
			return;
		}
		if (!IsActive)
		{
			args.MenuContext.GameMenu.EndWait();
			GameMenu.ExitToLast();
			return;
		}
		if (_resting && _patrolSettleUntilHours >= 0 && NowHours >= _patrolSettleUntilHours)
		{
			// The patrol's time is spent; hand control back to the camp menu.
			_patrolSettleUntilHours = -1;
			_phaseEndHours = -1;
			_resting = false;
		}
		if (_state == (int)CampState.Camped && !_resting)
		{
			args.MenuContext.GameMenu.EndWait();
			GameMenu.SwitchToMenu(MenuId);
			return;
		}
		RefreshStatusText(args);
		UpdateProgress(args);
	}

	private void UpdateProgress(MenuCallbackArgs args)
	{
		float progress = 0f;
		if (_state == (int)CampState.Pitching || _state == (int)CampState.Striking)
		{
			float total = _state == (int)CampState.Pitching ? FieldCampSettings.PitchHours : FieldCampSettings.StrikeHours;
			double remaining = Math.Max(0.0, _phaseEndHours - NowHours);
			progress = total <= 0f ? 1f : MathF.Clamp(1f - (float)(remaining / total), 0f, 0.999f);
		}
		else if (_patrolSettleUntilHours >= 0)
		{
			float total = FieldCampSettings.PatrolHours;
			double remaining = Math.Max(0.0, _patrolSettleUntilHours - NowHours);
			progress = total <= 0f ? 1f : MathF.Clamp(1f - (float)(remaining / total), 0f, 0.999f);
		}
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(progress);
	}

	private bool OnCancelPitchCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return _state == (int)CampState.Pitching;
	}

	private void OnCancelPitchConsequence(MenuCallbackArgs args)
	{
		args.MenuContext.GameMenu.EndWait();
		BeginStrike();
		_resting = false;
		GameMenu.ExitToLast();
	}

	private bool OnStopWaitCondition(MenuCallbackArgs args)
	{
		// Striking and the patrol's time cost cannot be interrupted; plain resting can.
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return _state == (int)CampState.Camped && _resting && _patrolSettleUntilHours < 0;
	}

	private void OnStopWaitConsequence(MenuCallbackArgs args)
	{
		args.MenuContext.GameMenu.EndWait();
		_resting = false;
		GameMenu.SwitchToMenu(MenuId);
	}
}
