using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001BD RID: 445
	public class ISteamMatchmakingRulesResponse
	{
		// Token: 0x06000B0C RID: 2828 RVA: 0x0000F528 File Offset: 0x0000D728
		public ISteamMatchmakingRulesResponse(ISteamMatchmakingRulesResponse.RulesResponded onRulesResponded, ISteamMatchmakingRulesResponse.RulesFailedToRespond onRulesFailedToRespond, ISteamMatchmakingRulesResponse.RulesRefreshComplete onRulesRefreshComplete)
		{
			if (onRulesResponded == null || onRulesFailedToRespond == null || onRulesRefreshComplete == null)
			{
				throw new ArgumentNullException();
			}
			this.m_RulesResponded = onRulesResponded;
			this.m_RulesFailedToRespond = onRulesFailedToRespond;
			this.m_RulesRefreshComplete = onRulesRefreshComplete;
			this.m_VTable = new ISteamMatchmakingRulesResponse.VTable
			{
				m_VTRulesResponded = new ISteamMatchmakingRulesResponse.InternalRulesResponded(this.InternalOnRulesResponded),
				m_VTRulesFailedToRespond = new ISteamMatchmakingRulesResponse.InternalRulesFailedToRespond(this.InternalOnRulesFailedToRespond),
				m_VTRulesRefreshComplete = new ISteamMatchmakingRulesResponse.InternalRulesRefreshComplete(this.InternalOnRulesRefreshComplete)
			};
			this.m_pVTable = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ISteamMatchmakingRulesResponse.VTable)));
			Marshal.StructureToPtr(this.m_VTable, this.m_pVTable, false);
			this.m_pGCHandle = GCHandle.Alloc(this.m_pVTable, GCHandleType.Pinned);
		}

		// Token: 0x06000B0D RID: 2829 RVA: 0x0000F5E4 File Offset: 0x0000D7E4
		~ISteamMatchmakingRulesResponse()
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

		// Token: 0x06000B0E RID: 2830 RVA: 0x0000F640 File Offset: 0x0000D840
		private void InternalOnRulesResponded(IntPtr thisptr, IntPtr pchRule, IntPtr pchValue)
		{
			this.m_RulesResponded(InteropHelp.PtrToStringUTF8(pchRule), InteropHelp.PtrToStringUTF8(pchValue));
		}

		// Token: 0x06000B0F RID: 2831 RVA: 0x0000F659 File Offset: 0x0000D859
		private void InternalOnRulesFailedToRespond(IntPtr thisptr)
		{
			this.m_RulesFailedToRespond();
		}

		// Token: 0x06000B10 RID: 2832 RVA: 0x0000F666 File Offset: 0x0000D866
		private void InternalOnRulesRefreshComplete(IntPtr thisptr)
		{
			this.m_RulesRefreshComplete();
		}

		// Token: 0x06000B11 RID: 2833 RVA: 0x0000F673 File Offset: 0x0000D873
		public static explicit operator IntPtr(ISteamMatchmakingRulesResponse that)
		{
			return that.m_pGCHandle.AddrOfPinnedObject();
		}

		// Token: 0x04000A8D RID: 2701
		private ISteamMatchmakingRulesResponse.VTable m_VTable;

		// Token: 0x04000A8E RID: 2702
		private IntPtr m_pVTable;

		// Token: 0x04000A8F RID: 2703
		private GCHandle m_pGCHandle;

		// Token: 0x04000A90 RID: 2704
		private ISteamMatchmakingRulesResponse.RulesResponded m_RulesResponded;

		// Token: 0x04000A91 RID: 2705
		private ISteamMatchmakingRulesResponse.RulesFailedToRespond m_RulesFailedToRespond;

		// Token: 0x04000A92 RID: 2706
		private ISteamMatchmakingRulesResponse.RulesRefreshComplete m_RulesRefreshComplete;

		// Token: 0x020001E2 RID: 482
		// (Invoke) Token: 0x06000BA8 RID: 2984
		public delegate void RulesResponded(string pchRule, string pchValue);

		// Token: 0x020001E3 RID: 483
		// (Invoke) Token: 0x06000BAC RID: 2988
		public delegate void RulesFailedToRespond();

		// Token: 0x020001E4 RID: 484
		// (Invoke) Token: 0x06000BB0 RID: 2992
		public delegate void RulesRefreshComplete();

		// Token: 0x020001E5 RID: 485
		// (Invoke) Token: 0x06000BB4 RID: 2996
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void InternalRulesResponded(IntPtr thisptr, IntPtr pchRule, IntPtr pchValue);

		// Token: 0x020001E6 RID: 486
		// (Invoke) Token: 0x06000BB8 RID: 3000
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void InternalRulesFailedToRespond(IntPtr thisptr);

		// Token: 0x020001E7 RID: 487
		// (Invoke) Token: 0x06000BBC RID: 3004
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void InternalRulesRefreshComplete(IntPtr thisptr);

		// Token: 0x020001E8 RID: 488
		[StructLayout(LayoutKind.Sequential)]
		private class VTable
		{
			// Token: 0x04000ADE RID: 2782
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingRulesResponse.InternalRulesResponded m_VTRulesResponded;

			// Token: 0x04000ADF RID: 2783
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingRulesResponse.InternalRulesFailedToRespond m_VTRulesFailedToRespond;

			// Token: 0x04000AE0 RID: 2784
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingRulesResponse.InternalRulesRefreshComplete m_VTRulesRefreshComplete;
		}
	}
}
