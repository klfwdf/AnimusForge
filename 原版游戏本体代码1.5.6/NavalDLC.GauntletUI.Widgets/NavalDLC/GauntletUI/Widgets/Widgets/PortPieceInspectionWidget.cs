using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortPieceInspectionWidget : BrushWidget
{
	private PortInspectionParentWidget _targetPiece;

	private float _fadeInOutDelta;

	private float _currentAlpha;

	private bool _isInspected;

	private float _animationSpeed;

	private float _fadeInOutDuration;

	private float _fadeOutDelay;

	private float _offsetFromTarget;

	private Widget _topFrameWidget;

	[Editor(false)]
	public bool IsInspected
	{
		get
		{
			return _isInspected;
		}
		set
		{
			if (value != _isInspected)
			{
				_isInspected = value;
				OnPropertyChanged(value, "IsInspected");
			}
		}
	}

	[Editor(false)]
	public float AnimationSpeed
	{
		get
		{
			return _animationSpeed;
		}
		set
		{
			if (value != _animationSpeed)
			{
				_animationSpeed = value;
				OnPropertyChanged(value, "AnimationSpeed");
			}
		}
	}

	[Editor(false)]
	public float FadeInOutDuration
	{
		get
		{
			return _fadeInOutDuration;
		}
		set
		{
			if (value != _fadeInOutDuration)
			{
				_fadeInOutDuration = value;
				OnPropertyChanged(value, "FadeInOutDuration");
			}
		}
	}

	[Editor(false)]
	public float FadeOutDelay
	{
		get
		{
			return _fadeOutDelay;
		}
		set
		{
			if (value != _fadeOutDelay)
			{
				_fadeOutDelay = value;
				OnPropertyChanged(value, "FadeOutDelay");
			}
		}
	}

	[Editor(false)]
	public float OffsetFromTarget
	{
		get
		{
			return _offsetFromTarget;
		}
		set
		{
			if (value != _offsetFromTarget)
			{
				_offsetFromTarget = value;
				OnPropertyChanged(value, "OffsetFromTarget");
			}
		}
	}

	[Editor(false)]
	public Widget TopFrameWidget
	{
		get
		{
			return _topFrameWidget;
		}
		set
		{
			if (value != _topFrameWidget)
			{
				_topFrameWidget = value;
				OnPropertyChanged(value, "TopFrameWidget");
			}
		}
	}

	public PortPieceInspectionWidget(UIContext context)
		: base(context)
	{
		this.SetGlobalAlphaRecursively(0f);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_targetPiece != null)
		{
			UpdateAnimation(dt);
		}
		HandleAlphaFactor(dt);
	}

	private void HandleAlphaFactor(float dt)
	{
		bool flag = _targetPiece != null && IsInspected;
		if (FadeInOutDuration <= 0f)
		{
			_currentAlpha = (flag ? 1f : 0f);
		}
		else
		{
			if (flag)
			{
				_fadeInOutDelta += dt;
			}
			else
			{
				_fadeInOutDelta -= dt;
			}
			_fadeInOutDelta = MathF.Clamp(_fadeInOutDelta, 0f, FadeInOutDuration + FadeOutDelay);
			float ratio = MathF.Clamp(_fadeInOutDelta / FadeInOutDuration, 0f, 1f);
			float amount = AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseInOut, AnimationInterpolation.Function.Cubic, ratio);
			_currentAlpha = MathF.Lerp(0f, 1f, amount);
		}
		this.SetGlobalAlphaRecursively(_currentAlpha);
	}

	private void UpdateAnimation(float dt)
	{
		bool num = base.PositionXOffset == 0f && base.PositionYOffset == 0f;
		base.VerticalAlignment = VerticalAlignment.Top;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		float amount = ((AnimationSpeed != 0f) ? MBMath.ClampFloat(AnimationSpeed * dt, 0f, 1f) : 1f);
		Vector2 center = _targetPiece.AreaRect.GetCenter();
		Vector2 value = new Vector2(base.PositionXOffset, base.PositionYOffset);
		Vector2 value2 = center * base._inverseScaleToUse + new Vector2(OffsetFromTarget, (0f - base.Size.Y) * base._inverseScaleToUse * 0.5f);
		Vector2 vector = Vector2.Lerp(value, value2, amount);
		base.PositionXOffset = vector.X;
		base.PositionYOffset = ClampYPosition(vector.Y);
		Vector2 vector2 = center * base._inverseScaleToUse;
		float num2 = ClampYPosition(value2.Y);
		float num3 = AreaRect.GetBoundingBox().Y - TopFrameWidget.AreaRect.GetBoundingBox().Y;
		float num4 = vector2.Y - num2 + num3;
		TopFrameWidget.SuggestedHeight = MathF.Max(0f, MathF.Lerp(TopFrameWidget.SuggestedHeight, num4, amount));
		if (num)
		{
			base.PositionXOffset = value2.X;
			base.PositionYOffset = ClampYPosition(value2.Y);
			TopFrameWidget.SuggestedHeight = MathF.Max(0f, num4);
		}
	}

	private float ClampYPosition(float positionToClamp)
	{
		return MBMath.ClampFloat(positionToClamp, 0f, (base.EventManager.PageSize.Y - base.Size.Y) * base._inverseScaleToUse - 70f);
	}

	public void SetTargetPiece(PortInspectionParentWidget targetPiece)
	{
		if (_targetPiece != targetPiece && IsInspected)
		{
			_targetPiece = targetPiece;
		}
	}
}
