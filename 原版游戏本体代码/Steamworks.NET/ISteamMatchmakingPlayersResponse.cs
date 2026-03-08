using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001BC RID: 444
	public class ISteamMatchmakingPlayersResponse
	{
		// Token: 0x06000B06 RID: 2822 RVA: 0x0000F3D0 File Offset: 0x0000D5D0
		public ISteamMatchmakingPlayersResponse(ISteamMatchmakingPlayersResponse.AddPlayerToList onAddPlayerToList, ISteamMatchmakingPlayersResponse.PlayersFailedToRespond onPlayersFailedToRespond, ISteamMatchmakingPlayersResponse.PlayersRefreshComplete onPlayersRefreshComplete)
		{
			if (onAddPlayerToList == null || onPlayersFailedToRespond == null || onPlayersRefreshComplete == null)
			{
				throw new ArgumentNullException();
			}
			this.m_AddPlayerToList = onAddPlayerToList;
			this.m_PlayersFailedToRespond = onPlayersFailedToRespond;
			this.m_PlayersRefreshComplete = onPlayersRefreshComplete;
			this.m_VTable = new ISteamMatchmakingPlayersResponse.VTable
			{
				m_VTAddPlayerToList = new ISteamMatchmakingPlayersResponse.InternalAddPlayerToList(this.InternalOnAddPlayerToList),
				m_VTPlayersFailedToRespond = new ISteamMatchmakingPlayersResponse.InternalPlayersFailedToRespond(this.InternalOnPlayersFailedToRespond),
				m_VTPlayersRefreshComplete = new ISteamMatchmakingPlayersResponse.InternalPlayersRefreshComplete(this.InternalOnPlayersRefreshComplete)
			};
			this.m_pVTable = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ISteamMatchmakingPlayersResponse.VTable)));
			Marshal.StructureToPtr(this.m_VTable, this.m_pVTable, false);
			this.m_pGCHandle = GCHandle.Alloc(this.m_pVTable, GCHandleType.Pinned);
		}

		// Token: 0x06000B07 RID: 2823 RVA: 0x0000F48C File Offset: 0x0000D68C
		~ISteamMatchmakingPlayersResponse()
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

		// Token: 0x06000B08 RID: 2824 RVA: 0x0000F4E8 File Offset: 0x0000D6E8
		private void InternalOnAddPlayerToList(IntPtr thisptr, IntPtr pchName, int nScore, float flTimePlayed)
		{
			this.m_AddPlayerToList(InteropHelp.PtrToStringUTF8(pchName), nScore, flTimePlayed);
		}

		// Token: 0x06000B09 RID: 2825 RVA: 0x0000F4FE File Offset: 0x0000D6FE
		private void InternalOnPlayersFailedToRespond(IntPtr thisptr)
		{
			this.m_PlayersFailedToRespond();
		}

		// Token: 0x06000B0A RID: 2826 RVA: 0x0000F50B File Offset: 0x0000D70B
		private void InternalOnPlayersRefreshComplete(IntPtr thisptr)
		{
			this.m_PlayersRefreshComplete();
		}

		// Token: 0x06000B0B RID: 2827 RVA: 0x0000F518 File Offset: 0x0000D718
		public static explicit operator IntPtr(ISteamMatchmakingPlayersResponse that)
		{
			return that.m_pGCHandle.AddrOfPinnedObject();
		}

		// Token: 0x04000A87 RID: 2695
		private ISteamMatchmakingPlayersResponse.VTable m_VTable;

		// Token: 0x04000A88 RID: 2696
		private IntPtr m_pVTable;

		// Token: 0x04000A89 RID: 2697
		private GCHandle m_pGCHandle;

		// Token: 0x04000A8A RID: 2698
		private ISteamMatchmakingPlayersResponse.AddPlayerToList m_AddPlayerToList;

		// Token: 0x04000A8B RID: 2699
		private ISteamMatchmakingPlayersResponse.PlayersFailedToRespond m_PlayersFailedToRespond;

		// Token: 0x04000A8C RID: 2700
		private ISteamMatchmakingPlayersResponse.PlayersRefreshComplete m_PlayersRefreshComplete;

		// Token: 0x020001DB RID: 475
		// (Invoke) Token: 0x06000B8F RID: 2959
		public delegate void AddPlayerToList(string pchName, int nScore, float flTimePlayed);

		// Token: 0x020001DC RID: 476
		// (Invoke) Token: 0x06000B93 RID: 2963
		public delegate void PlayersFailedToRespond();

		// Token: 0x020001DD RID: 477
		// (Invoke) Token: 0x06000B97 RID: 2967
		public delegate void PlayersRefreshComplete();

		// Token: 0x020001DE RID: 478
		// (Invoke) Token: 0x06000B9B RID: 2971
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void InternalAddPlayerToList(IntPtr thisptr, IntPtr pchName, int nScore, float flTimePlayed);

		// Token: 0x020001DF RID: 479
		// (Invoke) Token: 0x06000B9F RID: 2975
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void InternalPlayersFailedToRespond(IntPtr thisptr);

		// Token: 0x020001E0 RID: 480
		// (Invoke) Token: 0x06000BA3 RID: 2979
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void InternalPlayersRefreshComplete(IntPtr thisptr);

		// Token: 0x020001E1 RID: 481
		[StructLayout(LayoutKind.Sequential)]
		private class VTable
		{
			// Token: 0x04000ADB RID: 2779
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingPlayersResponse.InternalAddPlayerToList m_VTAddPlayerToList;

			// Token: 0x04000ADC RID: 2780
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingPlayersResponse.InternalPlayersFailedToRespond m_VTPlayersFailedToRespond;

			// Token: 0x04000ADD RID: 2781
			[NonSerialized]
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public ISteamMatchmakingPlayersResponse.InternalPlayersRefreshComplete m_VTPlayersRefreshComplete;
		}
	}
}
