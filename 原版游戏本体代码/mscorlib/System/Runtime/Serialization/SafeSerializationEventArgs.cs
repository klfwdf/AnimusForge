using System;
using System.Collections.Generic;

namespace System.Runtime.Serialization
{
	// Token: 0x02000753 RID: 1875
	public sealed class SafeSerializationEventArgs : EventArgs
	{
		// Token: 0x060052CD RID: 21197 RVA: 0x00123036 File Offset: 0x00121236
		internal SafeSerializationEventArgs(StreamingContext streamingContext)
		{
			this.m_streamingContext = streamingContext;
		}

		// Token: 0x060052CE RID: 21198 RVA: 0x00123050 File Offset: 0x00121250
		public void AddSerializedState(ISafeSerializationData serializedState)
		{
			if (serializedState == null)
			{
				throw new ArgumentNullException("serializedState");
			}
			if (!serializedState.GetType().IsSerializable)
			{
				throw new ArgumentException(Environment.GetResourceString("Serialization_NonSerType", new object[]
				{
					serializedState.GetType(),
					serializedState.GetType().Assembly.FullName
				}));
			}
			this.m_serializedStates.Add(serializedState);
		}

		// Token: 0x17000DB1 RID: 3505
		// (get) Token: 0x060052CF RID: 21199 RVA: 0x001230B6 File Offset: 0x001212B6
		internal IList<object> SerializedStates
		{
			get
			{
				return this.m_serializedStates;
			}
		}

		// Token: 0x17000DB2 RID: 3506
		// (get) Token: 0x060052D0 RID: 21200 RVA: 0x001230BE File Offset: 0x001212BE
		public StreamingContext StreamingContext
		{
			get
			{
				return this.m_streamingContext;
			}
		}

		// Token: 0x040024B6 RID: 9398
		private StreamingContext m_streamingContext;

		// Token: 0x040024B7 RID: 9399
		private List<object> m_serializedStates = new List<object>();
	}
}
