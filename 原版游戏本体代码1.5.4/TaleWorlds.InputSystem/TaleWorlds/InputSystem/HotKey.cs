using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public class HotKey
{
	[Flags]
	public enum Modifiers
	{
		None = 0,
		Shift = 1,
		Alt = 2,
		Control = 4
	}

	private const int DOUBLE_PRESS_TIME = 500;

	private int _doublePressTime;

	public string Id;

	public string GroupId;

	private Modifiers _modifiers;

	private Modifiers _negativeModifiers;

	private bool _isDoublePressActive
	{
		get
		{
			int num = Environment.TickCount - _doublePressTime;
			if (num < 500)
			{
				return num >= 0;
			}
			return false;
		}
	}

	public List<Key> Keys { get; internal set; }

	public List<Key> DefaultKeys { get; private set; }

	public HotKey(string id, string groupId, List<Key> keys, Modifiers modifiers = Modifiers.None, Modifiers negativeModifiers = Modifiers.None)
	{
		Id = id;
		GroupId = groupId;
		Keys = keys;
		DefaultKeys = new List<Key>();
		for (int i = 0; i < Keys.Count; i++)
		{
			DefaultKeys.Add(new Key(Keys[i].InputKey));
		}
		_modifiers = modifiers;
		_negativeModifiers = negativeModifiers;
	}

	public HotKey(string id, string groupId, InputKey inputKey, Modifiers modifiers = Modifiers.None, Modifiers negativeModifiers = Modifiers.None)
	{
		Id = id;
		GroupId = groupId;
		Keys = new List<Key>
		{
			new Key(inputKey)
		};
		DefaultKeys = new List<Key>
		{
			new Key(inputKey)
		};
		_modifiers = modifiers;
		_negativeModifiers = negativeModifiers;
	}

	private bool IsKeyAllowed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if ((!isKeysAllowed && key.IsKeyboardInput) || (!isMouseButtonAllowed && key.IsMouseButtonInput) || (!isMouseWheelAllowed && key.IsMouseWheelInput) || (!isControllerAllowed && key.IsControllerInput))
		{
			return false;
		}
		return true;
	}

	private bool CheckModifiers()
	{
		bool flag = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
		bool flag2 = Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt);
		bool flag3 = Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
		bool flag4 = true;
		bool flag5 = true;
		bool flag6 = true;
		if (_modifiers.HasAnyFlag(Modifiers.Control))
		{
			flag4 = flag;
		}
		if (_modifiers.HasAnyFlag(Modifiers.Alt))
		{
			flag5 = flag2;
		}
		if (_modifiers.HasAnyFlag(Modifiers.Shift))
		{
			flag6 = flag3;
		}
		if (_negativeModifiers.HasAnyFlag(Modifiers.Control))
		{
			flag4 = !flag;
		}
		if (_negativeModifiers.HasAnyFlag(Modifiers.Alt))
		{
			flag5 = !flag2;
		}
		if (_negativeModifiers.HasAnyFlag(Modifiers.Shift))
		{
			flag6 = !flag3;
		}
		return flag4 && flag5 && flag6;
	}

	private bool IsDown(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if (!IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			return false;
		}
		if (_modifiers != Modifiers.None && !CheckModifiers())
		{
			return false;
		}
		return key.IsDown();
	}

	internal bool IsDown(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		foreach (Key key in Keys)
		{
			if (IsDown(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsDownImmediate(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if (!IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			return false;
		}
		if (_modifiers != Modifiers.None && !CheckModifiers())
		{
			return false;
		}
		return key.IsDownImmediate();
	}

	internal bool IsDownImmediate(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		foreach (Key key in Keys)
		{
			if (IsDownImmediate(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsDoublePressed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if (!IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			return false;
		}
		if (_modifiers != Modifiers.None && !CheckModifiers())
		{
			return false;
		}
		if (key.IsPressed())
		{
			if (_isDoublePressActive)
			{
				_doublePressTime = 0;
				return true;
			}
			_doublePressTime = Environment.TickCount;
		}
		return false;
	}

	internal bool IsDoublePressed(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		foreach (Key key in Keys)
		{
			if (IsDoublePressed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsPressed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if (!IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			return false;
		}
		if (_modifiers != Modifiers.None && !CheckModifiers())
		{
			return false;
		}
		return key.IsPressed();
	}

	internal bool IsPressed(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		foreach (Key key in Keys)
		{
			if (IsPressed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReleased(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if (!IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			return false;
		}
		if (_modifiers != Modifiers.None && !CheckModifiers())
		{
			return false;
		}
		return key.IsReleased();
	}

	internal bool IsReleased(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		foreach (Key key in Keys)
		{
			if (IsReleased(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasModifier(Modifiers modifier)
	{
		return _modifiers.HasAnyFlag(modifier);
	}

	public bool HasSameModifiers(HotKey other)
	{
		return _modifiers == other._modifiers;
	}

	public override string ToString()
	{
		string result = "";
		bool isGamepadActive = Input.IsGamepadActive;
		for (int i = 0; i < Keys.Count; i++)
		{
			if ((!isGamepadActive && !Keys[i].IsControllerInput) || (isGamepadActive && Keys[i].IsControllerInput))
			{
				return Keys[i].ToString();
			}
		}
		return result;
	}

	public override bool Equals(object obj)
	{
		if (obj is HotKey hotKey && hotKey.Id.Equals(Id) && hotKey.GroupId.Equals(GroupId))
		{
			return hotKey.Keys.SequenceEqual(Keys);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
