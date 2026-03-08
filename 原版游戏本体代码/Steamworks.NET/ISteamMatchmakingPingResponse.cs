using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001BB RID: 443
	public class ISteamMatchmakingPingResponse
	{
		// Token: 0x06000B01 RID: 2817 RVA: 0x0000F2AC File Offset: 0x0000D4AC
		public ISteamMatchmakingPingResponse(ISteamMatchmakingPingResponse.ServerResponded onServerResponded, ISteamMatchmakingPingResponse.ServerFailedToRespond onServerFailedToRespond)
		{
			if (onServerResponded == null || onServerFailedToRespond == null)
			{
				throw new ArgumentNullException();
			}
			this.m_ServerResponded = onServerResponded;
			this.m_ServerFailedToRespond = onServerFailedToRespond;
			this.m_VTable = new ISteamMatchmakingPingResponse.VTable
			{
				m_VTServerResponded = new ISteamMatchmakingPingResponse.InternalServerResponded(this.InternalOnServerResponded),
				m_VTServerFailedToRespond = new ISteamMatchmakingPingResponse.InternalServerFailedToRespond(this.InternalOnServerFailedToRespond)
			};
			this.m_pVTable = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ISteamMatchmakingPingResponse.VTable)));
			Marshal.StructureToPtr(this.m_VTable, this.m_pVTable, false);
			this.m_pGCHandle = GCHandle.Alloc(this.m_pVTable, GCHandleType.Pinned);
		}

		// Token: 0x06000B02 RID: 2818 RVA: 0x0000F34C File Offset: 0x0000D54C
		~ISteamMatchmakingPingResponse()
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

		// Token: 0x06000B03 RID: 2819 RVA: 0x0000F3A8 File Offset: 0x0000D5A8
		private void InternalOnServerResponded(IntPtr thisptr, gameserveritem_t server)
		{
			this.m_ServerResponded(server);
		}

		// Token: 0x06000B04 RID: 2820 RVA: 0x0000F3B6 File Offset: 0x0000D5B6
		private void InternalOnServerFailedToRespond(IntPtr thisptr)
		{
			this.m_ServerFailedToRespond();
		}

		// Token: 0x06000B05 RID: 2821 RVA: 0x0000F3C3 File Offset: 0x0000D5C3
		public static explicit operator IntPtr(ISteamMatchmakingPingResponse that)
		{
			return that.m_pGCHandle.AddrOfPinnedObject();
		}

		// Token: 0x04000A82 RID: 2690
		private ISteamMatchmakingPingResponse.VTable m_VTable;

		// Token: 0x04000A83 RID: 2691
		private IntPtr m_pVTable;

		// Token: 0x04000A84 RID: 2692
		private GCHandle m_pGCHandle;

		// Token: 0x04000A85 RID: 2693
		private ISteamMatchmakingPingResponse.ServerResponded m_ServerResponded;

		// Token: 0x04000A86 RID: 2694
		private ISteamMatchmakingPingResponse.ServerFailedToRespond m_ServerFailedToRespond;

		// Token: 0x020001D6 RID: 470
		// (Invoke) Token: 0x06000B7E RID: 2942
		public delegate void ServerResponded(gameserveritem_t server);

		// Token: 0x020001D7 RID: 471
		// (Invoke) Token: 0x06000B82 RID: 2946
		public delegate void ServerFailedToRespond();

		// Token: 0x020001D8 RID: 472
		// (Invoke) Token: 0x06000B86 RID: 2950
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		private delegate void InternalServerResponded(IntPtr thisptr, gameserveritem_t server);

		// Token: 0x020001D9 RID: 473
		// (Invoke) Token: 0x06000B8A RID: 2954
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		private delegate void InternalServerFailedToRespond(IntPtr thisptr);

		// Token: 0x020001DA RID: 474
		[StructLayout(LayoutKind.Sequential)]
		private class VTable
		{
			// Token: 0x04000AD9 RID: 2777
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingPingResponse.InternalServerResponded m_VTServerResponded;

			// Token: 0x04000ADA RID: 2778
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingPingResponse.InternalServerFailedToRespond m_VTServerFailedToRespond;
		}
	}
}
