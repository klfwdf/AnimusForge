using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x0200021B RID: 539
	public interface IMapScene
	{
		// Token: 0x06002052 RID: 8274
		void Load();

		// Token: 0x06002053 RID: 8275
		void AfterLoad();

		// Token: 0x06002054 RID: 8276
		void Destroy();

		// Token: 0x06002055 RID: 8277
		PathFaceRecord GetFaceIndex(in CampaignVec2 vec2);

		// Token: 0x06002056 RID: 8278
		TerrainType GetTerrainTypeAtPosition(in CampaignVec2 vec2);

		// Token: 0x06002057 RID: 8279
		List<TerrainType> GetEnvironmentTerrainTypes(in CampaignVec2 vec2);

		// Token: 0x06002058 RID: 8280
		List<TerrainType> GetEnvironmentTerrainTypesCount(in CampaignVec2 vec2, out TerrainType currentPositionTerrainType);

		// Token: 0x06002059 RID: 8281
		MapPatchData GetMapPatchAtPosition(in CampaignVec2 position);

		// Token: 0x0600205A RID: 8282
		TerrainType GetFaceTerrainType(PathFaceRecord faceIndex);

		// Token: 0x0600205B RID: 8283
		CampaignVec2 GetNearestFaceCenterForPosition(in CampaignVec2 vec2, int[] excludedFaceIds);

		// Token: 0x0600205C RID: 8284
		CampaignVec2 GetNearestFaceCenterForPositionWithPath(PathFaceRecord pathFaceRecord, bool targetIsLand, float maxDist, int[] excludedFaceIds);

		// Token: 0x0600205D RID: 8285
		CampaignVec2 GetAccessiblePointNearPosition(in CampaignVec2 vec2, float radius);

		// Token: 0x0600205E RID: 8286
		bool GetPathBetweenAIFaces(PathFaceRecord startingFace, PathFaceRecord endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier, int regionSwitchCostFromLandToSea, int regionSwitchCostFromSeaToLand);

		// Token: 0x0600205F RID: 8287
		bool GetPathDistanceBetweenAIFaces(PathFaceRecord startingAiFace, PathFaceRecord endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] excludedFaceIds, int regionSwitchCostFromLandToSea, int regionSwitchCostFromSeaToLand);

		// Token: 0x06002060 RID: 8288
		bool IsLineToPointClear(PathFaceRecord startingFace, Vec2 position, Vec2 destination, float agentRadius);

		// Token: 0x06002061 RID: 8289
		Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(PathFaceRecord startingFace, Vec2 position, Vec2 destination, int[] excludedFaceIds = null);

		// Token: 0x06002062 RID: 8290
		Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(PathFaceRecord startingFace, Vec2 position, Vec2 destination);

		// Token: 0x06002063 RID: 8291
		Vec2 GetNavigationMeshCenterPosition(PathFaceRecord face);

		// Token: 0x06002064 RID: 8292
		Vec2 GetNavigationMeshCenterPosition(int faceIndex);

		// Token: 0x06002065 RID: 8293
		PathFaceRecord GetFaceAtIndex(int faceIndex);

		// Token: 0x06002066 RID: 8294
		int GetNumberOfNavigationMeshFaces();

		// Token: 0x06002067 RID: 8295
		bool GetHeightAtPoint(in CampaignVec2 point, ref float height);

		// Token: 0x06002068 RID: 8296
		float GetWinterTimeFactor();

		// Token: 0x06002069 RID: 8297
		void GetTerrainHeightAndNormal(Vec2 position, out float height, out Vec3 normal);

		// Token: 0x0600206A RID: 8298
		float GetFaceVertexZ(PathFaceRecord navMeshFace);

		// Token: 0x0600206B RID: 8299
		Vec3 GetGroundNormal(Vec2 position);

		// Token: 0x0600206C RID: 8300
		void GetSiegeCampFrames(Settlement settlement, out List<MatrixFrame> siegeCamp1GlobalFrames, out List<MatrixFrame> siegeCamp2GlobalFrames);

		// Token: 0x0600206D RID: 8301
		string GetTerrainTypeName(TerrainType type);

		// Token: 0x0600206E RID: 8302
		Vec2 GetTerrainSize();

		// Token: 0x0600206F RID: 8303
		uint GetSceneLevel(string name);

		// Token: 0x06002070 RID: 8304
		void SetSceneLevels(List<string> levels);

		// Token: 0x06002071 RID: 8305
		List<AtmosphereState> GetAtmosphereStates();

		// Token: 0x06002072 RID: 8306
		void SetAtmosphereColorgrade(TerrainType terrainType);

		// Token: 0x06002073 RID: 8307
		void AddNewEntityToMapScene(string entityId, in CampaignVec2 position);

		// Token: 0x06002074 RID: 8308
		void GetMapBorders(out Vec2 minimumPosition, out Vec2 maximumPosition, out float maximumHeight);

		// Token: 0x06002075 RID: 8309
		uint GetSceneXmlCrc();

		// Token: 0x06002076 RID: 8310
		uint GetSceneNavigationMeshCrc();

		// Token: 0x06002077 RID: 8311
		float GetSnowAmountAtPosition(Vec2 position);

		// Token: 0x06002078 RID: 8312
		float GetRainAmountAtPosition(Vec2 position);
	}
}
