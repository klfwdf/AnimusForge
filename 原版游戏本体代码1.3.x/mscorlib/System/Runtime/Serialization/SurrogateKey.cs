using System;

namespace System.Runtime.Serialization
{
	// Token: 0x0200075A RID: 1882
	[Serializable]
	internal class SurrogateKey
	{
		// Token: 0x060052F1 RID: 21233 RVA: 0x00123AFD File Offset: 0x00121CFD
		internal SurrogateKey(Type type, StreamingContext context)
		{
			this.m_type = type;
			this.m_context = context;
		}

		// Token: 0x060052F2 RID: 21234 RVA: 0x00123B13 File Offset: 0x00121D13
		public override int GetHashCode()
		{
			return this.m_type.GetHashCode();
		}

		// Token: 0x040024C8 RID: 9416
		internal Type m_type;

		// Token: 0x040024C9 RID: 9417
		internal StreamingContext m_context;
	}
}
