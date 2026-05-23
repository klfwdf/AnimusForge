using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000007 RID: 7
	internal class ScriptingInterfaceOfIManaged : IManaged
	{
		// Token: 0x0600003F RID: 63 RVA: 0x00002D06 File Offset: 0x00000F06
		public void DecreaseReferenceCount(UIntPtr ptr)
		{
			ScriptingInterfaceOfIManaged.call_DecreaseReferenceCountDelegate(ptr);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002D13 File Offset: 0x00000F13
		public void GetClassTypeDefinition(int index, ref EngineClassTypeDefinition engineClassTypeDefinition)
		{
			ScriptingInterfaceOfIManaged.call_GetClassTypeDefinitionDelegate(index, ref engineClassTypeDefinition);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002D21 File Offset: 0x00000F21
		public int GetClassTypeDefinitionCount()
		{
			return ScriptingInterfaceOfIManaged.call_GetClassTypeDefinitionCountDelegate();
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002D2D File Offset: 0x00000F2D
		public void IncreaseReferenceCount(UIntPtr ptr)
		{
			ScriptingInterfaceOfIManaged.call_IncreaseReferenceCountDelegate(ptr);
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002D3A File Offset: 0x00000F3A
		public void ReleaseManagedObject(UIntPtr ptr)
		{
			ScriptingInterfaceOfIManaged.call_ReleaseManagedObjectDelegate(ptr);
		}

		// Token: 0x04000005 RID: 5
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000006 RID: 6
		public static ScriptingInterfaceOfIManaged.DecreaseReferenceCountDelegate call_DecreaseReferenceCountDelegate;

		// Token: 0x04000007 RID: 7
		public static ScriptingInterfaceOfIManaged.GetClassTypeDefinitionDelegate call_GetClassTypeDefinitionDelegate;

		// Token: 0x04000008 RID: 8
		public static ScriptingInterfaceOfIManaged.GetClassTypeDefinitionCountDelegate call_GetClassTypeDefinitionCountDelegate;

		// Token: 0x04000009 RID: 9
		public static ScriptingInterfaceOfIManaged.IncreaseReferenceCountDelegate call_IncreaseReferenceCountDelegate;

		// Token: 0x0400000A RID: 10
		public static ScriptingInterfaceOfIManaged.ReleaseManagedObjectDelegate call_ReleaseManagedObjectDelegate;

		// Token: 0x0200003A RID: 58
		// (Invoke) Token: 0x0600011B RID: 283
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DecreaseReferenceCountDelegate(UIntPtr ptr);

		// Token: 0x0200003B RID: 59
		// (Invoke) Token: 0x0600011F RID: 287
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetClassTypeDefinitionDelegate(int index, ref EngineClassTypeDefinition engineClassTypeDefinition);

		// Token: 0x0200003C RID: 60
		// (Invoke) Token: 0x06000123 RID: 291
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetClassTypeDefinitionCountDelegate();

		// Token: 0x0200003D RID: 61
		// (Invoke) Token: 0x06000127 RID: 295
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void IncreaseReferenceCountDelegate(UIntPtr ptr);

		// Token: 0x0200003E RID: 62
		// (Invoke) Token: 0x0600012B RID: 299
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseManagedObjectDelegate(UIntPtr ptr);
	}
}
