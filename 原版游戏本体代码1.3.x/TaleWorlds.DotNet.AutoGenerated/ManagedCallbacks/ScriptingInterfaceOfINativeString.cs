using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x0200000A RID: 10
	internal class ScriptingInterfaceOfINativeString : INativeString
	{
		// Token: 0x06000057 RID: 87 RVA: 0x00002EE4 File Offset: 0x000010E4
		public NativeString Create()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfINativeString.call_CreateDelegate();
			NativeString result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new NativeString(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002F30 File Offset: 0x00001130
		public string GetString(NativeString nativeString)
		{
			UIntPtr nativeString2 = ((nativeString != null) ? nativeString.Pointer : UIntPtr.Zero);
			if (ScriptingInterfaceOfINativeString.call_GetStringDelegate(nativeString2) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00002F6C File Offset: 0x0000116C
		public void SetString(NativeString nativeString, string newString)
		{
			UIntPtr nativeString2 = ((nativeString != null) ? nativeString.Pointer : UIntPtr.Zero);
			byte[] array = null;
			if (newString != null)
			{
				int byteCount = ScriptingInterfaceOfINativeString._utf8.GetByteCount(newString);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfINativeString._utf8.GetBytes(newString, 0, newString.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfINativeString.call_SetStringDelegate(nativeString2, array);
		}

		// Token: 0x0400001A RID: 26
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400001B RID: 27
		public static ScriptingInterfaceOfINativeString.CreateDelegate call_CreateDelegate;

		// Token: 0x0400001C RID: 28
		public static ScriptingInterfaceOfINativeString.GetStringDelegate call_GetStringDelegate;

		// Token: 0x0400001D RID: 29
		public static ScriptingInterfaceOfINativeString.SetStringDelegate call_SetStringDelegate;

		// Token: 0x0200004C RID: 76
		// (Invoke) Token: 0x06000163 RID: 355
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateDelegate();

		// Token: 0x0200004D RID: 77
		// (Invoke) Token: 0x06000167 RID: 359
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetStringDelegate(UIntPtr nativeString);

		// Token: 0x0200004E RID: 78
		// (Invoke) Token: 0x0600016B RID: 363
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStringDelegate(UIntPtr nativeString, byte[] newString);
	}
}
