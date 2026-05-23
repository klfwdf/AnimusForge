using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class SiegeLane
{
	public enum LaneStateEnum
	{
		Safe,
		Unused,
		Used,
		Active,
		Abandoned,
		Contested,
		Conceited
	}

	public enum LaneDefenseStates
	{
		Empty,
		Token,
		Full
	}

	private readonly Formation[] _lastAssignedFormations;

	private SiegeQuerySystem _siegeQuerySystem;

	private SiegeWeapon _attackerMovableWeapon;

	public LaneStateEnum LaneState { get; private set; }

	public FormationAI.BehaviorSide LaneSide { get; }

	public List<IPrimarySiegeWeapon> PrimarySiegeWeapons { get; private set; }

	public bool IsOpen { get; private set; }

	public bool IsBreach { get; private set; }

	public bool HasGate { get; private set; }

	public List<ICastleKeyPosition> DefensePoints { get; private set; }

	public WorldPosition DefenderOrigin { get; private set; }

	public WorldPosition AttackerOrigin { get; private set; }

	public SiegeLane(FormationAI.BehaviorSide laneSide, SiegeQuerySystem siegeQuerySystem)
	{
		LaneSide = laneSide;
		IsOpen = false;
		PrimarySiegeWeapons = new List<IPrimarySiegeWeapon>();
		DefensePoints = new List<ICastleKeyPosition>();
		IsBreach = false;
		_siegeQuerySystem = siegeQuerySystem;
		_lastAssignedFormations = new Formation[Mission.Current.Teams.Count];
		HasGate = false;
		LaneState = LaneStateEnum.Active;
	}

	public bool CalculateIsLaneUnusable()
	{
		if (IsOpen)
		{
			return false;
		}
		if (HasGate)
		{
			for (int i = 0; i < DefensePoints.Count; i++)
			{
				if (DefensePoints[i] is CastleGate { IsGateOpen: not false, GameEntity: var gameEntity } && gameEntity.HasTag("outer_gate"))
				{
					return false;
				}
			}
		}
		for (int j = 0; j < PrimarySiegeWeapons.Count; j++)
		{
			IPrimarySiegeWeapon primarySiegeWeapon = PrimarySiegeWeapons[j];
			if (!(primarySiegeWeapon is UsableMachine { GameEntity: { IsValid: false } }) && !(primarySiegeWeapon is SiegeTower { IsDestroyed: not false }) && (primarySiegeWeapon.HasCompletedAction() || !(primarySiegeWeapon is BatteringRam { IsDestroyed: not false })))
			{
				return false;
			}
		}
		return true;
	}

	public Formation GetLastAssignedFormation(int teamIndex)
	{
		if (teamIndex >= 0)
		{
			return _lastAssignedFormations[teamIndex];
		}
		return null;
	}

	public void SetLaneState(LaneStateEnum newLaneState)
	{
		LaneState = newLaneState;
	}

	public void SetLastAssignedFormation(int teamIndex, Formation formation)
	{
		if (teamIndex >= 0)
		{
			_lastAssignedFormations[teamIndex] = formation;
		}
	}

	public void SetSiegeQuerySystem(SiegeQuerySystem siegeQuerySystem)
	{
		_siegeQuerySystem = siegeQuerySystem;
	}

	public float CalculateLaneCapacity()
	{
		bool flag = false;
		for (int i = 0; i < DefensePoints.Count; i++)
		{
			if (DefensePoints[i] is WallSegment { IsBreachedWall: not false })
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return 60f;
		}
		if (HasGate)
		{
			bool flag2 = true;
			for (int j = 0; j < DefensePoints.Count; j++)
			{
				if (DefensePoints[j] is CastleGate { IsGateOpen: false })
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				return 60f;
			}
		}
		float num = 0f;
		for (int k = 0; k < PrimarySiegeWeapons.Count; k++)
		{
			SiegeWeapon siegeWeapon = PrimarySiegeWeapons[k] as SiegeWeapon;
			if ((PrimarySiegeWeapons[k].HasCompletedAction() || !siegeWeapon.IsDeactivated) && !siegeWeapon.IsDestroyed)
			{
				num += PrimarySiegeWeapons[k].SiegeWeaponPriority;
			}
		}
		return num;
	}

	public LaneDefenseStates GetDefenseState()
	{
		switch (LaneState)
		{
		case LaneStateEnum.Safe:
		case LaneStateEnum.Unused:
			return LaneDefenseStates.Empty;
		case LaneStateEnum.Used:
		case LaneStateEnum.Abandoned:
			return LaneDefenseStates.Token;
		case LaneStateEnum.Active:
		case LaneStateEnum.Contested:
		case LaneStateEnum.Conceited:
			return LaneDefenseStates.Full;
		default:
			return LaneDefenseStates.Full;
		}
	}

	private bool IsPowerBehindLane()
	{
		switch (LaneSide)
		{
		case FormationAI.BehaviorSide.Left:
			return _siegeQuerySystem.LeftRegionMemberCount >= 30;
		case FormationAI.BehaviorSide.Middle:
			return _siegeQuerySystem.MiddleRegionMemberCount >= 30;
		case FormationAI.BehaviorSide.Right:
			return _siegeQuerySystem.RightRegionMemberCount >= 30;
		default:
			MBDebug.ShowWarning("Lane without side");
			return false;
		}
	}

	public bool IsUnderAttack()
	{
		switch (LaneSide)
		{
		case FormationAI.BehaviorSide.Left:
			return _siegeQuerySystem.LeftCloseAttackerCount >= 15;
		case FormationAI.BehaviorSide.Middle:
			return _siegeQuerySystem.MiddleCloseAttackerCount >= 15;
		case FormationAI.BehaviorSide.Right:
			return _siegeQuerySystem.RightCloseAttackerCount >= 15;
		default:
			MBDebug.ShowWarning("Lane without side");
			return false;
		}
	}

	public bool IsDefended()
	{
		switch (LaneSide)
		{
		case FormationAI.BehaviorSide.Left:
			return _siegeQuerySystem.LeftDefenderCount >= 15;
		case FormationAI.BehaviorSide.Middle:
			return _siegeQuerySystem.MiddleDefenderCount >= 15;
		case FormationAI.BehaviorSide.Right:
			return _siegeQuerySystem.RightDefenderCount >= 15;
		default:
			MBDebug.ShowWarning("Lane without side");
			return false;
		}
	}

	public void DetermineLaneState()
	{
		if (LaneState == LaneStateEnum.Conceited && !IsDefended())
		{
			return;
		}
		if (CalculateIsLaneUnusable())
		{
			LaneState = LaneStateEnum.Safe;
		}
		else if (Mission.Current.IsTeleportingAgents)
		{
			LaneState = LaneStateEnum.Active;
		}
		else if (!IsOpen)
		{
			bool flag = true;
			foreach (IPrimarySiegeWeapon primarySiegeWeapon in PrimarySiegeWeapons)
			{
				if (!(primarySiegeWeapon is IMoveableSiegeWeapon) || primarySiegeWeapon.HasCompletedAction() || ((SiegeWeapon)primarySiegeWeapon).IsUsed)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				LaneState = LaneStateEnum.Unused;
			}
			else
			{
				LaneState = ((!IsPowerBehindLane()) ? LaneStateEnum.Used : LaneStateEnum.Active);
			}
		}
		else if (!IsPowerBehindLane())
		{
			LaneState = LaneStateEnum.Abandoned;
		}
		else
		{
			LaneState = ((!IsUnderAttack() || IsDefended()) ? LaneStateEnum.Contested : LaneStateEnum.Conceited);
		}
		if (HasGate && LaneState < LaneStateEnum.Active && TeamAISiegeComponent.QuerySystem.InsideAttackerCount >= 15)
		{
			LaneState = LaneStateEnum.Active;
		}
	}

	public WorldPosition GetCurrentAttackerPosition()
	{
		if (IsBreach)
		{
			return DefenderOrigin;
		}
		if (_attackerMovableWeapon != null)
		{
			return _attackerMovableWeapon.WaitFrame.origin.ToWorldPosition();
		}
		return AttackerOrigin;
	}

	public void DetermineOrigins()
	{
		_attackerMovableWeapon = null;
		if (IsBreach)
		{
			WallSegment wallSegment = DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp is WallSegment && (dp as WallSegment).IsBreachedWall) as WallSegment;
			DefenderOrigin = wallSegment.MiddleFrame.Origin;
			AttackerOrigin = wallSegment.AttackerWaitFrame.Origin;
			return;
		}
		HasGate = DefensePoints.Any((ICastleKeyPosition dp) => dp is CastleGate);
		IEnumerable<IPrimarySiegeWeapon> enumerable;
		if (PrimarySiegeWeapons.Count != 0)
		{
			IEnumerable<IPrimarySiegeWeapon> primarySiegeWeapons = PrimarySiegeWeapons;
			enumerable = primarySiegeWeapons;
		}
		else
		{
			enumerable = from sw in Mission.Current.MissionObjects.FindAllWithType<SiegeWeapon>()
				where sw is IPrimarySiegeWeapon primarySiegeWeapon && primarySiegeWeapon.WeaponSide == LaneSide
				select sw as IPrimarySiegeWeapon;
		}
		IEnumerable<IPrimarySiegeWeapon> source = enumerable;
		if (source.FirstOrDefault((IPrimarySiegeWeapon psw) => psw is IMoveableSiegeWeapon) is IMoveableSiegeWeapon moveableSiegeWeapon)
		{
			_attackerMovableWeapon = moveableSiegeWeapon as SiegeWeapon;
			DefenderOrigin = ((moveableSiegeWeapon as IPrimarySiegeWeapon).TargetCastlePosition as ICastleKeyPosition).MiddleFrame.Origin;
			AttackerOrigin = moveableSiegeWeapon.GetInitialFrame().origin.ToWorldPosition();
			return;
		}
		SiegeLadder siegeLadder = source.FirstOrDefault((IPrimarySiegeWeapon psw) => psw is SiegeLadder) as SiegeLadder;
		DefenderOrigin = (siegeLadder.TargetCastlePosition as ICastleKeyPosition).MiddleFrame.Origin;
		AttackerOrigin = siegeLadder.InitialWaitPosition.GetGlobalFrame().origin.ToWorldPosition();
	}

	public void RefreshLane()
	{
		for (int num = PrimarySiegeWeapons.Count - 1; num >= 0; num--)
		{
			if (PrimarySiegeWeapons[num] is SiegeWeapon { IsDisabled: not false })
			{
				PrimarySiegeWeapons.RemoveAt(num);
			}
		}
		bool flag = false;
		for (int i = 0; i < DefensePoints.Count; i++)
		{
			if (DefensePoints[i] is WallSegment { IsBreachedWall: not false })
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			IsOpen = true;
			IsBreach = true;
			return;
		}
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = true;
		for (int j = 0; j < DefensePoints.Count; j++)
		{
			ICastleKeyPosition castleKeyPosition = DefensePoints[j];
			if (flag4 && castleKeyPosition is CastleGate castleGate)
			{
				flag2 = true;
				flag3 = true;
				if (!castleGate.IsDestroyed && castleGate.State != CastleGate.GateState.Open)
				{
					flag4 = false;
					break;
				}
			}
			else if (!flag3 && !(castleKeyPosition is WallSegment))
			{
				flag3 = true;
			}
		}
		bool flag5 = false;
		if (!flag3)
		{
			for (int k = 0; k < PrimarySiegeWeapons.Count; k++)
			{
				IPrimarySiegeWeapon primarySiegeWeapon = PrimarySiegeWeapons[k];
				if (primarySiegeWeapon.HasCompletedAction() && !(primarySiegeWeapon as UsableMachine).IsDestroyed)
				{
					flag5 = true;
					break;
				}
			}
		}
		IsOpen = (flag2 && flag4) || flag5;
	}

	public void SetPrimarySiegeWeapons(List<IPrimarySiegeWeapon> primarySiegeWeapons)
	{
		PrimarySiegeWeapons = primarySiegeWeapons;
	}

	public void SetDefensePoints(List<ICastleKeyPosition> defensePoints)
	{
		DefensePoints = defensePoints;
		foreach (ICastleKeyPosition item in PrimarySiegeWeapons.Select((IPrimarySiegeWeapon psw) => psw.TargetCastlePosition as ICastleKeyPosition))
		{
			if (item != null && !DefensePoints.Contains(item))
			{
				DefensePoints.Add(item);
			}
		}
	}
}
