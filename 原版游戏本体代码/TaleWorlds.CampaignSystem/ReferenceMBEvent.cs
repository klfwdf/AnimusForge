using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000046 RID: 70
	public class ReferenceMBEvent<T1> : ReferenceIMBEvent<T1>, IMbEventBase
	{
		// Token: 0x06000872 RID: 2162 RVA: 0x00025F54 File Offset: 0x00024154
		public void AddNonSerializedListener(object owner, ReferenceAction<T1> action)
		{
			ReferenceMBEvent<T1>.EventHandlerRec<T1> eventHandlerRec = new ReferenceMBEvent<T1>.EventHandlerRec<T1>(owner, action);
			ReferenceMBEvent<T1>.EventHandlerRec<T1> nonSerializedListenerList = this._nonSerializedListenerList;
			this._nonSerializedListenerList = eventHandlerRec;
			eventHandlerRec.Next = nonSerializedListenerList;
		}

		// Token: 0x06000873 RID: 2163 RVA: 0x00025F7E File Offset: 0x0002417E
		public void Invoke(ref T1 t1)
		{
			this.InvokeList(this._nonSerializedListenerList, ref t1);
		}

		// Token: 0x06000874 RID: 2164 RVA: 0x00025F8D File Offset: 0x0002418D
		private void InvokeList(ReferenceMBEvent<T1>.EventHandlerRec<T1> list, ref T1 t1)
		{
			while (list != null)
			{
				list.Action(ref t1);
				list = list.Next;
			}
		}

		// Token: 0x06000875 RID: 2165 RVA: 0x00025FA8 File Offset: 0x000241A8
		public void ClearListeners(object o)
		{
			this.ClearListenerOfList(ref this._nonSerializedListenerList, o);
		}

		// Token: 0x06000876 RID: 2166 RVA: 0x00025FB8 File Offset: 0x000241B8
		private void ClearListenerOfList(ref ReferenceMBEvent<T1>.EventHandlerRec<T1> list, object o)
		{
			ReferenceMBEvent<T1>.EventHandlerRec<T1> eventHandlerRec = list;
			while (eventHandlerRec != null && eventHandlerRec.Owner != o)
			{
				eventHandlerRec = eventHandlerRec.Next;
			}
			if (eventHandlerRec == null)
			{
				return;
			}
			ReferenceMBEvent<T1>.EventHandlerRec<T1> eventHandlerRec2 = list;
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

		// Token: 0x040002B3 RID: 691
		private ReferenceMBEvent<T1>.EventHandlerRec<T1> _nonSerializedListenerList;

		// Token: 0x02000507 RID: 1287
		internal class EventHandlerRec<TS>
		{
			// Token: 0x17000EAF RID: 3759
			// (get) Token: 0x06004B29 RID: 19241 RVA: 0x0017770A File Offset: 0x0017590A
			// (set) Token: 0x06004B2A RID: 19242 RVA: 0x00177712 File Offset: 0x00175912
			internal ReferenceAction<TS> Action { get; private set; }

			// Token: 0x17000EB0 RID: 3760
			// (get) Token: 0x06004B2B RID: 19243 RVA: 0x0017771B File Offset: 0x0017591B
			// (set) Token: 0x06004B2C RID: 19244 RVA: 0x00177723 File Offset: 0x00175923
			internal object Owner { get; private set; }

			// Token: 0x06004B2D RID: 19245 RVA: 0x0017772C File Offset: 0x0017592C
			public EventHandlerRec(object owner, ReferenceAction<TS> action)
			{
				this.Action = action;
				this.Owner = owner;
			}

			// Token: 0x04001571 RID: 5489
			public ReferenceMBEvent<T1>.EventHandlerRec<TS> Next;
		}
	}
}
