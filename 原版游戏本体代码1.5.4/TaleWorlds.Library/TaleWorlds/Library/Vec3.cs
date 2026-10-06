using System;
using System.Numerics;
using System.Xml.Serialization;

namespace TaleWorlds.Library;

[Serializable]
public struct Vec3
{
	public struct StackArray8Vec3
	{
		private Vec3 _element0;

		private Vec3 _element1;

		private Vec3 _element2;

		private Vec3 _element3;

		private Vec3 _element4;

		private Vec3 _element5;

		private Vec3 _element6;

		private Vec3 _element7;

		public const int Length = 8;

		public Vec3 this[int index]
		{
			get
			{
				switch (index)
				{
				case 0:
					return _element0;
				case 1:
					return _element1;
				case 2:
					return _element2;
				case 3:
					return _element3;
				case 4:
					return _element4;
				case 5:
					return _element5;
				case 6:
					return _element6;
				case 7:
					return _element7;
				default:
					Debug.FailedAssert("Index out of range.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Vec3.cs", "Item", 40);
					return Zero;
				}
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				case 3:
					_element3 = value;
					break;
				case 4:
					_element4 = value;
					break;
				case 5:
					_element5 = value;
					break;
				case 6:
					_element6 = value;
					break;
				case 7:
					_element7 = value;
					break;
				default:
					Debug.FailedAssert("Index out of range.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Vec3.cs", "Item", 58);
					break;
				}
			}
		}
	}

	[XmlAttribute]
	public float x;

	[XmlAttribute]
	public float y;

	[XmlAttribute]
	public float z;

	[XmlAttribute]
	public float w;

	public static readonly Vec3 Side = new Vec3(1f);

	public static readonly Vec3 Forward = new Vec3(0f, 1f);

	public static readonly Vec3 Up = new Vec3(0f, 0f, 1f);

	public static readonly Vec3 One = new Vec3(1f, 1f, 1f);

	public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f, -1f);

	public static readonly Vec3 Invalid = new Vec3(float.NaN, float.NaN, float.NaN);

	public float X => x;

	public float Y => y;

	public float Z => z;

	public float this[int i]
	{
		get
		{
			return i switch
			{
				0 => x, 
				1 => y, 
				2 => z, 
				3 => w, 
				_ => throw new IndexOutOfRangeException("Vec3 out of bounds."), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				x = value;
				break;
			case 1:
				y = value;
				break;
			case 2:
				z = value;
				break;
			case 3:
				w = value;
				break;
			default:
				throw new IndexOutOfRangeException("Vec3 out of bounds.");
			}
		}
	}

	public float Length => MathF.Sqrt(x * x + y * y + z * z);

	public float LengthSquared => x * x + y * y + z * z;

	public bool IsValid
	{
		get
		{
			if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && !float.IsInfinity(x) && !float.IsInfinity(y))
			{
				return !float.IsInfinity(z);
			}
			return false;
		}
	}

	public bool IsValidXYZW
	{
		get
		{
			if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && !float.IsNaN(w) && !float.IsInfinity(x) && !float.IsInfinity(y) && !float.IsInfinity(z))
			{
				return !float.IsInfinity(w);
			}
			return false;
		}
	}

	public bool IsUnit
	{
		get
		{
			float lengthSquared = LengthSquared;
			if (lengthSquared > 0.98010004f)
			{
				return lengthSquared < 1.0201f;
			}
			return false;
		}
	}

	public bool IsNonZero
	{
		get
		{
			if (x == 0f && y == 0f)
			{
				return z != 0f;
			}
			return true;
		}
	}

	public Vec2 AsVec2
	{
		get
		{
			return new Vec2(x, y);
		}
		set
		{
			x = value.x;
			y = value.y;
		}
	}

	public uint ToARGB
	{
		get
		{
			uint a = (uint)(w * 256f);
			uint a2 = (uint)(x * 256f);
			uint a3 = (uint)(y * 256f);
			uint a4 = (uint)(z * 256f);
			return (MathF.Min(a, 255u) << 24) | (MathF.Min(a2, 255u) << 16) | (MathF.Min(a3, 255u) << 8) | MathF.Min(a4, 255u);
		}
	}

	public float RotationZ => MathF.Atan2(0f - x, y);

	public float RotationX => MathF.Atan2(z, MathF.Sqrt(x * x + y * y));

	public Vec3(float x = 0f, float y = 0f, float z = 0f, float w = -1f)
	{
		this.x = x;
		this.y = y;
		this.z = z;
		this.w = w;
	}

	public Vec3(Vec3 c, float w = -1f)
	{
		x = c.x;
		y = c.y;
		z = c.z;
		this.w = w;
	}

	public Vec3(Vec2 xy, float z = 0f, float w = -1f)
	{
		x = xy.x;
		y = xy.y;
		this.z = z;
		this.w = w;
	}

	public Vec3(Vector3 vector3)
		: this(vector3.X, vector3.Y, vector3.Z)
	{
	}

	public static Vec3 Abs(Vec3 vec)
	{
		return new Vec3(MathF.Abs(vec.x), MathF.Abs(vec.y), MathF.Abs(vec.z));
	}

	public static explicit operator Vector3(Vec3 vec3)
	{
		return new Vector3(vec3.x, vec3.y, vec3.z);
	}

	public static float DotProduct(Vec3 v1, Vec3 v2)
	{
		return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
	}

	public static Vec3 Lerp(Vec3 v1, Vec3 v2, float alpha)
	{
		return v1 * (1f - alpha) + v2 * alpha;
	}

	public static Vec3 Slerp(Vec3 start, Vec3 end, float percent)
	{
		float value = DotProduct(start, end);
		value = MBMath.ClampFloat(value, -1f, 1f);
		float num = MathF.Acos(value) * percent;
		Vec3 vec = end - start * value;
		vec.Normalize();
		return start * MathF.Cos(num) + vec * MathF.Sin(num);
	}

	public static Vec3 Vec3Max(Vec3 v1, Vec3 v2)
	{
		return new Vec3(MathF.Max(v1.x, v2.x), MathF.Max(v1.y, v2.y), MathF.Max(v1.z, v2.z));
	}

	public static Vec3 Vec3Min(Vec3 v1, Vec3 v2)
	{
		return new Vec3(MathF.Min(v1.x, v2.x), MathF.Min(v1.y, v2.y), MathF.Min(v1.z, v2.z));
	}

	public static Vec3 CrossProduct(Vec3 va, Vec3 vb)
	{
		return new Vec3(va.y * vb.z - va.z * vb.y, va.z * vb.x - va.x * vb.z, va.x * vb.y - va.y * vb.x);
	}

	public static Vec3 ElementWiseProduct(Vec3 va, Vec3 vb)
	{
		return new Vec3(va.x * vb.x, va.y * vb.y, va.z * vb.z);
	}

	public static Vec3 ElementWiseDivision(Vec3 va, Vec3 vb)
	{
		return new Vec3(va.x / vb.x, va.y / vb.y, va.z / vb.z);
	}

	public static Vec3 operator -(Vec3 v)
	{
		return new Vec3(0f - v.x, 0f - v.y, 0f - v.z);
	}

	public static Vec3 operator +(Vec3 v1, Vec3 v2)
	{
		return new Vec3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
	}

	public static Vec3 operator -(Vec3 v1, Vec3 v2)
	{
		return new Vec3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
	}

	public static Vec3 operator *(Vec3 v, float f)
	{
		return new Vec3(v.x * f, v.y * f, v.z * f);
	}

	public static Vec3 operator *(float f, Vec3 v)
	{
		return new Vec3(v.x * f, v.y * f, v.z * f);
	}

	public static Vec3 operator *(Vec3 v, MatrixFrame frame)
	{
		return new Vec3(frame.rotation.s.x * v.x + frame.rotation.f.x * v.y + frame.rotation.u.x * v.z + frame.origin.x * v.w, frame.rotation.s.y * v.x + frame.rotation.f.y * v.y + frame.rotation.u.y * v.z + frame.origin.y * v.w, frame.rotation.s.z * v.x + frame.rotation.f.z * v.y + frame.rotation.u.z * v.z + frame.origin.z * v.w, frame.rotation.s.w * v.x + frame.rotation.f.w * v.y + frame.rotation.u.w * v.z + frame.origin.w * v.w);
	}

	public static Vec3 operator /(Vec3 v, float f)
	{
		f = 1f / f;
		return new Vec3(v.x * f, v.y * f, v.z * f);
	}

	public static bool operator ==(Vec3 v1, Vec3 v2)
	{
		if (v1.x == v2.x && v1.y == v2.y)
		{
			return v1.z == v2.z;
		}
		return false;
	}

	public static bool operator !=(Vec3 v1, Vec3 v2)
	{
		if (v1.x == v2.x && v1.y == v2.y)
		{
			return v1.z != v2.z;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		if (((Vec3)obj).x == x && ((Vec3)obj).y == y)
		{
			return ((Vec3)obj).z == z;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)(1001f * x + 10039f * y + 117f * z);
	}

	public Vec3 NormalizedCopy()
	{
		Vec3 result = this;
		result.Normalize();
		return result;
	}

	public float Normalize()
	{
		float length = Length;
		if (length > 1E-05f)
		{
			float num = 1f / length;
			x *= num;
			y *= num;
			z *= num;
		}
		else
		{
			x = 0f;
			y = 1f;
			z = 0f;
		}
		return length;
	}

	public void ClampMagnitude(float min, float max)
	{
		float value = Normalize();
		this *= MathF.Clamp(value, min, max);
	}

	public Vec3 ClampedCopy(float min, float max)
	{
		Vec3 result = this;
		result.x = MathF.Clamp(result.x, min, max);
		result.y = MathF.Clamp(result.y, min, max);
		result.z = MathF.Clamp(result.z, min, max);
		return result;
	}

	public Vec3 ClampedCopy(float min, float max, out bool valueClamped)
	{
		Vec3 result = this;
		valueClamped = false;
		if (result.x < min)
		{
			result.x = min;
			valueClamped = true;
		}
		else if (result.x > max)
		{
			result.x = max;
			valueClamped = true;
		}
		if (result.y < min)
		{
			result.y = min;
			valueClamped = true;
		}
		else if (result.y > max)
		{
			result.y = max;
			valueClamped = true;
		}
		if (result.z < min)
		{
			result.z = min;
			valueClamped = true;
		}
		else if (result.z > max)
		{
			result.z = max;
			valueClamped = true;
		}
		return result;
	}

	public void NormalizeWithoutChangingZ()
	{
		z = MBMath.ClampFloat(z, -0.99999f, 0.99999f);
		float length = AsVec2.Length;
		float num = MathF.Sqrt(1f - z * z);
		if (length < num - 1E-07f || length > num + 1E-07f)
		{
			if (length > 1E-09f)
			{
				float num2 = num / length;
				x *= num2;
				y *= num2;
			}
			else
			{
				x = 0f;
				y = num;
			}
		}
	}

	public Vec3 CrossProductWithUp()
	{
		return new Vec3(y, 0f - x);
	}

	public Vec3 CrossProductWithUpAsLeftParameter()
	{
		return new Vec3(0f - y, x);
	}

	public bool NearlyEquals(in Vec3 v, float epsilon = 1E-05f)
	{
		if (MathF.Abs(x - v.x) < epsilon && MathF.Abs(y - v.y) < epsilon)
		{
			return MathF.Abs(z - v.z) < epsilon;
		}
		return false;
	}

	public void RotateAboutX(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		float num = y * ca - z * sa;
		z = z * ca + y * sa;
		y = num;
	}

	public void RotateAboutY(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		float num = x * ca + z * sa;
		z = z * ca - x * sa;
		x = num;
	}

	public void RotateAboutZ(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		float num = x * ca - y * sa;
		y = y * ca + x * sa;
		x = num;
	}

	public Vec3 RotateAboutAnArbitraryVector(Vec3 vec, float a)
	{
		float num = vec.x;
		float num2 = vec.y;
		float num3 = vec.z;
		float num4 = num * x;
		float num5 = num * y;
		float num6 = num * z;
		float num7 = num2 * x;
		float num8 = num2 * y;
		float num9 = num2 * z;
		float num10 = num3 * x;
		float num11 = num3 * y;
		float num12 = num3 * z;
		MathF.SinCos(a, out var sa, out var ca);
		return new Vec3
		{
			x = num * (num4 + num8 + num12) + (x * (num2 * num2 + num3 * num3) - num * (num8 + num12)) * ca + (0f - num11 + num9) * sa,
			y = num2 * (num4 + num8 + num12) + (y * (num * num + num3 * num3) - num2 * (num4 + num12)) * ca + (num10 - num6) * sa,
			z = num3 * (num4 + num8 + num12) + (z * (num * num + num2 * num2) - num3 * (num4 + num8)) * ca + (0f - num7 + num5) * sa
		};
	}

	public Vec3 Reflect(Vec3 normal)
	{
		return this - normal * (2f * DotProduct(this, normal));
	}

	public Vec3 ProjectOnUnitVector(Vec3 ov)
	{
		return ov * (x * ov.x + y * ov.y + z * ov.z);
	}

	public float DistanceSquared(Vec3 v)
	{
		return (v.x - x) * (v.x - x) + (v.y - y) * (v.y - y) + (v.z - z) * (v.z - z);
	}

	public float Distance(Vec3 v)
	{
		return MathF.Sqrt((v.x - x) * (v.x - x) + (v.y - y) * (v.y - y) + (v.z - z) * (v.z - z));
	}

	public Vec3 RotateVectorToXYPlane()
	{
		float length = Length;
		Vec3 vec = this;
		vec.z = 0f;
		vec.Normalize();
		return vec * length;
	}

	public static float AngleBetweenTwoVectors(Vec3 v1, Vec3 v2)
	{
		return MathF.Acos(MathF.Clamp(DotProduct(v1, v2) / (v1.Length * v2.Length), -1f, 1f));
	}

	public override string ToString()
	{
		return "(" + x + ", " + y + ", " + z + ")";
	}

	public string ToString(string format)
	{
		return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ")";
	}

	public static Vec3 Parse(string input)
	{
		input = input.Replace(" ", "");
		string[] array = input.Split(new char[1] { ',' });
		if (array.Length < 3 || array.Length > 4)
		{
			throw new ArgumentOutOfRangeException();
		}
		float num = float.Parse(array[0]);
		float num2 = float.Parse(array[1]);
		float num3 = float.Parse(array[2]);
		float num4 = ((array.Length == 4) ? float.Parse(array[3]) : (-1f));
		return new Vec3(num, num2, num3, num4);
	}
}
