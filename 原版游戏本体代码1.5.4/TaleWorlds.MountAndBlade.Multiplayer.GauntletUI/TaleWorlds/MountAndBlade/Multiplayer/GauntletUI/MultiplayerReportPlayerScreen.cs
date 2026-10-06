using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.PlayerServices;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

public class MultiplayerReportPlayerScreen : GlobalLayer
{
	private MultiplayerReportPlayerVM _dataSource;

	private bool _isActive;

	public static MultiplayerReportPlayerScreen Current { get; private set; }

	public MultiplayerReportPlayerScreen()
	{
		_dataSource = new MultiplayerReportPlayerVM(OnReportDone, OnClose);
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		GauntletLayer gauntletLayer = new GauntletLayer("MultiplayerReportPlayer", 15350);
		gauntletLayer.LoadMovie("MultiplayerReportPlayer", _dataSource);
		gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer = gauntletLayer;
	}

	protected override void OnTick(float dt)
	{
		if (_isActive)
		{
			if (base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ExecuteDone();
			}
			else if (base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ExecuteCancel();
			}
		}
	}

	private void OnClose()
	{
		if (_isActive)
		{
			_isActive = false;
			base.Layer.InputRestrictions.ResetInputRestrictions();
			ScreenManager.SetSuspendLayer(base.Layer, isSuspended: true);
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
		}
	}

	public static void OnInitialize()
	{
		if (Current == null)
		{
			Current = new MultiplayerReportPlayerScreen();
			ScreenManager.AddGlobalLayer(Current, isFocusable: false);
			MultiplayerReportPlayerManager.ReportHandlers += Current.OnReportRequest;
			Current._isActive = false;
			ScreenManager.SetSuspendLayer(Current.Layer, isSuspended: true);
		}
	}

	public static void OnFinalize()
	{
		if (Current != null)
		{
			ScreenManager.RemoveGlobalLayer(Current);
			MultiplayerReportPlayerManager.ReportHandlers -= Current.OnReportRequest;
			Current._dataSource.OnFinalize();
			Current._dataSource = null;
			Current = null;
		}
	}

	private void OnReportRequest(string gameId, PlayerId playerId, string playerName, bool isRequestedFromMission)
	{
		if (!_isActive)
		{
			_isActive = true;
			ScreenManager.SetSuspendLayer(base.Layer, isSuspended: false);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			base.Layer.InputRestrictions.SetInputRestrictions();
			_dataSource.OpenNewReportWithGamePlayerId(gameId, playerId, playerName, isRequestedFromMission);
		}
	}

	private void OnReportDone(string gameId, PlayerId playerId, string playerName, PlayerReportType reportReason, string reasonText)
	{
		if (_isActive)
		{
			OnClose();
			NetworkMain.GameClient.ReportPlayer(gameId, playerId, playerName, reportReason, reasonText);
			MultiplayerReportPlayerManager.OnPlayerReported(playerId);
		}
	}
}
