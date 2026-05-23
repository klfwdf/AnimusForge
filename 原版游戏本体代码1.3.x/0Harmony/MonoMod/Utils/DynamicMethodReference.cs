using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MonoMod.Utils
{
	// Token: 0x0200089F RID: 2207
	[NullableContext(1)]
	[Nullable(0)]
	internal class DynamicMethodReference : MethodReference
	{
		// Token: 0x1700083F RID: 2111
		// (get) Token: 0x06002D4C RID: 11596 RVA: 0x000985B0 File Offset: 0x000967B0
		public MethodInfo DynamicMethod { get; }

		// Token: 0x06002D4D RID: 11597 RVA: 0x000985B8 File Offset: 0x000967B8
		public DynamicMethodReference(ModuleDefinition module, MethodInfo dm)
			: base("", Helpers.ThrowIfNull<ModuleDefinition>(module, "module").TypeSystem.Void)
		{
			this.DynamicMethod = dm;
		}
	}
}
