using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public class GameAxisKey
{
	public enum AxisType
	{
		X,
		Y
	}

	public string Id { get; private set; }

	public Key AxisKey { get; internal set; }

	public Key DefaultAxisKey { get; private set; }

	public GameKey PositiveKey { get; internal set; }

	public GameKey NegativeKey { get; internal set; }

	public AxisType Type { get; private set; }

	internal bool IsBinded { get; private set; }

	public GameAxisKey(string id, InputKey axisKey, GameKey positiveKey, GameKey negativeKey, AxisType type = AxisType.X)
	{
		Id = id;
		AxisKey = new Key(axisKey);
		DefaultAxisKey = new Key(axisKey);
		PositiveKey = positiveKey;
		NegativeKey = negativeKey;
		Type = type;
		IsBinded = PositiveKey != null || NegativeKey != null;
	}

	private bool IsKeyAllowed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		if ((!isKeysAllowed && key.IsKeyboardInput) || (!isMouseButtonAllowed && key.IsMouseButtonInput) || (!isMouseWheelAllowed && key.IsMouseWheelInput) || (!isControllerAllowed && key.IsControllerInput))
		{
			return false;
		}
		return true;
	}

	public float GetAxisState(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
	{
		bool flag = PositiveKey?.IsDown(isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed, checkControllerKey: false) ?? false;
		bool flag2 = NegativeKey?.IsDown(isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed, checkControllerKey: false) ?? false;
		if (flag || flag2)
		{
			return (flag ? 1f : 0f) - (flag2 ? 1f : 0f);
		}
		Vec2 vec = new Vec2(0f, 0f);
		if (AxisKey != null && IsKeyAllowed(AxisKey, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
		{
			vec = AxisKey.GetKeyState();
		}
		if (Type == AxisType.X)
		{
			return vec.X;
		}
		if (Type == AxisType.Y)
		{
			return vec.Y;
		}
		return 0f;
	}

	public override string ToString()
	{
		string result = "";
		if (AxisKey != null)
		{
			result = AxisKey.ToString();
		}
		return result;
	}
}
