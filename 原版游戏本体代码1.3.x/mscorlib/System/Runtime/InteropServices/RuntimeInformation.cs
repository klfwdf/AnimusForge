using System;
using System.Reflection;
using System.Security;
using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009AB RID: 2475
	public static class RuntimeInformation
	{
		// Token: 0x1700111B RID: 4379
		// (get) Token: 0x06006308 RID: 25352 RVA: 0x00151930 File Offset: 0x0014FB30
		public static string FrameworkDescription
		{
			get
			{
				if (RuntimeInformation.s_frameworkDescription == null)
				{
					AssemblyFileVersionAttribute assemblyFileVersionAttribute = (AssemblyFileVersionAttribute)typeof(object).GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute));
					RuntimeInformation.s_frameworkDescription = ".NET Framework " + assemblyFileVersionAttribute.Version;
				}
				return RuntimeInformation.s_frameworkDescription;
			}
		}

		// Token: 0x06006309 RID: 25353 RVA: 0x00151987 File Offset: 0x0014FB87
		public static bool IsOSPlatform(OSPlatform osPlatform)
		{
			return OSPlatform.Windows == osPlatform;
		}

		// Token: 0x1700111C RID: 4380
		// (get) Token: 0x0600630A RID: 25354 RVA: 0x00151994 File Offset: 0x0014FB94
		public static string OSDescription
		{
			[SecuritySafeCritical]
			get
			{
				if (RuntimeInformation.s_osDescription == null)
				{
					RuntimeInformation.s_osDescription = RuntimeInformation.RtlGetVersion();
				}
				return RuntimeInformation.s_osDescription;
			}
		}

		// Token: 0x1700111D RID: 4381
		// (get) Token: 0x0600630B RID: 25355 RVA: 0x001519AC File Offset: 0x0014FBAC
		public static Architecture OSArchitecture
		{
			[SecuritySafeCritical]
			get
			{
				object obj = RuntimeInformation.s_osLock;
				lock (obj)
				{
					if (RuntimeInformation.s_osArch == null)
					{
						Win32Native.SYSTEM_INFO system_INFO;
						Win32Native.GetNativeSystemInfo(out system_INFO);
						RuntimeInformation.s_osArch = new Architecture?(RuntimeInformation.GetArchitecture(system_INFO.wProcessorArchitecture));
					}
				}
				return RuntimeInformation.s_osArch.Value;
			}
		}

		// Token: 0x1700111E RID: 4382
		// (get) Token: 0x0600630C RID: 25356 RVA: 0x00151A18 File Offset: 0x0014FC18
		public static Architecture ProcessArchitecture
		{
			[SecuritySafeCritical]
			get
			{
				object obj = RuntimeInformation.s_processLock;
				lock (obj)
				{
					if (RuntimeInformation.s_processArch == null)
					{
						Win32Native.SYSTEM_INFO system_INFO = default(Win32Native.SYSTEM_INFO);
						Win32Native.GetSystemInfo(ref system_INFO);
						RuntimeInformation.s_processArch = new Architecture?(RuntimeInformation.GetArchitecture(system_INFO.wProcessorArchitecture));
					}
				}
				return RuntimeInformation.s_processArch.Value;
			}
		}

		// Token: 0x0600630D RID: 25357 RVA: 0x00151A8C File Offset: 0x0014FC8C
		private static Architecture GetArchitecture(ushort wProcessorArchitecture)
		{
			Architecture result = Architecture.X86;
			if (wProcessorArchitecture <= 5)
			{
				if (wProcessorArchitecture != 0)
				{
					if (wProcessorArchitecture == 5)
					{
						result = Architecture.Arm;
					}
				}
				else
				{
					result = Architecture.X86;
				}
			}
			else if (wProcessorArchitecture != 9)
			{
				if (wProcessorArchitecture == 12)
				{
					result = Architecture.Arm64;
				}
			}
			else
			{
				result = Architecture.X64;
			}
			return result;
		}

		// Token: 0x0600630E RID: 25358 RVA: 0x00151AC4 File Offset: 0x0014FCC4
		[SecuritySafeCritical]
		private static string RtlGetVersion()
		{
			Win32Native.RTL_OSVERSIONINFOEX rtl_OSVERSIONINFOEX = default(Win32Native.RTL_OSVERSIONINFOEX);
			rtl_OSVERSIONINFOEX.dwOSVersionInfoSize = (uint)Marshal.SizeOf<Win32Native.RTL_OSVERSIONINFOEX>(rtl_OSVERSIONINFOEX);
			if (Win32Native.RtlGetVersion(out rtl_OSVERSIONINFOEX) == 0)
			{
				return string.Format("{0} {1}.{2}.{3} {4}", new object[] { "Microsoft Windows", rtl_OSVERSIONINFOEX.dwMajorVersion, rtl_OSVERSIONINFOEX.dwMinorVersion, rtl_OSVERSIONINFOEX.dwBuildNumber, rtl_OSVERSIONINFOEX.szCSDVersion });
			}
			return "Microsoft Windows";
		}

		// Token: 0x04002CAF RID: 11439
		private const string FrameworkName = ".NET Framework";

		// Token: 0x04002CB0 RID: 11440
		private static string s_frameworkDescription;

		// Token: 0x04002CB1 RID: 11441
		private static string s_osDescription = null;

		// Token: 0x04002CB2 RID: 11442
		private static object s_osLock = new object();

		// Token: 0x04002CB3 RID: 11443
		private static object s_processLock = new object();

		// Token: 0x04002CB4 RID: 11444
		private static Architecture? s_osArch = null;

		// Token: 0x04002CB5 RID: 11445
		private static Architecture? s_processArch = null;
	}
}
