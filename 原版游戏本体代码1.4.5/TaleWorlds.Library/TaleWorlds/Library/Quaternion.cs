using System;

namespace TaleWorlds.Library;

[Serializable]
public struct Quaternion
{
	public float W;

	public float X;

	public float Y;

	public float Z;

	public float this[int i]
	{
		get
		{
			float num = 0f;
			return i switch
			{
				0 => W, 
				1 => X, 
				2 => Y, 
				3 => Z, 
				_ => throw new IndexOutOfRangeException("Quaternion out of bounds."), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				W = value;
				break;
			case 1:
				X = value;
				break;
			case 2:
				Y = value;
				break;
			case 3:
				Z = value;
				break;
			default:
				throw new IndexOutOfRangeException("Quaternion out of bounds.");
			}
		}
	}

	public bool IsIdentity
	{
		get
		{
			if (X == 0f && Y == 0f && Z == 0f)
			{
				return W == 1f;
			}
			return false;
		}
	}

	public bool IsUnit => MBMath.ApproximatelyEquals(X * X + Y * Y + Z * Z + W * W, 1f, 0.2f);

	public static Quaternion Identity => new Quaternion(0f, 0f, 0f, 1f);

	public Quaternion(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public static bool operator ==(Quaternion a, Quaternion b)
	{
		if ((object)a == (object)b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a.X == b.X && a.Y == b.Y && a.Z == b.Z)
		{
			return a.W == b.W;
		}
		return false;
	}

	public static bool operator !=(Quaternion a, Quaternion b)
	{
		return !(a == b);
	}

	public static Quaternion operator +(Quaternion a, Quaternion b)
	{
		return new Quaternion(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
	}

	public static Quaternion operator -(Quaternion a, Quaternion b)
	{
		return new Quaternion(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
	}

	public static Quaternion operator *(Quaternion a, float b)
	{
		return new Quaternion(a.X * b, a.Y * b, a.Z * b, a.W * b);
	}

	public static Quaternion operator *(float s, Quaternion v)
	{
		return v * s;
	}

	public static Quaternion operator *(Quaternion a, Quaternion b)
	{
		float w = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;
		float x = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
		float y = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
		float z = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;
		return new Quaternion(x, y, z, w);
	}

	public static Quaternion operator /(Quaternion v, float s)
	{
		return new Quaternion(v.X / s, v.Y / s, v.Z / s, v.W / s);
	}

	public float Normalize()
	{
		float num = MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
		if (num <= 1E-07f)
		{
			X = 0f;
			Y = 0f;
			Z = 0f;
			W = 1f;
		}
		else
		{
			float num2 = 1f / num;
			X *= num2;
			Y *= num2;
			Z *= num2;
			W *= num2;
		}
		return num;
	}

	public float SafeNormalize()
	{
		double num = Math.Sqrt((double)X * (double)X + (double)Y * (double)Y + (double)Z * (double)Z + (double)W * (double)W);
		if (num <= 1E-07)
		{
			X = 0f;
			Y = 0f;
			Z = 0f;
			W = 1f;
		}
		else
		{
			X = (float)((double)X / num);
			Y = (float)((double)Y / num);
			Z = (float)((double)Z / num);
			W = (float)((double)W / num);
		}
		return (float)num;
	}

	public float NormalizeWeighted()
	{
		float num = X * X + Y * Y + Z * Z;
		if (num <= 1E-09f)
		{
			X = 1f;
			Y = 0f;
			Z = 0f;
			W = 0f;
		}
		else
		{
			W = MathF.Sqrt(1f - num);
		}
		return num;
	}

	public void SetToRotationX(float angle)
	{
		MathF.SinCos(angle * 0.5f, out var sa, out var ca);
		X = sa;
		Y = 0f;
		Z = 0f;
		W = ca;
	}

	public void SetToRotationY(float angle)
	{
		MathF.SinCos(angle * 0.5f, out var sa, out var ca);
		X = 0f;
		Y = sa;
		Z = 0f;
		W = ca;
	}

	public void SetToRotationZ(float angle)
	{
		MathF.SinCos(angle * 0.5f, out var sa, out var ca);
		X = 0f;
		Y = 0f;
		Z = sa;
		W = ca;
	}

	public void Flip()
	{
		X = 0f - X;
		Y = 0f - Y;
		Z = 0f - Z;
		W = 0f - W;
	}

	public Quaternion TransformToParent(Quaternion q)
	{
		return new Quaternion
		{
			X = Y * q.Z - Z * q.Y + W * q.X + X * q.W,
			Y = Z * q.X - X * q.Z + W * q.Y + Y * q.W,
			Z = X * q.Y - Y * q.X + W * q.Z + Z * q.W,
			W = W * q.W - (X * q.X + Y * q.Y + Z * q.Z)
		};
	}

	public Quaternion TransformToLocal(Quaternion q)
	{
		return new Quaternion
		{
			X = Z * q.Y - Y * q.Z + W * q.X - X * q.W,
			Y = X * q.Z - Z * q.X + W * q.Y - Y * q.W,
			Z = Y * q.X - X * q.Y + W * q.Z - Z * q.W,
			W = W * q.W + (X * q.X + Y * q.Y + Z * q.Z)
		};
	}

	public Quaternion TransformToLocalWithoutNormalize(Quaternion q)
	{
		return new Quaternion
		{
			X = Z * q.Y - Y * q.Z + W * q.X - X * q.W,
			Y = X * q.Z - Z * q.X + W * q.Y - Y * q.W,
			Z = Y * q.X - X * q.Y + W * q.Z - Z * q.W,
			W = W * q.W + (X * q.X + Y * q.Y + Z * q.Z)
		};
	}

	public static Quaternion Slerp(Quaternion from, Quaternion to, float t)
	{
		float num = 1f;
		float num2 = from.Dotp4(to);
		if (num2 < 0f)
		{
			num2 = 0f - num2;
			num = -1f;
		}
		else
		{
			num = 1f;
		}
		float num6;
		float num7;
		if (0.9995f >= num2)
		{
			float num3 = MathF.Acos(num2);
			float num4 = 1f / MathF.Sin(num3);
			float num5 = t * num3;
			num6 = MathF.Sin(num3 - num5) * num4;
			num7 = MathF.Sin(num5) * num4;
		}
		else
		{
			num6 = 1f - t;
			num7 = t;
		}
		num7 *= num;
		Quaternion result = default(Quaternion);
		result.X = num6 * from.X + num7 * to.X;
		result.Y = num6 * from.Y + num7 * to.Y;
		result.Z = num6 * from.Z + num7 * to.Z;
		result.W = num6 * from.W + num7 * to.W;
		result.Normalize();
		return result;
	}

	public static Quaternion Lerp(Quaternion from, Quaternion to, float t)
	{
		float num = from.Dotp4(to);
		float num2 = 1f - t;
		float num3;
		if (num < 0f)
		{
			num = 0f - num;
			num3 = 0f - t;
		}
		else
		{
			num3 = t;
		}
		return new Quaternion
		{
			X = num2 * from.X + num3 * to.X,
			Y = num2 * from.Y + num3 * to.Y,
			Z = num2 * from.Z + num3 * to.Z,
			W = num2 * from.W + num3 * to.W
		};
	}

	public static Mat3 Mat3FromQuaternion(Quaternion quat)
	{
		Mat3 result = default(Mat3);
		float num = quat.X + quat.X;
		float num2 = quat.Y + quat.Y;
		float num3 = quat.Z + quat.Z;
		float num4 = quat.X * num;
		float num5 = quat.X * num2;
		float num6 = quat.X * num3;
		float num7 = quat.Y * num2;
		float num8 = quat.Y * num3;
		float num9 = quat.Z * num3;
		float num10 = quat.W * num;
		float num11 = quat.W * num2;
		float num12 = quat.W * num3;
		result.s.x = 1f - (num7 + num9);
		result.s.y = num5 + num12;
		result.s.z = num6 - num11;
		result.f.x = num5 - num12;
		result.f.y = 1f - (num4 + num9);
		result.f.z = num8 + num10;
		result.u.x = num6 + num11;
		result.u.y = num8 - num10;
		result.u.z = 1f - (num4 + num7);
		return result;
	}

	public static Quaternion QuaternionFromEulerAngles(float yaw, float pitch, float roll)
	{
		float num = yaw * (System.MathF.PI / 180f);
		float num2 = pitch * (System.MathF.PI / 180f);
		float num3 = roll * (System.MathF.PI / 180f);
		float num4 = MathF.Cos(num * 0.5f);
		float num5 = MathF.Sin(num * 0.5f);
		float num6 = MathF.Cos(num2 * 0.5f);
		float num7 = MathF.Sin(num2 * 0.5f);
		float num8 = MathF.Cos(num3 * 0.5f);
		float num9 = MathF.Sin(num3 * 0.5f);
		float w = num8 * num6 * num4 + num9 * num7 * num5;
		float x = num9 * num6 * num4 - num8 * num7 * num5;
		float y = num8 * num7 * num4 + num9 * num6 * num5;
		float z = num8 * num6 * num5 - num9 * num7 * num4;
		return new Quaternion(x, y, z, w);
	}

	public static Quaternion QuaternionFromMat3(Mat3 m)
	{
		Quaternion result = default(Quaternion);
		float num;
		if (m.u.z < 0f)
		{
			if (m.s.x > m.f.y)
			{
				num = 1f + m.s.x - m.f.y - m.u.z;
				result.W = m.f.z - m.u.y;
				result.X = num;
				result.Y = m.s.y + m.f.x;
				result.Z = m.u.x + m.s.z;
			}
			else
			{
				num = 1f - m.s.x + m.f.y - m.u.z;
				result.W = m.u.x - m.s.z;
				result.X = m.s.y + m.f.x;
				result.Y = num;
				result.Z = m.f.z + m.u.y;
			}
		}
		else if (m.s.x < 0f - m.f.y)
		{
			num = 1f - m.s.x - m.f.y + m.u.z;
			result.W = m.s.y - m.f.x;
			result.X = m.u.x + m.s.z;
			result.Y = m.f.z + m.u.y;
			result.Z = num;
		}
		else
		{
			num = (result.W = 1f + m.s.x + m.f.y + m.u.z);
			result.X = m.f.z - m.u.y;
			result.Y = m.u.x - m.s.z;
			result.Z = m.s.y - m.f.x;
		}
		float num2 = 0.5f / MathF.Sqrt(num);
		result.W *= num2;
		result.X *= num2;
		result.Y *= num2;
		result.Z *= num2;
		return result;
	}

	public static void AxisAngleFromQuaternion(out Vec3 axis, out float angle, Quaternion quat)
	{
		axis = default(Vec3);
		float w = quat.W;
		if (w > 0.9999999f)
		{
			axis.x = 1f;
			axis.y = 0f;
			axis.z = 0f;
			angle = 0f;
			return;
		}
		float num = MathF.Sqrt(1f - w * w);
		if (num < 0.0001f)
		{
			num = 1f;
		}
		axis.x = quat.X / num;
		axis.y = quat.Y / num;
		axis.z = quat.Z / num;
		angle = MathF.Acos(w) * 2f;
	}

	public static Quaternion QuaternionFromAxisAngle(Vec3 axis, float angle)
	{
		Quaternion result = default(Quaternion);
		MathF.SinCos(angle * 0.5f, out var sa, out var ca);
		result.X = axis.x * sa;
		result.Y = axis.y * sa;
		result.Z = axis.z * sa;
		result.W = ca;
		return result;
	}

	public static Vec3 EulerAngleFromQuaternion(Quaternion quat)
	{
		float w = quat.W;
		float x = quat.X;
		float y = quat.Y;
		float z = quat.Z;
		float num = w * w;
		float num2 = x * x;
		float num3 = y * y;
		float num4 = z * z;
		return new Vec3
		{
			z = MathF.Atan2(2f * (x * y + z * w), num2 - num3 - num4 + num),
			x = MathF.Atan2(2f * (y * z + x * w), 0f - num2 - num3 + num4 + num),
			y = MathF.Asin(-2f * (x * z - y * w))
		};
	}

	public static Quaternion FindShortestArcAsQuaternion(Vec3 v0, Vec3 v1)
	{
		Vec3 vec = Vec3.CrossProduct(v0, v1);
		float num = Vec3.DotProduct(v0, v1);
		if ((double)num < -0.9999900000002526)
		{
			Vec3 vec2 = default(Vec3);
			vec2 = ((!(MathF.Abs(v0.z) < 0.8f)) ? Vec3.CrossProduct(v0, new Vec3(1f)) : Vec3.CrossProduct(v0, new Vec3(0f, 0f, 1f)));
			vec2.Normalize();
			return new Quaternion(vec2.x, vec2.y, vec2.z, 0f);
		}
		float num2 = MathF.Sqrt((1f + num) * 2f);
		float num3 = 1f / num2;
		return new Quaternion(vec.x * num3, vec.y * num3, vec.z * num3, num2 * 0.5f);
	}

	public float Dotp4(Quaternion q2)
	{
		return X * q2.X + Y * q2.Y + Z * q2.Z + W * q2.W;
	}

	public Mat3 ToMat3()
	{
		return Mat3FromQuaternion(this);
	}

	public bool InverseDirection(Quaternion q2)
	{
		return Dotp4(q2) < 0f;
	}

	public Quaternion Conjugate()
	{
		return new Quaternion(0f - X, 0f - Y, 0f - Z, W);
	}

	public Quaternion Inverse()
	{
		float num = X * X + Y * Y + Z * Z + W * W;
		if (num == 0f)
		{
			Debug.FailedAssert("Cannot invert a quaternion with zero norm.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Quaternion.cs", "Inverse", 608);
			return this;
		}
		return Conjugate() / num;
	}
}
