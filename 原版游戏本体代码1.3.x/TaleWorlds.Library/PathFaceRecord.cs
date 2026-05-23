using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000079 RID: 121
	public struct PathFaceRecord
	{
		// Token: 0x06000452 RID: 1106 RVA: 0x0000F607 File Offset: 0x0000D807
		public PathFaceRecord(int index, int groupIndex, int islandIndex)
		{
			this.FaceIndex = index;
			this.FaceGroupIndex = groupIndex;
			this.FaceIslandIndex = islandIndex;
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x0000F61E File Offset: 0x0000D81E
		public bool IsValid()
		{
			return this.FaceIndex != -1;
		}

		// Token: 0x04000153 RID: 339
		public int FaceIndex;

		// Token: 0x04000154 RID: 340
		public int FaceGroupIndex;

		// Token: 0x04000155 RID: 341
		public int FaceIslandIndex;

		// Token: 0x04000156 RID: 342
		public static readonly PathFaceRecord NullFaceRecord = new PathFaceRecord(-1, -1, -1);
	}
}
