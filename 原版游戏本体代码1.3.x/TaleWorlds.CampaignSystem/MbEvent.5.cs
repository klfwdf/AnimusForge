using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000050 RID: 80
	public class MbEvent<T1, T2, T3, T4> : IMbEvent<T1, T2, T3, T4>, IMbEventBase
	{
		// Token: 0x06000895 RID: 2197 RVA: 0x0002634C File Offset: 0x0002454C
		public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4> action)
		{
			MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> eventHandlerRec = new MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4>(owner, action);
			MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x06000896 RID: 2198 RVA: 0x00026376 File Offset: 0x00024576
		public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2, t3, t4);
		}

		// Token: 0x06000897 RID: 2199 RVA: 0x00026389 File Offset: 0x00024589
		private void InvokeList(MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> list, T1 t1, T2 t2, T3 t3, T4 t4)
		{
			while (list != null)
			{
				list.Action(t1, t2, t3, t4);
				list = list.Next;
			}
		}

		// Token: 0x06000898 RID: 2200 RVA: 0x000263A9 File Offset: 0x000245A9
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x06000899 RID: 2201 RVA: 0x000263B8 File Offset: 0x000245B8
		private void ClearListenerOfList(ref MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> list, object o)
		{
			MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> eventHandlerRec2 = list;
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

		// Token: 0x040002B8 RID: 696
		private MbEvent<T1, T2, T3, T4>.EventHandlerRec<T1, T2, T3, T4> _nonSerializedListenerList;

		// Token: 0x0200050C RID: 1292
		internal class EventHandlerRec<TA, TB, TC, TD>
		{
			// Token: 0x17000EB9 RID: 3769
			// (get) Token: 0x06004B42 RID: 19266 RVA: 0x00177822 File Offset: 0x00175A22
			// (set) Token: 0x06004B43 RID: 19267 RVA: 0x0017782A File Offset: 0x00175A2A
			internal Action<TA, TB, TC, TD> Action { get; private set; }

			// Token: 0x17000EBA RID: 3770
			// (get) Token: 0x06004B44 RID: 19268 RVA: 0x00177833 File Offset: 0x00175A33
			// (set) Token: 0x06004B45 RID: 19269 RVA: 0x0017783B File Offset: 0x00175A3B
			internal object Owner { get; private set; }

			// Token: 0x06004B46 RID: 19270 RVA: 0x00177844 File Offset: 0x00175A44
			public EventHandlerRec(object owner, Action<TA, TB, TC, TD> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001580 RID: 5504
			public MbEvent<T1, T2, T3, T4>.EventHandlerRec<TA, TB, TC, TD> Next;
		}
	}
}
