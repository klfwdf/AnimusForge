using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public class InputContext : IInputContext
{
	private List<GameKey> _registeredGameKeys;

	private Dictionary<string, HotKey> _registeredHotKeys;

	private Dictionary<string, GameAxisKey> _registeredGameAxisKeys;

	private List<Key> _downInputKeys;

	private List<Key> _allKeysListMemoryCache = new List<Key>();

	private readonly List<GameKeyContext> _categories;

	public bool IsKeysAllowed { get; set; }

	public bool IsMouseButtonAllowed { get; set; }

	public bool IsMouseWheelAllowed { get; set; }

	public bool IsControllerAllowed => IsKeysAllowed;

	public bool MouseOnMe { get; set; }

	public InputContext()
	{
		_categories = new List<GameKeyContext>();
		_registeredGameKeys = new List<GameKey>();
		_registeredHotKeys = new Dictionary<string, HotKey>();
		_registeredGameAxisKeys = new Dictionary<string, GameAxisKey>();
		_downInputKeys = new List<Key>();
		MouseOnMe = false;
	}

	public int GetPointerX()
	{
		float x = Input.Resolution.x;
		return (int)(GetMousePositionRanged().x * x);
	}

	public int GetPointerY()
	{
		float y = Input.Resolution.y;
		return (int)(GetMousePositionRanged().y * y);
	}

	public Vector2 GetPointerPosition()
	{
		Vec2 resolution = Input.Resolution;
		float x = resolution.x;
		float y = resolution.y;
		float x2 = GetMousePositionRanged().x * x;
		float y2 = GetMousePositionRanged().y * y;
		return new Vector2(x2, y2);
	}

	public Vec2 GetPointerPositionVec2()
	{
		Vec2 resolution = Input.Resolution;
		float x = resolution.x;
		float y = resolution.y;
		float a = GetMousePositionRanged().x * x;
		float b = GetMousePositionRanged().y * y;
		return new Vec2(a, b);
	}

	public void RegisterHotKeyCategory(GameKeyContext category)
	{
		_categories.Add(category);
		foreach (HotKey registeredHotKey in category.RegisteredHotKeys)
		{
			if (!_registeredHotKeys.ContainsKey(registeredHotKey.Id))
			{
				_registeredHotKeys.Add(registeredHotKey.Id, registeredHotKey);
			}
		}
		if (_registeredGameKeys.Count == 0)
		{
			int count = category.RegisteredGameKeys.Count;
			for (int i = 0; i < count; i++)
			{
				_registeredGameKeys.Add(null);
			}
		}
		foreach (GameKey registeredGameKey in category.RegisteredGameKeys)
		{
			if (registeredGameKey != null)
			{
				_registeredGameKeys[registeredGameKey.Id] = registeredGameKey;
			}
		}
		foreach (GameAxisKey registeredGameAxisKey in category.RegisteredGameAxisKeys)
		{
			if (!_registeredGameAxisKeys.ContainsKey(registeredGameAxisKey.Id))
			{
				_registeredGameAxisKeys.Add(registeredGameAxisKey.Id, registeredGameAxisKey);
			}
		}
	}

	public bool IsCategoryRegistered(GameKeyContext category)
	{
		return _categories?.Contains(category) ?? false;
	}

	private List<Key> GetAllAvailableKeys()
	{
		_allKeysListMemoryCache.Clear();
		for (int i = 0; i < _registeredGameKeys.Count; i++)
		{
			GameKey gameKey = _registeredGameKeys[i];
			if (gameKey != null)
			{
				if (gameKey.KeyboardKey != null)
				{
					_allKeysListMemoryCache.Add(gameKey.KeyboardKey);
				}
				if (gameKey.ControllerKey != null)
				{
					_allKeysListMemoryCache.Add(gameKey.ControllerKey);
				}
			}
		}
		foreach (HotKey value in _registeredHotKeys.Values)
		{
			for (int j = 0; j < value.Keys.Count; j++)
			{
				if (value.Keys[j] != null)
				{
					_allKeysListMemoryCache.Add(value.Keys[j]);
				}
			}
		}
		return _allKeysListMemoryCache;
	}

	public void RegisterDownKeys()
	{
		List<Key> allAvailableKeys = GetAllAvailableKeys();
		for (int i = 0; i < allAvailableKeys.Count; i++)
		{
			Key key = allAvailableKeys[i];
			if (key.IsPressed() && !_downInputKeys.Contains(key))
			{
				_downInputKeys.Add(key);
			}
		}
	}

	public void UnregisterReleasedKeys()
	{
		List<Key> allAvailableKeys = GetAllAvailableKeys();
		for (int i = 0; i < allAvailableKeys.Count; i++)
		{
			Key key = allAvailableKeys[i];
			if (key.IsReleased() && _downInputKeys.Contains(key))
			{
				_downInputKeys.Remove(key);
			}
		}
	}

	public void ResetLastDownKeys()
	{
		_downInputKeys.Clear();
	}

	private bool IsHotKeyDown(HotKey hotKey)
	{
		return hotKey.IsDown(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public bool IsHotKeyDown(string hotKey)
	{
		if (_registeredHotKeys.TryGetValue(hotKey, out var value))
		{
			return IsHotKeyDown(value);
		}
		return false;
	}

	private bool IsGameKeyDown(GameKey gameKey)
	{
		return gameKey.IsDown(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public bool IsGameKeyDown(int gameKey)
	{
		GameKey gameKey2 = _registeredGameKeys[gameKey];
		return IsGameKeyDown(gameKey2);
	}

	private bool IsGameKeyDownImmediate(GameKey gameKey)
	{
		return gameKey.IsDownImmediate(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public bool IsGameKeyDownImmediate(int gameKey)
	{
		GameKey gameKey2 = _registeredGameKeys[gameKey];
		return IsGameKeyDownImmediate(gameKey2);
	}

	private bool IsHotKeyPressed(HotKey hotKey)
	{
		return hotKey.IsPressed(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public bool IsHotKeyPressed(string hotKey)
	{
		if (_registeredHotKeys.TryGetValue(hotKey, out var value))
		{
			return IsHotKeyPressed(value);
		}
		return false;
	}

	private bool IsGameKeyPressed(GameKey gameKey)
	{
		return gameKey.IsPressed(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public bool IsGameKeyPressed(int gameKey)
	{
		GameKey gameKey2 = _registeredGameKeys[gameKey];
		return IsGameKeyPressed(gameKey2);
	}

	private bool IsHotKeyReleased(HotKey hotKey)
	{
		for (int i = 0; i < hotKey.Keys.Count; i++)
		{
			if (_downInputKeys.Contains(hotKey.Keys[i]))
			{
				return hotKey.IsReleased(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
			}
		}
		return false;
	}

	public bool IsHotKeyReleased(string hotKey)
	{
		if (_registeredHotKeys.TryGetValue(hotKey, out var value))
		{
			return IsHotKeyReleased(value);
		}
		return false;
	}

	private bool IsGameKeyReleased(GameKey gameKey)
	{
		if (_downInputKeys.Contains(gameKey.KeyboardKey) || _downInputKeys.Contains(gameKey.ControllerKey))
		{
			return gameKey.IsReleased(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
		}
		return false;
	}

	public bool IsGameKeyReleased(int gameKey)
	{
		GameKey gameKey2 = _registeredGameKeys[gameKey];
		return IsGameKeyReleased(gameKey2);
	}

	private float GetGameKeyState(GameKey gameKey)
	{
		return gameKey.GetKeyState(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public float GetGameKeyState(int gameKey)
	{
		GameKey gameKey2 = _registeredGameKeys[gameKey];
		return GetGameKeyState(gameKey2);
	}

	private bool IsHotKeyDoublePressed(HotKey hotKey)
	{
		return hotKey.IsDoublePressed(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public bool IsHotKeyDoublePressed(string hotKey)
	{
		if (_registeredHotKeys.TryGetValue(hotKey, out var value))
		{
			return IsHotKeyDoublePressed(value);
		}
		return false;
	}

	public float GetGameKeyAxis(GameAxisKey gameKey)
	{
		return gameKey.GetAxisState(IsKeysAllowed, IsMouseButtonAllowed && MouseOnMe, IsMouseWheelAllowed, IsControllerAllowed);
	}

	public float GetGameKeyAxis(string gameKey)
	{
		if (_registeredGameAxisKeys.TryGetValue(gameKey, out var value))
		{
			return GetGameKeyAxis(value);
		}
		return 0f;
	}

	internal bool CanUse(InputKey key)
	{
		InputKey[] clickKeys = Input.GetClickKeys();
		for (int i = 0; i < clickKeys.Length; i++)
		{
			if (clickKeys[i] == key)
			{
				if (!IsMouseButtonAllowed)
				{
					return IsControllerAllowed;
				}
				return true;
			}
		}
		switch (key)
		{
		case InputKey.Escape:
		case InputKey.D1:
		case InputKey.D2:
		case InputKey.D3:
		case InputKey.D4:
		case InputKey.D5:
		case InputKey.D6:
		case InputKey.D7:
		case InputKey.D8:
		case InputKey.D9:
		case InputKey.D0:
		case InputKey.Minus:
		case InputKey.Equals:
		case InputKey.BackSpace:
		case InputKey.Tab:
		case InputKey.Q:
		case InputKey.W:
		case InputKey.E:
		case InputKey.R:
		case InputKey.T:
		case InputKey.Y:
		case InputKey.U:
		case InputKey.I:
		case InputKey.O:
		case InputKey.P:
		case InputKey.OpenBraces:
		case InputKey.CloseBraces:
		case InputKey.Enter:
		case InputKey.LeftControl:
		case InputKey.A:
		case InputKey.S:
		case InputKey.D:
		case InputKey.F:
		case InputKey.G:
		case InputKey.H:
		case InputKey.J:
		case InputKey.K:
		case InputKey.L:
		case InputKey.SemiColon:
		case InputKey.Apostrophe:
		case InputKey.Tilde:
		case InputKey.LeftShift:
		case InputKey.BackSlash:
		case InputKey.Z:
		case InputKey.X:
		case InputKey.C:
		case InputKey.V:
		case InputKey.B:
		case InputKey.N:
		case InputKey.M:
		case InputKey.Comma:
		case InputKey.Period:
		case InputKey.Slash:
		case InputKey.RightShift:
		case InputKey.NumpadMultiply:
		case InputKey.LeftAlt:
		case InputKey.Space:
		case InputKey.CapsLock:
		case InputKey.F1:
		case InputKey.F2:
		case InputKey.F3:
		case InputKey.F4:
		case InputKey.F5:
		case InputKey.F6:
		case InputKey.F7:
		case InputKey.F8:
		case InputKey.F9:
		case InputKey.F10:
		case InputKey.Numpad7:
		case InputKey.Numpad8:
		case InputKey.Numpad9:
		case InputKey.NumpadMinus:
		case InputKey.Numpad4:
		case InputKey.Numpad5:
		case InputKey.Numpad6:
		case InputKey.NumpadPlus:
		case InputKey.Numpad1:
		case InputKey.Numpad2:
		case InputKey.Numpad3:
		case InputKey.Numpad0:
		case InputKey.NumpadPeriod:
		case InputKey.F11:
		case InputKey.F12:
		case InputKey.NumpadEnter:
		case InputKey.RightControl:
		case InputKey.NumpadSlash:
		case InputKey.RightAlt:
		case InputKey.NumLock:
		case InputKey.Home:
		case InputKey.Up:
		case InputKey.PageUp:
		case InputKey.Left:
		case InputKey.Right:
		case InputKey.End:
		case InputKey.Down:
		case InputKey.PageDown:
		case InputKey.Insert:
		case InputKey.Delete:
			return IsKeysAllowed;
		case InputKey.LeftMouseButton:
		case InputKey.RightMouseButton:
		case InputKey.MiddleMouseButton:
		case InputKey.X1MouseButton:
		case InputKey.X2MouseButton:
			return IsMouseButtonAllowed;
		case InputKey.MouseScrollUp:
		case InputKey.MouseScrollDown:
			return IsMouseWheelAllowed;
		case InputKey.ControllerLStick:
		case InputKey.ControllerRStick:
		case InputKey.ControllerLStickUp:
		case InputKey.ControllerLStickDown:
		case InputKey.ControllerLStickLeft:
		case InputKey.ControllerLStickRight:
		case InputKey.ControllerRStickUp:
		case InputKey.ControllerRStickDown:
		case InputKey.ControllerRStickLeft:
		case InputKey.ControllerRStickRight:
		case InputKey.ControllerLUp:
		case InputKey.ControllerLDown:
		case InputKey.ControllerLLeft:
		case InputKey.ControllerLRight:
		case InputKey.ControllerRUp:
		case InputKey.ControllerRDown:
		case InputKey.ControllerRLeft:
		case InputKey.ControllerRRight:
		case InputKey.ControllerLBumper:
		case InputKey.ControllerRBumper:
		case InputKey.ControllerLOption:
		case InputKey.ControllerROption:
		case InputKey.ControllerLTrigger:
		case InputKey.ControllerRTrigger:
			return IsControllerAllowed;
		default:
			return false;
		}
	}

	public Vec2 GetKeyState(InputKey key)
	{
		if (!CanUse(key))
		{
			return new Vec2(0f, 0f);
		}
		return Input.GetKeyState(key);
	}

	protected bool IsMouseButton(InputKey key)
	{
		if (key != InputKey.LeftMouseButton && key != InputKey.RightMouseButton)
		{
			return key == InputKey.MiddleMouseButton;
		}
		return true;
	}

	public bool IsKeyDown(InputKey key)
	{
		if (IsMouseButton(key))
		{
			if (!MouseOnMe)
			{
				return false;
			}
		}
		else if (!CanUse(key))
		{
			return false;
		}
		return Input.IsKeyDown(key);
	}

	public bool IsKeyPressed(InputKey key)
	{
		if (CanUse(key))
		{
			return Input.IsKeyPressed(key);
		}
		return false;
	}

	public bool IsKeyReleased(InputKey key)
	{
		if (IsMouseButton(key))
		{
			if (!MouseOnMe)
			{
				return false;
			}
		}
		else if (!CanUse(key))
		{
			return false;
		}
		return Input.IsKeyReleased(key);
	}

	public float GetMouseMoveX()
	{
		return Input.GetMouseMoveX();
	}

	public float GetMouseMoveY()
	{
		return Input.GetMouseMoveY();
	}

	public float GetNormalizedMouseMoveX()
	{
		return Input.GetNormalizedMouseMoveX();
	}

	public float GetNormalizedMouseMoveY()
	{
		return Input.GetNormalizedMouseMoveY();
	}

	public Vec2 GetControllerRightStickState()
	{
		return Input.GetKeyState(InputKey.ControllerRStick);
	}

	public Vec2 GetControllerLeftStickState()
	{
		return Input.GetKeyState(InputKey.ControllerLStick);
	}

	public bool GetIsMouseActive()
	{
		return Input.IsMouseActive;
	}

	public bool GetIsMouseDown()
	{
		if (!Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			return Input.IsKeyDown(InputKey.RightMouseButton);
		}
		return true;
	}

	public Vec2 GetMousePositionPixel()
	{
		return Input.MousePositionPixel;
	}

	public float GetDeltaMouseScroll()
	{
		if (!IsMouseWheelAllowed)
		{
			return 0f;
		}
		return Input.DeltaMouseScroll;
	}

	public bool GetIsControllerConnected()
	{
		return Input.IsControllerConnected;
	}

	public Vec2 GetMousePositionRanged()
	{
		return Input.MousePositionRanged;
	}

	public float GetMouseSensitivity()
	{
		return Input.MouseSensitivity;
	}

	public bool IsControlDown()
	{
		if (IsKeysAllowed)
		{
			if (!Input.IsKeyDown(InputKey.LeftControl))
			{
				return Input.IsKeyDown(InputKey.RightControl);
			}
			return true;
		}
		return false;
	}

	public bool IsShiftDown()
	{
		if (IsKeysAllowed)
		{
			if (!Input.IsKeyDown(InputKey.LeftShift))
			{
				return Input.IsKeyDown(InputKey.RightShift);
			}
			return true;
		}
		return false;
	}

	public bool IsAltDown()
	{
		if (IsKeysAllowed)
		{
			if (!Input.IsKeyDown(InputKey.LeftAlt))
			{
				return Input.IsKeyDown(InputKey.RightAlt);
			}
			return true;
		}
		return false;
	}

	public InputKey[] GetClickKeys()
	{
		return Input.GetClickKeys();
	}
}
