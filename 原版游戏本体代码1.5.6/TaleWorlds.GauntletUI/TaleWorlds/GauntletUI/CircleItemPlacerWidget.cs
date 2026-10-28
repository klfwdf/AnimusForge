using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class CircleItemPlacerWidget : Widget
{
	private float _centerDistanceAnimationTimer;

	private float _centerDistanceAnimationDuration;

	private float _centerDistanceAnimationInitialValue;

	private float _centerDistanceAnimationTarget;

	public float DistanceFromCenterModifier { get; set; } = 300f;

	public Widget DirectionWidget { get; set; }

	public float DirectionWidgetDistanceMultiplier { get; set; } = 0.5f;

	public bool ActivateOnlyWithController { get; set; }

	public CircleItemPlacerWidget(UIContext context)
		: base(context)
	{
		_centerDistanceAnimationTimer = -1f;
		_centerDistanceAnimationDuration = -1f;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsRecursivelyVisible())
		{
			UpdateItemPlacement();
			AnimateDistanceFromCenter(dt);
		}
	}

	private void AnimateDistanceFromCenter(float dt)
	{
		if (_centerDistanceAnimationTimer != -1f && _centerDistanceAnimationDuration != -1f && _centerDistanceAnimationTarget != -1f)
		{
			if (_centerDistanceAnimationTimer < _centerDistanceAnimationDuration)
			{
				DistanceFromCenterModifier = TaleWorlds.Library.MathF.Lerp(_centerDistanceAnimationInitialValue, _centerDistanceAnimationTarget, _centerDistanceAnimationTimer / _centerDistanceAnimationDuration);
				_centerDistanceAnimationTimer += dt;
				return;
			}
			DistanceFromCenterModifier = _centerDistanceAnimationTarget;
			_centerDistanceAnimationTimer = -1f;
			_centerDistanceAnimationDuration = -1f;
			_centerDistanceAnimationTarget = -1f;
		}
	}

	public void AnimateDistanceFromCenterTo(float distanceFromCenter, float animationDuration)
	{
		_centerDistanceAnimationTimer = 0f;
		_centerDistanceAnimationInitialValue = DistanceFromCenterModifier;
		_centerDistanceAnimationDuration = animationDuration;
		_centerDistanceAnimationTarget = distanceFromCenter;
	}

	private void UpdateItemPlacement()
	{
		if (base.ChildCount > 0)
		{
			int childCount = base.ChildCount;
			float num = 360f / (float)childCount;
			float num2 = 0f - num / 2f;
			if (num2 < 0f)
			{
				num2 += 360f;
			}
			for (int i = 0; i < base.ChildCount; i++)
			{
				float angle = num * (float)i;
				float angle2 = AddAngle(num2, angle);
				angle2 = AddAngle(angle2, num / 2f);
				Vec2 vec = DirFromAngle(angle2 * (System.MathF.PI / 180f));
				Widget child = GetChild(i);
				child.PositionXOffset = vec.X * DistanceFromCenterModifier;
				child.PositionYOffset = vec.Y * DistanceFromCenterModifier * -1f;
			}
		}
	}

	private float AddAngle(float angle1, float angle2)
	{
		float num = angle1 + angle2;
		if (num < 0f)
		{
			num += 360f;
		}
		return num % 360f;
	}

	private bool IsAngleBetweenAngles(float angle, float minAngle, float maxAngle)
	{
		float num = angle - System.MathF.PI;
		float num2 = minAngle - System.MathF.PI;
		float num3 = maxAngle - System.MathF.PI;
		if (num2 == num3)
		{
			return true;
		}
		float num4 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num3, num2));
		if (num4.ApproximatelyEqualsTo(System.MathF.PI))
		{
			return num < num3;
		}
		float num5 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num, num2));
		float num6 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num, num3));
		if (num5 < num4)
		{
			return num6 < num4;
		}
		return false;
	}

	private float AngleFromDir(Vec2 directionVector)
	{
		if (directionVector.X < 0f)
		{
			return 360f - (float)Math.Atan2(directionVector.X, directionVector.Y) * 57.29578f * -1f;
		}
		return (float)Math.Atan2(directionVector.X, directionVector.Y) * 57.29578f;
	}

	private Vec2 DirFromAngle(float angle)
	{
		return new Vec2(TaleWorlds.Library.MathF.Sin(angle), TaleWorlds.Library.MathF.Cos(angle));
	}
}
