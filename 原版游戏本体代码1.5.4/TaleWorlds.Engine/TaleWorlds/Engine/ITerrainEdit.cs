using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ITerrainEdit
{
	[EngineMethod("create_terrain", false, null, false)]
	void CreateTerrain(UIntPtr scenePointer, int nodeDimX, int nodeDimY, float nodeSize, float minHeight, float maxHeight, int heightmapDetailLevel, string baseLayerName);

	[EngineMethod("set_node_height_data", false, null, false)]
	void SetNodeHeightData(Scene scene, int nodeX, int nodeY, float[] heights, int heightCount);

	[EngineMethod("add_layer_from_material", false, null, false)]
	int AddLayerFromMaterial(UIntPtr scenePointer, string layerPrefabName);

	[EngineMethod("add_empty_layer", false, null, false)]
	int AddEmptyLayer(UIntPtr scenePointer, string layerName);

	[EngineMethod("set_layer_texture", false, null, false)]
	void SetLayerTexture(UIntPtr scenePointer, int layerIndex, int textureType, string textureName);

	[EngineMethod("set_layer_property_float", false, null, false)]
	void SetLayerPropertyFloat(UIntPtr scenePointer, int layerIndex, int propertyId, float value);

	[EngineMethod("set_layer_property_string", false, null, false)]
	void SetLayerPropertyString(UIntPtr scenePointer, int layerIndex, int propertyId, string value);

	[EngineMethod("set_node_layer_weight_data", false, null, false)]
	void SetNodeLayerWeightData(Scene scene, int nodeX, int nodeY, int layerIndex, float[] weights, int weightCount);

	[EngineMethod("finalize", false, null, false)]
	void Finalize(UIntPtr scenePointer);

	[EngineMethod("add_procedural_flora_to_layer", false, null, false)]
	int AddProceduralFloraToLayer(Scene scene, int layerIndex, string floraKindName, float density, int seedIndex, float sizeMin, float sizeMax, float colonyRadius, float colonyThreshold, float weightOffset);

	[EngineMethod("add_placed_flora", false, null, false)]
	void AddPlacedFlora(Scene scene, string floraKindName, ref MatrixFrame frame);

	[EngineMethod("finalize_flora", false, null, false)]
	void FinalizeFlora(Scene scene);
}
