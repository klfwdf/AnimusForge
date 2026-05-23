using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBMapScene
{
	public static bool ApplyRainColorGrade;

	private static UIntPtr[] _tickedMapMeshesCachedArray = new UIntPtr[1024];

	public static Vec2 GetNearestFaceCenterForPosition(Scene mapScene, Vec2 position, bool isRegionMap0, int[] excludedFaceIds)
	{
		return MBAPI.IMBMapScene.GetNearestFaceCenterPositionForPosition(mapScene.Pointer, position.ToVec3(), isRegionMap0, excludedFaceIds, excludedFaceIds.Length);
	}

	public static Vec2 GetNearestFaceCenterForPositionWithPath(Scene mapScene, PathFaceRecord pathFaceRecord, bool targetRegionMap0, float maxDist, int[] excludedFaceIds)
	{
		return MBAPI.IMBMapScene.GetNearestFaceCenterForPositionWithPath(mapScene.Pointer, pathFaceRecord.FaceIndex, targetRegionMap0, maxDist, excludedFaceIds, excludedFaceIds.Length);
	}

	public static Vec2 GetAccessiblePointNearPosition(Scene mapScene, Vec2 position, bool isRegionMap1, float radius)
	{
		return MBAPI.IMBMapScene.GetAccessiblePointNearPosition(mapScene.Pointer, position, isRegionMap1, radius).AsVec2;
	}

	public static void RemoveZeroCornerBodies(Scene mapScene)
	{
		MBAPI.IMBMapScene.RemoveZeroCornerBodies(mapScene.Pointer);
	}

	public static void LoadAtmosphereData(Scene mapScene)
	{
		MBAPI.IMBMapScene.LoadAtmosphereData(mapScene.Pointer);
	}

	public static void TickStepSound(Scene mapScene, MBAgentVisuals visuals, int terrainType, TerrainTypeSoundSlot soundType, int partySize)
	{
		MBAPI.IMBMapScene.TickStepSound(mapScene.Pointer, visuals.Pointer, terrainType, soundType, partySize);
	}

	public static void TickAmbientSounds(Scene mapScene, int terrainType)
	{
		MBAPI.IMBMapScene.TickAmbientSounds(mapScene.Pointer, terrainType);
	}

	public static bool GetMouseVisible()
	{
		return MBAPI.IMBMapScene.GetMouseVisible();
	}

	public static bool GetApplyRainColorGrade()
	{
		return ApplyRainColorGrade;
	}

	public static void SendMouseKeyEvent(int mouseKeyId, bool isDown)
	{
		MBAPI.IMBMapScene.SendMouseKeyEvent(mouseKeyId, isDown);
	}

	public static void SetMousePos(int posX, int posY)
	{
		MBAPI.IMBMapScene.SetMousePos(posX, posY);
	}

	public static void TickVisuals(Scene mapScene, float tod, Mesh[] tickedMapMeshes)
	{
		for (int i = 0; i < tickedMapMeshes.Length; i++)
		{
			_tickedMapMeshesCachedArray[i] = tickedMapMeshes[i].Pointer;
		}
		MBAPI.IMBMapScene.TickVisuals(mapScene.Pointer, tod, _tickedMapMeshesCachedArray, tickedMapMeshes.Length);
	}

	public static void ValidateTerrainSoundIds()
	{
		MBAPI.IMBMapScene.ValidateTerrainSoundIds();
	}

	public static void GetGlobalIlluminationOfString(Scene mapScene, string value)
	{
		MBAPI.IMBMapScene.SetPoliticalColor(mapScene.Pointer, value);
	}

	public static void GetColorGradeGridData(Scene mapScene, byte[] gridData, string textureName)
	{
		MBAPI.IMBMapScene.GetColorGradeGridData(mapScene.Pointer, gridData, textureName);
	}

	public static void GetBattleSceneIndexMap(Scene mapScene, ref byte[] indexData, ref int width, ref int height)
	{
		MBAPI.IMBMapScene.GetBattleSceneIndexMapResolution(mapScene.Pointer, ref width, ref height);
		int num = width * height * 2;
		if (indexData == null || indexData.Length != num)
		{
			indexData = new byte[num];
		}
		MBAPI.IMBMapScene.GetBattleSceneIndexMap(mapScene.Pointer, indexData);
	}

	public static void SetFrameForAtmosphere(Scene mapScene, float tod, float cameraElevation, bool forceLoadTextures)
	{
		MBAPI.IMBMapScene.SetFrameForAtmosphere(mapScene.Pointer, tod, cameraElevation, forceLoadTextures);
	}

	public static void SetTerrainDynamicParams(Scene mapScene, Vec3 dynamic_params)
	{
		MBAPI.IMBMapScene.SetTerrainDynamicParams(mapScene.Pointer, dynamic_params);
	}

	public static void SetSeasonTimeFactor(Scene mapScene, float seasonTimeFactor)
	{
		MBAPI.IMBMapScene.SetSeasonTimeFactor(mapScene.Pointer, seasonTimeFactor);
	}

	public static float GetSeasonTimeFactor(Scene mapScene)
	{
		return MBAPI.IMBMapScene.GetSeasonTimeFactor(mapScene.Pointer);
	}
}
