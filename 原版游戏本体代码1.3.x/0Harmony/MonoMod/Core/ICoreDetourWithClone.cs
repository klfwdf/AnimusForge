using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core
{
	// Token: 0x020004DB RID: 1243
	[NullableContext(2)]
	internal interface ICoreDetourWithClone : ICoreDetour, ICoreDetourBase, IDisposable
	{
		// Token: 0x170005F0 RID: 1520
		// (get) Token: 0x06001B9A RID: 7066
		MethodInfo SourceMethodClone { get; }

		// Token: 0x170005F1 RID: 1521
		// (get) Token: 0x06001B9B RID: 7067
		DynamicMethodDefinition SourceMethodCloneIL { get; }
	}
}
