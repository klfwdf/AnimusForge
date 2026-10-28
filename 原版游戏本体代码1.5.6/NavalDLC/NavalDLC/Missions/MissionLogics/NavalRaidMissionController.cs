using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalRaidMissionController : MissionLogic
{
	public const string PlayerStandingPointEntityTag = "sp_naval_raid_player_spawn";

	private const int MaxPathNodeCount = 8;

	private const int MaxAllowedShipCount = 4;

	public NavalShipsLogic _shipsLogic;

	public NavalAgentsLogic _agentsLogic;

	private ShipCollisionOutcomeLogic _shipCollisionOutcomeLogic;

	public MatrixFrame[][] _landingFrames;

	private int[] _shipNextPathNodeIndices;

	public MatrixFrame[] _jumpingFrames;

	private readonly HashSet<MissionShip> _approachingShoutsPlayed = new HashSet<MissionShip>();

	private SoundEvent _warningBellsSoundEvent;

	private bool _hasLandingStarted;

	private bool _hasLandingCompleted;

	public override void OnBehaviorInitialize()
	{
		_shipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_agentsLogic = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
		_shipCollisionOutcomeLogic = Mission.Current.GetMissionBehavior<ShipCollisionOutcomeLogic>();
		_shipsLogic.ShipPreparedForAbandonmentEvent += OnShipPreparedForAbandonment;
		_shipsLogic.ShipSpawnedEvent += OnShipSpawned;
		_shipsLogic.ShipCollisionEvent += OnShipCollision;
		_landingFrames = new MatrixFrame[4][];
		for (int i = 0; i < _landingFrames.Length; i++)
		{
			_landingFrames[i] = new MatrixFrame[8];
		}
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTagExpression("landing(_\\d+)*"))
		{
			for (int j = 0; j < 8; j++)
			{
				string mainTag = $"landing_00{j + 1}";
				string text = item.Tags.FirstOrDefault((string tag) => tag.Contains(mainTag));
				if (!string.IsNullOrEmpty(text))
				{
					if (int.TryParse(text.Replace(mainTag + "_", ""), out var result))
					{
						_landingFrames[j][result] = item.GetGlobalFrame();
						break;
					}
					if (item.HasTag(text))
					{
						_landingFrames[j][0] = item.GetGlobalFrame();
						break;
					}
				}
			}
		}
		_jumpingFrames = new MatrixFrame[4];
		foreach (GameEntity item2 in Mission.Current.Scene.FindEntitiesWithTagExpression("jumping(_\\d+)*"))
		{
			for (int num = 0; num < 8; num++)
			{
				if (item2.HasTag($"jumping_00{num + 1}"))
				{
					_jumpingFrames[num] = item2.GetGlobalFrame();
					break;
				}
			}
		}
		_shipNextPathNodeIndices = new int[4];
		for (int num2 = 0; num2 < _shipNextPathNodeIndices.Length; num2++)
		{
			_shipNextPathNodeIndices[num2] = 8;
		}
	}

	private void OnShipCollision(MissionShip ship, WeakGameEntity targetEntity, BodyFlags bodyFlags, Vec3 averageContactPoint, Vec3 totalImpulseOnShip, bool isFirstImpact)
	{
		if (isFirstImpact && targetEntity == null && bodyFlags.HasAnyFlag(BodyFlags.BodyOwnerTerrain))
		{
			_shipCollisionOutcomeLogic.ActivateCooldownForShip(ship, float.MaxValue);
		}
	}

	private void OnShipSpawned(MissionShip ship)
	{
		foreach (UsableMachine item in ship.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<UsableMachine>())
		{
			foreach (StandingPoint standingPoint in item.StandingPoints)
			{
				standingPoint.SetIsDisabledForPlayersSynched(value: true);
			}
		}
		ship.ShipOrder.SetEnforcedSailUsage(-1);
		if (ship.ShipOrigin.IsPlayerShip)
		{
			WeakGameEntity firstChildEntityWithTagRecursive = ship.GameEntity.GetFirstChildEntityWithTagRecursive("sp_naval_raid_player_spawn");
			if (firstChildEntityWithTagRecursive != null)
			{
				GameEntity playerStandingPointEntity = GameEntity.CreateFromWeakEntity(firstChildEntityWithTagRecursive);
				ship.SetPlayerStandingPointEntity(playerStandingPointEntity);
			}
		}
	}

	private void OnShipPreparedForAbandonment(MissionShip ship)
	{
		Vec3 v = ship.Physics.PhysicsBoundingBoxWithoutChildren.center;
		v = ship.GlobalFrame.TransformToParent(in v);
		SortedList<float, ShipAttachmentPointMachine> sortedList = new SortedList<float, ShipAttachmentPointMachine>();
		foreach (ShipAttachmentPointMachine attachmentPointMachine in ship.AttachmentPointMachines)
		{
			Vec3 globalPosition = attachmentPointMachine.GameEntity.GlobalPosition;
			Vec3 f = _jumpingFrames[ship.Index].rotation.f;
			f.Normalize();
			Vec3 v2 = globalPosition - v;
			v2 += attachmentPointMachine.GameEntity.GetGlobalFrame().rotation.f;
			v2.Normalize();
			float key = Vec3.DotProduct(v2, f);
			sortedList.Add(key, attachmentPointMachine);
		}
		int num = 0;
		switch (ship.ShipOrigin.Hull.Type)
		{
		case ShipHull.ShipType.Light:
			num = 4;
			break;
		case ShipHull.ShipType.Medium:
			num = 6;
			break;
		case ShipHull.ShipType.Heavy:
			num = 8;
			break;
		}
		for (int i = 0; i < sortedList.Count; i++)
		{
			ShipAttachmentPointMachine shipAttachmentPointMachine = sortedList.Values[i];
			if (i >= sortedList.Count - num)
			{
				shipAttachmentPointMachine.SetJumpOffAction(ActionIndexCache.act_raid_jump);
				continue;
			}
			shipAttachmentPointMachine.SetIsDisabledForAI(isDisabledForAI: true);
			shipAttachmentPointMachine.SetScriptComponentToTick(shipAttachmentPointMachine.GetTickRequirement());
			foreach (StandingPoint standingPoint in shipAttachmentPointMachine.StandingPoints)
			{
				standingPoint.SetIsDisabledForPlayersSynched(value: true);
			}
		}
	}

	public override void OnDeploymentFinished()
	{
		foreach (MissionShip allShip in _shipsLogic.AllShips)
		{
			if (allShip.IsPlayerShip)
			{
				allShip.SetPlayerStandingPointEntity();
			}
			allShip.GameEntity.UpdateBodyRestOffset(0f - allShip.MissionShipObject.LandingDepth);
			for (int num = 7; num >= 0; num--)
			{
				if (!_landingFrames[allShip.Index][num].IsZero)
				{
					_shipNextPathNodeIndices[allShip.Index] = num;
					break;
				}
			}
			allShip.SetController(ShipControllerType.AI, autoUpdateController: false);
			allShip.ShipOrder.SetShipMovementOrder(_landingFrames[allShip.Index][_shipNextPathNodeIndices[allShip.Index]].origin.AsVec2, _landingFrames[allShip.Index][_shipNextPathNodeIndices[allShip.Index]].rotation.f.AsVec2.Normalized());
			allShip.SetCanBeTakenOver(value: false);
			if (allShip.ShipSiegeWeapon == null)
			{
				continue;
			}
			allShip.ShipSiegeWeapon.SetDisabledSynched();
			WeakGameEntity weakGameEntity = allShip.ShipSiegeWeapon.GameEntity;
			while (weakGameEntity != null && !weakGameEntity.HasTag("upgrade_slot"))
			{
				weakGameEntity = weakGameEntity.Parent;
			}
			allShip.ShipSiegeWeapon.GameEntity.SetVisibilityExcludeParents(visible: false);
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			weakGameEntity.GetChildrenRecursive(ref children);
			foreach (WeakGameEntity item in children)
			{
				item.SetVisibilityExcludeParents(visible: false);
			}
		}
		base.Mission.DefenderTeam.GetFormation(FormationClass.Ranged)?.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag("player_spawn_frame");
		if (gameEntity != null)
		{
			_warningBellsSoundEvent = SoundEvent.CreateEventFromString("event:/mission/ambient/detail/warning_bells", Mission.Current.Scene);
			_warningBellsSoundEvent.PlayInPosition(gameEntity.GetGlobalFrame().origin);
		}
	}

	public override void OnMissionTick(float dt)
	{
		foreach (MissionShip allShip in _shipsLogic.AllShips)
		{
			if (allShip.ShipOrder.MovementOrderEnum != ShipOrder.ShipMovementOrderEnum.Move)
			{
				continue;
			}
			float num = _landingFrames[allShip.Index][_shipNextPathNodeIndices[allShip.Index]].origin.AsVec2.DistanceSquared(allShip.GlobalFrame.origin.AsVec2);
			if (_shipNextPathNodeIndices[allShip.Index] == 0)
			{
				if (num > 225f && num < 400f)
				{
					MatrixFrame globalFrame = allShip.GlobalFrame;
					globalFrame.rotation.u = Vec3.Up;
					allShip.Physics.SetAnchorFrame(_landingFrames[allShip.Index][0].origin.AsVec2, _landingFrames[allShip.Index][0].rotation.f.AsVec2);
					allShip.SetAnchor(isAnchored: true);
					allShip.EnableBlockers();
					if (_approachingShoutsPlayed.Add(allShip))
					{
						MatrixFrame globalFrame2 = allShip.GlobalFrame;
						SoundManager.StartOneShotEvent("event:/alerts/naval/getting_rammed", in globalFrame2.origin);
					}
				}
				else if (!allShip.BeingAbandoned && num < 225f)
				{
					allShip.ShipOrder.SetShipStopOrder();
					string eventFullName = ((allShip.ShipOrigin.Hull.Type == ShipHull.ShipType.Heavy) ? "event:/mission/movement/vessel/ship_ground_heavy" : "event:/mission/movement/vessel/ship_ground");
					MatrixFrame globalFrame2 = allShip.GlobalFrame;
					SoundManager.StartOneShotEvent(eventFullName, in globalFrame2.origin);
					globalFrame2 = allShip.GlobalFrame;
					SoundManager.StartOneShotEvent("event:/alerts/report/battle_winning", in globalFrame2.origin);
					allShip.PrepareForAbandonment();
					allShip.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
					_hasLandingStarted = true;
					(base.Mission.DefenderTeam.TeamAI as TeamAINavalRaidDefenderComponent).OnShipLanded();
				}
			}
			else if (num < 2500f)
			{
				_shipNextPathNodeIndices[allShip.Index]--;
				allShip.ShipOrder.SetShipMovementOrder(_landingFrames[allShip.Index][_shipNextPathNodeIndices[allShip.Index]].origin.AsVec2, _landingFrames[allShip.Index][_shipNextPathNodeIndices[allShip.Index]].rotation.f.AsVec2);
			}
		}
		if (!_hasLandingStarted || _hasLandingCompleted)
		{
			return;
		}
		_hasLandingCompleted = true;
		foreach (Agent activeAgent in Mission.Current.AttackerTeam.ActiveAgents)
		{
			if (activeAgent.IsAIControlled && activeAgent.GetSteppedEntity() != null)
			{
				_hasLandingCompleted = false;
				break;
			}
		}
		if (_hasLandingCompleted)
		{
			(base.Mission.DefenderTeam.TeamAI as TeamAINavalRaidDefenderComponent).OnLandingCompleted();
		}
	}

	public override void OnFixedMissionTick(float fixedDt)
	{
		foreach (MissionShip allShip in _shipsLogic.AllShips)
		{
			if (allShip.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Stop && allShip.BeingAbandoned)
			{
				allShip.SetAnchor(isAnchored: false);
				MatrixFrame bodyWorldTransform = allShip.GameEntity.GetBodyWorldTransform();
				Vec3 u = bodyWorldTransform.rotation.u;
				Vec3 f = bodyWorldTransform.rotation.f;
				Vec3 vec = u - f * Vec3.DotProduct(u, f);
				vec.Normalize();
				Vec3 vec2 = Vec3.Up - f * Vec3.DotProduct(Vec3.Up, f);
				vec2.Normalize();
				float num = MathF.Atan2(Vec3.DotProduct(f, Vec3.CrossProduct(vec2, vec)), Vec3.DotProduct(vec2, vec));
				float num2 = Vec3.DotProduct(allShip.Physics.AngularVelocity, f);
				float num3 = 1.8f;
				float num4 = 1f;
				float num5 = 240f / fixedDt / num3;
				float num6 = num5 * num5;
				float num7 = 2f * num4 * num5;
				Vec3 torqueVec = f * ((0f - num) * num6 - num2 * num7);
				torqueVec /= 4200000f;
				allShip.Physics.ApplyTorque(in torqueVec, GameEntityPhysicsExtensions.ForceMode.Acceleration);
			}
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		Team team = agent.Team;
		if (agent.IsAIControlled && team.IsAttacker)
		{
			AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
			component.SetBlockOffShipConsideration(canCheckOffShipConsideration: false);
			component.SetBlockFormationCleanupOnShipAdabandonment(canCleanFormationOnShipAdabandonment: false);
			AgentNavalAIComponent component2 = agent.GetComponent<AgentNavalAIComponent>();
			int index = agent.Formation.Index;
			component2.ActivateSwimToShore(_jumpingFrames[index]);
		}
	}

	public override void OnAgentControllerSetToPlayer(Agent agent)
	{
		agent.GetComponent<AgentNavalAIComponent>().DeactivateSwimToShore();
	}

	public override void OnMissionStateFinalized()
	{
		_shipsLogic.ShipPreparedForAbandonmentEvent -= OnShipPreparedForAbandonment;
		_shipsLogic.ShipSpawnedEvent -= OnShipSpawned;
		_shipsLogic.ShipCollisionEvent -= OnShipCollision;
		if (_warningBellsSoundEvent != null)
		{
			_warningBellsSoundEvent.Stop();
			_warningBellsSoundEvent = null;
		}
	}

	public override void OnMissionResultReady(MissionResult missionResult)
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFFFFF7u));
		}
	}
}
