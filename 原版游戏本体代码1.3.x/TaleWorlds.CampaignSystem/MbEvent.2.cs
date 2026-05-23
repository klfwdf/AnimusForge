using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000044 RID: 68
	public class MbEvent<T> : IMbEvent<T>, IMbEventBase
	{
		// Token: 0x0600086B RID: 2155 RVA: 0x00025E8C File Offset: 0x0002408C
		public void AddNonSerializedListener(object owner, Action<T> action)
		{
			MbEvent<T>.EventHandlerRec<T> eventHandlerRec = new MbEvent<T>.EventHandlerRec<T>(owner, action);
			MbEvent<T>.EventHandlerRec<T> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x0600086C RID: 2156 RVA: 0x00025EB6 File Offset: 0x000240B6
		public void Invoke(T t)
		{
			this.InvokeList(this._nonSerializedListenerList, t);
		}

		// Token: 0x0600086D RID: 2157 RVA: 0x00025EC5 File Offset: 0x000240C5
		private void InvokeList(MbEvent<T>.EventHandlerRec<T> list, T t)
		{
			while (list != null)
			{
				list.Action(t);
				list = list.Next;
			}
		}

		// Token: 0x0600086E RID: 2158 RVA: 0x00025EE0 File Offset: 0x000240E0
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x0600086F RID: 2159 RVA: 0x00025EF0 File Offset: 0x000240F0
		private void ClearListenerOfList(ref MbEvent<T>.EventHandlerRec<T> list, object o)
		{
			MbEvent<T>.EventHandlerRec<T> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T>.EventHandlerRec<T> eventHandlerRec2 = list;
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

		// Token: 0x040002B2 RID: 690
		private MbEvent<T>.EventHandlerRec<T> _nonSerializedListenerList;

		// Token: 0x02000506 RID: 1286
		internal class EventHandlerRec<TS>
		{
			// Token: 0x17000EAD RID: 3757
			// (get) Token: 0x06004B24 RID: 19236 RVA: 0x001776D2 File Offset: 0x001758D2
			// (set) Token: 0x06004B25 RID: 19237 RVA: 0x001776DA File Offset: 0x001758DA
			internal Action<TS> Action { get; private set; }

			// Token: 0x17000EAE RID: 3758
			// (get) Token: 0x06004B26 RID: 19238 RVA: 0x001776E3 File Offset: 0x001758E3
			// (set) Token: 0x06004B27 RID: 19239 RVA: 0x001776EB File Offset: 0x001758EB
			internal object Owner { get; private set; }

			// Token: 0x06004B28 RID: 19240 RVA: 0x001776F4 File Offset: 0x001758F4
			public EventHandlerRec(object owner, Action<TS> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x0400156E RID: 5486
			public MbEvent<T>.EventHandlerRec<TS> Next;
		}
	}
}
