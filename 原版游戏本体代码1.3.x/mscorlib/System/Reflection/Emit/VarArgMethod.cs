using System;

namespace System.Reflection.Emit
{
	// Token: 0x02000637 RID: 1591
	internal sealed class VarArgMethod
	{
		// Token: 0x06004A30 RID: 18992 RVA: 0x0010C779 File Offset: 0x0010A979
		internal VarArgMethod(DynamicMethod dm, SignatureHelper signature)
		{
			this.m_dynamicMethod = dm;
			this.m_signature = signature;
		}

		// Token: 0x06004A31 RID: 18993 RVA: 0x0010C78F File Offset: 0x0010A98F
		internal VarArgMethod(RuntimeMethodInfo method, SignatureHelper signature)
		{
			this.m_method = method;
			this.m_signature = signature;
		}

		// Token: 0x04001E9B RID: 7835
		internal RuntimeMethodInfo m_method;

		// Token: 0x04001E9C RID: 7836
		internal DynamicMethod m_dynamicMethod;

		// Token: 0x04001E9D RID: 7837
		internal SignatureHelper m_signature;
	}
}
