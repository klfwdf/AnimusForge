using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorDefendCastleKeyPosition : BehaviorComponent
{
	private enum BehaviorState
	{
		UnSet,
		Waiting,
		Ready
	}

	private TeamAISiegeComponent _teamAISiegeDefender;

	private CastleGate _innerGate;

	private CastleGate _outerGate;

	private List<SiegeLadder> _laddersOnThisSide;

	private BehaviorState _behaviorState;

	private MovementOrder _waitOrder;

	private MovementOrder _readyOrder;

	private FacingOrder _waitFacingOrder;

	private FacingOrder _readyFacingOrder;

	private TacticalPosition _tacticalMiddlePos;

	private TacticalPosition _tacticalWaitPos;

	private bool _hasFormedShieldWall;

	private WorldPosition _readyOrderPosition;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorDefendCastleKeyPosition(Formation formation)
		: base(formation)
	{
		_teamAISiegeDefender = formation.Team.TeamAI as TeamAISiegeComponent;
		_behaviorState = BehaviorState.UnSet;
		_laddersOnThisSide = new List<SiegeLadder>();
		ResetOrderPositions();
		_hasFormedShieldWall = true;
	}

	protected override void CalculateCurrentOrder()
	{
		base.CalculateCurrentOrder();
		base.CurrentOrder = ((_behaviorState == BehaviorState.Ready) ? _readyOrder : _waitOrder);
		CurrentFacingOrder = ((base.Formation.CachedClosestEnemyFormation != null && TeamAISiegeComponent.IsFormationInsideCastle(base.Formation.CachedClosestEnemyFormation.Formation, includeOnlyPositionedUnits: true)) ? FacingOrder.FacingOrderLookAtEnemy : ((_behaviorState == BehaviorState.Ready) ? _readyFacingOrder : _waitFacingOrder));
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		TextObject variable = GameTexts.FindText("str_formation_ai_side_strings", base.Formation.AI.Side.ToString());
		behaviorString.SetTextVariable("SIDE_STRING", variable);
		behaviorString.SetTextVariable("IS_GENERAL_SIDE", "0");
		return behaviorString;
	}

	private void ResetOrderPositions()
	{
		_behaviorSide = base.Formation.AI.Side;
		_innerGate = null;
		_outerGate = null;
		_laddersOnThisSide.Clear();
		WorldFrame worldFrame;
		WorldFrame worldFrame2;
		if (_teamAISiegeDefender.OuterGate.DefenseSide == _behaviorSide)
		{
			CastleGate outerGate = _teamAISiegeDefender.OuterGate;
			_innerGate = _teamAISiegeDefender.InnerGate;
			_outerGate = _teamAISiegeDefender.OuterGate;
			worldFrame = outerGate.MiddleFrame;
			worldFrame2 = outerGate.DefenseWaitFrame;
			_tacticalMiddlePos = outerGate.MiddlePosition;
			_tacticalWaitPos = outerGate.WaitPosition;
		}
		else
		{
			WallSegment wallSegment = _teamAISiegeDefender.WallSegments.Where((WallSegment ws) => ws.DefenseSide == _behaviorSide && ws.IsBreachedWall).FirstOrDefault();
			if (wallSegment != null)
			{
				worldFrame = wallSegment.MiddleFrame;
				worldFrame2 = wallSegment.DefenseWaitFrame;
				_tacticalMiddlePos = wallSegment.MiddlePosition;
				_tacticalWaitPos = wallSegment.WaitPosition;
			}
			else
			{
				IEnumerable<IPrimarySiegeWeapon> source = _teamAISiegeDefender.PrimarySiegeWeapons.Where((IPrimarySiegeWeapon sw) => sw.WeaponSide == _behaviorSide && (sw is SiegeWeapon { IsDestroyed: false, IsDeactivated: false } || sw.HasCompletedAction()));
				if (!source.Any())
				{
					worldFrame = WorldFrame.Invalid;
					worldFrame2 = WorldFrame.Invalid;
					_tacticalMiddlePos = null;
					_tacticalWaitPos = null;
				}
				else
				{
					_laddersOnThisSide = source.OfType<SiegeLadder>().ToList();
					ICastleKeyPosition castleKeyPosition = source.FirstOrDefault().TargetCastlePosition as ICastleKeyPosition;
					worldFrame = castleKeyPosition.MiddleFrame;
					worldFrame2 = castleKeyPosition.DefenseWaitFrame;
					_tacticalMiddlePos = castleKeyPosition.MiddlePosition;
					_tacticalWaitPos = castleKeyPosition.WaitPosition;
				}
			}
		}
		if (_tacticalMiddlePos != null)
		{
			_readyOrderPosition = _tacticalMiddlePos.Position;
			_readyOrder = MovementOrder.MovementOrderMove(_readyOrderPosition);
			_readyFacingOrder = FacingOrder.FacingOrderLookAtDirection(_tacticalMiddlePos.Direction);
		}
		else if (worldFrame.Origin.IsValid)
		{
			worldFrame.Rotation.f.Normalize();
			_readyOrderPosition = worldFrame.Origin;
			_readyOrder = MovementOrder.MovementOrderMove(_readyOrderPosition);
			_readyFacingOrder = FacingOrder.FacingOrderLookAtDirection(worldFrame.Rotation.f.AsVec2);
		}
		else
		{
			_readyOrderPosition = WorldPosition.Invalid;
			_readyOrder = MovementOrder.MovementOrderStop;
			_readyFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		if (_tacticalWaitPos != null)
		{
			_waitOrder = MovementOrder.MovementOrderMove(_tacticalWaitPos.Position);
			_waitFacingOrder = FacingOrder.FacingOrderLookAtDirection(_tacticalWaitPos.Direction);
		}
		else if (worldFrame2.Origin.IsValid)
		{
			worldFrame2.Rotation.f.Normalize();
			_waitOrder = MovementOrder.MovementOrderMove(worldFrame2.Origin);
			_waitFacingOrder = FacingOrder.FacingOrderLookAtDirection(worldFrame2.Rotation.f.AsVec2);
		}
		else
		{
			_waitOrder = MovementOrder.MovementOrderStop;
			_waitFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		base.CurrentOrder = ((_behaviorState == BehaviorState.Ready) ? _readyOrder : _waitOrder);
		CurrentFacingOrder = ((base.Formation.CachedClosestEnemyFormation != null && TeamAISiegeComponent.IsFormationInsideCastle(base.Formation.CachedClosestEnemyFormation.Formation, includeOnlyPositionedUnits: true)) ? FacingOrder.FacingOrderLookAtEnemy : ((_behaviorState == BehaviorState.Ready) ? _readyFacingOrder : _waitFacingOrder));
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		ResetOrderPositions();
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		bool flag = false;
		if (_teamAISiegeDefender != null && !base.Formation.IsDeployment)
		{
			for (int i = 0; i < TeamAISiegeComponent.SiegeLanes.Count; i++)
			{
				SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes[i];
				if (siegeLane.LaneSide != _behaviorSide)
				{
					continue;
				}
				if (siegeLane.IsOpen)
				{
					flag = true;
					continue;
				}
				for (int j = 0; j < siegeLane.PrimarySiegeWeapons.Count; j++)
				{
					IPrimarySiegeWeapon primarySiegeWeapon = siegeLane.PrimarySiegeWeapons[j];
					if (primarySiegeWeapon is SiegeLadder siegeLadder)
					{
						if (siegeLadder.IsUsed)
						{
							flag = true;
							break;
						}
					}
					else if ((primarySiegeWeapon as SiegeWeapon).GetComponent<SiegeWeaponMovementComponent>().HasApproachedTarget)
					{
						flag = true;
						break;
					}
				}
			}
		}
		BehaviorState behaviorState = ((!flag) ? BehaviorState.Waiting : BehaviorState.Ready);
		bool flag2 = false;
		if (behaviorState != _behaviorState)
		{
			_behaviorState = behaviorState;
			base.CurrentOrder = ((_behaviorState == BehaviorState.Ready) ? _readyOrder : _waitOrder);
			CurrentFacingOrder = ((_behaviorState == BehaviorState.Ready) ? _readyFacingOrder : _waitFacingOrder);
			flag2 = true;
		}
		if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege)
		{
			if (_outerGate != null && _outerGate.State == CastleGate.GateState.Open && !_outerGate.IsDestroyed)
			{
				if (!_outerGate.IsUsedByFormation(base.Formation))
				{
					base.Formation.StartUsingMachine(_outerGate);
				}
			}
			else if (_innerGate != null && _innerGate.State == CastleGate.GateState.Open && !_innerGate.IsDestroyed && !_innerGate.IsUsedByFormation(base.Formation))
			{
				base.Formation.StartUsingMachine(_innerGate);
			}
			foreach (SiegeLadder item in _laddersOnThisSide)
			{
				if (!item.IsDisabledForBattleSide(BattleSideEnum.Defender) && !item.IsUsedByFormation(base.Formation))
				{
					base.Formation.StartUsingMachine(item);
				}
			}
		}
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (_behaviorState == BehaviorState.Ready && _tacticalMiddlePos != null)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_tacticalMiddlePos.Width));
		}
		else if (_behaviorState == BehaviorState.Waiting && _tacticalWaitPos != null)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_tacticalWaitPos.Width));
		}
		if (flag2 || !_hasFormedShieldWall)
		{
			bool flag3 = _behaviorState != BehaviorState.Ready || !_readyOrderPosition.IsValid || _readyOrderPosition.DistanceSquaredWithLimit(base.Formation.CachedMedianPosition.GetNavMeshVec3(), MathF.Min(base.Formation.Depth, base.Formation.Width) * 1.2f) <= (_hasFormedShieldWall ? (MathF.Min(base.Formation.Depth, base.Formation.Width) * MathF.Min(base.Formation.Depth, base.Formation.Width)) : (MathF.Min(base.Formation.Depth, base.Formation.Width) * MathF.Min(base.Formation.Depth, base.Formation.Width) * 0.25f));
			if (flag3 != _hasFormedShieldWall)
			{
				_hasFormedShieldWall = flag3;
				base.Formation.SetArrangementOrder(_hasFormedShieldWall ? ArrangementOrder.ArrangementOrderShieldWall : ArrangementOrder.ArrangementOrderLine);
			}
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		ResetOrderPositions();
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		ResetOrderPositions();
	}

	protected override void OnBehaviorActivatedAux()
	{
		ResetOrderPositions();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		_hasFormedShieldWall = true;
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
