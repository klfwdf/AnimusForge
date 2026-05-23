using System;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public static class Mathf
{
	public const float PI = System.MathF.PI;

	public const float Deg2Rad = System.MathF.PI / 180f;

	public const float Rad2Deg = 180f / System.MathF.PI;

	public const float Epsilon = 1E-05f;

	public static float Sqrt(float f)
	{
		return (float)Math.Sqrt(f);
	}

	public static float Abs(float f)
	{
		return Math.Abs(f);
	}

	public static float Floor(float f)
	{
		return (float)Math.Floor(f);
	}

	public static float Cos(float radian)
	{
		return (float)Math.Cos(radian);
	}

	public static float Sin(float radian)
	{
		return (float)Math.Sin(radian);
	}

	public static float Acos(float f)
	{
		return (float)Math.Acos(f);
	}

	public static float Atan2(float y, float x)
	{
		return (float)Math.Atan2(y, x);
	}

	public static float Clamp(float value, float min, float max)
	{
		if (!(value > max))
		{
			if (!(value < min))
			{
				return value;
			}
			return min;
		}
		return max;
	}

	public static int Clamp(int value, int min, int max)
	{
		if (value <= max)
		{
			if (value >= min)
			{
				return value;
			}
			return min;
		}
		return max;
	}

	public static float Min(float a, float b)
	{
		if (!(a > b))
		{
			return a;
		}
		return b;
	}

	public static float Max(float a, float b)
	{
		if (!(a > b))
		{
			return b;
		}
		return a;
	}

	public static bool IsZero(float f)
	{
		if (f < 1E-05f)
		{
			return f > -1E-05f;
		}
		return false;
	}

	public static bool IsZero(Vector2 vector2)
	{
		if (IsZero(vector2.X))
		{
			return IsZero(vector2.Y);
		}
		return false;
	}

	public static float Sign(float f)
	{
		return Math.Sign(f);
	}

	public static float Ceil(float f)
	{
		return (float)Math.Ceiling(f);
	}

	public static float Round(float f)
	{
		return (float)Math.Round(f);
	}

	public static float Lerp(float start, float end, float amount)
	{
		return (end - start) * amount + start;
	}

	private static float PingPong(float min, float max, float time)
	{
		int num = (int)(min * 100f);
		int num2 = (int)(max * 100f);
		int num3 = (int)(time * 100f);
		int num4 = num2 - num;
		bool num5 = num3 / num4 % 2 == 0;
		int num6 = num3 % num4;
		return (float)(num5 ? (num6 + num) : (num2 - num6)) / 100f;
	}

	public static Vec3 GetClosestPointInLineSegmentToLine(Vec3 linePosition, Vec3 lineDirection, Vec3 lineSegmentBegin, Vec3 lineSegmentEnd)
	{
		Vec3 vec = lineSegmentEnd - lineSegmentBegin;
		Vec3 v = linePosition - lineSegmentBegin;
		if (!vec.IsNonZero)
		{
			return lineSegmentBegin;
		}
		float num = Vec3.DotProduct(lineDirection, lineDirection);
		float num2 = Vec3.DotProduct(lineDirection, vec);
		float num3 = Vec3.DotProduct(vec, vec);
		float num4 = Vec3.DotProduct(lineDirection, v);
		float num5 = Vec3.DotProduct(vec, v);
		float num6 = num * num3 - num2 * num2;
		float value = ((!num6.ApproximatelyEqualsTo(0f)) ? ((num * num5 - num2 * num4) / num6) : (Vec3.DotProduct(vec, linePosition - lineSegmentBegin) / Vec3.DotProduct(vec, vec)));
		value = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
		return lineSegmentBegin + value * vec;
	}
}
