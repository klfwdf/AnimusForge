using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000744 RID: 1860
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct StreamingContext
	{
		// Token: 0x0600522F RID: 21039 RVA: 0x00120CCE File Offset: 0x0011EECE
		public StreamingContext(StreamingContextStates state)
		{
			this = new StreamingContext(state, null);
		}

		// Token: 0x06005230 RID: 21040 RVA: 0x00120CD8 File Offset: 0x0011EED8
		public StreamingContext(StreamingContextStates state, object additional)
		{
			this.m_state = state;
			this.m_additionalContext = additional;
		}

		// Token: 0x17000D8F RID: 3471
		// (get) Token: 0x06005231 RID: 21041 RVA: 0x00120CE8 File Offset: 0x0011EEE8
		public object Context
		{
			get
			{
				return this.m_additionalContext;
			}
		}

		// Token: 0x06005232 RID: 21042 RVA: 0x00120CF0 File Offset: 0x0011EEF0
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is StreamingContext && (((StreamingContext)obj).m_additionalContext == this.m_additionalContext && ((StreamingContext)obj).m_state == this.m_state);
		}

		// Token: 0x06005233 RID: 21043 RVA: 0x00120D25 File Offset: 0x0011EF25
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return (int)this.m_state;
		}

		// Token: 0x17000D90 RID: 3472
		// (get) Token: 0x06005234 RID: 21044 RVA: 0x00120D2D File Offset: 0x0011EF2D
		public StreamingContextStates State
		{
			get
			{
				return this.m_state;
			}
		}

		// Token: 0x04002462 RID: 9314
		internal object m_additionalContext;

		// Token: 0x04002463 RID: 9315
		internal StreamingContextStates m_state;
	}
}
