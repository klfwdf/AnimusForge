using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x0200009E RID: 158
	public class AlarmedBehaviorGroup : AgentBehaviorGroup
	{
		// Token: 0x17000093 RID: 147
		// (get) Token: 0x0600069F RID: 1695 RVA: 0x0002C961 File Offset: 0x0002AB61
		// (set) Token: 0x060006A0 RID: 1696 RVA: 0x0002C969 File Offset: 0x0002AB69
		public float AlarmFactor { get; private set; }

		// Token: 0x060006A1 RID: 1697 RVA: 0x0002C974 File Offset: 0x0002AB74
		public AlarmedBehaviorGroup(AgentNavigator navigator, Mission mission)
			: base(navigator, mission)
		{
			this._alarmedTimer = new BasicMissionTimer();
			this._checkCalmDownTimer = new BasicMissionTimer();
			this._missionFightHandler = base.Mission.GetMissionBehavior<MissionFightHandler>();
			this._lastSuspiciousPositionTimer = new MissionTimer(10f);
			this._alarmYellTimer = new MissionTimer(10f);
			this._ignoredAgentsForAlarm = new List<Agent>(0);
			this._lastAlarmTriggerTime = MissionTime.Zero;
			base.Mission.OnAddSoundAlarmFactorToAgents += new Mission.OnAddSoundAlarmFactorToAgentsDelegate(this.OnAddSoundAlarmFactor);
			List<GameEntity> collection = new List<GameEntity>();
			base.OwnerAgent.Mission.Scene.GetAllEntitiesWithScriptComponent<StealthIndoorLightingArea>(ref collection);
			this._stealthIndoorLightingAreas = new MBList<GameEntity>(collection);
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x0002CA2E File Offset: 0x0002AC2E
		public void SetCanMoveWhenCautious(bool value)
		{
			this._canMoveWhenCautious = value;
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x0002CA38 File Offset: 0x0002AC38
		private void UpdateAgentAlarmState(float dt)
		{
			if (!base.OwnerAgent.IsAlarmed())
			{
				bool flag = base.OwnerAgent.IsAIAtMoveDestination();
				if ((!base.OwnerAgent.IsCautious() || flag) && this._lastAlarmTriggerTime.ElapsedSeconds > 2f)
				{
					float alarmFactor = this.AlarmFactor;
					this.AlarmFactor = Math.Max(0f, this.AlarmFactor - (base.OwnerAgent.IsPatrollingCautious() ? 0.1f : (this._canMoveWhenCautious ? 0.15f : 0.25f)) * dt);
					if (alarmFactor >= 1f && this.AlarmFactor < 1f)
					{
						this.AlarmFactor = 0.3f;
					}
				}
				bool flag2 = false;
				bool flag3 = false;
				if (!this.DoNotCheckForAlarmFactorIncrease)
				{
					Vec3 vec = ((base.OwnerAgent.IsHuman && base.OwnerAgent.AgentVisuals.IsValid()) ? base.OwnerAgent.Frame.rotation.TransformToParent(base.OwnerAgent.AgentVisuals.GetBoneEntitialFrame(base.OwnerAgent.Monster.HeadLookDirectionBoneIndex, true).rotation.f) : base.OwnerAgent.LookDirection);
					Vec3 vec2 = Vec3.CrossProduct(Vec3.Up, vec);
					vec = vec.RotateAboutAnArbitraryVector(vec2.NormalizedCopy(), 0.2f);
					foreach (Agent agent in base.OwnerAgent.Mission.AllAgents)
					{
						float num = 0f;
						float num2 = 0f;
						AgentState state = agent.State;
						bool flag4 = agent.AgentVisuals.IsValid();
						if (state != AgentState.Deleted && state != AgentState.Routed && state != AgentState.None && flag4)
						{
							AgentFlag agentFlags = agent.GetAgentFlags();
							bool flag5 = this._ignoredAgentsForAlarm.IndexOf(agent) >= 0;
							if (agent != base.OwnerAgent && agentFlags.HasAllFlags(AgentFlag.CanAttack | AgentFlag.IsHumanoid) && ((!agent.IsActive() && !flag5) || (agent.IsActive() && (agent.IsAlarmed() || (agent.IsPatrollingCautious() && !flag5 && agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AlarmFactor > this.AlarmFactor + 0.1f) || base.OwnerAgent.IsEnemyOf(agent)))))
							{
								if (!this.DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy)
								{
									int effectiveSkill = MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, DefaultSkills.Roguery);
									float equipmentStealthBonus = MissionGameModels.Current.AgentStatCalculateModel.GetEquipmentStealthBonus(agent);
									float sneakingNoiseMultiplier = Math.Max(0f, 1f - ((float)effectiveSkill * 0.0001f + equipmentStealthBonus * 0.002f));
									num += this.GetSoundFactor(agent, sneakingNoiseMultiplier);
								}
								num2 += this.GetVisualFactor(vec, agent, this._stealthIndoorLightingAreas, ref flag3, ref flag2);
								float num3 = num + num2;
								if (num3 > 0f && (!flag2 || !this.DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy))
								{
									this.AlarmFactor += num3 * dt * Campaign.Current.Models.DifficultyModel.GetStealthDifficultyMultiplier();
									this._lastAlarmTriggerTime = MissionTime.Now;
								}
								if (this.AlarmFactor >= 1f && base.OwnerAgent.IsAlarmStateNormal())
								{
									base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Cautious);
									WorldPosition worldPosition = agent.GetWorldPosition();
									Vec2 asVec = worldPosition.AsVec2;
									vec2 = base.OwnerAgent.Position;
									worldPosition.SetVec2(asVec + (vec2.AsVec2 - worldPosition.AsVec2).Normalized() * 2f);
									this.SetAILastSuspiciousPositionHelper(worldPosition, true);
									this._lastSuspiciousPositionTimer.Reset();
								}
								else if (num3 > 0f && (base.OwnerAgent.IsCautious() || base.OwnerAgent.IsPatrollingCautious()) && this._lastSuspiciousPositionTimer.Check(true))
								{
									WorldPosition worldPosition2 = agent.GetWorldPosition();
									Vec2 asVec2 = worldPosition2.AsVec2;
									vec2 = base.OwnerAgent.Position;
									worldPosition2.SetVec2(asVec2 + (vec2.AsVec2 - worldPosition2.AsVec2).Normalized() * 2f);
									this.SetAILastSuspiciousPositionHelper(worldPosition2, true);
								}
								if (num2 > 0f && base.OwnerAgent.IsPatrollingCautious() && (!agent.IsActive() || (!agent.IsEnemyOf(base.OwnerAgent) && !agent.IsAlarmed())))
								{
									this._ignoredAgentsForAlarm.Add(agent);
								}
							}
						}
					}
				}
				if (this.AlarmFactor >= 2f && flag2)
				{
					base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
					this._alarmYellTimer.Set(-3f);
				}
				else if (this._canMoveWhenCautious && this.AlarmFactor >= 2f && base.OwnerAgent.IsCautious() && flag3)
				{
					base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.PatrollingCautious);
				}
				else if (this.AlarmFactor < 0.0001f)
				{
					base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.None);
				}
				for (int i = this._ignoredAgentsForAlarm.Count - 1; i >= 0; i--)
				{
					Agent agent2 = this._ignoredAgentsForAlarm[i];
					if (agent2.IsActive() && (agent2.IsAlarmStateNormal() || agent2.IsAlarmed()))
					{
						this._ignoredAgentsForAlarm.RemoveAt(i);
					}
				}
				this.AlarmFactor = Math.Min(this.AlarmFactor, 2f);
				return;
			}
			if (this._alarmYellTimer.Check(true))
			{
				base.OwnerAgent.MakeVoice(SkinVoiceManager.VoiceType.Yell, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				Mission mission = base.OwnerAgent.Mission;
				Agent ownerAgent = base.OwnerAgent;
				Vec3 vec2 = base.OwnerAgent.Position + new Vec3(0f, 0f, base.OwnerAgent.GetEyeGlobalHeight(), -1f);
				mission.AddSoundAlarmFactorToAgents(ownerAgent, vec2, 10f);
			}
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x0002D058 File Offset: 0x0002B258
		private void SetAILastSuspiciousPositionHelper(in WorldPosition lastSuspiciousPosition, bool checkNavMeshForCorrection)
		{
			if (this._canMoveWhenCautious)
			{
				base.OwnerAgent.SetAILastSuspiciousPosition(lastSuspiciousPosition, checkNavMeshForCorrection);
				return;
			}
			WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
			Vec2 asVec = worldPosition.AsVec2;
			WorldPosition worldPosition2 = lastSuspiciousPosition;
			worldPosition.SetVec2(asVec + (worldPosition2.AsVec2 - base.OwnerAgent.Position.AsVec2).Normalized() * 0.1f);
			base.OwnerAgent.SetAILastSuspiciousPosition(worldPosition, false);
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x0002D0E4 File Offset: 0x0002B2E4
		private float GetSoundFactor(Agent currentAgent, float sneakingNoiseMultiplier)
		{
			if (currentAgent.Velocity.LengthSquared > 0.010000001f)
			{
				float num = (currentAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight(), -1f) - (base.OwnerAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight(), -1f))).Normalize();
				float num2 = 200f * Math.Min(1f, currentAgent.AverageVelocity.Length / currentAgent.GetMaximumForwardUnlimitedSpeed());
				bool flag = false;
				if (currentAgent.Mission.Scene.GetWaterLevelAtPosition(currentAgent.Position.AsVec2, !GameNetwork.IsMultiplayer, true) > currentAgent.Position.z)
				{
					BodyFlags bodyFlags;
					currentAgent.Mission.Scene.GetGroundHeightAndBodyFlagsAtPosition(currentAgent.Position, out bodyFlags, BodyFlags.CommonCollisionExcludeFlagsForAgent);
					if ((bodyFlags & (BodyFlags.Moveable | BodyFlags.Sinking)) != BodyFlags.Moveable)
					{
						flag = true;
						num2 *= 4f;
					}
				}
				if (currentAgent.HasMount)
				{
					num2 *= 2f;
				}
				else if (currentAgent.State == AgentState.Active && currentAgent.AgentVisuals.IsValid())
				{
					switch (currentAgent.AgentVisuals.GetMovementMode())
					{
					case HumanWalkingMovementMode.Walking:
						num2 *= 0.7f;
						break;
					case HumanWalkingMovementMode.CrouchRunning:
						num2 *= (flag ? 0.6f : 0.1f);
						break;
					case HumanWalkingMovementMode.CrouchWalking:
						num2 *= (flag ? 0.2f : 0f);
						break;
					}
				}
				num2 *= sneakingNoiseMultiplier;
				num2 /= 20f + num * num * 2.5f;
				if (num2 > 0.25f)
				{
					return num2;
				}
			}
			return 0f;
		}

		// Token: 0x060006A6 RID: 1702 RVA: 0x0002D2A0 File Offset: 0x0002B4A0
		public float GetVisualFactor(Vec3 usedGlobalLookDirection, Agent currentAgent, MBReadOnlyList<GameEntity> stealthIndoorLightingAreas, ref bool hasVisualOnCorpse, ref bool hasVisualOnEnemy)
		{
			Vec3 vec = currentAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight(), -1f) - (base.OwnerAgent.Position + new Vec3(0f, 0f, currentAgent.GetEyeGlobalHeight(), -1f));
			float num = 0f;
			if (Vec3.DotProduct(vec, usedGlobalLookDirection) > 0f)
			{
				float distance = vec.Normalize();
				bool currentAgentHasSpeed = currentAgent.Velocity.LengthSquared > 0.010000001f;
				float equipmentStealthBonus = MissionGameModels.Current.AgentStatCalculateModel.GetEquipmentStealthBonus(currentAgent);
				float num2 = this.GetVisualStrength(vec, usedGlobalLookDirection, currentAgent, currentAgentHasSpeed, distance, equipmentStealthBonus);
				if (num2 > 0.1f)
				{
					bool isDayTime = base.OwnerAgent.Mission.Scene.IsDayTime;
					Vec3 position = currentAgent.Position;
					float ambientLightStrength = (isDayTime ? 0.7f : 0.2f);
					float sunMoonLightStrength = (isDayTime ? 1f : 0.3f);
					foreach (GameEntity gameEntity in stealthIndoorLightingAreas)
					{
						StealthIndoorLightingArea firstScriptOfType = gameEntity.GetFirstScriptOfType<StealthIndoorLightingArea>();
						if (firstScriptOfType.IsPointIn(position))
						{
							ambientLightStrength = firstScriptOfType.AmbientLightStrength;
							sunMoonLightStrength = firstScriptOfType.SunMoonLightStrength;
							break;
						}
					}
					float visualStrengthOfAgentVisual = base.OwnerAgent.AgentVisuals.GetVisualStrengthOfAgentVisual(currentAgent.AgentVisuals, base.OwnerAgent.Mission, ambientLightStrength, sunMoonLightStrength, base.OwnerAgent.Index);
					num2 *= visualStrengthOfAgentVisual;
					if (num2 > 0.35f)
					{
						num += num2;
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

		// Token: 0x060006A7 RID: 1703 RVA: 0x0002D490 File Offset: 0x0002B690
		private float GetVisualStrength(Vec3 positionDifferenceDirection, Vec3 usedGlobalLookDirection, Agent currentAgent, bool currentAgentHasSpeed, float distance, float equipmentStealthBonus)
		{
			float num = 1.0995574f;
			float num2 = 0.7853982f;
			Vec3 vec = usedGlobalLookDirection.CrossProductWithUp();
			vec = vec.NormalizedCopy();
			Mat3 mat = new Mat3(ref vec, ref usedGlobalLookDirection, ref Vec3.Up);
			mat.u = Vec3.CrossProduct(mat.s, mat.f);
			Vec3 vec2 = mat.TransformToLocal(positionDifferenceDirection);
			float a = MathF.Atan2(vec2.z, vec2.x);
			float num3 = MathF.Acos(MBMath.ClampFloat(vec2.y, 0f, 1f));
			float num4;
			float num5;
			MathF.SinCos(a, out num4, out num5);
			float num6 = num * num2 / MathF.Sqrt(num * num * num4 * num4 + num2 * num2 * num5 * num5);
			float num7 = ((num3 >= num6) ? 0f : Math.Min(1f, 0.25f + (num6 - num3) / num6));
			float num8 = 2f;
			num7 *= num7;
			if (currentAgent.HasMount || distance <= currentAgent.CollisionCapsule.Radius * 5f)
			{
				num7 *= 6.5f;
			}
			else if (currentAgent.AgentVisuals.IsValid() && currentAgent.CrouchMode)
			{
				num7 *= 0.45f;
				num8 = 5f;
			}
			if (!currentAgentHasSpeed)
			{
				num7 *= 0.85f;
			}
			else if (currentAgent.State != AgentState.Active)
			{
				num7 *= 0.85f;
			}
			float num9 = Math.Max(0f, 1f - equipmentStealthBonus * 0.0025f);
			num7 *= 750f * num9;
			return num7 / (10f + distance * distance / num8);
		}

		// Token: 0x060006A8 RID: 1704 RVA: 0x0002D61A File Offset: 0x0002B81A
		public void ResetAlarmFactor()
		{
			this.AlarmFactor = 0f;
		}

		// Token: 0x060006A9 RID: 1705 RVA: 0x0002D628 File Offset: 0x0002B828
		private void AddAlarmFactor(float addedAlarmFactor, Agent suspiciousAgent)
		{
			this.AlarmFactor += addedAlarmFactor;
			this._lastAlarmTriggerTime = MissionTime.Now;
			if (this.AlarmFactor >= 1f && base.OwnerAgent.IsAlarmStateNormal())
			{
				base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Cautious);
				if (suspiciousAgent != null)
				{
					WorldPosition worldPosition = suspiciousAgent.GetWorldPosition();
					this.SetAILastSuspiciousPositionHelper(worldPosition, true);
				}
				else
				{
					WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
					this.SetAILastSuspiciousPositionHelper(worldPosition, false);
				}
				this._lastSuspiciousPositionTimer.Reset();
				return;
			}
			if ((base.OwnerAgent.IsCautious() || base.OwnerAgent.IsPatrollingCautious()) && this._lastSuspiciousPositionTimer.Check(true))
			{
				WorldPosition worldPosition;
				if (suspiciousAgent != null)
				{
					worldPosition = suspiciousAgent.GetWorldPosition();
					this.SetAILastSuspiciousPositionHelper(worldPosition, true);
					return;
				}
				worldPosition = base.OwnerAgent.GetWorldPosition();
				this.SetAILastSuspiciousPositionHelper(worldPosition, false);
			}
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x0002D6FC File Offset: 0x0002B8FC
		public void AddAlarmFactor(float addedAlarmFactor, in WorldPosition suspiciousPosition)
		{
			this.AlarmFactor += addedAlarmFactor;
			this._lastAlarmTriggerTime = MissionTime.Now;
			if (this.AlarmFactor >= 1f && base.OwnerAgent.IsAlarmStateNormal())
			{
				base.OwnerAgent.SetAlarmState(Agent.AIStateFlag.Cautious);
				this.SetAILastSuspiciousPositionHelper(suspiciousPosition, true);
				this._lastSuspiciousPositionTimer.Reset();
				return;
			}
			if ((base.OwnerAgent.IsCautious() || base.OwnerAgent.IsPatrollingCautious()) && this._lastSuspiciousPositionTimer.Check(true))
			{
				this.SetAILastSuspiciousPositionHelper(suspiciousPosition, true);
			}
		}

		// Token: 0x060006AB RID: 1707 RVA: 0x0002D790 File Offset: 0x0002B990
		public override void Tick(float dt, bool isSimulation)
		{
			if (base.Mission.AllowAiTicking && base.OwnerAgent.IsAIControlled)
			{
				this.HandleMissiles(dt);
				if (base.OwnerAgent.GetAgentFlags().HasAllFlags(AgentFlag.CanWieldWeapon | AgentFlag.CanGetAlarmed))
				{
					this.UpdateAgentAlarmState(dt);
				}
			}
			if (base.IsActive)
			{
				if (base.ScriptedBehavior != null)
				{
					if (!base.ScriptedBehavior.IsActive)
					{
						base.DisableAllBehaviors();
						base.ScriptedBehavior.IsActive = true;
					}
				}
				else
				{
					float num = 0f;
					int num2 = -1;
					for (int i = 0; i < this.Behaviors.Count; i++)
					{
						float availability = this.Behaviors[i].GetAvailability(isSimulation);
						if (availability > num)
						{
							num = availability;
							num2 = i;
						}
					}
					if (num > 0f && num2 != -1 && !this.Behaviors[num2].IsActive)
					{
						base.DisableAllBehaviors();
						this.Behaviors[num2].IsActive = true;
					}
				}
				this.TickActiveBehaviors(dt, isSimulation);
			}
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x0002D88C File Offset: 0x0002BA8C
		private void TickActiveBehaviors(float dt, bool isSimulation)
		{
			foreach (AgentBehavior agentBehavior in this.Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.Tick(dt, isSimulation);
				}
			}
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x0002D8E8 File Offset: 0x0002BAE8
		public override float GetScore(bool isSimulation)
		{
			if (base.OwnerAgent.IsAlarmed() || base.OwnerAgent.IsPatrollingCautious() || base.OwnerAgent.IsCautious())
			{
				if (!this.DisableCalmDown && this._alarmedTimer.ElapsedTime > 10f && this._checkCalmDownTimer.ElapsedTime > 1f)
				{
					this._checkCalmDownTimer.Reset();
					if (!this.IsNearDanger())
					{
						base.OwnerAgent.DisableScriptedMovement();
					}
				}
				return 1f;
			}
			if (this.IsNearDanger())
			{
				AlarmedBehaviorGroup.AlarmAgent(base.OwnerAgent);
				return 1f;
			}
			return 0f;
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x0002D98C File Offset: 0x0002BB8C
		private bool IsNearDanger()
		{
			float num;
			Agent closestAlarmSource = this.GetClosestAlarmSource(out num);
			return closestAlarmSource != null && (num < 225f || this.Navigator.CanSeeAgent(closestAlarmSource));
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x0002D9C0 File Offset: 0x0002BBC0
		public Agent GetClosestAlarmSource(out float distanceSquared)
		{
			distanceSquared = float.MaxValue;
			if (this._missionFightHandler == null || !this._missionFightHandler.IsThereActiveFight())
			{
				return null;
			}
			Agent result = null;
			foreach (Agent agent in this._missionFightHandler.GetDangerSources(base.OwnerAgent))
			{
				float num = agent.Position.DistanceSquared(base.OwnerAgent.Position);
				if (num < distanceSquared)
				{
					distanceSquared = num;
					result = agent;
				}
			}
			return result;
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x0002DA58 File Offset: 0x0002BC58
		public static void AlarmAgent(Agent agent)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}

		// Token: 0x060006B1 RID: 1713 RVA: 0x0002DA64 File Offset: 0x0002BC64
		protected override void OnActivate()
		{
			TextObject textObject = new TextObject("{=!}{p0} {p1} activate alarmed behavior group.", null);
			textObject.SetTextVariable("p0", base.OwnerAgent.Name);
			textObject.SetTextVariable("p1", base.OwnerAgent.Index);
			this._alarmedTimer.Reset();
			this._checkCalmDownTimer.Reset();
			base.OwnerAgent.DisableScriptedMovement();
			base.OwnerAgent.ClearTargetFrame();
			this.Navigator.SetItemsVisibility(false);
			if (CampaignMission.Current.Location != null)
			{
				LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.OwnerAgent.Origin);
				if (locationCharacter != null && locationCharacter.ActionSetCode != locationCharacter.AlarmedActionSetCode)
				{
					AnimationSystemData animationSystemData = locationCharacter.GetAgentBuildData().AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(locationCharacter.AlarmedActionSetCode), locationCharacter.Character.GetStepSize(), false);
					base.OwnerAgent.SetActionSet(ref animationSystemData);
				}
			}
			if (this.Navigator.MemberOfAlley != null || MissionFightHandler.IsAgentAggressive(base.OwnerAgent))
			{
				this.DisableCalmDown = true;
			}
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x0002DB74 File Offset: 0x0002BD74
		private void HandleMissiles(float dt)
		{
			foreach (Mission.Missile missile in base.Mission.MissilesList)
			{
				Vec3 position = missile.GetPosition();
				Vec3 velocity = missile.GetVelocity();
				float num = velocity.Length / 20f + 0.1f;
				float num2 = 0.1f;
				float num3 = 20f;
				float num4 = MathF.Sqrt(num * num / num2 - num3);
				if (!base.OwnerAgent.IsAlarmed() && base.OwnerAgent.IsActive() && base.OwnerAgent.IsAIControlled && base.OwnerAgent.GetAgentFlags().HasAnyFlag(AgentFlag.CanGetAlarmed) && base.OwnerAgent.RiderAgent == null && base.OwnerAgent != missile.ShooterAgent)
				{
					Vec3 position2 = base.OwnerAgent.Position;
					position2.z += base.OwnerAgent.GetEyeGlobalHeight();
					Vec3 vec = position + velocity;
					float num5 = MBMath.GetClosestPointOnLineSegmentToPoint(position, vec, position2).DistanceSquared(position2);
					if (num5 < num4 * num4)
					{
						this.AddAlarmFactor(num * num / (num3 + num5) * dt, missile.ShooterAgent);
					}
				}
			}
		}

		// Token: 0x060006B3 RID: 1715 RVA: 0x0002DCE8 File Offset: 0x0002BEE8
		private void OnAddSoundAlarmFactor(Agent alarmCreatorAgent, in Vec3 soundPosition, float soundLevelSquareRoot)
		{
			if (!GameNetwork.IsClientOrReplay)
			{
				float num = 0.7f;
				float num2 = 20f;
				float num3 = MathF.Sqrt(soundLevelSquareRoot * soundLevelSquareRoot / num - num2);
				if (base.OwnerAgent.IsActive() && !base.OwnerAgent.IsAlarmed() && base.OwnerAgent.IsAIControlled && base.OwnerAgent.GetAgentFlags().HasAnyFlag(AgentFlag.CanGetAlarmed) && base.OwnerAgent.RiderAgent == null && base.OwnerAgent != alarmCreatorAgent)
				{
					Vec3 position = base.OwnerAgent.Position;
					position.z += base.OwnerAgent.GetEyeGlobalHeight();
					Vec3 vec = soundPosition;
					float num4 = vec.DistanceSquared(position);
					if (num4 < num3 * num3)
					{
						float addedAlarmFactor = soundLevelSquareRoot * soundLevelSquareRoot / (num2 + num4);
						WorldPosition worldPosition = new WorldPosition(base.Mission.Scene, soundPosition);
						this.AddAlarmFactor(addedAlarmFactor, worldPosition);
					}
				}
			}
		}

		// Token: 0x060006B4 RID: 1716 RVA: 0x0002DDD8 File Offset: 0x0002BFD8
		public override void OnAgentRemoved(Agent agent)
		{
			if (agent == base.OwnerAgent)
			{
				base.Mission.OnAddSoundAlarmFactorToAgents -= new Mission.OnAddSoundAlarmFactorToAgentsDelegate(this.OnAddSoundAlarmFactor);
			}
		}

		// Token: 0x060006B5 RID: 1717 RVA: 0x0002DDFC File Offset: 0x0002BFFC
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
				base.OwnerAgent.SetActionChannel(0, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				base.OwnerAgent.SetActionChannel(1, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x0002DEDE File Offset: 0x0002C0DE
		public override void ForceThink(float inSeconds)
		{
		}

		// Token: 0x04000387 RID: 903
		public const float SafetyDistance = 15f;

		// Token: 0x04000388 RID: 904
		public const float SafetyDistanceSquared = 225f;

		// Token: 0x04000389 RID: 905
		private readonly MissionFightHandler _missionFightHandler;

		// Token: 0x0400038A RID: 906
		public bool DisableCalmDown;

		// Token: 0x0400038B RID: 907
		private readonly BasicMissionTimer _alarmedTimer;

		// Token: 0x0400038C RID: 908
		private readonly BasicMissionTimer _checkCalmDownTimer;

		// Token: 0x0400038D RID: 909
		public bool DoNotCheckForAlarmFactorIncrease;

		// Token: 0x0400038E RID: 910
		public bool DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy;

		// Token: 0x04000390 RID: 912
		private bool _canMoveWhenCautious = true;

		// Token: 0x04000391 RID: 913
		private readonly MissionTimer _lastSuspiciousPositionTimer;

		// Token: 0x04000392 RID: 914
		private readonly MissionTimer _alarmYellTimer;

		// Token: 0x04000393 RID: 915
		private readonly List<Agent> _ignoredAgentsForAlarm;

		// Token: 0x04000394 RID: 916
		private readonly MBList<GameEntity> _stealthIndoorLightingAreas;

		// Token: 0x04000395 RID: 917
		private MissionTime _lastAlarmTriggerTime;
	}
}
