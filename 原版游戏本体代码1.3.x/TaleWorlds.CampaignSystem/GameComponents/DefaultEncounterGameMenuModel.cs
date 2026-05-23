using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000111 RID: 273
	public class DefaultEncounterGameMenuModel : EncounterGameMenuModel
	{
		// Token: 0x060017A0 RID: 6048 RVA: 0x0006F98C File Offset: 0x0006DB8C
		public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
		{
			PartyBase encounteredPartyBase = MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty);
			joinBattle = false;
			startBattle = false;
			if (defenderParty == null)
			{
				return "camp";
			}
			if (encounteredPartyBase.IsSettlement)
			{
				Settlement settlement = encounteredPartyBase.Settlement;
				if (settlement.IsVillage)
				{
					if (encounteredPartyBase.MapEvent != null && encounteredPartyBase.MapEvent.IsRaid)
					{
						if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && encounteredPartyBase.MapEvent.AttackerSide.LeaderParty == MobileParty.MainParty.Army.LeaderParty.Party && encounteredPartyBase.MapEvent.DefenderSide.TroopCount <= 0)
						{
							joinBattle = true;
							if (!encounteredPartyBase.MapEvent.IsRaid)
							{
								return "army_wait";
							}
							return "raiding_village";
						}
						else
						{
							if ((MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && attackerParty == MobileParty.MainParty.Army.LeaderParty.Party) || (MobileParty.MainParty.CurrentSettlement == settlement && MobileParty.MainParty.MapFaction == settlement.MapFaction))
							{
								joinBattle = true;
								return "encounter";
							}
							return "join_encounter";
						}
					}
					else
					{
						if (settlement.MapFaction == MobileParty.MainParty.MapFaction && MobileParty.MainParty.Army != null && attackerParty == MobileParty.MainParty.Army.LeaderParty.Party && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
						{
							return "army_wait_at_settlement";
						}
						return "village_outside";
					}
				}
				else if (settlement.IsFortification)
				{
					if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender && settlement.Party.MapEvent == null)
					{
						return "menu_siege_strategies";
					}
					if (settlement.Party.SiegeEvent != null && ((settlement.Party.MapEvent == null && (settlement.Town.GarrisonParty == null || settlement.Town.GarrisonParty.MapEvent == null || settlement.Town.GarrisonParty.MapEvent.IsSallyOut)) || MobileParty.MainParty.MapFaction == settlement.MapFaction))
					{
						if (settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty == MobileParty.MainParty)
						{
							return "continue_siege_after_attack";
						}
						if (MobileParty.MainParty.BesiegedSettlement == null && MobileParty.MainParty.CurrentSettlement == null)
						{
							if (settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null && (settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsSiegeOutside || settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsSallyOut || settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockade))
							{
								return "join_encounter";
							}
							if (!MobileParty.MainParty.IsCurrentlyAtSea)
							{
								return "join_siege_event";
							}
							if (!settlement.Party.SiegeEvent.IsBlockadeActive || settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
							{
								return "naval_town_outside";
							}
							return "join_encounter";
						}
					}
					if (settlement.Party.MapEvent != null)
					{
						if ((MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && attackerParty == MobileParty.MainParty.Army.LeaderParty.Party) || (MobileParty.MainParty.CurrentSettlement == settlement && settlement.Party.MapEvent.CanPartyJoinBattle(PartyBase.MainParty, settlement.BattleSide)))
						{
							return "encounter";
						}
						if (!MobileParty.MainParty.IsCurrentlyAtSea)
						{
							return "join_siege_event";
						}
						if (!settlement.Party.SiegeEvent.IsBlockadeActive || settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
						{
							return "naval_town_outside";
						}
						return "join_encounter";
					}
					else
					{
						if (settlement.MapFaction == MobileParty.MainParty.MapFaction && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
						{
							return "army_wait_at_settlement";
						}
						if (settlement.IsCastle)
						{
							return "castle_outside";
						}
						if (MobileParty.MainParty.IsCurrentlyAtSea)
						{
							return "naval_town_outside";
						}
						return "town_outside";
					}
				}
				else
				{
					if (settlement.IsHideout)
					{
						return "hideout_place";
					}
					if (settlement.SettlementComponent is RetirementSettlementComponent)
					{
						return "retirement_place";
					}
					return "";
				}
			}
			else if (encounteredPartyBase.MapEvent != null)
			{
				if (MobileParty.MainParty.CurrentSettlement != null || (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty))
				{
					if ((encounteredPartyBase.MapEvent.IsBlockade || encounteredPartyBase.MapEvent.IsBlockadeSallyOut) && !Settlement.CurrentSettlement.SiegeEvent.IsBlockadeActive)
					{
						startBattle = false;
						joinBattle = false;
						return "besiegers_lift_the_blockade";
					}
					joinBattle = true;
					return "encounter";
				}
				else
				{
					if (encounteredPartyBase.SiegeEvent == null || !encounteredPartyBase.MapEvent.IsSiegeAssault)
					{
						return "join_encounter";
					}
					if (!MobileParty.MainParty.IsCurrentlyAtSea)
					{
						return "join_siege_event";
					}
					if (!encounteredPartyBase.SiegeEvent.IsBlockadeActive || encounteredPartyBase.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
					{
						return "naval_town_outside";
					}
					return "join_encounter";
				}
			}
			else
			{
				if (!encounteredPartyBase.IsMobile)
				{
					return null;
				}
				if ((encounteredPartyBase.MobileParty.IsGarrison && MobileParty.MainParty.BesiegedSettlement != null) || (MobileParty.MainParty.CurrentSettlement != null && encounteredPartyBase.MobileParty.BesiegedSettlement == MobileParty.MainParty.CurrentSettlement))
				{
					if (PlayerEncounter.Current.ForceBlockadeSallyOutAttack && !Settlement.CurrentSettlement.SiegeEvent.IsBlockadeActive)
					{
						startBattle = false;
						joinBattle = false;
						return "besiegers_lift_the_blockade";
					}
					startBattle = true;
					joinBattle = false;
					if (encounteredPartyBase.MobileParty.IsGarrison && encounteredPartyBase.MobileParty.IsTargetingPort)
					{
						return "player_blockade_got_attacked";
					}
					return "encounter";
				}
				else
				{
					if (encounteredPartyBase.SiegeEvent == null)
					{
						return "encounter_meeting";
					}
					if (!MobileParty.MainParty.IsCurrentlyAtSea)
					{
						return "join_siege_event";
					}
					if (!encounteredPartyBase.SiegeEvent.IsBlockadeActive || encounteredPartyBase.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
					{
						return "naval_town_outside";
					}
					return "join_encounter";
				}
			}
		}

		// Token: 0x060017A1 RID: 6049 RVA: 0x0006FFD8 File Offset: 0x0006E1D8
		public override string GetRaidCompleteMenu()
		{
			return "village_loot_complete";
		}

		// Token: 0x060017A2 RID: 6050 RVA: 0x0006FFE0 File Offset: 0x0006E1E0
		public override string GetNewPartyJoinMenu(MobileParty newParty)
		{
			if (!PartyBase.MainParty.MapEvent.IsRaid || PartyBase.MainParty.MapEvent.AttackerSide.LeaderParty.MapFaction == PartyBase.MainParty.MapEvent.MapEventSettlement.MapFaction)
			{
				return null;
			}
			if (MobileParty.MainParty.CurrentSettlement != null || (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty))
			{
				return "encounter";
			}
			return "join_encounter";
		}

		// Token: 0x060017A3 RID: 6051 RVA: 0x00070068 File Offset: 0x0006E268
		public override string GetGenericStateMenu()
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (PlayerEncounter.Current != null && PlayerEncounter.CurrentBattleSimulation != null)
			{
				return null;
			}
			if (Hero.MainHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.None)
			{
				return null;
			}
			if (mainParty.MapEvent != null)
			{
				return "encounter";
			}
			if (mainParty.BesiegerCamp != null)
			{
				return "menu_siege_strategies";
			}
			if (mainParty.AttachedTo == null)
			{
				if (mainParty.CurrentSettlement != null)
				{
					Settlement currentSettlement = mainParty.CurrentSettlement;
					if (currentSettlement.IsFortification)
					{
						if (currentSettlement.Party.SiegeEvent != null && ((currentSettlement.Party.MapEvent == null && (currentSettlement.Town.GarrisonParty == null || currentSettlement.Town.GarrisonParty.MapEvent == null)) || MobileParty.MainParty.MapFaction == currentSettlement.MapFaction))
						{
							if (currentSettlement.Party.SiegeEvent.BesiegerCamp.LeaderParty == MobileParty.MainParty)
							{
								return "continue_siege_after_attack";
							}
							if (MobileParty.MainParty.BesiegedSettlement == null && MobileParty.MainParty.CurrentSettlement == null)
							{
								if (!MobileParty.MainParty.IsCurrentlyAtSea)
								{
									return "join_siege_event";
								}
								if (!currentSettlement.SiegeEvent.IsBlockadeActive || currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
								{
									return "naval_town_outside";
								}
								return "join_encounter";
							}
							else
							{
								if (mainParty.CurrentSettlement.Party.MapEvent != null && mainParty.CurrentSettlement.Party.MapEvent.InvolvedParties.Contains(PartyBase.MainParty))
								{
									return "encounter";
								}
								if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting)
								{
									return "encounter_interrupted_siege_preparations";
								}
								return "menu_siege_strategies";
							}
						}
						else if (currentSettlement.Party.MapEvent != null)
						{
							if (MobileParty.MainParty.MapFaction == currentSettlement.MapFaction)
							{
								return "encounter";
							}
							return "join_encounter";
						}
						else
						{
							if (currentSettlement.MapFaction == MobileParty.MainParty.MapFaction && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
							{
								return "army_wait_at_settlement";
							}
							if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting && currentSettlement.IsFortification)
							{
								return "town_wait_menus";
							}
							if (currentSettlement.IsCastle)
							{
								return "castle_outside";
							}
							if (MobileParty.MainParty.IsCurrentlyAtSea)
							{
								return "naval_town_outside";
							}
							return "town_outside";
						}
					}
					else if (currentSettlement.IsHideout)
					{
						return "hideout_place";
					}
				}
				else if (Settlement.CurrentSettlement != null)
				{
					Settlement currentSettlement2 = Settlement.CurrentSettlement;
					if (currentSettlement2.IsVillage)
					{
						if (currentSettlement2.IsUnderRaid)
						{
							return "encounter_interrupted_raid_started";
						}
						if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting)
						{
							return "village_wait_menus";
						}
					}
				}
				return null;
			}
			if (mainParty.Army.LeaderParty != mainParty && !mainParty.Army.LeaderParty.IsCurrentlyAtSea && mainParty.Army.LeaderParty.IsTransitionInProgress && !mainParty.HasNavalNavigationCapability)
			{
				return "menu_player_kicked_out_from_army_navigation_incapability";
			}
			if ((mainParty.AttachedTo.CurrentSettlement != null && !mainParty.AttachedTo.CurrentSettlement.IsUnderSiege) || (mainParty.AttachedTo.LastVisitedSettlement != null && mainParty.AttachedTo.LastVisitedSettlement.IsVillage && mainParty.AttachedTo.LastVisitedSettlement.Position.DistanceSquared(mainParty.AttachedTo.Position) < 1f))
			{
				return "army_wait_at_settlement";
			}
			if (mainParty.AttachedTo.CurrentSettlement == null || !mainParty.AttachedTo.CurrentSettlement.IsUnderSiege)
			{
				return "army_wait";
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting)
			{
				return "encounter_interrupted_siege_preparations";
			}
			return "menu_siege_strategies";
		}

		// Token: 0x060017A4 RID: 6052 RVA: 0x000703EB File Offset: 0x0006E5EB
		public override bool IsPlunderMenu(string gameMenuId)
		{
			return gameMenuId == "raiding_village";
		}
	}
}
