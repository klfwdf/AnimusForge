using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000039 RID: 57
	public class MbEvent : IMbEvent
	{
		// Token: 0x060003D6 RID: 982 RVA: 0x0001E18C File Offset: 0x0001C38C
		public void AddNonSerializedListener(object owner, Action action)
		{
			MbEvent.EventHandlerRec eventHandlerRec = new MbEvent.EventHandlerRec(owner, action);
			MbEvent.EventHandlerRec nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x0001E1B6 File Offset: 0x0001C3B6
		public void Invoke()
		{
			this.InvokeList(this._nonSerializedListenerList);
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x0001E1C4 File Offset: 0x0001C3C4
		private void InvokeList(MbEvent.EventHandlerRec list)
		{
			while (list != null)
			{
				list.Action();
				list = list.Next;
			}
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x0001E1DE File Offset: 0x0001C3DE
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x060003DA RID: 986 RVA: 0x0001E1F0 File Offset: 0x0001C3F0
		private void ClearListenerOfList(ref MbEvent.EventHandlerRec list, object o)
		{
			MbEvent.EventHandlerRec eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			MbEvent.EventHandlerRec eventHandlerRec2 = list;
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

		// Token: 0x04000180 RID: 384
		private MbEvent.EventHandlerRec _nonSerializedListenerList;

		// Token: 0x02000502 RID: 1282
		internal class EventHandlerRec
		{
			// Token: 0x17000EA9 RID: 3753
			// (get) Token: 0x06004AFB RID: 19195 RVA: 0x00177348 File Offset: 0x00175548
			// (set) Token: 0x06004AFC RID: 19196 RVA: 0x00177350 File Offset: 0x00175550
			internal Action Action { get; private set; }

			// Token: 0x17000EAA RID: 3754
			// (get) Token: 0x06004AFD RID: 19197 RVA: 0x00177359 File Offset: 0x00175559
			// (set) Token: 0x06004AFE RID: 19198 RVA: 0x00177361 File Offset: 0x00175561
			internal object Owner { get; private set; }

			// Token: 0x06004AFF RID: 19199 RVA: 0x0017736A File Offset: 0x0017556A
			public EventHandlerRec(object owner, Action action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001550 RID: 5456
			public MbEvent.EventHandlerRec Next;
		}
	}
}
