using TaleWorlds.GauntletUI;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ClickableCharacterTableauWidget : CharacterTableauWidget
{
	private const float DragThreshold = 5f;

	private float _dragThresholdSqr = 25f;

	private bool _isMouseDown;

	private bool _isDragging;

	private Vec2 _mousePressPos;

	public ClickableCharacterTableauWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_isMouseDown && !_isDragging && (_mousePressPos - base.EventManager.MousePosition).LengthSquared >= _dragThresholdSqr)
		{
			_isDragging = true;
			SetTextureProviderProperty("CurrentlyRotating", true);
		}
	}

	protected override void OnMousePressed()
	{
		_isMouseDown = true;
		_mousePressPos = base.EventManager.MousePosition;
	}

	protected override void OnMouseReleased(bool isFromInput)
	{
		SetTextureProviderProperty("CurrentlyRotating", false);
		if (!_isDragging && isFromInput)
		{
			EventFired("Click");
		}
		_isDragging = false;
		_isMouseDown = false;
	}
}
