using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.CampaignBehaviors;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns
{
	// Token: 0x0200008E RID: 142
	public class PrisonBreakMissionController : MissionLogic
	{
		// Token: 0x06000588 RID: 1416 RVA: 0x00024694 File Offset: 0x00022894
		public PrisonBreakMissionController(CharacterObject prisonerCharacter)
		{
			this._prisonerCharacter = prisonerCharacter;
			this._isFirstPhase = true;
			this._isPrisonerFollowing = false;
			this._aliveGuardAgents = new List<Agent>();
			this._killedGuardsInTheFirstPhase = new List<CharacterObject>();
			this._prisonBreakCampaignBehavior = Campaign.Current.GetCampaignBehavior<PrisonBreakCampaignBehavior>();
		}

		// Token: 0x06000589 RID: 1417 RVA: 0x000246E2 File Offset: 0x000228E2
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = false;
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x000246F8 File Offset: 0x000228F8
		public override void OnBehaviorInitialize()
		{
			Game.Current.EventManager.RegisterEvent<OnStealthMissionCounterFailedEvent>(new Action<OnStealthMissionCounterFailedEvent>(this.OnStealthMissionCounterFailed));
			Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
			base.Mission.IsAgentInteractionAllowed_AdditionalCondition += this.IsAgentInteractionAllowed_AdditionalCondition;
		}

		// Token: 0x0600058B RID: 1419 RVA: 0x00024752 File Offset: 0x00022952
		private void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent missionEvent)
		{
			if (missionEvent.LocationCharacter.Character == this._prisonerCharacter)
			{
				this._prisonerAgent = missionEvent.Agent;
				this._prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<WalkingBehavior>();
			}
		}

		// Token: 0x0600058C RID: 1420 RVA: 0x00024790 File Offset: 0x00022990
		public override void AfterStart()
		{
			base.Mission.SetMissionMode(MissionMode.Stealth, true);
			base.Mission.IsInventoryAccessible = false;
			base.Mission.IsQuestScreenAccessible = false;
			base.Mission.IsKingdomWindowAccessible = false;
			foreach (UsableMachine usableMachine in base.Mission.GetMissionBehavior<MissionAgentHandler>().TownPassageProps)
			{
				usableMachine.Deactivate();
			}
			this._failCounterMissionLogic = Mission.Current.GetMissionBehavior<StealthFailCounterMissionLogic>();
			this._failCounterMissionLogic.FailCounterSeconds = 15f;
			base.Mission.AllowAiTicking = false;
			SandBoxHelpers.MissionHelper.SpawnPlayer(false, true, false, false, "");
			base.Mission.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters(null);
			base.Mission.AllowAiTicking = true;
			Agent.Main.SetClothingColor1(4281281067U);
			Agent.Main.SetClothingColor2(4281281067U);
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.StealthEquipment);
			this.PreparePrisonAgent();
			Agent.Main.Formation = new Formation(Mission.Current.Teams.Player, 0);
			base.Mission.FocusableObjectInformationProvider.AddInfoCallback(new GetFocusableObjectInteractionTextsDelegate(this.GetFocusableObjectInteractionInfoTexts));
			TextObject textObject = new TextObject("{=QYFuj7H7}Find and talk to {PRISONER_NAME}, Do not alert the guards!", null);
			textObject.SetTextVariable("PRISONER_NAME", this._prisonerCharacter.Name);
			MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
			this._aliveGuardAgents = base.Mission.Agents.Where(delegate(Agent x)
			{
				CharacterObject characterObject;
				return (characterObject = x.Character as CharacterObject) != null && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.PrisonGuard);
			}).ToList<Agent>();
		}

		// Token: 0x0600058D RID: 1421 RVA: 0x00024950 File Offset: 0x00022B50
		private void SwitchPrisonerFollowingState(bool forceFollow = false)
		{
			this._isPrisonerFollowing = forceFollow || !this._isPrisonerFollowing;
			MBTextManager.SetTextVariable("IS_PRISONER_FOLLOWING", this._isPrisonerFollowing ? 1 : 0);
			FollowAgentBehavior behavior = this._prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().GetBehavior<FollowAgentBehavior>();
			if (this._isPrisonerFollowing)
			{
				this._prisonerAgent.SetCrouchMode(false);
				behavior.SetTargetAgent(Agent.Main);
				AgentFlag agentFlags = this._prisonerAgent.GetAgentFlags();
				this._prisonerAgent.SetAgentFlags(agentFlags & ~AgentFlag.CanGetAlarmed);
			}
			else
			{
				behavior.SetTargetAgent(null);
				this._prisonerAgent.SetCrouchMode(true);
			}
			this._prisonerAgent.SetAlarmState(Agent.AIStateFlag.None);
		}

		// Token: 0x0600058E RID: 1422 RVA: 0x00024A04 File Offset: 0x00022C04
		private void CheckPrisonerSwitchToAlarmState()
		{
			foreach (Agent agent in this._aliveGuardAgents)
			{
				if (this._prisonerAgent.Position.DistanceSquared(agent.Position) < 3f && agent.IsAlarmed())
				{
					AgentFlag agentFlags = this._prisonerAgent.GetAgentFlags();
					this._prisonerAgent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
					this._prisonerAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
				}
			}
		}

		// Token: 0x0600058F RID: 1423 RVA: 0x00024AA4 File Offset: 0x00022CA4
		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			if (userAgent == Agent.Main && agent == this._prisonerAgent)
			{
				if (this._aliveGuardAgents.All((Agent x) => !x.IsAlarmed()))
				{
					if (this._isFirstPhase)
					{
						this.SpawnPhase2Guards();
						this.SwitchToPhase2();
						this.SwitchPrisonerFollowingState(false);
						return;
					}
					this.SwitchPrisonerFollowingState(false);
				}
			}
		}

		// Token: 0x06000590 RID: 1424 RVA: 0x00024B14 File Offset: 0x00022D14
		private void SpawnPhase2Guards()
		{
			Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
			foreach (CharacterObject characterObject in this._killedGuardsInTheFirstPhase)
			{
				LocationCharacter locationCharacter = this._prisonBreakCampaignBehavior.CreatePrisonBreakGuard();
				locationCharacter.SpecialTargetTag = "prison_break_reinforcement_point";
				LocationComplex.Current.ChangeLocation(locationCharacter, null, locationWithId);
				this._aliveGuardAgents.Add(base.Mission.Agents.Last<Agent>());
			}
		}

		// Token: 0x06000591 RID: 1425 RVA: 0x00024BB0 File Offset: 0x00022DB0
		private void SwitchToPhase2()
		{
			this._isFirstPhase = false;
			MBInformationManager.AddQuickInformation(new TextObject("{=ap5pYDR7}Let's get out of here!", null), 0, this._prisonerCharacter, null, "");
			MBInformationManager.AddQuickInformation(new TextObject("{=S3MaaRQH}Guards know that something is up, be ready to fight!", null), 0, null, null, "");
			this._prisonerAgent.SetTeam(Mission.Current.PlayerTeam, true);
			DailyBehaviorGroup behaviorGroup = this._prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			followAgentBehavior.SetTargetAgent(Agent.Main);
			AgentFlag agentFlags = this._prisonerAgent.GetAgentFlags();
			this._prisonerAgent.SetAgentFlags(agentFlags & ~AgentFlag.CanGetAlarmed);
			this._prisonerAgent.WieldNextWeapon(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
			foreach (Agent agent in this._aliveGuardAgents)
			{
				AlarmedBehaviorGroup behaviorGroup2 = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
				float addedAlarmFactor = 2f;
				WorldPosition worldPosition = agent.GetWorldPosition();
				behaviorGroup2.AddAlarmFactor(addedAlarmFactor, worldPosition);
				agent.SetAlarmState(Agent.AIStateFlag.PatrollingCautious);
			}
			this._failCounterMissionLogic.IsActive = false;
			this.UpdateDoorPermission();
		}

		// Token: 0x06000592 RID: 1426 RVA: 0x00024CE4 File Offset: 0x00022EE4
		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			return userAgent == Agent.Main && otherAgent == this._prisonerAgent;
		}

		// Token: 0x06000593 RID: 1427 RVA: 0x00024CFC File Offset: 0x00022EFC
		private void GetFocusableObjectInteractionInfoTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
		{
			focusableObjectInformation = default(FocusableObjectInformation);
			Agent agent;
			if (requesterAgent.IsMainAgent && (agent = focusableObject as Agent) != null && agent == this._prisonerAgent)
			{
				focusableObjectInformation.PrimaryInteractionText = agent.Character.Name;
				MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13), 1f), false);
				focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action", null);
				focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use", null));
				focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", (!this._isFirstPhase) ? GameTexts.FindText("str_ui_prison_break", null) : GameTexts.FindText("str_ui_prison_break_prisoner_greeting", null));
				focusableObjectInformation.IsActive = true;
				return;
			}
			focusableObjectInformation.IsActive = false;
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x00024DDC File Offset: 0x00022FDC
		private void PreparePrisonAgent()
		{
			this._prisonerAgent.Health = this._prisonerAgent.HealthLimit;
			this._prisonerAgent.Defensiveness = 2f;
			AgentNavigator agentNavigator = this._prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.RemoveBehaviorGroup<AlarmedBehaviorGroup>();
			agentNavigator.SpecialTargetTag = "sp_prison_break_prisoner";
			ItemObject item = (from x in Items.All
				where x.IsCraftedWeapon && x.Type == ItemObject.ItemTypeEnum.OneHandedWeapon && x.WeaponComponent.GetItemType() == ItemObject.ItemTypeEnum.OneHandedWeapon && x.IsCivilian
				select x).MinBy((ItemObject x) => x.Value);
			MissionWeapon missionWeapon = new MissionWeapon(item, null, this._prisonerCharacter.HeroObject.ClanBanner);
			this._prisonerAgent.EquipWeaponWithNewEntity(EquipmentIndex.WeaponItemBeginSlot, ref missionWeapon);
			this._prisonerAgent.SpawnEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(missionWeapon.Item, null, null, false));
			this._prisonerAgent.SetCrouchMode(true);
			this._prisonerAgent.SetTeam(null, false);
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x00024EE0 File Offset: 0x000230E0
		public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
		{
			this.UpdateDoorPermission();
			if (agent == this._prisonerAgent && !this._prisonerAgent.IsAlarmed())
			{
				AgentFlag agentFlags = this._prisonerAgent.GetAgentFlags();
				this._prisonerAgent.SetAgentFlags(agentFlags & ~AgentFlag.CanGetAlarmed);
			}
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00024F28 File Offset: 0x00023128
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (this._prisonerAgent == affectedAgent)
			{
				this._prisonerAgent = null;
			}
			if (this._aliveGuardAgents.Contains(affectedAgent))
			{
				if (this._isFirstPhase)
				{
					this._killedGuardsInTheFirstPhase.Add((CharacterObject)affectedAgent.Character);
				}
				this._aliveGuardAgents.Remove(affectedAgent);
			}
			this.UpdateDoorPermission();
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x00024F84 File Offset: 0x00023184
		public override InquiryData OnEndMissionRequest(out bool canLeave)
		{
			canLeave = Agent.Main == null || !Agent.Main.IsActive();
			if (!canLeave)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat", null), 0, null, null, "");
			}
			return null;
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x00024FBC File Offset: 0x000231BC
		public void OnStealthMissionCounterFailed(OnStealthMissionCounterFailedEvent obj)
		{
			this._missionFailedByStealthCounter = true;
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x00024FC8 File Offset: 0x000231C8
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<OnStealthMissionCounterFailedEvent>(new Action<OnStealthMissionCounterFailedEvent>(this.OnStealthMissionCounterFailed));
			Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
			if (PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.Any((AccompanyingCharacter x) => x.LocationCharacter.Character == this._prisonerCharacter))
			{
				PlayerEncounter.LocationEncounter.RemoveAccompanyingCharacter(this._prisonerCharacter.HeroObject);
			}
			if (this._missionFailedByStealthCounter)
			{
				GameMenu.SwitchToMenu("settlement_prison_break_fail_player_unconscious");
			}
			else if (Agent.Main == null || !Agent.Main.IsActive())
			{
				GameMenu.SwitchToMenu("settlement_prison_break_fail_player_unconscious");
			}
			else if (this._prisonerAgent == null || !this._prisonerAgent.IsActive())
			{
				GameMenu.SwitchToMenu("settlement_prison_break_fail_prisoner_unconscious");
			}
			else
			{
				GameMenu.SwitchToMenu("settlement_prison_break_success");
			}
			Campaign.Current.GameMenuManager.NextLocation = null;
			Campaign.Current.GameMenuManager.PreviousLocation = null;
			base.Mission.IsAgentInteractionAllowed_AdditionalCondition -= this.IsAgentInteractionAllowed_AdditionalCondition;
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x000250D4 File Offset: 0x000232D4
		public override void OnMissionTick(float dt)
		{
			if (Agent.Main != null && this._prisonerAgent != null)
			{
				bool isPrisonerNear = this._isPrisonerNear;
				this._isPrisonerNear = Agent.Main.VisualPosition.DistanceSquared(this._prisonerAgent.VisualPosition) < 25f;
				if (isPrisonerNear != this._isPrisonerNear)
				{
					this.UpdateDoorPermission();
				}
			}
			if (this._prisonerAgent == null)
			{
				if (this._aliveGuardAgents.All((Agent x) => x.IsAlarmStateNormal()))
				{
					this.ShowMissionFailedPopup();
				}
			}
			if (this._prisonerAgent != null)
			{
				this.CheckPrisonerSwitchToAlarmState();
			}
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x00025178 File Offset: 0x00023378
		private void ShowMissionFailedPopup()
		{
			object obj = new TextObject("{=wQbfWNZO}Mission Failed!", null);
			TextObject textObject = new TextObject("{=KfrybSrr}You made your way out but {PRISONER.NAME} was badly wounded during the escape. You had no choice but to leave {?PRISONER.GENDER}her{?}him{\\?} behind.", null);
			textObject.SetCharacterProperties("PRISONER", this._prisonerCharacter, false);
			TextObject textObject2 = new TextObject("{=DM6luo3c}Continue", null);
			InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, false, textObject2.ToString(), null, delegate()
			{
				Mission.Current.EndMission();
			}, null, "", 0f, null, null, null), Campaign.Current.GameMode == CampaignGameMode.Campaign, false);
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x00025214 File Offset: 0x00023414
		private void UpdateDoorPermission()
		{
			bool flag;
			if (!this._isFirstPhase && (this._isPrisonerNear || this._aliveGuardAgents.Count == 0))
			{
				flag = this._aliveGuardAgents.All((Agent x) => x.IsAlarmStateNormal());
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			foreach (UsableMachine usableMachine in base.Mission.GetMissionBehavior<MissionAgentHandler>().TownPassageProps)
			{
				if (flag2)
				{
					usableMachine.Activate();
				}
				else
				{
					usableMachine.Deactivate();
				}
			}
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x000252C8 File Offset: 0x000234C8
		private bool IsAgentInteractionAllowed_AdditionalCondition()
		{
			return true;
		}

		// Token: 0x040002DE RID: 734
		private const int PrisonerNearThreshold = 5;

		// Token: 0x040002DF RID: 735
		private const int PrisonerSwitchToAlarmedDistance = 3;

		// Token: 0x040002E0 RID: 736
		private bool _isFirstPhase;

		// Token: 0x040002E1 RID: 737
		private List<CharacterObject> _killedGuardsInTheFirstPhase;

		// Token: 0x040002E2 RID: 738
		private readonly CharacterObject _prisonerCharacter;

		// Token: 0x040002E3 RID: 739
		private Agent _prisonerAgent;

		// Token: 0x040002E4 RID: 740
		private List<Agent> _aliveGuardAgents;

		// Token: 0x040002E5 RID: 741
		private PrisonBreakCampaignBehavior _prisonBreakCampaignBehavior;

		// Token: 0x040002E6 RID: 742
		private StealthFailCounterMissionLogic _failCounterMissionLogic;

		// Token: 0x040002E7 RID: 743
		private bool _isPrisonerFollowing;

		// Token: 0x040002E8 RID: 744
		private bool _isPrisonerNear;

		// Token: 0x040002E9 RID: 745
		private bool _missionFailedByStealthCounter;
	}
}
