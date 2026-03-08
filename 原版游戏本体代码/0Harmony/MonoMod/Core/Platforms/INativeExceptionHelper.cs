using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000500 RID: 1280
	[NullableContext(1)]
	internal interface INativeExceptionHelper
	{
		// Token: 0x17000634 RID: 1588
		// (get) Token: 0x06001CA5 RID: 7333
		GetExceptionSlot GetExceptionSlot { get; }

		// Token: 0x06001CA6 RID: 7334
		[NullableContext(2)]
		IntPtr CreateNativeToManagedHelper(IntPtr target, out IDisposable handle);

		// Token: 0x06001CA7 RID: 7335
		[NullableContext(2)]
		IntPtr CreateManagedToNativeHelper(IntPtr target, out IDisposable handle);
	}
}
