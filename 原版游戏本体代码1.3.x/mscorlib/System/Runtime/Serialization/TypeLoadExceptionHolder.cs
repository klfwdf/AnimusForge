using System;

namespace System.Runtime.Serialization
{
	// Token: 0x02000752 RID: 1874
	internal class TypeLoadExceptionHolder
	{
		// Token: 0x060052CB RID: 21195 RVA: 0x0012301F File Offset: 0x0012121F
		internal TypeLoadExceptionHolder(string typeName)
		{
			this.m_typeName = typeName;
		}

		// Token: 0x17000DB0 RID: 3504
		// (get) Token: 0x060052CC RID: 21196 RVA: 0x0012302E File Offset: 0x0012122E
		internal string TypeName
		{
			get
			{
				return this.m_typeName;
			}
		}

		// Token: 0x040024B5 RID: 9397
		private string m_typeName;
	}
}
