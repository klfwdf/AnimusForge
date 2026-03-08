using System;
using System.Collections;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000756 RID: 1878
	public sealed class SerializationObjectManager
	{
		// Token: 0x060052DC RID: 21212 RVA: 0x00123392 File Offset: 0x00121592
		public SerializationObjectManager(StreamingContext context)
		{
			this.m_context = context;
			this.m_objectSeenTable = new Hashtable();
		}

		// Token: 0x060052DD RID: 21213 RVA: 0x001233B8 File Offset: 0x001215B8
		[SecurityCritical]
		public void RegisterObject(object obj)
		{
			SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
			if (serializationEventsForType.HasOnSerializingEvents && this.m_objectSeenTable[obj] == null)
			{
				this.m_objectSeenTable[obj] = true;
				serializationEventsForType.InvokeOnSerializing(obj, this.m_context);
				this.AddOnSerialized(obj);
			}
		}

		// Token: 0x060052DE RID: 21214 RVA: 0x0012340D File Offset: 0x0012160D
		public void RaiseOnSerializedEvent()
		{
			if (this.m_onSerializedHandler != null)
			{
				this.m_onSerializedHandler(this.m_context);
			}
		}

		// Token: 0x060052DF RID: 21215 RVA: 0x00123428 File Offset: 0x00121628
		[SecuritySafeCritical]
		private void AddOnSerialized(object obj)
		{
			SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
			this.m_onSerializedHandler = serializationEventsForType.AddOnSerialized(obj, this.m_onSerializedHandler);
		}

		// Token: 0x040024BE RID: 9406
		private Hashtable m_objectSeenTable = new Hashtable();

		// Token: 0x040024BF RID: 9407
		private SerializationEventHandler m_onSerializedHandler;

		// Token: 0x040024C0 RID: 9408
		private StreamingContext m_context;
	}
}
