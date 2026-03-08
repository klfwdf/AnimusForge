using System;
using System.Numerics;

namespace TaleWorlds.Library
{
	// Token: 0x020000A0 RID: 160
	[Serializable]
	public struct Vec2
	{
		// Token: 0x17000099 RID: 153
		// (get) Token: 0x06000597 RID: 1431 RVA: 0x0001392E File Offset: 0x00011B2E
		public float X
		{
			get
			{
				return this.x;
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x06000598 RID: 1432 RVA: 0x00013936 File Offset: 0x00011B36
		public float Y
		{
			get
			{
				return this.y;
			}
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x0001393E File Offset: 0x00011B3E
		public Vec2(float a, float b)
		{
			this.x = a;
			this.y = b;
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x0001394E File Offset: 0x00011B4E
		public Vec2(Vec2 v)
		{
			this.x = v.x;
			this.y = v.y;
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x00013968 File Offset: 0x00011B68
		public Vec2(Vector2 v)
		{
			this.x = v.X;
			this.y = v.Y;
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x00013982 File Offset: 0x00011B82
		public Vec3 ToVec3(float z = 0f)
		{
			return new Vec3(this.x, this.y, z, -1f);
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x0001399B File Offset: 0x00011B9B
		public static explicit operator Vector2(Vec2 vec2)
		{
			return new Vector2(vec2.x, vec2.y);
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x000139AE File Offset: 0x00011BAE
		public static implicit operator Vec2(Vector2 vec2)
		{
			return new Vec2(vec2.X, vec2.Y);
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x000139C4 File Offset: 0x00011BC4
		public float Normalize()
		{
			float length = this.Length;
			if (length > 1E-05f)
			{
				this.x /= length;
				this.y /= length;
			}
			else
			{
				this.x = 0f;
				this.y = 1f;
			}
			return length;
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x00013A18 File Offset: 0x00011C18
		public Vec2 Normalized()
		{
			Vec2 result = this;
			result.Normalize();
			return result;
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x00013A38 File Offset: 0x00011C38
		public void ClampMagnitude(float min, float max)
		{
			float value = this.Normalize();
			this *= MathF.Clamp(value, min, max);
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x00013A68 File Offset: 0x00011C68
		public static WindingOrder GetWindingOrder(Vec2 first, Vec2 second, Vec2 third)
		{
			Vec2 vb = second - first;
			float num = Vec2.CCW(third - second, vb);
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

		// Token: 0x060005A3 RID: 1443 RVA: 0x00013AA0 File Offset: 0x00011CA0
		public static float CCW(Vec2 va, Vec2 vb)
		{
			return va.x * vb.y - va.y * vb.x;
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060005A4 RID: 1444 RVA: 0x00013ABD File Offset: 0x00011CBD
		public float Length
		{
			get
			{
				return MathF.Sqrt(this.x * this.x + this.y * this.y);
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060005A5 RID: 1445 RVA: 0x00013ADF File Offset: 0x00011CDF
		public float LengthSquared
		{
			get
			{
				return this.x * this.x + this.y * this.y;
			}
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x00013AFC File Offset: 0x00011CFC
		public override bool Equals(object obj)
		{
			return obj != null && !(base.GetType() != obj.GetType()) && ((Vec2)obj).x == this.x && ((Vec2)obj).y == this.y;
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x00013B53 File Offset: 0x00011D53
		public override int GetHashCode()
		{
			return (int)(1001f * this.x + 10039f * this.y);
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x00013B6F File Offset: 0x00011D6F
		public static bool operator ==(Vec2 v1, Vec2 v2)
		{
			return v1.x == v2.x && v1.y == v2.y;
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x00013B8F File Offset: 0x00011D8F
		public static bool operator !=(Vec2 v1, Vec2 v2)
		{
			return v1.x != v2.x || v1.y != v2.y;
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x00013BB2 File Offset: 0x00011DB2
		public static Vec2 operator -(Vec2 v)
		{
			return new Vec2(-v.x, -v.y);
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x00013BC7 File Offset: 0x00011DC7
		public static Vec2 operator +(Vec2 v1, Vec2 v2)
		{
			return new Vec2(v1.x + v2.x, v1.y + v2.y);
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x00013BE8 File Offset: 0x00011DE8
		public static Vec2 operator -(Vec2 v1, Vec2 v2)
		{
			return new Vec2(v1.x - v2.x, v1.y - v2.y);
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x00013C09 File Offset: 0x00011E09
		public static Vec2 operator *(Vec2 v, float f)
		{
			return new Vec2(v.x * f, v.y * f);
		}

		// Token: 0x060005AE RID: 1454 RVA: 0x00013C20 File Offset: 0x00011E20
		public static Vec2 operator *(float f, Vec2 v)
		{
			return new Vec2(v.x * f, v.y * f);
		}

		// Token: 0x060005AF RID: 1455 RVA: 0x00013C37 File Offset: 0x00011E37
		public static Vec2 operator /(float f, Vec2 v)
		{
			return new Vec2(f / v.x, f / v.y);
		}

		// Token: 0x060005B0 RID: 1456 RVA: 0x00013C4E File Offset: 0x00011E4E
		public static Vec2 operator /(Vec2 v, float f)
		{
			return new Vec2(v.x / f, v.y / f);
		}

		// Token: 0x060005B1 RID: 1457 RVA: 0x00013C68 File Offset: 0x00011E68
		public bool IsUnit()
		{
			float lengthSquared = this.LengthSquared;
			return lengthSquared > 0.98010004f && lengthSquared < 1.0201f;
		}

		// Token: 0x060005B2 RID: 1458 RVA: 0x00013C90 File Offset: 0x00011E90
		public bool IsNonZero()
		{
			float num = 1E-05f;
			return this.x > num || this.x < -num || this.y > num || this.y < -num;
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x00013CCB File Offset: 0x00011ECB
		public bool NearlyEquals(Vec2 v, float epsilon = 1E-05f)
		{
			return MathF.Abs(this.x - v.x) < epsilon && MathF.Abs(this.y - v.y) < epsilon;
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x00013CFC File Offset: 0x00011EFC
		public void RotateCCW(float angleInRadians)
		{
			float num;
			float num2;
			MathF.SinCos(angleInRadians, out num, out num2);
			float num3 = this.x * num2 - this.y * num;
			this.y = this.y * num2 + this.x * num;
			this.x = num3;
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x00013D43 File Offset: 0x00011F43
		public float DotProduct(Vec2 v)
		{
			return v.x * this.x + v.y * this.y;
		}

		// Token: 0x060005B6 RID: 1462 RVA: 0x00013D60 File Offset: 0x00011F60
		public static float DotProduct(Vec2 va, Vec2 vb)
		{
			return va.x * vb.x + va.y * vb.y;
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x00013D7D File Offset: 0x00011F7D
		public static Vec2 ElementWiseProduct(Vec2 va, Vec2 vb)
		{
			return new Vec2(va.x * vb.x, va.y * vb.y);
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060005B8 RID: 1464 RVA: 0x00013D9E File Offset: 0x00011F9E
		public float RotationInRadians
		{
			get
			{
				return MathF.Atan2(-this.x, this.y);
			}
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00013DB2 File Offset: 0x00011FB2
		public static Vec2 FromRotation(float rotation)
		{
			return new Vec2(-MathF.Sin(rotation), MathF.Cos(rotation));
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x00013DC6 File Offset: 0x00011FC6
		public Vec2 TransformToLocalUnitF(Vec2 a)
		{
			return new Vec2(this.y * a.x - this.x * a.y, this.x * a.x + this.y * a.y);
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x00013E03 File Offset: 0x00012003
		public Vec2 TransformToParentUnitF(Vec2 a)
		{
			return new Vec2(this.y * a.x + this.x * a.y, -this.x * a.x + this.y * a.y);
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x00013E41 File Offset: 0x00012041
		public Vec2 TransformToLocalUnitFLeftHanded(Vec2 a)
		{
			return new Vec2(-this.y * a.x + this.x * a.y, this.x * a.x + this.y * a.y);
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x00013E7F File Offset: 0x0001207F
		public Vec2 TransformToParentUnitFLeftHanded(Vec2 a)
		{
			return new Vec2(-this.y * a.x + this.x * a.y, this.x * a.x + this.y * a.y);
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x00013EBD File Offset: 0x000120BD
		public Vec2 RightVec()
		{
			return new Vec2(this.y, -this.x);
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00013ED1 File Offset: 0x000120D1
		public Vec2 LeftVec()
		{
			return new Vec2(-this.y, this.x);
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00013EE5 File Offset: 0x000120E5
		public static Vec2 Max(Vec2 v1, Vec2 v2)
		{
			return new Vec2(MathF.Max(v1.x, v2.x), MathF.Max(v1.y, v2.y));
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x00013F0E File Offset: 0x0001210E
		public static Vec2 Max(Vec2 v1, float f)
		{
			return new Vec2(MathF.Max(v1.x, f), MathF.Max(v1.y, f));
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x00013F2D File Offset: 0x0001212D
		public static Vec2 Min(Vec2 v1, Vec2 v2)
		{
			return new Vec2(MathF.Min(v1.x, v2.x), MathF.Min(v1.y, v2.y));
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x00013F56 File Offset: 0x00012156
		public static Vec2 Min(Vec2 v1, float f)
		{
			return new Vec2(MathF.Min(v1.x, f), MathF.Min(v1.y, f));
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x00013F75 File Offset: 0x00012175
		public override string ToString()
		{
			return string.Concat(new object[] { "(Vec2) X: ", this.x, " Y: ", this.y });
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00013FAE File Offset: 0x000121AE
		public float DistanceSquared(Vec2 v)
		{
			return (v.x - this.x) * (v.x - this.x) + (v.y - this.y) * (v.y - this.y);
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00013FE7 File Offset: 0x000121E7
		public float Distance(Vec2 v)
		{
			return MathF.Sqrt((v.x - this.x) * (v.x - this.x) + (v.y - this.y) * (v.y - this.y));
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x00014028 File Offset: 0x00012228
		public static float DistanceToLine(Vec2 line1, Vec2 line2, Vec2 point)
		{
			float num = line2.x - line1.x;
			float num2 = line2.y - line1.y;
			return MathF.Abs(num * (line1.y - point.y) - (line1.x - point.x) * num2) / MathF.Sqrt(num * num + num2 * num2);
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x00014082 File Offset: 0x00012282
		public static float DistanceToLineSegmentSquared(Vec2 line1, Vec2 line2, Vec2 point)
		{
			return point.DistanceSquared(MBMath.GetClosestPointOnLineSegmentToPoint(line1, line2, point));
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00014096 File Offset: 0x00012296
		public float DistanceToLineSegment(Vec2 v, Vec2 w, out Vec2 closestPointOnLineSegment)
		{
			return MathF.Sqrt(this.DistanceSquaredToLineSegment(v, w, out closestPointOnLineSegment));
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x000140A8 File Offset: 0x000122A8
		public float DistanceSquaredToLineSegment(Vec2 v, Vec2 w, out Vec2 closestPointOnLineSegment)
		{
			Vec2 v2 = this;
			float num = v.DistanceSquared(w);
			if (num == 0f)
			{
				closestPointOnLineSegment = v;
			}
			else
			{
				float num2 = Vec2.DotProduct(v2 - v, w - v) / num;
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
					Vec2 vec = v + (w - v) * num2;
					closestPointOnLineSegment = vec;
				}
			}
			return v2.DistanceSquared(closestPointOnLineSegment);
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x00014136 File Offset: 0x00012336
		public static Vec2 Abs(Vec2 vec)
		{
			return new Vec2(MathF.Abs(vec.x), MathF.Abs(vec.y));
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x00014153 File Offset: 0x00012353
		public static Vec2 Lerp(Vec2 v1, Vec2 v2, float alpha)
		{
			return v1 * (1f - alpha) + v2 * alpha;
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x00014170 File Offset: 0x00012370
		public static Vec2 Slerp(Vec2 start, Vec2 end, float percent)
		{
			float num = Vec2.DotProduct(start, end);
			num = MBMath.ClampFloat(num, -1f, 1f);
			float num2 = MathF.Acos(num) * percent;
			Vec2 v = end - start * num;
			v.Normalize();
			return start * MathF.Cos(num2) + v * MathF.Sin(num2);
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x000141D4 File Offset: 0x000123D4
		public float AngleBetween(Vec2 vector2)
		{
			float num = this.x * vector2.y - vector2.x * this.y;
			float num2 = this.x * vector2.x + this.y * vector2.y;
			return MathF.Atan2(num, num2);
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060005CF RID: 1487 RVA: 0x0001421E File Offset: 0x0001241E
		public bool IsValid
		{
			get
			{
				return !float.IsNaN(this.x) && !float.IsNaN(this.y) && !float.IsInfinity(this.x) && !float.IsInfinity(this.y);
			}
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00014257 File Offset: 0x00012457
		public static float Determinant(in Vec2 vec1, in Vec2 vec2)
		{
			return vec1.x * vec2.y - vec1.y * vec2.x;
		}

		// Token: 0x040001B7 RID: 439
		public float x;

		// Token: 0x040001B8 RID: 440
		public float y;

		// Token: 0x040001B9 RID: 441
		public static readonly Vec2 Side = new Vec2(1f, 0f);

		// Token: 0x040001BA RID: 442
		public static readonly Vec2 Forward = new Vec2(0f, 1f);

		// Token: 0x040001BB RID: 443
		public static readonly Vec2 One = new Vec2(1f, 1f);

		// Token: 0x040001BC RID: 444
		public static readonly Vec2 Zero = new Vec2(0f, 0f);

		// Token: 0x040001BD RID: 445
		public static readonly Vec2 Invalid = new Vec2(float.NaN, float.NaN);
	}
}
