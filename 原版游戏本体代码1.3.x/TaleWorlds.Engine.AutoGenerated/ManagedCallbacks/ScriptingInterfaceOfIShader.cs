using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x02000026 RID: 38
	internal class ScriptingInterfaceOfIShader : IShader
	{
		// Token: 0x0600056C RID: 1388 RVA: 0x00018010 File Offset: 0x00016210
		public Shader GetFromResource(string shaderName)
		{
			byte[] array = null;
			if (shaderName != null)
			{
				int byteCount = ScriptingInterfaceOfIShader._utf8.GetByteCount(shaderName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIShader._utf8.GetBytes(shaderName, 0, shaderName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIShader.call_GetFromResourceDelegate(array);
			Shader result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Shader(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x0001809C File Offset: 0x0001629C
		public ulong GetMaterialShaderFlagMask(UIntPtr shaderPointer, string flagName, bool showError)
		{
			byte[] array = null;
			if (flagName != null)
			{
				int byteCount = ScriptingInterfaceOfIShader._utf8.GetByteCount(flagName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIShader._utf8.GetBytes(flagName, 0, flagName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIShader.call_GetMaterialShaderFlagMaskDelegate(shaderPointer, array, showError);
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x000180F8 File Offset: 0x000162F8
		public string GetName(UIntPtr shaderPointer)
		{
			if (ScriptingInterfaceOfIShader.call_GetNameDelegate(shaderPointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600056F RID: 1391 RVA: 0x0001810F File Offset: 0x0001630F
		public void Release(UIntPtr shaderPointer)
		{
			ScriptingInterfaceOfIShader.call_ReleaseDelegate(shaderPointer);
		}

		// Token: 0x040004C3 RID: 1219
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040004C4 RID: 1220
		public static ScriptingInterfaceOfIShader.GetFromResourceDelegate call_GetFromResourceDelegate;

		// Token: 0x040004C5 RID: 1221
		public static ScriptingInterfaceOfIShader.GetMaterialShaderFlagMaskDelegate call_GetMaterialShaderFlagMaskDelegate;

		// Token: 0x040004C6 RID: 1222
		public static ScriptingInterfaceOfIShader.GetNameDelegate call_GetNameDelegate;

		// Token: 0x040004C7 RID: 1223
		public static ScriptingInterfaceOfIShader.ReleaseDelegate call_ReleaseDelegate;

		// Token: 0x0200052C RID: 1324
		// (Invoke) Token: 0x06001AC3 RID: 6851
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetFromResourceDelegate(byte[] shaderName);

		// Token: 0x0200052D RID: 1325
		// (Invoke) Token: 0x06001AC7 RID: 6855
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate ulong GetMaterialShaderFlagMaskDelegate(UIntPtr shaderPointer, byte[] flagName, [MarshalAs(UnmanagedType.U1)] bool showError);

		// Token: 0x0200052E RID: 1326
		// (Invoke) Token: 0x06001ACB RID: 6859
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr shaderPointer);

		// Token: 0x0200052F RID: 1327
		// (Invoke) Token: 0x06001ACF RID: 6863
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseDelegate(UIntPtr shaderPointer);
	}
}
