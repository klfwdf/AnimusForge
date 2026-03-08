using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000075 RID: 117
	public struct Oriented2DArea
	{
		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000424 RID: 1060 RVA: 0x0000E9D4 File Offset: 0x0000CBD4
		// (set) Token: 0x06000425 RID: 1061 RVA: 0x0000E9DC File Offset: 0x0000CBDC
		public Vec2 GlobalCenter { get; private set; }

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000426 RID: 1062 RVA: 0x0000E9E5 File Offset: 0x0000CBE5
		// (set) Token: 0x06000427 RID: 1063 RVA: 0x0000E9ED File Offset: 0x0000CBED
		public Vec2 GlobalForward { get; private set; }

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000428 RID: 1064 RVA: 0x0000E9F6 File Offset: 0x0000CBF6
		// (set) Token: 0x06000429 RID: 1065 RVA: 0x0000E9FE File Offset: 0x0000CBFE
		public Vec2 LocalDimensions { get; private set; }

		// Token: 0x0600042A RID: 1066 RVA: 0x0000EA07 File Offset: 0x0000CC07
		public Oriented2DArea(in Vec2 globalCenter, in Vec2 globalForward, in Vec2 localDimensions)
		{
			this.GlobalCenter = globalCenter;
			this.GlobalForward = globalForward;
			this.LocalDimensions = localDimensions;
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x0000EA2D File Offset: 0x0000CC2D
		public void SetGlobalCenter(in Vec2 globalCenter)
		{
			this.GlobalCenter = globalCenter;
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x0000EA3B File Offset: 0x0000CC3B
		public void SetLocalDimensions(in Vec2 localDimensions)
		{
			this.LocalDimensions = localDimensions;
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x0000EA4C File Offset: 0x0000CC4C
		public bool Overlaps(in Oriented2DArea otherArea, float clearanceMargin)
		{
			Oriented2DArea.Corners corners = this.GetCorners();
			Oriented2DArea oriented2DArea = otherArea;
			Oriented2DArea.Corners corners2 = oriented2DArea.GetCorners();
			if (!this.IsProjectionOverlap(corners, corners2, this.GlobalForward, clearanceMargin))
			{
				return false;
			}
			Vec2 axis = this.GlobalForward.RightVec();
			if (!this.IsProjectionOverlap(corners, corners2, axis, clearanceMargin))
			{
				return false;
			}
			oriented2DArea = otherArea;
			if (!this.IsProjectionOverlap(corners, corners2, oriented2DArea.GlobalForward, clearanceMargin))
			{
				return false;
			}
			oriented2DArea = otherArea;
			Vec2 axis2 = oriented2DArea.GlobalForward.RightVec();
			return this.IsProjectionOverlap(corners, corners2, axis2, clearanceMargin);
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x0000EAF0 File Offset: 0x0000CCF0
		public bool Intersects(in LineSegment2D line, float clearanceMargin)
		{
			Oriented2DArea.Corners corners = this.GetCorners();
			Vec2 axis = this.GlobalForward.RightVec();
			return this.DoesProjectionIntersect(corners, line, this.GlobalForward, clearanceMargin) && this.DoesProjectionIntersect(corners, line, axis, clearanceMargin) && this.DoesProjectionIntersect(corners, line, line.Normal, clearanceMargin);
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x0000EB44 File Offset: 0x0000CD44
		public Oriented2DArea.Corners GetCorners()
		{
			Vec2 v = this.GlobalForward.RightVec() * (this.LocalDimensions.x * 0.5f);
			Vec2 v2 = this.GlobalForward * (this.LocalDimensions.y * 0.5f);
			Vec2 vec = this.GlobalCenter + v + v2;
			Vec2 vec2 = this.GlobalCenter - v + v2;
			Vec2 vec3 = this.GlobalCenter - v - v2;
			Vec2 vec4 = this.GlobalCenter + v - v2;
			return new Oriented2DArea.Corners(ref vec2, ref vec, ref vec3, ref vec4);
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x0000EBF0 File Offset: 0x0000CDF0
		private bool IsProjectionOverlap(in Oriented2DArea.Corners cornersA, in Oriented2DArea.Corners cornersB, Vec2 axis, float clearanceMargin)
		{
			float num = float.MaxValue;
			float num2 = float.MinValue;
			float num3 = float.MaxValue;
			float num4 = float.MinValue;
			for (int i = 0; i < 4; i++)
			{
				Oriented2DArea.Corners corners = cornersA;
				float val = Vec2.DotProduct(corners[i], axis);
				num = Math.Min(num, val);
				num2 = Math.Max(num2, val);
			}
			for (int j = 0; j < 4; j++)
			{
				Oriented2DArea.Corners corners = cornersB;
				float val2 = Vec2.DotProduct(corners[j], axis);
				num3 = Math.Min(num3, val2);
				num4 = Math.Max(num4, val2);
			}
			return num2 + clearanceMargin >= num3 && num4 + clearanceMargin >= num;
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x0000ECA0 File Offset: 0x0000CEA0
		private bool DoesProjectionIntersect(in Oriented2DArea.Corners cornersOfArea, in LineSegment2D line, Vec2 axis, float clearanceMargin)
		{
			float num = float.MaxValue;
			float num2 = float.MinValue;
			float num3 = float.MaxValue;
			float num4 = float.MinValue;
			for (int i = 0; i < 4; i++)
			{
				Oriented2DArea.Corners corners = cornersOfArea;
				float b = Vec2.DotProduct(corners[i], axis);
				num = MathF.Min(num, b);
				num2 = MathF.Max(num2, b);
			}
			for (int j = 0; j < 2; j++)
			{
				LineSegment2D lineSegment2D = line;
				float b2 = Vec2.DotProduct(lineSegment2D[j], axis);
				num3 = MathF.Min(num3, b2);
				num4 = MathF.Max(num4, b2);
			}
			return num2 + clearanceMargin >= num3 && num4 + clearanceMargin >= num;
		}

		// Token: 0x020000DD RID: 221
		public struct Corners
		{
			// Token: 0x170000FE RID: 254
			// (get) Token: 0x06000782 RID: 1922 RVA: 0x00018DF1 File Offset: 0x00016FF1
			// (set) Token: 0x06000783 RID: 1923 RVA: 0x00018DF9 File Offset: 0x00016FF9
			public Vec2 TopLeft { get; private set; }

			// Token: 0x170000FF RID: 255
			// (get) Token: 0x06000784 RID: 1924 RVA: 0x00018E02 File Offset: 0x00017002
			// (set) Token: 0x06000785 RID: 1925 RVA: 0x00018E0A File Offset: 0x0001700A
			public Vec2 TopRight { get; private set; }

			// Token: 0x17000100 RID: 256
			// (get) Token: 0x06000786 RID: 1926 RVA: 0x00018E13 File Offset: 0x00017013
			// (set) Token: 0x06000787 RID: 1927 RVA: 0x00018E1B File Offset: 0x0001701B
			public Vec2 BottomLeft { get; private set; }

			// Token: 0x17000101 RID: 257
			// (get) Token: 0x06000788 RID: 1928 RVA: 0x00018E24 File Offset: 0x00017024
			// (set) Token: 0x06000789 RID: 1929 RVA: 0x00018E2C File Offset: 0x0001702C
			public Vec2 BottomRight { get; private set; }

			// Token: 0x0600078A RID: 1930 RVA: 0x00018E35 File Offset: 0x00017035
			public Corners(in Vec2 topLeft, in Vec2 topRight, in Vec2 bottomLeft, in Vec2 bottomRight)
			{
				this.TopLeft = topLeft;
				this.TopRight = topRight;
				this.BottomLeft = bottomLeft;
				this.BottomRight = bottomRight;
			}

			// Token: 0x17000102 RID: 258
			public Vec2 this[int index]
			{
				get
				{
					switch (index)
					{
					case 0:
						return this.TopLeft;
					case 1:
						return this.TopRight;
					case 2:
						return this.BottomLeft;
					case 3:
						return this.BottomRight;
					default:
						Debug.FailedAssert("Invalid index", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Oriented2DArea.cs", "Item", 39);
						return Vec2.Invalid;
					}
				}
			}

			// Token: 0x040002D2 RID: 722
			public const int Count = 4;
		}
	}
}
