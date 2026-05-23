using System;

namespace System.Reflection.Emit
{
	// Token: 0x02000635 RID: 1589
	internal sealed class GenericMethodInfo
	{
		// Token: 0x06004A2E RID: 18990 RVA: 0x0010C74D File Offset: 0x0010A94D
		internal GenericMethodInfo(RuntimeMethodHandle methodHandle, RuntimeTypeHandle context)
		{
			this.m_methodHandle = methodHandle;
			this.m_context = context;
		}

		// Token: 0x04001E97 RID: 7831
		internal RuntimeMethodHandle m_methodHandle;

		// Token: 0x04001E98 RID: 7832
		internal RuntimeTypeHandle m_context;
	}
}
