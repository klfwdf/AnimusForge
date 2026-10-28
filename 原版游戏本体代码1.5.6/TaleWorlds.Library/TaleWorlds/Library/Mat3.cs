using System;

namespace TaleWorlds.Library;

[Serializable]
public struct Mat3
{
	public Vec3 s;

	public Vec3 f;

	public Vec3 u;

	public Vec3 this[int i]
	{
		get
		{
			return i switch
			{
				0 => s, 
				1 => f, 
				2 => u, 
				_ => throw new IndexOutOfRangeException("Vec3 out of bounds."), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				s = value;
				break;
			case 1:
				f = value;
				break;
			case 2:
				u = value;
				break;
			default:
				throw new IndexOutOfRangeException("Vec3 out of bounds.");
			}
		}
	}

	public static Mat3 Identity => new Mat3(new Vec3(1f), new Vec3(0f, 1f), new Vec3(0f, 0f, 1f));

	public Mat3(in Vec3 s, in Vec3 f, in Vec3 u)
	{
		this.s = s;
		this.f = f;
		this.u = u;
	}

	public Mat3(float sx, float sy, float sz, float fx, float fy, float fz, float ux, float uy, float uz)
	{
		s = new Vec3(sx, sy, sz);
		f = new Vec3(fx, fy, fz);
		u = new Vec3(ux, uy, uz);
	}

	public void RotateAboutSide(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		Vec3 vec = f * ca + u * sa;
		Vec3 vec2 = u * ca - f * sa;
		u = vec2;
		f = vec;
	}

	public void RotateAboutForward(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		Vec3 vec = s * ca - u * sa;
		Vec3 vec2 = u * ca + s * sa;
		s = vec;
		u = vec2;
	}

	public void RotateAboutUp(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		Vec3 vec = s * ca + f * sa;
		Vec3 vec2 = f * ca - s * sa;
		s = vec;
		f = vec2;
	}

	public void RotateAboutAnArbitraryVector(in Vec3 v, float a)
	{
		s = s.RotateAboutAnArbitraryVector(v, a);
		f = f.RotateAboutAnArbitraryVector(v, a);
		u = u.RotateAboutAnArbitraryVector(v, a);
	}

	public bool IsOrthonormal()
	{
		bool result = s.IsUnit && f.IsUnit && u.IsUnit;
		float num = Vec3.DotProduct(s, f);
		if (num > 0.01f || num < -0.01f)
		{
			result = false;
		}
		else
		{
			Vec3 v = Vec3.CrossProduct(s, f);
			if (!u.NearlyEquals(in v, 0.01f))
			{
				result = false;
			}
		}
		return result;
	}

	public bool IsLeftHanded()
	{
		return Vec3.DotProduct(Vec3.CrossProduct(s, f), u) < 0f;
	}

	public bool NearlyEquals(in Mat3 rhs, float epsilon = 1E-05f)
	{
		if (s.NearlyEquals(in rhs.s, epsilon) && f.NearlyEquals(in rhs.f, epsilon))
		{
			return u.NearlyEquals(in rhs.u, epsilon);
		}
		return false;
	}

	public Vec3 TransformToParent(in Vec3 v)
	{
		return new Vec3(s.x * v.x + f.x * v.y + u.x * v.z, s.y * v.x + f.y * v.y + u.y * v.z, s.z * v.x + f.z * v.y + u.z * v.z);
	}

	public Vec2 TransformToParent(in Vec2 v)
	{
		return new Vec2(s.x * v.x + f.x * v.y, s.y * v.x + f.y * v.y);
	}

	public Vec3 TransformToLocal(in Vec3 v)
	{
		return new Vec3(s.x * v.x + s.y * v.y + s.z * v.z, f.x * v.x + f.y * v.y + f.z * v.z, u.x * v.x + u.y * v.y + u.z * v.z);
	}

	public Vec2 TransformToLocal(in Vec2 v)
	{
		return new Vec2(s.x * v.x + s.y * v.y, f.x * v.x + f.y * v.y);
	}

	public Mat3 TransformToParent(in Mat3 m)
	{
		return new Mat3(TransformToParent(in m.s), TransformToParent(in m.f), TransformToParent(in m.u));
	}

	public Mat3 TransformToLocal(in Mat3 m)
	{
		Mat3 result = default(Mat3);
		result.s = TransformToLocal(in m.s);
		result.f = TransformToLocal(in m.f);
		result.u = TransformToLocal(in m.u);
		return result;
	}

	public void Orthonormalize()
	{
		f.Normalize();
		s = Vec3.CrossProduct(f, u);
		s.Normalize();
		u = Vec3.CrossProduct(s, f);
	}

	public void OrthonormalizeAccordingToForwardAndKeepUpAsZAxis()
	{
		f.z = 0f;
		f.Normalize();
		u = Vec3.Up;
		s = Vec3.CrossProduct(f, u);
	}

	public Mat3 GetUnitRotation(float removedScale)
	{
		float num = 1f / removedScale;
		return new Mat3(s * num, f * num, u * num);
	}

	public Vec3 MakeUnit()
	{
		return new Vec3
		{
			x = s.Normalize(),
			y = f.Normalize(),
			z = u.Normalize()
		};
	}

	public bool IsUnit()
	{
		if (s.IsUnit && f.IsUnit)
		{
			return u.IsUnit;
		}
		return false;
	}

	public void ApplyScaleLocal(float scaleAmount)
	{
		s *= scaleAmount;
		f *= scaleAmount;
		u *= scaleAmount;
	}

	public void ApplyScaleLocal(in Vec3 scaleAmountXYZ)
	{
		s *= scaleAmountXYZ.x;
		f *= scaleAmountXYZ.y;
		u *= scaleAmountXYZ.z;
	}

	public bool HasScale()
	{
		if (s.IsUnit && f.IsUnit)
		{
			return !u.IsUnit;
		}
		return true;
	}

	public Vec3 GetScaleVector()
	{
		return new Vec3(s.Length, f.Length, u.Length);
	}

	public Vec3 GetScaleVectorSquared()
	{
		return new Vec3(s.LengthSquared, f.LengthSquared, u.LengthSquared);
	}

	public void ToQuaternion(out Quaternion quat)
	{
		quat = Quaternion.QuaternionFromMat3(this);
	}

	public Quaternion ToQuaternion()
	{
		return Quaternion.QuaternionFromMat3(this);
	}

	public static Mat3 Lerp(in Mat3 m1, in Mat3 m2, float alpha)
	{
		Mat3 identity = Identity;
		identity.f = Vec3.Lerp(m1.f, m2.f, alpha);
		identity.u = Vec3.Lerp(m1.u, m2.u, alpha);
		identity.Orthonormalize();
		return identity;
	}

	public static Mat3 LerpNonOrthogonal(in Mat3 m1, in Mat3 m2, float alpha)
	{
		Mat3 identity = Identity;
		identity.f = Vec3.Lerp(m1.f, m2.f, alpha);
		identity.u = Vec3.Lerp(m1.u, m2.u, alpha);
		identity.s = Vec3.Lerp(m1.s, m2.s, alpha);
		return identity;
	}

	public static Mat3 Slerp(in Mat3 m1, in Mat3 m2, float alpha)
	{
		return Quaternion.Slerp(Quaternion.QuaternionFromMat3(m1), Quaternion.QuaternionFromMat3(m2), alpha).ToMat3();
	}

	public static Mat3 SlerpFPSIndependent(in Mat3 m1, in Mat3 m2, float alpha)
	{
		float t = MathF.Pow(2f, 0f - alpha);
		return Quaternion.Slerp(Quaternion.QuaternionFromMat3(m2), Quaternion.QuaternionFromMat3(m1), t).ToMat3();
	}

	public static Mat3 CreateMat3WithForward(in Vec3 direction)
	{
		Mat3 identity = Identity;
		identity.f = direction;
		identity.f.Normalize();
		if (MathF.Abs(identity.f.z) < 0.99f)
		{
			identity.u = new Vec3(0f, 0f, 1f);
		}
		else
		{
			identity.u = new Vec3(0f, 1f);
		}
		identity.s = Vec3.CrossProduct(identity.f, identity.u);
		identity.s.Normalize();
		identity.u = Vec3.CrossProduct(identity.s, identity.f);
		identity.u.Normalize();
		return identity;
	}

	public static Mat3 CreateDiagonalMat3(in Vec3 diagonalData)
	{
		return new Mat3(new Vec3(diagonalData.x), new Vec3(0f, diagonalData.y), new Vec3(0f, 0f, diagonalData.z));
	}

	public Vec3 GetEulerAngles()
	{
		Mat3 mat = this;
		mat.Orthonormalize();
		return new Vec3(MathF.Asin(mat.f.z), MathF.Atan2(0f - mat.s.z, mat.u.z), MathF.Atan2(0f - mat.f.x, mat.f.y));
	}

	public Mat3 Transpose()
	{
		return new Mat3(s.x, f.x, u.x, s.y, f.y, u.y, s.z, f.z, u.z);
	}

	public static Mat3 operator *(in Mat3 v, float a)
	{
		return new Mat3(v.s * a, v.f * a, v.u * a);
	}

	public static bool operator ==(in Mat3 m1, in Mat3 m2)
	{
		if (m1.f == m2.f)
		{
			return m1.u == m2.u;
		}
		return false;
	}

	public static bool operator !=(in Mat3 m1, in Mat3 m2)
	{
		if (!(m1.f != m2.f))
		{
			return m1.u != m2.u;
		}
		return true;
	}

	public override string ToString()
	{
		string text = "Mat3: ";
		text = text + "s: " + s.x + ", " + s.y + ", " + s.z + ";";
		text = text + "f: " + f.x + ", " + f.y + ", " + f.z + ";";
		text = text + "u: " + u.x + ", " + u.y + ", " + u.z + ";";
		return text + "\n";
	}

	public override bool Equals(object obj)
	{
		return this == (Mat3)obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public bool IsIdentity()
	{
		if (s.x == 1f && s.y == 0f && s.z == 0f && f.x == 0f && f.y == 1f && f.z == 0f && u.x == 0f && u.y == 0f)
		{
			return u.z == 1f;
		}
		return false;
	}

	public bool IsZero()
	{
		if (s.x == 0f && s.y == 0f && s.z == 0f && f.x == 0f && f.y == 0f && f.z == 0f && u.x == 0f && u.y == 0f)
		{
			return u.z == 0f;
		}
		return false;
	}

	public bool IsUniformScaled()
	{
		Vec3 scaleVectorSquared = GetScaleVectorSquared();
		if (MBMath.ApproximatelyEquals(scaleVectorSquared.x, scaleVectorSquared.y, 0.01f))
		{
			return MBMath.ApproximatelyEquals(scaleVectorSquared.x, scaleVectorSquared.z, 0.01f);
		}
		return false;
	}

	public void ApplyEulerAngles(in Vec3 eulerAngles)
	{
		RotateAboutUp(eulerAngles.z);
		RotateAboutSide(eulerAngles.x);
		RotateAboutForward(eulerAngles.y);
	}
}
