using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200008B RID: 139
	[EngineStruct("rglRagdoll::Ragdoll_state", true, "rds", false)]
	public enum RagdollState : ushort
	{
		// Token: 0x040001BD RID: 445
		Disabled,
		// Token: 0x040001BE RID: 446
		NeedsActivation,
		// Token: 0x040001BF RID: 447
		ActiveFirstTick,
		// Token: 0x040001C0 RID: 448
		Active,
		// Token: 0x040001C1 RID: 449
		NeedsDeactivation
	}
}
