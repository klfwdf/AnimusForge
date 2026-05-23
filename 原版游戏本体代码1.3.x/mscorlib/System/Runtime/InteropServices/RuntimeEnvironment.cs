using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000954 RID: 2388
	[ComVisible(true)]
	public class RuntimeEnvironment
	{
		// Token: 0x060061B1 RID: 25009 RVA: 0x0014E638 File Offset: 0x0014C838
		[Obsolete("Do not create instances of the RuntimeEnvironment class.  Call the static methods directly on this type instead", true)]
		public RuntimeEnvironment()
		{
		}

		// Token: 0x060061B2 RID: 25010
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetModuleFileName();

		// Token: 0x060061B3 RID: 25011
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetDeveloperPath();

		// Token: 0x060061B4 RID: 25012
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetHostBindingFile();

		// Token: 0x060061B5 RID: 25013
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void _GetSystemVersion(StringHandleOnStack retVer);

		// Token: 0x060061B6 RID: 25014 RVA: 0x0014E640 File Offset: 0x0014C840
		public static bool FromGlobalAccessCache(Assembly a)
		{
			return a.GlobalAssemblyCache;
		}

		// Token: 0x060061B7 RID: 25015 RVA: 0x0014E648 File Offset: 0x0014C848
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static string GetSystemVersion()
		{
			string result = null;
			RuntimeEnvironment._GetSystemVersion(JitHelpers.GetStringHandleOnStack(ref result));
			return result;
		}

		// Token: 0x060061B8 RID: 25016 RVA: 0x0014E664 File Offset: 0x0014C864
		[SecuritySafeCritical]
		public static string GetRuntimeDirectory()
		{
			string runtimeDirectoryImpl = RuntimeEnvironment.GetRuntimeDirectoryImpl();
			new FileIOPermission(FileIOPermissionAccess.PathDiscovery, runtimeDirectoryImpl).Demand();
			return runtimeDirectoryImpl;
		}

		// Token: 0x060061B9 RID: 25017
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetRuntimeDirectoryImpl();

		// Token: 0x17001103 RID: 4355
		// (get) Token: 0x060061BA RID: 25018 RVA: 0x0014E684 File Offset: 0x0014C884
		public static string SystemConfigurationFile
		{
			[SecuritySafeCritical]
			get
			{
				StringBuilder stringBuilder = new StringBuilder(260);
				stringBuilder.Append(RuntimeEnvironment.GetRuntimeDirectory());
				stringBuilder.Append(AppDomainSetup.RuntimeConfigurationFile);
				string text = stringBuilder.ToString();
				new FileIOPermission(FileIOPermissionAccess.PathDiscovery, text).Demand();
				return text;
			}
		}

		// Token: 0x060061BB RID: 25019
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern IntPtr GetRuntimeInterfaceImpl([MarshalAs(UnmanagedType.LPStruct)] [In] Guid clsid, [MarshalAs(UnmanagedType.LPStruct)] [In] Guid riid);

		// Token: 0x060061BC RID: 25020 RVA: 0x0014E6C8 File Offset: 0x0014C8C8
		[SecurityCritical]
		[ComVisible(false)]
		public static IntPtr GetRuntimeInterfaceAsIntPtr(Guid clsid, Guid riid)
		{
			return RuntimeEnvironment.GetRuntimeInterfaceImpl(clsid, riid);
		}

		// Token: 0x060061BD RID: 25021 RVA: 0x0014E6D4 File Offset: 0x0014C8D4
		[SecurityCritical]
		[ComVisible(false)]
		public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid)
		{
			IntPtr intPtr = IntPtr.Zero;
			object objectForIUnknown;
			try
			{
				intPtr = RuntimeEnvironment.GetRuntimeInterfaceImpl(clsid, riid);
				objectForIUnknown = Marshal.GetObjectForIUnknown(intPtr);
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.Release(intPtr);
				}
			}
			return objectForIUnknown;
		}
	}
}
