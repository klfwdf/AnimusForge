using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001B4 RID: 436
	public sealed class CallResult<T> : CallResult, IDisposable
	{
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000ADD RID: 2781 RVA: 0x0000EC7C File Offset: 0x0000CE7C
		// (remove) Token: 0x06000ADE RID: 2782 RVA: 0x0000ECB4 File Offset: 0x0000CEB4
		private event CallResult<T>.APIDispatchDelegate m_Func;

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000ADF RID: 2783 RVA: 0x0000ECE9 File Offset: 0x0000CEE9
		public SteamAPICall_t Handle
		{
			get
			{
				return this.m_hAPICall;
			}
		}

		// Token: 0x06000AE0 RID: 2784 RVA: 0x0000ECF1 File Offset: 0x0000CEF1
		public static CallResult<T> Create(CallResult<T>.APIDispatchDelegate func = null)
		{
			return new CallResult<T>(func);
		}

		// Token: 0x06000AE1 RID: 2785 RVA: 0x0000ECF9 File Offset: 0x0000CEF9
		public CallResult(CallResult<T>.APIDispatchDelegate func = null)
		{
			this.m_Func = func;
		}

		// Token: 0x06000AE2 RID: 2786 RVA: 0x0000ED14 File Offset: 0x0000CF14
		~CallResult()
		{
			this.Dispose();
		}

		// Token: 0x06000AE3 RID: 2787 RVA: 0x0000ED40 File Offset: 0x0000CF40
		public void Dispose()
		{
			if (this.m_bDisposed)
			{
				return;
			}
			GC.SuppressFinalize(this);
			this.Cancel();
			this.m_bDisposed = true;
		}

		// Token: 0x06000AE4 RID: 2788 RVA: 0x0000ED60 File Offset: 0x0000CF60
		public void Set(SteamAPICall_t hAPICall, CallResult<T>.APIDispatchDelegate func = null)
		{
			if (func != null)
			{
				this.m_Func = func;
			}
			if (this.m_Func == null)
			{
				throw new Exception("CallResult function was null, you must either set it in the CallResult Constructor or via Set()");
			}
			if (this.m_hAPICall != SteamAPICall_t.Invalid)
			{
				CallbackDispatcher.Unregister(this.m_hAPICall, this);
			}
			this.m_hAPICall = hAPICall;
			if (hAPICall != SteamAPICall_t.Invalid)
			{
				CallbackDispatcher.Register(hAPICall, this);
			}
		}

		// Token: 0x06000AE5 RID: 2789 RVA: 0x0000EDC3 File Offset: 0x0000CFC3
		public bool IsActive()
		{
			return this.m_hAPICall != SteamAPICall_t.Invalid;
		}

		// Token: 0x06000AE6 RID: 2790 RVA: 0x0000EDD5 File Offset: 0x0000CFD5
		public void Cancel()
		{
			if (this.IsActive())
			{
				CallbackDispatcher.Unregister(this.m_hAPICall, this);
			}
		}

		// Token: 0x06000AE7 RID: 2791 RVA: 0x0000EDEB File Offset: 0x0000CFEB
		internal override Type GetCallbackType()
		{
			return typeof(T);
		}

		// Token: 0x06000AE8 RID: 2792 RVA: 0x0000EDF8 File Offset: 0x0000CFF8
		internal override void OnRunCallResult(IntPtr pvParam, bool bFailed, ulong hSteamAPICall_)
		{
			if ((SteamAPICall_t)hSteamAPICall_ == this.m_hAPICall)
			{
				try
				{
					this.m_Func((T)((object)Marshal.PtrToStructure(pvParam, typeof(T))), bFailed);
				}
				catch (Exception e)
				{
					CallbackDispatcher.ExceptionHandler(e);
				}
			}
		}

		// Token: 0x06000AE9 RID: 2793 RVA: 0x0000EE54 File Offset: 0x0000D054
		internal override void SetUnregistered()
		{
			this.m_hAPICall = SteamAPICall_t.Invalid;
		}

		// Token: 0x04000A77 RID: 2679
		private SteamAPICall_t m_hAPICall = SteamAPICall_t.Invalid;

		// Token: 0x04000A78 RID: 2680
		private bool m_bDisposed;

		// Token: 0x020001CC RID: 460
		// (Invoke) Token: 0x06000B5C RID: 2908
		public delegate void APIDispatchDelegate(T param, bool bIOFailure);
	}
}
