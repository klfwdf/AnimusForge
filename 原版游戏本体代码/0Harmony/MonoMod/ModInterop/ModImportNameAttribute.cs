using System;
using System.Runtime.CompilerServices;

namespace MonoMod.ModInterop
{
	// Token: 0x02000813 RID: 2067
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
	internal sealed class ModImportNameAttribute : Attribute
	{
		// Token: 0x17000806 RID: 2054
		// (get) Token: 0x0600278A RID: 10122 RVA: 0x00087E49 File Offset: 0x00086049
		public string Name { get; }

		// Token: 0x0600278B RID: 10123 RVA: 0x00087E51 File Offset: 0x00086051
		public ModImportNameAttribute(string name)
		{
			this.Name = name;
		}
	}
}
