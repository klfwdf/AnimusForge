using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004F7 RID: 1271
	[NullableContext(2)]
	internal interface IAltEntryFactory
	{
		// Token: 0x06001C5D RID: 7261
		IntPtr CreateAlternateEntrypoint(IntPtr entrypoint, int minLength, out IDisposable handle);
	}
}
