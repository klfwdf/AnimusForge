using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.Missions.MissionLogics;

public class MissionPathGenerationLogic : MissionLogic
{
	public enum PointOfInterests
	{
		VisitPoint,
		CrossRoadPoint,
		GuardSpawnPoint,
		LookBackPoint
	}

	public class UsableMachineData
	{
		public SynchedMissionObject MissionObject;

		public Vec2 ClosestPointToPath;

		public float PathDistanceRatio;

		public bool IsAlreadyAddedToPath;

		public UsableMachineData(SynchedMissionObject missionObject, Vec2 closestPointToPath, float pathDistanceRatio)
		{
			MissionObject = missionObject;
			ClosestPointToPath = closestPointToPath;
			PathDistanceRatio = pathDistanceRatio;
			IsAlreadyAddedToPath = false;
		}
	}

	public class NavigationPathData
	{
		public GameEntity StartingGameEntity;

		public GameEntity EndingGameEntity;

		public NavigationPath Path;

		public Dictionary<Vec2, float> PathNodeAndDistances;

		public List<UsableMachineData> ValidUsableMachinesData;

		public float TotalDistance;

		public NavigationPathData(List<UsableMachine> allUsablePoints, GameEntity startingEntity, GameEntity endingEntity, int disabledFaceId)
		{
			ValidUsableMachinesData = new List<UsableMachineData>();
			StartingGameEntity = startingEntity;
			EndingGameEntity = endingEntity;
			Path = new NavigationPath();
			PathFaceRecord record = new PathFaceRecord(-1, -1, -1);
			Mission.Current.Scene.GetNavMeshFaceIndex(ref record, startingEntity.GlobalPosition, checkIfDisabled: true);
			PathFaceRecord record2 = new PathFaceRecord(-1, -1, -1);
			Mission.Current.Scene.GetNavMeshFaceIndex(ref record2, endingEntity.GlobalPosition, checkIfDisabled: true);
			Mission.Current.Scene.GetPathBetweenAIFaces(record.FaceIndex, record2.FaceIndex, startingEntity.GlobalPosition.AsVec2, endingEntity.GlobalPosition.AsVec2, 0f, Path, new int[1] { disabledFaceId }, 1f);
			PathNodeAndDistances = new Dictionary<Vec2, float>();
			PathNodeAndDistances.Add(Path[0], 0f);
			float num = 0f;
			for (int i = 0; i < Path.Size - 1; i++)
			{
				Vec2 vec = Path[i];
				Vec2 vec2 = Path[i + 1];
				num += vec.Distance(vec2);
				PathNodeAndDistances.Add(vec2, num);
			}
			TotalDistance = num;
			InitializeUsablePoints(allUsablePoints);
		}

		private NavigationPathData(NavigationPathData navigationPathData)
		{
			Path = new NavigationPath();
			Path.Size = navigationPathData.Path.Size;
			for (int i = 0; i < navigationPathData.Path.Size; i++)
			{
				Path.PathPoints[i] = navigationPathData.Path.PathPoints[Path.Size - 1 - i];
			}
			TotalDistance = navigationPathData.TotalDistance;
			PathNodeAndDistances = new Dictionary<Vec2, float>();
			foreach (KeyValuePair<Vec2, float> pathNodeAndDistance in navigationPathData.PathNodeAndDistances)
			{
				PathNodeAndDistances.Add(pathNodeAndDistance.Key, TotalDistance - pathNodeAndDistance.Value);
			}
			ValidUsableMachinesData = new List<UsableMachineData>();
			foreach (UsableMachineData validUsableMachinesDatum in navigationPathData.ValidUsableMachinesData)
			{
				ValidUsableMachinesData.Add(new UsableMachineData(validUsableMachinesDatum.MissionObject, validUsableMachinesDatum.ClosestPointToPath, 1f - validUsableMachinesDatum.PathDistanceRatio));
			}
			StartingGameEntity = navigationPathData.EndingGameEntity;
			EndingGameEntity = navigationPathData.StartingGameEntity;
		}

		public NavigationPathData ReverseClone()
		{
			return new NavigationPathData(this);
		}

		private bool GetPositionData(Vec2 position, out Vec2 closestPointToPath, out float pathDistanceRatio)
		{
			bool result = false;
			closestPointToPath = Vec2.Invalid;
			pathDistanceRatio = 0f;
			float num = float.MaxValue;
			for (int i = 0; i < Path.Size - 1; i++)
			{
				Vec2 lineSegmentBegin = Path[i];
				Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(in lineSegmentBegin, Path[i + 1], in position);
				float num2 = position.DistanceSquared(closestPointOnLineSegmentToPoint);
				if (num2 < 2f)
				{
					result = false;
					break;
				}
				if (num2 < 400f)
				{
					result = true;
					if (num2 < num)
					{
						closestPointToPath = closestPointOnLineSegmentToPoint;
						num = num2;
						pathDistanceRatio = (PathNodeAndDistances[lineSegmentBegin] + lineSegmentBegin.Distance(closestPointOnLineSegmentToPoint)) / TotalDistance;
					}
				}
			}
			return result;
		}

		public void InitializeUsablePoints(List<UsableMachine> allUsableMachines)
		{
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			float num3 = float.MinValue;
			float num4 = float.MinValue;
			for (int i = 0; i < Path.Size; i++)
			{
				Vec2 vec = Path[i];
				if (vec.X > num3)
				{
					num3 = vec.X;
				}
				if (vec.X < num)
				{
					num = vec.X;
				}
				if (vec.Y > num4)
				{
					num4 = vec.Y;
				}
				if (vec.Y < num2)
				{
					num2 = vec.Y;
				}
			}
			num3 += 20f;
			num4 += 20f;
			num -= 20f;
			num2 -= 20f;
			foreach (UsableMachine allUsableMachine in allUsableMachines)
			{
				if (!(allUsableMachine.GameEntity.GlobalPosition.X > num3) && !(allUsableMachine.GameEntity.GlobalPosition.X < num) && !(allUsableMachine.GameEntity.GlobalPosition.Y > num4) && !(allUsableMachine.GameEntity.GlobalPosition.Y < num2) && !(allUsableMachine is Chair) && GetPositionData(allUsableMachine.GameEntity.GlobalPosition.AsVec2, out var closestPointToPath, out var pathDistanceRatio))
				{
					ValidUsableMachinesData.Add(new UsableMachineData(allUsableMachine, closestPointToPath, pathDistanceRatio));
				}
			}
		}
	}

	public abstract class PointOfInterestBaseData
	{
		public float Score;

		public abstract PointOfInterests GetPointOfInterestType();

		public abstract List<(Vec2, float)> GetPositionAndRadiusPairs();

		public abstract bool IsInRadius(PointOfInterestBaseData otherPointOfInterest);

		public abstract float GetLocationRatio();
	}

	public class LookBackPointData : PointOfInterestBaseData
	{
		public WorldPosition WorldPosition;

		public WorldPosition DirectionWorldPosition;

		public float PathDistanceRatio;

		public LookBackPointData(WorldPosition position, WorldPosition direction, float pathDistanceRatio)
		{
			WorldPosition = position;
			PathDistanceRatio = pathDistanceRatio;
			DirectionWorldPosition = direction;
		}

		public override PointOfInterests GetPointOfInterestType()
		{
			return PointOfInterests.LookBackPoint;
		}

		public override List<(Vec2, float)> GetPositionAndRadiusPairs()
		{
			return new List<(Vec2, float)> { (WorldPosition.GetNavMeshVec3().AsVec2, 10f) };
		}

		public override bool IsInRadius(PointOfInterestBaseData otherPointOfInterest)
		{
			if (otherPointOfInterest is LookBackPointData)
			{
				foreach (var positionAndRadiusPair in GetPositionAndRadiusPairs())
				{
					foreach (var positionAndRadiusPair2 in otherPointOfInterest.GetPositionAndRadiusPairs())
					{
						var (vec, _) = positionAndRadiusPair;
						if (vec.Distance(positionAndRadiusPair2.Item1) < 25f)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public override float GetLocationRatio()
		{
			return PathDistanceRatio;
		}
	}

	public class VisitPointNodeScoreData : PointOfInterestBaseData
	{
		public UsableMachineData VisitPointData;

		public bool UsingAsInteractablePoint;

		public WorldPosition PossibleBlendPointPosition;

		public List<(Vec2, float)> PositionAndRadiusPairs;

		public WorldPosition VisitPointPathStartPoint;

		public float VisitPointPathStartPointPathRatio;

		public WorldPosition ClosestPointToBlendPoint;

		public WorldPosition FWP;

		public WorldPosition SWP;

		public float StartingAngle;

		public Vec2 PathToVisitPoint;

		public VisitPointNodeScoreData(UsableMachineData visitPointData, WorldPosition possibleBlendPointPosition, WorldPosition visitPointPathStartPoint, float visitPointPathStartPointPathRatio, float score, float startingAngle, WorldPosition fWP, WorldPosition sWP, Vec2 pathToVisitPoint, WorldPosition closestPointToBlendPoint)
		{
			VisitPointData = visitPointData;
			PossibleBlendPointPosition = possibleBlendPointPosition;
			VisitPointPathStartPoint = visitPointPathStartPoint;
			Score = score;
			PathToVisitPoint = pathToVisitPoint;
			SWP = sWP;
			FWP = fWP;
			ClosestPointToBlendPoint = closestPointToBlendPoint;
			VisitPointPathStartPointPathRatio = visitPointPathStartPointPathRatio;
			StartingAngle = startingAngle;
			PositionAndRadiusPairs = new List<(Vec2, float)>();
			PositionAndRadiusPairs.Add((visitPointData.MissionObject.GameEntity.GlobalPosition.AsVec2, 7f));
			PositionAndRadiusPairs.Add((PossibleBlendPointPosition.AsVec2, 3f));
			PositionAndRadiusPairs.Add((VisitPointPathStartPoint.AsVec2, 3f));
			UsingAsInteractablePoint = false;
		}

		public override PointOfInterests GetPointOfInterestType()
		{
			return PointOfInterests.VisitPoint;
		}

		public override List<(Vec2, float)> GetPositionAndRadiusPairs()
		{
			return PositionAndRadiusPairs;
		}

		public override bool IsInRadius(PointOfInterestBaseData otherPointOfInterest)
		{
			float num = 1f;
			if (otherPointOfInterest is VisitPointNodeScoreData)
			{
				num = 2f;
			}
			else if (otherPointOfInterest is CrossRoadScoreData)
			{
				num = 0.5f;
			}
			foreach (var (vec, num2) in PositionAndRadiusPairs)
			{
				foreach (var (v, num3) in otherPointOfInterest.GetPositionAndRadiusPairs())
				{
					if (vec.Distance(v) < (num2 + num3) * num)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override float GetLocationRatio()
		{
			return VisitPointData.PathDistanceRatio;
		}
	}

	public class CrossRoadScoreData : PointOfInterestBaseData
	{
		public UsableMachineData LeftNode;

		public UsableMachineData RightNode;

		public List<(Vec2, float)> PositionAndRadiusPairs;

		public CrossRoadScoreData(UsableMachineData leftNode, UsableMachineData rightNode, float score)
		{
			LeftNode = leftNode;
			RightNode = rightNode;
			Score = score;
			PositionAndRadiusPairs = new List<(Vec2, float)>();
			PositionAndRadiusPairs.Add((LeftNode.MissionObject.GameEntity.GlobalPosition.AsVec2, 1f));
			PositionAndRadiusPairs.Add((RightNode.MissionObject.GameEntity.GlobalPosition.AsVec2, 1f));
			PositionAndRadiusPairs.Add((RightNode.ClosestPointToPath, 1f));
			PositionAndRadiusPairs.Add((LeftNode.ClosestPointToPath, 1f));
		}

		public override PointOfInterests GetPointOfInterestType()
		{
			return PointOfInterests.CrossRoadPoint;
		}

		public override List<(Vec2, float)> GetPositionAndRadiusPairs()
		{
			return PositionAndRadiusPairs;
		}

		public override bool IsInRadius(PointOfInterestBaseData otherPointOfInterest)
		{
			foreach (var (vec, num) in PositionAndRadiusPairs)
			{
				foreach (var (v, num2) in otherPointOfInterest.GetPositionAndRadiusPairs())
				{
					if (vec.Distance(v) < num + num2)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override float GetLocationRatio()
		{
			return (LeftNode.PathDistanceRatio + RightNode.PathDistanceRatio) * 0.5f;
		}
	}

	public class StandingGuardSpawnData : PointOfInterestBaseData
	{
		public UsableMachineData GuardPointData;

		public Vec2 SpawnDirection;

		public List<(Vec2, float)> PositionAndRadiusPairs;

		public StandingGuardSpawnData(UsableMachineData guardPointData, Vec2 spawnDirection, float score)
		{
			GuardPointData = guardPointData;
			SpawnDirection = spawnDirection;
			Score = score;
			PositionAndRadiusPairs = new List<(Vec2, float)>();
			PositionAndRadiusPairs.Add((GuardPointData.MissionObject.GameEntity.GlobalPosition.AsVec2, 2f));
		}

		public override PointOfInterests GetPointOfInterestType()
		{
			return PointOfInterests.GuardSpawnPoint;
		}

		public override List<(Vec2, float)> GetPositionAndRadiusPairs()
		{
			return PositionAndRadiusPairs;
		}

		public override bool IsInRadius(PointOfInterestBaseData otherPointOfInterest)
		{
			foreach (var (vec, num) in PositionAndRadiusPairs)
			{
				foreach (var (v, num2) in otherPointOfInterest.GetPositionAndRadiusPairs())
				{
					if (vec.Distance(v) < num + num2)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override float GetLocationRatio()
		{
			return GuardPointData.PathDistanceRatio;
		}
	}

	public class PointOfInterestScorePair
	{
		public NavigationPathData PathData;

		private List<PointOfInterestBaseData> _data;

		public Dictionary<PointOfInterests, int> PointOfInterestCount;

		public float Score;

		public List<PointOfInterestBaseData> Data => _data;

		public PointOfInterestScorePair(NavigationPathData pathData, List<PointOfInterestBaseData> data, float score)
		{
			PathData = pathData;
			_data = data;
			Score = score;
			PointOfInterestCount = new Dictionary<PointOfInterests, int>();
			PointOfInterests[] array = (PointOfInterests[])Enum.GetValues(typeof(PointOfInterests));
			foreach (PointOfInterests key in array)
			{
				PointOfInterestCount.Add(key, 0);
			}
			foreach (PointOfInterestBaseData datum in _data)
			{
				PointOfInterestCount[datum.GetPointOfInterestType()]++;
			}
		}

		private PointOfInterestScorePair(PointOfInterestScorePair otherPair)
		{
			PathData = otherPair.PathData;
			_data = otherPair._data.ToList();
			Score = otherPair.Score;
			PointOfInterestCount = otherPair.PointOfInterestCount.ToDictionary((KeyValuePair<PointOfInterests, int> x) => x.Key, (KeyValuePair<PointOfInterests, int> x) => x.Value);
		}

		public PointOfInterestScorePair Clone()
		{
			return new PointOfInterestScorePair(this);
		}

		public void AddToData(PointOfInterestBaseData pointOfInterestToAdd)
		{
			PointOfInterestCount[pointOfInterestToAdd.GetPointOfInterestType()]++;
			_data.Add(pointOfInterestToAdd);
			Score += pointOfInterestToAdd.Score;
		}

		public bool IsDataEqualTo(PointOfInterestScorePair other, PointOfInterestBaseData newDataToAdd)
		{
			if (PathData != other.PathData || other.Data.Count + 1 != Data.Count || !Score.ApproximatelyEqualsTo(other.Score + newDataToAdd.Score) || Data[Data.Count - 1] != newDataToAdd)
			{
				return false;
			}
			for (int num = other.Data.Count - 1; num >= 0; num--)
			{
				if (other.Data[num] != Data[num])
				{
					return false;
				}
			}
			return true;
		}

		public bool IsBetterThan(PointOfInterestScorePair other)
		{
			float num = (float)(MaximumVisitPointCountInPath + MinimumVisitPointCountInPath) * 0.5f;
			float num2 = Math.Abs((float)PointOfInterestCount[PointOfInterests.VisitPoint] - num);
			float num3 = Math.Abs((float)other.PointOfInterestCount[PointOfInterests.VisitPoint] - num);
			float num4 = 0.5f;
			float num5 = ((Score >= other.Score) ? 0.2f : (-0.2f));
			float num6 = ((num3 >= num2) ? 0.2f : (-0.2f));
			return num4 + num5 + num6 > 0.5f;
		}

		public bool IsSufficient()
		{
			int num = PointOfInterestCount[PointOfInterests.VisitPoint];
			int num2 = PointOfInterestCount[PointOfInterests.CrossRoadPoint];
			if (Score >= (float)ScoreToAchieve && PathData.TotalDistance >= (float)MinimumPathDistance && PathData.TotalDistance <= (float)MaximumPathDistance && num >= MinimumVisitPointCountInPath && num <= MaximumVisitPointCountInPath && num2 >= MinimumCrossRoadCountInPath)
			{
				return num2 <= MaximumCrossRoadCountInPath;
			}
			return false;
		}

		public void ReOrderDataAccordingToPathRatios()
		{
			_data = _data.OrderBy((PointOfInterestBaseData x) => x.GetLocationRatio()).ToList();
		}
	}

	private const float MaximumPathNodeDistanceSquaredToCheckForCrossRoads = 100f;

	private const float MinimumPathNodeDistanceSquaredToCheckForCrossRoads = 25f;

	private const float StandingGuardCountPerXMeter = 10f;

	private const float HumanMonsterCapsuleRadius = 0.37f;

	private const float MinimumStandingGuardSpawnDistance = 3f;

	private const float OptimumStandingGuardSpawnDistance = 5f;

	private const float MaximumStandingGuardSpawnDistance = 30f;

	private const float DoNotSpawnVisitPointPathRatioMin = 0.2f;

	private const float DoNotSpawnVisitPointPathRatioMax = 0.9f;

	private const float OptimumPathIndexRatioForVisitPoint = 0.75f;

	private const float FilterPadding = 20f;

	private const string VisitBarrelPrefabName = "disguise_mission_interactable_barrel";

	private const bool PlayerCompromised = false;

	private readonly CharacterObject _defaultDisguiseCharacter;

	private int _disabledFaceId;

	public static int MinimumPathDistance = 200;

	public static int MaximumPathDistance = 600;

	public float MinimumDistanceToBlendPointToVisitPoint = 5f;

	private PointOfInterestScorePair _selectedPath;

	public static int MinimumVisitPointCountInPath = 2;

	public static int MaximumVisitPointCountInPath = 10;

	public static int MinimumCrossRoadCountInPath = 2;

	public static int MaximumCrossRoadCountInPath = 10;

	public static int MinimumStandingGuardCountInPath = 5;

	public static int MaximumStandingGuardCountInPath = 50;

	public static float MinimumGuardSpawnPathRatio = 0.15f;

	public static int MaximumLookBackPointCountInPath;

	public static int ScoreToAchieve;

	private Dictionary<Agent, bool> _crossRoadAgentData;

	private DisguiseMissionLogic _disguiseMissionLogic;

	private readonly List<GameEntity> _visitBarrelEntities;

	public List<GameEntity> _startAndFinishPointPool;

	private GameEntity _currentStarting;

	private GameEntity _currentEnding;

	public int CrossRoadMaximumDistance = 30;

	public int CrossRoadMinimumDistance = 10;

	public int MinimumVisitPointDistance = 10;

	public int MaximumVisitPointDistance = 40;

	private List<UsableMachineData> _nearbyLeftSideUsableMachinesCache;

	private List<UsableMachineData> _nearbyRightSideUsableMachinesCache;

	private List<PointOfInterestBaseData> _allTargetAgentPointOfInterest;

	private WorldPosition _tempWorldPosition;

	public MissionPathGenerationLogic(CharacterObject defaultDisguiseCharacter)
	{
		_defaultDisguiseCharacter = defaultDisguiseCharacter;
		_selectedPath = null;
		_nearbyLeftSideUsableMachinesCache = new List<UsableMachineData>();
		_nearbyRightSideUsableMachinesCache = new List<UsableMachineData>();
		_allTargetAgentPointOfInterest = new List<PointOfInterestBaseData>();
		_crossRoadAgentData = new Dictionary<Agent, bool>();
		_visitBarrelEntities = new List<GameEntity>();
		_startAndFinishPointPool = new List<GameEntity>();
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (userAgent.IsMainAgent)
		{
			GameEntity item = GameEntity.CreateFromWeakEntity(usedObject.GameEntity);
			if (_visitBarrelEntities.Contains(item))
			{
				userAgent.SetActionChannel(0, in ActionIndexCache.act_smithing_machine_anvil_start, ignorePriority: false, (AnimFlags)0uL);
				_visitBarrelEntities.Remove(item);
			}
		}
	}

	private void SpawnDisguiseAgents()
	{
		foreach (PointOfInterestBaseData datum in _selectedPath.Data)
		{
			if (datum is CrossRoadScoreData selectedCrossRoad)
			{
				SpawnCrossRoadAgents(selectedCrossRoad);
			}
			else if (datum is StandingGuardSpawnData standingGuardSpawnPoint)
			{
				SpawnStandingGuards(standingGuardSpawnPoint);
			}
			else if (datum is VisitPointNodeScoreData visitPointNodeScoreData)
			{
				SpawnVisitPointGuardsAndBlendPoints(visitPointNodeScoreData, useAsBarrelPoint: true);
				_allTargetAgentPointOfInterest.Add(visitPointNodeScoreData);
			}
			else if (datum is LookBackPointData item)
			{
				_allTargetAgentPointOfInterest.Add(item);
			}
		}
		_allTargetAgentPointOfInterest = _allTargetAgentPointOfInterest.OrderBy((PointOfInterestBaseData x) => x.GetLocationRatio()).ToList();
	}

	private void SpawnVisitPointGuardsAndBlendPoints(VisitPointNodeScoreData visitPointData, bool useAsBarrelPoint)
	{
		FadeOutUserAgentsInUsableMachine(visitPointData.VisitPointData.MissionObject as UsableMachine);
		MatrixFrame globalFrame = visitPointData.VisitPointData.MissionObject.GameEntity.GetGlobalFrame();
		WorldFrame worldFrame = new WorldFrame(globalFrame.rotation, new WorldPosition(visitPointData.VisitPointData.MissionObject.GameEntity.Scene, globalFrame.origin));
		if (useAsBarrelPoint)
		{
			Vec3 groundVec = worldFrame.Origin.GetGroundVec3();
			float num = float.MaxValue;
			Vec3 vec = Vec3.Zero;
			for (int i = 0; (float)i < 360f; i++)
			{
				worldFrame.Rotation.RotateAboutUp(System.MathF.PI / 180f);
				Vec3 lastPointOnNavigationMeshFromWorldPositionToDestination = Mission.Current.Scene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref worldFrame.Origin, worldFrame.Origin.AsVec2 + worldFrame.Rotation.f.AsVec2 * 30f);
				float num2 = worldFrame.Origin.AsVec2.Distance(lastPointOnNavigationMeshFromWorldPositionToDestination.AsVec2);
				if (num2 < num)
				{
					num = num2;
					vec = lastPointOnNavigationMeshFromWorldPositionToDestination;
				}
			}
			PathFaceRecord record = new PathFaceRecord(-1, -1, -1);
			Mission.Current.Scene.GetNavMeshFaceIndex(ref record, vec, checkIfDisabled: true);
			Vec3 centerPosition = Vec3.Zero;
			Mission.Current.Scene.GetNavMeshCenterPosition(record.FaceIndex, ref centerPosition);
			worldFrame.Origin.SetVec2(vec.AsVec2 + (centerPosition.AsVec2 - vec.AsVec2) * 0.25f);
			float f = Vec3.AngleBetweenTwoVectors(groundVec - vec, worldFrame.Rotation.f);
			worldFrame.Rotation.RotateAboutUp(f.ToRadians());
			GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, "disguise_mission_interactable_barrel", worldFrame.ToGroundMatrixFrame());
			_visitBarrelEntities.Add(gameEntity);
			visitPointData.UsingAsInteractablePoint = true;
			visitPointData.VisitPointData.MissionObject = gameEntity.GetFirstScriptOfType<UsableMissionObject>();
		}
		else
		{
			Vec3 initialPosition = worldFrame.Origin.GetGroundVec3() - worldFrame.Rotation.f;
			Agent agent = _disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(_defaultDisguiseCharacter, initialPosition, worldFrame.Rotation.f.AsVec2.Normalized(), "_hideout_bandit");
			UsableMachine usableMachine = visitPointData.VisitPointData.MissionObject as UsableMachine;
			if (usableMachine.StandingPoints.Any() && usableMachine.StandingPoints[0] is AnimationPoint animationPoint)
			{
				agent.SetActionChannel(0, ActionIndexCache.Create(animationPoint.LoopStartAction), ignorePriority: true, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat);
			}
		}
		Vec2 vec2 = visitPointData.ClosestPointToBlendPoint.AsVec2 - visitPointData.PossibleBlendPointPosition.AsVec2;
		_disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(Settlement.CurrentSettlement.Culture.Beggar, visitPointData.PossibleBlendPointPosition.GetNavMeshVec3(), vec2.Normalized(), "_hideout_bandit", isEnemy: false).SetActionChannel(0, in ActionIndexCache.act_beggar_idle, ignorePriority: true, (AnimFlags)0uL);
	}

	private void SpawnStandingGuards(StandingGuardSpawnData standingGuardSpawnPoint)
	{
		FadeOutUserAgentsInUsableMachine(standingGuardSpawnPoint.GuardPointData.MissionObject as UsableMachine);
		MatrixFrame globalFrame = standingGuardSpawnPoint.GuardPointData.MissionObject.GameEntity.GetGlobalFrame();
		_disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(_defaultDisguiseCharacter, globalFrame.origin, standingGuardSpawnPoint.SpawnDirection.Normalized(), "_hideout_bandit");
	}

	private void SpawnCrossRoadAgents(CrossRoadScoreData selectedCrossRoad)
	{
		FadeOutUserAgentsInUsableMachine(selectedCrossRoad.LeftNode.MissionObject as UsableMachine);
		FadeOutUserAgentsInUsableMachine(selectedCrossRoad.RightNode.MissionObject as UsableMachine);
		MatrixFrame matrixFrame = ((MBRandom.RandomFloat < 0.5f) ? selectedCrossRoad.LeftNode.MissionObject.GameEntity.GetGlobalFrame() : selectedCrossRoad.RightNode.MissionObject.GameEntity.GetGlobalFrame());
		Agent agent = _disguiseMissionLogic.SpawnDisguiseMissionAgentInternal(_defaultDisguiseCharacter, matrixFrame.origin, matrixFrame.rotation.f.AsVec2.Normalized(), "_hideout_bandit");
		_crossRoadAgentData.Add(agent, value: false);
		ScriptBehavior.AddTargetWithDelegate(agent, CrossRoadAgentSelectTargetDelegate(selectedCrossRoad), CrossRoadAgentWaitDelegate, CrossRoadAgentOnTargetReachDelegate);
	}

	private void CrossRoadAgentWaitDelegate(Agent agent, ref float waitTimeInSeconds)
	{
		waitTimeInSeconds = MBRandom.RandomInt(6, 30);
	}

	private bool CrossRoadAgentOnTargetReachDelegate(Agent agent1, ref Agent targetAgent, ref UsableMachine machine, ref WorldFrame frame)
	{
		_crossRoadAgentData[agent1] = !_crossRoadAgentData[agent1];
		return true;
	}

	private ScriptBehavior.SelectTargetDelegate CrossRoadAgentSelectTargetDelegate(CrossRoadScoreData selectedCrossRoad)
	{
		return delegate(Agent agent1, ref Agent targetAgent, ref UsableMachine machine, ref WorldFrame frame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
		{
			customTargetReachedRangeThreshold = 2.5f;
			customTargetReachedRotationThreshold = 0.8f;
			if (_crossRoadAgentData[agent1])
			{
				WorldPosition origin = new WorldPosition(Mission.Current.Scene, selectedCrossRoad.LeftNode.MissionObject.GameEntity.GlobalPosition);
				frame = new WorldFrame(selectedCrossRoad.LeftNode.MissionObject.GameEntity.GetGlobalFrame().rotation, origin);
			}
			else
			{
				WorldPosition origin2 = new WorldPosition(Mission.Current.Scene, selectedCrossRoad.RightNode.MissionObject.GameEntity.GlobalPosition);
				frame = new WorldFrame(selectedCrossRoad.RightNode.MissionObject.GameEntity.GetGlobalFrame().rotation, origin2);
			}
			return true;
		};
	}

	private float CalculateCrossRoadScoreForUsableMachines(UsableMachineData leftSideUsableMachineData, UsableMachineData rightSideUsableMachineData, NavigationPath originalPath, WorldPosition pathNodeStartPosition, WorldPosition pathNodeEndPosition)
	{
		if (leftSideUsableMachineData.PathDistanceRatio < 0.1f || rightSideUsableMachineData.PathDistanceRatio < 0.1f)
		{
			return 0f;
		}
		if (leftSideUsableMachineData.ClosestPointToPath.Distance(rightSideUsableMachineData.ClosestPointToPath) > pathNodeStartPosition.GetNavMeshVec3().Distance(pathNodeEndPosition.GetNavMeshVec3()))
		{
			return 0f;
		}
		_tempWorldPosition.SetVec2(leftSideUsableMachineData.MissionObject.GameEntity.GlobalPosition.AsVec2);
		_tempWorldPosition.GetNavMeshZ();
		WorldPosition point = _tempWorldPosition;
		_tempWorldPosition.SetVec2(rightSideUsableMachineData.MissionObject.GameEntity.GlobalPosition.AsVec2);
		_tempWorldPosition.GetNavMeshZ();
		WorldPosition point2 = _tempWorldPosition;
		Mission.Current.Scene.GetPathDistanceBetweenPositions(ref point, ref point2, 0.37f, out var pathDistance);
		if (pathDistance > (float)CrossRoadMaximumDistance || pathDistance < (float)CrossRoadMinimumDistance)
		{
			return 0f;
		}
		_tempWorldPosition.SetVec2(leftSideUsableMachineData.ClosestPointToPath);
		_tempWorldPosition.GetNavMeshZ();
		WorldPosition point3 = _tempWorldPosition;
		_tempWorldPosition.SetVec2(rightSideUsableMachineData.ClosestPointToPath);
		_tempWorldPosition.GetNavMeshZ();
		WorldPosition point4 = _tempWorldPosition;
		base.Mission.Scene.GetPathDistanceBetweenPositions(ref point, ref point2, 0.37f, out var _);
		pathNodeStartPosition.AsVec2.Distance(pathNodeEndPosition.AsVec2);
		Mission.Current.Scene.GetPathDistanceBetweenPositions(ref point, ref point3, 0.37f, out var pathDistance3);
		Mission.Current.Scene.GetPathDistanceBetweenPositions(ref point4, ref point2, 0.37f, out var pathDistance4);
		if (pathDistance3 > pathDistance4 && pathDistance4 / pathDistance3 < 0.2f)
		{
			return 0f;
		}
		if (pathDistance4 > pathDistance3 && pathDistance3 / pathDistance4 < 0.2f)
		{
			return 0f;
		}
		float value = (pathNodeEndPosition.AsVec2 - pathNodeStartPosition.AsVec2).AngleBetween(point.AsVec2 - point2.AsVec2).ToDegrees();
		if (Math.Abs(value) > 150f || Math.Abs(value) < 30f)
		{
			return 0f;
		}
		float num = 0f;
		num = ((!(Math.Abs(value) > 90f)) ? MBMath.Map(Math.Abs(value), 30f, 90f, 0f, 1f) : MBMath.Map(Math.Abs(value), 90f, 150f, 1f, 0f));
		float num2 = MBMath.Map(pathDistance3 + pathDistance4, 0f, 20f, 0f, 1f);
		return num + num2;
	}

	private float CalculateVisitPointScore(UsableMachineData usableMachineData, NavigationPath originalPath, WorldPosition pathNodeStart, WorldPosition pathNodeEnd, out Vec3 possibleBlendPointPosition, out float startingAngle, out Vec2 pathToVisitPointZero, out Vec2 closestPointToPath)
	{
		possibleBlendPointPosition = Vec3.Invalid;
		startingAngle = 0f;
		pathToVisitPointZero = Vec2.Zero;
		closestPointToPath = Vec2.Invalid;
		if (usableMachineData.PathDistanceRatio < 0.2f || usableMachineData.PathDistanceRatio > 0.9f)
		{
			return 0f;
		}
		WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, usableMachineData.MissionObject.GameEntity.GlobalPosition);
		_tempWorldPosition.SetVec2(usableMachineData.ClosestPointToPath);
		_tempWorldPosition.GetNavMeshZ();
		WorldPosition tempWorldPosition = _tempWorldPosition;
		NavigationPath navigationPath = new NavigationPath();
		base.Mission.Scene.GetPathBetweenAIFaces(pathNodeStart.GetNearestNavMesh(), worldPosition.GetNearestNavMesh(), pathNodeStart.AsVec2, worldPosition.AsVec2, 0f, navigationPath, new int[1] { _disabledFaceId });
		Vec2 vec = pathNodeStart.AsVec2 + (pathNodeEnd.AsVec2 - pathNodeStart.AsVec2) * 0.5f;
		_tempWorldPosition.SetVec2(vec);
		_tempWorldPosition.GetNavMeshZ();
		WorldPosition tempWorldPosition2 = _tempWorldPosition;
		float num = navigationPath[0].Distance(vec);
		for (int i = 0; i < navigationPath.Size - 1; i++)
		{
			Vec2 vec2 = navigationPath[i];
			Vec2 v = navigationPath[i + 1];
			num += vec2.Distance(v);
		}
		if (num < (float)MinimumVisitPointDistance || num > (float)MaximumVisitPointDistance)
		{
			return 0f;
		}
		float num2 = 0f;
		startingAngle = (pathNodeEnd.GetNavMeshVec3().AsVec2 - pathNodeStart.GetNavMeshVec3().AsVec2).AngleBetween(navigationPath[0] - tempWorldPosition2.GetNavMeshVec3().AsVec2).ToDegrees();
		pathToVisitPointZero = navigationPath[0];
		if (Math.Abs(startingAngle) < 90f && Math.Abs(startingAngle) > 30f)
		{
			for (int j = 0; j < navigationPath.Size - 1; j++)
			{
				Vec2 vec3 = ((j == 0) ? tempWorldPosition.AsVec2 : navigationPath[j - 1]);
				Vec2 vec4 = navigationPath[j];
				Vec2 vec5 = navigationPath[j + 1];
				float value = (vec4 - vec3).AngleBetween(vec5 - vec4).ToDegrees();
				num2 += MBMath.Map(Math.Abs(value), 0f, 90f, 1f, 0f);
				if (!((float)j > (float)(navigationPath.Size - 1) * 0.25f) || possibleBlendPointPosition.IsValid)
				{
					continue;
				}
				_tempWorldPosition.SetVec2(vec4);
				_tempWorldPosition.GetNavMeshZ();
				WorldPosition tempWorldPosition3 = _tempWorldPosition;
				Vec3 navMeshVec = tempWorldPosition3.GetNavMeshVec3();
				Vec3 vec6 = Vec3.Invalid;
				PathFaceRecord record = PathFaceRecord.NullFaceRecord;
				int num3 = 0;
				float num4 = float.MaxValue;
				do
				{
					num3++;
					if (num3 > 150)
					{
						break;
					}
					vec6 = base.Mission.GetRandomPositionAroundPoint(navMeshVec, 2f, 6f, nearFirst: true);
					base.Mission.Scene.GetNavMeshFaceIndex(ref record, vec6, checkIfDisabled: true);
					if (record.FaceGroupIndex != _disabledFaceId)
					{
						continue;
					}
					for (int k = 0; k < navigationPath.Size - 1; k++)
					{
						Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(navigationPath[k], navigationPath[k + 1], vec6.AsVec2);
						float num5 = vec6.AsVec2.Distance(closestPointOnLineSegmentToPoint);
						if (num5 < num4)
						{
							closestPointToPath = closestPointOnLineSegmentToPoint;
							num4 = num5;
						}
					}
				}
				while (record.FaceGroupIndex != _disabledFaceId || num4 < 1.5f);
				if (num3 < 150)
				{
					possibleBlendPointPosition = vec6;
				}
			}
			num2 /= (float)navigationPath.Size;
			if (!possibleBlendPointPosition.IsValid || possibleBlendPointPosition.AsVec2.Distance(worldPosition.AsVec2) < MinimumDistanceToBlendPointToVisitPoint)
			{
				return 0f;
			}
			for (int l = 0; l < originalPath.Size - 1; l++)
			{
				Vec2 v2 = originalPath[l];
				for (int m = 0; m < navigationPath.Size - 1; m++)
				{
					if (navigationPath[m].Distance(v2) < 2f)
					{
						return 0f;
					}
				}
			}
			float num6 = 0f;
			float num7 = (float)(MaximumVisitPointDistance + MinimumVisitPointDistance) * 0.5f;
			num6 = ((!(num > num7)) ? MBMath.Map(num, MinimumVisitPointDistance, num7, 0f, 0.5f) : MBMath.Map(num, num7, MaximumVisitPointDistance, 0.5f, 1f));
			float num8 = 0f;
			UsableMachine usableMachine = usableMachineData.MissionObject as UsableMachine;
			if (usableMachine.StandingPoints.Count > 0 && usableMachine.StandingPoints.Count != usableMachine.StandingPoints.Count((StandingPoint x) => x.HasAlternative()) && usableMachine.StandingPoints.Count((StandingPoint x) => x is AnimationPoint animationPoint && animationPoint.PairEntity != null) == 2)
			{
				num8 = 2f;
			}
			float num9 = ((usableMachineData.PathDistanceRatio > 0.75f) ? MBMath.Map(usableMachineData.PathDistanceRatio, 0.75f, 1f, 1f, 0f) : MBMath.Map(usableMachineData.PathDistanceRatio, 0f, 0.75f, 0f, 1f));
			return 5f + num2 + num6 + num9 + num8;
		}
		return 0f;
	}

	private float CalculateSpawnGuardScore(UsableMachineData guardSpawnPointData, out Vec2 spawnRotation)
	{
		spawnRotation = Vec2.Zero;
		UsableMachine usableMachine = guardSpawnPointData.MissionObject as UsableMachine;
		if (usableMachine.PilotAgent != null)
		{
			return 0f;
		}
		foreach (StandingPoint standingPoint in usableMachine.StandingPoints)
		{
			if (standingPoint.UserAgent != null)
			{
				return 0f;
			}
		}
		if (guardSpawnPointData.PathDistanceRatio < MinimumGuardSpawnPathRatio)
		{
			return 0f;
		}
		float num = guardSpawnPointData.ClosestPointToPath.Distance(guardSpawnPointData.MissionObject.GameEntity.GlobalPosition.AsVec2);
		if (num < 3f)
		{
			return 0f;
		}
		float num2 = 0f;
		num2 = ((!(num > 5f)) ? MBMath.Map(num, 3f, 5f, 0f, 1f) : MBMath.Map(num, 5f, 30f, 1f, 0f));
		spawnRotation = guardSpawnPointData.ClosestPointToPath - guardSpawnPointData.MissionObject.GameEntity.GlobalPosition.AsVec2;
		return num2;
	}

	protected override void OnEndMission()
	{
		_nearbyLeftSideUsableMachinesCache = null;
		_nearbyRightSideUsableMachinesCache = null;
		_allTargetAgentPointOfInterest = null;
		_crossRoadAgentData = null;
		_startAndFinishPointPool = null;
	}

	public void InitializeBehavior()
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
		if (gameEntity != null)
		{
			NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
			_disabledFaceId = firstScriptOfType.DisableFaceWithId;
		}
		_disguiseMissionLogic = Mission.Current.GetMissionBehavior<DisguiseMissionLogic>();
		Mission.Current.Scene.GetAllEntitiesWithScriptComponent<PassageUsePoint>(ref _startAndFinishPointPool);
		for (int num = _startAndFinishPointPool.Count - 1; num >= 0; num--)
		{
			switch (_startAndFinishPointPool[num].GetFirstScriptOfType<PassageUsePoint>().ToLocation?.StringId)
			{
			case null:
			case "lordshall":
			case "prison":
				_startAndFinishPointPool.RemoveAt(num);
				break;
			}
		}
		Mission.Current.Scene.GetAllEntitiesWithScriptComponent<CastleGate>(ref _startAndFinishPointPool);
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("sp_player_conversation"))
		{
			_startAndFinishPointPool.Add(item);
		}
	}

	public override void OnMissionTick(float dt)
	{
	}

	private void FadeOutUserAgentsInUsableMachine(UsableMachine usableMachine)
	{
		if (usableMachine.PilotAgent != null)
		{
			usableMachine.PilotAgent.FadeOut(hideInstantly: true, hideMount: true);
		}
		foreach (StandingPoint standingPoint in usableMachine.StandingPoints)
		{
			if (standingPoint.UserAgent != null)
			{
				standingPoint.UserAgent.FadeOut(hideInstantly: true, hideMount: true);
			}
		}
		usableMachine.SetDisabled(isParentObject: true);
	}

	private PointOfInterestScorePair CreatePathScorePair(NavigationPathData pathData)
	{
		List<VisitPointNodeScoreData> list = GetVisitPoints(pathData);
		List<CrossRoadScoreData> list2 = GetCrossRoadPoints(pathData);
		if (list.Count == 0 && list2.Count == 0)
		{
			return null;
		}
		List<PointOfInterestBaseData> list3 = new List<PointOfInterestBaseData>();
		list.Shuffle();
		list2.Shuffle();
		if (list2.Count > 20)
		{
			list2 = list2.OrderByDescending((CrossRoadScoreData x) => x.Score).Take(20).ToList();
			list2.Shuffle();
		}
		if (list.Count > 10)
		{
			list = list.OrderByDescending((VisitPointNodeScoreData x) => x.Score).Take(10).ToList();
			list.Shuffle();
		}
		list3.AddRange(list);
		list3.AddRange(list2);
		list3.Shuffle();
		Stack<(PointOfInterestScorePair, int)> stack = new Stack<(PointOfInterestScorePair, int)>();
		stack.Push((new PointOfInterestScorePair(pathData, new List<PointOfInterestBaseData>(), 0f), 0));
		return CreatePathDataWith(stack, list3);
	}

	private PointOfInterestScorePair CreatePathDataWith(Stack<(PointOfInterestScorePair, int)> stack, List<PointOfInterestBaseData> pointOfInterestData)
	{
		PointOfInterestScorePair pointOfInterestScorePair = null;
		while (stack.Count > 0)
		{
			(PointOfInterestScorePair, int) tuple = stack.Pop();
			int i;
			for (i = tuple.Item2; i < pointOfInterestData.Count; i++)
			{
				PointOfInterestBaseData data = pointOfInterestData[i];
				if (tuple.Item1.Data.All((PointOfInterestBaseData x) => !x.IsInRadius(data)))
				{
					PointOfInterestScorePair pointOfInterestScorePair2 = tuple.Item1.Clone();
					pointOfInterestScorePair2.AddToData(data);
					if (i + 1 < pointOfInterestData.Count)
					{
						stack.Push((tuple.Item1, i + 1));
						stack.Push((pointOfInterestScorePair2, i + 1));
					}
					if (pointOfInterestScorePair == null || pointOfInterestScorePair2.IsBetterThan(pointOfInterestScorePair))
					{
						pointOfInterestScorePair = pointOfInterestScorePair2;
					}
					if (pointOfInterestScorePair2.IsSufficient())
					{
						return pointOfInterestScorePair2;
					}
					i++;
					tuple = (pointOfInterestScorePair2, i);
					break;
				}
			}
			if (i == pointOfInterestData.Count && tuple.Item1.IsSufficient())
			{
				return tuple.Item1;
			}
		}
		return pointOfInterestScorePair;
	}

	private PointOfInterestScorePair GetRandomPath()
	{
		PointOfInterestScorePair pathInternal = GetPathInternal();
		if (pathInternal != null)
		{
			if (MaximumStandingGuardCountInPath > 0)
			{
				AddStandingGuardsToThePath(pathInternal);
			}
			if (MaximumLookBackPointCountInPath > 0)
			{
				AddLookBackPointsToThePath(pathInternal);
			}
		}
		return pathInternal;
	}

	private void AddLookBackPointsToThePath(PointOfInterestScorePair path)
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		for (int i = (int)((float)path.PathData.Path.Size * 0.25f); (float)i < (float)path.PathData.Path.Size * 0.9f; i++)
		{
			Vec2 key = path.PathData.Path[i];
			Vec2 key2 = path.PathData.Path[i + 1];
			if (!key.IsNonZero() || !key2.IsNonZero())
			{
				continue;
			}
			float num = path.PathData.PathNodeAndDistances[key] / path.PathData.TotalDistance;
			float num2 = path.PathData.PathNodeAndDistances[key2] / path.PathData.TotalDistance;
			float num3 = (num + num2) * 0.5f;
			float num4 = 0f;
			int num5 = 0;
			foreach (PointOfInterestBaseData datum in path.Data)
			{
				float locationRatio = datum.GetLocationRatio();
				if (locationRatio > num3 - 0.1f && locationRatio < num3 + 0.1f)
				{
					num4 += Math.Abs(num3 - locationRatio);
					num5++;
				}
			}
			if (num5 > 0)
			{
				num4 /= (float)num5;
			}
			dictionary.Add(i, num4);
		}
		if (!dictionary.Any())
		{
			return;
		}
		List<KeyValuePair<int, float>> list = dictionary.OrderByDescending((KeyValuePair<int, float> x) => x.Value).ToList();
		int num6 = 0;
		int num7 = ((MaximumLookBackPointCountInPath > 0) ? MBRandom.RandomInt((int)((float)MaximumLookBackPointCountInPath * 0.5f), MaximumLookBackPointCountInPath) : 0);
		if (num7 <= 0)
		{
			return;
		}
		_tempWorldPosition = new WorldPosition(Mission.Current.Scene, path.PathData.StartingGameEntity.GlobalPosition);
		_tempWorldPosition.GetNavMeshZ();
		for (int num8 = 0; num8 < path.PathData.Path.Size - 1; num8++)
		{
			Vec2 vec = path.PathData.Path[num8];
			Vec2 vec2 = path.PathData.Path[num8 + 1];
			_tempWorldPosition.SetVec2(vec);
			_tempWorldPosition.GetNavMeshZ();
			_tempWorldPosition.SetVec2(vec2);
			_tempWorldPosition.GetNavMeshZ();
			if (num6 == num7)
			{
				continue;
			}
			foreach (KeyValuePair<int, float> item in list)
			{
				int key3 = item.Key;
				if (num8 == key3)
				{
					Vec2 vec3 = (vec + vec2) * 0.5f;
					float pathDistanceRatio = (path.PathData.PathNodeAndDistances[vec] / path.PathData.TotalDistance + path.PathData.PathNodeAndDistances[vec2] / path.PathData.TotalDistance) * 0.5f;
					Vec2 vec4 = vec3 + (vec2 - vec).Normalized();
					_tempWorldPosition.SetVec2(vec3);
					_tempWorldPosition.GetNavMeshZ();
					WorldPosition tempWorldPosition = _tempWorldPosition;
					_tempWorldPosition.SetVec2(vec4);
					_tempWorldPosition.GetNavMeshZ();
					WorldPosition tempWorldPosition2 = _tempWorldPosition;
					LookBackPointData newData = new LookBackPointData(tempWorldPosition, tempWorldPosition2, pathDistanceRatio);
					if (path.Data.All((PointOfInterestBaseData x) => !x.IsInRadius(newData)))
					{
						path.AddToData(newData);
						num6++;
					}
					if (num6 == num7)
					{
						break;
					}
				}
			}
		}
	}

	private void AddStandingGuardsToThePath(PointOfInterestScorePair path)
	{
		int value = (int)(path.PathData.TotalDistance / 10f);
		value = MBMath.ClampInt(value, MinimumStandingGuardCountInPath, MaximumStandingGuardCountInPath);
		List<StandingGuardSpawnData> guardSpawnPoints = GetGuardSpawnPoints(path.PathData);
		int num = 0;
		for (int i = 0; i < guardSpawnPoints.Count; i++)
		{
			StandingGuardSpawnData randomElementWithPredicate = guardSpawnPoints.GetRandomElementWithPredicate((StandingGuardSpawnData x) => path.Data.All((PointOfInterestBaseData y) => !y.IsInRadius(x)));
			if (randomElementWithPredicate != null)
			{
				path.AddToData(randomElementWithPredicate);
				num++;
				if (num >= value)
				{
					break;
				}
			}
		}
	}

	public List<PointOfInterestScorePair> GetAllPossiblePaths()
	{
		List<PointOfInterestScorePair> list = new List<PointOfInterestScorePair>();
		List<UsableMachine> usablePoints = base.Mission.GetMissionBehavior<MissionAgentHandler>().UsablePoints;
		for (int i = 0; i < _startAndFinishPointPool.Count - 1; i++)
		{
			for (int j = i + 1; j < _startAndFinishPointPool.Count; j++)
			{
				GameEntity gameEntity = _startAndFinishPointPool[i];
				GameEntity gameEntity2 = _startAndFinishPointPool[j];
				NavigationPathData navigationPathData = new NavigationPathData(usablePoints, gameEntity, gameEntity2, _disabledFaceId);
				_tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity.GlobalPosition);
				_tempWorldPosition.GetNavMeshZ();
				if (navigationPathData.TotalDistance < (float)MaximumPathDistance && navigationPathData.TotalDistance > (float)MinimumPathDistance)
				{
					PointOfInterestScorePair pointOfInterestScorePair = CreatePathScorePair(navigationPathData);
					if (pointOfInterestScorePair != null && pointOfInterestScorePair.Score > (float)ScoreToAchieve)
					{
						if (MaximumStandingGuardCountInPath > 0)
						{
							AddStandingGuardsToThePath(pointOfInterestScorePair);
						}
						if (MaximumLookBackPointCountInPath > 0)
						{
							AddLookBackPointsToThePath(pointOfInterestScorePair);
						}
						list.Add(pointOfInterestScorePair);
					}
				}
				NavigationPathData navigationPathData2 = navigationPathData.ReverseClone();
				_tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity2.GlobalPosition);
				_tempWorldPosition.GetNavMeshZ();
				if (!(navigationPathData2.TotalDistance < (float)MaximumPathDistance) || !(navigationPathData2.TotalDistance > (float)MinimumPathDistance))
				{
					continue;
				}
				PointOfInterestScorePair pointOfInterestScorePair2 = CreatePathScorePair(navigationPathData2);
				if (pointOfInterestScorePair2 != null && pointOfInterestScorePair2.Score > (float)ScoreToAchieve)
				{
					if (MaximumStandingGuardCountInPath > 0)
					{
						AddStandingGuardsToThePath(pointOfInterestScorePair2);
					}
					if (MaximumLookBackPointCountInPath > 0)
					{
						AddLookBackPointsToThePath(pointOfInterestScorePair2);
					}
					list.Add(pointOfInterestScorePair2);
				}
			}
		}
		return list;
	}

	public bool IsOnLeftSide(Vec2 lineA, Vec2 lineB, Vec2 point)
	{
		return (lineB.x - lineA.x) * (point.y - lineA.y) - (lineB.y - lineA.y) * (point.x - lineA.x) > 0f;
	}

	private PointOfInterestScorePair GetPathInternal()
	{
		List<UsableMachine> usablePoints = base.Mission.GetMissionBehavior<MissionAgentHandler>().UsablePoints;
		PointOfInterestScorePair pointOfInterestScorePair = null;
		for (int i = 0; i < _startAndFinishPointPool.Count - 1; i++)
		{
			for (int j = i + 1; j < _startAndFinishPointPool.Count; j++)
			{
				GameEntity gameEntity = _startAndFinishPointPool[i];
				GameEntity gameEntity2 = _startAndFinishPointPool[j];
				NavigationPathData navigationPathData = new NavigationPathData(usablePoints, gameEntity, gameEntity2, _disabledFaceId);
				_tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity.GlobalPosition);
				_tempWorldPosition.GetNavMeshZ();
				if (navigationPathData.TotalDistance < (float)MaximumPathDistance && navigationPathData.TotalDistance > (float)MinimumPathDistance)
				{
					PointOfInterestScorePair pointOfInterestScorePair2 = CreatePathScorePair(navigationPathData);
					if (pointOfInterestScorePair2 != null)
					{
						if (pointOfInterestScorePair2.IsSufficient())
						{
							_currentStarting = gameEntity;
							_currentEnding = gameEntity2;
							return pointOfInterestScorePair2;
						}
						if (pointOfInterestScorePair == null || pointOfInterestScorePair2.IsBetterThan(pointOfInterestScorePair))
						{
							pointOfInterestScorePair = pointOfInterestScorePair2;
						}
					}
				}
				NavigationPathData navigationPathData2 = navigationPathData.ReverseClone();
				_tempWorldPosition = new WorldPosition(Mission.Current.Scene, gameEntity2.GlobalPosition);
				_tempWorldPosition.GetNavMeshZ();
				if (!(navigationPathData2.TotalDistance < (float)MaximumPathDistance) || !(navigationPathData2.TotalDistance > (float)MinimumPathDistance))
				{
					continue;
				}
				PointOfInterestScorePair pointOfInterestScorePair3 = CreatePathScorePair(navigationPathData2);
				if (pointOfInterestScorePair3 != null)
				{
					if (pointOfInterestScorePair3.IsSufficient())
					{
						_currentStarting = gameEntity2;
						_currentEnding = gameEntity;
						return pointOfInterestScorePair3;
					}
					if (pointOfInterestScorePair == null || pointOfInterestScorePair3.IsBetterThan(pointOfInterestScorePair))
					{
						pointOfInterestScorePair = pointOfInterestScorePair3;
					}
				}
			}
		}
		if (pointOfInterestScorePair != null)
		{
			_currentStarting = pointOfInterestScorePair.PathData.StartingGameEntity;
			_currentEnding = pointOfInterestScorePair.PathData.EndingGameEntity;
		}
		return pointOfInterestScorePair;
	}

	private List<StandingGuardSpawnData> GetGuardSpawnPoints(NavigationPathData pathData)
	{
		List<StandingGuardSpawnData> list = new List<StandingGuardSpawnData>();
		foreach (UsableMachineData validUsableMachinesDatum in pathData.ValidUsableMachinesData)
		{
			Vec2 spawnRotation;
			float num = CalculateSpawnGuardScore(validUsableMachinesDatum, out spawnRotation);
			if (num > 0f)
			{
				list.Add(new StandingGuardSpawnData(validUsableMachinesDatum, spawnRotation, num));
			}
		}
		return list;
	}

	private List<VisitPointNodeScoreData> GetVisitPoints(NavigationPathData pathData)
	{
		List<VisitPointNodeScoreData> list = new List<VisitPointNodeScoreData>();
		NavigationPath path = pathData.Path;
		for (int i = 0; i < path.Size - 1; i++)
		{
			Vec2 vec = path[i];
			Vec2 vec2 = path[i + 1];
			_tempWorldPosition.SetVec2(vec);
			_tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition = _tempWorldPosition;
			_tempWorldPosition.SetVec2(vec2);
			_tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition2 = _tempWorldPosition;
			foreach (UsableMachineData validUsableMachinesDatum in pathData.ValidUsableMachinesData)
			{
				if (!validUsableMachinesDatum.IsAlreadyAddedToPath)
				{
					Vec3 possibleBlendPointPosition;
					float startingAngle;
					Vec2 pathToVisitPointZero;
					Vec2 closestPointToPath;
					float num = CalculateVisitPointScore(validUsableMachinesDatum, path, tempWorldPosition, tempWorldPosition2, out possibleBlendPointPosition, out startingAngle, out pathToVisitPointZero, out closestPointToPath);
					if (num > 0f)
					{
						Vec2 vec3 = vec + (vec2 - vec) * 0.5f;
						_tempWorldPosition.SetVec2(vec);
						_tempWorldPosition.GetNavMeshZ();
						_tempWorldPosition.SetVec2(vec3);
						_tempWorldPosition.GetNavMeshZ();
						WorldPosition tempWorldPosition3 = _tempWorldPosition;
						_tempWorldPosition.SetVec2(vec2);
						_tempWorldPosition.GetNavMeshVec3();
						_tempWorldPosition.SetVec2(possibleBlendPointPosition.AsVec2);
						_tempWorldPosition.GetNavMeshZ();
						WorldPosition tempWorldPosition4 = _tempWorldPosition;
						_tempWorldPosition.SetVec2(closestPointToPath);
						_tempWorldPosition.GetNavMeshZ();
						WorldPosition tempWorldPosition5 = _tempWorldPosition;
						float visitPointPathStartPointPathRatio = (pathData.PathNodeAndDistances[vec] / pathData.TotalDistance + pathData.PathNodeAndDistances[vec2] / pathData.TotalDistance) * 0.5f;
						list.Add(new VisitPointNodeScoreData(validUsableMachinesDatum, tempWorldPosition4, tempWorldPosition3, visitPointPathStartPointPathRatio, num, startingAngle, tempWorldPosition, tempWorldPosition2, pathToVisitPointZero, tempWorldPosition5));
						validUsableMachinesDatum.IsAlreadyAddedToPath = true;
					}
				}
			}
		}
		return list;
	}

	private List<CrossRoadScoreData> GetCrossRoadPoints(NavigationPathData pathData)
	{
		List<CrossRoadScoreData> list = new List<CrossRoadScoreData>();
		for (int i = 0; i < pathData.Path.Size - 1; i++)
		{
			_nearbyLeftSideUsableMachinesCache.Clear();
			_nearbyRightSideUsableMachinesCache.Clear();
			Vec2 vec = pathData.Path[i];
			Vec2 vec2 = pathData.Path[i + 1];
			_tempWorldPosition.SetVec2(vec);
			_tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition = _tempWorldPosition;
			_tempWorldPosition.SetVec2(vec2);
			_tempWorldPosition.GetNavMeshZ();
			WorldPosition tempWorldPosition2 = _tempWorldPosition;
			float num = vec2.DistanceSquared(vec);
			if (!(num > 25f) || !(num < 100f))
			{
				continue;
			}
			foreach (UsableMachineData validUsableMachinesDatum in pathData.ValidUsableMachinesData)
			{
				if (!validUsableMachinesDatum.IsAlreadyAddedToPath)
				{
					if (IsOnLeftSide(vec, vec2, validUsableMachinesDatum.MissionObject.GameEntity.GlobalPosition.AsVec2))
					{
						_nearbyLeftSideUsableMachinesCache.Add(validUsableMachinesDatum);
					}
					else
					{
						_nearbyRightSideUsableMachinesCache.Add(validUsableMachinesDatum);
					}
				}
			}
			foreach (UsableMachineData item in _nearbyLeftSideUsableMachinesCache)
			{
				foreach (UsableMachineData item2 in _nearbyRightSideUsableMachinesCache)
				{
					item.MissionObject.GameEntity.GlobalPosition.Distance(item2.MissionObject.GameEntity.GlobalPosition);
					if (!item.IsAlreadyAddedToPath && !item2.IsAlreadyAddedToPath)
					{
						float num2 = CalculateCrossRoadScoreForUsableMachines(item, item2, pathData.Path, tempWorldPosition, tempWorldPosition2);
						if (num2 > 0f)
						{
							list.Add(new CrossRoadScoreData(item, item2, num2));
							item.IsAlreadyAddedToPath = true;
							item2.IsAlreadyAddedToPath = true;
						}
					}
				}
			}
		}
		return list;
	}

	private void ShowMissionFailedPopup()
	{
		TextObject textObject = new TextObject("{=CMu4B9fZ}Mission Failed");
		TextObject textObject2 = new TextObject("{=RcY8uZA1}You have lost the target.");
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: delegate
		{
			Mission.Current.EndMission();
		}, negativeAction: null), Campaign.Current.GameMode == CampaignGameMode.Campaign);
	}
}
