using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000447 RID: 1095
	internal sealed class EventSourceActivity : IDisposable
	{
		// Token: 0x06003620 RID: 13856 RVA: 0x000D2704 File Offset: 0x000D0904
		public EventSourceActivity(EventSource eventSource)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			this.eventSource = eventSource;
		}

		// Token: 0x06003621 RID: 13857 RVA: 0x000D2721 File Offset: 0x000D0921
		public static implicit operator EventSourceActivity(EventSource eventSource)
		{
			return new EventSourceActivity(eventSource);
		}

		// Token: 0x17000803 RID: 2051
		// (get) Token: 0x06003622 RID: 13858 RVA: 0x000D2729 File Offset: 0x000D0929
		public EventSource EventSource
		{
			get
			{
				return this.eventSource;
			}
		}

		// Token: 0x17000804 RID: 2052
		// (get) Token: 0x06003623 RID: 13859 RVA: 0x000D2731 File Offset: 0x000D0931
		public Guid Id
		{
			get
			{
				return this.activityId;
			}
		}

		// Token: 0x06003624 RID: 13860 RVA: 0x000D2739 File Offset: 0x000D0939
		public EventSourceActivity Start<T>(string eventName, EventSourceOptions options, T data)
		{
			return this.Start<T>(eventName, ref options, ref data);
		}

		// Token: 0x06003625 RID: 13861 RVA: 0x000D2748 File Offset: 0x000D0948
		public EventSourceActivity Start(string eventName)
		{
			EventSourceOptions eventSourceOptions = default(EventSourceOptions);
			EmptyStruct emptyStruct = default(EmptyStruct);
			return this.Start<EmptyStruct>(eventName, ref eventSourceOptions, ref emptyStruct);
		}

		// Token: 0x06003626 RID: 13862 RVA: 0x000D2770 File Offset: 0x000D0970
		public EventSourceActivity Start(string eventName, EventSourceOptions options)
		{
			EmptyStruct emptyStruct = default(EmptyStruct);
			return this.Start<EmptyStruct>(eventName, ref options, ref emptyStruct);
		}

		// Token: 0x06003627 RID: 13863 RVA: 0x000D2790 File Offset: 0x000D0990
		public EventSourceActivity Start<T>(string eventName, T data)
		{
			EventSourceOptions eventSourceOptions = default(EventSourceOptions);
			return this.Start<T>(eventName, ref eventSourceOptions, ref data);
		}

		// Token: 0x06003628 RID: 13864 RVA: 0x000D27B0 File Offset: 0x000D09B0
		public void Stop<T>(T data)
		{
			this.Stop<T>(null, ref data);
		}

		// Token: 0x06003629 RID: 13865 RVA: 0x000D27BC File Offset: 0x000D09BC
		public void Stop<T>(string eventName)
		{
			EmptyStruct emptyStruct = default(EmptyStruct);
			this.Stop<EmptyStruct>(eventName, ref emptyStruct);
		}

		// Token: 0x0600362A RID: 13866 RVA: 0x000D27DA File Offset: 0x000D09DA
		public void Stop<T>(string eventName, T data)
		{
			this.Stop<T>(eventName, ref data);
		}

		// Token: 0x0600362B RID: 13867 RVA: 0x000D27E5 File Offset: 0x000D09E5
		public void Write<T>(string eventName, EventSourceOptions options, T data)
		{
			this.Write<T>(this.eventSource, eventName, ref options, ref data);
		}

		// Token: 0x0600362C RID: 13868 RVA: 0x000D27F8 File Offset: 0x000D09F8
		public void Write<T>(string eventName, T data)
		{
			EventSourceOptions eventSourceOptions = default(EventSourceOptions);
			this.Write<T>(this.eventSource, eventName, ref eventSourceOptions, ref data);
		}

		// Token: 0x0600362D RID: 13869 RVA: 0x000D2820 File Offset: 0x000D0A20
		public void Write(string eventName, EventSourceOptions options)
		{
			EmptyStruct emptyStruct = default(EmptyStruct);
			this.Write<EmptyStruct>(this.eventSource, eventName, ref options, ref emptyStruct);
		}

		// Token: 0x0600362E RID: 13870 RVA: 0x000D2848 File Offset: 0x000D0A48
		public void Write(string eventName)
		{
			EventSourceOptions eventSourceOptions = default(EventSourceOptions);
			EmptyStruct emptyStruct = default(EmptyStruct);
			this.Write<EmptyStruct>(this.eventSource, eventName, ref eventSourceOptions, ref emptyStruct);
		}

		// Token: 0x0600362F RID: 13871 RVA: 0x000D2876 File Offset: 0x000D0A76
		public void Write<T>(EventSource source, string eventName, EventSourceOptions options, T data)
		{
			this.Write<T>(source, eventName, ref options, ref data);
		}

		// Token: 0x06003630 RID: 13872 RVA: 0x000D2884 File Offset: 0x000D0A84
		public void Dispose()
		{
			if (this.state == EventSourceActivity.State.Started)
			{
				EmptyStruct emptyStruct = default(EmptyStruct);
				this.Stop<EmptyStruct>(null, ref emptyStruct);
			}
		}

		// Token: 0x06003631 RID: 13873 RVA: 0x000D28AC File Offset: 0x000D0AAC
		private EventSourceActivity Start<T>(string eventName, ref EventSourceOptions options, ref T data)
		{
			if (this.state != EventSourceActivity.State.Started)
			{
				throw new InvalidOperationException();
			}
			if (!this.eventSource.IsEnabled())
			{
				return this;
			}
			EventSourceActivity eventSourceActivity = new EventSourceActivity(this.eventSource);
			if (!this.eventSource.IsEnabled(options.Level, options.Keywords))
			{
				Guid id = this.Id;
				eventSourceActivity.activityId = Guid.NewGuid();
				eventSourceActivity.startStopOptions = options;
				eventSourceActivity.eventName = eventName;
				eventSourceActivity.startStopOptions.Opcode = EventOpcode.Start;
				this.eventSource.Write<T>(eventName, ref eventSourceActivity.startStopOptions, ref eventSourceActivity.activityId, ref id, ref data);
			}
			else
			{
				eventSourceActivity.activityId = this.Id;
			}
			return eventSourceActivity;
		}

		// Token: 0x06003632 RID: 13874 RVA: 0x000D2956 File Offset: 0x000D0B56
		private void Write<T>(EventSource eventSource, string eventName, ref EventSourceOptions options, ref T data)
		{
			if (this.state != EventSourceActivity.State.Started)
			{
				throw new InvalidOperationException();
			}
			if (eventName == null)
			{
				throw new ArgumentNullException();
			}
			eventSource.Write<T>(eventName, ref options, ref this.activityId, ref EventSourceActivity.s_empty, ref data);
		}

		// Token: 0x06003633 RID: 13875 RVA: 0x000D2984 File Offset: 0x000D0B84
		private void Stop<T>(string eventName, ref T data)
		{
			if (this.state != EventSourceActivity.State.Started)
			{
				throw new InvalidOperationException();
			}
			if (!this.StartEventWasFired)
			{
				return;
			}
			this.state = EventSourceActivity.State.Stopped;
			if (eventName == null)
			{
				eventName = this.eventName;
				if (eventName.EndsWith("Start"))
				{
					eventName = eventName.Substring(0, eventName.Length - 5);
				}
				eventName += "Stop";
			}
			this.startStopOptions.Opcode = EventOpcode.Stop;
			this.eventSource.Write<T>(eventName, ref this.startStopOptions, ref this.activityId, ref EventSourceActivity.s_empty, ref data);
		}

		// Token: 0x17000805 RID: 2053
		// (get) Token: 0x06003634 RID: 13876 RVA: 0x000D2A0F File Offset: 0x000D0C0F
		private bool StartEventWasFired
		{
			get
			{
				return this.eventName != null;
			}
		}

		// Token: 0x04001832 RID: 6194
		private readonly EventSource eventSource;

		// Token: 0x04001833 RID: 6195
		private EventSourceOptions startStopOptions;

		// Token: 0x04001834 RID: 6196
		internal Guid activityId;

		// Token: 0x04001835 RID: 6197
		private EventSourceActivity.State state;

		// Token: 0x04001836 RID: 6198
		private string eventName;

		// Token: 0x04001837 RID: 6199
		internal static Guid s_empty;

		// Token: 0x02000B9D RID: 2973
		private enum State
		{
			// Token: 0x04003534 RID: 13620
			Started,
			// Token: 0x04003535 RID: 13621
			Stopped
		}
	}
}
