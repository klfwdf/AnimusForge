using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001B2 RID: 434
	public sealed class Callback<T> : Callback, IDisposable
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000ACC RID: 2764 RVA: 0x0000EAE8 File Offset: 0x0000CCE8
		// (remove) Token: 0x06000ACD RID: 2765 RVA: 0x0000EB20 File Offset: 0x0000CD20
		private event Callback<T>.DispatchDelegate m_Func;

		// Token: 0x06000ACE RID: 2766 RVA: 0x0000EB55 File Offset: 0x0000CD55
		public static Callback<T> Create(Callback<T>.DispatchDelegate func)
		{
			return new Callback<T>(func, false);
		}

		// Token: 0x06000ACF RID: 2767 RVA: 0x0000EB5E File Offset: 0x0000CD5E
		public static Callback<T> CreateGameServer(Callback<T>.DispatchDelegate func)
		{
			return new Callback<T>(func, true);
		}

		// Token: 0x06000AD0 RID: 2768 RVA: 0x0000EB67 File Offset: 0x0000CD67
		public Callback(Callback<T>.DispatchDelegate func, bool bGameServer = false)
		{
			this.m_bGameServer = bGameServer;
			this.Register(func);
		}

		// Token: 0x06000AD1 RID: 2769 RVA: 0x0000EB80 File Offset: 0x0000CD80
		~Callback()
		{
			this.Dispose();
		}

		// Token: 0x06000AD2 RID: 2770 RVA: 0x0000EBAC File Offset: 0x0000CDAC
		public void Dispose()
		{
			if (this.m_bDisposed)
			{
				return;
			}
			GC.SuppressFinalize(this);
			if (this.m_bIsRegistered)
			{
				this.Unregister();
			}
			this.m_bDisposed = true;
		}

		// Token: 0x06000AD3 RID: 2771 RVA: 0x0000EBD2 File Offset: 0x0000CDD2
		public void Register(Callback<T>.DispatchDelegate func)
		{
			if (func == null)
			{
				throw new Exception("Callback function must not be null.");
			}
			if (this.m_bIsRegistered)
			{
				this.Unregister();
			}
			this.m_Func = func;
			CallbackDispatcher.Register(this);
			this.m_bIsRegistered = true;
		}

		// Token: 0x06000AD4 RID: 2772 RVA: 0x0000EC04 File Offset: 0x0000CE04
		public void Unregister()
		{
			CallbackDispatcher.Unregister(this);
			this.m_bIsRegistered = false;
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000AD5 RID: 2773 RVA: 0x0000EC13 File Offset: 0x0000CE13
		public override bool IsGameServer
		{
			get
			{
				return this.m_bGameServer;
			}
		}

		// Token: 0x06000AD6 RID: 2774 RVA: 0x0000EC1B File Offset: 0x0000CE1B
		internal override Type GetCallbackType()
		{
			return typeof(T);
		}

		// Token: 0x06000AD7 RID: 2775 RVA: 0x0000EC28 File Offset: 0x0000CE28
		internal override void OnRunCallback(IntPtr pvParam)
		{
			try
			{
				this.m_Func((T)((object)Marshal.PtrToStructure(pvParam, typeof(T))));
			}
			catch (Exception e)
			{
				CallbackDispatcher.ExceptionHandler(e);
			}
		}

		// Token: 0x06000AD8 RID: 2776 RVA: 0x0000EC70 File Offset: 0x0000CE70
		internal override void SetUnregistered()
		{
			this.m_bIsRegistered = false;
		}

		// Token: 0x04000A73 RID: 2675
		private bool m_bGameServer;

		// Token: 0x04000A74 RID: 2676
		private bool m_bIsRegistered;

		// Token: 0x04000A75 RID: 2677
		private bool m_bDisposed;

		// Token: 0x020001CB RID: 459
		// (Invoke) Token: 0x06000B58 RID: 2904
		public delegate void DispatchDelegate(T param);
	}
}
