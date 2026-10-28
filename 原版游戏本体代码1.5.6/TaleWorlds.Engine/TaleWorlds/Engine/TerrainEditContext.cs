using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public sealed class TerrainEditContext
{
	public enum TextureSlot
	{
		Diffuse,
		AreaMap,
		NormalMap,
		SpecularMap,
		SplattingMap,
		MaterialMap,
		DisplacementMap
	}

	public enum LayerProperty
	{
		PhysicsMaterial,
		UvScaleX,
		UvScaleY,
		UvRotation,
		ElevationAmount,
		ParallaxAmount,
		GroundSlopeScale,
		SmoothBlendAmount,
		BigDetailMapMode,
		BigDetailMapWeight,
		AlbedoFactorR,
		AlbedoFactorG,
		AlbedoFactorB,
		AlbedoFactorA,
		FlagUseParallax,
		FlagUseDisplacement,
		FlagSlopeTransparency,
		FlagRandomizedNormal,
		IsFloraLayer
	}

	public struct FloraDefinition
	{
		public string FloraKindName;

		public float Density;

		public int SeedIndex;

		public float SizeMin;

		public float SizeMax;

		public float ColonyRadius;

		public float ColonyThreshold;

		public float WeightOffset;

		public static FloraDefinition Default(string floraKindName)
		{
			return new FloraDefinition
			{
				FloraKindName = floraKindName,
				Density = 0.5f,
				SeedIndex = 0,
				SizeMin = 0.65f,
				SizeMax = 1.55f,
				ColonyRadius = 0f,
				ColonyThreshold = 0.3f,
				WeightOffset = 0.5f
			};
		}
	}

	private readonly Scene _scene;

	public TerrainEditContext(Scene scene, int nodeDimX, int nodeDimY, float nodeSize, float minHeight, float maxHeight, int heightmapDetailLevel = 7, string baseLayerName = "")
	{
		_scene = scene;
		EngineApplicationInterface.ITerrainEdit.CreateTerrain(scene.Pointer, nodeDimX, nodeDimY, nodeSize, minHeight, maxHeight, heightmapDetailLevel, baseLayerName);
	}

	public void SetHeightData(int nodeX, int nodeY, float[] heights)
	{
		EngineApplicationInterface.ITerrainEdit.SetNodeHeightData(_scene, nodeX, nodeY, heights, heights.Length);
	}

	public int AddLayerFromMaterial(string prefabName)
	{
		return EngineApplicationInterface.ITerrainEdit.AddLayerFromMaterial(_scene.Pointer, prefabName);
	}

	public int AddEmptyLayer(string layerName = "")
	{
		return EngineApplicationInterface.ITerrainEdit.AddEmptyLayer(_scene.Pointer, layerName ?? "");
	}

	public void SetLayerTexture(int layerIndex, TextureSlot slot, string textureName)
	{
		EngineApplicationInterface.ITerrainEdit.SetLayerTexture(_scene.Pointer, layerIndex, (int)slot, textureName);
	}

	public void SetLayerProperty(int layerIndex, LayerProperty property, float value)
	{
		EngineApplicationInterface.ITerrainEdit.SetLayerPropertyFloat(_scene.Pointer, layerIndex, (int)property, value);
	}

	public void SetLayerProperty(int layerIndex, LayerProperty property, string value)
	{
		EngineApplicationInterface.ITerrainEdit.SetLayerPropertyString(_scene.Pointer, layerIndex, (int)property, value);
	}

	public void SetLayerWeightData(int nodeX, int nodeY, int layerIndex, float[] weights)
	{
		EngineApplicationInterface.ITerrainEdit.SetNodeLayerWeightData(_scene, nodeX, nodeY, layerIndex, weights, weights.Length);
	}

	public void FinalizeEditing()
	{
		EngineApplicationInterface.ITerrainEdit.Finalize(_scene.Pointer);
	}

	public int AddProceduralFlora(int layerIndex, FloraDefinition def)
	{
		return EngineApplicationInterface.ITerrainEdit.AddProceduralFloraToLayer(_scene, layerIndex, def.FloraKindName, def.Density, def.SeedIndex, def.SizeMin, def.SizeMax, def.ColonyRadius, def.ColonyThreshold, def.WeightOffset);
	}

	public void AddPlacedFlora(string floraKindName, ref MatrixFrame frame)
	{
		EngineApplicationInterface.ITerrainEdit.AddPlacedFlora(_scene, floraKindName, ref frame);
	}

	public void FinalizeFlora()
	{
		EngineApplicationInterface.ITerrainEdit.FinalizeFlora(_scene);
	}
}
