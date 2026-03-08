using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000008 RID: 8
	internal class ScriptingInterfaceOfINativeArray : INativeArray
	{
		// Token: 0x06000046 RID: 70 RVA: 0x00002D5B File Offset: 0x00000F5B
		public void AddElement(UIntPtr pointer, IntPtr element, int elementSize)
		{
			ScriptingInterfaceOfINativeArray.call_AddElementDelegate(pointer, element, elementSize);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00002D6A File Offset: 0x00000F6A
		public void AddFloatElement(UIntPtr pointer, float value)
		{
			ScriptingInterfaceOfINativeArray.call_AddFloatElementDelegate(pointer, value);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00002D78 File Offset: 0x00000F78
		public void AddIntegerElement(UIntPtr pointer, int value)
		{
			ScriptingInterfaceOfINativeArray.call_AddIntegerElementDelegate(pointer, value);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002D86 File Offset: 0x00000F86
		public void Clear(UIntPtr pointer)
		{
			ScriptingInterfaceOfINativeArray.call_ClearDelegate(pointer);
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00002D94 File Offset: 0x00000F94
		public NativeArray Create()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfINativeArray.call_CreateDelegate();
			NativeArray result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new NativeArray(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00002DDD File Offset: 0x00000FDD
		public UIntPtr GetDataPointer(UIntPtr pointer)
		{
			return ScriptingInterfaceOfINativeArray.call_GetDataPointerDelegate(pointer);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00002DEA File Offset: 0x00000FEA
		public int GetDataPointerOffset()
		{
			return ScriptingInterfaceOfINativeArray.call_GetDataPointerOffsetDelegate();
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00002DF6 File Offset: 0x00000FF6
		public int GetDataSize(UIntPtr pointer)
		{
			return ScriptingInterfaceOfINativeArray.call_GetDataSizeDelegate(pointer);
		}

		// Token: 0x0400000B RID: 11
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400000C RID: 12
		public static ScriptingInterfaceOfINativeArray.AddElementDelegate call_AddElementDelegate;

		// Token: 0x0400000D RID: 13
		public static ScriptingInterfaceOfINativeArray.AddFloatElementDelegate call_AddFloatElementDelegate;

		// Token: 0x0400000E RID: 14
		public static ScriptingInterfaceOfINativeArray.AddIntegerElementDelegate call_AddIntegerElementDelegate;

		// Token: 0x0400000F RID: 15
		public static ScriptingInterfaceOfINativeArray.ClearDelegate call_ClearDelegate;

		// Token: 0x04000010 RID: 16
		public static ScriptingInterfaceOfINativeArray.CreateDelegate call_CreateDelegate;

		// Token: 0x04000011 RID: 17
		public static ScriptingInterfaceOfINativeArray.GetDataPointerDelegate call_GetDataPointerDelegate;

		// Token: 0x04000012 RID: 18
		public static ScriptingInterfaceOfINativeArray.GetDataPointerOffsetDelegate call_GetDataPointerOffsetDelegate;

		// Token: 0x04000013 RID: 19
		public static ScriptingInterfaceOfINativeArray.GetDataSizeDelegate call_GetDataSizeDelegate;

		// Token: 0x0200003F RID: 63
		// (Invoke) Token: 0x0600012F RID: 303
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddElementDelegate(UIntPtr pointer, IntPtr element, int elementSize);

		// Token: 0x02000040 RID: 64
		// (Invoke) Token: 0x06000133 RID: 307
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddFloatElementDelegate(UIntPtr pointer, float value);

		// Token: 0x02000041 RID: 65
		// (Invoke) Token: 0x06000137 RID: 311
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddIntegerElementDelegate(UIntPtr pointer, int value);

		// Token: 0x02000042 RID: 66
		// (Invoke) Token: 0x0600013B RID: 315
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearDelegate(UIntPtr pointer);

		// Token: 0x02000043 RID: 67
		// (Invoke) Token: 0x0600013F RID: 319
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateDelegate();

		// Token: 0x02000044 RID: 68
		// (Invoke) Token: 0x06000143 RID: 323
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetDataPointerDelegate(UIntPtr pointer);

		// Token: 0x02000045 RID: 69
		// (Invoke) Token: 0x06000147 RID: 327
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetDataPointerOffsetDelegate();

		// Token: 0x02000046 RID: 70
		// (Invoke) Token: 0x0600014B RID: 331
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetDataSizeDelegate(UIntPtr pointer);
	}
}
