using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialObjectiveItemWidget : Widget
{
	public enum InputTypes
	{
		MouseAndClick,
		Key,
		ControllerStick
	}

	private int _movementType;

	private int _inputType;

	public InputKeyVisualWidget KeyPressWidget { get; set; }

	public TutorialObjectiveMouseParentWidget MouseMoveWidget { get; set; }

	public TutorialObjectiveStickParentWidget StickMoveWidget { get; set; }

	[Editor(false)]
	public int MovementType
	{
		get
		{
			return _movementType;
		}
		set
		{
			if (value != _movementType)
			{
				_movementType = value;
				OnPropertyChanged(value, "MovementType");
			}
		}
	}

	[Editor(false)]
	public int InputType
	{
		get
		{
			return _inputType;
		}
		set
		{
			if (value != _inputType)
			{
				_inputType = value;
				OnPropertyChanged(value, "InputType");
			}
		}
	}

	public TutorialObjectiveItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		KeyPressWidget.IsVisible = InputType == 1;
		MouseMoveWidget.IsVisible = InputType == 0;
		StickMoveWidget.IsVisible = InputType == 2;
	}
}
