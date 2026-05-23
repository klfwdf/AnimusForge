using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Conversation.MissionLogics
{
	// Token: 0x020000C6 RID: 198
	public class ConversationMissionLogic : MissionLogic
	{
		// Token: 0x1700009F RID: 159
		// (get) Token: 0x06000817 RID: 2071 RVA: 0x0003A391 File Offset: 0x00038591
		private bool IsReadyForConversation
		{
			get
			{
				return this._isRenderingStarted && Agent.Main != null && Agent.Main.IsActive();
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x06000818 RID: 2072 RVA: 0x0003A3AE File Offset: 0x000385AE
		// (set) Token: 0x06000819 RID: 2073 RVA: 0x0003A3B6 File Offset: 0x000385B6
		public ConversationCharacterData OtherSideConversationData { get; private set; }

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x0600081A RID: 2074 RVA: 0x0003A3BF File Offset: 0x000385BF
		// (set) Token: 0x0600081B RID: 2075 RVA: 0x0003A3C7 File Offset: 0x000385C7
		public ConversationCharacterData PlayerConversationData { get; private set; }

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x0600081C RID: 2076 RVA: 0x0003A3D0 File Offset: 0x000385D0
		// (set) Token: 0x0600081D RID: 2077 RVA: 0x0003A3D8 File Offset: 0x000385D8
		public bool IsMultiAgentConversation { get; private set; }

		// Token: 0x0600081E RID: 2078 RVA: 0x0003A3E4 File Offset: 0x000385E4
		public ConversationMissionLogic(ConversationCharacterData playerCharacterData, ConversationCharacterData otherCharacterData, bool isMultiAgentConversation)
		{
			this.PlayerConversationData = playerCharacterData;
			this.OtherSideConversationData = otherCharacterData;
			this.IsMultiAgentConversation = isMultiAgentConversation;
			bool isNaval;
			if (!isMultiAgentConversation)
			{
				PartyBase party = playerCharacterData.Party;
				bool? flag;
				if (party == null)
				{
					flag = null;
				}
				else
				{
					MobileParty mobileParty = party.MobileParty;
					flag = ((mobileParty != null) ? new bool?(mobileParty.IsCurrentlyAtSea) : null);
				}
				bool? flag2 = flag;
				if (flag2 == null)
				{
					PartyBase party2 = otherCharacterData.Party;
					bool? flag3;
					if (party2 == null)
					{
						flag3 = null;
					}
					else
					{
						MobileParty mobileParty2 = party2.MobileParty;
						flag3 = ((mobileParty2 != null) ? new bool?(mobileParty2.IsCurrentlyAtSea) : null);
					}
					isNaval = flag3 ?? false;
				}
				else
				{
					isNaval = flag2.GetValueOrDefault();
				}
			}
			else
			{
				isNaval = false;
			}
			this._isNaval = isNaval;
			this._isCivilianEquipmentRequiredForLeader = otherCharacterData.IsCivilianEquipmentRequiredForLeader;
			this._isCivilianEquipmentRequiredForBodyGuards = otherCharacterData.IsCivilianEquipmentRequiredForBodyGuardCharacters;
			this._addBloodToAgents = new List<Agent>();
		}

		// Token: 0x0600081F RID: 2079 RVA: 0x0003A4CC File Offset: 0x000386CC
		public override void AfterStart()
		{
			base.AfterStart();
			this._realCameraController = base.Mission.CameraIsFirstPerson;
			if (this._isNaval)
			{
				string navalConversationCameraTag = this.GetNavalConversationCameraTag(this.OtherSideConversationData.Party);
				Vec2 vec = Mission.Current.Scene.GetGlobalWindStrengthVector();
				float value = vec.Length * 2f;
				float waterStrength = Mission.Current.Scene.GetWaterStrength();
				this.CustomConversationCameraEntity = base.Mission.Scene.FindEntityWithTag(navalConversationCameraTag);
				Scene scene = Mission.Current.Scene;
				vec = MathF.Clamp(value, 1E-05f, 6f) * Vec2.Side;
				scene.SetGlobalWindStrengthVector(vec);
				Mission.Current.Scene.SetWaterStrength(MathF.Clamp(waterStrength, 1E-05f, 2.5f));
			}
			else if (this.IsMultiAgentConversation)
			{
				Vec2 vec = Mission.Current.Scene.GetGlobalWindStrengthVector();
				float value2 = vec.Length * 2f;
				float waterStrength2 = Mission.Current.Scene.GetWaterStrength();
				Scene scene2 = Mission.Current.Scene;
				vec = MathF.Clamp(value2, 1E-05f, 6f) * Vec2.Side;
				scene2.SetGlobalWindStrengthVector(vec);
				Mission.Current.Scene.SetWaterStrength(MathF.Clamp(waterStrength2, 1E-05f, 2.5f));
				base.Mission.CameraIsFirstPerson = true;
			}
			else
			{
				base.Mission.CameraIsFirstPerson = true;
			}
			IEnumerable<GameEntity> source = base.Mission.Scene.FindEntitiesWithTag("binary_conversation_point");
			if (source.Any<GameEntity>())
			{
				this._conversationSet = source.ToMBList<GameEntity>().GetRandomElement<GameEntity>();
			}
			this._usedSpawnPoints = new List<GameEntity>();
			BattleSideEnum battleSideEnum = BattleSideEnum.Attacker;
			if (this._isNaval)
			{
				battleSideEnum = BattleSideEnum.Attacker;
			}
			else if (PlayerSiege.PlayerSiegeEvent != null)
			{
				battleSideEnum = PlayerSiege.PlayerSide;
			}
			else if (PlayerEncounter.Current != null)
			{
				if (PlayerEncounter.InsideSettlement && PlayerEncounter.Current.OpponentSide != BattleSideEnum.Defender)
				{
					battleSideEnum = BattleSideEnum.Defender;
				}
				else
				{
					battleSideEnum = BattleSideEnum.Attacker;
				}
				if (PlayerEncounter.Current.EncounterSettlementAux != null && PlayerEncounter.Current.EncounterSettlementAux.MapFaction == Hero.MainHero.MapFaction)
				{
					if (PlayerEncounter.Current.EncounterSettlementAux.IsUnderSiege)
					{
						battleSideEnum = BattleSideEnum.Defender;
					}
					else
					{
						battleSideEnum = BattleSideEnum.Attacker;
					}
				}
			}
			base.Mission.PlayerTeam = base.Mission.Teams.Add(battleSideEnum, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			bool flag = !this.OtherSideConversationData.NoHorse && this.OtherSideConversationData.Character.Equipment[10].Item != null && this.OtherSideConversationData.Character.Equipment[10].Item.HasHorseComponent && battleSideEnum == BattleSideEnum.Defender;
			MatrixFrame matrixFrame;
			MatrixFrame initialFrame;
			if (this._conversationSet != null)
			{
				if (base.Mission.PlayerTeam.IsDefender)
				{
					matrixFrame = this.GetDefenderSideSpawnFrame();
					initialFrame = this.GetAttackerSideSpawnFrame(flag);
				}
				else
				{
					matrixFrame = this.GetAttackerSideSpawnFrame(flag);
					initialFrame = this.GetDefenderSideSpawnFrame();
				}
			}
			else
			{
				matrixFrame = this.GetPlayerSideSpawnFrameInSettlement();
				initialFrame = this.GetOtherSideSpawnFrameInSettlement(matrixFrame);
			}
			if (this._isNaval)
			{
				if (this._navalConversationState != ConversationMissionLogic.NavalConversationCameraState.SameShip)
				{
					GameEntity firstEntityWithName = base.Mission.Scene.GetFirstEntityWithName("Ship");
					if (firstEntityWithName != null)
					{
						WeakGameEntity weakEntity = firstEntityWithName.WeakEntity;
						WeakGameEntity firstChildEntityWithTag = weakEntity.GetFirstChildEntityWithTag("tall_rope");
						if (firstChildEntityWithTag != WeakGameEntity.Invalid)
						{
							this._agentHangPointTall = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag.GetFirstChildEntityWithTagRecursive("rope_hang_point"));
							this._agentHangPointSecondTall = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag.GetFirstChildEntityWithTagRecursive("rope_hang_point2"));
						}
						WeakGameEntity firstChildEntityWithTag2 = weakEntity.GetFirstChildEntityWithTag("short_rope");
						if (firstChildEntityWithTag2 != WeakGameEntity.Invalid)
						{
							this._agentHangPointShort = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag2.GetFirstChildEntityWithTagRecursive("rope_hang_point"));
							this._agentHangPointSecondShort = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag2.GetFirstChildEntityWithTagRecursive("rope_hang_point2"));
						}
					}
				}
				else
				{
					initialFrame.Rotate(3.1415927f, Vec3.Up);
				}
			}
			this.SpawnPlayer(this.PlayerConversationData, matrixFrame);
			this.SpawnOtherSide(this.OtherSideConversationData, initialFrame, flag, !base.Mission.PlayerTeam.IsDefender);
		}

		// Token: 0x06000820 RID: 2080 RVA: 0x0003A910 File Offset: 0x00038B10
		private void SpawnPlayer(ConversationCharacterData playerConversationData, MatrixFrame initialFrame)
		{
			MatrixFrame initialFrame2 = new MatrixFrame(ref initialFrame.rotation, ref initialFrame.origin);
			initialFrame2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			this.SpawnCharacter(CharacterObject.PlayerCharacter, playerConversationData, initialFrame2, ActionIndexCache.act_conversation_normal_loop);
		}

		// Token: 0x06000821 RID: 2081 RVA: 0x0003A950 File Offset: 0x00038B50
		private void SpawnOtherSide(ConversationCharacterData characterData, MatrixFrame initialFrame, bool spawnWithHorse, bool isDefenderSide)
		{
			MatrixFrame matrixFrame = new MatrixFrame(ref initialFrame.rotation, ref initialFrame.origin);
			if (!this._isNaval && Agent.Main != null)
			{
				matrixFrame.rotation.f = Agent.Main.Position - matrixFrame.origin;
			}
			matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterData.Character.Race, "_settlement");
			AgentBuildData agentBuildData = new AgentBuildData(characterData.Character).TroopOrigin(new SimpleAgentOrigin(characterData.Character, -1, null, default(UniqueTroopDescriptor))).Team(base.Mission.PlayerTeam).Monster(monsterWithSuffix)
				.InitialPosition(matrixFrame.origin);
			Vec2 asVec = matrixFrame.rotation.f.AsVec2;
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(asVec).NoHorses(!spawnWithHorse).CivilianEquipment(this._isCivilianEquipmentRequiredForLeader)
				.SetPrepareImmediately();
			Hero heroObject = characterData.Character.HeroObject;
			if (((heroObject != null) ? heroObject.MapFaction : null) != null)
			{
				agentBuildData2.Banner(characterData.Character.HeroObject.MapFaction.Banner);
				agentBuildData2.ClothingColor1(characterData.Character.HeroObject.MapFaction.Color).ClothingColor2(characterData.Character.HeroObject.MapFaction.Color2);
			}
			else
			{
				PartyBase party = characterData.Party;
				bool flag;
				if (party == null)
				{
					flag = null != null;
				}
				else
				{
					Hero leaderHero = party.LeaderHero;
					flag = ((leaderHero != null) ? leaderHero.ClanBanner : null) != null;
				}
				if (flag)
				{
					agentBuildData2.Banner(characterData.Party.LeaderHero.ClanBanner);
					agentBuildData2.ClothingColor1(characterData.Party.LeaderHero.MapFaction.Color).ClothingColor2(characterData.Party.LeaderHero.MapFaction.Color2);
				}
				else
				{
					PartyBase party2 = characterData.Party;
					if (((party2 != null) ? party2.MapFaction : null) != null)
					{
						AgentBuildData agentBuildData3 = agentBuildData2;
						PartyBase party3 = characterData.Party;
						Banner banner;
						if (party3 == null)
						{
							banner = null;
						}
						else
						{
							IFaction mapFaction = party3.MapFaction;
							banner = ((mapFaction != null) ? mapFaction.Banner : null);
						}
						agentBuildData3.Banner(banner);
						agentBuildData2.ClothingColor1(characterData.Party.MapFaction.Color).ClothingColor2(characterData.Party.MapFaction.Color2);
					}
				}
			}
			if (spawnWithHorse)
			{
				agentBuildData2.MountKey(MountCreationKey.GetRandomMountKeyString(characterData.Character.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, characterData.Character.GetMountKeySeed()));
			}
			if (characterData.Party != null)
			{
				agentBuildData2.TroopOrigin(new PartyAgentOrigin(characterData.Party, characterData.Character, 0, new UniqueTroopDescriptor(FlattenedTroopRoster.GenerateUniqueNoFromParty(characterData.Party.MobileParty, 0)), false, false));
			}
			Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
			this._otherPartyHeightMultiplier = agent.GetEyeGlobalHeight();
			if (characterData.SpawnedAfterFight)
			{
				this._addBloodToAgents.Add(agent);
			}
			if (agent.MountAgent == null)
			{
				agent.SetActionChannel(0, ActionIndexCache.act_conversation_normal_loop, false, (AnimFlags)0UL, 0f, 1f, 0f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
			}
			else
			{
				agent.MountAgent.AgentVisuals.SetAgentLodZeroOrMax(true);
			}
			agent.AgentVisuals.SetAgentLodZeroOrMax(true);
			this._curConversationPartnerAgent = agent;
			bool flag2 = characterData.Character.HeroObject != null && characterData.Character.HeroObject.IsPlayerCompanion;
			if (!characterData.NoBodyguards && !flag2)
			{
				this.SpawnBodyguards(isDefenderSide);
			}
		}

		// Token: 0x06000822 RID: 2082 RVA: 0x0003ACB8 File Offset: 0x00038EB8
		private MatrixFrame GetDefenderSideSpawnFrame()
		{
			MatrixFrame result = MatrixFrame.Identity;
			foreach (GameEntity gameEntity in this._conversationSet.GetChildren())
			{
				if (gameEntity.HasTag("opponent_infantry_spawn"))
				{
					result = gameEntity.GetGlobalFrame();
					break;
				}
			}
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			return result;
		}

		// Token: 0x06000823 RID: 2083 RVA: 0x0003AD2C File Offset: 0x00038F2C
		private MatrixFrame GetAttackerSideSpawnFrame(bool hasHorse)
		{
			MatrixFrame result = MatrixFrame.Identity;
			if (this._isNaval && this.CustomConversationCameraEntity != null)
			{
				result = this.CustomConversationCameraEntity.GetGlobalFrame();
			}
			else
			{
				foreach (GameEntity gameEntity in this._conversationSet.GetChildren())
				{
					if (hasHorse && gameEntity.HasTag("player_cavalry_spawn"))
					{
						result = gameEntity.GetGlobalFrame();
						break;
					}
					if (gameEntity.HasTag("player_infantry_spawn"))
					{
						result = gameEntity.GetGlobalFrame();
						break;
					}
				}
			}
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			return result;
		}

		// Token: 0x06000824 RID: 2084 RVA: 0x0003ADE0 File Offset: 0x00038FE0
		private MatrixFrame GetPlayerSideSpawnFrameInSettlement()
		{
			GameEntity gameEntity;
			if ((gameEntity = base.Mission.Scene.FindEntityWithTag("spawnpoint_player")) == null)
			{
				gameEntity = base.Mission.Scene.FindEntitiesWithTag("sp_player_conversation").FirstOrDefault<GameEntity>() ?? base.Mission.Scene.FindEntityWithTag("spawnpoint_player_outside");
			}
			GameEntity gameEntity2 = gameEntity;
			MatrixFrame result = ((gameEntity2 != null) ? gameEntity2.GetFrame() : MatrixFrame.Identity);
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			return result;
		}

		// Token: 0x06000825 RID: 2085 RVA: 0x0003AE58 File Offset: 0x00039058
		private MatrixFrame GetOtherSideSpawnFrameInSettlement(MatrixFrame playerFrame)
		{
			MatrixFrame result = playerFrame;
			Vec3 v = new Vec3(playerFrame.rotation.f, -1f);
			v.Normalize();
			result.origin = playerFrame.origin + 4f * v;
			result.rotation.RotateAboutUp(3.1415927f);
			return result;
		}

		// Token: 0x06000826 RID: 2086 RVA: 0x0003AEB5 File Offset: 0x000390B5
		public override void OnRenderingStarted()
		{
			this._isRenderingStarted = true;
			Debug.Print("\n ConversationMissionLogic::OnRenderingStarted\n", 0, Debug.DebugColor.Cyan, 64UL);
		}

		// Token: 0x06000827 RID: 2087 RVA: 0x0003AECD File Offset: 0x000390CD
		private void InitializeAfterCreation(Agent conversationPartnerAgent, PartyBase conversationPartnerParty)
		{
			Campaign.Current.ConversationManager.SetupAndStartMapConversation((conversationPartnerParty != null) ? conversationPartnerParty.MobileParty : null, conversationPartnerAgent, Mission.Current.MainAgentServer);
			base.Mission.SetMissionMode(MissionMode.Conversation, true);
		}

		// Token: 0x06000828 RID: 2088 RVA: 0x0003AF04 File Offset: 0x00039104
		public override void OnMissionTick(float dt)
		{
			if (this._addBloodToAgents.Count > 0)
			{
				foreach (Agent agent in this._addBloodToAgents)
				{
					ValueTuple<sbyte, sbyte> randomPairOfRealBloodBurstBoneIndices = agent.GetRandomPairOfRealBloodBurstBoneIndices();
					if (randomPairOfRealBloodBurstBoneIndices.Item1 != -1 && randomPairOfRealBloodBurstBoneIndices.Item2 != -1)
					{
						agent.CreateBloodBurstAtLimb(randomPairOfRealBloodBurstBoneIndices.Item1, 0.1f + MBRandom.RandomFloat * 0.1f);
						agent.CreateBloodBurstAtLimb(randomPairOfRealBloodBurstBoneIndices.Item2, 0.2f + MBRandom.RandomFloat * 0.2f);
					}
				}
				this._addBloodToAgents.Clear();
			}
			if (!this._conversationStarted)
			{
				if (!this.IsReadyForConversation)
				{
					return;
				}
				this.InitializeAfterCreation(this._curConversationPartnerAgent, this.OtherSideConversationData.Party);
				this._conversationStarted = true;
			}
			if (base.Mission.InputManager.IsGameKeyPressed(4))
			{
				Campaign.Current.ConversationManager.EndConversation();
			}
			if (this._isNaval && this._curConversationPartnerAgent != null && this._agentHangPointShort != null && this._navalConversationState != ConversationMissionLogic.NavalConversationCameraState.SameShip)
			{
				if (ActionIndexCache.act_conversation_naval_start == this._curConversationPartnerAgent.GetCurrentAction(0) || ActionIndexCache.act_conversation_naval_idle_loop == this._curConversationPartnerAgent.GetCurrentAction(0))
				{
					MatrixFrame globalFrame = ((this._otherPartyHeightMultiplier >= 1.76f) ? this._agentHangPointTall : this._agentHangPointShort).GetGlobalFrame();
					Vec3 vec = ((this._otherPartyHeightMultiplier >= 1.76f) ? this._agentHangPointSecondTall : this._agentHangPointSecondShort).GetGlobalFrame().origin - globalFrame.origin;
					vec.Normalize();
					Vec3 vec2 = globalFrame.rotation.f;
					vec2.Normalize();
					Vec3 vec3 = Vec3.CrossProduct(vec2, vec);
					vec3.Normalize();
					vec2 = Vec3.CrossProduct(vec, vec3);
					vec2.Normalize();
					globalFrame.rotation.f = vec2;
					globalFrame.rotation.u = -vec;
					globalFrame.rotation.s = -vec3;
					Agent curConversationPartnerAgent = this._curConversationPartnerAgent;
					MatrixFrame identity = MatrixFrame.Identity;
					curConversationPartnerAgent.SetHandInverseKinematicsFrame(globalFrame, identity);
				}
				else
				{
					this._curConversationPartnerAgent.ClearHandInverseKinematics();
				}
			}
			if (this.IsMultiAgentConversation && (ActionIndexCache.act_conversation_naval_start == this._curConversationPartnerAgent.GetCurrentAction(0) || ActionIndexCache.act_conversation_naval_idle_loop == this._curConversationPartnerAgent.GetCurrentAction(0)))
			{
				this._curConversationPartnerAgent.SetCurrentActionProgress(0, 1f);
				this._curConversationPartnerAgent.SetActionChannel(0, ActionIndexCache.act_conversation_normal_loop, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
			if (!Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				base.Mission.EndMission();
			}
		}

		// Token: 0x06000829 RID: 2089 RVA: 0x0003B204 File Offset: 0x00039404
		private void SpawnBodyguards(bool isDefenderSide)
		{
			int num = 2;
			ConversationCharacterData otherSideConversationData = this.OtherSideConversationData;
			if (otherSideConversationData.Party == null)
			{
				return;
			}
			TroopRoster memberRoster = otherSideConversationData.Party.MemberRoster;
			int num2 = memberRoster.TotalManCount;
			if (memberRoster.Contains(CharacterObject.PlayerCharacter))
			{
				num2--;
			}
			if (num2 < num + 1)
			{
				return;
			}
			List<CharacterObject> list = new List<CharacterObject>();
			using (List<TroopRosterElement>.Enumerator enumerator = memberRoster.GetTroopRoster().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					TroopRosterElement troopRosterElement = enumerator.Current;
					if (troopRosterElement.Character.IsHero && otherSideConversationData.Character != troopRosterElement.Character && !list.Contains(troopRosterElement.Character) && troopRosterElement.Character.HeroObject.IsWounded && !troopRosterElement.Character.IsPlayerCharacter)
					{
						list.Add(troopRosterElement.Character);
					}
				}
				goto IL_16B;
			}
			IL_D4:
			foreach (TroopRosterElement troopRosterElement2 in from k in memberRoster.GetTroopRoster()
				orderby k.Character.Level descending
				select k)
			{
				if ((!otherSideConversationData.Character.IsHero || otherSideConversationData.Character != troopRosterElement2.Character) && !troopRosterElement2.Character.IsPlayerCharacter)
				{
					list.Add(troopRosterElement2.Character);
				}
				if (list.Count == num)
				{
					break;
				}
			}
			IL_16B:
			if (list.Count >= num)
			{
				List<ActionIndexCache> list2 = new List<ActionIndexCache>
				{
					ActionIndexCache.act_stand_1,
					ActionIndexCache.act_inventory_idle_start,
					ActionIndexCache.act_inventory_idle,
					ActionIndexCache.act_conversation_normal_loop,
					ActionIndexCache.act_conversation_warrior_loop,
					ActionIndexCache.act_conversation_hip_loop,
					ActionIndexCache.act_conversation_closed_loop,
					ActionIndexCache.act_conversation_demure_loop
				};
				for (int i = 0; i < num; i++)
				{
					int index = new Random().Next(0, list.Count);
					int index2 = MBRandom.RandomInt(0, list2.Count);
					CharacterObject character = list[index];
					ConversationCharacterData characterData = otherSideConversationData;
					MatrixFrame bodyguardSpawnFrame = this.GetBodyguardSpawnFrame(list[index].HasMount(), isDefenderSide);
					ActionIndexCache actionIndexCache = list2[index2];
					this.SpawnCharacter(character, characterData, bodyguardSpawnFrame, actionIndexCache);
					list2.RemoveAt(index2);
					list.RemoveAt(index);
				}
				return;
			}
			goto IL_D4;
		}

		// Token: 0x0600082A RID: 2090 RVA: 0x0003B47C File Offset: 0x0003967C
		private void SpawnCharacter(CharacterObject character, ConversationCharacterData characterData, MatrixFrame initialFrame, in ActionIndexCache conversationAction)
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
			AgentBuildData agentBuildData = new AgentBuildData(character).TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Team(base.Mission.PlayerTeam).Monster(monsterWithSuffix)
				.InitialPosition(initialFrame.origin);
			Vec2 vec = initialFrame.rotation.f.AsVec2;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).NoHorses(character.HasMount()).NoWeapons(characterData.NoWeapon)
				.CivilianEquipment((character == CharacterObject.PlayerCharacter) ? this._isCivilianEquipmentRequiredForLeader : this._isCivilianEquipmentRequiredForBodyGuards)
				.SetPrepareImmediately();
			PartyBase party = characterData.Party;
			bool flag;
			if (party == null)
			{
				flag = null != null;
			}
			else
			{
				Hero leaderHero = party.LeaderHero;
				flag = ((leaderHero != null) ? leaderHero.ClanBanner : null) != null;
			}
			if (flag)
			{
				agentBuildData2.Banner(characterData.Party.LeaderHero.ClanBanner);
			}
			else if (characterData.Party != null)
			{
				PartyBase party2 = characterData.Party;
				if (((party2 != null) ? party2.MapFaction : null) != null)
				{
					agentBuildData2.Banner(characterData.Party.MapFaction.Banner);
				}
			}
			if (characterData.Party != null)
			{
				agentBuildData2.ClothingColor1(characterData.Party.MapFaction.Color).ClothingColor2(characterData.Party.MapFaction.Color2);
			}
			if (characterData.Character == CharacterObject.PlayerCharacter)
			{
				agentBuildData2.Controller(AgentControllerType.Player);
			}
			Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
			agent.AgentVisuals.SetAgentLodZeroOrMax(true);
			agent.SetLookAgent(Agent.Main);
			AnimationSystemData animationSystemData = agentBuildData2.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agentBuildData2.AgentMonster, agentBuildData2.AgentIsFemale, "_poses"), character.GetStepSize(), false);
			agent.SetActionSet(ref animationSystemData);
			if (characterData.Character == CharacterObject.PlayerCharacter)
			{
				agent.AgentVisuals.GetSkeleton().TickAnimationsAndForceUpdate(0.1f, initialFrame, true);
			}
			if (characterData.SpawnedAfterFight)
			{
				this._addBloodToAgents.Add(agent);
				return;
			}
			if (agent.MountAgent == null)
			{
				agent.SetActionChannel(0, conversationAction, false, (AnimFlags)0UL, 0f, 1f, 0f, 0.4f, MBRandom.RandomFloat * 0.8f, false, -0.2f, 0, true);
			}
		}

		// Token: 0x0600082B RID: 2091 RVA: 0x0003B6B4 File Offset: 0x000398B4
		private MatrixFrame GetBodyguardSpawnFrame(bool spawnWithHorse, bool isDefenderSide)
		{
			MatrixFrame result = MatrixFrame.Identity;
			foreach (GameEntity gameEntity in this._conversationSet.GetChildren())
			{
				if (!isDefenderSide)
				{
					if (spawnWithHorse && gameEntity.HasTag("player_bodyguard_cavalry_spawn") && !this._usedSpawnPoints.Contains(gameEntity))
					{
						this._usedSpawnPoints.Add(gameEntity);
						result = gameEntity.GetGlobalFrame();
						break;
					}
					if (gameEntity.HasTag("player_bodyguard_infantry_spawn") && !this._usedSpawnPoints.Contains(gameEntity))
					{
						this._usedSpawnPoints.Add(gameEntity);
						result = gameEntity.GetGlobalFrame();
						break;
					}
				}
				else
				{
					if (spawnWithHorse && gameEntity.HasTag("opponent_bodyguard_cavalry_spawn") && !this._usedSpawnPoints.Contains(gameEntity))
					{
						this._usedSpawnPoints.Add(gameEntity);
						result = gameEntity.GetGlobalFrame();
						break;
					}
					if (gameEntity.HasTag("opponent_bodyguard_infantry_spawn") && !this._usedSpawnPoints.Contains(gameEntity))
					{
						this._usedSpawnPoints.Add(gameEntity);
						result = gameEntity.GetGlobalFrame();
						break;
					}
				}
			}
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			return result;
		}

		// Token: 0x0600082C RID: 2092 RVA: 0x0003B7E8 File Offset: 0x000399E8
		protected override void OnEndMission()
		{
			this._conversationSet = null;
			base.Mission.CameraIsFirstPerson = this._realCameraController;
		}

		// Token: 0x0600082D RID: 2093 RVA: 0x0003B804 File Offset: 0x00039A04
		private string GetNavalConversationCameraTag(PartyBase encounteredParty)
		{
			string result;
			if (encounteredParty == null || encounteredParty == PartyBase.MainParty)
			{
				result = "custom_camera_same_ship";
				this._navalConversationState = ConversationMissionLogic.NavalConversationCameraState.SameShip;
			}
			else
			{
				ShipHull.ShipType shipType = ((PartyBase.MainParty.Ships.Count > 0) ? PartyBase.MainParty.FlagShip.ShipHull.Type : ShipHull.ShipType.Medium);
				ShipHull.ShipType shipType2 = (encounteredParty.Ships.IsEmpty<Ship>() ? shipType : encounteredParty.FlagShip.ShipHull.Type);
				if (shipType < shipType2)
				{
					result = "custom_camera_lookup";
					this._navalConversationState = ConversationMissionLogic.NavalConversationCameraState.LookUp;
				}
				else if (shipType > shipType2)
				{
					result = "custom_camera_lookdown";
					this._navalConversationState = ConversationMissionLogic.NavalConversationCameraState.LookDown;
				}
				else
				{
					result = "custom_camera_level";
					this._navalConversationState = ConversationMissionLogic.NavalConversationCameraState.Level;
				}
			}
			return result;
		}

		// Token: 0x04000414 RID: 1044
		private const float MinimumAgentHeightForRopeAnimation = 1.76f;

		// Token: 0x04000415 RID: 1045
		private const float MaximumWindStrength = 6f;

		// Token: 0x04000416 RID: 1046
		private const float MaximumWaveStrength = 2.5f;

		// Token: 0x04000417 RID: 1047
		private const float WindStrengthAmplifier = 2f;

		// Token: 0x04000418 RID: 1048
		private readonly List<Agent> _addBloodToAgents;

		// Token: 0x04000419 RID: 1049
		private Agent _curConversationPartnerAgent;

		// Token: 0x0400041A RID: 1050
		private bool _isRenderingStarted;

		// Token: 0x0400041B RID: 1051
		private bool _conversationStarted;

		// Token: 0x0400041C RID: 1052
		private bool _isCivilianEquipmentRequiredForLeader;

		// Token: 0x0400041D RID: 1053
		private bool _isCivilianEquipmentRequiredForBodyGuards;

		// Token: 0x0400041E RID: 1054
		private List<GameEntity> _usedSpawnPoints;

		// Token: 0x0400041F RID: 1055
		private GameEntity _agentHangPointShort;

		// Token: 0x04000420 RID: 1056
		private GameEntity _agentHangPointSecondShort;

		// Token: 0x04000421 RID: 1057
		private GameEntity _agentHangPointTall;

		// Token: 0x04000422 RID: 1058
		private GameEntity _agentHangPointSecondTall;

		// Token: 0x04000423 RID: 1059
		private GameEntity _conversationSet;

		// Token: 0x04000424 RID: 1060
		private bool _realCameraController;

		// Token: 0x04000425 RID: 1061
		private readonly bool _isNaval;

		// Token: 0x04000426 RID: 1062
		private float _otherPartyHeightMultiplier;

		// Token: 0x04000427 RID: 1063
		private ConversationMissionLogic.NavalConversationCameraState _navalConversationState;

		// Token: 0x04000428 RID: 1064
		public GameEntity CustomConversationCameraEntity;

		// Token: 0x020001DA RID: 474
		private enum NavalConversationCameraState
		{
			// Token: 0x040008D6 RID: 2262
			None,
			// Token: 0x040008D7 RID: 2263
			SameShip,
			// Token: 0x040008D8 RID: 2264
			Level,
			// Token: 0x040008D9 RID: 2265
			LookDown,
			// Token: 0x040008DA RID: 2266
			LookUp
		}
	}
}
