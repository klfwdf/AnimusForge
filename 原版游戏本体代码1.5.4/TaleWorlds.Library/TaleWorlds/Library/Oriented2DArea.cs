using System;

namespace TaleWorlds.Library;

public struct Oriented2DArea
{
	public struct Corners
	{
		public const int Count = 4;

		public Vec2 TopLeft { get; private set; }

		public Vec2 TopRight { get; private set; }

		public Vec2 BottomLeft { get; private set; }

		public Vec2 BottomRight { get; private set; }

		public Vec2 this[int index]
		{
			get
			{
				switch (index)
				{
				case 0:
					return TopLeft;
				case 1:
					return TopRight;
				case 2:
					return BottomLeft;
				case 3:
					return BottomRight;
				default:
					Debug.FailedAssert("Invalid index", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Oriented2DArea.cs", "Item", 39);
					return Vec2.Invalid;
				}
			}
		}

		public Corners(in Vec2 topLeft, in Vec2 topRight, in Vec2 bottomLeft, in Vec2 bottomRight)
		{
			TopLeft = topLeft;
			TopRight = topRight;
			BottomLeft = bottomLeft;
			BottomRight = bottomRight;
		}
	}

	public Vec2 GlobalCenter { get; private set; }

	public Vec2 GlobalForward { get; private set; }

	public Vec2 LocalDimensions { get; private set; }

	public Oriented2DArea(in Vec2 globalCenter, in Vec2 globalForward, in Vec2 localDimensions)
	{
		GlobalCenter = globalCenter;
		GlobalForward = globalForward;
		LocalDimensions = localDimensions;
	}

	public void SetGlobalCenter(in Vec2 globalCenter)
	{
		GlobalCenter = globalCenter;
	}

	public void SetLocalDimensions(in Vec2 localDimensions)
	{
		LocalDimensions = localDimensions;
	}

	public bool Overlaps(in Oriented2DArea otherArea, float clearanceMargin)
	{
		Corners cornersA = GetCorners();
		Corners cornersB = otherArea.GetCorners();
		if (!IsProjectionOverlap(in cornersA, in cornersB, GlobalForward, clearanceMargin))
		{
			return false;
		}
		Vec2 axis = GlobalForward.RightVec();
		if (!IsProjectionOverlap(in cornersA, in cornersB, axis, clearanceMargin))
		{
			return false;
		}
		if (!IsProjectionOverlap(in cornersA, in cornersB, otherArea.GlobalForward, clearanceMargin))
		{
			return false;
		}
		Vec2 axis2 = otherArea.GlobalForward.RightVec();
		if (!IsProjectionOverlap(in cornersA, in cornersB, axis2, clearanceMargin))
		{
			return false;
		}
		return true;
	}

	public bool Intersects(in LineSegment2D line, float clearanceMargin)
	{
		Corners cornersOfArea = GetCorners();
		Vec2 axis = GlobalForward.RightVec();
		if (DoesProjectionIntersect(in cornersOfArea, in line, GlobalForward, clearanceMargin) && DoesProjectionIntersect(in cornersOfArea, in line, axis, clearanceMargin))
		{
			return DoesProjectionIntersect(in cornersOfArea, in line, line.Normal, clearanceMargin);
		}
		return false;
	}

	public Corners GetCorners()
	{
		Vec2 vec = GlobalForward.RightVec() * (LocalDimensions.x * 0.5f);
		Vec2 vec2 = GlobalForward * (LocalDimensions.y * 0.5f);
		Vec2 topRight = GlobalCenter + vec + vec2;
		return new Corners(GlobalCenter - vec + vec2, in topRight, GlobalCenter - vec - vec2, GlobalCenter + vec - vec2);
	}

	private bool IsProjectionOverlap(in Corners cornersA, in Corners cornersB, Vec2 axis, float clearanceMargin)
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		for (int i = 0; i < 4; i++)
		{
			float val = Vec2.DotProduct(cornersA[i], axis);
			num = Math.Min(num, val);
			num2 = Math.Max(num2, val);
		}
		for (int j = 0; j < 4; j++)
		{
			float val2 = Vec2.DotProduct(cornersB[j], axis);
			num3 = Math.Min(num3, val2);
			num4 = Math.Max(num4, val2);
		}
		if (!(num2 + clearanceMargin < num3))
		{
			return !(num4 + clearanceMargin < num);
		}
		return false;
	}

	private bool DoesProjectionIntersect(in Corners cornersOfArea, in LineSegment2D line, Vec2 axis, float clearanceMargin)
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		for (int i = 0; i < 4; i++)
		{
			float b = Vec2.DotProduct(cornersOfArea[i], axis);
			num = MathF.Min(num, b);
			num2 = MathF.Max(num2, b);
		}
		for (int j = 0; j < 2; j++)
		{
			float b2 = Vec2.DotProduct(line[j], axis);
			num3 = MathF.Min(num3, b2);
			num4 = MathF.Max(num4, b2);
		}
		if (!(num2 + clearanceMargin < num3))
		{
			return !(num4 + clearanceMargin < num);
		}
		return false;
	}
}
