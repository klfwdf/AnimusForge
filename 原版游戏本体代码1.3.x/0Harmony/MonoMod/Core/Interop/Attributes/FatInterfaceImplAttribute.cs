using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Interop.Attributes
{
	// Token: 0x02000606 RID: 1542
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
	internal sealed class FatInterfaceImplAttribute : Attribute
	{
		// Token: 0x17000755 RID: 1877
		// (get) Token: 0x060020D3 RID: 8403 RVA: 0x000683C1 File Offset: 0x000665C1
		public Type FatInterface { get; }

		// Token: 0x060020D4 RID: 8404 RVA: 0x000683C9 File Offset: 0x000665C9
		public FatInterfaceImplAttribute(Type fatInterface)
		{
			this.FatInterface = fatInterface;
		}
	}
}
