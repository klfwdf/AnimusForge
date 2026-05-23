using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000052 RID: 82
	public class MbEvent<T1, T2, T3, T4, T5> : IMbEvent<T1, T2, T3, T4, T5>, IMbEventBase
	{
		// Token: 0x0600089C RID: 2204 RVA: 0x0002641C File Offset: 0x0002461C
		public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5> action)
		{
			MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> eventHandlerRec = new MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5>(owner, action);
			MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x0600089D RID: 2205 RVA: 0x00026446 File Offset: 0x00024646
		public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2, t3, t4, t5);
		}

		// Token: 0x0600089E RID: 2206 RVA: 0x0002645B File Offset: 0x0002465B
		private void InvokeList(MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> list, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			while (list != null)
			{
				list.Action(t1, t2, t3, t4, t5);
				list = list.Next;
			}
		}

		// Token: 0x0600089F RID: 2207 RVA: 0x0002647D File Offset: 0x0002467D
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x060008A0 RID: 2208 RVA: 0x0002648C File Offset: 0x0002468C
		private void ClearListenerOfList(ref MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> list, object o)
		{
			MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> eventHandlerRec2 = list;
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

		// Token: 0x040002B9 RID: 697
		private MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<T1, T2, T3, T4, T5> _nonSerializedListenerList;

		// Token: 0x0200050D RID: 1293
		internal class EventHandlerRec<TA, TB, TC, TD, TE>
		{
			// Token: 0x17000EBB RID: 3771
			// (get) Token: 0x06004B47 RID: 19271 RVA: 0x0017785A File Offset: 0x00175A5A
			// (set) Token: 0x06004B48 RID: 19272 RVA: 0x00177862 File Offset: 0x00175A62
			internal Action<TA, TB, TC, TD, TE> Action { get; private set; }

			// Token: 0x17000EBC RID: 3772
			// (get) Token: 0x06004B49 RID: 19273 RVA: 0x0017786B File Offset: 0x00175A6B
			// (set) Token: 0x06004B4A RID: 19274 RVA: 0x00177873 File Offset: 0x00175A73
			internal object Owner { get; private set; }

			// Token: 0x06004B4B RID: 19275 RVA: 0x0017787C File Offset: 0x00175A7C
			public EventHandlerRec(object owner, Action<TA, TB, TC, TD, TE> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001583 RID: 5507
			public MbEvent<T1, T2, T3, T4, T5>.EventHandlerRec<TA, TB, TC, TD, TE> Next;
		}
	}
}
