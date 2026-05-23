using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x020007A5 RID: 1957
	internal sealed class TypeInformation
	{
		// Token: 0x17000DE0 RID: 3552
		// (get) Token: 0x060054ED RID: 21741 RVA: 0x0012E10A File Offset: 0x0012C30A
		internal string FullTypeName
		{
			get
			{
				return this.fullTypeName;
			}
		}

		// Token: 0x17000DE1 RID: 3553
		// (get) Token: 0x060054EE RID: 21742 RVA: 0x0012E112 File Offset: 0x0012C312
		internal string AssemblyString
		{
			get
			{
				return this.assemblyString;
			}
		}

		// Token: 0x17000DE2 RID: 3554
		// (get) Token: 0x060054EF RID: 21743 RVA: 0x0012E11A File Offset: 0x0012C31A
		internal bool HasTypeForwardedFrom
		{
			get
			{
				return this.hasTypeForwardedFrom;
			}
		}

		// Token: 0x060054F0 RID: 21744 RVA: 0x0012E122 File Offset: 0x0012C322
		internal TypeInformation(string fullTypeName, string assemblyString, bool hasTypeForwardedFrom)
		{
			this.fullTypeName = fullTypeName;
			this.assemblyString = assemblyString;
			this.hasTypeForwardedFrom = hasTypeForwardedFrom;
		}

		// Token: 0x0400270F RID: 9999
		private string fullTypeName;

		// Token: 0x04002710 RID: 10000
		private string assemblyString;

		// Token: 0x04002711 RID: 10001
		private bool hasTypeForwardedFrom;
	}
}
