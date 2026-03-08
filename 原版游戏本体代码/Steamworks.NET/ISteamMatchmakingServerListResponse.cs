using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001BA RID: 442
	public class ISteamMatchmakingServerListResponse
	{
		// Token: 0x06000AFB RID: 2811 RVA: 0x0000F0E8 File Offset: 0x0000D2E8
		public ISteamMatchmakingServerListResponse(ISteamMatchmakingServerListResponse.ServerResponded onServerResponded, ISteamMatchmakingServerListResponse.ServerFailedToRespond onServerFailedToRespond, ISteamMatchmakingServerListResponse.RefreshComplete onRefreshComplete)
		{
			if (onServerResponded == null || onServerFailedToRespond == null || onRefreshComplete == null)
			{
				throw new ArgumentNullException();
			}
			this.m_ServerResponded = onServerResponded;
			this.m_ServerFailedToRespond = onServerFailedToRespond;
			this.m_RefreshComplete = onRefreshComplete;
			this.m_VTable = new ISteamMatchmakingServerListResponse.VTable
			{
				m_VTServerResponded = new ISteamMatchmakingServerListResponse.InternalServerResponded(this.InternalOnServerResponded),
				m_VTServerFailedToRespond = new ISteamMatchmakingServerListResponse.InternalServerFailedToRespond(this.InternalOnServerFailedToRespond),
				m_VTRefreshComplete = new ISteamMatchmakingServerListResponse.InternalRefreshComplete(this.InternalOnRefreshComplete)
			};
			this.m_pVTable = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ISteamMatchmakingServerListResponse.VTable)));
			Marshal.StructureToPtr(this.m_VTable, this.m_pVTable, false);
			this.m_pGCHandle = GCHandle.Alloc(this.m_pVTable, GCHandleType.Pinned);
		}

		// Token: 0x06000AFC RID: 2812 RVA: 0x0000F1A4 File Offset: 0x0000D3A4
		~ISteamMatchmakingServerListResponse()
		{
			if (this.m_pVTable != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(this.m_pVTable);
			}
			if (this.m_pGCHandle.IsAllocated)
			{
				this.m_pGCHandle.Free();
			}
		}

		// Token: 0x06000AFD RID: 2813 RVA: 0x0000F200 File Offset: 0x0000D400
		private void InternalOnServerResponded(IntPtr thisptr, HServerListRequest hRequest, int iServer)
		{
			try
			{
				this.m_ServerResponded(hRequest, iServer);
			}
			catch (Exception e)
			{
				CallbackDispatcher.ExceptionHandler(e);
			}
		}

		// Token: 0x06000AFE RID: 2814 RVA: 0x0000F234 File Offset: 0x0000D434
		private void InternalOnServerFailedToRespond(IntPtr thisptr, HServerListRequest hRequest, int iServer)
		{
			try
			{
				this.m_ServerFailedToRespond(hRequest, iServer);
			}
			catch (Exception e)
			{
				CallbackDispatcher.ExceptionHandler(e);
			}
		}

		// Token: 0x06000AFF RID: 2815 RVA: 0x0000F268 File Offset: 0x0000D468
		private void InternalOnRefreshComplete(IntPtr thisptr, HServerListRequest hRequest, EMatchMakingServerResponse response)
		{
			try
			{
				this.m_RefreshComplete(hRequest, response);
			}
			catch (Exception e)
			{
				CallbackDispatcher.ExceptionHandler(e);
			}
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x0000F29C File Offset: 0x0000D49C
		public static explicit operator IntPtr(ISteamMatchmakingServerListResponse that)
		{
			return that.m_pGCHandle.AddrOfPinnedObject();
		}

		// Token: 0x04000A7C RID: 2684
		private ISteamMatchmakingServerListResponse.VTable m_VTable;

		// Token: 0x04000A7D RID: 2685
		private IntPtr m_pVTable;

		// Token: 0x04000A7E RID: 2686
		private GCHandle m_pGCHandle;

		// Token: 0x04000A7F RID: 2687
		private ISteamMatchmakingServerListResponse.ServerResponded m_ServerResponded;

		// Token: 0x04000A80 RID: 2688
		private ISteamMatchmakingServerListResponse.ServerFailedToRespond m_ServerFailedToRespond;

		// Token: 0x04000A81 RID: 2689
		private ISteamMatchmakingServerListResponse.RefreshComplete m_RefreshComplete;

		// Token: 0x020001CF RID: 463
		// (Invoke) Token: 0x06000B65 RID: 2917
		public delegate void ServerResponded(HServerListRequest hRequest, int iServer);

		// Token: 0x020001D0 RID: 464
		// (Invoke) Token: 0x06000B69 RID: 2921
		public delegate void ServerFailedToRespond(HServerListRequest hRequest, int iServer);

		// Token: 0x020001D1 RID: 465
		// (Invoke) Token: 0x06000B6D RID: 2925
		public delegate void RefreshComplete(HServerListRequest hRequest, EMatchMakingServerResponse response);

		// Token: 0x020001D2 RID: 466
		// (Invoke) Token: 0x06000B71 RID: 2929
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		private delegate void InternalServerResponded(IntPtr thisptr, HServerListRequest hRequest, int iServer);

		// Token: 0x020001D3 RID: 467
		// (Invoke) Token: 0x06000B75 RID: 2933
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		private delegate void InternalServerFailedToRespond(IntPtr thisptr, HServerListRequest hRequest, int iServer);

		// Token: 0x020001D4 RID: 468
		// (Invoke) Token: 0x06000B79 RID: 2937
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		private delegate void InternalRefreshComplete(IntPtr thisptr, HServerListRequest hRequest, EMatchMakingServerResponse response);

		// Token: 0x020001D5 RID: 469
		[StructLayout(LayoutKind.Sequential)]
		private class VTable
		{
			// Token: 0x04000AD6 RID: 2774
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingServerListResponse.InternalServerResponded m_VTServerResponded;

			// Token: 0x04000AD7 RID: 2775
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingServerListResponse.InternalServerFailedToRespond m_VTServerFailedToRespond;

			// Token: 0x04000AD8 RID: 2776
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingServerListResponse.InternalRefreshComplete m_VTRefreshComplete;
		}
	}
}
