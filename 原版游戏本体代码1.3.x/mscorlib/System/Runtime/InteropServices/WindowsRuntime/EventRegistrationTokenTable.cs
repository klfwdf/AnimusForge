using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E1 RID: 2529
	[__DynamicallyInvokable]
	public sealed class EventRegistrationTokenTable<T> where T : class
	{
		// Token: 0x06006476 RID: 25718 RVA: 0x0015678C File Offset: 0x0015498C
		[__DynamicallyInvokable]
		public EventRegistrationTokenTable()
		{
			if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EventTokenTableRequiresDelegate", new object[] { typeof(T) }));
			}
		}

		// Token: 0x17001152 RID: 4434
		// (get) Token: 0x06006477 RID: 25719 RVA: 0x001567E8 File Offset: 0x001549E8
		// (set) Token: 0x06006478 RID: 25720 RVA: 0x001567F4 File Offset: 0x001549F4
		[__DynamicallyInvokable]
		public T InvocationList
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_invokeList;
			}
			[__DynamicallyInvokable]
			set
			{
				Dictionary<EventRegistrationToken, T> tokens = this.m_tokens;
				lock (tokens)
				{
					this.m_tokens.Clear();
					this.m_invokeList = default(T);
					if (value != null)
					{
						this.AddEventHandlerNoLock(value);
					}
				}
			}
		}

		// Token: 0x06006479 RID: 25721 RVA: 0x0015685C File Offset: 0x00154A5C
		[__DynamicallyInvokable]
		public EventRegistrationToken AddEventHandler(T handler)
		{
			if (handler == null)
			{
				return new EventRegistrationToken(0UL);
			}
			Dictionary<EventRegistrationToken, T> tokens = this.m_tokens;
			EventRegistrationToken result;
			lock (tokens)
			{
				result = this.AddEventHandlerNoLock(handler);
			}
			return result;
		}

		// Token: 0x0600647A RID: 25722 RVA: 0x001568B0 File Offset: 0x00154AB0
		private EventRegistrationToken AddEventHandlerNoLock(T handler)
		{
			EventRegistrationToken preferredToken = EventRegistrationTokenTable<T>.GetPreferredToken(handler);
			while (this.m_tokens.ContainsKey(preferredToken))
			{
				preferredToken = new EventRegistrationToken(preferredToken.Value + 1UL);
			}
			this.m_tokens[preferredToken] = handler;
			Delegate @delegate = (Delegate)((object)this.m_invokeList);
			@delegate = Delegate.Combine(@delegate, (Delegate)((object)handler));
			this.m_invokeList = (T)((object)@delegate);
			return preferredToken;
		}

		// Token: 0x0600647B RID: 25723 RVA: 0x00156928 File Offset: 0x00154B28
		[FriendAccessAllowed]
		internal T ExtractHandler(EventRegistrationToken token)
		{
			T result = default(T);
			Dictionary<EventRegistrationToken, T> tokens = this.m_tokens;
			lock (tokens)
			{
				if (this.m_tokens.TryGetValue(token, out result))
				{
					this.RemoveEventHandlerNoLock(token);
				}
			}
			return result;
		}

		// Token: 0x0600647C RID: 25724 RVA: 0x00156984 File Offset: 0x00154B84
		private static EventRegistrationToken GetPreferredToken(T handler)
		{
			Delegate[] invocationList = ((Delegate)((object)handler)).GetInvocationList();
			uint hashCode;
			if (invocationList.Length == 1)
			{
				hashCode = (uint)invocationList[0].Method.GetHashCode();
			}
			else
			{
				hashCode = (uint)handler.GetHashCode();
			}
			ulong value = ((ulong)typeof(T).MetadataToken << 32) | (ulong)hashCode;
			return new EventRegistrationToken(value);
		}

		// Token: 0x0600647D RID: 25725 RVA: 0x001569E4 File Offset: 0x00154BE4
		[__DynamicallyInvokable]
		public void RemoveEventHandler(EventRegistrationToken token)
		{
			if (token.Value == 0UL)
			{
				return;
			}
			Dictionary<EventRegistrationToken, T> tokens = this.m_tokens;
			lock (tokens)
			{
				this.RemoveEventHandlerNoLock(token);
			}
		}

		// Token: 0x0600647E RID: 25726 RVA: 0x00156A30 File Offset: 0x00154C30
		[__DynamicallyInvokable]
		public void RemoveEventHandler(T handler)
		{
			if (handler == null)
			{
				return;
			}
			Dictionary<EventRegistrationToken, T> tokens = this.m_tokens;
			lock (tokens)
			{
				EventRegistrationToken preferredToken = EventRegistrationTokenTable<T>.GetPreferredToken(handler);
				T t;
				if (this.m_tokens.TryGetValue(preferredToken, out t) && t == handler)
				{
					this.RemoveEventHandlerNoLock(preferredToken);
				}
				else
				{
					foreach (KeyValuePair<EventRegistrationToken, T> keyValuePair in this.m_tokens)
					{
						if (keyValuePair.Value == (T)((object)handler))
						{
							this.RemoveEventHandlerNoLock(keyValuePair.Key);
							break;
						}
					}
				}
			}
		}

		// Token: 0x0600647F RID: 25727 RVA: 0x00156B0C File Offset: 0x00154D0C
		private void RemoveEventHandlerNoLock(EventRegistrationToken token)
		{
			T t;
			if (this.m_tokens.TryGetValue(token, out t))
			{
				this.m_tokens.Remove(token);
				Delegate @delegate = (Delegate)((object)this.m_invokeList);
				@delegate = Delegate.Remove(@delegate, (Delegate)((object)t));
				this.m_invokeList = (T)((object)@delegate);
			}
		}

		// Token: 0x06006480 RID: 25728 RVA: 0x00156B69 File Offset: 0x00154D69
		[__DynamicallyInvokable]
		public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T> refEventTable)
		{
			if (refEventTable == null)
			{
				Interlocked.CompareExchange<EventRegistrationTokenTable<T>>(ref refEventTable, new EventRegistrationTokenTable<T>(), null);
			}
			return refEventTable;
		}

		// Token: 0x04002CF1 RID: 11505
		private Dictionary<EventRegistrationToken, T> m_tokens = new Dictionary<EventRegistrationToken, T>();

		// Token: 0x04002CF2 RID: 11506
		private volatile T m_invokeList;
	}
}
