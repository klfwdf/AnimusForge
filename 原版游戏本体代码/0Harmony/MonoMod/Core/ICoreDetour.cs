using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MonoMod.Core
{
	// Token: 0x020004DA RID: 1242
	[NullableContext(1)]
	[CLSCompliant(true)]
	internal interface ICoreDetour : ICoreDetourBase, IDisposable
	{
		// Token: 0x170005EE RID: 1518
		// (get) Token: 0x06001B98 RID: 7064
		MethodBase Source { get; }

		// Token: 0x170005EF RID: 1519
		// (get) Token: 0x06001B99 RID: 7065
		MethodBase Target { get; }
	}
}
