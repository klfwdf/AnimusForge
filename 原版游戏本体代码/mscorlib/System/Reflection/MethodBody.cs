using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000613 RID: 1555
	[ComVisible(true)]
	public class MethodBody
	{
		// Token: 0x06004805 RID: 18437 RVA: 0x001060F4 File Offset: 0x001042F4
		protected MethodBody()
		{
		}

		// Token: 0x17000B1D RID: 2845
		// (get) Token: 0x06004806 RID: 18438 RVA: 0x001060FC File Offset: 0x001042FC
		public virtual int LocalSignatureMetadataToken
		{
			get
			{
				return this.m_localSignatureMetadataToken;
			}
		}

		// Token: 0x17000B1E RID: 2846
		// (get) Token: 0x06004807 RID: 18439 RVA: 0x00106104 File Offset: 0x00104304
		public virtual IList<LocalVariableInfo> LocalVariables
		{
			get
			{
				return Array.AsReadOnly<LocalVariableInfo>(this.m_localVariables);
			}
		}

		// Token: 0x17000B1F RID: 2847
		// (get) Token: 0x06004808 RID: 18440 RVA: 0x00106111 File Offset: 0x00104311
		public virtual int MaxStackSize
		{
			get
			{
				return this.m_maxStackSize;
			}
		}

		// Token: 0x17000B20 RID: 2848
		// (get) Token: 0x06004809 RID: 18441 RVA: 0x00106119 File Offset: 0x00104319
		public virtual bool InitLocals
		{
			get
			{
				return this.m_initLocals;
			}
		}

		// Token: 0x0600480A RID: 18442 RVA: 0x00106121 File Offset: 0x00104321
		public virtual byte[] GetILAsByteArray()
		{
			return this.m_IL;
		}

		// Token: 0x17000B21 RID: 2849
		// (get) Token: 0x0600480B RID: 18443 RVA: 0x00106129 File Offset: 0x00104329
		public virtual IList<ExceptionHandlingClause> ExceptionHandlingClauses
		{
			get
			{
				return Array.AsReadOnly<ExceptionHandlingClause>(this.m_exceptionHandlingClauses);
			}
		}

		// Token: 0x04001DD7 RID: 7639
		private byte[] m_IL;

		// Token: 0x04001DD8 RID: 7640
		private ExceptionHandlingClause[] m_exceptionHandlingClauses;

		// Token: 0x04001DD9 RID: 7641
		private LocalVariableInfo[] m_localVariables;

		// Token: 0x04001DDA RID: 7642
		internal MethodBase m_methodBase;

		// Token: 0x04001DDB RID: 7643
		private int m_localSignatureMetadataToken;

		// Token: 0x04001DDC RID: 7644
		private int m_maxStackSize;

		// Token: 0x04001DDD RID: 7645
		private bool m_initLocals;
	}
}
