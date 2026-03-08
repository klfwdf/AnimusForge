using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x0200000B RID: 11
	internal class ScriptingInterfaceOfINativeStringHelper : INativeStringHelper
	{
		// Token: 0x0600005C RID: 92 RVA: 0x00002FF4 File Offset: 0x000011F4
		public UIntPtr CreateRglVarString(string text)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfINativeStringHelper._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfINativeStringHelper._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfINativeStringHelper.call_CreateRglVarStringDelegate(array);
		}

		// Token: 0x0600005D RID: 93 RVA: 0x0000304E File Offset: 0x0000124E
		public void DeleteRglVarString(UIntPtr pointer)
		{
			ScriptingInterfaceOfINativeStringHelper.call_DeleteRglVarStringDelegate(pointer);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000305B File Offset: 0x0000125B
		public UIntPtr GetThreadLocalCachedRglVarString()
		{
			return ScriptingInterfaceOfINativeStringHelper.call_GetThreadLocalCachedRglVarStringDelegate();
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003068 File Offset: 0x00001268
		public void SetRglVarString(UIntPtr pointer, string text)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfINativeStringHelper._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfINativeStringHelper._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfINativeStringHelper.call_SetRglVarStringDelegate(pointer, array);
		}

		// Token: 0x0400001E RID: 30
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400001F RID: 31
		public static ScriptingInterfaceOfINativeStringHelper.CreateRglVarStringDelegate call_CreateRglVarStringDelegate;

		// Token: 0x04000020 RID: 32
		public static ScriptingInterfaceOfINativeStringHelper.DeleteRglVarStringDelegate call_DeleteRglVarStringDelegate;

		// Token: 0x04000021 RID: 33
		public static ScriptingInterfaceOfINativeStringHelper.GetThreadLocalCachedRglVarStringDelegate call_GetThreadLocalCachedRglVarStringDelegate;

		// Token: 0x04000022 RID: 34
		public static ScriptingInterfaceOfINativeStringHelper.SetRglVarStringDelegate call_SetRglVarStringDelegate;

		// Token: 0x0200004F RID: 79
		// (Invoke) Token: 0x0600016F RID: 367
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr CreateRglVarStringDelegate(byte[] text);

		// Token: 0x02000050 RID: 80
		// (Invoke) Token: 0x06000173 RID: 371
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeleteRglVarStringDelegate(UIntPtr pointer);

		// Token: 0x02000051 RID: 81
		// (Invoke) Token: 0x06000177 RID: 375
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetThreadLocalCachedRglVarStringDelegate();

		// Token: 0x02000052 RID: 82
		// (Invoke) Token: 0x0600017B RID: 379
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRglVarStringDelegate(UIntPtr pointer, byte[] text);
	}
}
