using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalDeploymentPlan
{
	public const float MaxHorizontalShipGap = 20f;

	public const float ShipRowGap = 20f;

	public const bool ForceSingleRowDeployment = true;

	public readonly Team Team;

	private readonly Mission _mission;

	private readonly NavalFormationDeploymentPlan[] _formationPlans;

	private int _planCount;

	private bool _isRiverPlan;

	private bool _isRaidPlan;

	private Vec3 _meanPosition;

	public int PlanCount => _planCount;

	public bool IsPlanMade { get; private set; }

	public float SpawnPathOffset { get; private set; }

	public float TargetOffset { get; private set; }

	public bool IsRiverPlan => _isRiverPlan;

	public bool IsRaidPlan => _isRaidPlan;

	public int ShipCount
	{
		get
		{
			int num = 0;
			NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
			for (int i = 0; i < formationPlans.Length; i++)
			{
				if (formationPlans[i].HasShipObject)
				{
					num++;
				}
			}
			return num;
		}
	}

	public int TroopCount
	{
		get
		{
			int num = 0;
			NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
			foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
			{
				num += navalFormationDeploymentPlan.PlannedTroopCount;
			}
			return num;
		}
	}

	public Vec3 MeanPosition => _meanPosition;

	public static NavalDeploymentPlan CreatePlan(Mission mission, Team team, bool isRiverPlan, bool isRaidPlan)
	{
		return new NavalDeploymentPlan(mission, team, isRiverPlan, isRaidPlan);
	}

	private NavalDeploymentPlan(Mission mission, Team team, bool isRiverPlan, bool isRaidPlan)
	{
		_mission = mission;
		_planCount = 0;
		Team = team;
		_formationPlans = new NavalFormationDeploymentPlan[11];
		_isRiverPlan = isRiverPlan;
		_isRaidPlan = isRaidPlan;
		IsPlanMade = false;
		SpawnPathOffset = 0f;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			FormationClass fClass = (FormationClass)i;
			_formationPlans[i] = new NavalFormationDeploymentPlan(fClass, _mission);
		}
		ClearAddedShips();
		ClearPlan();
	}

	public void AddShip(FormationClass formationClass, IShipOrigin shipOrigin)
	{
		_formationPlans[(int)formationClass].SetShipOrigin(shipOrigin);
	}

	public bool RemoveShip(FormationClass formationIndex)
	{
		NavalFormationDeploymentPlan navalFormationDeploymentPlan = _formationPlans[(int)formationIndex];
		if (navalFormationDeploymentPlan.ShipObject != null)
		{
			navalFormationDeploymentPlan.SetShipOrigin(null);
			return true;
		}
		return false;
	}

	public void ClearAddedShips()
	{
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		for (int i = 0; i < formationPlans.Length; i++)
		{
			formationPlans[i].SetShipOrigin(null);
		}
	}

	public void MakeDeploymentPlan(float spawnPathOffset, float targetOffset, FormationSceneSpawnEntry[,] formationSceneSpawnEntries = null)
	{
		SpawnPathOffset = spawnPathOffset;
		TargetOffset = targetOffset;
		if (_mission.HasSpawnPath)
		{
			PlanNavalBattleDeploymentFromSpawnPath(spawnPathOffset, targetOffset);
		}
		else
		{
			PlanNavalBattleDeploymentFromSceneData(formationSceneSpawnEntries);
		}
		ComputeMeanPosition();
	}

	public void ClearPlan()
	{
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		for (int i = 0; i < formationPlans.Length; i++)
		{
			formationPlans[i].Clear();
		}
		IsPlanMade = false;
	}

	public NavalFormationDeploymentPlan GetFormationPlan(FormationClass fClass)
	{
		return _formationPlans[(int)fClass];
	}

	public bool GetFirstValidFormationDeploymentFrame(out MatrixFrame frame)
	{
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
		{
			if (navalFormationDeploymentPlan.HasFrame())
			{
				frame = navalFormationDeploymentPlan.GetFrame();
				return true;
			}
		}
		frame = MatrixFrame.Identity;
		return false;
	}

	public bool GetFormationFrame(FormationClass fClass, out MatrixFrame frame)
	{
		NavalFormationDeploymentPlan formationPlan = GetFormationPlan(fClass);
		if (formationPlan.HasFrame())
		{
			frame = formationPlan.GetFrame();
			return true;
		}
		frame = MatrixFrame.Identity;
		return false;
	}

	public MatrixFrame ComputeFormationsCenterFrameAndExtents(bool ignoreDimensionlessFormations, out Vec2 halfExtents)
	{
		GetFirstValidFormationFrame(out var frame, checkDimensions: true);
		float num = float.MinValue;
		float num2 = float.MaxValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
		{
			if (navalFormationDeploymentPlan.HasFrame() && (!ignoreDimensionlessFormations || navalFormationDeploymentPlan.HasDimensions))
			{
				MatrixFrame matrixFrame = frame.TransformToLocal(navalFormationDeploymentPlan.GetFrame());
				float num5 = navalFormationDeploymentPlan.PlannedWidth * 0.5f;
				float num6 = navalFormationDeploymentPlan.PlannedDepth * 0.5f;
				float num7 = MathF.Abs(matrixFrame.rotation.s.x) * num5 + MathF.Abs(matrixFrame.rotation.f.x) * num6;
				float num8 = MathF.Abs(matrixFrame.rotation.s.y) * num5 + MathF.Abs(matrixFrame.rotation.f.y) * num6;
				float b = matrixFrame.origin.y + num8;
				float b2 = matrixFrame.origin.y - num8;
				float b3 = matrixFrame.origin.x - num7;
				float b4 = matrixFrame.origin.x + num7;
				num = MathF.Max(num, b);
				num2 = MathF.Min(num2, b2);
				num3 = MathF.Min(num3, b3);
				num4 = MathF.Max(num4, b4);
			}
		}
		float x = (num3 + num4) * 0.5f;
		float y = (num2 + num) * 0.5f;
		halfExtents = new Vec2((num4 - num3) / 2f, (num - num2) / 2f);
		Vec3 o = frame.TransformToParent(new Vec3(x, y));
		o.z = Mission.Current.Scene.GetWaterLevelAtPosition(o.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		return new MatrixFrame(in frame.rotation, in o);
	}

	public bool GetFirstValidFormationFrame(out MatrixFrame frame, bool checkDimensions)
	{
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
		{
			if (navalFormationDeploymentPlan.HasFrame() && (!checkDimensions || navalFormationDeploymentPlan.HasDimensions))
			{
				frame = navalFormationDeploymentPlan.GetFrame();
				return true;
			}
		}
		frame = MatrixFrame.Identity;
		return false;
	}

	private void DeployShips(in Vec2 deployPosition, in Vec2 deployDirection)
	{
		MBList<(int, NavalFormationDeploymentPlan)> mBList = new MBList<(int, NavalFormationDeploymentPlan)>();
		for (int i = 0; i < _formationPlans.Count(); i++)
		{
			NavalFormationDeploymentPlan navalFormationDeploymentPlan = _formationPlans[i];
			if (navalFormationDeploymentPlan.HasShipObject)
			{
				int totalCrewCapacity = navalFormationDeploymentPlan.ShipOrigin.TotalCrewCapacity;
				mBList.Add((totalCrewCapacity, navalFormationDeploymentPlan));
			}
		}
		mBList.Sort(((int crewCapacity, NavalFormationDeploymentPlan plan) x, (int crewCapacity, NavalFormationDeploymentPlan plan) y) => y.crewCapacity.CompareTo(x.crewCapacity));
		float desiredTotalWidth = (_isRiverPlan ? 190f : 400f);
		int num = 4;
		DeployShipRow(in deployPosition, in deployDirection, mBList, desiredTotalWidth);
		mBList.Clear();
	}

	private void ComputeMeanPosition()
	{
		_meanPosition = Vec3.Zero;
		Vec2 zero = Vec2.Zero;
		int num = 0;
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
		{
			if (navalFormationDeploymentPlan.HasFrame())
			{
				zero += navalFormationDeploymentPlan.GetPosition().AsVec2;
				num++;
			}
		}
		if (num > 0)
		{
			_meanPosition = new Vec2(zero.X / (float)num, zero.Y / (float)num).ToVec3();
		}
	}

	private void PlanNavalBattleDeploymentFromSceneData(FormationSceneSpawnEntry[,] formationSceneSpawnEntries)
	{
		if (formationSceneSpawnEntries == null || formationSceneSpawnEntries.GetLength(0) != 2 || formationSceneSpawnEntries.GetLength(1) != _formationPlans.Length)
		{
			return;
		}
		int side = (int)Team.Side;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			NavalFormationDeploymentPlan navalFormationDeploymentPlan = _formationPlans[i];
			if (navalFormationDeploymentPlan.HasShipObject)
			{
				MatrixFrame globalFrame = formationSceneSpawnEntries[side, i].SpawnEntity.GetGlobalFrame();
				navalFormationDeploymentPlan.SetFrame(globalFrame.origin.AsVec2, globalFrame.rotation.f.AsVec2.Normalized());
			}
		}
		IsPlanMade = true;
		_planCount++;
	}

	private void PlanNavalBattleDeploymentFromSpawnPath(float pathOffset, float targetOffset)
	{
		_mission.GetInitialSpawnPathData(Team.Side).GetSpawnPathFrameFacingTarget(pathOffset, targetOffset, _isRiverPlan, out var spawnPathPosition, out var spawnPathDirection);
		DeployShips(in spawnPathPosition, in spawnPathDirection);
		IsPlanMade = true;
		_planCount++;
	}

	private static void DeployShipRow(in Vec2 deployPosition, in Vec2 deployDirection, MBList<(int crewCapacity, NavalFormationDeploymentPlan plan)> sortedPlans, float desiredTotalWidth = 0f)
	{
		float num = 20f;
		if (desiredTotalWidth > 0f && sortedPlans.Count > 1)
		{
			float num2 = 0f;
			for (int i = 0; i < sortedPlans.Count; i++)
			{
				num2 += sortedPlans[i].plan.ShipObject.DeploymentArea.x;
			}
			int num3 = sortedPlans.Count - 1;
			num = (desiredTotalWidth - num2) / (float)num3;
			num = MathF.Max(num, 0f);
			num = MathF.Min(num, 20f);
		}
		float num4 = 0f;
		float num5 = 0f;
		Vec2 vec = deployDirection.LeftVec().Normalized();
		Vec2 vec2 = -vec;
		int j = 0;
		if (sortedPlans.Count % 2 != 0)
		{
			NavalFormationDeploymentPlan item = sortedPlans[j].plan;
			item.SetFrame(in deployPosition, in deployDirection);
			float num6 = item.ShipObject.DeploymentArea.x * 0.5f;
			num4 += num6 + num;
			num5 += num6 + num;
			j++;
		}
		else
		{
			num4 += num * 0.5f;
			num5 += num * 0.5f;
		}
		for (; j < sortedPlans.Count; j++)
		{
			NavalFormationDeploymentPlan item2 = sortedPlans[j].plan;
			float num7 = item2.ShipObject.DeploymentArea.x * 0.5f;
			if (j % 2 == 0)
			{
				num5 += num7;
				item2.SetFrame(deployPosition + vec2 * num5, in deployDirection);
				num5 += num7 + num;
			}
			else
			{
				num4 += num7;
				item2.SetFrame(deployPosition + vec * num4, in deployDirection);
				num4 += num7 + num;
			}
		}
	}
}
