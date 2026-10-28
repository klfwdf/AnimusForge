using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialArrowWidget : Widget
{
	private float _localWidth;

	private float _localHeight;

	private bool _isDirectionDown;

	private bool _isDirectionRight;

	private float _startTime;

	public bool IsArrowEnabled { get; set; }

	public float FadeInTime { get; set; } = 1f;

	public float BigCircleRadius { get; set; } = 2f;

	public float SmallCircleRadius { get; set; } = 2f;

	public TutorialArrowWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsArrowEnabled)
		{
			base.IsVisible = true;
			base.ScaledSuggestedWidth = _localWidth;
			base.ScaledSuggestedHeight = _localHeight;
			if (_startTime > -1f)
			{
				float alphaFactor = Mathf.Lerp(0f, 1f, Mathf.Clamp((base.EventManager.Time - _startTime) / FadeInTime, 0f, 1f));
				this.SetGlobalAlphaRecursively(alphaFactor);
			}
			else
			{
				this.SetGlobalAlphaRecursively(0f);
			}
		}
		else
		{
			base.IsVisible = false;
			this.SetGlobalAlphaRecursively(0f);
		}
	}

	public void SetArrowProperties(float width, float height, bool isDirectionDown, bool isDirectionRight)
	{
		if (_localWidth != width || _localHeight != height || _isDirectionDown != isDirectionDown || _isDirectionRight != isDirectionRight)
		{
			RemoveAllChildren();
			float num = (float)Math.Sqrt(width * width + height * height);
			float num2 = (BigCircleRadius + SmallCircleRadius) / 2f;
			int num3 = (int)(num / num2);
			float num4 = 0f;
			float start = 0f;
			float num5 = 0f;
			float num6 = 0f;
			if (isDirectionDown)
			{
				num5 = width;
				num6 = height;
			}
			else
			{
				num5 = width;
				start = height;
				num6 = 0f;
			}
			float start2 = (isDirectionRight ? BigCircleRadius : SmallCircleRadius);
			float end = (isDirectionRight ? SmallCircleRadius : BigCircleRadius);
			for (int i = 0; i < num3; i++)
			{
				Widget defaultCircleWidgetTemplate = GetDefaultCircleWidgetTemplate();
				AddChild(defaultCircleWidgetTemplate);
				float amount = num2 * (float)i / TaleWorlds.Library.MathF.Abs(num4 - num5);
				float num7 = Mathf.Lerp(start2, end, amount);
				defaultCircleWidgetTemplate.PositionXOffset = Mathf.Lerp(num4, num5, amount);
				defaultCircleWidgetTemplate.PositionYOffset = Mathf.Lerp(start, num6, amount);
				defaultCircleWidgetTemplate.SuggestedHeight = num7;
				defaultCircleWidgetTemplate.SuggestedWidth = num7;
			}
			_localWidth = width;
			_localHeight = height;
			_isDirectionDown = isDirectionDown;
			_isDirectionRight = isDirectionRight;
		}
	}

	public void ResetFade()
	{
		_startTime = base.EventManager.Time;
	}

	public void DisableFade()
	{
		_startTime = base.EventManager.Time;
	}

	private Widget GetDefaultCircleWidgetTemplate()
	{
		return new Widget(base.Context)
		{
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.Fixed,
			Sprite = base.Context.SpriteData.GetSprite("BlankWhiteCircle"),
			IsEnabled = false
		};
	}
}
