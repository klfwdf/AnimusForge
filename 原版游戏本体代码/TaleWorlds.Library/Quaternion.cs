using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000084 RID: 132
	[Serializable]
	public struct Quaternion
	{
		// Token: 0x060004A8 RID: 1192 RVA: 0x00010879 File Offset: 0x0000EA79
		public Quaternion(float x, float y, float z, float w)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}

		// Token: 0x060004A9 RID: 1193 RVA: 0x00010898 File Offset: 0x0000EA98
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x060004AA RID: 1194 RVA: 0x000108AA File Offset: 0x0000EAAA
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x060004AB RID: 1195 RVA: 0x000108C0 File Offset: 0x0000EAC0
		public static bool operator ==(Quaternion a, Quaternion b)
		{
			return a == b || (a != null && b != null && (a.X == b.X && a.Y == b.Y && a.Z == b.Z) && a.W == b.W);
		}

		// Token: 0x060004AC RID: 1196 RVA: 0x00010929 File Offset: 0x0000EB29
		public static bool operator !=(Quaternion a, Quaternion b)
		{
			return !(a == b);
		}

		// Token: 0x060004AD RID: 1197 RVA: 0x00010935 File Offset: 0x0000EB35
		public static Quaternion operator +(Quaternion a, Quaternion b)
		{
			return new Quaternion(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
		}

		// Token: 0x060004AE RID: 1198 RVA: 0x00010970 File Offset: 0x0000EB70
		public static Quaternion operator -(Quaternion a, Quaternion b)
		{
			return new Quaternion(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
		}

		// Token: 0x060004AF RID: 1199 RVA: 0x000109AB File Offset: 0x0000EBAB
		public static Quaternion operator *(Quaternion a, float b)
		{
			return new Quaternion(a.X * b, a.Y * b, a.Z * b, a.W * b);
		}

		// Token: 0x060004B0 RID: 1200 RVA: 0x000109D2 File Offset: 0x0000EBD2
		public static Quaternion operator *(float s, Quaternion v)
		{
			return v * s;
		}

		// Token: 0x060004B1 RID: 1201 RVA: 0x000109DC File Offset: 0x0000EBDC
		public static Quaternion operator *(Quaternion a, Quaternion b)
		{
			float w = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;
			float x = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
			float y = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
			float z = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;
			return new Quaternion(x, y, z, w);
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x00010AD0 File Offset: 0x0000ECD0
		public static Quaternion operator /(Quaternion v, float s)
		{
			return new Quaternion(v.X / s, v.Y / s, v.Z / s, v.W / s);
		}

		// Token: 0x1700007B RID: 123
		public float this[int i]
		{
			get
			{
				float result;
				switch (i)
				{
				case 0:
					result = this.W;
					break;
				case 1:
					result = this.X;
					break;
				case 2:
					result = this.Y;
					break;
				case 3:
					result = this.Z;
					break;
				default:
					throw new IndexOutOfRangeException("Quaternion out of bounds.");
				}
				return result;
			}
			set
			{
				switch (i)
				{
				case 0:
					this.W = value;
					return;
				case 1:
					this.X = value;
					return;
				case 2:
					this.Y = value;
					return;
				case 3:
					this.Z = value;
					return;
				default:
					throw new IndexOutOfRangeException("Quaternion out of bounds.");
				}
			}
		}

		// Token: 0x060004B5 RID: 1205 RVA: 0x00010BA4 File Offset: 0x0000EDA4
		public float Normalize()
		{
			float num = MathF.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W);
			if (num <= 1E-07f)
			{
				this.X = 0f;
				this.Y = 0f;
				this.Z = 0f;
				this.W = 1f;
			}
			else
			{
				float num2 = 1f / num;
				this.X *= num2;
				this.Y *= num2;
				this.Z *= num2;
				this.W *= num2;
			}
			return num;
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x00010C68 File Offset: 0x0000EE68
		public float SafeNormalize()
		{
			double num = Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
			if (num <= 1E-07)
			{
				this.X = 0f;
				this.Y = 0f;
				this.Z = 0f;
				this.W = 1f;
			}
			else
			{
				this.X = (float)((double)this.X / num);
				this.Y = (float)((double)this.Y / num);
				this.Z = (float)((double)this.Z / num);
				this.W = (float)((double)this.W / num);
			}
			return (float)num;
		}

		// Token: 0x060004B7 RID: 1207 RVA: 0x00010D38 File Offset: 0x0000EF38
		public float NormalizeWeighted()
		{
			float num = this.X * this.X + this.Y * this.Y + this.Z * this.Z;
			if (num <= 1E-09f)
			{
				this.X = 1f;
				this.Y = 0f;
				this.Z = 0f;
				this.W = 0f;
			}
			else
			{
				this.W = MathF.Sqrt(1f - num);
			}
			return num;
		}

		// Token: 0x060004B8 RID: 1208 RVA: 0x00010DB8 File Offset: 0x0000EFB8
		public void SetToRotationX(float angle)
		{
			float x;
			float w;
			MathF.SinCos(angle * 0.5f, out x, out w);
			this.X = x;
			this.Y = 0f;
			this.Z = 0f;
			this.W = w;
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x00010DFC File Offset: 0x0000EFFC
		public void SetToRotationY(float angle)
		{
			float y;
			float w;
			MathF.SinCos(angle * 0.5f, out y, out w);
			this.X = 0f;
			this.Y = y;
			this.Z = 0f;
			this.W = w;
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x00010E40 File Offset: 0x0000F040
		public void SetToRotationZ(float angle)
		{
			float z;
			float w;
			MathF.SinCos(angle * 0.5f, out z, out w);
			this.X = 0f;
			this.Y = 0f;
			this.Z = z;
			this.W = w;
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x00010E81 File Offset: 0x0000F081
		public void Flip()
		{
			this.X = -this.X;
			this.Y = -this.Y;
			this.Z = -this.Z;
			this.W = -this.W;
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x060004BC RID: 1212 RVA: 0x00010EB7 File Offset: 0x0000F0B7
		public bool IsIdentity
		{
			get
			{
				return this.X == 0f && this.Y == 0f && this.Z == 0f && this.W == 1f;
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x060004BD RID: 1213 RVA: 0x00010EF0 File Offset: 0x0000F0F0
		public bool IsUnit
		{
			get
			{
				return MBMath.ApproximatelyEquals(this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W, 1f, 0.2f);
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x060004BE RID: 1214 RVA: 0x00010F43 File Offset: 0x0000F143
		public static Quaternion Identity
		{
			get
			{
				return new Quaternion(0f, 0f, 0f, 1f);
			}
		}

		// Token: 0x060004BF RID: 1215 RVA: 0x00010F60 File Offset: 0x0000F160
		public Quaternion TransformToParent(Quaternion q)
		{
			return new Quaternion
			{
				X = this.Y * q.Z - this.Z * q.Y + this.W * q.X + this.X * q.W,
				Y = this.Z * q.X - this.X * q.Z + this.W * q.Y + this.Y * q.W,
				Z = this.X * q.Y - this.Y * q.X + this.W * q.Z + this.Z * q.W,
				W = this.W * q.W - (this.X * q.X + this.Y * q.Y + this.Z * q.Z)
			};
		}

		// Token: 0x060004C0 RID: 1216 RVA: 0x00011070 File Offset: 0x0000F270
		public Quaternion TransformToLocal(Quaternion q)
		{
			return new Quaternion
			{
				X = this.Z * q.Y - this.Y * q.Z + this.W * q.X - this.X * q.W,
				Y = this.X * q.Z - this.Z * q.X + this.W * q.Y - this.Y * q.W,
				Z = this.Y * q.X - this.X * q.Y + this.W * q.Z - this.Z * q.W,
				W = this.W * q.W + (this.X * q.X + this.Y * q.Y + this.Z * q.Z)
			};
		}

		// Token: 0x060004C1 RID: 1217 RVA: 0x00011180 File Offset: 0x0000F380
		public Quaternion TransformToLocalWithoutNormalize(Quaternion q)
		{
			return new Quaternion
			{
				X = this.Z * q.Y - this.Y * q.Z + this.W * q.X - this.X * q.W,
				Y = this.X * q.Z - this.Z * q.X + this.W * q.Y - this.Y * q.W,
				Z = this.Y * q.X - this.X * q.Y + this.W * q.Z - this.Z * q.W,
				W = this.W * q.W + (this.X * q.X + this.Y * q.Y + this.Z * q.Z)
			};
		}

		// Token: 0x060004C2 RID: 1218 RVA: 0x00011290 File Offset: 0x0000F490
		public static Quaternion Slerp(Quaternion from, Quaternion to, float t)
		{
			float num = from.Dotp4(to);
			float num2;
			if (num < 0f)
			{
				num = -num;
				num2 = -1f;
			}
			else
			{
				num2 = 1f;
			}
			float num6;
			float num7;
			if (0.9995f >= num)
			{
				float num3 = MathF.Acos(num);
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
			num7 *= num2;
			Quaternion result = default(Quaternion);
			result.X = num6 * from.X + num7 * to.X;
			result.Y = num6 * from.Y + num7 * to.Y;
			result.Z = num6 * from.Z + num7 * to.Z;
			result.W = num6 * from.W + num7 * to.W;
			result.Normalize();
			return result;
		}

		// Token: 0x060004C3 RID: 1219 RVA: 0x00011388 File Offset: 0x0000F588
		public static Quaternion Lerp(Quaternion from, Quaternion to, float t)
		{
			float num = from.Dotp4(to);
			float num2 = 1f - t;
			float num3;
			if (num < 0f)
			{
				num = -num;
				num3 = -t;
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

		// Token: 0x060004C4 RID: 1220 RVA: 0x00011424 File Offset: 0x0000F624
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

		// Token: 0x060004C5 RID: 1221 RVA: 0x0001156C File Offset: 0x0000F76C
		public static Quaternion QuaternionFromEulerAngles(float yaw, float pitch, float roll)
		{
			float num = yaw * 0.017453292f;
			float num2 = pitch * 0.017453292f;
			float num3 = roll * 0.017453292f;
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

		// Token: 0x060004C6 RID: 1222 RVA: 0x00011638 File Offset: 0x0000F838
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
			else if (m.s.x < -m.f.y)
			{
				num = 1f - m.s.x - m.f.y + m.u.z;
				result.W = m.s.y - m.f.x;
				result.X = m.u.x + m.s.z;
				result.Y = m.f.z + m.u.y;
				result.Z = num;
			}
			else
			{
				num = 1f + m.s.x + m.f.y + m.u.z;
				result.W = num;
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

		// Token: 0x060004C7 RID: 1223 RVA: 0x00011918 File Offset: 0x0000FB18
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

		// Token: 0x060004C8 RID: 1224 RVA: 0x000119BC File Offset: 0x0000FBBC
		public static Quaternion QuaternionFromAxisAngle(Vec3 axis, float angle)
		{
			Quaternion result = default(Quaternion);
			float num;
			float w;
			MathF.SinCos(angle * 0.5f, out num, out w);
			result.X = axis.x * num;
			result.Y = axis.y * num;
			result.Z = axis.z * num;
			result.W = w;
			return result;
		}

		// Token: 0x060004C9 RID: 1225 RVA: 0x00011A18 File Offset: 0x0000FC18
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
				x = MathF.Atan2(2f * (y * z + x * w), -num2 - num3 + num4 + num),
				y = MathF.Asin(-2f * (x * z - y * w))
			};
		}

		// Token: 0x060004CA RID: 1226 RVA: 0x00011AC4 File Offset: 0x0000FCC4
		public static Quaternion FindShortestArcAsQuaternion(Vec3 v0, Vec3 v1)
		{
			Vec3 vec = Vec3.CrossProduct(v0, v1);
			float num = Vec3.DotProduct(v0, v1);
			if ((double)num < -0.9999900000002526)
			{
				Vec3 vec2 = default(Vec3);
				if (MathF.Abs(v0.z) < 0.8f)
				{
					vec2 = Vec3.CrossProduct(v0, new Vec3(0f, 0f, 1f, -1f));
				}
				else
				{
					vec2 = Vec3.CrossProduct(v0, new Vec3(1f, 0f, 0f, -1f));
				}
				vec2.Normalize();
				return new Quaternion(vec2.x, vec2.y, vec2.z, 0f);
			}
			float num2 = MathF.Sqrt((1f + num) * 2f);
			float num3 = 1f / num2;
			return new Quaternion(vec.x * num3, vec.y * num3, vec.z * num3, num2 * 0.5f);
		}

		// Token: 0x060004CB RID: 1227 RVA: 0x00011BB6 File Offset: 0x0000FDB6
		public float Dotp4(Quaternion q2)
		{
			return this.X * q2.X + this.Y * q2.Y + this.Z * q2.Z + this.W * q2.W;
		}

		// Token: 0x060004CC RID: 1228 RVA: 0x00011BEF File Offset: 0x0000FDEF
		public Mat3 ToMat3()
		{
			return Quaternion.Mat3FromQuaternion(this);
		}

		// Token: 0x060004CD RID: 1229 RVA: 0x00011BFC File Offset: 0x0000FDFC
		public bool InverseDirection(Quaternion q2)
		{
			return this.Dotp4(q2) < 0f;
		}

		// Token: 0x060004CE RID: 1230 RVA: 0x00011C0C File Offset: 0x0000FE0C
		public Quaternion Conjugate()
		{
			return new Quaternion(-this.X, -this.Y, -this.Z, this.W);
		}

		// Token: 0x060004CF RID: 1231 RVA: 0x00011C30 File Offset: 0x0000FE30
		public Quaternion Inverse()
		{
			float num = this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W;
			if (num == 0f)
			{
				Debug.FailedAssert("Cannot invert a quaternion with zero norm.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Quaternion.cs", "Inverse", 608);
				return this;
			}
			return this.Conjugate() / num;
		}

		// Token: 0x04000170 RID: 368
		public float W;

		// Token: 0x04000171 RID: 369
		public float X;

		// Token: 0x04000172 RID: 370
		public float Y;

		// Token: 0x04000173 RID: 371
		public float Z;
	}
}
