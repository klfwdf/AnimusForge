using System;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Behaviors;

public sealed class BehaviorNavalRamming : NavalBehaviorComponent
{
	private NavalShipsLogic _navalShipsLogic;

	private MissionShip _formationMainShip;

	private MissionShip _ignoredShip;

	private bool _isRammingActive;

	public BehaviorNavalRamming(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.8f;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	private void CalculateAndSetShipOrders()
	{
		if (base.Formation.CachedClosestEnemyFormation == null || !_formationMainShip.IsFormationAndShipAIControlled)
		{
			return;
		}
		MissionShip missionShip = _navalShipsLogic.GetShipAssignment(base.Formation.CachedClosestEnemyFormation.Team.Team.TeamSide, base.Formation.CachedClosestEnemyFormation.Formation.FormationIndex).MissionShip;
		Vec3 origin = missionShip.GlobalFrame.origin;
		_formationMainShip.ShipOrder.SetShipMovementOrder((origin + (origin - _formationMainShip.GlobalFrame.origin) * 2f).AsVec2);
		if (_ignoredShip != missionShip)
		{
			if (_ignoredShip != null)
			{
				_formationMainShip.AIController.RemoveShipFromCollisionIgnoreListOnAccountOfRamming(_ignoredShip);
			}
			_formationMainShip.AIController.AddShipToCollisionIgnoreListOnAccountOfRamming(missionShip);
			_ignoredShip = missionShip;
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	public override void RefreshShipReferences()
	{
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	protected override void OnBehaviorActivatedAux()
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		_formationMainShip.ShipOrder.SetBoardingTargetShip(null);
		_formationMainShip.ShipOrder.SetCutLoose(enable: false);
		_formationMainShip.ShipOrder.SetOrderOarsmenLevel(2);
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
		_isRammingActive = true;
		_ignoredShip = null;
	}

	public override void OnBehaviorCanceled()
	{
		_isRammingActive = false;
		if (_ignoredShip != null)
		{
			_formationMainShip.AIController.RemoveShipFromCollisionIgnoreListOnAccountOfRamming(_ignoredShip);
		}
	}

	public override void TickOccasionally()
	{
		if (_navalShipsLogic == null)
		{
			_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
			if (_navalShipsLogic == null)
			{
				return;
			}
		}
		CalculateAndSetShipOrders();
	}

	protected override float GetAiWeight()
	{
		if (_formationMainShip.Formation != base.Formation)
		{
			_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		}
		if (base.Formation.CachedClosestEnemyFormation != null)
		{
			MatrixFrame globalFrame = _navalShipsLogic.GetShipAssignment(base.Formation.CachedClosestEnemyFormation.Team.Team.TeamSide, base.Formation.CachedClosestEnemyFormation.Formation.FormationIndex).MissionShip.GlobalFrame;
			Vec3 vec = globalFrame.origin - _formationMainShip.GlobalFrame.origin;
			float num = vec.AsVec2.Normalized().DotProduct(_formationMainShip.Physics.LinearVelocity.AsVec2.Normalized());
			if (num > 0.9f * (_isRammingActive ? 0.5f : 1f))
			{
				float num2 = num * 1.5f;
				num = Math.Abs(_formationMainShip.Physics.LinearVelocity.AsVec2.Normalized().DotProduct(globalFrame.rotation.f.AsVec2.Normalized()));
				if (num <= 0.1f * (_isRammingActive ? 2f : 1f))
				{
					float length = _formationMainShip.Physics.LinearVelocity.Length;
					if (length > 3f * (_isRammingActive ? 0.5f : 1f))
					{
						float num3 = vec.AsVec2.Length / length;
						if (num3 < 30f * (_isRammingActive ? 2f : 1f))
						{
							if (num3 <= 10f)
							{
								num3 = 10f;
							}
							float num4 = 1.5f - num;
							float num5 = length / 3f;
							float num6 = 30f / num3;
							return num2 * num4 * num5 * num6;
						}
					}
				}
			}
		}
		return 0f;
	}
}
