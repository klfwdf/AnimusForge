using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITextureView : ITextureView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateTextureViewDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTextureDelegate(UIntPtr pointer, UIntPtr texture_ptr);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateTextureViewDelegate call_CreateTextureViewDelegate;

	public static SetTextureDelegate call_SetTextureDelegate;

	public TextureView CreateTextureView()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateTextureViewDelegate();
		TextureView result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new TextureView(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void SetTexture(UIntPtr pointer, UIntPtr texture_ptr)
	{
		call_SetTextureDelegate(pointer, texture_ptr);
	}
}
