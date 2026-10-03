using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

internal sealed class SettlementBalancePopup
{
	private static SettlementBalancePopup _active;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly SettlementBalanceEditorVM _vm;
	private int _pending;
	private bool _closed;
	private SettlementBalancePopup(ScreenBase screen)
	{
		_screen = screen;
		_vm = new SettlementBalanceEditorVM(SettlementBalanceSettings.Current, () => Request(1), () => Request(2));
		_layer = new GauntletLayer("SettlementBalancePopup", 4211, false);
	}
	internal static void Open()
	{
		try
		{
			_active?.Close();
			SettlementBalanceSettings.Initialize();
			var screen = ScreenManager.TopScreen;
			if (screen == null) throw new InvalidOperationException("没有可用的设置页面。");
			_active = new SettlementBalancePopup(screen);
			_active._layer.LoadMovie("SettlementBalancePopup", _active._vm);
			_active._layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			var hotKeys = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
			if (hotKeys != null) _active._layer.Input.RegisterHotKeyCategory(hotKeys);
			screen.AddLayer(_active._layer);
			_active._layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_active._layer);
		}
		catch (Exception ex)
		{
			_active?.Close();
			PolicySystemLog.Failure("Balance", "popup-open-failed", ex.Message, ex.ToString());
			InformationManager.DisplayMessage(new InformationMessage("无法打开总量上限设置：" + ex.Message));
		}
	}
	private void Request(int action) { if (!_closed && _pending == 0) _pending = action; }
	internal static void ProcessDeferredCloseIfNeeded()
	{
		var popup = _active;
		if (popup == null || popup._closed) return;
		if (ScreenManager.TopScreen != popup._screen) { popup.Close(); return; }
		if (popup._layer.Input.IsHotKeyReleased("Exit") || popup._layer.Input.IsKeyReleased(InputKey.Escape)) popup.Request(2);
		int action = popup._pending;
		popup._pending = 0;
		if (action == 0) return;
		if (action == 2) { popup.Close(); return; }
		if (!popup._vm.TryCreateSnapshot(out var snapshot, out string error) || !SettlementBalanceSettings.TrySave(snapshot, out error))
		{
			popup._vm.SetSaveFailure(error);
			return;
		}
		popup.Close();
		InformationManager.DisplayMessage(new InformationMessage("总量上限已保存；旧存量不会被削减。"));
	}
	private void Close()
	{
		if (_closed) return;
		_closed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch (Exception ex) { PolicySystemLog.Failure("Balance", "popup-close-failed", ex.Message, ex.ToString()); }
		try { _screen.RemoveLayer(_layer); }
		catch (Exception ex) { PolicySystemLog.Failure("Balance", "popup-layer-remove-failed", ex.Message, ex.ToString()); }
		try { _vm.OnFinalize(); }
		finally
		{
			if (ReferenceEquals(_active, this)) _active = null;
		}
	}
}
