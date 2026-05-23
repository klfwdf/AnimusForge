using System;
using System.Reflection;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000503 RID: 1283
	// (Invoke) Token: 0x06001CB9 RID: 7353
	internal delegate void OnMethodCompiledCallback(RuntimeMethodHandle methodHandle, MethodBase method, IntPtr codeStart, IntPtr codeRw, ulong codeSize);
}
