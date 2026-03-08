using System;

namespace TaleWorlds.Library
{
	// Token: 0x020000A3 RID: 163
	[Serializable]
	public struct Vec3i
	{
		// Token: 0x0600061B RID: 1563 RVA: 0x000155D1 File Offset: 0x000137D1
		public Vec3i(int x = 0, int y = 0, int z = 0)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		// Token: 0x0600061C RID: 1564 RVA: 0x000155E8 File Offset: 0x000137E8
		public static bool operator ==(Vec3i v1, Vec3i v2)
		{
			return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
		}

		// Token: 0x0600061D RID: 1565 RVA: 0x00015616 File Offset: 0x00013816
		public static bool operator !=(Vec3i v1, Vec3i v2)
		{
			return v1.X != v2.X || v1.Y != v2.Y || v1.Z != v2.Z;
		}

		// Token: 0x0600061E RID: 1566 RVA: 0x00015647 File Offset: 0x00013847
		public Vec3 ToVec3()
		{
			return new Vec3((float)this.X, (float)this.Y, (float)this.Z, -1f);
		}

		// Token: 0x170000AF RID: 175
		public int this[int index]
		{
			get
			{
				if (index == 0)
				{
					return this.X;
				}
				if (index != 1)
				{
					return this.Z;
				}
				return this.Y;
			}
			set
			{
				if (index == 0)
				{
					this.X = value;
					return;
				}
				if (index == 1)
				{
					this.Y = value;
					return;
				}
				this.Z = value;
			}
		}

		// Token: 0x06000621 RID: 1569 RVA: 0x000156A5 File Offset: 0x000138A5
		public static Vec3i operator *(Vec3i v, int mult)
		{
			return new Vec3i(v.X * mult, v.Y * mult, v.Z * mult);
		}

		// Token: 0x06000622 RID: 1570 RVA: 0x000156C4 File Offset: 0x000138C4
		public static Vec3i operator +(Vec3i v1, Vec3i v2)
		{
			return new Vec3i(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
		}

		// Token: 0x06000623 RID: 1571 RVA: 0x000156F2 File Offset: 0x000138F2
		public static Vec3i operator -(Vec3i v1, Vec3i v2)
		{
			return new Vec3i(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
		}

		// Token: 0x06000624 RID: 1572 RVA: 0x00015720 File Offset: 0x00013920
		public override bool Equals(object obj)
		{
			return obj != null && !(base.GetType() != obj.GetType()) && (((Vec3i)obj).X == this.X && ((Vec3i)obj).Y == this.Y) && ((Vec3i)obj).Z == this.Z;
		}

		// Token: 0x06000625 RID: 1573 RVA: 0x0001578A File Offset: 0x0001398A
		public override int GetHashCode()
		{
			return (((this.X * 397) ^ this.Y) * 397) ^ this.Z;
		}

		// Token: 0x06000626 RID: 1574 RVA: 0x000157AC File Offset: 0x000139AC
		public override string ToString()
		{
			return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", new object[] { "X", this.X, "Y", this.Y, "Z", this.Z });
		}

		// Token: 0x040001CE RID: 462
		public int X;

		// Token: 0x040001CF RID: 463
		public int Y;

		// Token: 0x040001D0 RID: 464
		public int Z;

		// Token: 0x040001D1 RID: 465
		public static readonly Vec3i Zero = new Vec3i(0, 0, 0);
	}
}
