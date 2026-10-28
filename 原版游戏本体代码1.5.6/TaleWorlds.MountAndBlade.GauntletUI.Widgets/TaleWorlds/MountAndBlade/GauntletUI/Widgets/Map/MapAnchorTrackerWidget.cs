using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;

public class MapAnchorTrackerWidget : Widget
{
	private float _positionX;

	private float _positionY;

	private float _positionW;

	[Editor(false)]
	public float PositionX
	{
		get
		{
			return _positionX;
		}
		set
		{
			if (value != _positionX)
			{
				_positionX = value;
				OnPropertyChanged(value, "PositionX");
			}
		}
	}

	[Editor(false)]
	public float PositionY
	{
		get
		{
			return _positionY;
		}
		set
		{
			if (value != _positionY)
			{
				_positionY = value;
				OnPropertyChanged(value, "PositionY");
			}
		}
	}

	[Editor(false)]
	public float PositionW
	{
		get
		{
			return _positionW;
		}
		set
		{
			if (value != _positionW)
			{
				_positionW = value;
				OnPropertyChanged(value, "PositionW");
			}
		}
	}

	public MapAnchorTrackerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsRecursivelyVisible())
		{
			float num = PositionX - base.Size.X * 0.5f;
			float num2 = PositionY - base.Size.Y * 0.5f;
			if (num + base.Size.X >= 0f && num <= base.EventManager.PageSize.X && num2 + base.Size.Y >= 0f && num2 <= base.EventManager.PageSize.Y)
			{
				base.ScaledPositionXOffset = MathF.Clamp(num, 0f, base.EventManager.PageSize.X - base.Size.X);
				base.ScaledPositionYOffset = MathF.Clamp(num2, 0f, base.EventManager.PageSize.Y - base.Size.Y - 50f * base._scaleToUse);
			}
			else
			{
				base.ScaledPositionXOffset = -5000f;
				base.ScaledPositionYOffset = -5000f;
			}
		}
	}
}
