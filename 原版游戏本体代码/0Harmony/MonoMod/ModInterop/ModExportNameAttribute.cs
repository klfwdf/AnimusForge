using System;
using System.Runtime.CompilerServices;

namespace MonoMod.ModInterop
{
	// Token: 0x02000812 RID: 2066
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Class)]
	internal sealed class ModExportNameAttribute : Attribute
	{
		// Token: 0x17000805 RID: 2053
		// (get) Token: 0x06002788 RID: 10120 RVA: 0x00087E32 File Offset: 0x00086032
		public string Name { get; }

		// Token: 0x06002789 RID: 10121 RVA: 0x00087E3A File Offset: 0x0008603A
		public ModExportNameAttribute(string name)
		{
			this.Name = name;
		}
	}
}
