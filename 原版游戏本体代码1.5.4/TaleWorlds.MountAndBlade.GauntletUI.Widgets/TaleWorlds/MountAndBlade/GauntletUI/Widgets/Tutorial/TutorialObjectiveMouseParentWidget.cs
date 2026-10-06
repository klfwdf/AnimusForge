using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialObjectiveMouseParentWidget : Widget
{
	public enum MovementTypes
	{
		None,
		MoveLeft,
		MoveRight,
		MoveUp,
		MoveDown
	}

	private bool _animationsSet;

	private string _keyId;

	private int _movementType;

	public BrushWidget MouseBodyWidget { get; set; }

	public BrushWidget MouseLeftClickWidget { get; set; }

	public BrushWidget MouseRightClickWidget { get; set; }

	public BrushWidget MouseMiddleClickWidget { get; set; }

	[Editor(false)]
	public string KeyId
	{
		get
		{
			return _keyId;
		}
		set
		{
			if (value != _keyId)
			{
				_keyId = value;
				DecideClick();
				OnPropertyChanged(value, "KeyId");
			}
		}
	}

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
				DecideMovement();
				OnPropertyChanged(value, "MovementType");
			}
		}
	}

	public TutorialObjectiveMouseParentWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_animationsSet || MouseBodyWidget == null || MouseLeftClickWidget == null || MouseRightClickWidget == null || MouseMiddleClickWidget == null)
		{
			return;
		}
		_animationsSet = true;
		BrushAnimation animation = MouseLeftClickWidget.Brush.GetAnimation("BlinkAnimation");
		foreach (BrushAnimation animation2 in MouseLeftClickWidget.Brush.GetAnimations())
		{
			if (animation2 == animation)
			{
				continue;
			}
			foreach (BrushLayerAnimation layerAnimation in animation.GetLayerAnimations())
			{
				layerAnimation.Collections.ToList().ForEach(delegate(BrushAnimationProperty x)
				{
					animation2.AddAnimationProperty(x);
				});
			}
		}
		foreach (BrushAnimation animation3 in MouseRightClickWidget.Brush.GetAnimations())
		{
			if (animation3 == animation)
			{
				continue;
			}
			foreach (BrushLayerAnimation layerAnimation2 in animation.GetLayerAnimations())
			{
				layerAnimation2.Collections.ToList().ForEach(delegate(BrushAnimationProperty x)
				{
					animation3.AddAnimationProperty(x);
				});
			}
		}
		foreach (BrushAnimation animation4 in MouseMiddleClickWidget.Brush.GetAnimations())
		{
			if (animation4 == animation)
			{
				continue;
			}
			foreach (BrushLayerAnimation layerAnimation3 in animation.GetLayerAnimations())
			{
				layerAnimation3.Collections.ToList().ForEach(delegate(BrushAnimationProperty x)
				{
					animation4.AddAnimationProperty(x);
				});
			}
		}
	}

	private void DecideMovement()
	{
		switch (MovementType)
		{
		case 2:
			MouseBodyWidget.SetState("Right");
			break;
		case 1:
			MouseBodyWidget.SetState("Left");
			break;
		case 3:
			MouseBodyWidget.SetState("Up");
			break;
		case 4:
			MouseBodyWidget.SetState("Down");
			break;
		default:
			MouseBodyWidget.SetState("Default");
			break;
		}
	}

	private void DecideClick()
	{
		switch (KeyId)
		{
		case "mouse_left_click":
			MouseLeftClickWidget.IsVisible = true;
			MouseMiddleClickWidget.IsVisible = false;
			MouseRightClickWidget.IsVisible = false;
			break;
		case "mouse_middle_click":
			MouseLeftClickWidget.IsVisible = false;
			MouseMiddleClickWidget.IsVisible = true;
			MouseRightClickWidget.IsVisible = false;
			break;
		case "mouse_right_click":
			MouseLeftClickWidget.IsVisible = false;
			MouseMiddleClickWidget.IsVisible = false;
			MouseRightClickWidget.IsVisible = true;
			break;
		default:
			MouseLeftClickWidget.IsVisible = false;
			MouseMiddleClickWidget.IsVisible = false;
			MouseRightClickWidget.IsVisible = false;
			break;
		}
	}
}
