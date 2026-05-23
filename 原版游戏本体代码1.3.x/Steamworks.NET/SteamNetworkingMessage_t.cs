using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200019C RID: 412
	[Serializable]
	public struct SteamNetworkingMessage_t
	{
		// Token: 0x060009F5 RID: 2549 RVA: 0x0000D7E1 File Offset: 0x0000B9E1
		public void Release()
		{
			throw new NotImplementedException("Please use the static Release function instead which takes an IntPtr.");
		}

		// Token: 0x060009F6 RID: 2550 RVA: 0x0000D7ED File Offset: 0x0000B9ED
		public static void Release(IntPtr pointer)
		{
			NativeMethods.SteamAPI_SteamNetworkingMessage_t_Release(pointer);
		}

		// Token: 0x060009F7 RID: 2551 RVA: 0x0000D7F5 File Offset: 0x0000B9F5
		public static SteamNetworkingMessage_t FromIntPtr(IntPtr pointer)
		{
			return (SteamNetworkingMessage_t)Marshal.PtrToStructure(pointer, typeof(SteamNetworkingMessage_t));
		}

		// Token: 0x04000A3E RID: 2622
		public IntPtr m_pData;

		// Token: 0x04000A3F RID: 2623
		public int m_cbSize;

		// Token: 0x04000A40 RID: 2624
		public HSteamNetConnection m_conn;

		// Token: 0x04000A41 RID: 2625
		public SteamNetworkingIdentity m_identityPeer;

		// Token: 0x04000A42 RID: 2626
		public long m_nConnUserData;

		// Token: 0x04000A43 RID: 2627
		public SteamNetworkingMicroseconds m_usecTimeReceived;

		// Token: 0x04000A44 RID: 2628
		public long m_nMessageNumber;

		// Token: 0x04000A45 RID: 2629
		public IntPtr m_pfnFreeData;

		// Token: 0x04000A46 RID: 2630
		internal IntPtr m_pfnRelease;

		// Token: 0x04000A47 RID: 2631
		public int m_nChannel;

		// Token: 0x04000A48 RID: 2632
		public int m_nFlags;

		// Token: 0x04000A49 RID: 2633
		public long m_nUserData;

		// Token: 0x04000A4A RID: 2634
		public ushort m_idxLane;

		// Token: 0x04000A4B RID: 2635
		public ushort _pad1__;
	}
}
