using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004FC RID: 1276
	[NullableContext(1)]
	internal interface IMemoryAllocator
	{
		// Token: 0x17000628 RID: 1576
		// (get) Token: 0x06001C7E RID: 7294
		int MaxSize { get; }

		// Token: 0x06001C7F RID: 7295
		bool TryAllocate(AllocationRequest request, [<24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhen(false)] out IAllocatedMemory allocated);

		// Token: 0x06001C80 RID: 7296
		bool TryAllocateInRange(PositionedAllocationRequest request, [<24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhen(false)] out IAllocatedMemory allocated);
	}
}
