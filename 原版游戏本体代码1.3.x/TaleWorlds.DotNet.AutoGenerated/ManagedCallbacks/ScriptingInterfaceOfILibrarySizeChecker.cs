using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000006 RID: 6
	internal class ScriptingInterfaceOfILibrarySizeChecker : ILibrarySizeChecker
	{
		// Token: 0x0600003B RID: 59 RVA: 0x00002BF8 File Offset: 0x00000DF8
		public IntPtr GetEngineStructMemberOffset(string className, string memberName)
		{
			byte[] array = null;
			if (className != null)
			{
				int byteCount = ScriptingInterfaceOfILibrarySizeChecker._utf8.GetByteCount(className);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfILibrarySizeChecker._utf8.GetBytes(className, 0, className.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (memberName != null)
			{
				int byteCount2 = ScriptingInterfaceOfILibrarySizeChecker._utf8.GetByteCount(memberName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfILibrarySizeChecker._utf8.GetBytes(memberName, 0, memberName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			return ScriptingInterfaceOfILibrarySizeChecker.call_GetEngineStructMemberOffsetDelegate(array, array2);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002C98 File Offset: 0x00000E98
		public int GetEngineStructSize(string str)
		{
			byte[] array = null;
			if (str != null)
			{
				int byteCount = ScriptingInterfaceOfILibrarySizeChecker._utf8.GetByteCount(str);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfILibrarySizeChecker._utf8.GetBytes(str, 0, str.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfILibrarySizeChecker.call_GetEngineStructSizeDelegate(array);
		}

		// Token: 0x04000002 RID: 2
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000003 RID: 3
		public static ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructMemberOffsetDelegate call_GetEngineStructMemberOffsetDelegate;

		// Token: 0x04000004 RID: 4
		public static ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructSizeDelegate call_GetEngineStructSizeDelegate;

		// Token: 0x02000038 RID: 56
		// (Invoke) Token: 0x06000113 RID: 275
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate IntPtr GetEngineStructMemberOffsetDelegate(byte[] className, byte[] memberName);

		// Token: 0x02000039 RID: 57
		// (Invoke) Token: 0x06000117 RID: 279
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEngineStructSizeDelegate(byte[] str);
	}
}
