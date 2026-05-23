using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map;

public interface IMapScene
{
	void Load();

	void AfterLoad();

	void Destroy();

	PathFaceRecord GetFaceIndex(in CampaignVec2 vec2);

	TerrainType GetTerrainTypeAtPosition(in CampaignVec2 vec2);

	List<TerrainType> GetEnvironmentTerrainTypes(in CampaignVec2 vec2);

	List<TerrainType> GetEnvironmentTerrainTypesCount(in CampaignVec2 vec2, out TerrainType currentPositionTerrainType);

	MapPatchData GetMapPatchAtPosition(in CampaignVec2 position);

	TerrainType GetFaceTerrainType(PathFaceRecord faceIndex);

	CampaignVec2 GetNearestFaceCenterForPosition(in CampaignVec2 vec2, int[] excludedFaceIds);

	CampaignVec2 GetNearestFaceCenterForPositionWithPath(PathFaceRecord pathFaceRecord, bool targetIsLand, float maxDist, int[] excludedFaceIds);

	CampaignVec2 GetAccessiblePointNearPosition(in CampaignVec2 vec2, float radius);

	bool GetPathBetweenAIFaces(PathFaceRecord startingFace, PathFaceRecord endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier, int regionSwitchCostFromLandToSea, int regionSwitchCostFromSeaToLand);

	bool GetPathDistanceBetweenAIFaces(PathFaceRecord startingAiFace, PathFaceRecord endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] excludedFaceIds, int regionSwitchCostFromLandToSea, int regionSwitchCostFromSeaToLand);

	bool IsLineToPointClear(PathFaceRecord startingFace, Vec2 position, Vec2 destination, float agentRadius);

	Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(PathFaceRecord startingFace, Vec2 position, Vec2 destination, int[] excludedFaceIds = null);

	Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(PathFaceRecord startingFace, Vec2 position, Vec2 destination);

	Vec2 GetNavigationMeshCenterPosition(PathFaceRecord face);

	Vec2 GetNavigationMeshCenterPosition(int faceIndex);

	PathFaceRecord GetFaceAtIndex(int faceIndex);

	int GetNumberOfNavigationMeshFaces();

	bool GetHeightAtPoint(in CampaignVec2 point, ref float height);

	float GetWinterTimeFactor();

	void GetTerrainHeightAndNormal(Vec2 position, out float height, out Vec3 normal);

	float GetFaceVertexZ(PathFaceRecord navMeshFace);

	Vec3 GetGroundNormal(Vec2 position);

	void GetSiegeCampFrames(Settlement settlement, out List<MatrixFrame> siegeCamp1GlobalFrames, out List<MatrixFrame> siegeCamp2GlobalFrames);

	string GetTerrainTypeName(TerrainType type);

	Vec2 GetTerrainSize();

	uint GetSceneLevel(string name);

	void SetSceneLevels(List<string> levels);

	List<AtmosphereState> GetAtmosphereStates();

	void SetAtmosphereColorgrade(TerrainType terrainType);

	void AddNewEntityToMapScene(string entityId, in CampaignVec2 position);

	void GetMapBorders(out Vec2 minimumPosition, out Vec2 maximumPosition, out float maximumHeight);

	uint GetSceneXmlCrc();

	uint GetSceneNavigationMeshCrc();

	float GetSnowAmountAtPosition(Vec2 position);

	float GetRainAmountAtPosition(Vec2 position);
}
