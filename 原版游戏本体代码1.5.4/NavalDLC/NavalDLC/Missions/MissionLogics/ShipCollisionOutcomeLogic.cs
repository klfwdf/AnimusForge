using System;
using System.Collections.Generic;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class ShipCollisionOutcomeLogic : MissionLogic
{
	private const float EffectCooldownForShipInSeconds = 2f;

	private static readonly int _ramCollisionSoundEffectSoundId = SoundManager.GetEventGlobalIndex("event:/physics/vessel/ship_ramming");

	private readonly Mission _mission;

	private NavalShipsLogic _navalShipsLogic;

	private float _cameraShakeStartTime;

	private float _cameraShakeCurrentTimeWithFrequency;

	private float _cameraShakeIntensity;

	private Vec2 _cameraShakeInitialVelocity;

	private readonly Dictionary<MissionShip, float> _shipCollisionEffectCooldowns;

	private readonly Queue<(MissionShip, Vec3, Vec2, float)> _agentActionQueue;

	private MBFastRandom _effectRandom;

	public ShipCollisionOutcomeLogic(Mission mission)
	{
		_mission = mission;
		_shipCollisionEffectCooldowns = new Dictionary<MissionShip, float>();
		_agentActionQueue = new Queue<(MissionShip, Vec3, Vec2, float)>();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_effectRandom = new MBFastRandom();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipRammingEvent += OnShipRamming;
		_navalShipsLogic.ShipCollisionEvent += OnShipCollision;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipRammingEvent -= OnShipRamming;
		_navalShipsLogic.ShipCollisionEvent -= OnShipCollision;
	}

	public override void OnMissionTick(float dt)
	{
		while (_agentActionQueue.Count > 0)
		{
			(MissionShip, Vec3, Vec2, float) tuple = _agentActionQueue.Dequeue();
			HandleAgentActions(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
		}
		if (!(_cameraShakeStartTime > 0f))
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		if (_cameraShakeStartTime > currentTime - 2f)
		{
			float num = 1f - TaleWorlds.Library.MathF.Pow((currentTime - _cameraShakeStartTime) / 2f, 0.4f);
			float num2 = num * _cameraShakeIntensity * 0.6f;
			float num3 = num2 * 0.02f;
			_cameraShakeCurrentTimeWithFrequency += dt * 15f * num;
			if (num2 > 0f)
			{
				Vec3 vec = MBPerlin.NoiseVec3(_cameraShakeCurrentTimeWithFrequency);
				float num4 = (currentTime - _cameraShakeStartTime) / 2f;
				_mission.SetCustomCameraLocalOffset2(new Vec3(vec.x * num2, 0f, vec.z * num2));
				_mission.SetCustomCameraGlobalOffset(new Vec3(_cameraShakeInitialVelocity * (9.821568f * num4 - 32.17632f * num4 * num4 + 41.68837f * num4 * num4 * num4 - 25.76999f * num4 * num4 * num4 * num4 + 6.436929f * num4 * num4 * num4 * num4 * num4)));
				_mission.SetCustomCameraLocalRotationalOffset(new Vec3(vec.x * num3, vec.y * num3));
			}
		}
		else
		{
			_cameraShakeStartTime = 0f;
			_mission.SetCustomCameraLocalOffset2(Vec3.Zero);
			_mission.SetCustomCameraGlobalOffset(Vec3.Zero);
			_mission.SetCustomCameraLocalRotationalOffset(Vec3.Zero);
		}
	}

	private void OnShipRamming(MissionShip rammingShip, MissionShip rammedShip, float damagePercent, bool isFirstImpact, CapsuleData capsuleData, int ramQuality)
	{
		if (isFirstImpact)
		{
			Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = rammingShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(capsuleData.P2);
			Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody2 = rammedShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(capsuleData.P2);
			Vec3 collisionDirection = linearVelocityAtGlobalPointForEntityWithDynamicBody - linearVelocityAtGlobalPointForEntityWithDynamicBody2;
			collisionDirection.Normalize();
			ShipCollisionEffect(rammingShip, rammedShip.GameEntity, capsuleData.P2, collisionDirection, shouldMakeSound: false);
			ShipCollisionEffect(rammedShip, rammingShip.GameEntity, capsuleData.P2, collisionDirection, shouldMakeSound: false);
		}
	}

	private void OnShipCollision(MissionShip ship, WeakGameEntity targetEntity, BodyFlags bodyFlags, Vec3 averageContactPoint, Vec3 totalImpulseOnShip, bool isFirstImpact)
	{
		if (isFirstImpact)
		{
			Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = ship.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(averageContactPoint);
			Vec3 vec = ((!targetEntity.IsValid || !targetEntity.BodyFlag.HasAnyFlag(BodyFlags.Dynamic | BodyFlags.DynamicConvexHull)) ? Vec3.Zero : targetEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(averageContactPoint));
			Vec3 vec2 = linearVelocityAtGlobalPointForEntityWithDynamicBody - vec;
			vec2.Normalize();
			ShipCollisionEffect(ship, targetEntity, averageContactPoint, -vec2, shouldMakeSound: true);
		}
	}

	private void ShipCollisionEffect(MissionShip ship, WeakGameEntity targetEntity, Vec3 collisionGlobalPosition, Vec3 collisionDirection, bool shouldMakeSound)
	{
		float currentTime = Mission.Current.CurrentTime;
		if (_shipCollisionEffectCooldowns.TryGetValue(ship, out var value) && !(currentTime - value >= 2f))
		{
			return;
		}
		bool num = targetEntity.IsValid && targetEntity.BodyFlag.HasAnyFlag(BodyFlags.Dynamic | BodyFlags.DynamicConvexHull);
		Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = ship.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(collisionGlobalPosition);
		Vec3 vec = ((!num) ? Vec3.Zero : targetEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(collisionGlobalPosition));
		float num2 = (linearVelocityAtGlobalPointForEntityWithDynamicBody - vec).Normalize();
		float num3 = (num ? targetEntity.GetMass() : float.MaxValue);
		float mass = ship.GameEntity.Mass;
		float num4 = 1f / mass + 1f / num3;
		float num5 = num2 * num2 * (1f / num4);
		float num6 = 0.15f * (num5 / mass);
		if (!(num6 >= 1f))
		{
			return;
		}
		_shipCollisionEffectCooldowns[ship] = currentTime;
		Vec2 asVec = ship.Physics.LinearVelocity.AsVec2;
		asVec.Normalize();
		Agent mainAgent = _mission.MainAgent;
		if (mainAgent != null && mainAgent.IsActive() && ship.GetIsAgentOnShip(_mission.MainAgent))
		{
			_cameraShakeStartTime = currentTime;
			_cameraShakeIntensity = TaleWorlds.Library.MathF.Clamp(num6 * 0.3f, 1f, 3f);
			_cameraShakeInitialVelocity = asVec * num6 * 0.5f;
			_cameraShakeCurrentTimeWithFrequency = 0f;
		}
		MissionShip firstScriptOfType = targetEntity.GetFirstScriptOfType<MissionShip>();
		shouldMakeSound = shouldMakeSound && (firstScriptOfType == null || !_shipCollisionEffectCooldowns.TryGetValue(firstScriptOfType, out value) || currentTime - value >= 2f);
		if (shouldMakeSound)
		{
			SoundEventParameter parameter = new SoundEventParameter("Force", TaleWorlds.Library.MathF.Min(num6 * 0.1f, 0.5f));
			MBSoundEvent.PlaySound(_ramCollisionSoundEffectSoundId, ref parameter, collisionGlobalPosition);
		}
		_agentActionQueue.Enqueue((ship, collisionGlobalPosition, collisionDirection.AsVec2, num6));
		foreach (ShipUnmannedOar shipUnmannedOar in ship.ShipUnmannedOars)
		{
			float num7 = shipUnmannedOar.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin.DistanceSquared(collisionGlobalPosition);
			if (num7 < 900f)
			{
				float num8 = num6 * 0.04f * (30f / (TaleWorlds.Library.MathF.Sqrt(num7) + 0.1f)) * _effectRandom.NextFloat();
				if (num8 > 1f)
				{
					shipUnmannedOar.SetSlowDownPhaseForDuration(Math.Max(1f - num8 * 0.3f, 0f), Math.Min(num8, 3f));
				}
			}
		}
		foreach (ShipOarMachine leftSideShipOarMachine in ship.LeftSideShipOarMachines)
		{
			float num9 = leftSideShipOarMachine.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin.DistanceSquared(collisionGlobalPosition);
			if (num9 < 900f)
			{
				float num10 = num6 * 0.04f * (30f / (TaleWorlds.Library.MathF.Sqrt(num9) + 0.1f)) * _effectRandom.NextFloat();
				if (num10 > 1f)
				{
					leftSideShipOarMachine.SetSlowDownPhaseForDuration(Math.Max(1f - num10 * 0.3f, 0f), Math.Min(num10, 3f));
				}
			}
		}
		foreach (ShipOarMachine rightSideShipOarMachine in ship.RightSideShipOarMachines)
		{
			float num11 = rightSideShipOarMachine.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin.DistanceSquared(collisionGlobalPosition);
			if (num11 < 900f)
			{
				float num12 = num6 * 0.04f * (30f / (TaleWorlds.Library.MathF.Sqrt(num11) + 0.1f)) * _effectRandom.NextFloat();
				if (num12 > 1f)
				{
					rightSideShipOarMachine.SetSlowDownPhaseForDuration(Math.Max(1f - num12 * 0.3f, 0f), Math.Min(num12, 3f));
				}
			}
		}
	}

	public void ActivateCooldownForShip(MissionShip ship, float cooldown)
	{
		float currentTime = Mission.Current.CurrentTime;
		if (!_shipCollisionEffectCooldowns.TryGetValue(ship, out var value) || currentTime - value > 0f - cooldown)
		{
			_shipCollisionEffectCooldowns[ship] = currentTime - (2f - cooldown);
		}
	}

	private void HandleAgentActions(MissionShip ship, Vec3 collisionGlobalPosition, Vec2 shipDirection, float impactFactor)
	{
		foreach (Agent agent in _mission.Agents)
		{
			if (agent.IsUsingGameObject && agent.GetCurrentAnimationFlag(0).HasAnyFlag(AnimFlags.anf_align_with_ground))
			{
				continue;
			}
			float num = agent.Position.DistanceSquared(collisionGlobalPosition);
			if (!(num < 900f) || !ship.GetIsAgentOnShip(agent))
			{
				continue;
			}
			int effectiveSkill = MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, NavalSkills.Mariner);
			float num2 = impactFactor * 0.15f * (30f / (TaleWorlds.Library.MathF.Sqrt(num) + 0.1f)) * (0.5f + _effectRandom.NextFloat() * 0.5f) * (100f / ((float)effectiveSkill + 100f));
			if (num2 > 1f)
			{
				if (ship.ShipControllerMachine?.PilotAgent == agent)
				{
					num2 = Math.Min(num2, 2f);
				}
				float num3 = agent.GetMovementDirection().DotProduct(shipDirection);
				if (num3 > 0.7f)
				{
					agent.SetActionChannel(0, (num2 >= 3f) ? ActionIndexCache.act_stagger_backward_3 : ((num2 >= 2f) ? ActionIndexCache.act_stagger_backward_2 : ActionIndexCache.act_stagger_backward), ignorePriority: false, (AnimFlags)0uL, 0f, _effectRandom.NextFloatRanged(0.7f, 1.3f), -0.2f, 0.4f, _effectRandom.NextFloatRanged(0f, 0.3f));
				}
				else if (num3 < -0.7f)
				{
					agent.SetActionChannel(0, (num2 >= 3f) ? ActionIndexCache.act_stagger_forward_3 : ((num2 >= 2f) ? ActionIndexCache.act_stagger_forward_2 : ActionIndexCache.act_stagger_forward), ignorePriority: false, (AnimFlags)0uL, 0f, _effectRandom.NextFloatRanged(0.7f, 1.3f), -0.2f, 0.4f, _effectRandom.NextFloatRanged(0f, 0.3f));
				}
				else if (agent.GetMovementDirection().RightVec().DotProduct(shipDirection) > 0f)
				{
					agent.SetActionChannel(0, (num2 >= 3f) ? ActionIndexCache.act_stagger_left_3 : ((num2 >= 2f) ? ActionIndexCache.act_stagger_left_2 : ActionIndexCache.act_stagger_left), ignorePriority: false, (AnimFlags)0uL, 0f, _effectRandom.NextFloatRanged(0.7f, 1.3f), -0.2f, 0.4f, _effectRandom.NextFloatRanged(0f, 0.3f));
				}
				else
				{
					agent.SetActionChannel(0, (num2 >= 3f) ? ActionIndexCache.act_stagger_right_3 : ((num2 >= 2f) ? ActionIndexCache.act_stagger_right_2 : ActionIndexCache.act_stagger_right), ignorePriority: false, (AnimFlags)0uL, 0f, _effectRandom.NextFloatRanged(0.7f, 1.3f), -0.2f, 0.4f, _effectRandom.NextFloatRanged(0f, 0.3f));
				}
			}
		}
	}
}
