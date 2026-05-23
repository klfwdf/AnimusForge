using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000902 RID: 2306
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	internal sealed class IgnoresAccessChecksToAttribute : Attribute
	{
		// Token: 0x17000869 RID: 2153
		// (get) Token: 0x06003085 RID: 12421 RVA: 0x000A751D File Offset: 0x000A571D
		public string AssemblyName { get; }

		// Token: 0x06003086 RID: 12422 RVA: 0x000A7525 File Offset: 0x000A5725
		public IgnoresAccessChecksToAttribute(string assemblyName)
		{
			this.AssemblyName = assemblyName;
		}
	}
}
