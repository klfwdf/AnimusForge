using System;

namespace System.Reflection.Emit
{
	// Token: 0x02000636 RID: 1590
	internal sealed class GenericFieldInfo
	{
		// Token: 0x06004A2F RID: 18991 RVA: 0x0010C763 File Offset: 0x0010A963
		internal GenericFieldInfo(RuntimeFieldHandle fieldHandle, RuntimeTypeHandle context)
		{
			this.m_fieldHandle = fieldHandle;
			this.m_context = context;
		}

		// Token: 0x04001E99 RID: 7833
		internal RuntimeFieldHandle m_fieldHandle;

		// Token: 0x04001E9A RID: 7834
		internal RuntimeTypeHandle m_context;
	}
}
