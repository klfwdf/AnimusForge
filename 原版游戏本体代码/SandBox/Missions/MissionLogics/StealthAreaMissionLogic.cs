using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000087 RID: 135
	public class StealthAreaMissionLogic : MissionLogic
	{
		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000538 RID: 1336 RVA: 0x00022E4C File Offset: 0x0002104C
		public MBReadOnlyList<Agent> AllyTroops
		{
			get
			{
				return this._allyTroops;
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000539 RID: 1337 RVA: 0x00022E54 File Offset: 0x00021054
		// (set) Token: 0x0600053A RID: 1338 RVA: 0x00022E5C File Offset: 0x0002105C
		public bool AllReinforcementsCalled { get; private set; }

		// Token: 0x0600053B RID: 1339 RVA: 0x00022E65 File Offset: 0x00021065
		public StealthAreaMissionLogic()
		{
			this.SetAgentSpawnTypes();
		}

		// Token: 0x0600053C RID: 1340 RVA: 0x00022E94 File Offset: 0x00021094
		public bool IsSentry(Agent agent)
		{
			foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
			{
				foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
				{
					if (keyValuePair.Value.Contains(agent))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600053D RID: 1341 RVA: 0x00022F30 File Offset: 0x00021130
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			foreach (StealthAreaUsePoint stealthAreaUsePoint in base.Mission.MissionObjects.FindAllWithType<StealthAreaUsePoint>())
			{
				this._stealthAreaData.Add(new StealthAreaMissionLogic.StealthAreaData(stealthAreaUsePoint));
			}
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x00022F98 File Offset: 0x00021198
		public void AddAgentSpawnType(string spawnGroupId, Dictionary<string, int> spawnDictionary)
		{
			this._agentSpawnTypes[spawnGroupId] = spawnDictionary;
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x00022FA8 File Offset: 0x000211A8
		private void SetAgentSpawnTypes()
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			dictionary.Add("deserter", 1);
			dictionary.Add("forest_bandits_bandit", 2);
			this._agentSpawnTypes.Add("reinforcement_ally_group_1", dictionary);
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			dictionary2.Add("aserai_footman", 3);
			dictionary2.Add("aserai_skirmisher", 2);
			this._agentSpawnTypes.Add("reinforcement_ally_group_cambush", dictionary2);
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00023014 File Offset: 0x00021214
		private List<IAgentOriginBase> GetReinforcementAllyGroupTroops(StealthAreaMissionLogic.StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker)
		{
			if (this.GetReinforcementAllyTroops == null)
			{
				string reinforcementAllyGroupId = stealthAreaMarker.ReinforcementAllyGroupId;
				List<IAgentOriginBase> list = new List<IAgentOriginBase>();
				Dictionary<string, int> dictionary;
				if (this._agentSpawnTypes.TryGetValue(reinforcementAllyGroupId, out dictionary))
				{
					foreach (KeyValuePair<string, int> keyValuePair in dictionary)
					{
						CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>(keyValuePair.Key);
						int value = keyValuePair.Value;
						for (int i = 0; i < value; i++)
						{
							list.Add(new PartyAgentOrigin(PartyBase.MainParty, @object, -1, default(UniqueTroopDescriptor), false, false));
						}
					}
				}
				return list;
			}
			return this.GetReinforcementAllyTroops(triggeredStealthAreaData, stealthAreaMarker);
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x000230E0 File Offset: 0x000212E0
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			base.OnAgentBuild(agent, banner);
			this.CheckStealthAreaMarkerForAgent(agent);
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x000230F1 File Offset: 0x000212F1
		public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
		{
			base.OnAgentTeamChanged(prevTeam, newTeam, agent);
			this.CheckStealthAreaMarkerForAgent(agent);
		}

		// Token: 0x06000543 RID: 1347 RVA: 0x00023104 File Offset: 0x00021304
		private void CheckStealthAreaMarkerForAgent(Agent agent)
		{
			if (agent.IsHuman && agent.Team == Mission.Current.PlayerEnemyTeam)
			{
				foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
				{
					foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
					{
						if (keyValuePair.Key.IsPositionInRange(agent.Position))
						{
							stealthAreaData.AddAgentToStealthAreaMarker(keyValuePair.Key, agent);
							break;
						}
					}
				}
			}
		}

		// Token: 0x06000544 RID: 1348 RVA: 0x000231D0 File Offset: 0x000213D0
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (affectorAgent != null && affectorAgent.IsMainAgent)
			{
				foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData in this._stealthAreaData)
				{
					foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
					{
						if (keyValuePair.Value.Contains(affectedAgent))
						{
							stealthAreaData.RemoveAgentFromStealthAreaMarker(keyValuePair.Key, affectedAgent);
						}
					}
				}
			}
		}

		// Token: 0x06000545 RID: 1349 RVA: 0x00023284 File Offset: 0x00021484
		public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
		{
			if (usedObject is StealthAreaUsePoint)
			{
				StealthAreaMissionLogic.StealthAreaData stealthAreaData = null;
				foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaData2 in this._stealthAreaData)
				{
					if (stealthAreaData2.StealthAreaUsePoint == usedObject)
					{
						stealthAreaData = stealthAreaData2;
						break;
					}
				}
				if (stealthAreaData != null)
				{
					stealthAreaData.IsReinforcementCalled = true;
					foreach (KeyValuePair<StealthAreaMarker, List<Agent>> keyValuePair in stealthAreaData.StealthAreaMarkers)
					{
						List<IAgentOriginBase> reinforcementAllyGroupTroops = this.GetReinforcementAllyGroupTroops(stealthAreaData, keyValuePair.Key);
						if (!reinforcementAllyGroupTroops.IsEmpty<IAgentOriginBase>())
						{
							using (List<IAgentOriginBase>.Enumerator enumerator3 = reinforcementAllyGroupTroops.GetEnumerator())
							{
								while (enumerator3.MoveNext())
								{
									IAgentOriginBase character = enumerator3.Current;
									this.SpawnAllyAgent(character, keyValuePair.Key.ReinforcementAllyGroupSpawnPoint, keyValuePair.Key.WaitPoint.GlobalPosition);
								}
								continue;
							}
						}
						Debug.FailedAssert("There is not any troops to spawn as stealth area reinforcement.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\StealthAreaMissionLogic.cs", "OnObjectUsed", 269);
					}
				}
			}
			this.AllReinforcementsCalled = this._stealthAreaData.All((StealthAreaMissionLogic.StealthAreaData x) => x.IsReinforcementCalled);
		}

		// Token: 0x06000546 RID: 1350 RVA: 0x000233FC File Offset: 0x000215FC
		private void SpawnAllyAgent(IAgentOriginBase character, GameEntity spawnPoint, Vec3 position)
		{
			MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
			Agent agent = Mission.Current.SpawnTroop(character, true, false, false, false, 0, 0, true, true, true, new Vec3?(globalFrame.origin), new Vec2?(globalFrame.rotation.f.AsVec2.Normalized()), null, null, FormationClass.NumberOfAllFormations, false);
			Vec3 randomPositionAroundPoint = Mission.Current.GetRandomPositionAroundPoint(position, 0f, 2f, true);
			WorldPosition worldPosition = new WorldPosition(spawnPoint.Scene, randomPositionAroundPoint);
			agent.SetScriptedPosition(ref worldPosition, true, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.Crouch);
			this._allyTroops.Add(agent);
		}

		// Token: 0x06000547 RID: 1351 RVA: 0x00023493 File Offset: 0x00021693
		public bool CheckIfAllStealthAreasAreTriggered()
		{
			return this._stealthAreaData.All((StealthAreaMissionLogic.StealthAreaData x) => x.IsStealthAreaTriggered);
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x000234BF File Offset: 0x000216BF
		public bool CheckIfAllStealthAreasReinforcementsAreCalled()
		{
			return this.AllReinforcementsCalled;
		}

		// Token: 0x040002C5 RID: 709
		private readonly MBList<StealthAreaMissionLogic.StealthAreaData> _stealthAreaData = new MBList<StealthAreaMissionLogic.StealthAreaData>();

		// Token: 0x040002C6 RID: 710
		private readonly Dictionary<string, Dictionary<string, int>> _agentSpawnTypes = new Dictionary<string, Dictionary<string, int>>();

		// Token: 0x040002C7 RID: 711
		private readonly MBList<Agent> _allyTroops = new MBList<Agent>();

		// Token: 0x040002C8 RID: 712
		public StealthAreaMissionLogic.GetReinforcementAllyTroopsDelegate GetReinforcementAllyTroops;

		// Token: 0x02000183 RID: 387
		// (Invoke) Token: 0x06000E9E RID: 3742
		public delegate List<IAgentOriginBase> GetReinforcementAllyTroopsDelegate(StealthAreaMissionLogic.StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker);

		// Token: 0x02000184 RID: 388
		public class StealthAreaData
		{
			// Token: 0x06000EA1 RID: 3745 RVA: 0x00065B74 File Offset: 0x00063D74
			internal StealthAreaData(StealthAreaUsePoint stealthAreaUsePoint)
			{
				this.StealthAreaUsePoint = stealthAreaUsePoint;
				this.StealthAreaMarkers = new Dictionary<StealthAreaMarker, List<Agent>>();
				foreach (WeakGameEntity weakGameEntity in stealthAreaUsePoint.GameEntity.GetChildren())
				{
					if (weakGameEntity.HasScriptOfType<StealthAreaMarker>())
					{
						this.StealthAreaMarkers.Add(weakGameEntity.GetFirstScriptOfType<StealthAreaMarker>(), new List<Agent>());
					}
				}
			}

			// Token: 0x06000EA2 RID: 3746 RVA: 0x00065BFC File Offset: 0x00063DFC
			internal void AddAgentToStealthAreaMarker(StealthAreaMarker stealthAreaMarker, Agent agent)
			{
				this.StealthAreaMarkers[stealthAreaMarker].Add(agent);
			}

			// Token: 0x06000EA3 RID: 3747 RVA: 0x00065C10 File Offset: 0x00063E10
			internal void RemoveAgentFromStealthAreaMarker(StealthAreaMarker stealthAreaMarker, Agent agent)
			{
				this.StealthAreaMarkers[stealthAreaMarker].Remove(agent);
				if (this.StealthAreaMarkers.All((KeyValuePair<StealthAreaMarker, List<Agent>> x) => x.Value.IsEmpty<Agent>()))
				{
					this.StealthAreaUsePoint.EnableStealthAreaUsePoint();
					this.IsStealthAreaTriggered = true;
				}
			}

			// Token: 0x04000748 RID: 1864
			internal bool IsStealthAreaTriggered;

			// Token: 0x04000749 RID: 1865
			internal bool IsReinforcementCalled;

			// Token: 0x0400074A RID: 1866
			internal readonly StealthAreaUsePoint StealthAreaUsePoint;

			// Token: 0x0400074B RID: 1867
			internal readonly Dictionary<StealthAreaMarker, List<Agent>> StealthAreaMarkers;
		}
	}
}
