using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x02000025 RID: 37
	internal class ScriptingInterfaceOfIScriptComponent : IScriptComponent
	{
		// Token: 0x06000566 RID: 1382 RVA: 0x00017F12 File Offset: 0x00016112
		public string GetName(UIntPtr pointer)
		{
			if (ScriptingInterfaceOfIScriptComponent.call_GetNameDelegate(pointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x00017F29 File Offset: 0x00016129
		public ScriptComponentBehavior GetScriptComponentBehavior(UIntPtr pointer)
		{
			return DotNetObject.GetManagedObjectWithId(ScriptingInterfaceOfIScriptComponent.call_GetScriptComponentBehaviorDelegate(pointer)) as ScriptComponentBehavior;
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x00017F40 File Offset: 0x00016140
		public void SetVariableEditorWidgetStatus(UIntPtr pointer, string field, bool enabled)
		{
			byte[] array = null;
			if (field != null)
			{
				int byteCount = ScriptingInterfaceOfIScriptComponent._utf8.GetByteCount(field);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScriptComponent._utf8.GetBytes(field, 0, field.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScriptComponent.call_SetVariableEditorWidgetStatusDelegate(pointer, array, enabled);
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x00017F9C File Offset: 0x0001619C
		public void SetVariableEditorWidgetValue(UIntPtr pointer, string field, RglScriptFieldType fieldType, double value)
		{
			byte[] array = null;
			if (field != null)
			{
				int byteCount = ScriptingInterfaceOfIScriptComponent._utf8.GetByteCount(field);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScriptComponent._utf8.GetBytes(field, 0, field.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScriptComponent.call_SetVariableEditorWidgetValueDelegate(pointer, array, fieldType, value);
		}

		// Token: 0x040004BE RID: 1214
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040004BF RID: 1215
		public static ScriptingInterfaceOfIScriptComponent.GetNameDelegate call_GetNameDelegate;

		// Token: 0x040004C0 RID: 1216
		public static ScriptingInterfaceOfIScriptComponent.GetScriptComponentBehaviorDelegate call_GetScriptComponentBehaviorDelegate;

		// Token: 0x040004C1 RID: 1217
		public static ScriptingInterfaceOfIScriptComponent.SetVariableEditorWidgetStatusDelegate call_SetVariableEditorWidgetStatusDelegate;

		// Token: 0x040004C2 RID: 1218
		public static ScriptingInterfaceOfIScriptComponent.SetVariableEditorWidgetValueDelegate call_SetVariableEditorWidgetValueDelegate;

		// Token: 0x02000528 RID: 1320
		// (Invoke) Token: 0x06001AB3 RID: 6835
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr pointer);

		// Token: 0x02000529 RID: 1321
		// (Invoke) Token: 0x06001AB7 RID: 6839
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetScriptComponentBehaviorDelegate(UIntPtr pointer);

		// Token: 0x0200052A RID: 1322
		// (Invoke) Token: 0x06001ABB RID: 6843
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVariableEditorWidgetStatusDelegate(UIntPtr pointer, byte[] field, [MarshalAs(UnmanagedType.U1)] bool enabled);

		// Token: 0x0200052B RID: 1323
		// (Invoke) Token: 0x06001ABF RID: 6847
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVariableEditorWidgetValueDelegate(UIntPtr pointer, byte[] field, RglScriptFieldType fieldType, double value);
	}
}
