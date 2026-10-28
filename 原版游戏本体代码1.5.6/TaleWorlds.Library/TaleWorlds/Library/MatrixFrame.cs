using System;

namespace TaleWorlds.Library;

[Serializable]
public struct MatrixFrame
{
	public Mat3 rotation;

	public Vec3 origin;

	public static MatrixFrame Identity => new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, 0f, 1f));

	public static MatrixFrame Zero => new MatrixFrame(new Mat3(in Vec3.Zero, in Vec3.Zero, in Vec3.Zero), new Vec3(0f, 0f, 0f, 1f));

	public bool IsIdentity
	{
		get
		{
			if (!origin.IsNonZero)
			{
				return rotation.IsIdentity();
			}
			return false;
		}
	}

	public bool IsZero
	{
		get
		{
			if (!origin.IsNonZero)
			{
				return rotation.IsZero();
			}
			return false;
		}
	}

	public Vec3 this[int i]
	{
		get
		{
			return i switch
			{
				0 => rotation.s, 
				1 => rotation.f, 
				2 => rotation.u, 
				3 => origin, 
				_ => throw new IndexOutOfRangeException("MatrixFrame out of bounds."), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				rotation.s = value;
				break;
			case 1:
				rotation.f = value;
				break;
			case 2:
				rotation.u = value;
				break;
			case 3:
				origin = value;
				break;
			default:
				throw new IndexOutOfRangeException("MatrixFrame out of bounds.");
			}
		}
	}

	public float this[int i, int j]
	{
		get
		{
			return i switch
			{
				0 => rotation.s[j], 
				1 => rotation.f[j], 
				2 => rotation.u[j], 
				3 => origin[j], 
				_ => throw new IndexOutOfRangeException("MatrixFrame out of bounds."), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				rotation.s[j] = value;
				break;
			case 1:
				rotation.f[j] = value;
				break;
			case 2:
				rotation.u[j] = value;
				break;
			case 3:
				origin[j] = value;
				break;
			default:
				throw new IndexOutOfRangeException("MatrixFrame out of bounds.");
			}
		}
	}

	public MatrixFrame(in Mat3 rot, in Vec3 o)
	{
		rotation = rot;
		origin = o;
	}

	public MatrixFrame(float _11, float _12, float _13, float _21, float _22, float _23, float _31, float _32, float _33, float _41, float _42, float _43)
	{
		rotation = new Mat3(_11, _12, _13, _21, _22, _23, _31, _32, _33);
		origin = new Vec3(_41, _42, _43);
	}

	public MatrixFrame(float _11, float _12, float _13, float _14, float _21, float _22, float _23, float _24, float _31, float _32, float _33, float _34, float _41, float _42, float _43, float _44)
	{
		rotation = new Mat3
		{
			s = new Vec3(_11, _12, _13, _14),
			f = new Vec3(_21, _22, _23, _24),
			u = new Vec3(_31, _32, _33, _34)
		};
		origin = new Vec3(_41, _42, _43, _44);
	}

	public Vec3 TransformToParent(in Vec3 v)
	{
		return new Vec3(rotation.s.x * v.x + rotation.f.x * v.y + rotation.u.x * v.z + origin.x, rotation.s.y * v.x + rotation.f.y * v.y + rotation.u.y * v.z + origin.y, rotation.s.z * v.x + rotation.f.z * v.y + rotation.u.z * v.z + origin.z);
	}

	public Vec3 TransformToParentDouble(in Vec3 v)
	{
		return new Vec3((float)((double)rotation.s.x * (double)v.x + (double)rotation.f.x * (double)v.y + (double)rotation.u.x * (double)v.z + (double)origin.x), (float)((double)rotation.s.y * (double)v.x + (double)rotation.f.y * (double)v.y + (double)rotation.u.y * (double)v.z + (double)origin.y), (float)((double)rotation.s.z * (double)v.x + (double)rotation.f.z * (double)v.y + (double)rotation.u.z * (double)v.z + (double)origin.z));
	}

	public Vec2 TransformToParent(in Vec2 v)
	{
		return new Vec2(rotation.s.x * v.x + rotation.f.x * v.y + origin.x, rotation.s.y * v.x + rotation.f.y * v.y + origin.y);
	}

	public Vec3 TransformToLocal(in Vec3 v)
	{
		Vec3 vec = v - origin;
		return new Vec3(rotation.s.x * vec.x + rotation.s.y * vec.y + rotation.s.z * vec.z, rotation.f.x * vec.x + rotation.f.y * vec.y + rotation.f.z * vec.z, rotation.u.x * vec.x + rotation.u.y * vec.y + rotation.u.z * vec.z);
	}

	public Vec3 TransformToLocalNonUnit(in Vec3 v)
	{
		Vec3 vec = v - origin;
		return new Vec3(rotation.s.x * vec.x + rotation.s.y * vec.y + rotation.s.z * vec.z, rotation.f.x * vec.x + rotation.f.y * vec.y + rotation.f.z * vec.z, rotation.u.x * vec.x + rotation.u.y * vec.y + rotation.u.z * vec.z);
	}

	public bool NearlyEquals(MatrixFrame rhs, float epsilon = 1E-05f)
	{
		if (rotation.NearlyEquals(in rhs.rotation, epsilon))
		{
			return origin.NearlyEquals(in rhs.origin, epsilon);
		}
		return false;
	}

	public Vec3 TransformToLocalNonOrthogonal(in Vec3 v)
	{
		return new MatrixFrame(rotation.s.x, rotation.s.y, rotation.s.z, 0f, rotation.f.x, rotation.f.y, rotation.f.z, 0f, rotation.u.x, rotation.u.y, rotation.u.z, 0f, origin.x, origin.y, origin.z, 1f).Inverse().TransformToParent(in v);
	}

	public MatrixFrame TransformToLocalNonOrthogonal(in MatrixFrame frame)
	{
		return new MatrixFrame(rotation.s.x, rotation.s.y, rotation.s.z, 0f, rotation.f.x, rotation.f.y, rotation.f.z, 0f, rotation.u.x, rotation.u.y, rotation.u.z, 0f, origin.x, origin.y, origin.z, 1f).Inverse().TransformToParent(in frame);
	}

	public static MatrixFrame Lerp(in MatrixFrame m1, in MatrixFrame m2, float alpha)
	{
		MatrixFrame result = default(MatrixFrame);
		result.rotation = Mat3.Lerp(in m1.rotation, in m2.rotation, alpha);
		result.origin = Vec3.Lerp(m1.origin, m2.origin, alpha);
		return result;
	}

	public static MatrixFrame LerpNonOrthogonal(in MatrixFrame m1, in MatrixFrame m2, float alpha)
	{
		MatrixFrame result = default(MatrixFrame);
		result.rotation = Mat3.LerpNonOrthogonal(in m1.rotation, in m2.rotation, alpha);
		result.origin = Vec3.Lerp(m1.origin, m2.origin, alpha);
		result.Fill();
		return result;
	}

	public static MatrixFrame Slerp(in MatrixFrame m1, in MatrixFrame m2, float alpha)
	{
		MatrixFrame result = default(MatrixFrame);
		result.origin = Vec3.Lerp(m1.origin, m2.origin, alpha);
		result.rotation = Quaternion.Slerp(Quaternion.QuaternionFromMat3(m1.rotation), Quaternion.QuaternionFromMat3(m2.rotation), alpha).ToMat3();
		return result;
	}

	public MatrixFrame TransformToParent(in MatrixFrame m)
	{
		return new MatrixFrame(rotation.TransformToParent(in m.rotation), TransformToParent(in m.origin));
	}

	public MatrixFrame TransformToLocal(in MatrixFrame m)
	{
		return new MatrixFrame(rotation.TransformToLocal(in m.rotation), TransformToLocal(in m.origin));
	}

	public Vec3 TransformToParentWithW(Vec3 _s)
	{
		return new Vec3(rotation.s.x * _s.x + rotation.f.x * _s.y + rotation.u.x * _s.z + origin.x * _s.w, rotation.s.y * _s.x + rotation.f.y * _s.y + rotation.u.y * _s.z + origin.y * _s.w, rotation.s.z * _s.x + rotation.f.z * _s.y + rotation.u.z * _s.z + origin.z * _s.w, rotation.s.w * _s.x + rotation.f.w * _s.y + rotation.u.w * _s.z + origin.w * _s.w);
	}

	public MatrixFrame GetUnitRotFrame(float removedScale)
	{
		return new MatrixFrame(rotation.GetUnitRotation(removedScale), in origin);
	}

	public MatrixFrame InverseFast()
	{
		AssertFilled();
		MatrixFrame matrix = default(MatrixFrame);
		float num = rotation.u.z * origin.w - rotation.u.w * origin.z;
		float num2 = rotation.f.z * origin.w - rotation.f.w * origin.z;
		float num3 = rotation.f.z * rotation.u.w - rotation.f.w * rotation.u.z;
		float num4 = rotation.s.z * origin.w - rotation.s.w * origin.z;
		float num5 = rotation.s.z * rotation.u.w - rotation.s.w * rotation.u.z;
		float num6 = rotation.s.z * rotation.f.w - rotation.s.w * rotation.f.z;
		float num7 = rotation.u.y * origin.w - rotation.u.w * origin.y;
		float num8 = rotation.f.y * origin.w - rotation.f.w * origin.y;
		float num9 = rotation.f.y * rotation.u.w - rotation.f.w * rotation.u.y;
		float num10 = rotation.s.y * origin.w - rotation.s.w * origin.y;
		float num11 = rotation.s.y * rotation.u.w - rotation.s.w * rotation.u.y;
		float num12 = rotation.f.y * origin.w - rotation.f.w * origin.y;
		float num13 = rotation.s.y * rotation.f.w - rotation.s.w * rotation.f.y;
		float num14 = rotation.u.y * origin.z - rotation.u.z * origin.y;
		float num15 = rotation.f.y * origin.z - rotation.f.z * origin.y;
		float num16 = rotation.f.y * rotation.u.z - rotation.f.z * rotation.u.y;
		float num17 = rotation.s.y * origin.z - rotation.s.z * origin.y;
		float num18 = rotation.s.y * rotation.u.z - rotation.s.z * rotation.u.y;
		float num19 = rotation.s.y * rotation.f.z - rotation.s.z * rotation.f.y;
		matrix.rotation.s.x = rotation.f.y * num - rotation.u.y * num2 + origin.y * num3;
		matrix.rotation.s.y = (0f - rotation.s.y) * num + rotation.u.y * num4 - origin.y * num5;
		matrix.rotation.s.z = rotation.s.y * num2 - rotation.f.y * num4 + origin.y * num6;
		matrix.rotation.s.w = (0f - rotation.s.y) * num3 + rotation.f.y * num5 - rotation.u.y * num6;
		matrix.rotation.f.x = (0f - rotation.f.x) * num + rotation.u.x * num2 - origin.x * num3;
		matrix.rotation.f.y = rotation.s.x * num - rotation.u.x * num4 + origin.x * num5;
		matrix.rotation.f.z = (0f - rotation.s.x) * num2 + rotation.f.x * num4 - origin.x * num6;
		matrix.rotation.f.w = rotation.s.x * num3 - rotation.f.x * num5 + rotation.u.x * num6;
		matrix.rotation.u.x = rotation.f.x * num7 - rotation.u.x * num8 + origin.x * num9;
		matrix.rotation.u.y = (0f - rotation.s.x) * num7 + rotation.u.x * num10 - origin.x * num11;
		matrix.rotation.u.z = rotation.s.x * num12 - rotation.f.x * num10 + origin.x * num13;
		matrix.rotation.u.w = (0f - rotation.s.x) * num9 + rotation.f.x * num11 - rotation.u.x * num13;
		matrix.origin.x = (0f - rotation.f.x) * num14 + rotation.u.x * num15 - origin.x * num16;
		matrix.origin.y = rotation.s.x * num14 - rotation.u.x * num17 + origin.x * num18;
		matrix.origin.z = (0f - rotation.s.x) * num15 + rotation.f.x * num17 - origin.x * num19;
		matrix.origin.w = rotation.s.x * num16 - rotation.f.x * num18 + rotation.u.x * num19;
		float num20 = rotation.s.x * matrix.rotation.s.x + rotation.f.x * matrix.rotation.s.y + rotation.u.x * matrix.rotation.s.z + origin.x * matrix.rotation.s.w;
		if (num20 != 1f)
		{
			DivideWith(ref matrix, num20);
		}
		return matrix;
	}

	public MatrixFrame Inverse()
	{
		return InverseFast();
	}

	public float Determinant4X4()
	{
		return rotation.s.x * Determinant3X3(new Vec3(rotation.f.y, rotation.f.z, rotation.f.w), new Vec3(rotation.u.y, rotation.u.z, rotation.u.w), new Vec3(origin.y, origin.z, origin.w)) - rotation.s.y * Determinant3X3(new Vec3(rotation.f.x, rotation.f.z, rotation.f.w), new Vec3(rotation.u.x, rotation.u.z, rotation.u.w), new Vec3(origin.x, origin.z, origin.w)) + rotation.s.z * Determinant3X3(new Vec3(rotation.f.x, rotation.f.y, rotation.f.w), new Vec3(rotation.u.x, rotation.u.y, rotation.u.w), new Vec3(origin.x, origin.y, origin.w)) - rotation.s.w * Determinant3X3(new Vec3(rotation.f.x, rotation.f.y, rotation.f.z), new Vec3(rotation.u.x, rotation.u.y, rotation.u.z), new Vec3(origin.x, origin.y, origin.z));
	}

	private static float Determinant3X3(in Vec3 a, in Vec3 b, in Vec3 c)
	{
		return a.x * (b.y * c.z - b.z * c.y) - a.y * (b.x * c.z - b.z * c.x) + a.z * (b.x * c.y - b.y * c.x);
	}

	private static void DivideWith(ref MatrixFrame matrix, float w)
	{
		float num = 1f / w;
		matrix.rotation.s.x *= num;
		matrix.rotation.s.y *= num;
		matrix.rotation.s.z *= num;
		matrix.rotation.s.w *= num;
		matrix.rotation.f.x *= num;
		matrix.rotation.f.y *= num;
		matrix.rotation.f.z *= num;
		matrix.rotation.f.w *= num;
		matrix.rotation.u.x *= num;
		matrix.rotation.u.y *= num;
		matrix.rotation.u.z *= num;
		matrix.rotation.u.w *= num;
		matrix.origin.x *= num;
		matrix.origin.y *= num;
		matrix.origin.z *= num;
		matrix.origin.w *= num;
	}

	public void Rotate(float radian, in Vec3 axis)
	{
		MathF.SinCos(radian, out var sa, out var ca);
		MatrixFrame matrixFrame = default(MatrixFrame);
		matrixFrame.rotation.s.x = axis.x * axis.x * (1f - ca) + ca;
		matrixFrame.rotation.f.x = axis.x * axis.y * (1f - ca) - axis.z * sa;
		matrixFrame.rotation.u.x = axis.x * axis.z * (1f - ca) + axis.y * sa;
		matrixFrame.origin.x = 0f;
		matrixFrame.rotation.s.y = axis.y * axis.x * (1f - ca) + axis.z * sa;
		matrixFrame.rotation.f.y = axis.y * axis.y * (1f - ca) + ca;
		matrixFrame.rotation.u.y = axis.y * axis.z * (1f - ca) - axis.x * sa;
		matrixFrame.origin.y = 0f;
		matrixFrame.rotation.s.z = axis.x * axis.z * (1f - ca) - axis.y * sa;
		matrixFrame.rotation.f.z = axis.y * axis.z * (1f - ca) + axis.x * sa;
		matrixFrame.rotation.u.z = axis.z * axis.z * (1f - ca) + ca;
		matrixFrame.origin.z = 0f;
		matrixFrame.rotation.s.w = 0f;
		matrixFrame.rotation.f.w = 0f;
		matrixFrame.rotation.u.w = 0f;
		matrixFrame.origin.w = 1f;
		origin = TransformToParent(in matrixFrame.origin);
		rotation = rotation.TransformToParent(in matrixFrame.rotation);
	}

	public static MatrixFrame operator *(in MatrixFrame m1, in MatrixFrame m2)
	{
		return m1.TransformToParent(in m2);
	}

	public static bool operator ==(in MatrixFrame m1, in MatrixFrame m2)
	{
		if (m1.origin == m2.origin)
		{
			return m1.rotation == m2.rotation;
		}
		return false;
	}

	public static bool operator !=(in MatrixFrame m1, in MatrixFrame m2)
	{
		if (!(m1.origin != m2.origin))
		{
			return m1.rotation != m2.rotation;
		}
		return true;
	}

	public override string ToString()
	{
		string text = "MatrixFrame:\n";
		text += "Rotation:\n";
		text += rotation.ToString();
		return text + "Origin: " + origin.x + ", " + origin.y + ", " + origin.z + "\n";
	}

	public override bool Equals(object obj)
	{
		return this == (MatrixFrame)obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public MatrixFrame Strafe(float a)
	{
		origin += rotation.s * a;
		return this;
	}

	public MatrixFrame Advance(float a)
	{
		origin += rotation.f * a;
		return this;
	}

	public MatrixFrame Elevate(float a)
	{
		origin += rotation.u * a;
		return this;
	}

	public void Scale(in Vec3 scalingVector)
	{
		MatrixFrame identity = Identity;
		identity.rotation.s.x = scalingVector.x;
		identity.rotation.f.y = scalingVector.y;
		identity.rotation.u.z = scalingVector.z;
		origin = TransformToParent(in identity.origin);
		rotation = rotation.TransformToParent(in identity.rotation);
	}

	public Vec3 GetScale()
	{
		return new Vec3(rotation.s.Length, rotation.f.Length, rotation.u.Length);
	}

	public static MatrixFrame CreateLookAt(in Vec3 position, in Vec3 target, in Vec3 upVector)
	{
		Vec3 vec = target - position;
		vec.Normalize();
		Vec3 vec2 = Vec3.CrossProduct(upVector, vec);
		vec2.Normalize();
		Vec3 v = Vec3.CrossProduct(vec, vec2);
		float x = vec2.x;
		float x2 = v.x;
		float x3 = vec.x;
		float _ = 0f;
		float y = vec2.y;
		float y2 = v.y;
		float y3 = vec.y;
		float _2 = 0f;
		float z = vec2.z;
		float z2 = v.z;
		float z3 = vec.z;
		float _3 = 0f;
		float _4 = 0f - Vec3.DotProduct(vec2, position);
		float _5 = 0f - Vec3.DotProduct(v, position);
		float _6 = 0f - Vec3.DotProduct(vec, position);
		float _7 = 1f;
		return new MatrixFrame(x, x2, x3, _, y, y2, y3, _2, z, z2, z3, _3, _4, _5, _6, _7);
	}

	public static MatrixFrame CenterFrameOfTwoPoints(in Vec3 p1, in Vec3 p2, Vec3 upVector)
	{
		MatrixFrame result = default(MatrixFrame);
		result.origin = (p1 + p2) * 0.5f;
		result.rotation.s = p2 - p1;
		result.rotation.s.Normalize();
		if (MathF.Abs(Vec3.DotProduct(result.rotation.s, upVector)) > 0.95f)
		{
			upVector = new Vec3(0f, 1f);
		}
		result.rotation.u = upVector;
		result.rotation.f = Vec3.CrossProduct(result.rotation.u, result.rotation.s);
		result.rotation.f.Normalize();
		result.rotation.u = Vec3.CrossProduct(result.rotation.s, result.rotation.f);
		result.Fill();
		return result;
	}

	public void Fill()
	{
		rotation.s.w = 0f;
		rotation.f.w = 0f;
		rotation.u.w = 0f;
		origin.w = 1f;
	}

	public MatrixFrame Filled()
	{
		MatrixFrame result = this;
		result.Fill();
		return result;
	}

	private void AssertFilled()
	{
	}
}
