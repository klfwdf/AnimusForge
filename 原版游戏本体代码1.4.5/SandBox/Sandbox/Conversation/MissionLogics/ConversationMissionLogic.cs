using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Conversation.MissionLogics;

public class ConversationMissionLogic : MissionLogic
{
	private enum NavalConversationCameraState
	{
		None,
		SameShip,
		Level,
		LookDown,
		LookUp
	}

	private const float MinimumAgentHeightForRopeAnimation = 1.76f;

	private const float MaximumWindStrength = 6f;

	private const float MaximumWaveStrength = 2.5f;

	private const float WindStrengthAmplifier = 2f;

	private readonly List<Agent> _addBloodToAgents;

	private Agent _curConversationPartnerAgent;

	private bool _isRenderingStarted;

	private bool _conversationStarted;

	private bool _isCivilianEquipmentRequiredForLeader;

	private bool _isCivilianEquipmentRequiredForBodyGuards;

	private List<GameEntity> _usedSpawnPoints;

	private GameEntity _agentHangPointShort;

	private GameEntity _agentHangPointSecondShort;

	private GameEntity _agentHangPointTall;

	private GameEntity _agentHangPointSecondTall;

	private GameEntity _conversationSet;

	private bool _realCameraController;

	private readonly bool _isNaval;

	private float _otherPartyHeightMultiplier;

	private NavalConversationCameraState _navalConversationState;

	public GameEntity CustomConversationCameraEntity;

	private bool IsReadyForConversation
	{
		get
		{
			if (_isRenderingStarted && Agent.Main != null)
			{
				return Agent.Main.IsActive();
			}
			return false;
		}
	}

	public ConversationCharacterData OtherSideConversationData { get; private set; }

	public ConversationCharacterData PlayerConversationData { get; private set; }

	public bool IsMultiAgentConversation { get; private set; }

	public ConversationMissionLogic(ConversationCharacterData playerCharacterData, ConversationCharacterData otherCharacterData, bool isMultiAgentConversation)
	{
		PlayerConversationData = playerCharacterData;
		OtherSideConversationData = otherCharacterData;
		IsMultiAgentConversation = isMultiAgentConversation;
		int isNaval;
		if (!isMultiAgentConversation)
		{
			PartyBase party = playerCharacterData.Party;
			if (party == null || party.MobileParty?.IsCurrentlyAtSea != true)
			{
				PartyBase party2 = otherCharacterData.Party;
				isNaval = ((party2 != null && party2.MobileParty?.IsCurrentlyAtSea == true) ? 1 : 0);
			}
			else
			{
				isNaval = 1;
			}
		}
		else
		{
			isNaval = 0;
		}
		_isNaval = (byte)isNaval != 0;
		_isCivilianEquipmentRequiredForLeader = otherCharacterData.IsCivilianEquipmentRequiredForLeader;
		_isCivilianEquipmentRequiredForBodyGuards = otherCharacterData.IsCivilianEquipmentRequiredForBodyGuardCharacters;
		_addBloodToAgents = new List<Agent>();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_realCameraController = base.Mission.CameraIsFirstPerson;
		if (_isNaval)
		{
			string navalConversationCameraTag = GetNavalConversationCameraTag(OtherSideConversationData.Party);
			float value = Mission.Current.Scene.GetGlobalWindStrengthVector().Length * 2f;
			float waterStrength = Mission.Current.Scene.GetWaterStrength();
			CustomConversationCameraEntity = base.Mission.Scene.FindEntityWithTag(navalConversationCameraTag);
			Mission.Current.Scene.SetGlobalWindStrengthVector(TaleWorlds.Library.MathF.Clamp(value, 1E-05f, 6f) * Vec2.Side);
			Mission.Current.Scene.SetWaterStrength(TaleWorlds.Library.MathF.Clamp(waterStrength, 1E-05f, 2.5f));
		}
		else if (IsMultiAgentConversation)
		{
			float value2 = Mission.Current.Scene.GetGlobalWindStrengthVector().Length * 2f;
			float waterStrength2 = Mission.Current.Scene.GetWaterStrength();
			Mission.Current.Scene.SetGlobalWindStrengthVector(TaleWorlds.Library.MathF.Clamp(value2, 1E-05f, 6f) * Vec2.Side);
			Mission.Current.Scene.SetWaterStrength(TaleWorlds.Library.MathF.Clamp(waterStrength2, 1E-05f, 2.5f));
			base.Mission.CameraIsFirstPerson = true;
		}
		else
		{
			base.Mission.CameraIsFirstPerson = true;
		}
		IEnumerable<GameEntity> source = base.Mission.Scene.FindEntitiesWithTag("binary_conversation_point");
		if (source.Any())
		{
			_conversationSet = source.ToMBList().GetRandomElement();
		}
		_usedSpawnPoints = new List<GameEntity>();
		BattleSideEnum battleSideEnum = BattleSideEnum.Attacker;
		if (_isNaval)
		{
			battleSideEnum = BattleSideEnum.Attacker;
		}
		else if (PlayerSiege.PlayerSiegeEvent != null)
		{
			battleSideEnum = PlayerSiege.PlayerSide;
		}
		else if (PlayerEncounter.Current != null)
		{
			battleSideEnum = ((!PlayerEncounter.InsideSettlement || PlayerEncounter.Current.OpponentSide == BattleSideEnum.Defender) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			if (PlayerEncounter.Current.EncounterSettlementAux != null && PlayerEncounter.Current.EncounterSettlementAux.MapFaction == Hero.MainHero.MapFaction)
			{
				battleSideEnum = ((!PlayerEncounter.Current.EncounterSettlementAux.IsUnderSiege) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			}
		}
		base.Mission.PlayerTeam = base.Mission.Teams.Add(battleSideEnum, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2);
		bool flag = !OtherSideConversationData.NoHorse && OtherSideConversationData.Character.Equipment[10].Item != null && OtherSideConversationData.Character.Equipment[10].Item.HasHorseComponent && battleSideEnum == BattleSideEnum.Defender;
		MatrixFrame matrixFrame;
		MatrixFrame initialFrame;
		if (_conversationSet != null)
		{
			if (base.Mission.PlayerTeam.IsDefender)
			{
				matrixFrame = GetDefenderSideSpawnFrame();
				initialFrame = GetAttackerSideSpawnFrame(flag);
			}
			else
			{
				matrixFrame = GetAttackerSideSpawnFrame(flag);
				initialFrame = GetDefenderSideSpawnFrame();
			}
		}
		else
		{
			matrixFrame = GetPlayerSideSpawnFrameInSettlement();
			initialFrame = GetOtherSideSpawnFrameInSettlement(matrixFrame);
		}
		if (_isNaval)
		{
			if (_navalConversationState != NavalConversationCameraState.SameShip)
			{
				GameEntity firstEntityWithName = base.Mission.Scene.GetFirstEntityWithName("Ship");
				if (firstEntityWithName != null)
				{
					WeakGameEntity weakEntity = firstEntityWithName.WeakEntity;
					WeakGameEntity firstChildEntityWithTag = weakEntity.GetFirstChildEntityWithTag("tall_rope");
					if (firstChildEntityWithTag != WeakGameEntity.Invalid)
					{
						_agentHangPointTall = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag.GetFirstChildEntityWithTagRecursive("rope_hang_point"));
						_agentHangPointSecondTall = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag.GetFirstChildEntityWithTagRecursive("rope_hang_point2"));
					}
					WeakGameEntity firstChildEntityWithTag2 = weakEntity.GetFirstChildEntityWithTag("short_rope");
					if (firstChildEntityWithTag2 != WeakGameEntity.Invalid)
					{
						_agentHangPointShort = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag2.GetFirstChildEntityWithTagRecursive("rope_hang_point"));
						_agentHangPointSecondShort = GameEntity.CreateFromWeakEntity(firstChildEntityWithTag2.GetFirstChildEntityWithTagRecursive("rope_hang_point2"));
					}
				}
			}
			else
			{
				initialFrame.Rotate(System.MathF.PI, in Vec3.Up);
			}
		}
		SpawnPlayer(PlayerConversationData, matrixFrame);
		SpawnOtherSide(OtherSideConversationData, initialFrame, flag, !base.Mission.PlayerTeam.IsDefender);
	}

	private void SpawnPlayer(ConversationCharacterData playerConversationData, MatrixFrame initialFrame)
	{
		MatrixFrame initialFrame2 = new MatrixFrame(in initialFrame.rotation, in initialFrame.origin);
		initialFrame2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		SpawnCharacter(CharacterObject.PlayerCharacter, playerConversationData, initialFrame2, in ActionIndexCache.act_conversation_normal_loop);
	}

	private void SpawnOtherSide(ConversationCharacterData characterData, MatrixFrame initialFrame, bool spawnWithHorse, bool isDefenderSide)
	{
		MatrixFrame matrixFrame = new MatrixFrame(in initialFrame.rotation, in initialFrame.origin);
		if (!_isNaval && Agent.Main != null)
		{
			matrixFrame.rotation.f = Agent.Main.Position - matrixFrame.origin;
		}
		matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterData.Character.Race, "_settlement");
		AgentBuildData agentBuildData = new AgentBuildData(characterData.Character).TroopOrigin(new SimpleAgentOrigin(characterData.Character)).Team(base.Mission.PlayerTeam).Monster(monsterWithSuffix)
			.InitialPosition(in matrixFrame.origin)
			.InitialDirection(matrixFrame.rotation.f.AsVec2)
			.NoHorses(!spawnWithHorse)
			.CivilianEquipment(_isCivilianEquipmentRequiredForLeader)
			.SetPrepareImmediately();
		if (characterData.Character.HeroObject?.MapFaction != null)
		{
			agentBuildData.Banner(characterData.Character.HeroObject.MapFaction.Banner);
			agentBuildData.ClothingColor1(characterData.Character.HeroObject.MapFaction.Color).ClothingColor2(characterData.Character.HeroObject.MapFaction.Color2);
		}
		else if (characterData.Party?.LeaderHero?.ClanBanner != null)
		{
			agentBuildData.Banner(characterData.Party.LeaderHero.ClanBanner);
			agentBuildData.ClothingColor1(characterData.Party.LeaderHero.MapFaction.Color).ClothingColor2(characterData.Party.LeaderHero.MapFaction.Color2);
		}
		else if (characterData.Party?.MapFaction != null)
		{
			agentBuildData.Banner(characterData.Party?.MapFaction?.Banner);
			agentBuildData.ClothingColor1(characterData.Party.MapFaction.Color).ClothingColor2(characterData.Party.MapFaction.Color2);
		}
		if (spawnWithHorse)
		{
			agentBuildData.MountKey(MountCreationKey.GetRandomMountKeyString(characterData.Character.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, characterData.Character.GetMountKeySeed()));
		}
		if (characterData.Party != null)
		{
			agentBuildData.TroopOrigin(new PartyAgentOrigin(characterData.Party, characterData.Character, 0, new UniqueTroopDescriptor(FlattenedTroopRoster.GenerateUniqueNoFromParty(characterData.Party.MobileParty, 0))));
		}
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		_otherPartyHeightMultiplier = agent.GetEyeGlobalHeight();
		if (characterData.SpawnedAfterFight)
		{
			_addBloodToAgents.Add(agent);
		}
		if (agent.MountAgent == null)
		{
			agent.SetActionChannel(0, in ActionIndexCache.act_conversation_normal_loop, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, 0f, 0.4f, MBRandom.RandomFloat);
		}
		else
		{
			agent.MountAgent.AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
		}
		agent.AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
		_curConversationPartnerAgent = agent;
		bool flag = characterData.Character.HeroObject != null && characterData.Character.HeroObject.IsPlayerCompanion;
		if (!characterData.NoBodyguards && !flag)
		{
			SpawnBodyguards(isDefenderSide);
		}
	}

	private MatrixFrame GetDefenderSideSpawnFrame()
	{
		MatrixFrame result = MatrixFrame.Identity;
		foreach (GameEntity child in _conversationSet.GetChildren())
		{
			if (child.HasTag("opponent_infantry_spawn"))
			{
				result = child.GetGlobalFrame();
				break;
			}
		}
		result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return result;
	}

	private MatrixFrame GetAttackerSideSpawnFrame(bool hasHorse)
	{
		MatrixFrame result = MatrixFrame.Identity;
		if (_isNaval && CustomConversationCameraEntity != null)
		{
			result = CustomConversationCameraEntity.GetGlobalFrame();
		}
		else
		{
			foreach (GameEntity child in _conversationSet.GetChildren())
			{
				if (hasHorse && child.HasTag("player_cavalry_spawn"))
				{
					result = child.GetGlobalFrame();
					break;
				}
				if (child.HasTag("player_infantry_spawn"))
				{
					result = child.GetGlobalFrame();
					break;
				}
			}
		}
		result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return result;
	}

	private MatrixFrame GetPlayerSideSpawnFrameInSettlement()
	{
		MatrixFrame result = (base.Mission.Scene.FindEntityWithTag("spawnpoint_player") ?? base.Mission.Scene.FindEntitiesWithTag("sp_player_conversation").FirstOrDefault() ?? base.Mission.Scene.FindEntityWithTag("spawnpoint_player_outside"))?.GetFrame() ?? MatrixFrame.Identity;
		result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return result;
	}

	private MatrixFrame GetOtherSideSpawnFrameInSettlement(MatrixFrame playerFrame)
	{
		MatrixFrame result = playerFrame;
		Vec3 vec = new Vec3(playerFrame.rotation.f);
		vec.Normalize();
		result.origin = playerFrame.origin + 4f * vec;
		result.rotation.RotateAboutUp(System.MathF.PI);
		return result;
	}

	public override void OnRenderingStarted()
	{
		_isRenderingStarted = true;
		Debug.Print("\n ConversationMissionLogic::OnRenderingStarted\n", 0, Debug.DebugColor.Cyan, 64uL);
	}

	private void InitializeAfterCreation(Agent conversationPartnerAgent, PartyBase conversationPartnerParty)
	{
		Campaign.Current.ConversationManager.SetupAndStartMapConversation(conversationPartnerParty?.MobileParty, conversationPartnerAgent, Mission.Current.MainAgentServer);
		base.Mission.SetMissionMode(MissionMode.Conversation, atStart: true);
	}

	public override void OnMissionTick(float dt)
	{
		if (_addBloodToAgents.Count > 0)
		{
			foreach (Agent addBloodToAgent in _addBloodToAgents)
			{
				(sbyte, sbyte) randomPairOfRealBloodBurstBoneIndices = addBloodToAgent.GetRandomPairOfRealBloodBurstBoneIndices();
				if (randomPairOfRealBloodBurstBoneIndices.Item1 != -1 && randomPairOfRealBloodBurstBoneIndices.Item2 != -1)
				{
					addBloodToAgent.CreateBloodBurstAtLimb(randomPairOfRealBloodBurstBoneIndices.Item1, 0.1f + MBRandom.RandomFloat * 0.1f);
					addBloodToAgent.CreateBloodBurstAtLimb(randomPairOfRealBloodBurstBoneIndices.Item2, 0.2f + MBRandom.RandomFloat * 0.2f);
				}
			}
			_addBloodToAgents.Clear();
		}
		if (!_conversationStarted)
		{
			if (!IsReadyForConversation)
			{
				return;
			}
			InitializeAfterCreation(_curConversationPartnerAgent, OtherSideConversationData.Party);
			_conversationStarted = true;
		}
		if (base.Mission.InputManager.IsGameKeyPressed(4))
		{
			Campaign.Current.ConversationManager.EndConversation();
		}
		if (_isNaval && _curConversationPartnerAgent != null && _agentHangPointShort != null && _navalConversationState != NavalConversationCameraState.SameShip)
		{
			if (ActionIndexCache.act_conversation_naval_start == _curConversationPartnerAgent.GetCurrentAction(0) || ActionIndexCache.act_conversation_naval_idle_loop == _curConversationPartnerAgent.GetCurrentAction(0))
			{
				MatrixFrame leftGlobalFrame = ((_otherPartyHeightMultiplier >= 1.76f) ? _agentHangPointTall : _agentHangPointShort).GetGlobalFrame();
				Vec3 vec = ((_otherPartyHeightMultiplier >= 1.76f) ? _agentHangPointSecondTall : _agentHangPointSecondShort).GetGlobalFrame().origin - leftGlobalFrame.origin;
				vec.Normalize();
				Vec3 f = leftGlobalFrame.rotation.f;
				f.Normalize();
				Vec3 vec2 = Vec3.CrossProduct(f, vec);
				vec2.Normalize();
				f = Vec3.CrossProduct(vec, vec2);
				f.Normalize();
				leftGlobalFrame.rotation.f = f;
				leftGlobalFrame.rotation.u = -vec;
				leftGlobalFrame.rotation.s = -vec2;
				_curConversationPartnerAgent.SetHandInverseKinematicsFrame(in leftGlobalFrame, MatrixFrame.Identity);
			}
			else
			{
				_curConversationPartnerAgent.ClearHandInverseKinematics();
			}
		}
		if (IsMultiAgentConversation && (ActionIndexCache.act_conversation_naval_start == _curConversationPartnerAgent.GetCurrentAction(0) || ActionIndexCache.act_conversation_naval_idle_loop == _curConversationPartnerAgent.GetCurrentAction(0)))
		{
			_curConversationPartnerAgent.SetCurrentActionProgress(0, 1f);
			_curConversationPartnerAgent.SetActionChannel(0, in ActionIndexCache.act_conversation_normal_loop, ignorePriority: false, (AnimFlags)0uL);
		}
		if (!Campaign.Current.ConversationManager.IsConversationInProgress)
		{
			base.Mission.EndMission();
		}
	}

	private void SpawnBodyguards(bool isDefenderSide)
	{
		int num = 2;
		ConversationCharacterData otherSideConversationData = OtherSideConversationData;
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
		foreach (TroopRosterElement item in memberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero && otherSideConversationData.Character != item.Character && !list.Contains(item.Character) && item.Character.HeroObject.IsWounded && !item.Character.IsPlayerCharacter)
			{
				list.Add(item.Character);
			}
		}
		while (list.Count < num)
		{
			foreach (TroopRosterElement item2 in from k in memberRoster.GetTroopRoster()
				orderby k.Character.Level descending
				select k)
			{
				if ((!otherSideConversationData.Character.IsHero || otherSideConversationData.Character != item2.Character) && !item2.Character.IsPlayerCharacter)
				{
					list.Add(item2.Character);
				}
				if (list.Count == num)
				{
					break;
				}
			}
		}
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
		for (int num3 = 0; num3 < num; num3++)
		{
			int index = new Random().Next(0, list.Count);
			int index2 = MBRandom.RandomInt(0, list2.Count);
			SpawnCharacter(list[index], otherSideConversationData, GetBodyguardSpawnFrame(list[index].HasMount(), isDefenderSide), list2[index2]);
			list2.RemoveAt(index2);
			list.RemoveAt(index);
		}
	}

	private void SpawnCharacter(CharacterObject character, ConversationCharacterData characterData, MatrixFrame initialFrame, in ActionIndexCache conversationAction)
	{
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
		AgentBuildData agentBuildData = new AgentBuildData(character).TroopOrigin(new SimpleAgentOrigin(character)).Team(base.Mission.PlayerTeam).Monster(monsterWithSuffix)
			.InitialPosition(in initialFrame.origin)
			.InitialDirection(initialFrame.rotation.f.AsVec2.Normalized())
			.NoHorses(character.HasMount())
			.NoWeapons(characterData.NoWeapon)
			.CivilianEquipment((character == CharacterObject.PlayerCharacter) ? _isCivilianEquipmentRequiredForLeader : _isCivilianEquipmentRequiredForBodyGuards)
			.SetPrepareImmediately();
		if (characterData.Party?.LeaderHero?.ClanBanner != null)
		{
			agentBuildData.Banner(characterData.Party.LeaderHero.ClanBanner);
		}
		else if (characterData.Party != null && characterData.Party?.MapFaction != null)
		{
			agentBuildData.Banner(characterData.Party.MapFaction.Banner);
		}
		if (characterData.Party != null)
		{
			agentBuildData.ClothingColor1(characterData.Party.MapFaction.Color).ClothingColor2(characterData.Party.MapFaction.Color2);
		}
		if (characterData.Character == CharacterObject.PlayerCharacter)
		{
			agentBuildData.Controller(AgentControllerType.Player);
		}
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
		agent.SetLookAgent(Agent.Main);
		AnimationSystemData animationSystemData = agentBuildData.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agentBuildData.AgentMonster, agentBuildData.AgentIsFemale, "_poses"), character.GetStepSize(), hasClippingPlane: false);
		agent.SetActionSet(ref animationSystemData);
		if (characterData.Character == CharacterObject.PlayerCharacter)
		{
			agent.AgentVisuals.GetSkeleton().TickAnimationsAndForceUpdate(0.1f, initialFrame, tickAnimsForChildren: true);
		}
		if (characterData.SpawnedAfterFight)
		{
			_addBloodToAgents.Add(agent);
		}
		else if (agent.MountAgent == null)
		{
			agent.SetActionChannel(0, in conversationAction, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, 0f, 0.4f, MBRandom.RandomFloat * 0.8f);
		}
	}

	private MatrixFrame GetBodyguardSpawnFrame(bool spawnWithHorse, bool isDefenderSide)
	{
		MatrixFrame result = MatrixFrame.Identity;
		foreach (GameEntity child in _conversationSet.GetChildren())
		{
			if (!isDefenderSide)
			{
				if (spawnWithHorse && child.HasTag("player_bodyguard_cavalry_spawn") && !_usedSpawnPoints.Contains(child))
				{
					_usedSpawnPoints.Add(child);
					result = child.GetGlobalFrame();
					break;
				}
				if (child.HasTag("player_bodyguard_infantry_spawn") && !_usedSpawnPoints.Contains(child))
				{
					_usedSpawnPoints.Add(child);
					result = child.GetGlobalFrame();
					break;
				}
			}
			else
			{
				if (spawnWithHorse && child.HasTag("opponent_bodyguard_cavalry_spawn") && !_usedSpawnPoints.Contains(child))
				{
					_usedSpawnPoints.Add(child);
					result = child.GetGlobalFrame();
					break;
				}
				if (child.HasTag("opponent_bodyguard_infantry_spawn") && !_usedSpawnPoints.Contains(child))
				{
					_usedSpawnPoints.Add(child);
					result = child.GetGlobalFrame();
					break;
				}
			}
		}
		result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return result;
	}

	protected override void OnEndMission()
	{
		_conversationSet = null;
		base.Mission.CameraIsFirstPerson = _realCameraController;
	}

	private string GetNavalConversationCameraTag(PartyBase encounteredParty)
	{
		string result;
		if (encounteredParty == null || encounteredParty == PartyBase.MainParty)
		{
			result = "custom_camera_same_ship";
			_navalConversationState = NavalConversationCameraState.SameShip;
			goto IL_00c4;
		}
		ShipHull.ShipType shipType;
		ShipHull.ShipType shipType2;
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			MobileParty mobileParty = encounteredParty.MobileParty;
			if (mobileParty != null && mobileParty.IsCurrentlyAtSea)
			{
				shipType = ((PartyBase.MainParty.Ships.Count <= 0) ? ShipHull.ShipType.Medium : PartyBase.MainParty.FlagShip.ShipHull.Type);
				shipType2 = (encounteredParty.Ships.IsEmpty() ? shipType : encounteredParty.FlagShip.ShipHull.Type);
				goto IL_0091;
			}
		}
		shipType = ShipHull.ShipType.Medium;
		shipType2 = ShipHull.ShipType.Medium;
		goto IL_0091;
		IL_0091:
		if (shipType < shipType2)
		{
			result = "custom_camera_lookup";
			_navalConversationState = NavalConversationCameraState.LookUp;
		}
		else if (shipType > shipType2)
		{
			result = "custom_camera_lookdown";
			_navalConversationState = NavalConversationCameraState.LookDown;
		}
		else
		{
			result = "custom_camera_level";
			_navalConversationState = NavalConversationCameraState.Level;
		}
		goto IL_00c4;
		IL_00c4:
		return result;
	}
}
