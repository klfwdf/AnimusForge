using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200005A RID: 90
	public struct LineSegment2D
	{
		// Token: 0x0600029D RID: 669 RVA: 0x00007C9C File Offset: 0x00005E9C
		public LineSegment2D(Vec2 point1, Vec2 point2)
		{
			this.Point1 = point1;
			this.Point2 = point2;
			this.Normal = (point1 - point2).Normalized().RightVec();
		}

		// Token: 0x1700003A RID: 58
		public Vec2 this[int index]
		{
			get
			{
				if (index == 0)
				{
					return this.Point1;
				}
				if (index != 1)
				{
					Debug.FailedAssert("Invalid index", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\LineSegment2D.cs", "Item", 30);
					return Vec2.Invalid;
				}
				return this.Point2;
			}
		}

		// Token: 0x040000F9 RID: 249
		public Vec2 Point1;

		// Token: 0x040000FA RID: 250
		public Vec2 Point2;

		// Token: 0x040000FB RID: 251
		public Vec2 Normal;
	}
}
