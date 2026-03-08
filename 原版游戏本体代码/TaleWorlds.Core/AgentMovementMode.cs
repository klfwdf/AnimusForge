using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000009 RID: 9
	[Flags]
	public enum AgentMovementMode : byte
	{
		// Token: 0x040000CC RID: 204
		None = 0,
		// Token: 0x040000CD RID: 205
		Land = 1,
		// Token: 0x040000CE RID: 206
		WaterSurface = 2,
		// Token: 0x040000CF RID: 207
		WaterDiving = 3,
		// Token: 0x040000D0 RID: 208
		PhysicsCheck = 4,
		// Token: 0x040000D1 RID: 209
		NoPhysics = 8,
		// Token: 0x040000D2 RID: 210
		MovementModeMask = 3
	}
}
