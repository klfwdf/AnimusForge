using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class AgentAlarmStateWidget : Widget
{
	private string _alarmState;

	private int _wSign;

	private Vec2 _position;

	public string AlarmState
	{
		get
		{
			return _alarmState;
		}
		set
		{
			if (_alarmState != value)
			{
				_alarmState = value;
				OnPropertyChanged(value, "AlarmState");
			}
		}
	}

	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (value != _wSign)
			{
				_wSign = value;
				OnPropertyChanged(value, "WSign");
			}
		}
	}

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (value != _position)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	public AgentAlarmStateWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdatePosition();
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		if (AlarmState != null)
		{
			SetState(AlarmState);
		}
	}

	private void UpdatePosition()
	{
		float num = Position.X - base.Size.X / 2f;
		float num2 = Position.X + base.Size.X / 2f;
		float num3 = Position.Y - base.Size.Y / 2f;
		float num4 = Position.Y + base.Size.Y / 2f;
		bool flag = WSign > 0 && num > 0f && num2 < base.Context.EventManager.PageSize.X && num3 > 0f && num4 < base.Context.EventManager.PageSize.Y;
		bool flag2 = WSign > 0 && (num2 > 0f || num < base.Context.EventManager.PageSize.X) && (num4 > 0f || num3 < base.Context.EventManager.PageSize.Y);
		if (!flag)
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
			float num5 = Mathf.Cos(radian);
			float num6 = Mathf.Sin(radian);
			float num7 = num5 / num6;
			Vec2 vec3 = vec2 * 1f;
			vec = ((num5 > 0f) ? new Vec2((0f - vec3.y) / num7, vec2.y) : new Vec2(vec3.y / num7, 0f - vec2.y));
			if (vec.x > vec3.x)
			{
				vec = new Vec2(vec3.x, (0f - vec3.x) * num7);
			}
			else if (vec.x < 0f - vec3.x)
			{
				vec = new Vec2(0f - vec3.x, vec3.x * num7);
			}
			vec += vec2;
			base.ScaledPositionXOffset = Mathf.Clamp(vec.x, 0f, vector.X);
			base.ScaledPositionYOffset = Mathf.Clamp(vec.y, 0f, vector.Y);
		}
		else if (flag || flag2)
		{
			base.ScaledPositionXOffset = num;
			base.ScaledPositionYOffset = num3;
		}
	}
}
