using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITexture : ITexture
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CheckAndGetFromResourceDelegate(byte[] textureName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateDepthTargetDelegate(byte[] name, int width, int height);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateFromByteArrayDelegate(ManagedArray data, int width, int height);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateFromMemoryDelegate(ManagedArray data);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateRenderTargetDelegate(byte[] name, int width, int height, [MarshalAs(UnmanagedType.U1)] bool autoMipmaps, [MarshalAs(UnmanagedType.U1)] bool isTableau, [MarshalAs(UnmanagedType.U1)] bool createUninitialized, [MarshalAs(UnmanagedType.U1)] bool always_valid);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateTextureFromPathDelegate(PlatformFilePath filePath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetCurObjectDelegate(UIntPtr texturePointer, [MarshalAs(UnmanagedType.U1)] bool blocking);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFromResourceDelegate(byte[] textureName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetHeightDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMemorySizeDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetPixelDataDelegate(UIntPtr texturePointer, ManagedArray bytes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetRenderTargetComponentDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetSDFBoundingBoxDataDelegate(UIntPtr texturePointer, ref Vec3 min, ref Vec3 max);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetTableauViewDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetWidthDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsLoadedDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsRenderTargetDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer LoadTextureFromPathDelegate(byte[] fileName, byte[] folder);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseAfterNumberOfFramesDelegate(UIntPtr texturePointer, int numberOfFrames);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseGpuMemoriesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseNextFrameDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveContinousTableauTextureDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SaveTextureAsAlwaysValidDelegate(UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SaveToFileDelegate(UIntPtr texturePointer, byte[] fileName, [MarshalAs(UnmanagedType.U1)] bool isRelativePath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNameDelegate(UIntPtr texturePointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTableauViewDelegate(UIntPtr texturePointer, UIntPtr tableauView);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TransformRenderTargetToResourceTextureDelegate(UIntPtr texturePointer, byte[] name);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CheckAndGetFromResourceDelegate call_CheckAndGetFromResourceDelegate;

	public static CreateDepthTargetDelegate call_CreateDepthTargetDelegate;

	public static CreateFromByteArrayDelegate call_CreateFromByteArrayDelegate;

	public static CreateFromMemoryDelegate call_CreateFromMemoryDelegate;

	public static CreateRenderTargetDelegate call_CreateRenderTargetDelegate;

	public static CreateTextureFromPathDelegate call_CreateTextureFromPathDelegate;

	public static GetCurObjectDelegate call_GetCurObjectDelegate;

	public static GetFromResourceDelegate call_GetFromResourceDelegate;

	public static GetHeightDelegate call_GetHeightDelegate;

	public static GetMemorySizeDelegate call_GetMemorySizeDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetPixelDataDelegate call_GetPixelDataDelegate;

	public static GetRenderTargetComponentDelegate call_GetRenderTargetComponentDelegate;

	public static GetSDFBoundingBoxDataDelegate call_GetSDFBoundingBoxDataDelegate;

	public static GetTableauViewDelegate call_GetTableauViewDelegate;

	public static GetWidthDelegate call_GetWidthDelegate;

	public static IsLoadedDelegate call_IsLoadedDelegate;

	public static IsRenderTargetDelegate call_IsRenderTargetDelegate;

	public static LoadTextureFromPathDelegate call_LoadTextureFromPathDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public static ReleaseAfterNumberOfFramesDelegate call_ReleaseAfterNumberOfFramesDelegate;

	public static ReleaseGpuMemoriesDelegate call_ReleaseGpuMemoriesDelegate;

	public static ReleaseNextFrameDelegate call_ReleaseNextFrameDelegate;

	public static RemoveContinousTableauTextureDelegate call_RemoveContinousTableauTextureDelegate;

	public static SaveTextureAsAlwaysValidDelegate call_SaveTextureAsAlwaysValidDelegate;

	public static SaveToFileDelegate call_SaveToFileDelegate;

	public static SetNameDelegate call_SetNameDelegate;

	public static SetTableauViewDelegate call_SetTableauViewDelegate;

	public static TransformRenderTargetToResourceTextureDelegate call_TransformRenderTargetToResourceTextureDelegate;

	public Texture CheckAndGetFromResource(string textureName)
	{
		byte[] array = null;
		if (textureName != null)
		{
			int byteCount = _utf8.GetByteCount(textureName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CheckAndGetFromResourceDelegate(array);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Texture CreateDepthTarget(string name, int width, int height)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateDepthTargetDelegate(array, width, height);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Texture CreateFromByteArray(byte[] data, int width, int height)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
		NativeObjectPointer nativeObjectPointer = call_CreateFromByteArrayDelegate(data2, width, height);
		pinnedArrayData.Dispose();
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Texture CreateFromMemory(byte[] data)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
		NativeObjectPointer nativeObjectPointer = call_CreateFromMemoryDelegate(data2);
		pinnedArrayData.Dispose();
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Texture CreateRenderTarget(string name, int width, int height, bool autoMipmaps, bool isTableau, bool createUninitialized, bool always_valid)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateRenderTargetDelegate(array, width, height, autoMipmaps, isTableau, createUninitialized, always_valid);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Texture CreateTextureFromPath(PlatformFilePath filePath)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateTextureFromPathDelegate(filePath);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void GetCurObject(UIntPtr texturePointer, bool blocking)
	{
		call_GetCurObjectDelegate(texturePointer, blocking);
	}

	public Texture GetFromResource(string textureName)
	{
		byte[] array = null;
		if (textureName != null)
		{
			int byteCount = _utf8.GetByteCount(textureName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetFromResourceDelegate(array);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetHeight(UIntPtr texturePointer)
	{
		return call_GetHeightDelegate(texturePointer);
	}

	public int GetMemorySize(UIntPtr texturePointer)
	{
		return call_GetMemorySizeDelegate(texturePointer);
	}

	public string GetName(UIntPtr texturePointer)
	{
		if (call_GetNameDelegate(texturePointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetPixelData(UIntPtr texturePointer, byte[] bytes)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(bytes);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray bytes2 = new ManagedArray(pointer, (bytes != null) ? bytes.Length : 0);
		call_GetPixelDataDelegate(texturePointer, bytes2);
		pinnedArrayData.Dispose();
	}

	public RenderTargetComponent GetRenderTargetComponent(UIntPtr texturePointer)
	{
		return DotNetObject.GetManagedObjectWithId(call_GetRenderTargetComponentDelegate(texturePointer)) as RenderTargetComponent;
	}

	public void GetSDFBoundingBoxData(UIntPtr texturePointer, ref Vec3 min, ref Vec3 max)
	{
		call_GetSDFBoundingBoxDataDelegate(texturePointer, ref min, ref max);
	}

	public TableauView GetTableauView(UIntPtr texturePointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetTableauViewDelegate(texturePointer);
		TableauView result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new TableauView(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetWidth(UIntPtr texturePointer)
	{
		return call_GetWidthDelegate(texturePointer);
	}

	public bool IsLoaded(UIntPtr texturePointer)
	{
		return call_IsLoadedDelegate(texturePointer);
	}

	public bool IsRenderTarget(UIntPtr texturePointer)
	{
		return call_IsRenderTargetDelegate(texturePointer);
	}

	public Texture LoadTextureFromPath(string fileName, string folder)
	{
		byte[] array = null;
		if (fileName != null)
		{
			int byteCount = _utf8.GetByteCount(fileName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(fileName, 0, fileName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (folder != null)
		{
			int byteCount2 = _utf8.GetByteCount(folder);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(folder, 0, folder.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_LoadTextureFromPathDelegate(array, array2);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void Release(UIntPtr texturePointer)
	{
		call_ReleaseDelegate(texturePointer);
	}

	public void ReleaseAfterNumberOfFrames(UIntPtr texturePointer, int numberOfFrames)
	{
		call_ReleaseAfterNumberOfFramesDelegate(texturePointer, numberOfFrames);
	}

	public void ReleaseGpuMemories()
	{
		call_ReleaseGpuMemoriesDelegate();
	}

	public void ReleaseNextFrame(UIntPtr texturePointer)
	{
		call_ReleaseNextFrameDelegate(texturePointer);
	}

	public void RemoveContinousTableauTexture(UIntPtr texturePointer)
	{
		call_RemoveContinousTableauTextureDelegate(texturePointer);
	}

	public void SaveTextureAsAlwaysValid(UIntPtr texturePointer)
	{
		call_SaveTextureAsAlwaysValidDelegate(texturePointer);
	}

	public void SaveToFile(UIntPtr texturePointer, string fileName, bool isRelativePath)
	{
		byte[] array = null;
		if (fileName != null)
		{
			int byteCount = _utf8.GetByteCount(fileName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(fileName, 0, fileName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SaveToFileDelegate(texturePointer, array, isRelativePath);
	}

	public void SetName(UIntPtr texturePointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetNameDelegate(texturePointer, array);
	}

	public void SetTableauView(UIntPtr texturePointer, UIntPtr tableauView)
	{
		call_SetTableauViewDelegate(texturePointer, tableauView);
	}

	public void TransformRenderTargetToResourceTexture(UIntPtr texturePointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_TransformRenderTargetToResourceTextureDelegate(texturePointer, array);
	}
}
