using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001B8 RID: 440
	public class MMKVPMarshaller
	{
		// Token: 0x06000AF6 RID: 2806 RVA: 0x0000EFE0 File Offset: 0x0000D1E0
		public MMKVPMarshaller(MatchMakingKeyValuePair_t[] filters)
		{
			if (filters == null)
			{
				return;
			}
			int num = Marshal.SizeOf(typeof(MatchMakingKeyValuePair_t));
			this.m_pNativeArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * filters.Length);
			this.m_pArrayEntries = Marshal.AllocHGlobal(num * filters.Length);
			for (int i = 0; i < filters.Length; i++)
			{
				Marshal.StructureToPtr(filters[i], new IntPtr(this.m_pArrayEntries.ToInt64() + (long)(i * num)), false);
			}
			Marshal.WriteIntPtr(this.m_pNativeArray, this.m_pArrayEntries);
		}

		// Token: 0x06000AF7 RID: 2807 RVA: 0x0000F07C File Offset: 0x0000D27C
		~MMKVPMarshaller()
		{
			if (this.m_pArrayEntries != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(this.m_pArrayEntries);
			}
			if (this.m_pNativeArray != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(this.m_pNativeArray);
			}
		}

		// Token: 0x06000AF8 RID: 2808 RVA: 0x0000F0DC File Offset: 0x0000D2DC
		public static implicit operator IntPtr(MMKVPMarshaller that)
		{
			return that.m_pNativeArray;
		}

		// Token: 0x04000A7A RID: 2682
		private IntPtr m_pNativeArray;

		// Token: 0x04000A7B RID: 2683
		private IntPtr m_pArrayEntries;
	}
}
