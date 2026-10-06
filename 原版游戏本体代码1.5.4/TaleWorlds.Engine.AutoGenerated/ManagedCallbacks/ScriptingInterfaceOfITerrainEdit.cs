using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITerrainEdit : ITerrainEdit
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddEmptyLayerDelegate(UIntPtr scenePointer, byte[] layerName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddLayerFromMaterialDelegate(UIntPtr scenePointer, byte[] layerPrefabName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPlacedFloraDelegate(UIntPtr scene, byte[] floraKindName, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddProceduralFloraToLayerDelegate(UIntPtr scene, int layerIndex, byte[] floraKindName, float density, int seedIndex, float sizeMin, float sizeMax, float colonyRadius, float colonyThreshold, float weightOffset);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CreateTerrainDelegate(UIntPtr scenePointer, int nodeDimX, int nodeDimY, float nodeSize, float minHeight, float maxHeight, int heightmapDetailLevel, byte[] baseLayerName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FinalizeDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FinalizeFloraDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLayerPropertyFloatDelegate(UIntPtr scenePointer, int layerIndex, int propertyId, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLayerPropertyStringDelegate(UIntPtr scenePointer, int layerIndex, int propertyId, byte[] value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLayerTextureDelegate(UIntPtr scenePointer, int layerIndex, int textureType, byte[] textureName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNodeHeightDataDelegate(UIntPtr scene, int nodeX, int nodeY, IntPtr heights, int heightCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNodeLayerWeightDataDelegate(UIntPtr scene, int nodeX, int nodeY, int layerIndex, IntPtr weights, int weightCount);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddEmptyLayerDelegate call_AddEmptyLayerDelegate;

	public static AddLayerFromMaterialDelegate call_AddLayerFromMaterialDelegate;

	public static AddPlacedFloraDelegate call_AddPlacedFloraDelegate;

	public static AddProceduralFloraToLayerDelegate call_AddProceduralFloraToLayerDelegate;

	public static CreateTerrainDelegate call_CreateTerrainDelegate;

	public static FinalizeDelegate call_FinalizeDelegate;

	public static FinalizeFloraDelegate call_FinalizeFloraDelegate;

	public static SetLayerPropertyFloatDelegate call_SetLayerPropertyFloatDelegate;

	public static SetLayerPropertyStringDelegate call_SetLayerPropertyStringDelegate;

	public static SetLayerTextureDelegate call_SetLayerTextureDelegate;

	public static SetNodeHeightDataDelegate call_SetNodeHeightDataDelegate;

	public static SetNodeLayerWeightDataDelegate call_SetNodeLayerWeightDataDelegate;

	public int AddEmptyLayer(UIntPtr scenePointer, string layerName)
	{
		byte[] array = null;
		if (layerName != null)
		{
			int byteCount = _utf8.GetByteCount(layerName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(layerName, 0, layerName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_AddEmptyLayerDelegate(scenePointer, array);
	}

	public int AddLayerFromMaterial(UIntPtr scenePointer, string layerPrefabName)
	{
		byte[] array = null;
		if (layerPrefabName != null)
		{
			int byteCount = _utf8.GetByteCount(layerPrefabName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(layerPrefabName, 0, layerPrefabName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_AddLayerFromMaterialDelegate(scenePointer, array);
	}

	public void AddPlacedFlora(Scene scene, string floraKindName, ref MatrixFrame frame)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		byte[] array = null;
		if (floraKindName != null)
		{
			int byteCount = _utf8.GetByteCount(floraKindName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(floraKindName, 0, floraKindName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddPlacedFloraDelegate(scene2, array, ref frame);
	}

	public int AddProceduralFloraToLayer(Scene scene, int layerIndex, string floraKindName, float density, int seedIndex, float sizeMin, float sizeMax, float colonyRadius, float colonyThreshold, float weightOffset)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		byte[] array = null;
		if (floraKindName != null)
		{
			int byteCount = _utf8.GetByteCount(floraKindName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(floraKindName, 0, floraKindName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_AddProceduralFloraToLayerDelegate(scene2, layerIndex, array, density, seedIndex, sizeMin, sizeMax, colonyRadius, colonyThreshold, weightOffset);
	}

	public void CreateTerrain(UIntPtr scenePointer, int nodeDimX, int nodeDimY, float nodeSize, float minHeight, float maxHeight, int heightmapDetailLevel, string baseLayerName)
	{
		byte[] array = null;
		if (baseLayerName != null)
		{
			int byteCount = _utf8.GetByteCount(baseLayerName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(baseLayerName, 0, baseLayerName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CreateTerrainDelegate(scenePointer, nodeDimX, nodeDimY, nodeSize, minHeight, maxHeight, heightmapDetailLevel, array);
	}

	public void Finalize(UIntPtr scenePointer)
	{
		call_FinalizeDelegate(scenePointer);
	}

	public void FinalizeFlora(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_FinalizeFloraDelegate(scene2);
	}

	public void SetLayerPropertyFloat(UIntPtr scenePointer, int layerIndex, int propertyId, float value)
	{
		call_SetLayerPropertyFloatDelegate(scenePointer, layerIndex, propertyId, value);
	}

	public void SetLayerPropertyString(UIntPtr scenePointer, int layerIndex, int propertyId, string value)
	{
		byte[] array = null;
		if (value != null)
		{
			int byteCount = _utf8.GetByteCount(value);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(value, 0, value.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetLayerPropertyStringDelegate(scenePointer, layerIndex, propertyId, array);
	}

	public void SetLayerTexture(UIntPtr scenePointer, int layerIndex, int textureType, string textureName)
	{
		byte[] array = null;
		if (textureName != null)
		{
			int byteCount = _utf8.GetByteCount(textureName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetLayerTextureDelegate(scenePointer, layerIndex, textureType, array);
	}

	public void SetNodeHeightData(Scene scene, int nodeX, int nodeY, float[] heights, int heightCount)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(heights);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_SetNodeHeightDataDelegate(scene2, nodeX, nodeY, pointer, heightCount);
		pinnedArrayData.Dispose();
	}

	public void SetNodeLayerWeightData(Scene scene, int nodeX, int nodeY, int layerIndex, float[] weights, int weightCount)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(weights);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_SetNodeLayerWeightDataDelegate(scene2, nodeX, nodeY, layerIndex, pointer, weightCount);
		pinnedArrayData.Dispose();
	}
}
