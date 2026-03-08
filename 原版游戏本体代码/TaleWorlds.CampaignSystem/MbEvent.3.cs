using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200004C RID: 76
	public class MbEvent<T1, T2> : IMbEvent<T1, T2>, IMbEventBase
	{
		// Token: 0x06000887 RID: 2183 RVA: 0x000261B4 File Offset: 0x000243B4
		public void AddNonSerializedListener(object owner, Action<T1, T2> action)
		{
			MbEvent<T1, T2>.EventHandlerRec<T1, T2> eventHandlerRec = new MbEvent<T1, T2>.EventHandlerRec<T1, T2>(owner, action);
			MbEvent<T1, T2>.EventHandlerRec<T1, T2> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x000261DE File Offset: 0x000243DE
		public void Invoke(T1 t1, T2 t2)
		{
			this.InvokeList(this._nonSerializedListenerList, t1, t2);
		}

		// Token: 0x06000889 RID: 2185 RVA: 0x000261EE File Offset: 0x000243EE
		private void InvokeList(MbEvent<T1, T2>.EventHandlerRec<T1, T2> list, T1 t1, T2 t2)
		{
			while (list != null)
			{
				list.Action(t1, t2);
				list = list.Next;
			}
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x0002620A File Offset: 0x0002440A
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x0600088B RID: 2187 RVA: 0x0002621C File Offset: 0x0002441C
		private void ClearListenerOfList(ref MbEvent<T1, T2>.EventHandlerRec<T1, T2> list, object o)
		{
			MbEvent<T1, T2>.EventHandlerRec<T1, T2> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent<T1, T2>.EventHandlerRec<T1, T2> eventHandlerRec2 = list;
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

		// Token: 0x040002B6 RID: 694
		private MbEvent<T1, T2>.EventHandlerRec<T1, T2> _nonSerializedListenerList;

		// Token: 0x0200050A RID: 1290
		internal class EventHandlerRec<TS, TQ>
		{
			// Token: 0x17000EB5 RID: 3765
			// (get) Token: 0x06004B38 RID: 19256 RVA: 0x001777B2 File Offset: 0x001759B2
			// (set) Token: 0x06004B39 RID: 19257 RVA: 0x001777BA File Offset: 0x001759BA
			internal Action<TS, TQ> Action { get; private set; }

			// Token: 0x17000EB6 RID: 3766
			// (get) Token: 0x06004B3A RID: 19258 RVA: 0x001777C3 File Offset: 0x001759C3
			// (set) Token: 0x06004B3B RID: 19259 RVA: 0x001777CB File Offset: 0x001759CB
			internal object Owner { get; private set; }

			// Token: 0x06004B3C RID: 19260 RVA: 0x001777D4 File Offset: 0x001759D4
			public EventHandlerRec(object owner, Action<TS, TQ> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x0400157A RID: 5498
			public MbEvent<T1, T2>.EventHandlerRec<TS, TQ> Next;
		}
	}
}
