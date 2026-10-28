using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options.Gamepad;

public class OptionsGamepadCategoryWidget : Widget
{
	private bool _initalized;

	private int _currentGamepadType = -1;

	public Widget Playstation4LayoutParentWidget { get; set; }

	public Widget Playstation5LayoutParentWidget { get; set; }

	public Widget XboxLayoutParentWidget { get; set; }

	public int CurrentGamepadType
	{
		get
		{
			return _currentGamepadType;
		}
		set
		{
			if (_currentGamepadType != value)
			{
				_currentGamepadType = value;
			}
		}
	}

	public OptionsGamepadCategoryWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initalized)
		{
			SetGamepadLayoutVisibility(CurrentGamepadType);
			_initalized = true;
		}
	}

	private void SetGamepadLayoutVisibility(int gamepadType)
	{
		XboxLayoutParentWidget.IsVisible = false;
		Playstation4LayoutParentWidget.IsVisible = false;
		Playstation5LayoutParentWidget.IsVisible = false;
		switch (gamepadType)
		{
		case 0:
			XboxLayoutParentWidget.IsVisible = true;
			break;
		case 1:
			Playstation4LayoutParentWidget.IsVisible = true;
			break;
		case 2:
			Playstation5LayoutParentWidget.IsVisible = true;
			break;
		default:
			XboxLayoutParentWidget.IsVisible = true;
			Debug.FailedAssert("This kind of gamepad is not visually supported", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Options\\Gamepad\\OptionsGamepadCategoryWidget.cs", "SetGamepadLayoutVisibility", 47);
			break;
		}
	}
}
