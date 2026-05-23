using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x02000020 RID: 32
	internal class ScriptingInterfaceOfIPhysicsMaterial : IPhysicsMaterial
	{
		// Token: 0x060003D5 RID: 981 RVA: 0x00014F0E File Offset: 0x0001310E
		public float GetAngularDampingAtIndex(int index)
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetAngularDampingAtIndexDelegate(index);
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x00014F1B File Offset: 0x0001311B
		public float GetDynamicFrictionAtIndex(int index)
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetDynamicFrictionAtIndexDelegate(index);
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x00014F28 File Offset: 0x00013128
		public PhysicsMaterialFlags GetFlagsAtIndex(int index)
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetFlagsAtIndexDelegate(index);
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x00014F38 File Offset: 0x00013138
		public PhysicsMaterial GetIndexWithName(string materialName)
		{
			byte[] array = null;
			if (materialName != null)
			{
				int byteCount = ScriptingInterfaceOfIPhysicsMaterial._utf8.GetByteCount(materialName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIPhysicsMaterial._utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetIndexWithNameDelegate(array);
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x00014F92 File Offset: 0x00013192
		public float GetLinearDampingAtIndex(int index)
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetLinearDampingAtIndexDelegate(index);
		}

		// Token: 0x060003DA RID: 986 RVA: 0x00014F9F File Offset: 0x0001319F
		public int GetMaterialCount()
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetMaterialCountDelegate();
		}

		// Token: 0x060003DB RID: 987 RVA: 0x00014FAB File Offset: 0x000131AB
		public string GetMaterialNameAtIndex(int index)
		{
			if (ScriptingInterfaceOfIPhysicsMaterial.call_GetMaterialNameAtIndexDelegate(index) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x060003DC RID: 988 RVA: 0x00014FC2 File Offset: 0x000131C2
		public float GetRestitutionAtIndex(int index)
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetRestitutionAtIndexDelegate(index);
		}

		// Token: 0x060003DD RID: 989 RVA: 0x00014FCF File Offset: 0x000131CF
		public float GetStaticFrictionAtIndex(int index)
		{
			return ScriptingInterfaceOfIPhysicsMaterial.call_GetStaticFrictionAtIndexDelegate(index);
		}

		// Token: 0x04000341 RID: 833
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000342 RID: 834
		public static ScriptingInterfaceOfIPhysicsMaterial.GetAngularDampingAtIndexDelegate call_GetAngularDampingAtIndexDelegate;

		// Token: 0x04000343 RID: 835
		public static ScriptingInterfaceOfIPhysicsMaterial.GetDynamicFrictionAtIndexDelegate call_GetDynamicFrictionAtIndexDelegate;

		// Token: 0x04000344 RID: 836
		public static ScriptingInterfaceOfIPhysicsMaterial.GetFlagsAtIndexDelegate call_GetFlagsAtIndexDelegate;

		// Token: 0x04000345 RID: 837
		public static ScriptingInterfaceOfIPhysicsMaterial.GetIndexWithNameDelegate call_GetIndexWithNameDelegate;

		// Token: 0x04000346 RID: 838
		public static ScriptingInterfaceOfIPhysicsMaterial.GetLinearDampingAtIndexDelegate call_GetLinearDampingAtIndexDelegate;

		// Token: 0x04000347 RID: 839
		public static ScriptingInterfaceOfIPhysicsMaterial.GetMaterialCountDelegate call_GetMaterialCountDelegate;

		// Token: 0x04000348 RID: 840
		public static ScriptingInterfaceOfIPhysicsMaterial.GetMaterialNameAtIndexDelegate call_GetMaterialNameAtIndexDelegate;

		// Token: 0x04000349 RID: 841
		public static ScriptingInterfaceOfIPhysicsMaterial.GetRestitutionAtIndexDelegate call_GetRestitutionAtIndexDelegate;

		// Token: 0x0400034A RID: 842
		public static ScriptingInterfaceOfIPhysicsMaterial.GetStaticFrictionAtIndexDelegate call_GetStaticFrictionAtIndexDelegate;

		// Token: 0x020003B0 RID: 944
		// (Invoke) Token: 0x060014D3 RID: 5331
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetAngularDampingAtIndexDelegate(int index);

		// Token: 0x020003B1 RID: 945
		// (Invoke) Token: 0x060014D7 RID: 5335
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetDynamicFrictionAtIndexDelegate(int index);

		// Token: 0x020003B2 RID: 946
		// (Invoke) Token: 0x060014DB RID: 5339
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate PhysicsMaterialFlags GetFlagsAtIndexDelegate(int index);

		// Token: 0x020003B3 RID: 947
		// (Invoke) Token: 0x060014DF RID: 5343
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate PhysicsMaterial GetIndexWithNameDelegate(byte[] materialName);

		// Token: 0x020003B4 RID: 948
		// (Invoke) Token: 0x060014E3 RID: 5347
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetLinearDampingAtIndexDelegate(int index);

		// Token: 0x020003B5 RID: 949
		// (Invoke) Token: 0x060014E7 RID: 5351
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMaterialCountDelegate();

		// Token: 0x020003B6 RID: 950
		// (Invoke) Token: 0x060014EB RID: 5355
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMaterialNameAtIndexDelegate(int index);

		// Token: 0x020003B7 RID: 951
		// (Invoke) Token: 0x060014EF RID: 5359
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetRestitutionAtIndexDelegate(int index);

		// Token: 0x020003B8 RID: 952
		// (Invoke) Token: 0x060014F3 RID: 5363
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetStaticFrictionAtIndexDelegate(int index);
	}
}
