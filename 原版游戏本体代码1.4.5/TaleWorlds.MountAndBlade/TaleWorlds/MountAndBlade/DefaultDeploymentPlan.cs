using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefaultDeploymentPlan
{
	public const float VerticalFormationGap = 3f;

	public const float HorizontalFormationGap = 2f;

	public const float MaxSafetyScore = 100f;

	public readonly Team Team;

	public readonly bool IsReinforcement;

	public readonly SpawnPathData SpawnPathData;

	private readonly Mission _mission;

	private int _planCount;

	private bool _spawnWithHorses;

	private readonly int[] _formationMountedTroopCounts;

	private readonly int[] _formationFootTroopCounts;

	private Vec3 _meanPosition;

	private readonly DefaultFormationDeploymentPlan[] _formationPlans;

	private readonly SortedList<FormationDeploymentOrder, DefaultFormationDeploymentPlan>[] _deploymentFlanks = new SortedList<FormationDeploymentOrder, DefaultFormationDeploymentPlan>[4];

	public bool SpawnWithHorses => _spawnWithHorses;

	public int PlanCount => _planCount;

	public bool IsPlanMade { get; private set; }

	public float SpawnPathOffset { get; private set; }

	public float TargetOffset { get; private set; }

	public bool IsSafeToDeploy => SafetyScore >= 50f;

	public float SafetyScore { get; private set; }

	public int FootTroopCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < 11; i++)
			{
				num += _formationFootTroopCounts[i];
			}
			return num;
		}
	}

	public int MountedTroopCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < 11; i++)
			{
				num += _formationMountedTroopCounts[i];
			}
			return num;
		}
	}

	public int TroopCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < 11; i++)
			{
				num += _formationFootTroopCounts[i] + _formationMountedTroopCounts[i];
			}
			return num;
		}
	}

	public Vec3 MeanPosition => _meanPosition;

	public static DefaultDeploymentPlan CreateInitialPlan(Mission mission, Team team)
	{
		return new DefaultDeploymentPlan(mission, team, isReinforcement: false, null);
	}

	public static DefaultDeploymentPlan CreateReinforcementPlan(Mission mission, Team team)
	{
		return new DefaultDeploymentPlan(mission, team, isReinforcement: true, null);
	}

	public static DefaultDeploymentPlan CreateReinforcementPlanWithSpawnPath(Mission mission, Team team, SpawnPathData spawnPathData)
	{
		return new DefaultDeploymentPlan(mission, team, isReinforcement: true, spawnPathData);
	}

	private DefaultDeploymentPlan(Mission mission, Team team, bool isReinforcement, SpawnPathData spawnPathData)
	{
		_mission = mission;
		_planCount = 0;
		Team = team;
		IsReinforcement = isReinforcement;
		SpawnPathData = spawnPathData;
		int num = 11;
		_formationPlans = new DefaultFormationDeploymentPlan[num];
		_formationFootTroopCounts = new int[num];
		_formationMountedTroopCounts = new int[num];
		_meanPosition = Vec3.Zero;
		IsPlanMade = false;
		SpawnPathOffset = 0f;
		TargetOffset = 0f;
		SafetyScore = 100f;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			FormationClass fClass = (FormationClass)i;
			_formationPlans[i] = new DefaultFormationDeploymentPlan(fClass);
		}
		for (int j = 0; j < 4; j++)
		{
			_deploymentFlanks[j] = new SortedList<FormationDeploymentOrder, DefaultFormationDeploymentPlan>(FormationDeploymentOrder.GetComparer());
		}
		ClearAddedTroops();
		ClearPlan();
	}

	public void SetSpawnWithHorses(bool value)
	{
		_spawnWithHorses = value;
	}

	public void MakeDeploymentPlan(float spawnPathOffset = 0f, float targetOffset = 0f, FormationSceneSpawnEntry[,] formationSceneSpawnEntries = null)
	{
		SpawnPathOffset = spawnPathOffset;
		TargetOffset = targetOffset;
		PlanFormationDimensions();
		if (_mission.HasSpawnPath)
		{
			PlanFieldBattleDeploymentFromSpawnPath(spawnPathOffset, targetOffset);
		}
		else if (_mission.IsFieldBattle)
		{
			PlanFieldBattleDeploymentFromSceneData(formationSceneSpawnEntries);
		}
		else
		{
			PlanBattleDeploymentFromSceneData(formationSceneSpawnEntries);
		}
		ComputeMeanPosition();
	}

	public void ClearPlan()
	{
		DefaultFormationDeploymentPlan[] formationPlans = _formationPlans;
		for (int i = 0; i < formationPlans.Length; i++)
		{
			formationPlans[i].Clear();
		}
		SortedList<FormationDeploymentOrder, DefaultFormationDeploymentPlan>[] deploymentFlanks = _deploymentFlanks;
		for (int i = 0; i < deploymentFlanks.Length; i++)
		{
			deploymentFlanks[i].Clear();
		}
		IsPlanMade = false;
	}

	public void ClearAddedTroops()
	{
		for (int i = 0; i < 11; i++)
		{
			_formationFootTroopCounts[i] = 0;
			_formationMountedTroopCounts[i] = 0;
		}
	}

	public void AddTroops(FormationClass formationClass, int footTroopCount, int mountedTroopCount)
	{
		if (footTroopCount + mountedTroopCount > 0 && formationClass < FormationClass.NumberOfAllFormationsWithUnset)
		{
			_formationFootTroopCounts[(int)formationClass] += footTroopCount;
			_formationMountedTroopCounts[(int)formationClass] += mountedTroopCount;
		}
	}

	public DefaultFormationDeploymentPlan GetFormationPlan(FormationClass fClass)
	{
		return _formationPlans[(int)fClass];
	}

	public bool GetFormationDeploymentFrame(FormationClass fClass, out MatrixFrame frame)
	{
		DefaultFormationDeploymentPlan formationPlan = GetFormationPlan(fClass);
		if (formationPlan.HasFrame())
		{
			frame = formationPlan.GetFrame();
			return true;
		}
		frame = MatrixFrame.Identity;
		return false;
	}

	public bool GetFirstValidFormationDeploymentFrame(out MatrixFrame frame)
	{
		DefaultFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (DefaultFormationDeploymentPlan defaultFormationDeploymentPlan in formationPlans)
		{
			if (defaultFormationDeploymentPlan.HasFrame())
			{
				frame = defaultFormationDeploymentPlan.GetFrame();
				return true;
			}
		}
		frame = MatrixFrame.Identity;
		return false;
	}

	public bool IsPlanSuitableForFormations((int, int)[] troopDataPerFormationClass)
	{
		if (troopDataPerFormationClass.Length == 11)
		{
			for (int i = 0; i < 11; i++)
			{
				FormationClass fClass = (FormationClass)i;
				DefaultFormationDeploymentPlan formationPlan = GetFormationPlan(fClass);
				(int, int) tuple = troopDataPerFormationClass[i];
				if (formationPlan.PlannedFootTroopCount != tuple.Item1 || formationPlan.PlannedMountedTroopCount != tuple.Item2)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public void UpdateSafetyScore()
	{
		if (_mission.Teams == null)
		{
			return;
		}
		float num = 100f;
		Team team = ((Team.Side == BattleSideEnum.Attacker) ? _mission.Teams.Defender : ((Team.Side == BattleSideEnum.Defender) ? _mission.Teams.Attacker : null));
		if (team != null)
		{
			foreach (Formation item in team.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					float num2 = _meanPosition.AsVec2.Distance(item.CachedAveragePosition);
					if (num >= num2)
					{
						num = num2;
					}
				}
			}
		}
		team = ((Team.Side == BattleSideEnum.Attacker) ? _mission.Teams.DefenderAlly : ((Team.Side == BattleSideEnum.Defender) ? _mission.Teams.AttackerAlly : null));
		if (team != null)
		{
			foreach (Formation item2 in team.FormationsIncludingEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					float num3 = _meanPosition.AsVec2.Distance(item2.CachedAveragePosition);
					if (num >= num3)
					{
						num = num3;
					}
				}
			}
		}
		SafetyScore = num;
	}

	public WorldFrame GetFrameFromFormationSpawnEntity(GameEntity formationSpawnEntity, float depthOffset = 0f)
	{
		MatrixFrame globalFrame = formationSpawnEntity.GetGlobalFrame();
		globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		WorldPosition worldPosition = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalFrame.origin, hasValidZ: false);
		WorldPosition origin = worldPosition;
		if (depthOffset != 0f)
		{
			origin.SetVec2(origin.AsVec2 - depthOffset * globalFrame.rotation.f.AsVec2);
			if (!origin.IsValid || origin.GetNavMesh() == UIntPtr.Zero)
			{
				origin = worldPosition;
			}
		}
		return new WorldFrame(globalFrame.rotation, origin);
	}

	private void PlanFieldBattleDeploymentFromSpawnPath(float pathOffset, float targetOffset)
	{
		bool teamPlanHasAnyFootTroops = FootTroopCount > 0;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			FormationClass formationClass = (FormationClass)i;
			int num = _formationFootTroopCounts[i] + _formationMountedTroopCounts[i];
			if (num > 0 || (!IsReinforcement && (formationClass == FormationClass.NumberOfRegularFormations || formationClass == FormationClass.Bodyguard)))
			{
				DefaultFormationDeploymentPlan defaultFormationDeploymentPlan = _formationPlans[i];
				FormationDeploymentFlank defaultFlank = defaultFormationDeploymentPlan.GetDefaultFlank(num, teamPlanHasAnyFootTroops, _spawnWithHorses);
				int offset = ((num <= 0 && formationClass != FormationClass.NumberOfRegularFormations) ? 1 : 0);
				FormationDeploymentOrder flankDeploymentOrder = defaultFormationDeploymentPlan.GetFlankDeploymentOrder(offset);
				_deploymentFlanks[(int)defaultFlank].Add(flankDeploymentOrder, defaultFormationDeploymentPlan);
			}
		}
		float horizontalCenterOffset = ComputeHorizontalCenterOffset();
		SpawnPathData initialSpawnPathData = _mission.GetInitialSpawnPathData(Team.Side);
		Vec2 spawnPathPosition;
		Vec2 spawnPathDirection;
		if (IsReinforcement)
		{
			spawnPathPosition = SpawnPathData.GetSpawnFrame(SpawnPathData.GetBaseOffset(), searchNearestValidFrame: true, SpawnPathData.SearchDirection.Forward).origin.AsVec2;
			spawnPathDirection = (initialSpawnPathData.GetSpawnFrame(0f).origin.AsVec2 - spawnPathPosition).Normalized();
		}
		else
		{
			initialSpawnPathData.GetSpawnPathFrameFacingTarget(pathOffset, targetOffset, useTangentDirection: false, out spawnPathPosition, out spawnPathDirection, decideDirectionDynamically: true);
		}
		DeployFlanks(spawnPathPosition, spawnPathDirection, horizontalCenterOffset);
		SortedList<FormationDeploymentOrder, DefaultFormationDeploymentPlan>[] deploymentFlanks = _deploymentFlanks;
		for (int j = 0; j < deploymentFlanks.Length; j++)
		{
			deploymentFlanks[j].Clear();
		}
		IsPlanMade = true;
		_planCount++;
	}

	private void PlanFieldBattleDeploymentFromSceneData(FormationSceneSpawnEntry[,] formationSceneSpawnEntries)
	{
		if (formationSceneSpawnEntries == null || formationSceneSpawnEntries.GetLength(0) != 2 || formationSceneSpawnEntries.GetLength(1) != _formationPlans.Length)
		{
			return;
		}
		int side = (int)Team.Side;
		int num = ((Team.Side != BattleSideEnum.Attacker) ? 1 : 0);
		Dictionary<GameEntity, float> spawnDepths = new Dictionary<GameEntity, float>();
		bool flag = !IsReinforcement;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			DefaultFormationDeploymentPlan defaultFormationDeploymentPlan = _formationPlans[i];
			FormationSceneSpawnEntry formationSceneSpawnEntry = formationSceneSpawnEntries[side, i];
			FormationSceneSpawnEntry formationSceneSpawnEntry2 = formationSceneSpawnEntries[num, i];
			GameEntity gameEntity = (flag ? formationSceneSpawnEntry.SpawnEntity : formationSceneSpawnEntry.ReinforcementSpawnEntity);
			GameEntity gameEntity2 = (flag ? formationSceneSpawnEntry2.SpawnEntity : formationSceneSpawnEntry2.ReinforcementSpawnEntity);
			if (gameEntity != null && gameEntity2 != null)
			{
				defaultFormationDeploymentPlan.SetFrame(ComputeFieldBattleDeploymentFrameForFormation(defaultFormationDeploymentPlan, gameEntity, gameEntity2, ref spawnDepths));
			}
			else
			{
				defaultFormationDeploymentPlan.SetFrame(in WorldFrame.Invalid);
			}
			defaultFormationDeploymentPlan.SetSpawnClass(formationSceneSpawnEntry.FormationClass);
		}
		IsPlanMade = true;
		_planCount++;
	}

	private void PlanBattleDeploymentFromSceneData(FormationSceneSpawnEntry[,] formationSceneSpawnEntries)
	{
		if (formationSceneSpawnEntries == null || formationSceneSpawnEntries.GetLength(0) != 2 || formationSceneSpawnEntries.GetLength(1) != _formationPlans.Length)
		{
			return;
		}
		int side = (int)Team.Side;
		Dictionary<GameEntity, float> spawnDepths = new Dictionary<GameEntity, float>();
		bool flag = !IsReinforcement;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			DefaultFormationDeploymentPlan defaultFormationDeploymentPlan = _formationPlans[i];
			FormationSceneSpawnEntry formationSceneSpawnEntry = formationSceneSpawnEntries[side, i];
			GameEntity gameEntity = (flag ? formationSceneSpawnEntry.SpawnEntity : formationSceneSpawnEntry.ReinforcementSpawnEntity);
			if (gameEntity != null)
			{
				float andUpdateSpawnDepth = GetAndUpdateSpawnDepth(ref spawnDepths, gameEntity, defaultFormationDeploymentPlan);
				defaultFormationDeploymentPlan.SetFrame(GetFrameFromFormationSpawnEntity(gameEntity, andUpdateSpawnDepth));
			}
			else
			{
				defaultFormationDeploymentPlan.SetFrame(in WorldFrame.Invalid);
			}
			defaultFormationDeploymentPlan.SetSpawnClass(formationSceneSpawnEntry.FormationClass);
		}
		IsPlanMade = true;
		_planCount++;
	}

	private void PlanFormationDimensions()
	{
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			int num = _formationFootTroopCounts[i];
			int num2 = _formationMountedTroopCounts[i];
			int num3 = num + num2;
			DefaultFormationDeploymentPlan defaultFormationDeploymentPlan = _formationPlans[i];
			if (num3 > 0)
			{
				bool hasMountedTroops = DefaultMissionDeploymentPlan.HasSignificantMountedTroops(num, num2);
				var (width, depth) = GetFormationSpawnWidthAndDepth(defaultFormationDeploymentPlan.Class, num3, hasMountedTroops, !_spawnWithHorses);
				defaultFormationDeploymentPlan.SetPlannedDimensions(width, depth);
				defaultFormationDeploymentPlan.SetPlannedTroopCount(num, num2);
			}
		}
	}

	private void DeployFlanks(Vec2 deployPosition, Vec2 deployDirection, float horizontalCenterOffset)
	{
		(float flankWidth, float flankDepth) tuple = PlanFlankDeployment(FormationDeploymentFlank.Front, deployPosition, deployDirection, 0f, horizontalCenterOffset);
		float item = tuple.flankWidth;
		float item2 = tuple.flankDepth;
		item2 += 3f;
		float item3 = PlanFlankDeployment(FormationDeploymentFlank.Rear, deployPosition, deployDirection, item2, horizontalCenterOffset).flankWidth;
		float num = TaleWorlds.Library.MathF.Max(item, item3);
		float num2 = ComputeFlankDepth(FormationDeploymentFlank.Front, countPositiveNumTroops: true);
		num2 += 3f;
		float num3 = ComputeFlankWidth(FormationDeploymentFlank.Left);
		float horizontalOffset = horizontalCenterOffset + 2f + 0.5f * (num + num3);
		PlanFlankDeployment(FormationDeploymentFlank.Left, deployPosition, deployDirection, num2, horizontalOffset);
		float num4 = ComputeFlankWidth(FormationDeploymentFlank.Right);
		float horizontalOffset2 = horizontalCenterOffset - (2f + 0.5f * (num + num4));
		PlanFlankDeployment(FormationDeploymentFlank.Right, deployPosition, deployDirection, num2, horizontalOffset2);
	}

	private void ComputeMeanPosition()
	{
		_meanPosition = Vec3.Zero;
		Vec2 zero = Vec2.Zero;
		int num = 0;
		DefaultFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (DefaultFormationDeploymentPlan defaultFormationDeploymentPlan in formationPlans)
		{
			if (defaultFormationDeploymentPlan.HasFrame())
			{
				zero += defaultFormationDeploymentPlan.GetPosition().AsVec2;
				num++;
			}
		}
		if (num > 0)
		{
			zero = new Vec2(zero.X / (float)num, zero.Y / (float)num);
			float height = 0f;
			Mission.Current.Scene.GetHeightAtPoint(zero, BodyFlags.None, ref height);
			_meanPosition = new Vec3(zero, height);
		}
	}

	private (float flankWidth, float flankDepth) PlanFlankDeployment(FormationDeploymentFlank flankFlank, Vec2 deployPosition, Vec2 deployDirection, float verticalOffset = 0f, float horizontalOffset = 0f)
	{
		Mat3 identity = Mat3.Identity;
		identity.RotateAboutUp(deployDirection.RotationInRadians);
		float num = 0f;
		float num2 = 0f;
		Vec2 vec = deployDirection.LeftVec();
		WorldPosition position = new WorldPosition(_mission.Scene, UIntPtr.Zero, deployPosition.ToVec3(), hasValidZ: false);
		foreach (KeyValuePair<FormationDeploymentOrder, DefaultFormationDeploymentPlan> item in _deploymentFlanks[(int)flankFlank])
		{
			DefaultFormationDeploymentPlan value = item.Value;
			Vec2 destination = position.AsVec2 - (num2 + verticalOffset) * deployDirection + horizontalOffset * vec;
			Vec3 lastPointOnNavigationMeshFromWorldPositionToDestination = _mission.Scene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref position, destination);
			value.SetFrame(new WorldFrame(origin: new WorldPosition(_mission.Scene, UIntPtr.Zero, lastPointOnNavigationMeshFromWorldPositionToDestination, hasValidZ: false), rotation: identity));
			float num3 = value.PlannedDepth + 3f;
			num2 += num3;
			num = TaleWorlds.Library.MathF.Max(num, value.PlannedWidth);
		}
		num2 = TaleWorlds.Library.MathF.Max(num2 - 3f, 0f);
		return (flankWidth: num, flankDepth: num2);
	}

	private WorldFrame ComputeFieldBattleDeploymentFrameForFormation(DefaultFormationDeploymentPlan formationPlan, GameEntity formationSceneEntity, GameEntity counterSideFormationSceneEntity, ref Dictionary<GameEntity, float> spawnDepths)
	{
		Vec3 globalPosition = formationSceneEntity.GlobalPosition;
		Vec2 asVec = (counterSideFormationSceneEntity.GlobalPosition - globalPosition).AsVec2;
		asVec.Normalize();
		float andUpdateSpawnDepth = GetAndUpdateSpawnDepth(ref spawnDepths, formationSceneEntity, formationPlan);
		WorldPosition origin = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalPosition, hasValidZ: false);
		origin.SetVec2(origin.AsVec2 - andUpdateSpawnDepth * asVec);
		Mat3 identity = Mat3.Identity;
		identity.RotateAboutUp(asVec.RotationInRadians);
		return new WorldFrame(identity, origin);
	}

	private float ComputeFlankWidth(FormationDeploymentFlank flank)
	{
		float num = 0f;
		foreach (KeyValuePair<FormationDeploymentOrder, DefaultFormationDeploymentPlan> item in _deploymentFlanks[(int)flank])
		{
			num = TaleWorlds.Library.MathF.Max(num, item.Value.PlannedWidth);
		}
		return num;
	}

	private float ComputeFlankDepth(FormationDeploymentFlank flank, bool countPositiveNumTroops = false)
	{
		float num = 0f;
		foreach (KeyValuePair<FormationDeploymentOrder, DefaultFormationDeploymentPlan> item in _deploymentFlanks[(int)flank])
		{
			if (!countPositiveNumTroops)
			{
				num += item.Value.PlannedDepth + 3f;
			}
			else if (item.Value.PlannedTroopCount > 0)
			{
				num += item.Value.PlannedDepth + 3f;
			}
		}
		return num - 3f;
	}

	private float ComputeHorizontalCenterOffset()
	{
		float num = TaleWorlds.Library.MathF.Max(ComputeFlankWidth(FormationDeploymentFlank.Front), ComputeFlankWidth(FormationDeploymentFlank.Rear));
		float num2 = ComputeFlankWidth(FormationDeploymentFlank.Left);
		float num3 = ComputeFlankWidth(FormationDeploymentFlank.Right);
		float num4 = num / 2f + num2 + 2f;
		return (num / 2f + num3 + 2f - num4) / 2f;
	}

	private float GetAndUpdateSpawnDepth(ref Dictionary<GameEntity, float> spawnDepths, GameEntity spawnEntity, DefaultFormationDeploymentPlan formationPlan)
	{
		float value;
		bool num = spawnDepths.TryGetValue(spawnEntity, out value);
		float num2 = (formationPlan.HasDimensions ? (formationPlan.PlannedDepth + 3f) : 0f);
		if (!num)
		{
			value = 0f;
			spawnDepths[spawnEntity] = num2;
		}
		else if (formationPlan.HasDimensions)
		{
			spawnDepths[spawnEntity] = value + num2;
		}
		return value;
	}

	public static (float, float) GetFormationSpawnWidthAndDepth(FormationClass formationNo, int troopCount, bool hasMountedTroops, bool considerCavalryAsInfantry = false)
	{
		bool flag = !considerCavalryAsInfantry && hasMountedTroops;
		float defaultUnitDiameter = Formation.GetDefaultUnitDiameter(flag);
		int unitSpacingOf = ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.ArrangementOrderEnum.Line);
		float num = (flag ? Formation.CavalryInterval(unitSpacingOf) : Formation.InfantryInterval(unitSpacingOf));
		float num2 = (flag ? Formation.CavalryDistance(unitSpacingOf) : Formation.InfantryDistance(unitSpacingOf));
		float num3 = (float)TaleWorlds.Library.MathF.Max(0, troopCount - 1) * (num + defaultUnitDiameter) + defaultUnitDiameter;
		float num4 = (flag ? 18f : 9f);
		int b = (int)(num3 / TaleWorlds.Library.MathF.Sqrt(num4 * (float)troopCount + 1f));
		b = TaleWorlds.Library.MathF.Max(1, b);
		float num5 = (float)troopCount / (float)b;
		float item = TaleWorlds.Library.MathF.Max(0f, num5 - 1f) * (num + defaultUnitDiameter) + defaultUnitDiameter;
		float item2 = (float)(b - 1) * (num2 + defaultUnitDiameter) + defaultUnitDiameter;
		return (item, item2);
	}
}
