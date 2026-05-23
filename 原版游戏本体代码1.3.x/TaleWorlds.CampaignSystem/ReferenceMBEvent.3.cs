using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200004A RID: 74
	public class ReferenceMBEvent<T1, T2, T3> : ReferenceIMBEvent<T1, T2, T3>, IMbEventBase
	{
		// Token: 0x06000880 RID: 2176 RVA: 0x000260E8 File Offset: 0x000242E8
		public void AddNonSerializedListener(object owner, ReferenceAction<T1, T2, T3> action)
		{
			ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> eventHandlerRec = new ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3>(owner, action);
			ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x06000881 RID: 2177 RVA: 0x00026112 File Offset: 0x00024312
		public void Invoke(T1 t1, T2 t2, ref T3 t3)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2, ref t3);
		}

		// Token: 0x06000882 RID: 2178 RVA: 0x00026123 File Offset: 0x00024323
		private void InvokeList(ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> list, T1 t1, T2 t2, ref T3 t3)
		{
			while (list != null)
			{
				list.Action(t1, t2, ref t3);
				list = list.Next;
			}
		}

		// Token: 0x06000883 RID: 2179 RVA: 0x00026141 File Offset: 0x00024341
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x06000884 RID: 2180 RVA: 0x00026150 File Offset: 0x00024350
		private void ClearListenerOfList(ref ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> list, object o)
		{
			ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> eventHandlerRec2 = list;
			if (eventHandlerRec2 == eventHandlerRec)
			{
				list = eventHandlerRec2.Next;
				return;
			}
			while (eventHandlerRec2 != null)
			{
				if (eventHandlerRec2.Next == eventHandlerRec)
				{
					eventHandlerRec2.Next = eventHandlerRec.Next;
				}
				else
				{
					eventHandlerRec2 = eventHandlerRec2.Next;
				}
			}
		}

		// Token: 0x040002B5 RID: 693
		private ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> _nonSerializedListenerList;

		// Token: 0x02000509 RID: 1289
		internal class EventHandlerRec<TS, TQ, TR>
		{
			// Token: 0x17000EB3 RID: 3763
			// (get) Token: 0x06004B33 RID: 19251 RVA: 0x0017777A File Offset: 0x0017597A
			// (set) Token: 0x06004B34 RID: 19252 RVA: 0x00177782 File Offset: 0x00175982
			internal ReferenceAction<TS, TQ, TR> Action { get; private set; }

			// Token: 0x17000EB4 RID: 3764
			// (get) Token: 0x06004B35 RID: 19253 RVA: 0x0017778B File Offset: 0x0017598B
			// (set) Token: 0x06004B36 RID: 19254 RVA: 0x00177793 File Offset: 0x00175993
			internal object Owner { get; private set; }

			// Token: 0x06004B37 RID: 19255 RVA: 0x0017779C File Offset: 0x0017599C
			public EventHandlerRec(object owner, ReferenceAction<TS, TQ, TR> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001577 RID: 5495
			public ReferenceMBEvent<T1, T2, T3>.EventHandlerRec<TS, TQ, TR> Next;
		}
	}
}
