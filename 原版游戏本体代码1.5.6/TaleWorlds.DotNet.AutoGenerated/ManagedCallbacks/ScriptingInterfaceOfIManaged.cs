using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIManaged : IManaged
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DecreaseReferenceCountDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetClassTypeDefinitionDelegate(int index, ref EngineClassTypeDefinition engineClassTypeDefinition);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetClassTypeDefinitionCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void IncreaseReferenceCountDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseManagedObjectDelegate(UIntPtr ptr);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static DecreaseReferenceCountDelegate call_DecreaseReferenceCountDelegate;

	public static GetClassTypeDefinitionDelegate call_GetClassTypeDefinitionDelegate;

	public static GetClassTypeDefinitionCountDelegate call_GetClassTypeDefinitionCountDelegate;

	public static IncreaseReferenceCountDelegate call_IncreaseReferenceCountDelegate;

	public static ReleaseManagedObjectDelegate call_ReleaseManagedObjectDelegate;

	public void DecreaseReferenceCount(UIntPtr ptr)
	{
		call_DecreaseReferenceCountDelegate(ptr);
	}

	public void GetClassTypeDefinition(int index, ref EngineClassTypeDefinition engineClassTypeDefinition)
	{
		call_GetClassTypeDefinitionDelegate(index, ref engineClassTypeDefinition);
	}

	public int GetClassTypeDefinitionCount()
	{
		return call_GetClassTypeDefinitionCountDelegate();
	}

	public void IncreaseReferenceCount(UIntPtr ptr)
	{
		call_IncreaseReferenceCountDelegate(ptr);
	}

	public void ReleaseManagedObject(UIntPtr ptr)
	{
		call_ReleaseManagedObjectDelegate(ptr);
	}
}
