using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200042F RID: 1071
	internal class EventDispatcher
	{
		// Token: 0x0600356F RID: 13679 RVA: 0x000CF20A File Offset: 0x000CD40A
		internal EventDispatcher(EventDispatcher next, bool[] eventEnabled, EventListener listener)
		{
			this.m_Next = next;
			this.m_EventEnabled = eventEnabled;
			this.m_Listener = listener;
		}

		// Token: 0x040017C3 RID: 6083
		internal readonly EventListener m_Listener;

		// Token: 0x040017C4 RID: 6084
		internal bool[] m_EventEnabled;

		// Token: 0x040017C5 RID: 6085
		internal bool m_activityFilteringEnabled;

		// Token: 0x040017C6 RID: 6086
		internal EventDispatcher m_Next;
	}
}
