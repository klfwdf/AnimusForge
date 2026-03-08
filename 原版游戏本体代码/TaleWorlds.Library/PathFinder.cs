using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200007A RID: 122
	public abstract class PathFinder
	{
		// Token: 0x06000455 RID: 1109 RVA: 0x0000F63B File Offset: 0x0000D83B
		public PathFinder()
		{
		}

		// Token: 0x06000456 RID: 1110 RVA: 0x0000F643 File Offset: 0x0000D843
		public virtual void Destroy()
		{
		}

		// Token: 0x06000457 RID: 1111
		public abstract void Initialize(Vec3 bbSize);

		// Token: 0x06000458 RID: 1112
		public abstract bool FindPath(Vec3 wSource, Vec3 wDestination, List<Vec3> path, float craftWidth = 5f);

		// Token: 0x04000157 RID: 343
		public static float BuildingCost = 5000f;

		// Token: 0x04000158 RID: 344
		public static float WaterCost = 400f;

		// Token: 0x04000159 RID: 345
		public static float ShallowWaterCost = 100f;
	}
}
