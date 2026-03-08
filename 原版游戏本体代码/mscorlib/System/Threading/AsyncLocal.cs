using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004E5 RID: 1253
	[__DynamicallyInvokable]
	public sealed class AsyncLocal<T> : IAsyncLocal
	{
		// Token: 0x06003B78 RID: 15224 RVA: 0x000E24C3 File Offset: 0x000E06C3
		[__DynamicallyInvokable]
		public AsyncLocal()
		{
		}

		// Token: 0x06003B79 RID: 15225 RVA: 0x000E24CB File Offset: 0x000E06CB
		[SecurityCritical]
		[__DynamicallyInvokable]
		public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>> valueChangedHandler)
		{
			this.m_valueChangedHandler = valueChangedHandler;
		}

		// Token: 0x17000901 RID: 2305
		// (get) Token: 0x06003B7A RID: 15226 RVA: 0x000E24DC File Offset: 0x000E06DC
		// (set) Token: 0x06003B7B RID: 15227 RVA: 0x000E2503 File Offset: 0x000E0703
		[__DynamicallyInvokable]
		public T Value
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				object localValue = ExecutionContext.GetLocalValue(this);
				if (localValue != null)
				{
					return (T)((object)localValue);
				}
				return default(T);
			}
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			set
			{
				ExecutionContext.SetLocalValue(this, value, this.m_valueChangedHandler != null);
			}
		}

		// Token: 0x06003B7C RID: 15228 RVA: 0x000E251C File Offset: 0x000E071C
		[SecurityCritical]
		void IAsyncLocal.OnValueChanged(object previousValueObj, object currentValueObj, bool contextChanged)
		{
			T previousValue = ((previousValueObj == null) ? default(T) : ((T)((object)previousValueObj)));
			T currentValue = ((currentValueObj == null) ? default(T) : ((T)((object)currentValueObj)));
			this.m_valueChangedHandler(new AsyncLocalValueChangedArgs<T>(previousValue, currentValue, contextChanged));
		}

		// Token: 0x04001967 RID: 6503
		[SecurityCritical]
		private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;
	}
}
