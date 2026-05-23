using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000065 RID: 101
	[Serializable]
	public struct MatrixFrame
	{
		// Token: 0x060002F3 RID: 755 RVA: 0x00009782 File Offset: 0x00007982
		public MatrixFrame(in Mat3 rot, in Vec3 o)
		{
			this.rotation = rot;
			this.origin = o;
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x0000979C File Offset: 0x0000799C
		public MatrixFrame(float _11, float _12, float _13, float _21, float _22, float _23, float _31, float _32, float _33, float _41, float _42, float _43)
		{
			this.rotation = new Mat3(_11, _12, _13, _21, _22, _23, _31, _32, _33);
			this.origin = new Vec3(_41, _42, _43, -1f);
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x000097DC File Offset: 0x000079DC
		public MatrixFrame(float _11, float _12, float _13, float _14, float _21, float _22, float _23, float _24, float _31, float _32, float _33, float _34, float _41, float _42, float _43, float _44)
		{
			this.rotation = new Mat3
			{
				s = new Vec3(_11, _12, _13, _14),
				f = new Vec3(_21, _22, _23, _24),
				u = new Vec3(_31, _32, _33, _34)
			};
			this.origin = new Vec3(_41, _42, _43, _44);
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x00009844 File Offset: 0x00007A44
		public Vec3 TransformToParent(in Vec3 v)
		{
			return new Vec3(this.rotation.s.x * v.x + this.rotation.f.x * v.y + this.rotation.u.x * v.z + this.origin.x, this.rotation.s.y * v.x + this.rotation.f.y * v.y + this.rotation.u.y * v.z + this.origin.y, this.rotation.s.z * v.x + this.rotation.f.z * v.y + this.rotation.u.z * v.z + this.origin.z, -1f);
		}

		// Token: 0x060002F7 RID: 759 RVA: 0x00009954 File Offset: 0x00007B54
		public Vec3 TransformToParentDouble(in Vec3 v)
		{
			return new Vec3((float)((double)this.rotation.s.x * (double)v.x + (double)this.rotation.f.x * (double)v.y + (double)this.rotation.u.x * (double)v.z + (double)this.origin.x), (float)((double)this.rotation.s.y * (double)v.x + (double)this.rotation.f.y * (double)v.y + (double)this.rotation.u.y * (double)v.z + (double)this.origin.y), (float)((double)this.rotation.s.z * (double)v.x + (double)this.rotation.f.z * (double)v.y + (double)this.rotation.u.z * (double)v.z + (double)this.origin.z), -1f);
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x00009A7C File Offset: 0x00007C7C
		public Vec2 TransformToParent(in Vec2 v)
		{
			return new Vec2(this.rotation.s.x * v.x + this.rotation.f.x * v.y + this.origin.x, this.rotation.s.y * v.x + this.rotation.f.y * v.y + this.origin.y);
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x00009B04 File Offset: 0x00007D04
		public Vec3 TransformToLocal(in Vec3 v)
		{
			Vec3 vec = v - this.origin;
			return new Vec3(this.rotation.s.x * vec.x + this.rotation.s.y * vec.y + this.rotation.s.z * vec.z, this.rotation.f.x * vec.x + this.rotation.f.y * vec.y + this.rotation.f.z * vec.z, this.rotation.u.x * vec.x + this.rotation.u.y * vec.y + this.rotation.u.z * vec.z, -1f);
		}

		// Token: 0x060002FA RID: 762 RVA: 0x00009C04 File Offset: 0x00007E04
		public Vec3 TransformToLocalNonUnit(in Vec3 v)
		{
			Vec3 vec = v - this.origin;
			return new Vec3(this.rotation.s.x * vec.x + this.rotation.s.y * vec.y + this.rotation.s.z * vec.z, this.rotation.f.x * vec.x + this.rotation.f.y * vec.y + this.rotation.f.z * vec.z, this.rotation.u.x * vec.x + this.rotation.u.y * vec.y + this.rotation.u.z * vec.z, -1f);
		}

		// Token: 0x060002FB RID: 763 RVA: 0x00009D02 File Offset: 0x00007F02
		public bool NearlyEquals(MatrixFrame rhs, float epsilon = 1E-05f)
		{
			return this.rotation.NearlyEquals(rhs.rotation, epsilon) && this.origin.NearlyEquals(rhs.origin, epsilon);
		}

		// Token: 0x060002FC RID: 764 RVA: 0x00009D30 File Offset: 0x00007F30
		public Vec3 TransformToLocalNonOrthogonal(in Vec3 v)
		{
			MatrixFrame matrixFrame = new MatrixFrame(this.rotation.s.x, this.rotation.s.y, this.rotation.s.z, 0f, this.rotation.f.x, this.rotation.f.y, this.rotation.f.z, 0f, this.rotation.u.x, this.rotation.u.y, this.rotation.u.z, 0f, this.origin.x, this.origin.y, this.origin.z, 1f);
			return matrixFrame.Inverse().TransformToParent(v);
		}

		// Token: 0x060002FD RID: 765 RVA: 0x00009E1C File Offset: 0x0000801C
		public MatrixFrame TransformToLocalNonOrthogonal(in MatrixFrame frame)
		{
			MatrixFrame matrixFrame = new MatrixFrame(this.rotation.s.x, this.rotation.s.y, this.rotation.s.z, 0f, this.rotation.f.x, this.rotation.f.y, this.rotation.f.z, 0f, this.rotation.u.x, this.rotation.u.y, this.rotation.u.z, 0f, this.origin.x, this.origin.y, this.origin.z, 1f);
			return matrixFrame.Inverse().TransformToParent(frame);
		}

		// Token: 0x060002FE RID: 766 RVA: 0x00009F08 File Offset: 0x00008108
		public static MatrixFrame Lerp(in MatrixFrame m1, in MatrixFrame m2, float alpha)
		{
			MatrixFrame result;
			result.rotation = Mat3.Lerp(m1.rotation, m2.rotation, alpha);
			result.origin = Vec3.Lerp(m1.origin, m2.origin, alpha);
			return result;
		}

		// Token: 0x060002FF RID: 767 RVA: 0x00009F48 File Offset: 0x00008148
		public static MatrixFrame LerpNonOrthogonal(in MatrixFrame m1, in MatrixFrame m2, float alpha)
		{
			MatrixFrame result;
			result.rotation = Mat3.LerpNonOrthogonal(m1.rotation, m2.rotation, alpha);
			result.origin = Vec3.Lerp(m1.origin, m2.origin, alpha);
			result.Fill();
			return result;
		}

		// Token: 0x06000300 RID: 768 RVA: 0x00009F90 File Offset: 0x00008190
		public static MatrixFrame Slerp(in MatrixFrame m1, in MatrixFrame m2, float alpha)
		{
			MatrixFrame result;
			result.origin = Vec3.Lerp(m1.origin, m2.origin, alpha);
			result.rotation = Quaternion.Slerp(Quaternion.QuaternionFromMat3(m1.rotation), Quaternion.QuaternionFromMat3(m2.rotation), alpha).ToMat3();
			return result;
		}

		// Token: 0x06000301 RID: 769 RVA: 0x00009FE4 File Offset: 0x000081E4
		public MatrixFrame TransformToParent(in MatrixFrame m)
		{
			Mat3 mat = this.rotation.TransformToParent(m.rotation);
			Vec3 vec = this.TransformToParent(m.origin);
			return new MatrixFrame(ref mat, ref vec);
		}

		// Token: 0x06000302 RID: 770 RVA: 0x0000A01C File Offset: 0x0000821C
		public MatrixFrame TransformToLocal(in MatrixFrame m)
		{
			Mat3 mat = this.rotation.TransformToLocal(m.rotation);
			Vec3 vec = this.TransformToLocal(m.origin);
			return new MatrixFrame(ref mat, ref vec);
		}

		// Token: 0x06000303 RID: 771 RVA: 0x0000A054 File Offset: 0x00008254
		public Vec3 TransformToParentWithW(Vec3 _s)
		{
			return new Vec3(this.rotation.s.x * _s.x + this.rotation.f.x * _s.y + this.rotation.u.x * _s.z + this.origin.x * _s.w, this.rotation.s.y * _s.x + this.rotation.f.y * _s.y + this.rotation.u.y * _s.z + this.origin.y * _s.w, this.rotation.s.z * _s.x + this.rotation.f.z * _s.y + this.rotation.u.z * _s.z + this.origin.z * _s.w, this.rotation.s.w * _s.x + this.rotation.f.w * _s.y + this.rotation.u.w * _s.z + this.origin.w * _s.w);
		}

		// Token: 0x06000304 RID: 772 RVA: 0x0000A1D0 File Offset: 0x000083D0
		public MatrixFrame GetUnitRotFrame(float removedScale)
		{
			Mat3 unitRotation = this.rotation.GetUnitRotation(removedScale);
			return new MatrixFrame(ref unitRotation, ref this.origin);
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000305 RID: 773 RVA: 0x0000A1F8 File Offset: 0x000083F8
		public static MatrixFrame Identity
		{
			get
			{
				Mat3 identity = Mat3.Identity;
				Vec3 vec = new Vec3(0f, 0f, 0f, 1f);
				return new MatrixFrame(ref identity, ref vec);
			}
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000306 RID: 774 RVA: 0x0000A230 File Offset: 0x00008430
		public static MatrixFrame Zero
		{
			get
			{
				Mat3 mat = new Mat3(ref Vec3.Zero, ref Vec3.Zero, ref Vec3.Zero);
				Vec3 vec = new Vec3(0f, 0f, 0f, 1f);
				return new MatrixFrame(ref mat, ref vec);
			}
		}

		// Token: 0x06000307 RID: 775 RVA: 0x0000A278 File Offset: 0x00008478
		public MatrixFrame InverseFast()
		{
			this.AssertFilled();
			MatrixFrame matrixFrame = default(MatrixFrame);
			float num = this.rotation.u.z * this.origin.w - this.rotation.u.w * this.origin.z;
			float num2 = this.rotation.f.z * this.origin.w - this.rotation.f.w * this.origin.z;
			float num3 = this.rotation.f.z * this.rotation.u.w - this.rotation.f.w * this.rotation.u.z;
			float num4 = this.rotation.s.z * this.origin.w - this.rotation.s.w * this.origin.z;
			float num5 = this.rotation.s.z * this.rotation.u.w - this.rotation.s.w * this.rotation.u.z;
			float num6 = this.rotation.s.z * this.rotation.f.w - this.rotation.s.w * this.rotation.f.z;
			float num7 = this.rotation.u.y * this.origin.w - this.rotation.u.w * this.origin.y;
			float num8 = this.rotation.f.y * this.origin.w - this.rotation.f.w * this.origin.y;
			float num9 = this.rotation.f.y * this.rotation.u.w - this.rotation.f.w * this.rotation.u.y;
			float num10 = this.rotation.s.y * this.origin.w - this.rotation.s.w * this.origin.y;
			float num11 = this.rotation.s.y * this.rotation.u.w - this.rotation.s.w * this.rotation.u.y;
			float num12 = this.rotation.f.y * this.origin.w - this.rotation.f.w * this.origin.y;
			float num13 = this.rotation.s.y * this.rotation.f.w - this.rotation.s.w * this.rotation.f.y;
			float num14 = this.rotation.u.y * this.origin.z - this.rotation.u.z * this.origin.y;
			float num15 = this.rotation.f.y * this.origin.z - this.rotation.f.z * this.origin.y;
			float num16 = this.rotation.f.y * this.rotation.u.z - this.rotation.f.z * this.rotation.u.y;
			float num17 = this.rotation.s.y * this.origin.z - this.rotation.s.z * this.origin.y;
			float num18 = this.rotation.s.y * this.rotation.u.z - this.rotation.s.z * this.rotation.u.y;
			float num19 = this.rotation.s.y * this.rotation.f.z - this.rotation.s.z * this.rotation.f.y;
			matrixFrame.rotation.s.x = this.rotation.f.y * num - this.rotation.u.y * num2 + this.origin.y * num3;
			matrixFrame.rotation.s.y = -this.rotation.s.y * num + this.rotation.u.y * num4 - this.origin.y * num5;
			matrixFrame.rotation.s.z = this.rotation.s.y * num2 - this.rotation.f.y * num4 + this.origin.y * num6;
			matrixFrame.rotation.s.w = -this.rotation.s.y * num3 + this.rotation.f.y * num5 - this.rotation.u.y * num6;
			matrixFrame.rotation.f.x = -this.rotation.f.x * num + this.rotation.u.x * num2 - this.origin.x * num3;
			matrixFrame.rotation.f.y = this.rotation.s.x * num - this.rotation.u.x * num4 + this.origin.x * num5;
			matrixFrame.rotation.f.z = -this.rotation.s.x * num2 + this.rotation.f.x * num4 - this.origin.x * num6;
			matrixFrame.rotation.f.w = this.rotation.s.x * num3 - this.rotation.f.x * num5 + this.rotation.u.x * num6;
			matrixFrame.rotation.u.x = this.rotation.f.x * num7 - this.rotation.u.x * num8 + this.origin.x * num9;
			matrixFrame.rotation.u.y = -this.rotation.s.x * num7 + this.rotation.u.x * num10 - this.origin.x * num11;
			matrixFrame.rotation.u.z = this.rotation.s.x * num12 - this.rotation.f.x * num10 + this.origin.x * num13;
			matrixFrame.rotation.u.w = -this.rotation.s.x * num9 + this.rotation.f.x * num11 - this.rotation.u.x * num13;
			matrixFrame.origin.x = -this.rotation.f.x * num14 + this.rotation.u.x * num15 - this.origin.x * num16;
			matrixFrame.origin.y = this.rotation.s.x * num14 - this.rotation.u.x * num17 + this.origin.x * num18;
			matrixFrame.origin.z = -this.rotation.s.x * num15 + this.rotation.f.x * num17 - this.origin.x * num19;
			matrixFrame.origin.w = this.rotation.s.x * num16 - this.rotation.f.x * num18 + this.rotation.u.x * num19;
			float num20 = this.rotation.s.x * matrixFrame.rotation.s.x + this.rotation.f.x * matrixFrame.rotation.s.y + this.rotation.u.x * matrixFrame.rotation.s.z + this.origin.x * matrixFrame.rotation.s.w;
			if (num20 != 1f)
			{
				MatrixFrame.DivideWith(ref matrixFrame, num20);
			}
			return matrixFrame;
		}

		// Token: 0x06000308 RID: 776 RVA: 0x0000AC4E File Offset: 0x00008E4E
		public MatrixFrame Inverse()
		{
			return this.InverseFast();
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0000AC58 File Offset: 0x00008E58
		public float Determinant4X4()
		{
			float x = this.rotation.s.x;
			Vec3 vec = new Vec3(this.rotation.f.y, this.rotation.f.z, this.rotation.f.w, -1f);
			Vec3 vec2 = new Vec3(this.rotation.u.y, this.rotation.u.z, this.rotation.u.w, -1f);
			Vec3 vec3 = new Vec3(this.origin.y, this.origin.z, this.origin.w, -1f);
			float num = x * MatrixFrame.Determinant3X3(vec, vec2, vec3);
			float y = this.rotation.s.y;
			Vec3 vec4 = new Vec3(this.rotation.f.x, this.rotation.f.z, this.rotation.f.w, -1f);
			Vec3 vec5 = new Vec3(this.rotation.u.x, this.rotation.u.z, this.rotation.u.w, -1f);
			Vec3 vec6 = new Vec3(this.origin.x, this.origin.z, this.origin.w, -1f);
			float num2 = num - y * MatrixFrame.Determinant3X3(vec4, vec5, vec6);
			float z = this.rotation.s.z;
			Vec3 vec7 = new Vec3(this.rotation.f.x, this.rotation.f.y, this.rotation.f.w, -1f);
			Vec3 vec8 = new Vec3(this.rotation.u.x, this.rotation.u.y, this.rotation.u.w, -1f);
			Vec3 vec9 = new Vec3(this.origin.x, this.origin.y, this.origin.w, -1f);
			float num3 = num2 + z * MatrixFrame.Determinant3X3(vec7, vec8, vec9);
			float w = this.rotation.s.w;
			Vec3 vec10 = new Vec3(this.rotation.f.x, this.rotation.f.y, this.rotation.f.z, -1f);
			Vec3 vec11 = new Vec3(this.rotation.u.x, this.rotation.u.y, this.rotation.u.z, -1f);
			Vec3 vec12 = new Vec3(this.origin.x, this.origin.y, this.origin.z, -1f);
			return num3 - w * MatrixFrame.Determinant3X3(vec10, vec11, vec12);
		}

		// Token: 0x0600030A RID: 778 RVA: 0x0000AF68 File Offset: 0x00009168
		private static float Determinant3X3(in Vec3 a, in Vec3 b, in Vec3 c)
		{
			return a.x * (b.y * c.z - b.z * c.y) - a.y * (b.x * c.z - b.z * c.x) + a.z * (b.x * c.y - b.y * c.x);
		}

		// Token: 0x0600030B RID: 779 RVA: 0x0000AFE0 File Offset: 0x000091E0
		private static void DivideWith(ref MatrixFrame matrix, float w)
		{
			float num = 1f / w;
			matrix.rotation.s.x = matrix.rotation.s.x * num;
			matrix.rotation.s.y = matrix.rotation.s.y * num;
			matrix.rotation.s.z = matrix.rotation.s.z * num;
			matrix.rotation.s.w = matrix.rotation.s.w * num;
			matrix.rotation.f.x = matrix.rotation.f.x * num;
			matrix.rotation.f.y = matrix.rotation.f.y * num;
			matrix.rotation.f.z = matrix.rotation.f.z * num;
			matrix.rotation.f.w = matrix.rotation.f.w * num;
			matrix.rotation.u.x = matrix.rotation.u.x * num;
			matrix.rotation.u.y = matrix.rotation.u.y * num;
			matrix.rotation.u.z = matrix.rotation.u.z * num;
			matrix.rotation.u.w = matrix.rotation.u.w * num;
			matrix.origin.x = matrix.origin.x * num;
			matrix.origin.y = matrix.origin.y * num;
			matrix.origin.z = matrix.origin.z * num;
			matrix.origin.w = matrix.origin.w * num;
		}

		// Token: 0x0600030C RID: 780 RVA: 0x0000B134 File Offset: 0x00009334
		public void Rotate(float radian, in Vec3 axis)
		{
			float num;
			float num2;
			MathF.SinCos(radian, out num, out num2);
			MatrixFrame matrixFrame = default(MatrixFrame);
			matrixFrame.rotation.s.x = axis.x * axis.x * (1f - num2) + num2;
			matrixFrame.rotation.f.x = axis.x * axis.y * (1f - num2) - axis.z * num;
			matrixFrame.rotation.u.x = axis.x * axis.z * (1f - num2) + axis.y * num;
			matrixFrame.origin.x = 0f;
			matrixFrame.rotation.s.y = axis.y * axis.x * (1f - num2) + axis.z * num;
			matrixFrame.rotation.f.y = axis.y * axis.y * (1f - num2) + num2;
			matrixFrame.rotation.u.y = axis.y * axis.z * (1f - num2) - axis.x * num;
			matrixFrame.origin.y = 0f;
			matrixFrame.rotation.s.z = axis.x * axis.z * (1f - num2) - axis.y * num;
			matrixFrame.rotation.f.z = axis.y * axis.z * (1f - num2) + axis.x * num;
			matrixFrame.rotation.u.z = axis.z * axis.z * (1f - num2) + num2;
			matrixFrame.origin.z = 0f;
			matrixFrame.rotation.s.w = 0f;
			matrixFrame.rotation.f.w = 0f;
			matrixFrame.rotation.u.w = 0f;
			matrixFrame.origin.w = 1f;
			this.origin = this.TransformToParent(matrixFrame.origin);
			this.rotation = this.rotation.TransformToParent(matrixFrame.rotation);
		}

		// Token: 0x0600030D RID: 781 RVA: 0x0000B398 File Offset: 0x00009598
		public static MatrixFrame operator *(in MatrixFrame m1, in MatrixFrame m2)
		{
			MatrixFrame matrixFrame = m1;
			return matrixFrame.TransformToParent(m2);
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0000B3B4 File Offset: 0x000095B4
		public static bool operator ==(in MatrixFrame m1, in MatrixFrame m2)
		{
			return m1.origin == m2.origin && (m1.rotation) == (m2.rotation);
		}

		// Token: 0x0600030F RID: 783 RVA: 0x0000B3DC File Offset: 0x000095DC
		public static bool operator !=(in MatrixFrame m1, in MatrixFrame m2)
		{
			return m1.origin != m2.origin || (m1.rotation) != (m2.rotation);
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0000B404 File Offset: 0x00009604
		public override string ToString()
		{
			string text = "MatrixFrame:\n";
			text += "Rotation:\n";
			text += this.rotation.ToString();
			return string.Concat(new object[]
			{
				text,
				"Origin: ",
				this.origin.x,
				", ",
				this.origin.y,
				", ",
				this.origin.z,
				"\n"
			});
		}

		// Token: 0x06000311 RID: 785 RVA: 0x0000B4A8 File Offset: 0x000096A8
		public override bool Equals(object obj)
		{
			MatrixFrame matrixFrame = (MatrixFrame)obj;
			return (this) == (matrixFrame);
		}

		// Token: 0x06000312 RID: 786 RVA: 0x0000B4C4 File Offset: 0x000096C4
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06000313 RID: 787 RVA: 0x0000B4D6 File Offset: 0x000096D6
		public MatrixFrame Strafe(float a)
		{
			this.origin += this.rotation.s * a;
			return this;
		}

		// Token: 0x06000314 RID: 788 RVA: 0x0000B500 File Offset: 0x00009700
		public MatrixFrame Advance(float a)
		{
			this.origin += this.rotation.f * a;
			return this;
		}

		// Token: 0x06000315 RID: 789 RVA: 0x0000B52A File Offset: 0x0000972A
		public MatrixFrame Elevate(float a)
		{
			this.origin += this.rotation.u * a;
			return this;
		}

		// Token: 0x06000316 RID: 790 RVA: 0x0000B554 File Offset: 0x00009754
		public void Scale(in Vec3 scalingVector)
		{
			MatrixFrame identity = MatrixFrame.Identity;
			identity.rotation.s.x = scalingVector.x;
			identity.rotation.f.y = scalingVector.y;
			identity.rotation.u.z = scalingVector.z;
			this.origin = this.TransformToParent(identity.origin);
			this.rotation = this.rotation.TransformToParent(identity.rotation);
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0000B5D7 File Offset: 0x000097D7
		public Vec3 GetScale()
		{
			return new Vec3(this.rotation.s.Length, this.rotation.f.Length, this.rotation.u.Length, -1f);
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000318 RID: 792 RVA: 0x0000B613 File Offset: 0x00009813
		public bool IsIdentity
		{
			get
			{
				return !this.origin.IsNonZero && this.rotation.IsIdentity();
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000319 RID: 793 RVA: 0x0000B62F File Offset: 0x0000982F
		public bool IsZero
		{
			get
			{
				return !this.origin.IsNonZero && this.rotation.IsZero();
			}
		}

		// Token: 0x17000049 RID: 73
		public Vec3 this[int i]
		{
			get
			{
				Vec3 result;
				switch (i)
				{
				case 0:
					result = this.rotation.s;
					break;
				case 1:
					result = this.rotation.f;
					break;
				case 2:
					result = this.rotation.u;
					break;
				case 3:
					result = this.origin;
					break;
				default:
					throw new IndexOutOfRangeException("MatrixFrame out of bounds.");
				}
				return result;
			}
			set
			{
				switch (i)
				{
				case 0:
					this.rotation.s = value;
					return;
				case 1:
					this.rotation.f = value;
					return;
				case 2:
					this.rotation.u = value;
					return;
				case 3:
					this.origin = value;
					return;
				default:
					throw new IndexOutOfRangeException("MatrixFrame out of bounds.");
				}
			}
		}

		// Token: 0x1700004A RID: 74
		public float this[int i, int j]
		{
			get
			{
				float result;
				switch (i)
				{
				case 0:
					result = this.rotation.s[j];
					break;
				case 1:
					result = this.rotation.f[j];
					break;
				case 2:
					result = this.rotation.u[j];
					break;
				case 3:
					result = this.origin[j];
					break;
				default:
					throw new IndexOutOfRangeException("MatrixFrame out of bounds.");
				}
				return result;
			}
			set
			{
				switch (i)
				{
				case 0:
					this.rotation.s[j] = value;
					return;
				case 1:
					this.rotation.f[j] = value;
					return;
				case 2:
					this.rotation.u[j] = value;
					return;
				case 3:
					this.origin[j] = value;
					return;
				default:
					throw new IndexOutOfRangeException("MatrixFrame out of bounds.");
				}
			}
		}

		// Token: 0x0600031E RID: 798 RVA: 0x0000B804 File Offset: 0x00009A04
		public static MatrixFrame CreateLookAt(in Vec3 position, in Vec3 target, in Vec3 upVector)
		{
			Vec3 vec = target - position;
			vec.Normalize();
			Vec3 vec2 = Vec3.CrossProduct(upVector, vec);
			vec2.Normalize();
			Vec3 vec3 = Vec3.CrossProduct(vec, vec2);
			float x = vec2.x;
			float x2 = vec3.x;
			float x3 = vec.x;
			float <<EMPTY_NAME>> = 0f;
			float y = vec2.y;
			float y2 = vec3.y;
			float y3 = vec.y;
			float 2 = 0f;
			float z = vec2.z;
			float z2 = vec3.z;
			float z3 = vec.z;
			float 3 = 0f;
			float 4 = -Vec3.DotProduct(vec2, position);
			float 5 = -Vec3.DotProduct(vec3, position);
			float 6 = -Vec3.DotProduct(vec, position);
			float 7 = 1f;
			return new MatrixFrame(x, x2, x3, <<EMPTY_NAME>>, y, y2, y3, 2, z, z2, z3, 3, 4, 5, 6, 7);
		}

		// Token: 0x0600031F RID: 799 RVA: 0x0000B8F8 File Offset: 0x00009AF8
		public static MatrixFrame CenterFrameOfTwoPoints(in Vec3 p1, in Vec3 p2, Vec3 upVector)
		{
			MatrixFrame matrixFrame;
			matrixFrame.origin = (p1 + p2) * 0.5f;
			matrixFrame.rotation.s = p2 - p1;
			matrixFrame.rotation.s.Normalize();
			if (MathF.Abs(Vec3.DotProduct(matrixFrame.rotation.s, upVector)) > 0.95f)
			{
				upVector = new Vec3(0f, 1f, 0f, -1f);
			}
			matrixFrame.rotation.u = upVector;
			matrixFrame.rotation.f = Vec3.CrossProduct(matrixFrame.rotation.u, matrixFrame.rotation.s);
			matrixFrame.rotation.f.Normalize();
			matrixFrame.rotation.u = Vec3.CrossProduct(matrixFrame.rotation.s, matrixFrame.rotation.f);
			matrixFrame.Fill();
			return matrixFrame;
		}

		// Token: 0x06000320 RID: 800 RVA: 0x0000BA04 File Offset: 0x00009C04
		public void Fill()
		{
			this.rotation.s.w = 0f;
			this.rotation.f.w = 0f;
			this.rotation.u.w = 0f;
			this.origin.w = 1f;
		}

		// Token: 0x06000321 RID: 801 RVA: 0x0000BA60 File Offset: 0x00009C60
		private void AssertFilled()
		{
		}

		// Token: 0x04000128 RID: 296
		public Mat3 rotation;

		// Token: 0x04000129 RID: 297
		public Vec3 origin;
	}
}
