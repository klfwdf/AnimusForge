using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Barter;

public class BarterItemCountControlButtonWidget : ButtonWidget
{
	private float _clickStartTime;

	private float _totalTime;

	public float IncreaseToHoldDelay { get; set; } = 1f;

	public BarterItemCountControlButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		_totalTime += dt;
		if (base.IsPressed && _clickStartTime + IncreaseToHoldDelay < _totalTime)
		{
			EventFired("MoveOne");
		}
	}

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		_clickStartTime = _totalTime;
		EventFired("MoveOne");
	}

	protected override void OnMouseReleased(bool isFromInput)
	{
		base.OnMouseReleased(isFromInput);
		_clickStartTime = 0f;
	}
}
