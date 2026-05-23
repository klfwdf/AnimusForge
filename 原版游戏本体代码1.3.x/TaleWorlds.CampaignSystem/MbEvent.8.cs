using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000056 RID: 86
	public class MbEvent<T1, T2, T3, T4, T5, T6, T7> : IMbEvent<T1, T2, T3, T4, T5, T6, T7>, IMbEventBase
	{
		// Token: 0x060008AA RID: 2218 RVA: 0x000265C8 File Offset: 0x000247C8
		public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6, T7> action)
		{
			MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> eventHandlerRec = new MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7>(owner, action);
			MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x060008AB RID: 2219 RVA: 0x000265F4 File Offset: 0x000247F4
		public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2, t3, t4, t5, t6, t7);
		}

		// Token: 0x060008AC RID: 2220 RVA: 0x00026618 File Offset: 0x00024818
		private void InvokeList(MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> list, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
		{
			while (list != null)
			{
				list.Action(t1, t2, t3, t4, t5, t6, t7);
				list = list.Next;
			}
		}

		// Token: 0x060008AD RID: 2221 RVA: 0x0002663E File Offset: 0x0002483E
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x060008AE RID: 2222 RVA: 0x00026650 File Offset: 0x00024850
		private void ClearListenerOfList(ref MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> list, object o)
		{
			MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> eventHandlerRec2 = list;
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

		// Token: 0x040002BB RID: 699
		private MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> _nonSerializedListenerList;

		// Token: 0x0200050F RID: 1295
		internal class EventHandlerRec<TA, TB, TC, TD, TE, TF, TG>
		{
			// Token: 0x17000EBF RID: 3775
			// (get) Token: 0x06004B51 RID: 19281 RVA: 0x001778CA File Offset: 0x00175ACA
			// (set) Token: 0x06004B52 RID: 19282 RVA: 0x001778D2 File Offset: 0x00175AD2
			internal Action<TA, TB, TC, TD, TE, TF, TG> Action { get; private set; }

			// Token: 0x17000EC0 RID: 3776
			// (get) Token: 0x06004B53 RID: 19283 RVA: 0x001778DB File Offset: 0x00175ADB
			// (set) Token: 0x06004B54 RID: 19284 RVA: 0x001778E3 File Offset: 0x00175AE3
			internal object Owner { get; private set; }

			// Token: 0x06004B55 RID: 19285 RVA: 0x001778EC File Offset: 0x00175AEC
			public EventHandlerRec(object owner, Action<TA, TB, TC, TD, TE, TF, TG> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001589 RID: 5513
			public MbEvent<T1, T2, T3, T4, T5, T6, T7>.EventHandlerRec<TA, TB, TC, TD, TE, TF, TG> Next;
		}
	}
}
