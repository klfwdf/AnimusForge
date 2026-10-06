using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIPhysicsMaterial : IPhysicsMaterial
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAngularDampingAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetDynamicFrictionAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate PhysicsMaterialFlags GetFlagsAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate PhysicsMaterial GetIndexWithNameDelegate(byte[] materialName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetLinearDampingAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMaterialCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMaterialNameAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRestitutionAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetStaticFrictionAtIndexDelegate(int index);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetAngularDampingAtIndexDelegate call_GetAngularDampingAtIndexDelegate;

	public static GetDynamicFrictionAtIndexDelegate call_GetDynamicFrictionAtIndexDelegate;

	public static GetFlagsAtIndexDelegate call_GetFlagsAtIndexDelegate;

	public static GetIndexWithNameDelegate call_GetIndexWithNameDelegate;

	public static GetLinearDampingAtIndexDelegate call_GetLinearDampingAtIndexDelegate;

	public static GetMaterialCountDelegate call_GetMaterialCountDelegate;

	public static GetMaterialNameAtIndexDelegate call_GetMaterialNameAtIndexDelegate;

	public static GetRestitutionAtIndexDelegate call_GetRestitutionAtIndexDelegate;

	public static GetStaticFrictionAtIndexDelegate call_GetStaticFrictionAtIndexDelegate;

	public float GetAngularDampingAtIndex(int index)
	{
		return call_GetAngularDampingAtIndexDelegate(index);
	}

	public float GetDynamicFrictionAtIndex(int index)
	{
		return call_GetDynamicFrictionAtIndexDelegate(index);
	}

	public PhysicsMaterialFlags GetFlagsAtIndex(int index)
	{
		return call_GetFlagsAtIndexDelegate(index);
	}

	public PhysicsMaterial GetIndexWithName(string materialName)
	{
		byte[] array = null;
		if (materialName != null)
		{
			int byteCount = _utf8.GetByteCount(materialName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetIndexWithNameDelegate(array);
	}

	public float GetLinearDampingAtIndex(int index)
	{
		return call_GetLinearDampingAtIndexDelegate(index);
	}

	public int GetMaterialCount()
	{
		return call_GetMaterialCountDelegate();
	}

	public string GetMaterialNameAtIndex(int index)
	{
		if (call_GetMaterialNameAtIndexDelegate(index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetRestitutionAtIndex(int index)
	{
		return call_GetRestitutionAtIndexDelegate(index);
	}

	public float GetStaticFrictionAtIndex(int index)
	{
		return call_GetStaticFrictionAtIndexDelegate(index);
	}
}
