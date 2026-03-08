using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000054 RID: 84
	public class MbEvent<T1, T2, T3, T4, T5, T6> : IMbEvent<T1, T2, T3, T4, T5, T6>, IMbEventBase
	{
		// Token: 0x060008A3 RID: 2211 RVA: 0x000264F0 File Offset: 0x000246F0
		public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6> action)
		{
			MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> eventHandlerRec = new MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6>(owner, action);
			MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x060008A4 RID: 2212 RVA: 0x0002651A File Offset: 0x0002471A
		public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2, t3, t4, t5, t6);
		}

		// Token: 0x060008A5 RID: 2213 RVA: 0x00026531 File Offset: 0x00024731
		private void InvokeList(MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> list, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			while (list != null)
			{
				list.Action(t1, t2, t3, t4, t5, t6);
				list = list.Next;
			}
		}

		// Token: 0x060008A6 RID: 2214 RVA: 0x00026555 File Offset: 0x00024755
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x060008A7 RID: 2215 RVA: 0x00026564 File Offset: 0x00024764
		private void ClearListenerOfList(ref MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> list, object o)
		{
			MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> eventHandlerRec2 = list;
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

		// Token: 0x040002BA RID: 698
		private MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<T1, T2, T3, T4, T5, T6> _nonSerializedListenerList;

		// Token: 0x0200050E RID: 1294
		internal class EventHandlerRec<TA, TB, TC, TD, TE, TF>
		{
			// Token: 0x17000EBD RID: 3773
			// (get) Token: 0x06004B4C RID: 19276 RVA: 0x00177892 File Offset: 0x00175A92
			// (set) Token: 0x06004B4D RID: 19277 RVA: 0x0017789A File Offset: 0x00175A9A
			internal Action<TA, TB, TC, TD, TE, TF> Action { get; private set; }

			// Token: 0x17000EBE RID: 3774
			// (get) Token: 0x06004B4E RID: 19278 RVA: 0x001778A3 File Offset: 0x00175AA3
			// (set) Token: 0x06004B4F RID: 19279 RVA: 0x001778AB File Offset: 0x00175AAB
			internal object Owner { get; private set; }

			// Token: 0x06004B50 RID: 19280 RVA: 0x001778B4 File Offset: 0x00175AB4
			public EventHandlerRec(object owner, Action<TA, TB, TC, TD, TE, TF> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001586 RID: 5510
			public MbEvent<T1, T2, T3, T4, T5, T6>.EventHandlerRec<TA, TB, TC, TD, TE, TF> Next;
		}
	}
}
