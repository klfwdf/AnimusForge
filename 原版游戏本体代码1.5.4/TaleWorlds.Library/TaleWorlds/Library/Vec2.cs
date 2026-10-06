using System;
using System.Numerics;

namespace TaleWorlds.Library;

[Serializable]
public struct Vec2
{
	public float x;

	public float y;

	public static readonly Vec2 Side = new Vec2(1f, 0f);

	public static readonly Vec2 Forward = new Vec2(0f, 1f);

	public static readonly Vec2 One = new Vec2(1f, 1f);

	public static readonly Vec2 Zero = new Vec2(0f, 0f);

	public static readonly Vec2 Invalid = new Vec2(float.NaN, float.NaN);

	public float X => x;

	public float Y => y;

	public float Length => MathF.Sqrt(x * x + y * y);

	public float LengthSquared => x * x + y * y;

	public float RotationInRadians => MathF.Atan2(0f - x, y);

	public bool IsValid
	{
		get
		{
			if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsInfinity(x))
			{
				return !float.IsInfinity(y);
			}
			return false;
		}
	}

	public Vec2(float a, float b)
	{
		x = a;
		y = b;
	}

	public Vec2(Vec2 v)
	{
		x = v.x;
		y = v.y;
	}

	public Vec2(Vector2 v)
	{
		x = v.X;
		y = v.Y;
	}

	public Vec3 ToVec3(float z = 0f)
	{
		return new Vec3(x, y, z);
	}

	public static explicit operator Vector2(Vec2 vec2)
	{
		return new Vector2(vec2.x, vec2.y);
	}

	public static implicit operator Vec2(Vector2 vec2)
	{
		return new Vec2(vec2.X, vec2.Y);
	}

	public float Normalize()
	{
		float length = Length;
		if (length > 1E-05f)
		{
			x /= length;
			y /= length;
		}
		else
		{
			x = 0f;
			y = 1f;
		}
		return length;
	}

	public Vec2 Normalized()
	{
		Vec2 result = this;
		result.Normalize();
		return result;
	}

	public void ClampMagnitude(float min, float max)
	{
		float value = Normalize();
		this *= MathF.Clamp(value, min, max);
	}

	public static WindingOrder GetWindingOrder(Vec2 first, Vec2 second, Vec2 third)
	{
		Vec2 vb = second - first;
		float num = CCW(third - second, vb);
		if (num > 0f)
		{
			return WindingOrder.Ccw;
		}
		if (num < 0f)
		{
			return WindingOrder.Cw;
		}
		return WindingOrder.None;
	}

	public static float CCW(Vec2 va, Vec2 vb)
	{
		return va.x * vb.y - va.y * vb.x;
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		if (((Vec2)obj).x == x)
		{
			return ((Vec2)obj).y == y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)(1001f * x + 10039f * y);
	}

	public static bool operator ==(Vec2 v1, Vec2 v2)
	{
		if (v1.x == v2.x)
		{
			return v1.y == v2.y;
		}
		return false;
	}

	public static bool operator !=(Vec2 v1, Vec2 v2)
	{
		if (v1.x == v2.x)
		{
			return v1.y != v2.y;
		}
		return true;
	}

	public static Vec2 operator -(Vec2 v)
	{
		return new Vec2(0f - v.x, 0f - v.y);
	}

	public static Vec2 operator +(Vec2 v1, Vec2 v2)
	{
		return new Vec2(v1.x + v2.x, v1.y + v2.y);
	}

	public static Vec2 operator -(Vec2 v1, Vec2 v2)
	{
		return new Vec2(v1.x - v2.x, v1.y - v2.y);
	}

	public static Vec2 operator *(Vec2 v, float f)
	{
		return new Vec2(v.x * f, v.y * f);
	}

	public static Vec2 operator *(float f, Vec2 v)
	{
		return new Vec2(v.x * f, v.y * f);
	}

	public static Vec2 operator /(float f, Vec2 v)
	{
		return new Vec2(f / v.x, f / v.y);
	}

	public static Vec2 operator /(Vec2 v, float f)
	{
		return new Vec2(v.x / f, v.y / f);
	}

	public bool IsUnit()
	{
		float lengthSquared = LengthSquared;
		if (lengthSquared > 0.98010004f)
		{
			return lengthSquared < 1.0201f;
		}
		return false;
	}

	public bool IsNonZero()
	{
		float num = 1E-05f;
		if (!(x > num) && !(x < 0f - num) && !(y > num))
		{
			return y < 0f - num;
		}
		return true;
	}

	public bool NearlyEquals(Vec2 v, float epsilon = 1E-05f)
	{
		if (MathF.Abs(x - v.x) < epsilon)
		{
			return MathF.Abs(y - v.y) < epsilon;
		}
		return false;
	}

	public void RotateCCW(float angleInRadians)
	{
		MathF.SinCos(angleInRadians, out var sa, out var ca);
		float num = x * ca - y * sa;
		y = y * ca + x * sa;
		x = num;
	}

	public float DotProduct(Vec2 v)
	{
		return v.x * x + v.y * y;
	}

	public static float DotProduct(Vec2 va, Vec2 vb)
	{
		return va.x * vb.x + va.y * vb.y;
	}

	public static Vec2 ElementWiseProduct(Vec2 va, Vec2 vb)
	{
		return new Vec2(va.x * vb.x, va.y * vb.y);
	}

	public static Vec2 FromRotation(float rotation)
	{
		return new Vec2(0f - MathF.Sin(rotation), MathF.Cos(rotation));
	}

	public Vec2 TransformToLocalUnitF(Vec2 a)
	{
		return new Vec2(y * a.x - x * a.y, x * a.x + y * a.y);
	}

	public Vec2 TransformToParentUnitF(Vec2 a)
	{
		return new Vec2(y * a.x + x * a.y, (0f - x) * a.x + y * a.y);
	}

	public Vec2 TransformToLocalUnitFLeftHanded(Vec2 a)
	{
		return new Vec2((0f - y) * a.x + x * a.y, x * a.x + y * a.y);
	}

	public Vec2 TransformToParentUnitFLeftHanded(Vec2 a)
	{
		return new Vec2((0f - y) * a.x + x * a.y, x * a.x + y * a.y);
	}

	public Vec2 RightVec()
	{
		return new Vec2(y, 0f - x);
	}

	public Vec2 LeftVec()
	{
		return new Vec2(0f - y, x);
	}

	public static Vec2 Max(Vec2 v1, Vec2 v2)
	{
		return new Vec2(MathF.Max(v1.x, v2.x), MathF.Max(v1.y, v2.y));
	}

	public static Vec2 Max(Vec2 v1, float f)
	{
		return new Vec2(MathF.Max(v1.x, f), MathF.Max(v1.y, f));
	}

	public static Vec2 Min(Vec2 v1, Vec2 v2)
	{
		return new Vec2(MathF.Min(v1.x, v2.x), MathF.Min(v1.y, v2.y));
	}

	public static Vec2 Min(Vec2 v1, float f)
	{
		return new Vec2(MathF.Min(v1.x, f), MathF.Min(v1.y, f));
	}

	public override string ToString()
	{
		return "(Vec2) X: " + x + " Y: " + y;
	}

	public float DistanceSquared(Vec2 v)
	{
		return (v.x - x) * (v.x - x) + (v.y - y) * (v.y - y);
	}

	public float Distance(Vec2 v)
	{
		return MathF.Sqrt((v.x - x) * (v.x - x) + (v.y - y) * (v.y - y));
	}

	public static float DistanceToLine(Vec2 line1, Vec2 line2, Vec2 point)
	{
		float num = line2.x - line1.x;
		float num2 = line2.y - line1.y;
		return MathF.Abs(num * (line1.y - point.y) - (line1.x - point.x) * num2) / MathF.Sqrt(num * num + num2 * num2);
	}

	public static float DistanceToLineSegmentSquared(Vec2 line1, Vec2 line2, Vec2 point)
	{
		return point.DistanceSquared(MBMath.GetClosestPointOnLineSegmentToPoint(in line1, in line2, in point));
	}

	public float DistanceToLineSegment(Vec2 v, Vec2 w, out Vec2 closestPointOnLineSegment)
	{
		return MathF.Sqrt(DistanceSquaredToLineSegment(v, w, out closestPointOnLineSegment));
	}

	public float DistanceSquaredToLineSegment(Vec2 v, Vec2 w, out Vec2 closestPointOnLineSegment)
	{
		Vec2 vec = this;
		float num = v.DistanceSquared(w);
		if (num == 0f)
		{
			closestPointOnLineSegment = v;
		}
		else
		{
			float num2 = DotProduct(vec - v, w - v) / num;
			if (num2 < 0f)
			{
				closestPointOnLineSegment = v;
			}
			else if (num2 > 1f)
			{
				closestPointOnLineSegment = w;
			}
			else
			{
				Vec2 vec2 = v + (w - v) * num2;
				closestPointOnLineSegment = vec2;
			}
		}
		return vec.DistanceSquared(closestPointOnLineSegment);
	}

	public static Vec2 Abs(Vec2 vec)
	{
		return new Vec2(MathF.Abs(vec.x), MathF.Abs(vec.y));
	}

	public static Vec2 Lerp(Vec2 v1, Vec2 v2, float alpha)
	{
		return v1 * (1f - alpha) + v2 * alpha;
	}

	public static Vec2 Slerp(Vec2 start, Vec2 end, float percent)
	{
		float value = DotProduct(start, end);
		value = MBMath.ClampFloat(value, -1f, 1f);
		float num = MathF.Acos(value) * percent;
		Vec2 vec = end - start * value;
		vec.Normalize();
		return start * MathF.Cos(num) + vec * MathF.Sin(num);
	}

	public float AngleBetween(Vec2 vector2)
	{
		float num = x * vector2.y - vector2.x * y;
		float num2 = x * vector2.x + y * vector2.y;
		return MathF.Atan2(num, num2);
	}

	public static float Determinant(in Vec2 vec1, in Vec2 vec2)
	{
		return vec1.x * vec2.y - vec1.y * vec2.x;
	}
}
