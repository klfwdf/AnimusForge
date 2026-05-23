using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000504 RID: 1284
	[NullableContext(1)]
	internal interface ISystem
	{
		// Token: 0x17000638 RID: 1592
		// (get) Token: 0x06001CBC RID: 7356
		OSKind Target { get; }

		// Token: 0x17000639 RID: 1593
		// (get) Token: 0x06001CBD RID: 7357
		SystemFeature Features { get; }

		// Token: 0x1700063A RID: 1594
		// (get) Token: 0x06001CBE RID: 7358
		Abi? DefaultAbi { get; }

		// Token: 0x1700063B RID: 1595
		// (get) Token: 0x06001CBF RID: 7359
		IMemoryAllocator MemoryAllocator { get; }

		// Token: 0x1700063C RID: 1596
		// (get) Token: 0x06001CC0 RID: 7360
		[Nullable(2)]
		INativeExceptionHelper NativeExceptionHelper
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x06001CC1 RID: 7361
		[return: Nullable(new byte[] { 1, 2 })]
		IEnumerable<string> EnumerateLoadedModuleFiles();

		// Token: 0x06001CC2 RID: 7362
		[return: NativeInteger]
		IntPtr GetSizeOfReadableMemory(IntPtr start, [NativeInteger] IntPtr guess);

		// Token: 0x06001CC3 RID: 7363
		[NullableContext(0)]
		void PatchData(PatchTargetKind targetKind, IntPtr patchTarget, ReadOnlySpan<byte> data, Span<byte> backup);

		// Token: 0x06001CC4 RID: 7364
		IntPtr GetNativeJitHookConfig(int runtimeMajMin);
	}
}
