using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapEvents;

public class MapEventVisualItemWidget : Widget
{
	private Vec2 _position;

	private bool _isVisibleOnMap;

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	public bool IsVisibleOnMap
	{
		get
		{
			return _isVisibleOnMap;
		}
		set
		{
			if (_isVisibleOnMap != value)
			{
				_isVisibleOnMap = value;
				OnPropertyChanged(value, "IsVisibleOnMap");
			}
		}
	}

	public MapEventVisualItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		UpdatePosition();
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		base.IsVisible = IsVisibleOnMap;
	}

	private void UpdatePosition()
	{
		if (IsVisibleOnMap)
		{
			base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = Position.y - base.Size.Y;
		}
		else
		{
			base.ScaledPositionXOffset = -10000f;
			base.ScaledPositionYOffset = -10000f;
		}
	}
}
