using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class ProceduralTerrainTestScript : ScriptComponentBehavior
{
	public int NodeDimensionX = 2;

	public int NodeDimensionY = 2;

	public float NodeSize = 256f;

	public float MinHeight;

	public float MaxHeight = 90f;

	public int HeightmapDetailLevel = 7;

	public float NoiseFrequency = 0.008f;

	public int NoiseOctaves = 5;

	public string GrassKindName = "flora_grass_a_non_shadow";

	public string ShrubKindName = "shrub__mix";

	public string TreeKindName = "tree_high_a";

	public float GrassDensity = 65f;

	public float ShrubDensity = 8f;

	public float TreeDensity = 1.5f;

	public string UndergrowthKindName = "flora_green__mix";

	public string FlowerKindName = "valley_flower__mix";

	public float UndergrowthDensity = 40f;

	public float FlowerDensity = 18f;

	public SimpleButton GenerateTerrain;

	private bool _pendingGenerate;

	private const float RockGradStart = 0.4f;

	private const float RockGradFull = 0.85f;

	private const float SoilGradStart = 0.12f;

	private const float SoilGradFull = 0.45f;

	private const float GrassNoiseFreq = 0.01f;

	private const float GrassMaskFloor = 0.46f;

	private const float GrassMaskCeil = 0.66f;

	private const float GrassGradLimit = 0.55f;

	private const float FloraGradStart = 0.25f;

	private const float FloraGradFull = 0.6f;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
		_pendingGenerate = !base.GameEntity.Scene.ContainsTerrain;
	}

	protected internal override void OnTick(float dt)
	{
		if (_pendingGenerate)
		{
			_pendingGenerate = false;
			TryGenerate();
		}
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "GenerateTerrain")
		{
			TryGenerate();
		}
	}

	private void TryGenerate()
	{
		Scene scene = base.Scene;
		if (!(scene == null))
		{
			GenerateProceduralTerrain(scene);
		}
	}

	private void GenerateProceduralTerrain(Scene scene)
	{
		int num = (1 << HeightmapDetailLevel) + 1;
		float quadLen = NodeSize / (float)(num - 1);
		float[] heights = new float[num * num];
		float[] array = new float[num * num];
		float[] array2 = new float[num * num];
		float[] array3 = new float[num * num];
		float[] array4 = new float[num * num];
		TerrainEditContext terrainEditContext = new TerrainEditContext(scene, NodeDimensionX, NodeDimensionY, NodeSize, MinHeight, MaxHeight, HeightmapDetailLevel, "desert_a");
		for (int i = 0; i < NodeDimensionX; i++)
		{
			for (int j = 0; j < NodeDimensionY; j++)
			{
				FillHeights(heights, i, j, num, quadLen);
				terrainEditContext.SetHeightData(i, j, heights);
			}
		}
		int layerIndex = terrainEditContext.AddLayerFromMaterial("rock_cliff");
		terrainEditContext.SetLayerTexture(layerIndex, TerrainEditContext.TextureSlot.Diffuse, "rock_cliff_b_d");
		terrainEditContext.SetLayerTexture(layerIndex, TerrainEditContext.TextureSlot.NormalMap, "rock_cliff_b_n");
		terrainEditContext.SetLayerTexture(layerIndex, TerrainEditContext.TextureSlot.SpecularMap, "rock_cliff_b_s");
		terrainEditContext.SetLayerTexture(layerIndex, TerrainEditContext.TextureSlot.DisplacementMap, "rock_cliff_b_h");
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.PhysicsMaterial, "stone");
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.UvScaleX, 6f);
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.UvScaleY, 6f);
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.ParallaxAmount, 0.15f);
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.GroundSlopeScale, 1f);
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.FlagUseParallax, 1f);
		terrainEditContext.SetLayerProperty(layerIndex, TerrainEditContext.LayerProperty.FlagSlopeTransparency, 1f);
		int layerIndex2 = terrainEditContext.AddLayerFromMaterial("soil_b");
		terrainEditContext.SetLayerTexture(layerIndex2, TerrainEditContext.TextureSlot.Diffuse, "ground_soil_b_d");
		terrainEditContext.SetLayerTexture(layerIndex2, TerrainEditContext.TextureSlot.NormalMap, "ground_soil_b_n");
		terrainEditContext.SetLayerTexture(layerIndex2, TerrainEditContext.TextureSlot.SpecularMap, "ground_soil_b_s");
		terrainEditContext.SetLayerTexture(layerIndex2, TerrainEditContext.TextureSlot.DisplacementMap, "ground_soil_b_h");
		terrainEditContext.SetLayerProperty(layerIndex2, TerrainEditContext.LayerProperty.PhysicsMaterial, "soil");
		terrainEditContext.SetLayerProperty(layerIndex2, TerrainEditContext.LayerProperty.UvScaleX, 5f);
		terrainEditContext.SetLayerProperty(layerIndex2, TerrainEditContext.LayerProperty.UvScaleY, 5f);
		terrainEditContext.SetLayerProperty(layerIndex2, TerrainEditContext.LayerProperty.ParallaxAmount, 0.15f);
		terrainEditContext.SetLayerProperty(layerIndex2, TerrainEditContext.LayerProperty.FlagUseParallax, 1f);
		int layerIndex3 = terrainEditContext.AddLayerFromMaterial("flora_habitat_a");
		terrainEditContext.SetLayerTexture(layerIndex3, TerrainEditContext.TextureSlot.Diffuse, "ground_grass_c_d");
		terrainEditContext.SetLayerTexture(layerIndex3, TerrainEditContext.TextureSlot.NormalMap, "ground_grass_c_n");
		terrainEditContext.SetLayerTexture(layerIndex3, TerrainEditContext.TextureSlot.SpecularMap, "ground_grass_c_s");
		terrainEditContext.SetLayerTexture(layerIndex3, TerrainEditContext.TextureSlot.DisplacementMap, "ground_grass_c_h");
		terrainEditContext.SetLayerProperty(layerIndex3, TerrainEditContext.LayerProperty.PhysicsMaterial, "soil");
		terrainEditContext.SetLayerProperty(layerIndex3, TerrainEditContext.LayerProperty.UvScaleX, 5f);
		terrainEditContext.SetLayerProperty(layerIndex3, TerrainEditContext.LayerProperty.UvScaleY, 5f);
		terrainEditContext.SetLayerProperty(layerIndex3, TerrainEditContext.LayerProperty.ParallaxAmount, 0.15f);
		terrainEditContext.SetLayerProperty(layerIndex3, TerrainEditContext.LayerProperty.FlagUseParallax, 1f);
		int layerIndex4 = terrainEditContext.AddEmptyLayer("flora_only");
		terrainEditContext.SetLayerProperty(layerIndex4, TerrainEditContext.LayerProperty.IsFloraLayer, 1f);
		for (int k = 0; k < NodeDimensionX; k++)
		{
			for (int l = 0; l < NodeDimensionY; l++)
			{
				FillHeights(heights, k, l, num, quadLen);
				FillWeights(heights, array, array2, array3, array4, num, quadLen, (float)k * NodeSize, (float)l * NodeSize);
				terrainEditContext.SetLayerWeightData(k, l, layerIndex, array);
				terrainEditContext.SetLayerWeightData(k, l, layerIndex2, array2);
				terrainEditContext.SetLayerWeightData(k, l, layerIndex3, array3);
				terrainEditContext.SetLayerWeightData(k, l, layerIndex4, array4);
			}
		}
		terrainEditContext.AddProceduralFlora(layerIndex3, new TerrainEditContext.FloraDefinition
		{
			FloraKindName = GrassKindName,
			Density = GrassDensity,
			SeedIndex = 3,
			SizeMin = 0.65f,
			SizeMax = 1.4f,
			ColonyRadius = 0f,
			ColonyThreshold = 0f,
			WeightOffset = 0.5f
		});
		terrainEditContext.AddProceduralFlora(layerIndex3, new TerrainEditContext.FloraDefinition
		{
			FloraKindName = ShrubKindName,
			Density = ShrubDensity,
			SeedIndex = 7,
			SizeMin = 0.65f,
			SizeMax = 1.3f,
			ColonyRadius = 0f,
			ColonyThreshold = 0f,
			WeightOffset = 0.5f
		});
		terrainEditContext.AddProceduralFlora(layerIndex3, new TerrainEditContext.FloraDefinition
		{
			FloraKindName = TreeKindName,
			Density = TreeDensity,
			SeedIndex = 42,
			SizeMin = 0.8f,
			SizeMax = 1.4f,
			ColonyRadius = 0f,
			ColonyThreshold = 0.3f,
			WeightOffset = 0.3f
		});
		terrainEditContext.AddProceduralFlora(layerIndex4, new TerrainEditContext.FloraDefinition
		{
			FloraKindName = UndergrowthKindName,
			Density = UndergrowthDensity,
			SeedIndex = 11,
			SizeMin = 0.7f,
			SizeMax = 1.3f,
			ColonyRadius = 0f,
			ColonyThreshold = 0f,
			WeightOffset = 0.5f
		});
		terrainEditContext.AddProceduralFlora(layerIndex4, new TerrainEditContext.FloraDefinition
		{
			FloraKindName = FlowerKindName,
			Density = FlowerDensity,
			SeedIndex = 23,
			SizeMin = 0.8f,
			SizeMax = 1.2f,
			ColonyRadius = 35f,
			ColonyThreshold = 0.4f,
			WeightOffset = 0.4f
		});
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = new Vec3((float)NodeDimensionX * NodeSize * 0.5f, (float)NodeDimensionY * NodeSize * 0.5f);
		terrainEditContext.AddPlacedFlora(TreeKindName, ref frame);
		terrainEditContext.FinalizeEditing();
		terrainEditContext.FinalizeFlora();
	}

	private void FillHeights(float[] heights, int nx, int ny, int verts, float quadLen)
	{
		float num = (float)nx * NodeSize;
		float num2 = (float)ny * NodeSize;
		for (int i = 0; i < verts; i++)
		{
			for (int j = 0; j < verts; j++)
			{
				float num3 = num + (float)i * quadLen;
				float num4 = num2 + (float)j * quadLen;
				float num5 = FbmNoise(num3 * NoiseFrequency, num4 * NoiseFrequency, NoiseOctaves);
				heights[i * verts + j] = MinHeight + (num5 * 0.5f + 0.5f) * (MaxHeight - MinHeight);
			}
		}
	}

	private static float SmoothStep(float edge0, float edge1, float x)
	{
		if (edge1 <= edge0)
		{
			if (!(x < edge0))
			{
				return 1f;
			}
			return 0f;
		}
		float num = (x - edge0) / (edge1 - edge0);
		num = ((num < 0f) ? 0f : ((num > 1f) ? 1f : num));
		return num * num * (3f - 2f * num);
	}

	private static void FillWeights(float[] heights, float[] rock, float[] soil, float[] grass, float[] floraOnly, int verts, float quadLen, float nodeWorldX, float nodeWorldY)
	{
		for (int i = 0; i < verts; i++)
		{
			for (int j = 0; j < verts; j++)
			{
				int num = i * verts + j;
				float num2 = heights[Math.Max(i - 1, 0) * verts + j];
				float num3 = heights[Math.Min(i + 1, verts - 1) * verts + j];
				float num4 = heights[i * verts + Math.Max(j - 1, 0)];
				float num5 = heights[i * verts + Math.Min(j + 1, verts - 1)];
				float num6 = (num3 - num2) / (2f * quadLen);
				float num7 = (num5 - num4) / (2f * quadLen);
				float x = (float)Math.Sqrt(num6 * num6 + num7 * num7);
				rock[num] = SmoothStep(0.4f, 0.85f, x);
				float num8 = SmoothStep(0.12f, 0.45f, x);
				soil[num] = num8 * (1f - rock[num] * 0.85f);
				float num9 = nodeWorldX + (float)i * quadLen;
				float num10 = nodeWorldY + (float)j * quadLen;
				float x2 = FbmNoise(num9 * 0.01f, num10 * 0.01f, 3) * 0.5f + 0.5f;
				float num11 = SmoothStep(0.46f, 0.66f, x2);
				float num12 = 1f - SmoothStep(0.33f, 0.55f, x);
				float num13 = 1f - Math.Max(rock[num], soil[num]);
				grass[num] = num11 * num12 * ((num13 < 0f) ? 0f : num13);
				float num14 = 1f - SmoothStep(0.25f, 0.6f, x);
				floraOnly[num] = num11 * num14;
			}
		}
	}

	private static float FbmNoise(float x, float y, int octaves)
	{
		float num = 0f;
		float num2 = 0.5f;
		float num3 = 1f;
		for (int i = 0; i < octaves; i++)
		{
			num += ValueNoise(x * num3, y * num3) * num2;
			num3 *= 2f;
			num2 *= 0.5f;
		}
		return num;
	}

	private static float ValueNoise(float x, float y)
	{
		int num = (int)Math.Floor(x);
		int num2 = (int)Math.Floor(y);
		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		float num5 = num3 * num3 * (3f - 2f * num3);
		float num6 = num4 * num4 * (3f - 2f * num4);
		float num7 = PseudoRandom(num, num2);
		float num8 = PseudoRandom(num + 1, num2);
		float num9 = PseudoRandom(num, num2 + 1);
		float num10 = PseudoRandom(num + 1, num2 + 1);
		float num11 = num7 + (num8 - num7) * num5;
		float num12 = num9 + (num10 - num9) * num5;
		return num11 + (num12 - num11) * num6;
	}

	private static float PseudoRandom(int x, int y)
	{
		int num = x * 1619 + y * 31337 + 6971;
		int num2 = (num ^ (num >>> 16)) * 73244475;
		return (float)(uint)((num2 ^ (num2 >>> 16)) & 0xFFFF) / 32767.5f - 1f;
	}
}
