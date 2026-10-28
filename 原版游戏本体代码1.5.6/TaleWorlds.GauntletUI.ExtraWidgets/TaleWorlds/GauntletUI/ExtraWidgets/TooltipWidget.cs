using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class TooltipWidget : Widget
{
	private enum AnimationState
	{
		NotStarted,
		InProgress,
		Finished
	}

	protected int _animationDelayInFrames;

	private int _animationDelayTimerInFrames;

	private AnimationState _animationState;

	private float _animationProgress;

	private bool _lastCheckedVisibility;

	private Vector2 _lastCheckedSize;

	private float _animTime = 0.2f;

	public TooltipPositioningType PositioningType { get; set; }

	private float _tooltipOffset => 30f;

	[Editor(false)]
	public float AnimTime
	{
		get
		{
			return _animTime;
		}
		set
		{
			if (_animTime != value)
			{
				_animTime = value;
				OnPropertyChanged(value, "AnimTime");
			}
		}
	}

	public TooltipWidget(UIContext context)
		: base(context)
	{
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		_lastCheckedVisibility = true;
		base.IsVisible = true;
		PositioningType = TooltipPositioningType.FixedMouseMirrored;
		ResetAnimationProperties();
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		if (_lastCheckedVisibility != base.IsVisible)
		{
			_lastCheckedVisibility = base.IsVisible;
			if (base.IsVisible)
			{
				ResetAnimationProperties();
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_animationState == AnimationState.NotStarted)
		{
			if (_animationDelayTimerInFrames >= _animationDelayInFrames)
			{
				_animationState = AnimationState.InProgress;
			}
			else
			{
				_animationDelayTimerInFrames++;
				this.SetGlobalAlphaRecursively(0f);
			}
		}
		if (_animationState == AnimationState.NotStarted)
		{
			return;
		}
		if (_animationState == AnimationState.InProgress)
		{
			_animationProgress += ((AnimTime < 1E-05f) ? 1f : (dt / AnimTime));
			_animationProgress = MathF.Clamp(_animationProgress, 0f, 1f);
			this.SetGlobalAlphaRecursively(_animationProgress);
			if (_animationProgress >= 1f)
			{
				_animationState = AnimationState.Finished;
			}
		}
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		if (PositioningType == TooltipPositioningType.FixedMouse || PositioningType == TooltipPositioningType.FixedMouseMirrored)
		{
			if (MathF.Abs(_lastCheckedSize.X - base.Size.X) > 0.1f || MathF.Abs(_lastCheckedSize.Y - base.Size.Y) > 0.1f)
			{
				_lastCheckedSize = base.Size;
				if (PositioningType == TooltipPositioningType.FixedMouse)
				{
					SetPosition(base.EventManager.MousePosition);
				}
				else
				{
					SetMirroredPosition(base.EventManager.MousePosition);
				}
			}
		}
		else if (PositioningType == TooltipPositioningType.FollowMouse)
		{
			SetPosition(base.EventManager.MousePosition);
		}
		else if (PositioningType == TooltipPositioningType.FollowMouseMirrored)
		{
			SetMirroredPosition(base.EventManager.MousePosition);
		}
	}

	private void SetPosition(Vector2 position)
	{
		Vector2 vector = position + new Vector2(_tooltipOffset, _tooltipOffset);
		bool flag = base.Size.X > base.EventManager.PageSize.X;
		bool flag2 = base.Size.Y > base.EventManager.PageSize.Y;
		base.ScaledPositionXOffset = (flag ? vector.X : MathF.Clamp(vector.X, 0f, base.EventManager.PageSize.X - base.Size.X));
		base.ScaledPositionYOffset = (flag2 ? vector.Y : MathF.Clamp(vector.Y, 0f, base.EventManager.PageSize.Y - base.Size.Y));
	}

	private void SetMirroredPosition(Vector2 tooltipPosition)
	{
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
		VerticalAlignment verticalAlignment = VerticalAlignment.Center;
		float x = 0f;
		float y = 0f;
		if ((double)tooltipPosition.X < (double)base.EventManager.PageSize.X * 0.5)
		{
			horizontalAlignment = HorizontalAlignment.Left;
			x = _tooltipOffset;
		}
		else
		{
			horizontalAlignment = HorizontalAlignment.Right;
			tooltipPosition = new Vector2(0f - (base.EventManager.PageSize.X - tooltipPosition.X), tooltipPosition.Y);
		}
		if ((double)tooltipPosition.Y < (double)base.EventManager.PageSize.Y * 0.5)
		{
			verticalAlignment = VerticalAlignment.Top;
			y = _tooltipOffset;
		}
		else
		{
			verticalAlignment = VerticalAlignment.Bottom;
			tooltipPosition = new Vector2(tooltipPosition.X, 0f - (base.EventManager.PageSize.Y - tooltipPosition.Y));
		}
		tooltipPosition += new Vector2(x, y);
		if (base.Size.X > base.EventManager.PageSize.X)
		{
			horizontalAlignment = HorizontalAlignment.Left;
			tooltipPosition = new Vector2(0f, tooltipPosition.Y);
		}
		else
		{
			if (horizontalAlignment == HorizontalAlignment.Left && tooltipPosition.X + base.Size.X > base.EventManager.PageSize.X)
			{
				tooltipPosition += new Vector2(0f - (tooltipPosition.X + base.Size.X - base.EventManager.PageSize.X), 0f);
			}
			if (horizontalAlignment == HorizontalAlignment.Right && tooltipPosition.X - base.Size.X + base.EventManager.PageSize.X < 0f)
			{
				tooltipPosition += new Vector2(0f - (tooltipPosition.X - base.Size.X + base.EventManager.PageSize.X), 0f);
			}
		}
		if (base.Size.Y > base.EventManager.PageSize.Y)
		{
			verticalAlignment = VerticalAlignment.Top;
			tooltipPosition = new Vector2(tooltipPosition.X, 0f);
		}
		else
		{
			if (verticalAlignment == VerticalAlignment.Top && tooltipPosition.Y + base.Size.Y > base.EventManager.PageSize.Y)
			{
				tooltipPosition += new Vector2(0f, 0f - (tooltipPosition.Y + base.Size.Y - base.EventManager.PageSize.Y));
			}
			if (verticalAlignment == VerticalAlignment.Bottom && tooltipPosition.Y - base.Size.Y + base.EventManager.PageSize.Y < 0f)
			{
				tooltipPosition += new Vector2(0f, 0f - (tooltipPosition.Y - base.Size.Y + base.EventManager.PageSize.Y));
			}
		}
		base.HorizontalAlignment = horizontalAlignment;
		base.VerticalAlignment = verticalAlignment;
		base.ScaledPositionXOffset = tooltipPosition.X - base.EventManager.LeftUsableAreaStart;
		base.ScaledPositionYOffset = tooltipPosition.Y - base.EventManager.TopUsableAreaStart;
	}

	private void ResetAnimationProperties()
	{
		_animationState = AnimationState.NotStarted;
		_animationProgress = 0f;
		_animationDelayTimerInFrames = 0;
		this.SetGlobalAlphaRecursively(0f);
	}
}
