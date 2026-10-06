using System.Collections.Generic;
using System.Linq;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline;

public class NavalStorylineAlleyFightMissionController : MissionLogic
{
	private const string EnemyTroopStringId = "naval_storyline_alley_fight_enemy";

	private const float SpeechDelayAfterCombatDuration = 1.5f;

	private const float BanterNotificationRepeatDuration = 12f;

	private const string GunnarEquipmentId = "item_set_gangradir_alleyfight";

	private bool _isMissionInitialized;

	private bool _isMissionFailed;

	private List<GameEntity> _entities = new List<GameEntity>();

	private Agent _gunnarAgent;

	private bool _willGunnarBecomeVulnerable;

	private float _gunnarInvulnerabilityTimer;

	private float _gunnarInvulnerabilityDurationAfterCinematic = 18f;

	private bool _shouldShowEndNotification;

	private bool _shouldShowBanterNotifications = true;

	private float _banterNotificationTimer = 12f;

	private int _banterLineIndex;

	private List<TextObject> _banterLines = new List<TextObject>
	{
		new TextObject("{=kDQXVwSJ}Hey old man! We want a word."),
		new TextObject("{=J3eXaYJs}Don't worry - we just want to talk to you."),
		new TextObject("{=q7cvwXab}We're not going to hurt you."),
		new TextObject("{=aneZwbHJ}Easy there, grandpa. Hand off your sword hilt.")
	};

	private bool _shoulStartOutroConversation;

	private float _speechDelayTimer;

	private bool _isEnemyAttackToPlayerQueued;

	private float _enemyAttackToPlayerTimer;

	private float _enemyAttackToPlayerDuration = 3f;

	private Agent _directedEnemyAgent;

	public override void EarlyStart()
	{
		base.EarlyStart();
		base.Mission.Teams.Add(BattleSideEnum.Defender, Clan.PlayerClan.Color, Clan.PlayerClan.Color2, Clan.PlayerClan.Banner);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, NavalStorylineData.CorsairBanner.GetPrimaryColor(), NavalStorylineData.CorsairBanner.GetSecondaryColor(), NavalStorylineData.CorsairBanner);
		base.Mission.PlayerTeam = base.Mission.DefenderTeam;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			_isMissionInitialized = true;
			UpdateEntityReferences();
			Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
			Formation formation = team.GetFormation(FormationClass.Infantry);
			Mission.GetTeam(TeamSideEnum.EnemyTeam);
			SpawnPlayer();
			GameEntity spawnPoint = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_gangradir"));
			SpawnGunnar(spawnPoint);
			SpawnEnemyTroop("sp_thug_1", "act_argue_trio_right");
			SpawnEnemyTroop("sp_thug_2", "act_argue_trio_middle_2");
			SpawnEnemyTroop("sp_thug_3", "act_argue_trio_left");
			team.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
			formation.PlayerOwner = Agent.Main;
			base.Mission.OnInitialSpawnCompleted();
		}
		if (_willGunnarBecomeVulnerable)
		{
			_gunnarInvulnerabilityTimer += dt;
			if (_gunnarInvulnerabilityTimer >= _gunnarInvulnerabilityDurationAfterCinematic)
			{
				_gunnarAgent.ToggleInvulnerable();
				_willGunnarBecomeVulnerable = false;
			}
		}
		if (_shoulStartOutroConversation)
		{
			_speechDelayTimer += dt;
			if (_speechDelayTimer >= 1.5f)
			{
				_shoulStartOutroConversation = false;
				TriggerCombatEnd();
			}
		}
		if (_shouldShowEndNotification)
		{
			GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4)));
			ShowNotification(GameTexts.FindText("str_battle_won_press_tab_to_view_results"), null);
			_shouldShowEndNotification = false;
		}
		if (_shouldShowBanterNotifications)
		{
			_banterNotificationTimer += dt;
			if (_banterNotificationTimer > 12f)
			{
				_banterNotificationTimer = 0f;
				TextObject text = _banterLines[_banterLineIndex];
				ShowNotification(text, base.Mission.Teams.PlayerEnemy.ActiveAgents.GetRandomElement().Character);
				_banterLineIndex = (_banterLineIndex + 1) % _banterLines.Count;
			}
		}
		if (_isEnemyAttackToPlayerQueued)
		{
			_enemyAttackToPlayerTimer += dt;
			if (_enemyAttackToPlayerTimer >= _enemyAttackToPlayerDuration)
			{
				_isEnemyAttackToPlayerQueued = false;
				Agent randomElement = base.Mission.PlayerEnemyTeam.ActiveAgents.GetRandomElement();
				if (randomElement != null)
				{
					_directedEnemyAgent = randomElement;
				}
			}
		}
		if (_directedEnemyAgent != null && _directedEnemyAgent.IsActive())
		{
			if (_directedEnemyAgent.Position.DistanceSquared(Agent.Main.Position) <= 1f)
			{
				_directedEnemyAgent.ClearTargetFrame();
				_directedEnemyAgent = null;
			}
			else
			{
				WorldPosition position = new WorldPosition(base.Mission.Scene, Agent.Main.Position);
				_directedEnemyAgent.SetScriptedPosition(ref position, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
		}
	}

	private void UpdateEntityReferences()
	{
		base.Mission.Scene.GetEntities(ref _entities);
	}

	public void OnCinematicStarted()
	{
		_shouldShowBanterNotifications = false;
		Mission.Current.SetMissionMode(MissionMode.CutScene, atStart: true);
	}

	public void StartFight()
	{
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: true);
		_willGunnarBecomeVulnerable = true;
		foreach (Agent activeAgent in base.Mission.Teams.PlayerEnemy.ActiveAgents)
		{
			activeAgent.ToggleInvulnerable();
		}
		OnTeamAgentsShouldAttack(base.Mission.Teams.Player);
		OnTeamAgentsShouldAttack(base.Mission.Teams.PlayerEnemy);
		base.Mission.PlayerTeam.MasterOrderController.SelectAllFormations();
		base.Mission.PlayerTeam.MasterOrderController.SetOrder(OrderType.Charge);
		_isEnemyAttackToPlayerQueued = true;
		ShowNotification(new TextObject("{=6zHJnnil}Hey you! Stranger! Would you like to help an old man drive off a few stray dogs here?"), NavalStorylineData.Gunnar.CharacterObject);
	}

	private void SpawnPlayer()
	{
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_player"));
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
		AgentBuildData agentBuildData = new AgentBuildData(Hero.MainHero.CharacterObject).TroopOrigin(new SimpleAgentOrigin(Hero.MainHero.CharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false)
			.Formation(formation);
		Mission.Current.SpawnAgent(agentBuildData).Controller = AgentControllerType.Player;
	}

	private void SpawnGunnar(GameEntity spawnPoint)
	{
		MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("item_set_gangradir_alleyfight");
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, NavalStorylineData.Gunnar.CharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(spawnPoint.GlobalPosition)
			.InitialDirection(spawnPoint.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false)
			.Equipment(mBEquipmentRoster.DefaultEquipment);
		_gunnarAgent = Mission.Current.SpawnAgent(agentBuildData);
		MBActionSet actionSet = MBGlobals.GetActionSet("as_human_hideout_bandit");
		AnimationSystemData animationSystemData = agentBuildData.AgentMonster.FillAnimationSystemData(actionSet, NavalStorylineData.Gunnar.CharacterObject.GetStepSize(), hasClippingPlane: false);
		_gunnarAgent.SetActionSet(ref animationSystemData);
		_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_argue_trio_middle, ignorePriority: false, (AnimFlags)0uL);
		UsableMachine firstScriptOfType = spawnPoint.GetFirstScriptOfType<UsableMachine>();
		if (firstScriptOfType != null)
		{
			StandingPoint usedObject = firstScriptOfType.StandingPoints.FirstOrDefault();
			_gunnarAgent.UseGameObject(usedObject);
		}
		_gunnarAgent.ToggleInvulnerable();
	}

	private void SpawnEnemyTroop(string spawnPointId, string animationId)
	{
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("naval_storyline_alley_fight_enemy");
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag(spawnPointId));
		Vec3 position = gameEntity.GlobalPosition;
		Vec2 direction = gameEntity.GetGlobalFrame().rotation.f.AsVec2;
		AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(in position)
			.InitialDirection(in direction)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false)
			.Banner(NavalStorylineData.CorsairBanner);
		Agent agent = Mission.Current.SpawnAgent(agentBuildData);
		AnimationSystemData animationSystemData = MonsterExtensions.FillAnimationSystemData(actionSet: MBGlobals.GetActionSet("as_human_hideout_bandit"), monster: agentBuildData.AgentMonster, stepSize: characterObject.GetStepSize(), hasClippingPlane: false);
		agent.SetActionSet(ref animationSystemData);
		agent.SetActionChannel(0, ActionIndexCache.Create(animationId), ignorePriority: false, (AnimFlags)0uL);
		StandingPoint usedObject = gameEntity.GetFirstScriptOfType<UsableMachine>().StandingPoints.FirstOrDefault();
		agent.UseGameObject(usedObject);
		for (int num = 0; num < 50; num++)
		{
			agent.TickActionChannels(0.1f);
		}
		agent.ToggleInvulnerable();
	}

	private void OnTeamAgentsShouldAttack(Team team)
	{
		foreach (Agent activeAgent in team.ActiveAgents)
		{
			AgentFlag agentFlags = activeAgent.GetAgentFlags();
			activeAgent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
			CampaignAgentComponent component = activeAgent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component.AgentNavigator;
			if (agentNavigator == null)
			{
				agentNavigator = component.CreateAgentNavigator();
				agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
			}
			agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().SetScriptedBehavior<FightBehavior>();
			activeAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			if (activeAgent.IsUsingGameObject)
			{
				activeAgent.StopUsingGameObject();
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (base.Mission.PlayerEnemyTeam.ActiveAgents.IsEmpty())
		{
			OnEnemyTeamDefeated();
		}
		else if (base.Mission.PlayerTeam.ActiveAgents.IsEmpty() || affectedAgent.IsMainAgent)
		{
			OnPlayerTeamDefeated();
			if (affectedAgent.IsMainAgent)
			{
				base.Mission.EndMission();
			}
		}
	}

	private void OnEnemyTeamDefeated()
	{
		_shoulStartOutroConversation = true;
	}

	private void TriggerCombatEnd()
	{
		_shouldShowBanterNotifications = false;
		AgentNavigator agentNavigator = (_gunnarAgent?.GetComponent<CampaignAgentComponent>())?.AgentNavigator;
		if (agentNavigator != null)
		{
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (behaviorGroup != null)
			{
				behaviorGroup.IsActive = false;
			}
		}
		base.Mission.GetMissionBehavior<NavalStorylineAlleyFightCinematicController>().OnFightEnded();
	}

	public void SetupConversation()
	{
		if (Agent.Main == null || !Agent.Main.IsActive())
		{
			SpawnPlayer();
		}
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("conversation_ally"));
		GameEntity gameEntity2 = _entities.FirstOrDefault((GameEntity t) => t.HasTag("conversation_player"));
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			SpawnGunnar(gameEntity);
		}
		if (gameEntity != null && gameEntity2 != null)
		{
			_gunnarAgent.TeleportToPosition(gameEntity.GlobalPosition);
			_gunnarAgent.SetTargetPosition(gameEntity.GlobalPosition.AsVec2);
			Agent.Main.TeleportToPosition(gameEntity2.GlobalPosition);
			Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			Agent.Main.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			Agent.Main.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			_gunnarAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			_gunnarAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			_gunnarAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			Vec3 vec = Agent.Main.Position - _gunnarAgent.Position;
			base.Mission.GetMissionBehavior<NavalStorylineAlleyFightCinematicController>().OnConversationSetup(-vec);
			_gunnarAgent.SetMovementDirection(vec.AsVec2.Normalized());
			_gunnarAgent.Controller = AgentControllerType.None;
		}
	}

	public void StartPostFightConversation()
	{
		Campaign.Current.ConversationManager.SetupAndStartMissionConversation(_gunnarAgent, base.Mission.MainAgent, setActionsInstantly: true);
		Mission.Current.SetMissionMode(MissionMode.Conversation, atStart: true);
	}

	private void ShowNotification(TextObject text, BasicCharacterObject speaker)
	{
		MBInformationManager.AddQuickInformation(text, 0, speaker, NavalStorylineData.Gunnar.CharacterObject.FirstCivilianEquipment);
	}

	private void OnPlayerTeamDefeated()
	{
		_isMissionFailed = true;
		_shouldShowEndNotification = true;
	}

	public CharacterObject GetEnemyCharacterObject()
	{
		return Campaign.Current.ObjectManager.GetObject<CharacterObject>("naval_storyline_alley_fight_enemy");
	}

	public void OnConversationEnded()
	{
		Mission.Current.EndMission();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		bool result = false;
		if (_isMissionFailed)
		{
			missionResult = MissionResult.CreateDefeated(base.Mission);
			result = true;
		}
		return result;
	}

	protected override void OnEndMission()
	{
		CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
	}
}
