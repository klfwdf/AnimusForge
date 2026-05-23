using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32
{
	// Token: 0x0200000D RID: 13
	internal static class Fusion
	{
		// Token: 0x06000018 RID: 24 RVA: 0x00002108 File Offset: 0x00000308
		[SecurityCritical]
		public static void ReadCache(ArrayList alAssems, string name, uint nFlag)
		{
			IAssemblyEnum assemblyEnum = null;
			IAssemblyName aName = null;
			IAssemblyName pName = null;
			IApplicationContext pAppCtx = null;
			int num;
			if (name != null)
			{
				num = Win32Native.CreateAssemblyNameObject(out pName, name, 1U, IntPtr.Zero);
				if (num != 0)
				{
					Marshal.ThrowExceptionForHR(num);
				}
			}
			num = Win32Native.CreateAssemblyEnum(out assemblyEnum, pAppCtx, pName, nFlag, IntPtr.Zero);
			if (num != 0)
			{
				Marshal.ThrowExceptionForHR(num);
			}
			for (;;)
			{
				num = assemblyEnum.GetNextAssembly(out pAppCtx, out aName, 0U);
				if (num != 0)
				{
					break;
				}
				string displayName = Fusion.GetDisplayName(aName, 0U);
				if (displayName != null)
				{
					alAssems.Add(displayName);
				}
			}
			if (num < 0)
			{
				Marshal.ThrowExceptionForHR(num);
				return;
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002190 File Offset: 0x00000390
		[SecuritySafeCritical]
		private unsafe static string GetDisplayName(IAssemblyName aName, uint dwDisplayFlags)
		{
			uint num = 0U;
			string result = null;
			aName.GetDisplayName((IntPtr)0, ref num, dwDisplayFlags);
			if (num > 0U)
			{
				IntPtr intPtr = (IntPtr)0;
				byte[] array = new byte[(num + 1U) * 2U];
				byte[] array2;
				byte* value;
				if ((array2 = array) == null || array2.Length == 0)
				{
					value = null;
				}
				else
				{
					value = &array2[0];
				}
				intPtr = new IntPtr((void*)value);
				aName.GetDisplayName(intPtr, ref num, dwDisplayFlags);
				result = Marshal.PtrToStringUni(intPtr);
				array2 = null;
			}
			return result;
		}
	}
}
