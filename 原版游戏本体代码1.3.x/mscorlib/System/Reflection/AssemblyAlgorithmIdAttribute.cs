using System;
using System.Configuration.Assemblies;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C2 RID: 1474
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	public sealed class AssemblyAlgorithmIdAttribute : Attribute
	{
		// Token: 0x0600446E RID: 17518 RVA: 0x000FC564 File Offset: 0x000FA764
		public AssemblyAlgorithmIdAttribute(AssemblyHashAlgorithm algorithmId)
		{
			this.m_algId = (uint)algorithmId;
		}

		// Token: 0x0600446F RID: 17519 RVA: 0x000FC573 File Offset: 0x000FA773
		[CLSCompliant(false)]
		public AssemblyAlgorithmIdAttribute(uint algorithmId)
		{
			this.m_algId = algorithmId;
		}

		// Token: 0x17000A1D RID: 2589
		// (get) Token: 0x06004470 RID: 17520 RVA: 0x000FC582 File Offset: 0x000FA782
		[CLSCompliant(false)]
		public uint AlgorithmId
		{
			get
			{
				return this.m_algId;
			}
		}

		// Token: 0x04001C0C RID: 7180
		private uint m_algId;
	}
}
