using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

/// <summary>
/// Campaign-map-only edge-triggered hotkey. Per frame: one cached key lookup
/// and one IsKeyDown; settings are re-read at most once per second.
/// </summary>
internal static class FieldCampHotkey
{
	private const float KeyRefreshIntervalSeconds = 1f;
	private const float CooldownSeconds = 0.4f;

	private static InputKey _key = InputKey.Z;
	private static string _keyRaw = "Z";
	private static float _nextKeyRefresh;
	private static float _lastPress;
	private static bool _wasDown;

	internal static bool ConsumePress()
	{
		if (!FieldCampSettings.Enabled || Mission.Current != null)
		{
			_wasDown = false;
			return false;
		}
		bool down;
		try
		{
			down = Input.IsKeyDown(CurrentKey());
		}
		catch
		{
			down = false;
		}
		if (!down)
		{
			_wasDown = false;
			return false;
		}
		if (_wasDown)
		{
			return false;
		}
		_wasDown = true;
		if (HotkeyInputGuard.IsTextInputFocused() || !IsPlainMapScreen())
		{
			return false;
		}
		float now = Time.ApplicationTime;
		if (now - _lastPress < CooldownSeconds)
		{
			return false;
		}
		_lastPress = now;
		return true;
	}

	private static bool IsPlainMapScreen()
	{
		if (!(ScreenManager.TopScreen is MapScreen map))
		{
			return false;
		}
		if (Campaign.Current.ConversationManager?.IsConversationInProgress == true)
		{
			return false;
		}
		return !map.IsEscapeMenuOpened && !map.IsInBattleSimulation && map.EncyclopediaScreenManager?.IsEncyclopediaOpen != true;
	}

	private static InputKey CurrentKey()
	{
		float now = Time.ApplicationTime;
		if (now < _nextKeyRefresh)
		{
			return _key;
		}
		_nextKeyRefresh = now + KeyRefreshIntervalSeconds;
		string raw = FieldCampSettings.KeyText ?? "";
		if (string.Equals(raw, _keyRaw, StringComparison.Ordinal))
		{
			return _key;
		}
		_keyRaw = raw;
		_key = Enum.TryParse(raw.Trim().ToUpperInvariant(), out InputKey parsed) && parsed != InputKey.Invalid ? parsed : InputKey.Z;
		return _key;
	}
}
