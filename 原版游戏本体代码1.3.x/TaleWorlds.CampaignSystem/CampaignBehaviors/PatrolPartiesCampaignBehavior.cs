using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000429 RID: 1065
	public class PatrolPartiesCampaignBehavior : CampaignBehaviorBase, IPatrolPartiesCampaignBehavior
	{
		// Token: 0x06004331 RID: 17201 RVA: 0x00145240 File Offset: 0x00143440
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChangedEvent));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreated));
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.SettlementEntered));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.MobilePartyDestroyed));
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.MobilePartyCreated));
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickParty));
			CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, new Action<Town, Building, int>(this.OnBuildingLevelChanged));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x06004332 RID: 17202 RVA: 0x0014534C File Offset: 0x0014354C
		private void OnBuildingLevelChanged(Town town, Building building, int levelChange)
		{
			if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse)
			{
				TextObject textObject;
				if (building.CurrentLevel == 1 && levelChange > 0 && this.CanSettlementSpawnNewPartyCurrently(town.Settlement, false, out textObject))
				{
					this.UpdateSettlementQueue(town.Settlement, CampaignTime.Zero);
				}
				if (town.Settlement.PatrolParty != null)
				{
					town.Settlement.PatrolParty.Party.MemberRoster.UpdateVersion();
				}
			}
		}

		// Token: 0x06004333 RID: 17203 RVA: 0x001453BC File Offset: 0x001435BC
		private void DailyTickSettlement(Settlement settlement)
		{
			TextObject textObject;
			if (this.CanSettlementSpawnNewPartyCurrently(settlement, false, out textObject))
			{
				CampaignTime campaignTime;
				if (!this._partyGenerationQueue.TryGetValue(settlement, out campaignTime))
				{
					this.UpdateSettlementQueue(settlement, CampaignTime.Now + Campaign.Current.Models.SettlementPatrolModel.GetPatrolPartySpawnDuration(settlement, false));
					return;
				}
				if (campaignTime.IsPast)
				{
					this.SpawnPatrolParty(settlement);
					return;
				}
			}
			else
			{
				this.UpdateSettlementParties(settlement);
			}
		}

		// Token: 0x06004334 RID: 17204 RVA: 0x00145428 File Offset: 0x00143628
		private void HourlyTickParty(MobileParty mobileParty)
		{
			if (mobileParty.IsPatrolParty)
			{
				if (mobileParty.CurrentSettlement == mobileParty.HomeSettlement && !mobileParty.CurrentSettlement.IsUnderSiege && mobileParty.HomeSettlement.Party.MapEvent == null)
				{
					CampaignTime campaignTime;
					this.GetLastHomeSettlementVisitTime(mobileParty, out campaignTime);
					float elapsedHoursUntilNow = campaignTime.ElapsedHoursUntilNow;
					if (MBRandom.RandomFloat < elapsedHoursUntilNow * elapsedHoursUntilNow * 0.005f && mobileParty.PartySizeRatio < 1f)
					{
						this.ReplenishParty(mobileParty);
					}
				}
				if (mobileParty.CurrentSettlement == null && mobileParty.TargetSettlement == mobileParty.HomeSettlement && mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && !mobileParty.TargetSettlement.IsUnderSiege)
				{
					mobileParty.Ai.SetInitiative(0.1f, 1f, 1f);
				}
			}
		}

		// Token: 0x06004335 RID: 17205 RVA: 0x001454EC File Offset: 0x001436EC
		private void ReplenishParty(MobileParty party)
		{
			PartyTemplateObject partyTemplateForPatrolParty = Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(party.CurrentSettlement, party.PatrolPartyComponent.IsNaval);
			TroopRoster troopRoster = Campaign.Current.Models.PartySizeLimitModel.FindAppropriateInitialRosterForMobileParty(party, partyTemplateForPatrolParty);
			party.MemberRoster.Clear();
			party.MemberRoster.Add(troopRoster);
			this.SortRoster(party);
		}

		// Token: 0x06004336 RID: 17206 RVA: 0x00145554 File Offset: 0x00143754
		private void SortRoster(MobileParty mobileParty)
		{
			mobileParty.PatrolPartyComponent.SortRoster();
		}

		// Token: 0x06004337 RID: 17207 RVA: 0x00145561 File Offset: 0x00143761
		private void OnSessionLaunched(CampaignGameStarter starter)
		{
			this.AddDialogs(starter);
		}

		// Token: 0x06004338 RID: 17208 RVA: 0x0014556C File Offset: 0x0014376C
		private void OnNewGameCreated(CampaignGameStarter starter, int index)
		{
			if (index == 50)
			{
				foreach (Town town in Town.AllFiefs)
				{
					TextObject textObject;
					if (this.CanSettlementSpawnNewPartyCurrently(town.Settlement, false, out textObject))
					{
						this.SpawnPatrolParty(town.Settlement);
					}
				}
			}
		}

		// Token: 0x06004339 RID: 17209 RVA: 0x001455D4 File Offset: 0x001437D4
		private void AddDialogs(CampaignGameStarter starter)
		{
			starter.AddDialogLine("patrol_talk_start_patrol_party", "start", "patrol_talk_start_1", "{=!}{PATROL_PARTY_GREETING}", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition), new ConversationSentence.OnConsequenceDelegate(this.patrol_talk_on_consequence), 100, null);
			starter.AddPlayerLine("patrol_talk_start_enemy_1", "patrol_talk_start_1", "patrol_talk_start_attack", "{=!}{PLAYER_ATTACK_TEXT}", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_player_is_attacker), null, 100, null, null);
			starter.AddPlayerLine("patrol_talk_start_enemy_2", "patrol_talk_start_1", "close_window", "{=5KGuQb5C}We'll see who slays whom here.", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_player_is_not_attacker), null, 100, null, null);
			starter.AddDialogLine("patrol_talk_start_enemy_attack", "patrol_talk_start_attack", "patrol_talk_start_attack_final", "{=dQfha0al}You'll answer to the {?OWNER.GENDER}lady{?}lord{\\?} of {SETTLEMENT_LINK}, then!", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_enemy_attack), null, 100, null);
			starter.AddDialogLine("patrol_talk_start_neutral_attack", "patrol_talk_start_attack", "patrol_talk_start_attack_final", "{=dQfha0al}You'll answer to the {?OWNER.GENDER}lady{?}lord{\\?} of {SETTLEMENT_LINK}, then!", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_non_enemy_attack), null, 100, null);
			starter.AddPlayerLine("patrol_talk_start_attack_final", "patrol_talk_start_attack_final", "patrol_talk_start_attack_final_response", "{=z7fFBuqt}I'll take that chance. Men, attack!", null, new ConversationSentence.OnConsequenceDelegate(this.patrol_attack_on_consequence), 100, null, null);
			starter.AddPlayerLine("patrol_talk_start_attack_final_decline", "patrol_talk_start_attack_final", "close_window", "{=uJuOTHnb}On second thought, be on your way.", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_dont_attack), new ConversationSentence.OnConsequenceDelegate(this.patrol_talk_start_enemy_leave_on_consequence), 100, null, null);
			starter.AddDialogLine("patrol_talk_start_attack_final_response", "patrol_talk_start_attack_final_response", "close_window", "{=4VfGEtuS}Curse you!", null, null, 100, null);
			starter.AddPlayerLine("patrol_talk_start_ask_hideout", "patrol_talk_start_1", "patrol_talk_start_hideout", "{=y6aBcAFF}You might know if any bandits have made their lairs around here, then?", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_hideout), null, 100, null, null);
			starter.AddDialogLine("patrol_talk_hideout", "patrol_talk_start_hideout", "start", "{=!}{HIDEOUT_TEXT}", new ConversationSentence.OnConditionDelegate(this.patrol_talk_hideout_on_condition), null, 100, null);
			starter.AddPlayerLine("patrol_talk_start_ask_security", "patrol_talk_start_1", "patrol_talk_start_security", "{=!}{SECURITY_QUESTION}", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_security_start), null, 100, null, null);
			starter.AddDialogLine("patrol_talk_security", "patrol_talk_start_security", "start", "{=!}{SECURITY_TEXT}", new ConversationSentence.OnConditionDelegate(this.patrol_talk_on_condition_security), null, 100, null);
			starter.AddPlayerLine("patrol_talk_start_leave", "patrol_talk_start_1", "close_window", "{=tqh6ydEW}Carry on, then.", new ConversationSentence.OnConditionDelegate(this.patrol_talk_start_enemy_leave_on_condition), new ConversationSentence.OnConsequenceDelegate(this.patrol_talk_start_enemy_leave_on_consequence), 100, null, null);
		}

		// Token: 0x0600433A RID: 17210 RVA: 0x0014582D File Offset: 0x00143A2D
		private bool patrol_talk_on_condition_dont_attack()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty && PlayerEncounter.PlayerIsAttacker;
		}

		// Token: 0x0600433B RID: 17211 RVA: 0x00145849 File Offset: 0x00143A49
		private bool patrol_talk_start_enemy_leave_on_condition()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty && (!MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) || PlayerEncounter.PlayerIsAttacker);
		}

		// Token: 0x0600433C RID: 17212 RVA: 0x00145882 File Offset: 0x00143A82
		private void patrol_attack_on_consequence()
		{
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
		}

		// Token: 0x0600433D RID: 17213 RVA: 0x00145898 File Offset: 0x00143A98
		private bool patrol_talk_hideout_on_condition()
		{
			TextObject text;
			if (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
			{
				text = new TextObject("{=IjcuJggo}Listen... We don't want to fight you, but that doesn't mean we should sit here bandying words and passing the time of day.", null);
			}
			else if (this.IsThereHideoutNearSettlement(MobileParty.ConversationParty.HomeSettlement))
			{
				text = new TextObject("{=H0DiZ3fk}We've heard talk that some have holed up a short distance of here. If you could find them and roust them out, you'd be doing us all a service.", null);
			}
			else if (MobileParty.ConversationParty.HomeSettlement.OwnerClan.Culture.StringId == "nord" || MobileParty.ConversationParty.HomeSettlement.OwnerClan.Culture.StringId == "battania")
			{
				text = new TextObject("{=wWYyOARQ}We've heard of none near here, the gods be praised.", null);
			}
			else
			{
				text = new TextObject("{=PI6kC7Mp}We've heard of none near here, Heaven be praised.", null);
			}
			MBTextManager.SetTextVariable("HIDEOUT_TEXT", text, false);
			return true;
		}

		// Token: 0x0600433E RID: 17214 RVA: 0x00145964 File Offset: 0x00143B64
		private bool patrol_talk_on_condition_security_start()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty && PlayerEncounter.PlayerIsAttacker)
			{
				TextObject textObject = TextObject.GetEmpty();
				if (MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					textObject = new TextObject("{=5xOINADa}What news out of {SETTLEMENT}, then? Are these waters safe?", null);
				}
				else
				{
					textObject = new TextObject("{=EVprpuUB}How are things around {SETTLEMENT}, then. Do the people feel safe?", null);
				}
				textObject.SetTextVariable("SETTLEMENT", MobileParty.ConversationParty.HomeSettlement.Name);
				MBTextManager.SetTextVariable("SECURITY_QUESTION", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600433F RID: 17215 RVA: 0x001459E4 File Offset: 0x00143BE4
		private bool patrol_talk_on_condition_security()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty)
			{
				TextObject text;
				if (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
				{
					text = new TextObject("{=IjcuJggo}Listen... We don't want to fight you, but that doesn't mean we should sit here bandying words and passing the time of day.", null);
				}
				else
				{
					Settlement homeSettlement = MobileParty.ConversationParty.HomeSettlement;
					if (homeSettlement.Town.Security <= 20f)
					{
						if (MobileParty.ConversationParty.IsCurrentlyAtSea)
						{
							text = new TextObject("{=iTNOzgah}Safe? What safety is there, in this dark age? There are so many pirates about that you can’t earn an honest living afloat, which is probably why so many turn pirate to begin with.", null);
						}
						else
						{
							text = new TextObject("{=Tudsduxi}Safe? What safety is there, in these cursed times? We do what we can, but these bandits are a plague, sent upon us for our sins. Track down one band, and a dozen more take their place.", null);
						}
					}
					else if (homeSettlement.Town.Security >= 70f)
					{
						text = new TextObject("{=pWSgSp4l}Aye, safe enough, given the times we're in.", null);
					}
					else if (MobileParty.ConversationParty.IsCurrentlyAtSea)
					{
						text = new TextObject("{=AE81KA7d}Things could be better. Fishermen and merchants come and go, but you never know when some cursed vessel full of cutthroats will set on you and take you for all you've got.", null);
					}
					else
					{
						text = new TextObject("{=sYcqstOy}Things could be better. People still go about their business, but you never know when some miscreant will set on you and take you for all you've got.", null);
					}
				}
				MBTextManager.SetTextVariable("SECURITY_TEXT", text, false);
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
			}
			return true;
		}

		// Token: 0x06004340 RID: 17216 RVA: 0x00145AEA File Offset: 0x00143CEA
		private bool patrol_talk_on_condition_hideout()
		{
			return MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty && PlayerEncounter.PlayerIsAttacker && !MobileParty.ConversationParty.PatrolPartyComponent.IsNaval;
		}

		// Token: 0x06004341 RID: 17217 RVA: 0x00145B1C File Offset: 0x00143D1C
		private bool IsThereHideoutNearSettlement(Settlement settlement)
		{
			Hideout hideout = SettlementHelper.FindNearestHideoutToSettlement(settlement, MobileParty.NavigationType.Default, null);
			float num;
			if (hideout != null && hideout.IsInfested && DistanceHelper.FindClosestDistanceFromSettlementToSettlement(hideout.Settlement, settlement, MobileParty.NavigationType.Default, out num) <= Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 0.6f)
			{
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName, false);
				return true;
			}
			foreach (Village village in settlement.BoundVillages)
			{
				if (this.IsThereHideoutNearSettlement(village.Settlement))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06004342 RID: 17218 RVA: 0x00145BC8 File Offset: 0x00143DC8
		private void patrol_talk_start_enemy_leave_on_consequence()
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x06004343 RID: 17219 RVA: 0x00145BD7 File Offset: 0x00143DD7
		private bool patrol_talk_common_condition_enemy()
		{
			return MobileParty.ConversationParty.IsPatrolParty && MobileParty.ConversationParty.MapFaction != Hero.MainHero.MapFaction;
		}

		// Token: 0x06004344 RID: 17220 RVA: 0x00145C00 File Offset: 0x00143E00
		private bool patrol_talk_on_condition_player_is_attacker()
		{
			if (this.patrol_talk_common_condition_enemy() && PlayerEncounter.PlayerIsAttacker)
			{
				TextObject text;
				if (FactionManager.IsAtWarAgainstFaction(MobileParty.ConversationParty.MapFaction, Hero.MainHero.MapFaction))
				{
					if (MobileParty.ConversationParty.IsCurrentlyAtSea)
					{
						text = new TextObject("{=SNeZ6ETU}You are a lawful enemy. Yield or die!", null);
					}
					else
					{
						text = new TextObject("{=bVVh2LR9}You are a foe in arms. Yield or die!", null);
					}
				}
				else if (MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					text = new TextObject("{=92m8r4lH}I'm afraid I just don't like the cut of your jib. Yield or die!", null);
				}
				else
				{
					text = new TextObject("{=blwPe1jQ}I'm afraid I just don't like the look of you. Yield or die!", null);
				}
				MBTextManager.SetTextVariable("PLAYER_ATTACK_TEXT", text, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004345 RID: 17221 RVA: 0x00145C9C File Offset: 0x00143E9C
		private bool patrol_talk_on_condition_player_is_not_attacker()
		{
			if (this.patrol_talk_common_condition_enemy() && !PlayerEncounter.PlayerIsAttacker)
			{
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
				StringHelpers.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.Owner.CharacterObject, null, false);
				return true;
			}
			return false;
		}

		// Token: 0x06004346 RID: 17222 RVA: 0x00145CF8 File Offset: 0x00143EF8
		private bool patrol_talk_on_condition_non_enemy_attack()
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
			StringHelpers.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.Owner.CharacterObject, null, false);
			return !FactionManager.IsAtWarAgainstFaction(MobileParty.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x06004347 RID: 17223 RVA: 0x00145D5C File Offset: 0x00143F5C
		private bool patrol_talk_on_condition_enemy_attack()
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName, false);
			StringHelpers.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.Owner.CharacterObject, null, false);
			return FactionManager.IsAtWarAgainstFaction(MobileParty.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x06004348 RID: 17224 RVA: 0x00145DBD File Offset: 0x00143FBD
		private void patrol_talk_on_consequence()
		{
			this._interactedPatrolParties[MobileParty.ConversationParty.HomeSettlement] = CampaignTime.Now;
		}

		// Token: 0x06004349 RID: 17225 RVA: 0x00145DDC File Offset: 0x00143FDC
		private bool patrol_talk_on_condition()
		{
			if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty)
			{
				TextObject textObject = TextObject.GetEmpty();
				int num = int.MaxValue;
				CampaignTime campaignTime;
				if (this._interactedPatrolParties.TryGetValue(MobileParty.ConversationParty.HomeSettlement, out campaignTime))
				{
					num = (int)campaignTime.ElapsedHoursUntilNow;
				}
				if (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
				{
					if (PlayerEncounter.PlayerIsAttacker)
					{
						if (MobileParty.ConversationParty.IsCurrentlyAtSea)
						{
							textObject = new TextObject("{=Y8tKOnlq}We're not looking for trouble with you. We're here to chase pirates, the common enemy of all law-abiding sailors.", null);
						}
						else
						{
							textObject = new TextObject("{=SJ3KYtzM}We're not looking for trouble with you. We're here to chase bandits and brigands, the common enemy of all law-abiding folk.", null);
						}
					}
					else
					{
						if (MobileParty.ConversationParty.IsCurrentlyAtSea)
						{
							textObject = new TextObject("{=yX0bOiCI}You are an enemy of the {?OWNER.GENDER}lady{?}lord{\\?} and people of {SETTLEMENT_LINK}. Give up your ship, or be slain!", null);
						}
						else
						{
							textObject = new TextObject("{=uwtzkbsX}You are an enemy of the {?OWNER.GENDER}lady{?}lord{\\?} and people of {SETTLEMENT_LINK}. Surrender, or be slain!", null);
						}
						textObject.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject, false);
					}
				}
				else if (num > CampaignTime.HoursInDay)
				{
					TextObject introText = this.GetIntroText();
					TextObject statusText = this.GetStatusText();
					textObject = new TextObject("{=!}{INTRO} {STATUS}", null);
					textObject.SetTextVariable("INTRO", introText);
					textObject.SetTextVariable("STATUS", statusText);
				}
				else if (MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					textObject = new TextObject("{=wXjmwOZK}Keep a watchful eye. You never know who’s lurking just below the horizon.", null);
				}
				else
				{
					textObject = new TextObject("{=7PgV69zl}Hope you're keeping safe. You never know who's lurking about.", null);
				}
				textObject.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
				MBTextManager.SetTextVariable("PATROL_PARTY_GREETING", textObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600434A RID: 17226 RVA: 0x00145F60 File Offset: 0x00144160
		private TextObject GetStatusText()
		{
			int num = 0;
			LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(MobileParty.ConversationParty.Position.ToVec2(), MobileParty.ConversationParty.Speed * (float)CampaignTime.HoursInDay * 0.5f);
			for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData))
			{
				if (mobileParty.IsBandit && mobileParty.IsCurrentlyAtSea == MobileParty.MainParty.IsCurrentlyAtSea)
				{
					num++;
				}
				if (num >= 5)
				{
					break;
				}
			}
			TextObject result;
			if (num >= 5)
			{
				if (MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					result = new TextObject("{=H3nSfb9e}These waters are thick with pirates, like sharks amid chum.", null);
				}
				else
				{
					result = new TextObject("{=FbnCZqHa}This place is thick with bandits and troublemakers.", null);
				}
			}
			else if (num >= 3)
			{
				if (MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					result = new TextObject("{=DMWbQBHg}There are a few pirates around here who've been giving us some trouble.", null);
				}
				else
				{
					result = new TextObject("{=yNWFgFl1}There are a few brigands around here who've been giving us some trouble.", null);
				}
			}
			else if (MobileParty.ConversationParty.IsCurrentlyAtSea)
			{
				result = new TextObject("{=eF47hqVx}Haven't heard any reports of piracy around here, but you never know when things will change.", null);
			}
			else
			{
				result = new TextObject("{=Gbav1PEd}Haven't heard any reports of bandits around here, but you never know when things will change.", null);
			}
			return result;
		}

		// Token: 0x0600434B RID: 17227 RVA: 0x00146058 File Offset: 0x00144258
		private TextObject GetIntroText()
		{
			TextObject textObject;
			if (MobileParty.ConversationParty.HomeSettlement.Owner == Hero.MainHero)
			{
				if (MobileParty.ConversationParty.IsCurrentlyAtSea)
				{
					textObject = new TextObject("{=GXOkfamn}Greetings, your {?OWNER.GENDER}ladyship{?}lordship{\\?}. We're doing our best to protect shipping out of {SETTLEMENT_LINK}.", null);
				}
				else
				{
					textObject = new TextObject("{=T4sfyeNq}Greetings, your {?OWNER.GENDER}ladyship{?}lordship{\\?}. We're doing our best to keep your lands and the people of {SETTLEMENT_LINK} safe.", null);
				}
				textObject.SetCharacterProperties("OWNER", CharacterObject.PlayerCharacter, false);
			}
			else if (MobileParty.ConversationParty.IsCurrentlyAtSea)
			{
				textObject = new TextObject("{=i2a7b9az}Greetings. We're here to keep the waters around {SETTLEMENT_LINK} safe.", null);
			}
			else
			{
				textObject = new TextObject("{=GC4coq47}Greetings. We're here to keep the lands around {SETTLEMENT_LINK} safe.", null);
			}
			textObject.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}

		// Token: 0x0600434C RID: 17228 RVA: 0x001460F8 File Offset: 0x001442F8
		private void OnSettlementOwnerChangedEvent(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement.PatrolParty != null)
			{
				this.RemoveSettlementParties(settlement);
			}
		}

		// Token: 0x0600434D RID: 17229 RVA: 0x0014610C File Offset: 0x0014430C
		private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			if (!mobileParty.IsPatrolParty || mobileParty.IsDisbanding)
			{
				return;
			}
			if (mobileParty.CurrentSettlement != null && !this.CanVisitSettlement(mobileParty, mobileParty.CurrentSettlement))
			{
				return;
			}
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) != null)
			{
				return;
			}
			if (mobileParty.CurrentSettlement != null && mobileParty.PartySizeRatio < 0.9f)
			{
				return;
			}
			if (mobileParty.PatrolPartyComponent.HomeSettlement.HasPort && mobileParty.PatrolPartyComponent.IsNaval)
			{
				this.CalculatePatrollingScoreForSettlement(mobileParty.PatrolPartyComponent.HomeSettlement, p, true);
			}
			else
			{
				this.CalculatePatrollingScoreForSettlement(mobileParty.PatrolPartyComponent.HomeSettlement, p, false);
				foreach (Village village in mobileParty.PatrolPartyComponent.HomeSettlement.BoundVillages)
				{
					this.CalculatePatrollingScoreForSettlement(village.Settlement, p, false);
				}
			}
			this.CalculateVisitHomeSettlementScore(mobileParty, p);
		}

		// Token: 0x0600434E RID: 17230 RVA: 0x00146214 File Offset: 0x00144414
		private void CalculateVisitHomeSettlementScore(MobileParty mobileParty, PartyThinkParams p)
		{
			float partySizeRatio = mobileParty.PartySizeRatio;
			if (this.CanVisitSettlement(mobileParty, mobileParty.HomeSettlement))
			{
				float num = 0.15f;
				float num2 = 0f;
				CampaignTime campaignTime;
				if (this.GetLastHomeSettlementVisitTime(mobileParty, out campaignTime))
				{
					num2 = campaignTime.ElapsedDaysUntilNow;
				}
				num += (num2 - 5f) * 0.025f;
				num += 1.1f / MathF.Max(partySizeRatio, 0.01f);
				if (num > 0.15f)
				{
					MobileParty.NavigationType navigationType;
					float num3;
					bool isFromPort;
					AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.HomeSettlement, mobileParty.HomeSettlement.HasPort && mobileParty.IsCurrentlyAtSea, out navigationType, out num3, out isFromPort);
					AIBehaviorData item = new AIBehaviorData(mobileParty.HomeSettlement, AiBehavior.GoToSettlement, navigationType, false, isFromPort, mobileParty.HomeSettlement.HasPort && mobileParty.IsCurrentlyAtSea);
					float num4;
					if (p.TryGetBehaviorScore(item, out num4))
					{
						p.SetBehaviorScore(item, num + num4);
						return;
					}
					ValueTuple<AIBehaviorData, float> valueTuple = new ValueTuple<AIBehaviorData, float>(item, num);
					p.AddBehaviorScore(valueTuple);
				}
			}
		}

		// Token: 0x0600434F RID: 17231 RVA: 0x00146308 File Offset: 0x00144508
		private bool CanVisitSettlement(MobileParty mobileParty, Settlement settlement)
		{
			return (mobileParty.HasLandNavigationCapability && !settlement.IsUnderSiege && settlement.Party.MapEvent == null) || (!mobileParty.HasLandNavigationCapability && (settlement.SiegeEvent == null || !settlement.SiegeEvent.IsBlockadeActive));
		}

		// Token: 0x06004350 RID: 17232 RVA: 0x00146356 File Offset: 0x00144556
		private float GetSettlementScoreAdjustment(Settlement settlement, bool isNavalPatrolling)
		{
			if (isNavalPatrolling)
			{
				return 0.4f;
			}
			if (settlement.IsFortification)
			{
				return 0.9f;
			}
			if (!settlement.IsVillage)
			{
				return 1f;
			}
			if (!settlement.IsUnderRaid)
			{
				return 1.2f;
			}
			return 1.5f;
		}

		// Token: 0x06004351 RID: 17233 RVA: 0x00146390 File Offset: 0x00144590
		private void CalculatePatrollingScoreForSettlement(Settlement settlement, PartyThinkParams p, bool isNavalPatrolling)
		{
			MobileParty mobilePartyOf = p.MobilePartyOf;
			MobileParty.NavigationType navigationType;
			float num;
			bool isFromPort;
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobilePartyOf, settlement, isNavalPatrolling, out navigationType, out num, out isFromPort);
			if (navigationType != MobileParty.NavigationType.None)
			{
				AIBehaviorData item = new AIBehaviorData(settlement, AiBehavior.PatrolAroundPoint, navigationType, false, isFromPort, isNavalPatrolling);
				float num2 = Campaign.Current.Models.TargetScoreCalculatingModel.CalculatePatrollingScoreForSettlement(settlement, isNavalPatrolling, mobilePartyOf);
				num2 *= this.GetSettlementScoreAdjustment(settlement, navigationType == MobileParty.NavigationType.Naval);
				num2 = MathF.Max(num2, 0.01f);
				if (1.25f + num2 > 0f)
				{
					if (!mobilePartyOf.IsCurrentlyAtSea)
					{
					}
					ValueTuple<AIBehaviorData, float> valueTuple = new ValueTuple<AIBehaviorData, float>(item, 1.25f + num2);
					p.AddBehaviorScore(valueTuple);
				}
			}
		}

		// Token: 0x06004352 RID: 17234 RVA: 0x0014642E File Offset: 0x0014462E
		private void MobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
		{
			if (party.IsPatrolParty)
			{
				this.RemoveLastVisitEntry(party);
				if (!party.PatrolPartyComponent.IsNaval)
				{
					this._interactedPatrolParties.Remove(party.HomeSettlement);
				}
			}
		}

		// Token: 0x06004353 RID: 17235 RVA: 0x0014645E File Offset: 0x0014465E
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party != null && party.IsPatrolParty && settlement == party.HomeSettlement)
			{
				this.SetLastHomeSettlementVisitTime(party, CampaignTime.Now);
			}
		}

		// Token: 0x06004354 RID: 17236 RVA: 0x00146480 File Offset: 0x00144680
		private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party != null && party.IsPatrolParty && settlement == party.HomeSettlement)
			{
				this.SetLastHomeSettlementVisitTime(party, CampaignTime.Now);
				foreach (TroopRosterElement troopRosterElement in party.PrisonRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character.HeroObject != null)
					{
						TransferPrisonerAction.Apply(troopRosterElement.Character, party.Party, settlement.Party);
					}
				}
				if (party.PrisonRoster.Count > 0)
				{
					SellPrisonersAction.ApplyForAllPrisoners(party.Party, settlement.Party);
				}
			}
		}

		// Token: 0x06004355 RID: 17237 RVA: 0x00146540 File Offset: 0x00144740
		private bool CanSettlementSpawnNewPartyCurrently(Settlement settlement, bool includeReason, out TextObject reason)
		{
			if (!Campaign.Current.Models.SettlementPatrolModel.CanSettlementHavePatrolParties(settlement, false))
			{
				reason = (includeReason ? new TextObject("{=RosQSZWa}No Guard House", null) : null);
				return false;
			}
			if (settlement.InRebelliousState)
			{
				reason = (includeReason ? new TextObject("{=UHDv0qer}Rebellious", null) : null);
				return false;
			}
			if (settlement.Town.IsUnderSiege || settlement.Party.MapEvent != null)
			{
				reason = (includeReason ? new TextObject("{=BhiOmgst}Under Siege", null) : null);
				return false;
			}
			reason = (includeReason ? TextObject.GetEmpty() : null);
			return settlement.PatrolParty == null;
		}

		// Token: 0x06004356 RID: 17238 RVA: 0x001465DC File Offset: 0x001447DC
		private void MobilePartyCreated(MobileParty party)
		{
			if (party.IsPatrolParty)
			{
				this.SetLastHomeSettlementVisitTime(party, CampaignTime.Now);
			}
		}

		// Token: 0x06004357 RID: 17239 RVA: 0x001465F4 File Offset: 0x001447F4
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_partyGenerationQueue", ref this._partyGenerationQueue);
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_lastHomeSettlementVisitTimes", ref this._lastHomeSettlementVisitTimes);
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_lastHomeSettlementVisitTimesCoastal", ref this._lastHomeSettlementVisitTimesCoastal);
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_interactedPatrolParties", ref this._interactedPatrolParties);
		}

		// Token: 0x06004358 RID: 17240 RVA: 0x00146649 File Offset: 0x00144849
		private bool GetLastHomeSettlementVisitTime(MobileParty mobileParty, out CampaignTime campaignTime)
		{
			return (mobileParty.PatrolPartyComponent.IsNaval ? this._lastHomeSettlementVisitTimesCoastal : this._lastHomeSettlementVisitTimes).TryGetValue(mobileParty.HomeSettlement, out campaignTime);
		}

		// Token: 0x06004359 RID: 17241 RVA: 0x00146672 File Offset: 0x00144872
		private void SetLastHomeSettlementVisitTime(MobileParty mobileParty, CampaignTime time)
		{
			(mobileParty.PatrolPartyComponent.IsNaval ? this._lastHomeSettlementVisitTimesCoastal : this._lastHomeSettlementVisitTimes)[mobileParty.HomeSettlement] = time;
		}

		// Token: 0x0600435A RID: 17242 RVA: 0x0014669B File Offset: 0x0014489B
		private void RemoveLastVisitEntry(MobileParty mobileParty)
		{
			(mobileParty.PatrolPartyComponent.IsNaval ? this._lastHomeSettlementVisitTimesCoastal : this._lastHomeSettlementVisitTimes).Remove(mobileParty.HomeSettlement);
		}

		// Token: 0x0600435B RID: 17243 RVA: 0x001466C4 File Offset: 0x001448C4
		private void UpdateSettlementParties(Settlement settlement)
		{
			if (!Campaign.Current.Models.SettlementPatrolModel.CanSettlementHavePatrolParties(settlement, false) && settlement.PatrolParty != null)
			{
				this.RemoveSettlementParties(settlement);
			}
		}

		// Token: 0x0600435C RID: 17244 RVA: 0x001466F0 File Offset: 0x001448F0
		private void RemoveSettlementParties(Settlement settlement)
		{
			this._partyGenerationQueue.Remove(settlement);
			settlement.PatrolParty.MobileParty.MapEventSide = null;
			if (settlement.PatrolParty.MobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, settlement.PatrolParty.MobileParty);
			}
		}

		// Token: 0x0600435D RID: 17245 RVA: 0x0014673E File Offset: 0x0014493E
		private void UpdateSettlementQueue(Settlement settlement, CampaignTime time)
		{
			this._partyGenerationQueue[settlement] = time;
		}

		// Token: 0x0600435E RID: 17246 RVA: 0x00146750 File Offset: 0x00144950
		private void SpawnPatrolParty(Settlement settlement)
		{
			this._partyGenerationQueue.Remove(settlement);
			PartyTemplateObject partyTemplateForPatrolParty = Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(settlement, false);
			CampaignVec2 position = ((partyTemplateForPatrolParty.ShipHulls.Any<ShipTemplateStack>() && settlement.HasPort && MBRandom.RandomFloat < 0.25f) ? settlement.PortPosition : settlement.GatePosition);
			PatrolPartyComponent.CreatePatrolParty("patrol_party_1", position, 8f * Campaign.Current.EstimatedAverageBanditPartySpeed, settlement, partyTemplateForPatrolParty);
		}

		// Token: 0x0600435F RID: 17247 RVA: 0x001467D0 File Offset: 0x001449D0
		public TextObject GetSettlementPatrolStatus(Settlement settlement)
		{
			TextObject textObject = TextObject.GetEmpty();
			TextObject textObject2;
			CampaignTime x;
			if (settlement.PatrolParty != null)
			{
				textObject = new TextObject("{=sUb6FHIE}{REMAINING_TROOP_COUNT}/{TOTAL_TROOP_COUNT}", null);
				textObject.SetTextVariable("REMAINING_TROOP_COUNT", settlement.PatrolParty.MobileParty.MemberRoster.TotalManCount);
				textObject.SetTextVariable("TOTAL_TROOP_COUNT", settlement.PatrolParty.MobileParty.Party.PartySizeLimit);
			}
			else if (!this.CanSettlementSpawnNewPartyCurrently(settlement, true, out textObject2))
			{
				textObject = textObject2;
			}
			else if (this._partyGenerationQueue.TryGetValue(settlement, out x))
			{
				int variable = ((x == CampaignTime.Zero) ? 1 : Math.Max((int)Math.Ceiling((double)x.RemainingDaysFromNow), 1));
				textObject = new TextObject("{=LvwUsZ9p}Ready in {DAYS} {?DAYS > 1}days{?}day{\\?}", null);
				textObject.SetTextVariable("DAYS", variable);
			}
			else
			{
				textObject = new TextObject("{=trainingPatrolParties}Training", null);
			}
			return textObject;
		}

		// Token: 0x04001318 RID: 4888
		private const float BasePatrolScore = 1.25f;

		// Token: 0x04001319 RID: 4889
		private const float VisitHomeSettlementPartySizeRatioThreshold = 5f;

		// Token: 0x0400131A RID: 4890
		private const float VisitHomeSettlementBaseScore = 0.15f;

		// Token: 0x0400131B RID: 4891
		private const float ConsiderReplenishPartySizeRatioThreshold = 0.15f;

		// Token: 0x0400131C RID: 4892
		private const float BaseReplenishmentChance = 0.005f;

		// Token: 0x0400131D RID: 4893
		private Dictionary<Settlement, CampaignTime> _partyGenerationQueue = new Dictionary<Settlement, CampaignTime>();

		// Token: 0x0400131E RID: 4894
		private Dictionary<Settlement, CampaignTime> _lastHomeSettlementVisitTimes = new Dictionary<Settlement, CampaignTime>();

		// Token: 0x0400131F RID: 4895
		private Dictionary<Settlement, CampaignTime> _lastHomeSettlementVisitTimesCoastal = new Dictionary<Settlement, CampaignTime>();

		// Token: 0x04001320 RID: 4896
		private Dictionary<Settlement, CampaignTime> _interactedPatrolParties = new Dictionary<Settlement, CampaignTime>();
	}
}
