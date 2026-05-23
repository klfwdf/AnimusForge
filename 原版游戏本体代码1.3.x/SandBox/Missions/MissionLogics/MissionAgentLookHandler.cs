using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000074 RID: 116
	public class MissionAgentLookHandler : MissionLogic
	{
		// Token: 0x060004AB RID: 1195 RVA: 0x0001D94C File Offset: 0x0001BB4C
		public MissionAgentLookHandler()
		{
			this._staticPointList = new List<MissionAgentLookHandler.PointOfInterest>();
			this._checklist = new List<MissionAgentLookHandler.LookInfo>();
			this._selectionDelegate = new MissionAgentLookHandler.SelectionDelegate(this.SelectRandomAccordingToScore);
		}

		// Token: 0x060004AC RID: 1196 RVA: 0x0001D97C File Offset: 0x0001BB7C
		public override void AfterStart()
		{
			this.AddStablePointsOfInterest();
		}

		// Token: 0x060004AD RID: 1197 RVA: 0x0001D984 File Offset: 0x0001BB84
		private void AddStablePointsOfInterest()
		{
			foreach (GameEntity gameEntity in base.Mission.Scene.FindEntitiesWithTag("point_of_interest"))
			{
				this._staticPointList.Add(new MissionAgentLookHandler.PointOfInterest(gameEntity.GetGlobalFrame()));
			}
		}

		// Token: 0x060004AE RID: 1198 RVA: 0x0001D9F0 File Offset: 0x0001BBF0
		private void DebugTick()
		{
		}

		// Token: 0x060004AF RID: 1199 RVA: 0x0001D9F4 File Offset: 0x0001BBF4
		public override void OnMissionTick(float dt)
		{
			if (Game.Current.IsDevelopmentMode)
			{
				this.DebugTick();
			}
			float currentTime = base.Mission.CurrentTime;
			foreach (MissionAgentLookHandler.LookInfo lookInfo in this._checklist)
			{
				if (lookInfo.Agent.IsActive() && !ConversationMission.ConversationAgents.Contains(lookInfo.Agent) && (!ConversationMission.ConversationAgents.Any<Agent>() || !lookInfo.Agent.IsPlayerControlled))
				{
					if (lookInfo.CheckTimer.Check(currentTime))
					{
						MissionAgentLookHandler.PointOfInterest pointOfInterest = this._selectionDelegate(lookInfo.Agent);
						if (pointOfInterest != null)
						{
							lookInfo.Reset(pointOfInterest, 5f);
						}
						else
						{
							lookInfo.Reset(null, 1f + MBRandom.RandomFloat);
						}
					}
					else if (lookInfo.PointOfInterest != null && (!lookInfo.PointOfInterest.IsActive || !lookInfo.PointOfInterest.IsVisibleFor(lookInfo.Agent)))
					{
						MissionAgentLookHandler.PointOfInterest pointOfInterest2 = this._selectionDelegate(lookInfo.Agent);
						if (pointOfInterest2 != null)
						{
							lookInfo.Reset(pointOfInterest2, 5f + MBRandom.RandomFloat);
						}
						else
						{
							lookInfo.Reset(null, MBRandom.RandomFloat * 5f + 5f);
						}
					}
					else if (lookInfo.PointOfInterest != null)
					{
						Vec3 targetPosition = lookInfo.PointOfInterest.GetTargetPosition();
						lookInfo.Agent.SetLookToPointOfInterest(targetPosition);
					}
				}
			}
		}

		// Token: 0x060004B0 RID: 1200 RVA: 0x0001DB8C File Offset: 0x0001BD8C
		private MissionAgentLookHandler.PointOfInterest SelectFirstNonAgent(Agent agent)
		{
			if (agent.IsAIControlled)
			{
				int num = MBRandom.RandomInt(this._staticPointList.Count);
				int num2 = num;
				MissionAgentLookHandler.PointOfInterest pointOfInterest;
				for (;;)
				{
					pointOfInterest = this._staticPointList[num2];
					if (pointOfInterest.GetScore(agent) > 0f)
					{
						break;
					}
					num2 = ((num2 + 1 == this._staticPointList.Count) ? 0 : (num2 + 1));
					if (num2 == num)
					{
						goto IL_53;
					}
				}
				return pointOfInterest;
			}
			IL_53:
			return null;
		}

		// Token: 0x060004B1 RID: 1201 RVA: 0x0001DBF0 File Offset: 0x0001BDF0
		private MissionAgentLookHandler.PointOfInterest SelectBestOfLimitedNonAgent(Agent agent)
		{
			int num = 3;
			MissionAgentLookHandler.PointOfInterest result = null;
			float num2 = -1f;
			if (agent.IsAIControlled)
			{
				int num3 = MBRandom.RandomInt(this._staticPointList.Count);
				int num4 = num3;
				do
				{
					MissionAgentLookHandler.PointOfInterest pointOfInterest = this._staticPointList[num4];
					float score = pointOfInterest.GetScore(agent);
					if (score > 0f)
					{
						if (score > num2)
						{
							num2 = score;
							result = pointOfInterest;
						}
						num--;
					}
					num4 = ((num4 + 1 == this._staticPointList.Count) ? 0 : (num4 + 1));
				}
				while (num4 != num3 && num > 0);
			}
			return result;
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x0001DC78 File Offset: 0x0001BE78
		private MissionAgentLookHandler.PointOfInterest SelectBest(Agent agent)
		{
			MissionAgentLookHandler.PointOfInterest result = null;
			float num = -1f;
			if (agent.IsAIControlled)
			{
				foreach (MissionAgentLookHandler.PointOfInterest pointOfInterest in this._staticPointList)
				{
					float score = pointOfInterest.GetScore(agent);
					if (score > 0f && score > num)
					{
						num = score;
						result = pointOfInterest;
					}
				}
				AgentProximityMap.ProximityMapSearchStruct proximityMapSearchStruct = AgentProximityMap.BeginSearch(base.Mission, agent.Position.AsVec2, 5f, false);
				while (proximityMapSearchStruct.LastFoundAgent != null)
				{
					MissionAgentLookHandler.PointOfInterest pointOfInterest2 = new MissionAgentLookHandler.PointOfInterest(proximityMapSearchStruct.LastFoundAgent);
					float score2 = pointOfInterest2.GetScore(agent);
					if (score2 > 0f && score2 > num)
					{
						num = score2;
						result = pointOfInterest2;
					}
					AgentProximityMap.FindNext(base.Mission, ref proximityMapSearchStruct);
				}
			}
			return result;
		}

		// Token: 0x060004B3 RID: 1203 RVA: 0x0001DD5C File Offset: 0x0001BF5C
		private MissionAgentLookHandler.PointOfInterest SelectRandomAccordingToScore(Agent agent)
		{
			float num = 0f;
			List<KeyValuePair<float, MissionAgentLookHandler.PointOfInterest>> list = new List<KeyValuePair<float, MissionAgentLookHandler.PointOfInterest>>();
			if (agent.IsAIControlled)
			{
				foreach (MissionAgentLookHandler.PointOfInterest pointOfInterest in this._staticPointList)
				{
					float score = pointOfInterest.GetScore(agent);
					if (score > 0f)
					{
						list.Add(new KeyValuePair<float, MissionAgentLookHandler.PointOfInterest>(score, pointOfInterest));
						num += score;
					}
				}
				AgentProximityMap.ProximityMapSearchStruct proximityMapSearchStruct = AgentProximityMap.BeginSearch(Mission.Current, agent.Position.AsVec2, 5f, false);
				while (proximityMapSearchStruct.LastFoundAgent != null)
				{
					MissionAgentLookHandler.PointOfInterest pointOfInterest2 = new MissionAgentLookHandler.PointOfInterest(proximityMapSearchStruct.LastFoundAgent);
					float score2 = pointOfInterest2.GetScore(agent);
					if (score2 > 0f)
					{
						list.Add(new KeyValuePair<float, MissionAgentLookHandler.PointOfInterest>(score2, pointOfInterest2));
						num += score2;
					}
					AgentProximityMap.FindNext(Mission.Current, ref proximityMapSearchStruct);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			float num2 = MBRandom.RandomFloat * num;
			MissionAgentLookHandler.PointOfInterest value = list[list.Count - 1].Value;
			foreach (KeyValuePair<float, MissionAgentLookHandler.PointOfInterest> keyValuePair in list)
			{
				num2 -= keyValuePair.Key;
				if (num2 <= 0f)
				{
					value = keyValuePair.Value;
					break;
				}
			}
			return value;
		}

		// Token: 0x060004B4 RID: 1204 RVA: 0x0001DED4 File Offset: 0x0001C0D4
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (agent.IsHuman)
			{
				this._checklist.Add(new MissionAgentLookHandler.LookInfo(agent, MBRandom.RandomFloat));
			}
		}

		// Token: 0x060004B5 RID: 1205 RVA: 0x0001DEF4 File Offset: 0x0001C0F4
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			for (int i = 0; i < this._checklist.Count; i++)
			{
				MissionAgentLookHandler.LookInfo lookInfo = this._checklist[i];
				if (lookInfo.Agent == affectedAgent)
				{
					this._checklist.RemoveAt(i);
					i--;
				}
				else if (lookInfo.PointOfInterest != null && lookInfo.PointOfInterest.IsRelevant(affectedAgent))
				{
					lookInfo.Reset(null, MBRandom.RandomFloat * 2f + 2f);
				}
			}
		}

		// Token: 0x04000277 RID: 631
		private readonly List<MissionAgentLookHandler.PointOfInterest> _staticPointList;

		// Token: 0x04000278 RID: 632
		private readonly List<MissionAgentLookHandler.LookInfo> _checklist;

		// Token: 0x04000279 RID: 633
		private MissionAgentLookHandler.SelectionDelegate _selectionDelegate;

		// Token: 0x02000167 RID: 359
		private class PointOfInterest
		{
			// Token: 0x17000125 RID: 293
			// (get) Token: 0x06000E34 RID: 3636 RVA: 0x000644ED File Offset: 0x000626ED
			public bool IsActive
			{
				get
				{
					return this._agent == null || this._agent.IsActive();
				}
			}

			// Token: 0x06000E35 RID: 3637 RVA: 0x00064504 File Offset: 0x00062704
			public PointOfInterest(Agent agent)
			{
				this._agent = agent;
				this._selectDistance = 5;
				this._releaseDistanceSquare = 36;
				this._ignoreDirection = false;
				CharacterObject characterObject = (CharacterObject)agent.Character;
				if (!agent.IsHuman)
				{
					this._priority = 1;
					return;
				}
				if (characterObject.IsHero)
				{
					this._priority = 5;
					return;
				}
				if (characterObject.Occupation == Occupation.HorseTrader || characterObject.Occupation == Occupation.Weaponsmith || characterObject.Occupation == Occupation.GoodsTrader || characterObject.Occupation == Occupation.Armorer || characterObject.Occupation == Occupation.Blacksmith)
				{
					this._priority = 3;
					return;
				}
				this._priority = 1;
			}

			// Token: 0x06000E36 RID: 3638 RVA: 0x000645A0 File Offset: 0x000627A0
			public PointOfInterest(MatrixFrame frame)
			{
				this._frame = frame;
				this._selectDistance = 4;
				this._releaseDistanceSquare = 25;
				this._ignoreDirection = true;
				this._priority = 2;
			}

			// Token: 0x06000E37 RID: 3639 RVA: 0x000645CC File Offset: 0x000627CC
			public float GetScore(Agent agent)
			{
				if (agent == this._agent || this.GetBasicPosition().DistanceSquared(agent.Position) > (float)(this._selectDistance * this._selectDistance))
				{
					return -1f;
				}
				Vec3 vec = this.GetTargetPosition() - agent.GetEyeGlobalPosition();
				float num = vec.Normalize();
				if (Vec2.DotProduct(vec.AsVec2, agent.GetMovementDirection()) < 0.7f)
				{
					return -1f;
				}
				float num2 = (float)(this._priority * this._selectDistance) / num;
				if (this.IsMoving())
				{
					num2 *= 5f;
				}
				if (!this._ignoreDirection)
				{
					MatrixFrame matrixFrame = this.GetTargetFrame();
					Vec2 asVec = matrixFrame.rotation.f.AsVec2;
					matrixFrame = agent.Frame;
					float num3 = Vec2.DotProduct(asVec, matrixFrame.rotation.f.AsVec2);
					if (num3 < -0.7f)
					{
						num2 *= 2f;
					}
					else if (MathF.Abs(num3) < 0.1f)
					{
						num2 *= 2f;
					}
				}
				return num2;
			}

			// Token: 0x06000E38 RID: 3640 RVA: 0x000646D1 File Offset: 0x000628D1
			public Vec3 GetTargetPosition()
			{
				Agent agent = this._agent;
				if (agent == null)
				{
					return this._frame.origin;
				}
				return agent.GetEyeGlobalPosition();
			}

			// Token: 0x06000E39 RID: 3641 RVA: 0x000646EE File Offset: 0x000628EE
			public Vec3 GetBasicPosition()
			{
				if (this._agent == null)
				{
					return this._frame.origin;
				}
				return this._agent.Position;
			}

			// Token: 0x06000E3A RID: 3642 RVA: 0x00064710 File Offset: 0x00062910
			private bool IsMoving()
			{
				return this._agent == null || this._agent.GetCurrentVelocity().LengthSquared > 0.040000003f;
			}

			// Token: 0x06000E3B RID: 3643 RVA: 0x00064741 File Offset: 0x00062941
			private MatrixFrame GetTargetFrame()
			{
				if (this._agent == null)
				{
					return this._frame;
				}
				return this._agent.Frame;
			}

			// Token: 0x06000E3C RID: 3644 RVA: 0x00064760 File Offset: 0x00062960
			public bool IsVisibleFor(Agent agent)
			{
				Vec3 basicPosition = this.GetBasicPosition();
				Vec3 position = agent.Position;
				if (agent == this._agent || position.DistanceSquared(basicPosition) > (float)this._releaseDistanceSquare)
				{
					return false;
				}
				Vec3 vec = basicPosition - position;
				vec.Normalize();
				return Vec2.DotProduct(vec.AsVec2, agent.GetMovementDirection()) > 0.4f;
			}

			// Token: 0x06000E3D RID: 3645 RVA: 0x000647C0 File Offset: 0x000629C0
			public bool IsRelevant(Agent agent)
			{
				return agent == this._agent;
			}

			// Token: 0x040006EE RID: 1774
			public const int MaxSelectDistanceForAgent = 5;

			// Token: 0x040006EF RID: 1775
			public const int MaxSelectDistanceForFrame = 4;

			// Token: 0x040006F0 RID: 1776
			private readonly int _selectDistance;

			// Token: 0x040006F1 RID: 1777
			private readonly int _releaseDistanceSquare;

			// Token: 0x040006F2 RID: 1778
			private readonly Agent _agent;

			// Token: 0x040006F3 RID: 1779
			private readonly MatrixFrame _frame;

			// Token: 0x040006F4 RID: 1780
			private readonly bool _ignoreDirection;

			// Token: 0x040006F5 RID: 1781
			private readonly int _priority;
		}

		// Token: 0x02000168 RID: 360
		private class LookInfo
		{
			// Token: 0x06000E3E RID: 3646 RVA: 0x000647CB File Offset: 0x000629CB
			public LookInfo(Agent agent, float checkTime)
			{
				this.Agent = agent;
				this.CheckTimer = new Timer(Mission.Current.CurrentTime, checkTime, true);
			}

			// Token: 0x06000E3F RID: 3647 RVA: 0x000647F4 File Offset: 0x000629F4
			public void Reset(MissionAgentLookHandler.PointOfInterest pointOfInterest, float duration)
			{
				if (this.PointOfInterest != pointOfInterest)
				{
					this.PointOfInterest = pointOfInterest;
					if (this.PointOfInterest != null)
					{
						this.Agent.SetLookToPointOfInterest(this.PointOfInterest.GetTargetPosition());
					}
					else if (this.Agent.IsActive())
					{
						this.Agent.DisableLookToPointOfInterest();
					}
				}
				this.CheckTimer.Reset(Mission.Current.CurrentTime, duration);
			}

			// Token: 0x040006F6 RID: 1782
			public readonly Agent Agent;

			// Token: 0x040006F7 RID: 1783
			public MissionAgentLookHandler.PointOfInterest PointOfInterest;

			// Token: 0x040006F8 RID: 1784
			public readonly Timer CheckTimer;
		}

		// Token: 0x02000169 RID: 361
		// (Invoke) Token: 0x06000E41 RID: 3649
		private delegate MissionAgentLookHandler.PointOfInterest SelectionDelegate(Agent agent);
	}
}
