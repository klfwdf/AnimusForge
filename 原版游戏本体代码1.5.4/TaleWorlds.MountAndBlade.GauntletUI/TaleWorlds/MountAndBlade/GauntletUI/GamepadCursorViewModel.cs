using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GamepadCursorViewModel : ViewModel
{
	private float _cursorPositionX = 960f;

	private float _cursorPositionY = 540f;

	private bool _isConsoleMouseVisible;

	private bool _isGamepadCursorVisible;

	[DataSourceProperty]
	public bool IsConsoleMouseVisible
	{
		get
		{
			return _isConsoleMouseVisible;
		}
		set
		{
			if (_isConsoleMouseVisible != value)
			{
				_isConsoleMouseVisible = value;
				OnPropertyChangedWithValue(value, "IsConsoleMouseVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGamepadCursorVisible
	{
		get
		{
			return _isGamepadCursorVisible;
		}
		set
		{
			if (_isGamepadCursorVisible != value)
			{
				_isGamepadCursorVisible = value;
				OnPropertyChangedWithValue(value, "IsGamepadCursorVisible");
			}
		}
	}

	[DataSourceProperty]
	public float CursorPositionX
	{
		get
		{
			return _cursorPositionX;
		}
		set
		{
			if (_cursorPositionX != value)
			{
				_cursorPositionX = value;
				OnPropertyChangedWithValue(value, "CursorPositionX");
			}
		}
	}

	[DataSourceProperty]
	public float CursorPositionY
	{
		get
		{
			return _cursorPositionY;
		}
		set
		{
			if (_cursorPositionY != value)
			{
				_cursorPositionY = value;
				OnPropertyChangedWithValue(value, "CursorPositionY");
			}
		}
	}
}
