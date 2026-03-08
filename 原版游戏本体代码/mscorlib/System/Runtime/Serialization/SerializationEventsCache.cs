using System;
using System.Collections;

namespace System.Runtime.Serialization
{
	// Token: 0x02000758 RID: 1880
	internal static class SerializationEventsCache
	{
		// Token: 0x060052E8 RID: 21224 RVA: 0x0012382C File Offset: 0x00121A2C
		internal static SerializationEvents GetSerializationEventsForType(Type t)
		{
			SerializationEvents serializationEvents;
			if ((serializationEvents = (SerializationEvents)SerializationEventsCache.cache[t]) == null)
			{
				object syncRoot = SerializationEventsCache.cache.SyncRoot;
				lock (syncRoot)
				{
					if ((serializationEvents = (SerializationEvents)SerializationEventsCache.cache[t]) == null)
					{
						serializationEvents = new SerializationEvents(t);
						SerializationEventsCache.cache[t] = serializationEvents;
					}
				}
			}
			return serializationEvents;
		}

		// Token: 0x040024C5 RID: 9413
		private static Hashtable cache = new Hashtable();
	}
}
