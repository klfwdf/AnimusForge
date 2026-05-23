using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.DividableTasks;

namespace TaleWorlds.MountAndBlade;

public abstract class RangedSiegeWeaponAi : UsableMachineAIBase
{
	public class ThreatSeeker
	{
		private FindMostDangerousThreat _getMostDangerousThreat;

		private const float SingleUnitThreatValue = 3f;

		private const float InsideWallsThreatMultiplier = 3f;

		private Threat _currentThreat;

		private Agent _targetAgent;

		public RangedSiegeWeapon Weapon;

		public List<Vec3> WeaponPositions;

		private List<ITargetable> _potentialTargetObjects;

		private List<ICastleKeyPosition> _referencePositions;

		public ThreatSeeker(RangedSiegeWeapon weapon)
		{
			Weapon = weapon;
			WeaponPositions = new List<Vec3> { Weapon.GameEntity.GlobalPosition };
			_targetAgent = null;
			_getMostDangerousThreat = new FindMostDangerousThreat();
		}

		public void InitializeTargetableObjects()
		{
			IEnumerable<MissionObject> source = Mission.Current.ActiveMissionObjects.WhereQ((MissionObject mo) => mo is ITargetable);
			_potentialTargetObjects = (from to in source.WhereQ((MissionObject to) => to is ITargetable targetable && targetable.IsDestructable() && targetable.GetTargetEntity() != null)
				select to as ITargetable).ToList();
			_referencePositions = source.OfType<ICastleKeyPosition>().ToList();
		}

		public Threat PrepareTargetFromTask()
		{
			_currentThreat = _getMostDangerousThreat.GetResult(out var targetAgent);
			if (_currentThreat != null && _currentThreat.TargetableObject == null)
			{
				_currentThreat.Agent = _targetAgent;
				if (_targetAgent == null || !_targetAgent.IsActive() || _targetAgent.Formation != _currentThreat.Formation || !Weapon.CanShootAtAgent(_targetAgent))
				{
					_targetAgent = targetAgent;
					float selectedAgentScore = float.MaxValue;
					Agent selectedAgent = _targetAgent;
					Action<Agent> action = delegate(Agent agent)
					{
						float num = agent.Position.DistanceSquared(Weapon.GameEntity.GlobalPosition) * (MBRandom.RandomFloat * 0.2f + 0.8f);
						if (agent == _targetAgent)
						{
							num *= 0.5f;
						}
						if (selectedAgentScore > num && Weapon.CanShootAtAgent(agent))
						{
							selectedAgent = agent;
							selectedAgentScore = num;
						}
					};
					if (targetAgent.Detachment == null)
					{
						_currentThreat.Formation.ApplyActionOnEachAttachedUnit(action);
					}
					else
					{
						_currentThreat.Formation.ApplyActionOnEachDetachedUnit(action);
					}
					_targetAgent = selectedAgent ?? _currentThreat.Formation.GetUnitWithIndex(MBRandom.RandomInt(_currentThreat.Formation.CountOfUnits));
					_currentThreat.Agent = _targetAgent;
				}
			}
			if (_currentThreat != null && _currentThreat.TargetableObject == null && _currentThreat.Agent == null)
			{
				_currentThreat = null;
			}
			return _currentThreat;
		}

		public bool UpdateThreatSeekerTask()
		{
			Agent targetAgent = _targetAgent;
			if (targetAgent != null && !targetAgent.IsActive())
			{
				_targetAgent = null;
			}
			return _getMostDangerousThreat.Update();
		}

		public void PrepareThreatSeekerTask(Action lastAction)
		{
			_getMostDangerousThreat.Prepare(GetAllThreats(), Weapon);
			_getMostDangerousThreat.SetLastAction(lastAction);
		}

		public void Release()
		{
			_targetAgent = null;
			_currentThreat = null;
		}

		public List<Threat> GetAllThreats()
		{
			List<Threat> list = new List<Threat>();
			for (int num = _potentialTargetObjects.Count - 1; num >= 0; num--)
			{
				ITargetable targetable = _potentialTargetObjects[num];
				if ((targetable is UsableMachine usableMachine && (usableMachine.IsDestroyed || usableMachine.IsDeactivated || !usableMachine.GameEntity.IsValid)) || targetable is MissionObject { IsDisabled: not false } || targetable.GetSide() == Weapon.Side)
				{
					_potentialTargetObjects.RemoveAt(num);
				}
				else
				{
					Threat item = new Threat
					{
						TargetableObject = targetable,
						ThreatValue = Weapon.ProcessTargetValue(targetable.GetTargetValue(WeaponPositions), targetable.GetTargetFlags()),
						ForceTarget = targetable.Entity().HasTag("attackMe")
					};
					list.Add(item);
				}
			}
			foreach (Team team in Mission.Current.Teams)
			{
				if (team.Side.GetOppositeSide() != Weapon.Side)
				{
					continue;
				}
				foreach (Formation item2 in team.FormationsIncludingSpecialAndEmpty)
				{
					if (item2.CountOfUnits > 0)
					{
						float targetValueOfFormation = GetTargetValueOfFormation(item2, _referencePositions);
						if (targetValueOfFormation != -1f)
						{
							list.Add(new Threat
							{
								Formation = item2,
								ThreatValue = Weapon.ProcessTargetValue(targetValueOfFormation, GetTargetFlagsOfFormation()),
								ForceTarget = false
							});
						}
					}
				}
			}
			return list;
		}

		private static float GetTargetValueOfFormation(Formation formation, IEnumerable<ICastleKeyPosition> referencePositions)
		{
			if (formation.QuerySystem.LocalEnemyPower / formation.QuerySystem.LocalAllyPower > 0.5f)
			{
				return -1f;
			}
			float num = (float)formation.CountOfUnits * 3f;
			if (TeamAISiegeComponent.IsFormationInsideCastle(formation, includeOnlyPositionedUnits: true))
			{
				num *= 3f;
			}
			num *= GetPositionMultiplierOfFormation(formation, referencePositions);
			float num2 = MBMath.ClampFloat(formation.QuerySystem.LocalAllyPower / (formation.QuerySystem.LocalEnemyPower + 0.01f), 0f, 5f) / 5f;
			return num * num2;
		}

		public static TargetFlags GetTargetFlagsOfFormation()
		{
			return TargetFlags.IsMoving | TargetFlags.IsFlammable | TargetFlags.IsAttacker;
		}

		private static float GetPositionMultiplierOfFormation(Formation formation, IEnumerable<ICastleKeyPosition> referencePositions)
		{
			ICastleKeyPosition closestCastlePosition;
			float minimumDistanceBetweenPositions = GetMinimumDistanceBetweenPositions(formation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: false, formation.GetAveragePositionOfUnits(excludeDetachedUnits: false, excludePlayer: false)).Position, referencePositions, out closestCastlePosition);
			bool flag = closestCastlePosition != null && closestCastlePosition.AttackerSiegeWeapon != null && closestCastlePosition.AttackerSiegeWeapon.HasCompletedAction();
			float num;
			if (formation.PhysicalClass.IsRanged())
			{
				num = ((minimumDistanceBetweenPositions < 20f) ? 1f : ((!(minimumDistanceBetweenPositions < 35f)) ? 0.6f : 0.8f));
				return num + (flag ? 0.2f : 0f);
			}
			num = ((minimumDistanceBetweenPositions < 15f) ? 0.2f : ((!(minimumDistanceBetweenPositions < 40f)) ? 0.12f : 0.15f));
			return num * (flag ? 7.5f : 1f);
		}

		private static float GetMinimumDistanceBetweenPositions(Vec3 position, IEnumerable<ICastleKeyPosition> referencePositions, out ICastleKeyPosition closestCastlePosition)
		{
			if (referencePositions != null && referencePositions.Count() != 0)
			{
				closestCastlePosition = TaleWorlds.Core.Extensions.MinBy(referencePositions, (ICastleKeyPosition rp) => rp.GetPosition().DistanceSquared(position));
				return TaleWorlds.Library.MathF.Sqrt(closestCastlePosition.GetPosition().DistanceSquared(position));
			}
			closestCastlePosition = null;
			return -1f;
		}

		public static Threat GetMaxThreat(List<ICastleKeyPosition> castleKeyPositions)
		{
			List<ITargetable> list = new List<ITargetable>();
			List<Threat> list2 = new List<Threat>();
			foreach (WeakGameEntity item2 in Mission.Current.ActiveMissionObjects.Select((MissionObject amo) => amo.GameEntity))
			{
				if (item2.GetFirstScriptOfType<UsableMachine>() is ITargetable item)
				{
					list.Add(item);
				}
			}
			list.RemoveAll((ITargetable um) => um.GetSide() == BattleSideEnum.Defender);
			list2.AddRange(list.Select((ITargetable um) => new Threat
			{
				TargetableObject = um,
				ThreatValue = um.GetTargetValue(castleKeyPositions.Select((ICastleKeyPosition c) => c.GetPosition()).ToList()),
				ForceTarget = um.Entity().HasTag("attackMe")
			}));
			return TaleWorlds.Core.Extensions.MaxBy(list2, (Threat t) => t.ThreatValue);
		}
	}

	private const float TargetEvaluationDelay = 0.5f;

	private const int MaxTargetEvaluationCount = 4;

	public const string ForceTargetEntityTag = "attackMe";

	private readonly ThreatSeeker _threatSeeker;

	private Threat _target;

	private float _delayTimer;

	private float _delayDuration = 1f;

	private int _cannotShootCounter;

	private readonly Timer _targetEvaluationTimer;

	public RangedSiegeWeaponAi(RangedSiegeWeapon rangedSiegeWeapon)
		: base(rangedSiegeWeapon)
	{
		_threatSeeker = new ThreatSeeker(rangedSiegeWeapon);
		((RangedSiegeWeapon)UsableMachine).OnReloadDone += FindNextTarget;
		_delayTimer = _delayDuration;
		_targetEvaluationTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
	}

	public void InitializeThreatSeeker()
	{
		_threatSeeker.InitializeTargetableObjects();
	}

	protected override void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
	{
		base.OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
		if (UsableMachine.PilotAgent != null && UsableMachine.PilotAgent.IsAIControlled)
		{
			RangedSiegeWeapon rangedSiegeWeapon = UsableMachine as RangedSiegeWeapon;
			if (rangedSiegeWeapon.State == RangedSiegeWeapon.WeaponState.WaitingAfterShooting && rangedSiegeWeapon.PilotAgent != null && rangedSiegeWeapon.PilotAgent.IsAIControlled)
			{
				rangedSiegeWeapon.AiRequestsManualReload();
			}
			UpdateAim(rangedSiegeWeapon, dt);
		}
		AfterTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
	}

	protected virtual void UpdateAim(RangedSiegeWeapon rangedSiegeWeapon, float dt)
	{
		if (_threatSeeker.UpdateThreatSeekerTask() && dt > 0f && _target == null && rangedSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Idle)
		{
			if (_delayTimer <= 0f)
			{
				FindNextTarget();
			}
			_delayTimer -= dt;
		}
		if (_target == null)
		{
			return;
		}
		if (_target.Agent != null && !_target.Agent.IsActive())
		{
			_target = null;
		}
		else if (rangedSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Idle && rangedSiegeWeapon.UserCountNotInStruckAction > 0)
		{
			if (DebugSiegeBehavior.ToggleTargetDebug && UsableMachine.PilotAgent != null)
			{
				_target.ComputeGlobalTargetingBoundingBoxMinMax();
				_ = _target.TargetingPosition;
			}
			if (_targetEvaluationTimer.Check(Mission.Current.CurrentTime) && !((RangedSiegeWeapon)UsableMachine).CanShootAtThreat(_target))
			{
				_cannotShootCounter++;
			}
			if (_cannotShootCounter < 4)
			{
				if (rangedSiegeWeapon.AimAtThreat(_target) && rangedSiegeWeapon.PilotAgent != null)
				{
					_delayTimer -= dt;
					if (_delayTimer <= 0f && rangedSiegeWeapon.CanShootAtThreat(_target))
					{
						rangedSiegeWeapon.AiRequestsShoot();
						_target = null;
						SetTargetingTimer();
						_cannotShootCounter = 0;
						_targetEvaluationTimer.Reset(Mission.Current.CurrentTime);
					}
				}
			}
			else
			{
				_target = null;
				SetTargetingTimer();
				_cannotShootCounter = 0;
			}
		}
		else
		{
			_targetEvaluationTimer.Reset(Mission.Current.CurrentTime);
		}
	}

	private void SetTargetFromThreatSeeker()
	{
		_target = _threatSeeker.PrepareTargetFromTask();
	}

	public void FindNextTarget()
	{
		if (UsableMachine.PilotAgent != null && UsableMachine.PilotAgent.IsAIControlled)
		{
			_threatSeeker.PrepareThreatSeekerTask(SetTargetFromThreatSeeker);
			SetTargetingTimer();
		}
	}

	private void AfterTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
	{
		if ((!(dt > 0f) || (agentToCompareTo != null && UsableMachine.PilotAgent != agentToCompareTo) || (formationToCompareTo != null && (UsableMachine.PilotAgent == null || !UsableMachine.PilotAgent.IsAIControlled || UsableMachine.PilotAgent.Formation != formationToCompareTo))) && UsableMachine.PilotAgent == null)
		{
			_threatSeeker.Release();
			_target = null;
		}
	}

	private void SetTargetingTimer()
	{
		_delayTimer = _delayDuration + MBRandom.RandomFloat * 0.5f;
	}
}
