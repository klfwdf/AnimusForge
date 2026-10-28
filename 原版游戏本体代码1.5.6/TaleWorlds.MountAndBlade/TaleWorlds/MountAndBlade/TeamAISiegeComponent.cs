using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class TeamAISiegeComponent : TeamAIComponent
{
	public const int InsideCastleNavMeshID = 1;

	public const int SiegeTokenForceSize = 15;

	private const float FormationInsideCastleThresholdPercentage = 0.4f;

	private const float CastleBreachThresholdPercentage = 0.7f;

	public readonly IEnumerable<WallSegment> WallSegments;

	public readonly List<SiegeWeapon> SceneSiegeWeapons;

	protected readonly IEnumerable<CastleGate> CastleGates;

	protected readonly List<SiegeTower> SiegeTowers;

	protected readonly HashSet<int> PrimarySiegeWeaponNavMeshFaceIDs;

	protected BatteringRam Ram;

	protected List<MissionObject> CastleKeyPositions;

	private readonly MBList<SiegeLadder> _ladders;

	private bool _noProperLaneRemains;

	public static List<SiegeLane> SiegeLanes { get; private set; }

	public static SiegeQuerySystem QuerySystem { get; protected set; }

	public CastleGate OuterGate { get; }

	public List<IPrimarySiegeWeapon> PrimarySiegeWeapons { get; }

	public CastleGate InnerGate { get; }

	public MBReadOnlyList<SiegeLadder> Ladders => _ladders;

	public bool AreLaddersReady { get; private set; }

	public List<int> DifficultNavmeshIDs { get; private set; }

	protected TeamAISiegeComponent(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		CastleGates = currentMission.ActiveMissionObjects.FindAllWithType<CastleGate>().ToList();
		WallSegments = currentMission.ActiveMissionObjects.FindAllWithType<WallSegment>().ToList();
		OuterGate = CastleGates.FirstOrDefault((CastleGate g) => g.GameEntity.HasTag("outer_gate"));
		InnerGate = CastleGates.FirstOrDefault((CastleGate g) => g.GameEntity.HasTag("inner_gate"));
		SceneSiegeWeapons = Mission.Current.MissionObjects.FindAllWithType<SiegeWeapon>().ToList();
		_ladders = SceneSiegeWeapons.OfType<SiegeLadder>().ToMBList();
		Ram = SceneSiegeWeapons.FirstOrDefault((SiegeWeapon ssw) => ssw is BatteringRam) as BatteringRam;
		SiegeTowers = SceneSiegeWeapons.OfType<SiegeTower>().ToList();
		PrimarySiegeWeapons = new List<IPrimarySiegeWeapon>();
		PrimarySiegeWeapons.AddRange(_ladders);
		if (Ram != null)
		{
			PrimarySiegeWeapons.Add(Ram);
		}
		PrimarySiegeWeapons.AddRange(SiegeTowers);
		PrimarySiegeWeaponNavMeshFaceIDs = new HashSet<int>();
		foreach (IPrimarySiegeWeapon primarySiegeWeapon in PrimarySiegeWeapons)
		{
			IPrimarySiegeWeapon current;
			if ((current = primarySiegeWeapon) != null && current.GetNavmeshFaceIds(out var navmeshFaceIds))
			{
				PrimarySiegeWeaponNavMeshFaceIDs.UnionWith(navmeshFaceIds);
			}
		}
		CastleKeyPositions = new List<MissionObject>();
		CastleKeyPositions.AddRange(CastleGates);
		CastleKeyPositions.AddRange(WallSegments);
		SiegeLanes = new List<SiegeLane>();
		int i;
		for (i = 0; i < 3; i++)
		{
			SiegeLanes.Add(new SiegeLane((FormationAI.BehaviorSide)i, QuerySystem));
			SiegeLanes[i].SetPrimarySiegeWeapons((from um in PrimarySiegeWeapons
				where um.WeaponSide == (FormationAI.BehaviorSide)i
				select (um)).ToList());
			SiegeLanes[i].SetDefensePoints((from dp in CastleKeyPositions
				where ((ICastleKeyPosition)dp).DefenseSide == (FormationAI.BehaviorSide)i
				select (ICastleKeyPosition)dp).ToList());
			SiegeLanes[i].RefreshLane();
		}
		SiegeLanes.ForEach(delegate(SiegeLane sl)
		{
			sl.SetSiegeQuerySystem(QuerySystem);
		});
		DifficultNavmeshIDs = new List<int>();
	}

	protected internal override void Tick(float dt)
	{
		if (!_noProperLaneRemains)
		{
			int num = 0;
			SiegeLane siegeLane = null;
			foreach (SiegeLane siegeLane3 in SiegeLanes)
			{
				siegeLane3.RefreshLane();
				siegeLane3.DetermineLaneState();
				if (siegeLane3.IsBreach)
				{
					num++;
				}
				else
				{
					siegeLane = siegeLane3;
				}
			}
			if (siegeLane != null && num >= 2 && !siegeLane.IsOpen && siegeLane.LaneState >= SiegeLane.LaneStateEnum.Used)
			{
				siegeLane.SetLaneState(SiegeLane.LaneStateEnum.Unused);
			}
			if (SiegeLanes.Count == 0)
			{
				_noProperLaneRemains = true;
				foreach (FormationAI.BehaviorSide difficultLaneSide in from ckp in CastleKeyPositions
					where ckp is CastleGate castleGate && castleGate.DefenseSide != FormationAI.BehaviorSide.BehaviorSideNotSet
					select ((CastleGate)ckp).DefenseSide)
				{
					SiegeLane siegeLane2 = new SiegeLane(difficultLaneSide, QuerySystem);
					siegeLane2.SetPrimarySiegeWeapons(new List<IPrimarySiegeWeapon>());
					siegeLane2.SetDefensePoints((from dp in CastleKeyPositions
						where ((ICastleKeyPosition)dp).DefenseSide == difficultLaneSide && dp is CastleGate
						select dp as ICastleKeyPosition).ToList());
					siegeLane2.RefreshLane();
					siegeLane2.DetermineLaneState();
					SiegeLanes.Add(siegeLane2);
				}
			}
		}
		else
		{
			foreach (SiegeLane siegeLane4 in SiegeLanes)
			{
				siegeLane4.RefreshLane();
				siegeLane4.DetermineLaneState();
			}
		}
		base.Tick(dt);
	}

	public static void OnMissionFinalize()
	{
		if (SiegeLanes != null)
		{
			SiegeLanes.Clear();
			SiegeLanes = null;
		}
		QuerySystem = null;
	}

	public bool CalculateIsChargePastWallsApplicable(FormationAI.BehaviorSide side)
	{
		if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.SallyOut)
		{
			return false;
		}
		if (side == FormationAI.BehaviorSide.BehaviorSideNotSet && InnerGate != null && !InnerGate.IsGateOpen)
		{
			return false;
		}
		foreach (SiegeLane siegeLane in SiegeLanes)
		{
			if (side == FormationAI.BehaviorSide.BehaviorSideNotSet)
			{
				if (!siegeLane.IsOpen)
				{
					return false;
				}
			}
			else if (side == siegeLane.LaneSide)
			{
				return siegeLane.IsOpen && (siegeLane.IsBreach || (siegeLane.HasGate && (InnerGate == null || InnerGate.IsGateOpen)));
			}
		}
		return true;
	}

	public void SetAreLaddersReady(bool areLaddersReady)
	{
		AreLaddersReady = areLaddersReady;
	}

	public bool CalculateIsAnyLaneOpenToGetInside()
	{
		return SiegeLanes.Any((SiegeLane sl) => sl.IsOpen);
	}

	public bool CalculateIsAnyLaneOpenToGoOutside()
	{
		return SiegeLanes.Any((SiegeLane sl) => sl.IsOpen && (sl.IsBreach || sl.HasGate || sl.PrimarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => psw is SiegeTower)));
	}

	public bool IsPrimarySiegeWeaponNavmeshFaceId(int id)
	{
		return PrimarySiegeWeaponNavMeshFaceIDs.Contains(id);
	}

	public static bool IsFormationGroupInsideCastle(MBList<Formation> formationGroup, bool includeOnlyPositionedUnits, float thresholdPercentage = 0.4f)
	{
		int num = 0;
		foreach (Formation item in formationGroup)
		{
			num += (includeOnlyPositionedUnits ? item.Arrangement.PositionedUnitCount : item.CountOfUnits);
		}
		float num2 = (float)num * thresholdPercentage;
		foreach (Formation item2 in formationGroup)
		{
			if (item2.CountOfUnits > 0)
			{
				num2 -= (float)item2.CountUnitsOnNavMeshIDMod10(1, includeOnlyPositionedUnits);
				if (num2 <= 0f)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsFormationInsideCastle(Formation formation, bool includeOnlyPositionedUnits, float thresholdPercentage = 0.4f)
	{
		int num = (includeOnlyPositionedUnits ? formation.Arrangement.PositionedUnitCount : formation.CountOfUnits);
		float num2 = (float)num * thresholdPercentage;
		if (num == 0)
		{
			if (formation.Team.TeamAI is TeamAISiegeAttacker || formation.Team.TeamAI is TeamAISallyOutDefender)
			{
				return false;
			}
			if (formation.Team.TeamAI is TeamAISiegeDefender || formation.Team.TeamAI is TeamAISallyOutAttacker)
			{
				return true;
			}
			return false;
		}
		if (includeOnlyPositionedUnits)
		{
			return (float)formation.QuerySystem.InsideCastleUnitCountPositioned >= num2;
		}
		return (float)formation.QuerySystem.InsideCastleUnitCountIncludingUnpositioned >= num2;
	}

	public bool IsCastleBreached()
	{
		int num = 0;
		int num2 = 0;
		foreach (Formation item in Mission.AttackerTeam.FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				num2++;
				if (IsFormationInsideCastle(item, includeOnlyPositionedUnits: true))
				{
					num++;
				}
			}
		}
		if (Mission.AttackerAllyTeam != null)
		{
			foreach (Formation item2 in Mission.AttackerAllyTeam.FormationsIncludingSpecialAndEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					num2++;
					if (IsFormationInsideCastle(item2, includeOnlyPositionedUnits: true))
					{
						num++;
					}
				}
			}
		}
		return (float)num >= (float)num2 * 0.7f;
	}

	public override void OnDeploymentFinished()
	{
		foreach (SiegeLadder item in _ladders.Where((SiegeLadder l) => !l.IsDisabled))
		{
			DifficultNavmeshIDs.Add(item.OnWallNavMeshId);
		}
		foreach (SiegeTower siegeTower in SiegeTowers)
		{
			DifficultNavmeshIDs.AddRange(siegeTower.CollectGetDifficultNavmeshIDs());
		}
		foreach (Formation item2 in Team.FormationsIncludingEmpty)
		{
			item2.OnDeploymentFinished();
		}
	}
}
