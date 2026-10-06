using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Missions.AgentBehaviors;

public class AlarmedBehaviorGroup : AgentBehaviorGroup
{
	public const float SafetyDistance = 15f;

	public const float SafetyDistanceSquared = 225f;

	private const float NearbyDistanceThreshold = 1f;

	private const float NearbyDistanceThresholdSquared = 1f;

	private readonly MissionFightHandler _missionFightHandler;

	public bool DisableCalmDown;

	private readonly BasicMissionTimer _alarmedTimer;

	private readonly BasicMissionTimer _checkCalmDownTimer;

	public bool DoNotCheckForAlarmFactorIncrease;

	public bool DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy;

	private bool _canMoveWhenCautious = true;

	private readonly MissionTimer _lastSuspiciousPositionTimer;

	private readonly MissionTimer _alarmYellTimer;

	private readonly List<Agent> _ignoredAgentsForAlarm;

	private readonly MBList<GameEntity> _stealthIndoorLightingAreas;

	private readonly MBList<StealthBox> _stealthBoxes;

	private MissionTime _lastAlarmTriggerTime;

	public float AlarmFactor { get; private set; }

	public AlarmedBehaviorGroup(AgentNavigator navigator, Mission mission)
		: base(navigator, mission)
	{
		_alarmedTimer = new BasicMissionTimer();
		_checkCalmDownTimer = new BasicMissionTimer();
		_missionFightHandler = base.Mission.GetMissionBehavior<MissionFightHandler>();
		_lastSuspiciousPositionTimer = new MissionTimer(10f);
		_alarmYellTimer = new MissionTimer(10f);
		_ignoredAgentsForAlarm = new List<Agent>(0);
		_lastAlarmTriggerTime = MissionTime.Zero;
		base.Mission.OnAddSoundAlarmFactorToAgents += OnAddSoundAlarmFactor;
		List<GameEntity> entities = new List<GameEntity>();
		base.OwnerAgent.Mission.Scene.GetAllEntitiesWithScriptComponent<StealthIndoorLightingArea>(ref entities);
		_stealthIndoorLightingAreas = new MBList<GameEntity>(entities);
		List<GameEntity> entities2 = new List<GameEntity>();
		base.OwnerAgent.Mission.Scene.GetAllEntitiesWithScriptComponent<StealthBox>(ref entities2);
		_stealthBoxes = new MBList<StealthBox>(entities2.Select((GameEntity ge) => ge.GetFirstScriptOfType<StealthBox>()));
	}

	public void SetCanMoveWhenCautious(bool value)
	{
		_canMoveWhenCautious = value;
	}

	private void UpdateAgentAlarmState(float dt)
	{
		if (!base.OwnerAgent.IsAlarmed())
		{
			bool flag = base.OwnerAgent.IsAIAtMoveDestination();
			if ((!base.OwnerAgent.IsCautious() || flag) && _lastAlarmTriggerTime.ElapsedSeconds > 2f)
			{
				float alarmFactor = AlarmFactor;
				AlarmFactor = Math.Max(0f, AlarmFactor - (base.OwnerAgent.IsPatrollingCautious() ? 0.025f : (_canMoveWhenCautious ? 0.125f : 0.08f)) * dt);
				if (alarmFactor >= 1f && AlarmFactor < 1f)
				{
					AlarmFactor = 0.3f;
				}
			}
			bool hasVisualOnEnemy = false;
			bool hasVisualOnCorpse = false;
			bool flag2 = false;
			if (!DoNotCheckForAlarmFactorIncrease)
			{
				Vec3 vec;
				if (!base.OwnerAgent.IsHuman || !base.OwnerAgent.AgentVisuals.IsValid())
				{
					vec = base.OwnerAgent.LookDirection;
				}
				else
				{
					MatrixFrame frame = base.OwnerAgent.Frame;
					ref Mat3 rotation = ref frame.rotation;
					MatrixFrame boneEntitialFrame = base.OwnerAgent.GetBoneEntitialFrame(base.OwnerAgent.Monster.HeadLookDirectionBoneIndex, useBoneMapping: true);
					vec = rotation.TransformToParent(in boneEntitialFrame.rotation.f);
				}
				Vec3 vb = vec;
				WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
				worldPosition.SetVec2(worldPosition.AsVec2 + vb.AsVec2.Normalized() * 1.25f);
				float num = MBMath.ClampFloat(TaleWorlds.Library.MathF.Tan(worldPosition.GetGroundVec3().z - base.OwnerAgent.Position.z) * 1f, -0.025f, 0.55f);
				vb = vb.RotateAboutAnArbitraryVector(Vec3.CrossProduct(Vec3.Up, vb).NormalizedCopy(), 0.02f - num);
				foreach (Agent allAgent in base.OwnerAgent.Mission.AllAgents)
				{
					float num2 = 0f;
					float num3 = 0f;
					AgentState state = allAgent.State;
					bool flag3 = allAgent.AgentVisuals.IsValid();
					if (state == AgentState.Deleted || state == AgentState.Routed || state == AgentState.None || !flag3)
					{
						continue;
					}
					AgentFlag agentFlags = allAgent.GetAgentFlags();
					bool flag4 = _ignoredAgentsForAlarm.IndexOf(allAgent) >= 0;
					if (allAgent == base.OwnerAgent || !agentFlags.HasAllFlags(AgentFlag.CanAttack | AgentFlag.IsHumanoid) || ((state == AgentState.Active || flag4) && (state != AgentState.Active || (!allAgent.IsAlarmed() && (!allAgent.IsPatrollingCautious() || flag4 || !(allAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AlarmFactor > AlarmFactor + 0.1f)) && !base.OwnerAgent.IsEnemyOf(allAgent)))))
					{
						continue;
					}
					if (!DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy)
					{
						int effectiveSkill = MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(allAgent, DefaultSkills.Roguery);
						float equipmentStealthBonus = MissionGameModels.Current.AgentStatCalculateModel.GetEquipmentStealthBonus(allAgent);
						float sneakingNoiseMultiplier = Math.Max(0f, 1f - ((float)effectiveSkill * 0.0001f + equipmentStealthBonus * 0.002f));
						num2 += GetSoundFactor(allAgent, sneakingNoiseMultiplier);
					}
					num3 += GetVisualFactor(vb, allAgent, _stealthIndoorLightingAreas, ref hasVisualOnCorpse, ref hasVisualOnEnemy);
					float num4 = Math.Min(3f, num2 + num3);
					if (num4 > 0f && (!hasVisualOnEnemy || !DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy))
					{
						AlarmFactor += num4 * dt * Campaign.Current.Models.DifficultyModel.GetStealthDifficultyMultiplier();
						if (state == AgentState.Active && allAgent.Position.DistanceSquared(base.OwnerAgent.Position) < 1f)
						{
							flag2 = true;
						}
						_lastAlarmTriggerTime = MissionTime.Now;
					}
					if (AlarmFactor >= 1f && base.OwnerAgent.IsAlarmStateNormal())
					{
						base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Cautious);
						WorldPosition lastSuspiciousPosition = allAgent.GetWorldPosition();
						lastSuspiciousPosition.SetVec2(lastSuspiciousPosition.AsVec2 + (base.OwnerAgent.Position.AsVec2 - lastSuspiciousPosition.AsVec2).Normalized() * (((base.OwnerAgent.Position.AsVec2 - lastSuspiciousPosition.AsVec2).LengthSquared < 25f) ? 0f : 2f));
						SetAILastSuspiciousPositionHelper(in lastSuspiciousPosition, checkNavMeshForCorrection: true);
						_lastSuspiciousPositionTimer.Reset();
					}
					else if (num4 > 0f && (base.OwnerAgent.IsCautious() || base.OwnerAgent.IsPatrollingCautious()) && _lastSuspiciousPositionTimer.Check(reset: true))
					{
						WorldPosition lastSuspiciousPosition2 = allAgent.GetWorldPosition();
						lastSuspiciousPosition2.SetVec2(lastSuspiciousPosition2.AsVec2 + (base.OwnerAgent.Position.AsVec2 - lastSuspiciousPosition2.AsVec2).Normalized() * (((base.OwnerAgent.Position.AsVec2 - lastSuspiciousPosition2.AsVec2).LengthSquared < 25f) ? 0f : 2f));
						SetAILastSuspiciousPositionHelper(in lastSuspiciousPosition2, checkNavMeshForCorrection: true);
					}
					if (num3 > 0f && base.OwnerAgent.IsPatrollingCautious() && (!allAgent.IsActive() || (!allAgent.IsEnemyOf(base.OwnerAgent) && !allAgent.IsAlarmed())))
					{
						_ignoredAgentsForAlarm.Add(allAgent);
					}
				}
			}
			if ((AlarmFactor >= 2f && (hasVisualOnEnemy || flag2)) || (AlarmFactor >= 1f && hasVisualOnEnemy && flag2))
			{
				base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
				_alarmYellTimer.Set(-9f);
				AlarmFactor = 2f;
			}
			else if (_canMoveWhenCautious && AlarmFactor >= 2f && base.OwnerAgent.IsCautious() && hasVisualOnCorpse)
			{
				base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.PatrollingCautious);
			}
			else if (AlarmFactor < 0.0001f)
			{
				base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.None);
			}
			for (int num5 = _ignoredAgentsForAlarm.Count - 1; num5 >= 0; num5--)
			{
				Agent agent = _ignoredAgentsForAlarm[num5];
				if (agent.IsActive() && (agent.IsAlarmStateNormal() || agent.IsAlarmed()))
				{
					_ignoredAgentsForAlarm.RemoveAt(num5);
				}
			}
			AlarmFactor = Math.Min(AlarmFactor, 2f);
		}
		else if (_alarmYellTimer.Check(reset: true))
		{
			base.OwnerAgent.MakeVoice(SkinVoiceManager.VoiceType.Yell, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			base.OwnerAgent.Mission.AddSoundAlarmFactorToAgents(base.OwnerAgent, base.OwnerAgent.Position + new Vec3(0f, 0f, base.OwnerAgent.GetEyeGlobalHeight()), 10f);
		}
	}

	private void SetAILastSuspiciousPositionHelper(in WorldPosition lastSuspiciousPosition, bool checkNavMeshForCorrection)
	{
		if (_canMoveWhenCautious)
		{
			base.OwnerAgent.SetAILastSuspiciousPosition(lastSuspiciousPosition, checkNavMeshForCorrection);
			return;
		}
		WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
		worldPosition.SetVec2(worldPosition.AsVec2 + (lastSuspiciousPosition.AsVec2 - base.OwnerAgent.Position.AsVec2).Normalized() * 0.1f);
		base.OwnerAgent.SetAILastSuspiciousPosition(worldPosition, checkNavMeshForCorrection: false);
	}

	private float GetSoundFactor(Agent currentAgent, float sneakingNoiseMultiplier)
	{
		if (currentAgent.Velocity.LengthSquared > 0.010000001f)
		{
			float num = (currentAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight()) - (base.OwnerAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight()))).Normalize();
			float num2 = 125f * Math.Min(1f, currentAgent.AverageVelocity.Length / currentAgent.GetMaximumForwardUnlimitedSpeed());
			bool flag = false;
			if (currentAgent.Mission.Scene.GetWaterLevelAtPosition(currentAgent.Position.AsVec2, !GameNetwork.IsMultiplayer, checkWaterBodyEntities: true) > currentAgent.Position.z)
			{
				currentAgent.Mission.Scene.GetGroundHeightAndBodyFlagsAtPosition(currentAgent.Position, out var contactPointFlags, BodyFlags.CommonCollisionExcludeFlagsForAgent);
				if ((contactPointFlags & (BodyFlags.Moveable | BodyFlags.Sinking)) != BodyFlags.Moveable)
				{
					flag = true;
					num2 *= 4f;
				}
			}
			if (currentAgent.HasMount || num <= currentAgent.CollisionCapsule.Radius * 2.5f)
			{
				num2 *= 12f;
			}
			else if (currentAgent.State == AgentState.Active && currentAgent.AgentVisuals.IsValid())
			{
				switch (currentAgent.AgentVisuals.GetMovementMode())
				{
				case HumanWalkingMovementMode.Walking:
					num2 *= 0.7f;
					break;
				case HumanWalkingMovementMode.CrouchRunning:
					num2 *= (flag ? 0.45f : 0.25f);
					break;
				case HumanWalkingMovementMode.CrouchWalking:
					num2 *= (flag ? 0.1f : 0f);
					break;
				}
			}
			num2 *= sneakingNoiseMultiplier;
			num2 /= 20f + num * num * 2.5f;
			if (num2 > 0.125f)
			{
				return num2;
			}
		}
		return 0f;
	}

	public float GetVisualFactor(Vec3 usedGlobalLookDirection, Agent currentAgent, MBReadOnlyList<GameEntity> stealthIndoorLightingAreas, ref bool hasVisualOnCorpse, ref bool hasVisualOnEnemy)
	{
		Vec3 vec = currentAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight()) - (base.OwnerAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight()));
		float num = 0f;
		float num2 = Vec3.DotProduct(vec, usedGlobalLookDirection);
		bool flag = vec.LengthSquared < 1f;
		if (num2 > 0f && (flag || !IsAgentCoveredByAStealthBox(currentAgent)))
		{
			float distance = vec.Normalize();
			bool currentAgentHasSpeed = currentAgent.Velocity.LengthSquared > 0.010000001f;
			float equipmentStealthBonus = MissionGameModels.Current.AgentStatCalculateModel.GetEquipmentStealthBonus(currentAgent);
			float visualStrength = GetVisualStrength(vec, usedGlobalLookDirection, currentAgent, currentAgentHasSpeed, distance, equipmentStealthBonus);
			if (visualStrength > 0.001f)
			{
				bool isDayTime = base.OwnerAgent.Mission.Scene.IsDayTime;
				Vec3 position = currentAgent.Position;
				float ambientLightStrength = (isDayTime ? 0.7f : 0.2f);
				float sunMoonLightStrength = (isDayTime ? 1f : 0.15f);
				foreach (GameEntity stealthIndoorLightingArea in stealthIndoorLightingAreas)
				{
					StealthIndoorLightingArea firstScriptOfType = stealthIndoorLightingArea.GetFirstScriptOfType<StealthIndoorLightingArea>();
					if (firstScriptOfType.IsPointIn(position))
					{
						ambientLightStrength = firstScriptOfType.AmbientLightStrength;
						sunMoonLightStrength = firstScriptOfType.SunMoonLightStrength;
						break;
					}
				}
				float visualStrengthOfAgentVisual = base.OwnerAgent.AgentVisuals.GetVisualStrengthOfAgentVisual(currentAgent.AgentVisuals, base.OwnerAgent.Mission, ambientLightStrength, sunMoonLightStrength, base.OwnerAgent.Index);
				visualStrength *= visualStrengthOfAgentVisual;
				if (visualStrength > 0.3f)
				{
					num += visualStrength;
					if (!currentAgent.IsActive())
					{
						hasVisualOnCorpse = true;
					}
					else if (base.OwnerAgent.IsEnemyOf(currentAgent))
					{
						hasVisualOnEnemy = true;
						if (currentAgent != Agent.Main && Agent.Main != null && currentAgent.IsFriendOf(Agent.Main))
						{
							num *= 0.5f;
						}
					}
				}
			}
		}
		return num;
	}

	private float GetVisualStrength(Vec3 positionDifferenceDirection, Vec3 usedGlobalLookDirection, Agent currentAgent, bool currentAgentHasSpeed, float distance, float equipmentStealthBonus)
	{
		float num = System.MathF.PI * 19f / 40f;
		float num2 = System.MathF.PI * 57f / 200f;
		Mat3 mat = new Mat3(usedGlobalLookDirection.CrossProductWithUp().NormalizedCopy(), in usedGlobalLookDirection, in Vec3.Up);
		mat.u = Vec3.CrossProduct(mat.s, mat.f);
		Vec3 vec = mat.TransformToLocal(in positionDifferenceDirection);
		float a = TaleWorlds.Library.MathF.Atan2(vec.z, vec.x);
		float num3 = TaleWorlds.Library.MathF.Acos(MBMath.ClampFloat(vec.y, 0f, 1f));
		TaleWorlds.Library.MathF.SinCos(a, out var sa, out var ca);
		float num4 = num * num2 / TaleWorlds.Library.MathF.Sqrt(num * num * sa * sa + num2 * num2 * ca * ca);
		float num5 = ((num3 >= num4) ? 0f : Math.Min(1f, 0.025f + (num4 - num3) / num4));
		num5 *= num5;
		if (currentAgent.HasMount || distance <= currentAgent.CollisionCapsule.Radius * 6.5f)
		{
			num5 *= 15f;
		}
		else if (currentAgent.AgentVisuals.IsValid() && currentAgent.CrouchMode)
		{
			num5 *= (currentAgentHasSpeed ? 0.9f : 0.8f);
		}
		if (currentAgent.State != AgentState.Active || currentAgent.IsAlarmed())
		{
			num5 *= 1.45f;
		}
		float num6 = Math.Max(0f, 1f - equipmentStealthBonus * 0.0025f);
		num5 *= 575f * num6;
		return num5 / (5f + distance * distance * 1.1f);
	}

	public void ResetAlarmFactor()
	{
		AlarmFactor = 0f;
	}

	private void AddAlarmFactor(float addedAlarmFactor, Agent suspiciousAgent)
	{
		AlarmFactor += addedAlarmFactor;
		_lastAlarmTriggerTime = MissionTime.Now;
		if (AlarmFactor >= 1f && base.OwnerAgent.IsAlarmStateNormal())
		{
			base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Cautious);
			if (suspiciousAgent != null)
			{
				SetAILastSuspiciousPositionHelper(suspiciousAgent.GetWorldPosition(), checkNavMeshForCorrection: true);
			}
			else
			{
				SetAILastSuspiciousPositionHelper(base.OwnerAgent.GetWorldPosition(), checkNavMeshForCorrection: false);
			}
			_lastSuspiciousPositionTimer.Reset();
		}
		else if ((base.OwnerAgent.IsCautious() || base.OwnerAgent.IsPatrollingCautious()) && _lastSuspiciousPositionTimer.Check(reset: true))
		{
			if (suspiciousAgent != null)
			{
				SetAILastSuspiciousPositionHelper(suspiciousAgent.GetWorldPosition(), checkNavMeshForCorrection: true);
			}
			else
			{
				SetAILastSuspiciousPositionHelper(base.OwnerAgent.GetWorldPosition(), checkNavMeshForCorrection: false);
			}
		}
	}

	public void AddAlarmFactor(float addedAlarmFactor, in WorldPosition suspiciousPosition)
	{
		AlarmFactor += addedAlarmFactor;
		_lastAlarmTriggerTime = MissionTime.Now;
		if (AlarmFactor >= 1f && base.OwnerAgent.IsAlarmStateNormal())
		{
			base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Cautious);
			SetAILastSuspiciousPositionHelper(in suspiciousPosition, checkNavMeshForCorrection: true);
			_lastSuspiciousPositionTimer.Reset();
		}
		else if ((base.OwnerAgent.IsCautious() || base.OwnerAgent.IsPatrollingCautious()) && _lastSuspiciousPositionTimer.Check(reset: true))
		{
			SetAILastSuspiciousPositionHelper(in suspiciousPosition, checkNavMeshForCorrection: true);
		}
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (base.Mission.AllowAiTicking && base.OwnerAgent.IsAIControlled)
		{
			HandleMissiles(dt);
			if (base.OwnerAgent.GetAgentFlags().HasAllFlags(AgentFlag.CanWieldWeapon | AgentFlag.CanGetAlarmed))
			{
				UpdateAgentAlarmState(dt);
			}
		}
		if (!base.IsActive)
		{
			return;
		}
		if (base.ScriptedBehavior != null)
		{
			if (!base.ScriptedBehavior.IsActive)
			{
				DisableAllBehaviors();
				base.ScriptedBehavior.IsActive = true;
			}
		}
		else
		{
			float num = 0f;
			int num2 = -1;
			for (int i = 0; i < Behaviors.Count; i++)
			{
				float availability = Behaviors[i].GetAvailability(isSimulation);
				if (availability > num)
				{
					num = availability;
					num2 = i;
				}
			}
			if (num > 0f && num2 != -1 && !Behaviors[num2].IsActive)
			{
				DisableAllBehaviors();
				Behaviors[num2].IsActive = true;
			}
		}
		TickActiveBehaviors(dt, isSimulation);
	}

	private void TickActiveBehaviors(float dt, bool isSimulation)
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				behavior.Tick(dt, isSimulation);
			}
		}
	}

	public override float GetScore(bool isSimulation)
	{
		if (base.OwnerAgent.IsAlarmed() || base.OwnerAgent.IsPatrollingCautious() || base.OwnerAgent.IsCautious())
		{
			if (!DisableCalmDown && _alarmedTimer.ElapsedTime > 10f && _checkCalmDownTimer.ElapsedTime > 1f)
			{
				_checkCalmDownTimer.Reset();
				if (!IsNearDanger())
				{
					base.OwnerAgent.DisableScriptedMovement();
				}
			}
			return 1f;
		}
		if (IsNearDanger())
		{
			AlarmAgent(base.OwnerAgent);
			return 1f;
		}
		return 0f;
	}

	private bool IsNearDanger()
	{
		float distanceSquared;
		Agent closestAlarmSource = GetClosestAlarmSource(out distanceSquared);
		if (closestAlarmSource != null)
		{
			if (!(distanceSquared < 225f))
			{
				return Navigator.CanSeeAgent(closestAlarmSource);
			}
			return true;
		}
		return false;
	}

	public Agent GetClosestAlarmSource(out float distanceSquared)
	{
		distanceSquared = float.MaxValue;
		if (_missionFightHandler == null || !_missionFightHandler.IsThereActiveFight())
		{
			return null;
		}
		Agent result = null;
		foreach (Agent dangerSource in _missionFightHandler.GetDangerSources(base.OwnerAgent))
		{
			float num = dangerSource.Position.DistanceSquared(base.OwnerAgent.Position);
			if (num < distanceSquared)
			{
				distanceSquared = num;
				result = dangerSource;
			}
		}
		return result;
	}

	public static void AlarmAgent(Agent agent)
	{
		agent.SetWatchState(Agent.WatchState.Alarmed);
	}

	protected override void OnActivate()
	{
		TextObject textObject = new TextObject("{=!}{p0} {p1} activate alarmed behavior group.");
		textObject.SetTextVariable("p0", base.OwnerAgent.Name);
		textObject.SetTextVariable("p1", base.OwnerAgent.Index);
		_alarmedTimer.Reset();
		_checkCalmDownTimer.Reset();
		base.OwnerAgent.DisableScriptedMovement();
		base.OwnerAgent.ClearTargetFrame();
		Navigator.SetItemsVisibility(isVisible: false);
		if (CampaignMission.Current.Location != null)
		{
			LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.OwnerAgent.Origin);
			if (locationCharacter != null && locationCharacter.ActionSetCode != locationCharacter.AlarmedActionSetCode)
			{
				AnimationSystemData animationSystemData = locationCharacter.GetAgentBuildData().AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(locationCharacter.AlarmedActionSetCode), locationCharacter.Character.GetStepSize(), hasClippingPlane: false);
				base.OwnerAgent.SetActionSet(ref animationSystemData);
			}
		}
		if (Navigator.MemberOfAlley != null || MissionFightHandler.IsAgentAggressive(base.OwnerAgent))
		{
			DisableCalmDown = true;
		}
	}

	private void HandleMissiles(float dt)
	{
		foreach (Mission.Missile missiles in base.Mission.MissilesList)
		{
			Vec3 lineSegmentBegin = missiles.GetPosition();
			Vec3 velocity = missiles.GetVelocity();
			float num = velocity.Length / 20f + 0.1f;
			float num2 = 0.1f;
			float num3 = 20f;
			float num4 = TaleWorlds.Library.MathF.Sqrt(num * num / num2 - num3);
			if (!base.OwnerAgent.IsAlarmed() && base.OwnerAgent.IsActive() && base.OwnerAgent.IsAIControlled && base.OwnerAgent.GetAgentFlags().HasAnyFlag(AgentFlag.CanGetAlarmed) && base.OwnerAgent.RiderAgent == null && base.OwnerAgent != missiles.ShooterAgent)
			{
				Vec3 point = base.OwnerAgent.Position;
				point.z += base.OwnerAgent.GetEyeGlobalHeight();
				float num5 = MBMath.GetClosestPointOnLineSegmentToPoint(in lineSegmentBegin, lineSegmentBegin + velocity, in point).DistanceSquared(point);
				if (num5 < num4 * num4)
				{
					AddAlarmFactor(num * num / (num3 + num5) * dt, missiles.ShooterAgent);
				}
			}
		}
	}

	private void OnAddSoundAlarmFactor(Agent alarmCreatorAgent, in Vec3 soundPosition, float soundLevelSquareRoot)
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		float num = 0.7f;
		float num2 = 20f;
		float num3 = TaleWorlds.Library.MathF.Sqrt(soundLevelSquareRoot * soundLevelSquareRoot / num - num2);
		if (base.OwnerAgent.IsActive() && !base.OwnerAgent.IsAlarmed() && base.OwnerAgent.IsAIControlled && base.OwnerAgent.GetAgentFlags().HasAnyFlag(AgentFlag.CanGetAlarmed) && base.OwnerAgent.RiderAgent == null && base.OwnerAgent != alarmCreatorAgent)
		{
			Vec3 position = base.OwnerAgent.Position;
			position.z += base.OwnerAgent.GetEyeGlobalHeight();
			float num4 = soundPosition.DistanceSquared(position);
			if (num4 < num3 * num3)
			{
				AddAlarmFactor(soundLevelSquareRoot * soundLevelSquareRoot / (num2 + num4), new WorldPosition(base.Mission.Scene, soundPosition));
			}
		}
	}

	public override void OnAgentRemoved(Agent agent)
	{
		if (agent == base.OwnerAgent)
		{
			base.Mission.OnAddSoundAlarmFactorToAgents -= OnAddSoundAlarmFactor;
		}
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		if (base.OwnerAgent.IsActive())
		{
			EquipmentIndex offhandWieldedItemIndex = base.OwnerAgent.GetOffhandWieldedItemIndex();
			if (offhandWieldedItemIndex != EquipmentIndex.None && offhandWieldedItemIndex != EquipmentIndex.ExtraWeaponSlot)
			{
				base.Mission.AddTickAction(Mission.MissionTickAction.TryToSheathWeaponInHand, base.OwnerAgent, 1, 0);
			}
			base.Mission.AddTickAction(Mission.MissionTickAction.TryToSheathWeaponInHand, base.OwnerAgent, 0, 3);
			base.OwnerAgent.SetWatchState(Agent.WatchState.Patrolling);
			base.OwnerAgent.ResetLookAgent();
			base.OwnerAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
			base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
		}
	}

	public override void ForceThink(float inSeconds)
	{
	}

	private bool IsAgentCoveredByAStealthBox(Agent agent)
	{
		ItemObject item = agent.WieldedOffhandWeapon.Item;
		if (item != null && item.ItemFlags.HasAnyFlag(ItemFlags.HasToBeHeldUp))
		{
			return false;
		}
		foreach (StealthBox stealthBox in _stealthBoxes)
		{
			if (stealthBox.IsAgentInside(agent) && (stealthBox.CoversStandingAgents || agent.CrouchMode || !agent.IsActive()))
			{
				return true;
			}
		}
		return false;
	}

	public override void ConversationTick()
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				behavior.ConversationTick();
			}
		}
	}
}
