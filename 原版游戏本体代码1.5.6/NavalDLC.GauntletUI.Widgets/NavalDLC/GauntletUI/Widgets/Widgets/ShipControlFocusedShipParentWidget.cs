using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class ShipControlFocusedShipParentWidget : Widget
{
	private int _wSign;

	private Vec2 _position;

	[DataSourceProperty]
	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (_wSign != value)
			{
				_wSign = value;
				OnPropertyChanged(value, "WSign");
			}
		}
	}

	[DataSourceProperty]
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

	public ShipControlFocusedShipParentWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsVisible)
		{
			UpdateScreenPosition();
		}
	}

	private void UpdateScreenPosition()
	{
		float num = Position.X - base.Size.X / 2f;
		float num2 = Position.X + base.Size.X / 2f;
		float num3 = Position.Y - base.Size.Y;
		float y = Position.Y;
		if (WSign <= 0 || !(num > 0f) || !(num2 < base.Context.EventManager.PageSize.X) || !(num3 > 0f) || !(y < base.Context.EventManager.PageSize.Y))
		{
			Vec2 vec = new Vec2(num, num3);
			Vector2 vector = base.Context.EventManager.PageSize - base.Size;
			Vec2 vec2 = vector / 2f;
			vec -= vec2;
			if (WSign < 0)
			{
				vec *= -1f;
			}
			float radian = Mathf.Atan2(vec.y, vec.x) - System.MathF.PI / 2f;
			float num4 = Mathf.Cos(radian);
			float num5 = Mathf.Sin(radian);
			float num6 = num4 / num5;
			Vec2 vec3 = vec2 * 1f;
			vec = ((num4 > 0f) ? new Vec2((0f - vec3.y) / num6, vec2.y) : new Vec2(vec3.y / num6, 0f - vec2.y));
			if (vec.x > vec3.x)
			{
				vec = new Vec2(vec3.x, (0f - vec3.x) * num6);
			}
			else if (vec.x < 0f - vec3.x)
			{
				vec = new Vec2(0f - vec3.x, vec3.x * num6);
			}
			vec += vec2;
			base.ScaledPositionXOffset = Mathf.Clamp(vec.x, 0f, vector.X);
			base.ScaledPositionYOffset = Mathf.Clamp(vec.y, 0f, vector.Y);
		}
		else
		{
			base.ScaledPositionXOffset = num;
			base.ScaledPositionYOffset = num3;
		}
	}
}
