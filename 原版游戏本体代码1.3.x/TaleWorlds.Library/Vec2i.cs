using System;

namespace TaleWorlds.Library
{
	// Token: 0x020000A1 RID: 161
	[Serializable]
	public struct Vec2i : IEquatable<Vec2i>
	{
		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060005D2 RID: 1490 RVA: 0x000142E5 File Offset: 0x000124E5
		public int Item1
		{
			get
			{
				return this.X;
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060005D3 RID: 1491 RVA: 0x000142ED File Offset: 0x000124ED
		public int Item2
		{
			get
			{
				return this.Y;
			}
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x000142F5 File Offset: 0x000124F5
		public Vec2i(int x = 0, int y = 0)
		{
			this.X = x;
			this.Y = y;
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x00014305 File Offset: 0x00012505
		public static bool operator ==(Vec2i a, Vec2i b)
		{
			return a.X == b.X && a.Y == b.Y;
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00014325 File Offset: 0x00012525
		public static bool operator !=(Vec2i a, Vec2i b)
		{
			return a.X != b.X || a.Y != b.Y;
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x00014348 File Offset: 0x00012548
		public override bool Equals(object obj)
		{
			return obj != null && !(base.GetType() != obj.GetType()) && ((Vec2i)obj).X == this.X && ((Vec2i)obj).Y == this.Y;
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x0001439F File Offset: 0x0001259F
		public bool Equals(Vec2i value)
		{
			return value.X == this.X && value.Y == this.Y;
		}

		// Token: 0x060005D9 RID: 1497 RVA: 0x000143BF File Offset: 0x000125BF
		public override int GetHashCode()
		{
			return (23 * 31 + this.X.GetHashCode()) * 31 + this.Y.GetHashCode();
		}

		// Token: 0x040001BE RID: 446
		public int X;

		// Token: 0x040001BF RID: 447
		public int Y;

		// Token: 0x040001C0 RID: 448
		public static readonly Vec2i Side = new Vec2i(1, 0);

		// Token: 0x040001C1 RID: 449
		public static readonly Vec2i Forward = new Vec2i(0, 1);

		// Token: 0x040001C2 RID: 450
		public static readonly Vec2i One = new Vec2i(1, 1);

		// Token: 0x040001C3 RID: 451
		public static readonly Vec2i Zero = new Vec2i(0, 0);
	}
}
