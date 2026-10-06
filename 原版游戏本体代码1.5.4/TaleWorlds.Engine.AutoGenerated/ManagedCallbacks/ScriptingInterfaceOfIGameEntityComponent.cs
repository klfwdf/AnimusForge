using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIGameEntityComponent : IGameEntityComponent
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetEntityDelegate(UIntPtr entityComponent);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr GetEntityPointerDelegate(UIntPtr componentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFirstMetaMeshDelegate(UIntPtr entityComponent);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetEntityDelegate call_GetEntityDelegate;

	public static GetEntityPointerDelegate call_GetEntityPointerDelegate;

	public static GetFirstMetaMeshDelegate call_GetFirstMetaMeshDelegate;

	public GameEntity GetEntity(GameEntityComponent entityComponent)
	{
		UIntPtr entityComponent2 = ((entityComponent != null) ? entityComponent.Pointer : UIntPtr.Zero);
		NativeObjectPointer nativeObjectPointer = call_GetEntityDelegate(entityComponent2);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public UIntPtr GetEntityPointer(UIntPtr componentPointer)
	{
		return call_GetEntityPointerDelegate(componentPointer);
	}

	public MetaMesh GetFirstMetaMesh(GameEntityComponent entityComponent)
	{
		UIntPtr entityComponent2 = ((entityComponent != null) ? entityComponent.Pointer : UIntPtr.Zero);
		NativeObjectPointer nativeObjectPointer = call_GetFirstMetaMeshDelegate(entityComponent2);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}
}
