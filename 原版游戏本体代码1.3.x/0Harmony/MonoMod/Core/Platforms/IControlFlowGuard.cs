using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004FB RID: 1275
	internal interface IControlFlowGuard
	{
		// Token: 0x17000626 RID: 1574
		// (get) Token: 0x06001C7B RID: 7291
		bool IsSupported { get; }

		// Token: 0x17000627 RID: 1575
		// (get) Token: 0x06001C7C RID: 7292
		int TargetAlignmentRequirement { get; }

		// Token: 0x06001C7D RID: 7293
		unsafe void RegisterValidIndirectCallTargets(void* memoryRegionStart, [NativeInteger] IntPtr memoryRegionLength, [NativeInteger] ReadOnlySpan<IntPtr> validTargetsInMemoryRegion);
	}
}
