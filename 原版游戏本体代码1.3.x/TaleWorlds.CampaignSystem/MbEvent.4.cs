using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200004E RID: 78
	public class MbEvent<T1, T2, T3> : IMbEvent<T1, T2, T3>, IMbEventBase
	{
		// Token: 0x0600088E RID: 2190 RVA: 0x00026280 File Offset: 0x00024480
		public void AddNonSerializedListener(object owner, Action<T1, T2, T3> action)
		{
			MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> eventHandlerRec = new MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3>(owner, action);
			MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x0600088F RID: 2191 RVA: 0x000262AA File Offset: 0x000244AA
		public void Invoke(T1 t1, T2 t2, T3 t3)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2, t3);
		}

		// Token: 0x06000890 RID: 2192 RVA: 0x000262BB File Offset: 0x000244BB
		private void InvokeList(MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> list, T1 t1, T2 t2, T3 t3)
		{
			while (list != null)
			{
				list.Action(t1, t2, t3);
				list = list.Next;
			}
		}

		// Token: 0x06000891 RID: 2193 RVA: 0x000262D9 File Offset: 0x000244D9
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x06000892 RID: 2194 RVA: 0x000262E8 File Offset: 0x000244E8
		private void ClearListenerOfList(ref MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> list, object o)
		{
			MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> eventHandlerRec2 = list;
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

		// Token: 0x040002B7 RID: 695
		private MbEvent<T1, T2, T3>.EventHandlerRec<T1, T2, T3> _nonSerializedListenerList;

		// Token: 0x0200050B RID: 1291
		internal class EventHandlerRec<TS, TQ, TR>
		{
			// Token: 0x17000EB7 RID: 3767
			// (get) Token: 0x06004B3D RID: 19261 RVA: 0x001777EA File Offset: 0x001759EA
			// (set) Token: 0x06004B3E RID: 19262 RVA: 0x001777F2 File Offset: 0x001759F2
			internal Action<TS, TQ, TR> Action { get; private set; }

			// Token: 0x17000EB8 RID: 3768
			// (get) Token: 0x06004B3F RID: 19263 RVA: 0x001777FB File Offset: 0x001759FB
			// (set) Token: 0x06004B40 RID: 19264 RVA: 0x00177803 File Offset: 0x00175A03
			internal object Owner { get; private set; }

			// Token: 0x06004B41 RID: 19265 RVA: 0x0017780C File Offset: 0x00175A0C
			public EventHandlerRec(object owner, Action<TS, TQ, TR> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x0400157D RID: 5501
			public MbEvent<T1, T2, T3>.EventHandlerRec<TS, TQ, TR> Next;
		}
	}
}
