using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms.Memory
{
	// Token: 0x02000554 RID: 1364
	internal abstract class QueryingMemoryPageAllocatorBase
	{
		// Token: 0x170006A9 RID: 1705
		// (get) Token: 0x06001E95 RID: 7829
		public abstract uint PageSize { get; }

		// Token: 0x06001E96 RID: 7830
		public abstract bool TryQueryPage(IntPtr pageAddr, out bool isFree, out IntPtr allocBase, [NativeInteger] out IntPtr allocSize);

		// Token: 0x06001E97 RID: 7831
		public abstract bool TryAllocatePage([NativeInteger] IntPtr size, bool executable, out IntPtr allocated);

		// Token: 0x06001E98 RID: 7832
		public abstract bool TryAllocatePage(IntPtr pageAddr, [NativeInteger] IntPtr size, bool executable, out IntPtr allocated);

		// Token: 0x06001E99 RID: 7833
		[NullableContext(2)]
		public abstract bool TryFreePage(IntPtr pageAddr, [<24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullWhen(false)] out string errorMsg);
	}
}
