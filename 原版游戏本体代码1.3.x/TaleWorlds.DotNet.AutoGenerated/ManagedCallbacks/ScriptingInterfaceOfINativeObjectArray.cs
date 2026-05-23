using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000009 RID: 9
	internal class ScriptingInterfaceOfINativeObjectArray : INativeObjectArray
	{
		// Token: 0x06000050 RID: 80 RVA: 0x00002E17 File Offset: 0x00001017
		public void AddElement(UIntPtr pointer, UIntPtr nativeObject)
		{
			ScriptingInterfaceOfINativeObjectArray.call_AddElementDelegate(pointer, nativeObject);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00002E25 File Offset: 0x00001025
		public void Clear(UIntPtr pointer)
		{
			ScriptingInterfaceOfINativeObjectArray.call_ClearDelegate(pointer);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002E34 File Offset: 0x00001034
		public NativeObjectArray Create()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfINativeObjectArray.call_CreateDelegate();
			NativeObjectArray result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new NativeObjectArray(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00002E7D File Offset: 0x0000107D
		public int GetCount(UIntPtr pointer)
		{
			return ScriptingInterfaceOfINativeObjectArray.call_GetCountDelegate(pointer);
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002E8C File Offset: 0x0000108C
		public NativeObject GetElementAtIndex(UIntPtr pointer, int index)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfINativeObjectArray.call_GetElementAtIndexDelegate(pointer, index);
			NativeObject result = NativeObject.CreateNativeObjectWrapper<NativeObject>(nativeObjectPointer);
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x04000014 RID: 20
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000015 RID: 21
		public static ScriptingInterfaceOfINativeObjectArray.AddElementDelegate call_AddElementDelegate;

		// Token: 0x04000016 RID: 22
		public static ScriptingInterfaceOfINativeObjectArray.ClearDelegate call_ClearDelegate;

		// Token: 0x04000017 RID: 23
		public static ScriptingInterfaceOfINativeObjectArray.CreateDelegate call_CreateDelegate;

		// Token: 0x04000018 RID: 24
		public static ScriptingInterfaceOfINativeObjectArray.GetCountDelegate call_GetCountDelegate;

		// Token: 0x04000019 RID: 25
		public static ScriptingInterfaceOfINativeObjectArray.GetElementAtIndexDelegate call_GetElementAtIndexDelegate;

		// Token: 0x02000047 RID: 71
		// (Invoke) Token: 0x0600014F RID: 335
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddElementDelegate(UIntPtr pointer, UIntPtr nativeObject);

		// Token: 0x02000048 RID: 72
		// (Invoke) Token: 0x06000153 RID: 339
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearDelegate(UIntPtr pointer);

		// Token: 0x02000049 RID: 73
		// (Invoke) Token: 0x06000157 RID: 343
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateDelegate();

		// Token: 0x0200004A RID: 74
		// (Invoke) Token: 0x0600015B RID: 347
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetCountDelegate(UIntPtr pointer);

		// Token: 0x0200004B RID: 75
		// (Invoke) Token: 0x0600015F RID: 351
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetElementAtIndexDelegate(UIntPtr pointer, int index);
	}
}
