using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000048 RID: 72
	public class ReferenceMBEvent<T1, T2> : ReferenceIMBEvent<T1, T2>, IMbEventBase
	{
		// Token: 0x06000879 RID: 2169 RVA: 0x0002601C File Offset: 0x0002421C
		public void AddNonSerializedListener(object owner, ReferenceAction<T1, T2> action)
		{
			ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> eventHandlerRec = new ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2>(owner, action);
			ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x0600087A RID: 2170 RVA: 0x00026046 File Offset: 0x00024246
		public void Invoke(T1 t1, ref T2 t2)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, ref t2);
		}

		// Token: 0x0600087B RID: 2171 RVA: 0x00026056 File Offset: 0x00024256
		private void InvokeList(ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> list, T1 t1, ref T2 t2)
		{
			while (list != null)
			{
				list.Action(t1, ref t2);
				list = list.Next;
			}
		}

		// Token: 0x0600087C RID: 2172 RVA: 0x00026072 File Offset: 0x00024272
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x0600087D RID: 2173 RVA: 0x00026084 File Offset: 0x00024284
		private void ClearListenerOfList(ref ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> list, object o)
		{
			ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> eventHandlerRec2 = list;
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

		// Token: 0x040002B4 RID: 692
		private ReferenceMBEvent<T1, T2>.EventHandlerRec<T1, T2> _nonSerializedListenerList;

		// Token: 0x02000508 RID: 1288
		internal class EventHandlerRec<TS, TQ>
		{
			// Token: 0x17000EB1 RID: 3761
			// (get) Token: 0x06004B2E RID: 19246 RVA: 0x00177742 File Offset: 0x00175942
			// (set) Token: 0x06004B2F RID: 19247 RVA: 0x0017774A File Offset: 0x0017594A
			internal ReferenceAction<TS, TQ> Action { get; private set; }

			// Token: 0x17000EB2 RID: 3762
			// (get) Token: 0x06004B30 RID: 19248 RVA: 0x00177753 File Offset: 0x00175953
			// (set) Token: 0x06004B31 RID: 19249 RVA: 0x0017775B File Offset: 0x0017595B
			internal object Owner { get; private set; }

			// Token: 0x06004B32 RID: 19250 RVA: 0x00177764 File Offset: 0x00175964
			public EventHandlerRec(object owner, ReferenceAction<TS, TQ> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001574 RID: 5492
			public ReferenceMBEvent<T1, T2>.EventHandlerRec<TS, TQ> Next;
		}
	}
}
