namespace TaleWorlds.InputSystem;

public class GameKey
{
	public int Id { get; private set; }

	public string StringId { get; private set; }

	public string GroupId { get; private set; }

	public string MainCategoryId { get; private set; }

	public Key KeyboardKey { get; internal set; }

	public Key DefaultKeyboardKey { get; private set; }

	public Key ControllerKey { get; internal set; }

	public Key DefaultControllerKey { get; internal set; }

	public GameKey(int id, string stringId, string groupId, InputKey defaultKeyboardKey, InputKey defaultControllerKey, string mainCategoryId = "")
	{
		Id = id;
		StringId = stringId;
		GroupId = groupId;
		MainCategoryId = mainCategoryId;
		KeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
		DefaultKeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
		ControllerKey = ((defaultControllerKey != InputKey.Invalid) ? new Key(defaultControllerKey) : null);
		DefaultControllerKey = ((defaultControllerKey != InputKey.Invalid) ? new Key(defaultControllerKey) : null);
	}

	public GameKey(int id, string stringId, string groupId, InputKey defaultKeyboardKey, string mainCategoryId = "")
	{
		Id = id;
		StringId = stringId;
		GroupId = groupId;
		MainCategoryId = mainCategoryId;
		KeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
		DefaultKeyboardKey = ((defaultKeyboardKey != InputKey.Invalid) ? new Key(defaultKeyboardKey) : null);
		ControllerKey = new Key(InputKey.Invalid);
		DefaultControllerKey = new Key(InputKey.Invalid);
	}

	private bool IsKeyAllowed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if ((!isKeysAllowed && key.IsKeyboardInput) || (!isMouseButtonAllowed && key.IsMouseButtonInput) || (!isMouseWheelAllowed && key.IsMouseWheelInput) || (!isControllerAllowed && key.IsControllerInput))
		{
			return false;
		}
		return true;
	}

	internal bool IsUp(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		bool flag = false;
		if (KeyboardKey != null && IsKeyAllowed(KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || !KeyboardKey.IsDown();
		}
		if (ControllerKey != null && IsKeyAllowed(ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || !ControllerKey.IsDown();
		}
		return flag;
	}

	internal bool IsDown(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed, bool checkControllerKey = true)
	{
		bool flag = false;
		if (KeyboardKey != null && IsKeyAllowed(KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || KeyboardKey.IsDown();
		}
		if (checkControllerKey && ControllerKey != null && IsKeyAllowed(ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || ControllerKey.IsDown();
		}
		return flag;
	}

	internal bool IsDownImmediate(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		bool flag = false;
		if (KeyboardKey != null && IsKeyAllowed(KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || KeyboardKey.IsDownImmediate();
		}
		if (ControllerKey != null && IsKeyAllowed(ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || ControllerKey.IsDownImmediate();
		}
		return flag;
	}

	internal bool IsPressed(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		bool flag = false;
		if (KeyboardKey != null && IsKeyAllowed(KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || KeyboardKey.IsPressed();
		}
		if (ControllerKey != null && IsKeyAllowed(ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || ControllerKey.IsPressed();
		}
		return flag;
	}

	internal bool IsReleased(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		bool flag = false;
		if (KeyboardKey != null && IsKeyAllowed(KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || KeyboardKey.IsReleased();
		}
		if (ControllerKey != null && IsKeyAllowed(ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			flag = flag || ControllerKey.IsReleased();
		}
		return flag;
	}

	internal float GetKeyState(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		float num = 0f;
		if (KeyboardKey != null && IsKeyAllowed(KeyboardKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			num = KeyboardKey.GetKeyState().X;
		}
		if (num == 0f && ControllerKey != null && IsKeyAllowed(ControllerKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			num = ControllerKey.GetKeyState().X;
		}
		return num;
	}

	public override string ToString()
	{
		string result = "invalid";
		bool isGamepadActive = Input.IsGamepadActive;
		if (!isGamepadActive && KeyboardKey != null)
		{
			result = KeyboardKey.ToString();
		}
		else if (isGamepadActive && ControllerKey != null)
		{
			result = ControllerKey.ToString();
		}
		return result;
	}

	public override bool Equals(object obj)
	{
		if (obj is GameKey { Id: var id } gameKey && id.Equals(Id) && gameKey.GroupId.Equals(GroupId) && gameKey.KeyboardKey == KeyboardKey)
		{
			return gameKey.ControllerKey == ControllerKey;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
