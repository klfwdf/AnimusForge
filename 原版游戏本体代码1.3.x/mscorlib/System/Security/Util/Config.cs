using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace System.Security.Util
{
	// Token: 0x0200037A RID: 890
	internal static class Config
	{
		// Token: 0x06002C1C RID: 11292 RVA: 0x000A44C4 File Offset: 0x000A26C4
		[SecurityCritical]
		private static void GetFileLocales()
		{
			if (Config.m_machineConfig == null)
			{
				string machineConfig = null;
				Config.GetMachineDirectory(JitHelpers.GetStringHandleOnStack(ref machineConfig));
				Config.m_machineConfig = machineConfig;
			}
			if (Config.m_userConfig == null)
			{
				string userConfig = null;
				Config.GetUserDirectory(JitHelpers.GetStringHandleOnStack(ref userConfig));
				Config.m_userConfig = userConfig;
			}
		}

		// Token: 0x170005DE RID: 1502
		// (get) Token: 0x06002C1D RID: 11293 RVA: 0x000A450F File Offset: 0x000A270F
		internal static string MachineDirectory
		{
			[SecurityCritical]
			get
			{
				Config.GetFileLocales();
				return Config.m_machineConfig;
			}
		}

		// Token: 0x170005DF RID: 1503
		// (get) Token: 0x06002C1E RID: 11294 RVA: 0x000A451D File Offset: 0x000A271D
		internal static string UserDirectory
		{
			[SecurityCritical]
			get
			{
				Config.GetFileLocales();
				return Config.m_userConfig;
			}
		}

		// Token: 0x06002C1F RID: 11295
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern int SaveDataByte(string path, [In] byte[] data, int length);

		// Token: 0x06002C20 RID: 11296
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern bool RecoverData(ConfigId id);

		// Token: 0x06002C21 RID: 11297
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void SetQuickCache(ConfigId id, QuickCacheEntryType quickCacheFlags);

		// Token: 0x06002C22 RID: 11298
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern bool GetCacheEntry(ConfigId id, int numKey, [In] byte[] key, int keyLength, ObjectHandleOnStack retData);

		// Token: 0x06002C23 RID: 11299 RVA: 0x000A452C File Offset: 0x000A272C
		[SecurityCritical]
		internal static bool GetCacheEntry(ConfigId id, int numKey, byte[] key, out byte[] data)
		{
			byte[] array = null;
			bool cacheEntry = Config.GetCacheEntry(id, numKey, key, key.Length, JitHelpers.GetObjectHandleOnStack<byte[]>(ref array));
			data = array;
			return cacheEntry;
		}

		// Token: 0x06002C24 RID: 11300
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddCacheEntry(ConfigId id, int numKey, [In] byte[] key, int keyLength, byte[] data, int dataLength);

		// Token: 0x06002C25 RID: 11301 RVA: 0x000A4552 File Offset: 0x000A2752
		[SecurityCritical]
		internal static void AddCacheEntry(ConfigId id, int numKey, byte[] key, byte[] data)
		{
			Config.AddCacheEntry(id, numKey, key, key.Length, data, data.Length);
		}

		// Token: 0x06002C26 RID: 11302
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void ResetCacheData(ConfigId id);

		// Token: 0x06002C27 RID: 11303
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetMachineDirectory(StringHandleOnStack retDirectory);

		// Token: 0x06002C28 RID: 11304
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetUserDirectory(StringHandleOnStack retDirectory);

		// Token: 0x06002C29 RID: 11305
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern bool WriteToEventLog(string message);

		// Token: 0x040011C4 RID: 4548
		private static volatile string m_machineConfig;

		// Token: 0x040011C5 RID: 4549
		private static volatile string m_userConfig;
	}
}
