using System;
using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000422 RID: 1058
	[__DynamicallyInvokable]
	public class EventListener : IDisposable
	{
		// Token: 0x14000016 RID: 22
		// (add) Token: 0x060034F1 RID: 13553 RVA: 0x000CDBDC File Offset: 0x000CBDDC
		// (remove) Token: 0x060034F2 RID: 13554 RVA: 0x000CDC14 File Offset: 0x000CBE14
		private event EventHandler<EventSourceCreatedEventArgs> _EventSourceCreated;

		// Token: 0x14000017 RID: 23
		// (add) Token: 0x060034F3 RID: 13555 RVA: 0x000CDC4C File Offset: 0x000CBE4C
		// (remove) Token: 0x060034F4 RID: 13556 RVA: 0x000CDCA4 File Offset: 0x000CBEA4
		public event EventHandler<EventSourceCreatedEventArgs> EventSourceCreated
		{
			add
			{
				object obj = EventListener.s_EventSourceCreatedLock;
				lock (obj)
				{
					this.CallBackForExistingEventSources(false, value);
					this._EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>)Delegate.Combine(this._EventSourceCreated, value);
				}
			}
			remove
			{
				object obj = EventListener.s_EventSourceCreatedLock;
				lock (obj)
				{
					this._EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>)Delegate.Remove(this._EventSourceCreated, value);
				}
			}
		}

		// Token: 0x14000018 RID: 24
		// (add) Token: 0x060034F5 RID: 13557 RVA: 0x000CDCF4 File Offset: 0x000CBEF4
		// (remove) Token: 0x060034F6 RID: 13558 RVA: 0x000CDD2C File Offset: 0x000CBF2C
		public event EventHandler<EventWrittenEventArgs> EventWritten;

		// Token: 0x060034F7 RID: 13559 RVA: 0x000CDD61 File Offset: 0x000CBF61
		[__DynamicallyInvokable]
		public EventListener()
		{
			this.CallBackForExistingEventSources(true, delegate(object obj, EventSourceCreatedEventArgs args)
			{
				args.EventSource.AddListener(this);
			});
		}

		// Token: 0x060034F8 RID: 13560 RVA: 0x000CDD7C File Offset: 0x000CBF7C
		[__DynamicallyInvokable]
		public virtual void Dispose()
		{
			object eventListenersLock = EventListener.EventListenersLock;
			lock (eventListenersLock)
			{
				if (EventListener.s_Listeners != null)
				{
					if (this == EventListener.s_Listeners)
					{
						EventListener listenerToRemove = EventListener.s_Listeners;
						EventListener.s_Listeners = this.m_Next;
						EventListener.RemoveReferencesToListenerInEventSources(listenerToRemove);
					}
					else
					{
						EventListener eventListener = EventListener.s_Listeners;
						EventListener next;
						for (;;)
						{
							next = eventListener.m_Next;
							if (next == null)
							{
								break;
							}
							if (next == this)
							{
								goto Block_6;
							}
							eventListener = next;
						}
						return;
						Block_6:
						eventListener.m_Next = next.m_Next;
						EventListener.RemoveReferencesToListenerInEventSources(next);
					}
				}
			}
		}

		// Token: 0x060034F9 RID: 13561 RVA: 0x000CDE1C File Offset: 0x000CC01C
		[__DynamicallyInvokable]
		public void EnableEvents(EventSource eventSource, EventLevel level)
		{
			this.EnableEvents(eventSource, level, EventKeywords.None);
		}

		// Token: 0x060034FA RID: 13562 RVA: 0x000CDE28 File Offset: 0x000CC028
		[__DynamicallyInvokable]
		public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
		{
			this.EnableEvents(eventSource, level, matchAnyKeyword, null);
		}

		// Token: 0x060034FB RID: 13563 RVA: 0x000CDE34 File Offset: 0x000CC034
		[__DynamicallyInvokable]
		public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> arguments)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			eventSource.SendCommand(this, 0, 0, EventCommand.Update, true, level, matchAnyKeyword, arguments);
		}

		// Token: 0x060034FC RID: 13564 RVA: 0x000CDE60 File Offset: 0x000CC060
		[__DynamicallyInvokable]
		public void DisableEvents(EventSource eventSource)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			eventSource.SendCommand(this, 0, 0, EventCommand.Update, false, EventLevel.LogAlways, EventKeywords.None, null);
		}

		// Token: 0x060034FD RID: 13565 RVA: 0x000CDE8A File Offset: 0x000CC08A
		[__DynamicallyInvokable]
		public static int EventSourceIndex(EventSource eventSource)
		{
			return eventSource.m_id;
		}

		// Token: 0x060034FE RID: 13566 RVA: 0x000CDE94 File Offset: 0x000CC094
		[__DynamicallyInvokable]
		protected internal virtual void OnEventSourceCreated(EventSource eventSource)
		{
			EventHandler<EventSourceCreatedEventArgs> eventSourceCreated = this._EventSourceCreated;
			if (eventSourceCreated != null)
			{
				eventSourceCreated(this, new EventSourceCreatedEventArgs
				{
					EventSource = eventSource
				});
			}
		}

		// Token: 0x060034FF RID: 13567 RVA: 0x000CDEC0 File Offset: 0x000CC0C0
		[__DynamicallyInvokable]
		protected internal virtual void OnEventWritten(EventWrittenEventArgs eventData)
		{
			EventHandler<EventWrittenEventArgs> eventWritten = this.EventWritten;
			if (eventWritten != null)
			{
				eventWritten(this, eventData);
			}
		}

		// Token: 0x06003500 RID: 13568 RVA: 0x000CDEE0 File Offset: 0x000CC0E0
		internal static void AddEventSource(EventSource newEventSource)
		{
			object eventListenersLock = EventListener.EventListenersLock;
			lock (eventListenersLock)
			{
				if (EventListener.s_EventSources == null)
				{
					EventListener.s_EventSources = new List<WeakReference>(2);
				}
				if (!EventListener.s_EventSourceShutdownRegistered)
				{
					EventListener.s_EventSourceShutdownRegistered = true;
					AppDomain.CurrentDomain.ProcessExit += EventListener.DisposeOnShutdown;
					AppDomain.CurrentDomain.DomainUnload += EventListener.DisposeOnShutdown;
				}
				int num = -1;
				if (EventListener.s_EventSources.Count % 64 == 63)
				{
					int num2 = EventListener.s_EventSources.Count;
					while (0 < num2)
					{
						num2--;
						WeakReference weakReference = EventListener.s_EventSources[num2];
						if (!weakReference.IsAlive)
						{
							num = num2;
							weakReference.Target = newEventSource;
							break;
						}
					}
				}
				if (num < 0)
				{
					num = EventListener.s_EventSources.Count;
					EventListener.s_EventSources.Add(new WeakReference(newEventSource));
				}
				newEventSource.m_id = num;
				for (EventListener next = EventListener.s_Listeners; next != null; next = next.m_Next)
				{
					newEventSource.AddListener(next);
				}
			}
		}

		// Token: 0x06003501 RID: 13569 RVA: 0x000CDFF4 File Offset: 0x000CC1F4
		private static void DisposeOnShutdown(object sender, EventArgs e)
		{
			object eventListenersLock = EventListener.EventListenersLock;
			lock (eventListenersLock)
			{
				foreach (WeakReference weakReference in EventListener.s_EventSources)
				{
					EventSource eventSource = weakReference.Target as EventSource;
					if (eventSource != null)
					{
						eventSource.Dispose();
					}
				}
			}
		}

		// Token: 0x06003502 RID: 13570 RVA: 0x000CE080 File Offset: 0x000CC280
		private static void RemoveReferencesToListenerInEventSources(EventListener listenerToRemove)
		{
			using (List<WeakReference>.Enumerator enumerator = EventListener.s_EventSources.GetEnumerator())
			{
				IL_7E:
				while (enumerator.MoveNext())
				{
					WeakReference weakReference = enumerator.Current;
					EventSource eventSource = weakReference.Target as EventSource;
					if (eventSource != null)
					{
						if (eventSource.m_Dispatchers.m_Listener == listenerToRemove)
						{
							eventSource.m_Dispatchers = eventSource.m_Dispatchers.m_Next;
						}
						else
						{
							EventDispatcher eventDispatcher = eventSource.m_Dispatchers;
							EventDispatcher next;
							for (;;)
							{
								next = eventDispatcher.m_Next;
								if (next == null)
								{
									goto IL_7E;
								}
								if (next.m_Listener == listenerToRemove)
								{
									break;
								}
								eventDispatcher = next;
							}
							eventDispatcher.m_Next = next.m_Next;
						}
					}
				}
			}
		}

		// Token: 0x06003503 RID: 13571 RVA: 0x000CE134 File Offset: 0x000CC334
		[Conditional("DEBUG")]
		internal static void Validate()
		{
			object eventListenersLock = EventListener.EventListenersLock;
			lock (eventListenersLock)
			{
				Dictionary<EventListener, bool> dictionary = new Dictionary<EventListener, bool>();
				for (EventListener next = EventListener.s_Listeners; next != null; next = next.m_Next)
				{
					dictionary.Add(next, true);
				}
				int num = -1;
				foreach (WeakReference weakReference in EventListener.s_EventSources)
				{
					num++;
					EventSource eventSource = weakReference.Target as EventSource;
					if (eventSource != null)
					{
						for (EventDispatcher eventDispatcher = eventSource.m_Dispatchers; eventDispatcher != null; eventDispatcher = eventDispatcher.m_Next)
						{
						}
						foreach (EventListener eventListener in dictionary.Keys)
						{
							EventDispatcher eventDispatcher = eventSource.m_Dispatchers;
							while (eventDispatcher.m_Listener != eventListener)
							{
								eventDispatcher = eventDispatcher.m_Next;
							}
						}
					}
				}
			}
		}

		// Token: 0x170007CF RID: 1999
		// (get) Token: 0x06003504 RID: 13572 RVA: 0x000CE264 File Offset: 0x000CC464
		internal static object EventListenersLock
		{
			get
			{
				if (EventListener.s_EventSources == null)
				{
					Interlocked.CompareExchange<List<WeakReference>>(ref EventListener.s_EventSources, new List<WeakReference>(2), null);
				}
				return EventListener.s_EventSources;
			}
		}

		// Token: 0x06003505 RID: 13573 RVA: 0x000CE284 File Offset: 0x000CC484
		private void CallBackForExistingEventSources(bool addToListenersList, EventHandler<EventSourceCreatedEventArgs> callback)
		{
			object eventListenersLock = EventListener.EventListenersLock;
			lock (eventListenersLock)
			{
				if (EventListener.s_CreatingListener)
				{
					throw new InvalidOperationException(Environment.GetResourceString("EventSource_ListenerCreatedInsideCallback"));
				}
				try
				{
					EventListener.s_CreatingListener = true;
					if (addToListenersList)
					{
						this.m_Next = EventListener.s_Listeners;
						EventListener.s_Listeners = this;
					}
					foreach (WeakReference weakReference in EventListener.s_EventSources.ToArray())
					{
						EventSource eventSource = weakReference.Target as EventSource;
						if (eventSource != null)
						{
							callback(this, new EventSourceCreatedEventArgs
							{
								EventSource = eventSource
							});
						}
					}
				}
				finally
				{
					EventListener.s_CreatingListener = false;
				}
			}
		}

		// Token: 0x04001776 RID: 6006
		private static readonly object s_EventSourceCreatedLock = new object();

		// Token: 0x04001779 RID: 6009
		internal volatile EventListener m_Next;

		// Token: 0x0400177A RID: 6010
		internal ActivityFilter m_activityFilter;

		// Token: 0x0400177B RID: 6011
		internal static EventListener s_Listeners;

		// Token: 0x0400177C RID: 6012
		internal static List<WeakReference> s_EventSources;

		// Token: 0x0400177D RID: 6013
		private static bool s_CreatingListener = false;

		// Token: 0x0400177E RID: 6014
		private static bool s_EventSourceShutdownRegistered = false;
	}
}
