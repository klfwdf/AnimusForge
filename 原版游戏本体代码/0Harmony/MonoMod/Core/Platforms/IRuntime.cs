using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000502 RID: 1282
	[NullableContext(1)]
	internal interface IRuntime
	{
		// Token: 0x17000635 RID: 1589
		// (get) Token: 0x06001CAC RID: 7340
		RuntimeKind Target { get; }

		// Token: 0x17000636 RID: 1590
		// (get) Token: 0x06001CAD RID: 7341
		RuntimeFeature Features { get; }

		// Token: 0x17000637 RID: 1591
		// (get) Token: 0x06001CAE RID: 7342
		Abi Abi { get; }

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06001CAF RID: 7343
		// (remove) Token: 0x06001CB0 RID: 7344
		[Nullable(2)]
		event OnMethodCompiledCallback OnMethodCompiled;

		// Token: 0x06001CB1 RID: 7345
		MethodBase GetIdentifiable(MethodBase method);

		// Token: 0x06001CB2 RID: 7346
		RuntimeMethodHandle GetMethodHandle(MethodBase method);

		// Token: 0x06001CB3 RID: 7347
		bool RequiresGenericContext(MethodBase method);

		// Token: 0x06001CB4 RID: 7348
		void DisableInlining(MethodBase method);

		// Token: 0x06001CB5 RID: 7349
		[return: Nullable(2)]
		IDisposable PinMethodIfNeeded(MethodBase method);

		// Token: 0x06001CB6 RID: 7350
		IntPtr GetMethodEntryPoint(MethodBase method);

		// Token: 0x06001CB7 RID: 7351
		void Compile(MethodBase method);
	}
}
