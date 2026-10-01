using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class DevLargeSelectionPopup
{
	public sealed class Option
	{
		public Option(string id, string titleText, string detailText = null, string metaText = null, bool isDanger = false, bool isPrimary = false)
		{
			Id = id ?? "";
			TitleText = titleText ?? "";
			DetailText = detailText ?? "";
			MetaText = metaText ?? "";
			IsDanger = isDanger;
			IsPrimary = isPrimary;
		}

		public string Id { get; }

		public string TitleText { get; }

		public string DetailText { get; }

		public string MetaText { get; }

		public bool IsDanger { get; }

		public bool IsPrimary { get; }
	}

	private static DevLargeSelectionPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly DevLargeSelectionPopupVM _dataSource;

	private readonly Action<string> _onSelect;

	private readonly Action _onCancel;

	private bool _isClosed;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private DevLargeSelectionPopup(ScreenBase screen, string titleText, string subtitleText, string bodyText, IReadOnlyList<Option> options, Action<string> onSelect, Action onCancel, string cancelText)
	{
		_screen = screen;
		_onSelect = onSelect;
		_onCancel = onCancel;
		_dataSource = new DevLargeSelectionPopupVM(titleText, subtitleText, bodyText, options ?? new List<Option>(), HandleSelectRequested, HandleCancelRequested, cancelText);
		_layer = new GauntletLayer("DevLargeSelectionPopup", 4000, false);
	}

	public static bool Show(string titleText, string subtitleText, string bodyText, IEnumerable<Option> options, Action<string> onSelect, Action onCancel, string cancelText = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			List<Option> optionList = (options ?? Enumerable.Empty<Option>()).Where(x => x != null).ToList();
			DevLargeSelectionPopup popup = new DevLargeSelectionPopup(topScreen, titleText, subtitleText, bodyText, optionList, onSelect, onCancel, cancelText);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DevLargeSelectionPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static bool ShowText(string titleText, string subtitleText, string bodyText, Action onClose, string closeText = null)
	{
		return Show(titleText, subtitleText, bodyText, Enumerable.Empty<Option>(), _ => { }, onClose, closeText);
	}

	private void Open()
	{
		_layer.LoadMovie("DevLargeSelectionPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandleSelectRequested(string id)
	{
		Close(silent: true);
		_onSelect?.Invoke(id ?? "");
	}

	private void HandleCancelRequested()
	{
		Close(silent: true);
		_onCancel?.Invoke();
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		try
		{
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				Logger.Log("DevLargeSelectionPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}
