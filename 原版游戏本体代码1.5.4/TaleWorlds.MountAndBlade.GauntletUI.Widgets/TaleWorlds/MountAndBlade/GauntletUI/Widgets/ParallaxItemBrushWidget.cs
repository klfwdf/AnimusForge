using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ParallaxItemBrushWidget : BrushWidget
{
	public enum ParallaxMovementDirection
	{
		None,
		Left,
		Right,
		Up,
		Down
	}

	private bool _initialized;

	public bool IsEaseInOutEnabled { get; set; } = true;

	public float OneDirectionDuration { get; set; } = 1f;

	public float OneDirectionDistance { get; set; } = 1f;

	public ParallaxMovementDirection InitialDirection { get; set; }

	private float _centerOffset => OneDirectionDuration / 2f;

	private float _localTime => base.Context.EventManager.Time + _centerOffset;

	public ParallaxItemBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			OneDirectionDuration = TaleWorlds.Library.MathF.Max(float.Epsilon, OneDirectionDuration);
			_initialized = true;
		}
		if (InitialDirection != ParallaxMovementDirection.None)
		{
			bool flag = _localTime % (OneDirectionDuration * 4f) > OneDirectionDuration * 2f;
			float num = 0f;
			if (IsEaseInOutEnabled)
			{
				_ = _localTime % (OneDirectionDuration * 4f);
				_ = OneDirectionDuration;
				float t = TaleWorlds.Library.MathF.PingPong(0f, OneDirectionDuration * 4f, _localTime) / (OneDirectionDuration * 4f);
				float quadEaseInOut = GetQuadEaseInOut(t);
				num = TaleWorlds.Library.MathF.Lerp(0f - OneDirectionDistance, OneDirectionDistance, quadEaseInOut);
			}
			else
			{
				float num2 = TaleWorlds.Library.MathF.PingPong(0f, OneDirectionDuration, _localTime) / OneDirectionDuration;
				num = OneDirectionDistance * num2;
				num = (flag ? (0f - num) : num);
			}
			switch (InitialDirection)
			{
			case ParallaxMovementDirection.Left:
				base.PositionXOffset = num;
				break;
			case ParallaxMovementDirection.Right:
				base.PositionXOffset = 0f - num;
				break;
			case ParallaxMovementDirection.Up:
				base.PositionYOffset = 0f - num;
				break;
			case ParallaxMovementDirection.Down:
				base.PositionYOffset = num;
				break;
			}
		}
	}

	private float GetCubicEaseInOut(float t)
	{
		if (t < 0.5f)
		{
			return 4f * t * t * t;
		}
		float num = 2f * t - 2f;
		return 0.5f * num * num * num + 1f;
	}

	private float GetElasticEaseInOut(float t)
	{
		if (t < 0.5f)
		{
			return (float)(0.5 * Math.Sin(20.420352248333657 * (double)(2f * t)) * Math.Pow(2.0, 10f * (2f * t - 1f)));
		}
		return (float)(0.5 * (Math.Sin(-20.420352248333657 * (double)(2f * t - 1f + 1f)) * Math.Pow(2.0, -10f * (2f * t - 1f)) + 2.0));
	}

	private float ExponentialEaseInOut(float t)
	{
		if (t == 0f || t == 1f)
		{
			return t;
		}
		if (t < 0.5f)
		{
			return (float)(0.5 * Math.Pow(2.0, 20f * t - 10f));
		}
		return (float)(-0.5 * Math.Pow(2.0, -20f * t + 10f) + 1.0);
	}

	private float GetQuadEaseInOut(float t)
	{
		if (t < 0.5f)
		{
			return 2f * t * t;
		}
		return -2f * t * t + 4f * t - 1f;
	}
}
