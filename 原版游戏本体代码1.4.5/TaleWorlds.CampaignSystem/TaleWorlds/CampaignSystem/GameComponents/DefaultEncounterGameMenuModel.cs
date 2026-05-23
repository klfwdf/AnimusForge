using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultEncounterGameMenuModel : EncounterGameMenuModel
{
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
					if (settlement.LastAttackerParty != null && settlement.LastAttackerParty != MobileParty.MainParty && MobileParty.MainParty.MapFaction.IsAtWarWith(settlement.MapFaction))
					{
						return "raid_occupied";
					}
					if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && encounteredPartyBase.MapEvent.AttackerSide.LeaderParty == MobileParty.MainParty.Army.LeaderParty.Party && encounteredPartyBase.MapEvent.DefenderSide.TroopCount <= 0)
					{
						joinBattle = true;
						if (!encounteredPartyBase.MapEvent.IsRaid)
						{
							return "army_wait";
						}
						return "raiding_village";
					}
					if ((MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && attackerParty == MobileParty.MainParty.Army.LeaderParty.Party) || (MobileParty.MainParty.CurrentSettlement == settlement && MobileParty.MainParty.MapFaction == settlement.MapFaction))
					{
						joinBattle = true;
						return "encounter";
					}
					return "join_encounter";
				}
				if (settlement.MapFaction == MobileParty.MainParty.MapFaction && MobileParty.MainParty.Army != null && attackerParty == MobileParty.MainParty.Army.LeaderParty.Party && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
				{
					return "army_wait_at_settlement";
				}
				return "village_outside";
			}
			if (settlement.IsFortification)
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
						if (MobileParty.MainParty.IsCurrentlyAtSea)
						{
							if (!settlement.Party.SiegeEvent.IsBlockadeActive || settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
							{
								return "naval_town_outside";
							}
							return "join_encounter";
						}
						return "join_siege_event";
					}
				}
				if (settlement.Party.MapEvent != null)
				{
					if ((MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && attackerParty == MobileParty.MainParty.Army.LeaderParty.Party) || (MobileParty.MainParty.CurrentSettlement == settlement && settlement.Party.MapEvent.CanPartyJoinBattle(PartyBase.MainParty, settlement.BattleSide)))
					{
						return "encounter";
					}
					if (MobileParty.MainParty.IsCurrentlyAtSea)
					{
						if (!settlement.Party.SiegeEvent.IsBlockadeActive || settlement.Party.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
						{
							return "naval_town_outside";
						}
						return "join_encounter";
					}
					return "join_siege_event";
				}
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
		if (encounteredPartyBase.MapEvent != null)
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
			if (encounteredPartyBase.SiegeEvent != null && encounteredPartyBase.MapEvent.IsSiegeAssault)
			{
				if (MobileParty.MainParty.IsCurrentlyAtSea)
				{
					if (!encounteredPartyBase.SiegeEvent.IsBlockadeActive || encounteredPartyBase.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
					{
						return "naval_town_outside";
					}
					return "join_encounter";
				}
				return "join_siege_event";
			}
			Settlement mapEventSettlement = encounteredPartyBase.MapEvent.MapEventSettlement;
			if (mapEventSettlement != null && mapEventSettlement.IsVillage && mapEventSettlement.IsUnderRaid && mapEventSettlement.LastAttackerParty != null && mapEventSettlement.LastAttackerParty != MobileParty.MainParty && !MobileParty.MainParty.MapFaction.IsAtWarWith(defenderParty.MapFaction))
			{
				return "raid_occupied";
			}
			return "join_encounter";
		}
		if (encounteredPartyBase.IsMobile)
		{
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
			if (encounteredPartyBase.SiegeEvent != null)
			{
				if (MobileParty.MainParty.IsCurrentlyAtSea)
				{
					if (!encounteredPartyBase.SiegeEvent.IsBlockadeActive || encounteredPartyBase.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
					{
						return "naval_town_outside";
					}
					return "join_encounter";
				}
				return "join_siege_event";
			}
			return "encounter_meeting";
		}
		return null;
	}

	public override string GetRaidCompleteMenu()
	{
		return "village_loot_complete";
	}

	public override string GetNewPartyJoinMenu(MobileParty newParty)
	{
		if (PartyBase.MainParty.MapEvent.IsRaid && PartyBase.MainParty.MapEvent.AttackerSide.LeaderParty.MapFaction != PartyBase.MainParty.MapEvent.MapEventSettlement.MapFaction)
		{
			if (MobileParty.MainParty.CurrentSettlement != null || (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty))
			{
				return "encounter";
			}
			return "join_encounter";
		}
		return null;
	}

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
		if (mainParty.AttachedTo != null)
		{
			if (mainParty.Army.LeaderParty != mainParty && !mainParty.Army.LeaderParty.IsCurrentlyAtSea && mainParty.Army.LeaderParty.IsTransitionInProgress && !mainParty.HasNavalNavigationCapability)
			{
				return "menu_player_kicked_out_from_army_navigation_incapability";
			}
			if ((mainParty.AttachedTo.CurrentSettlement != null && !mainParty.AttachedTo.CurrentSettlement.IsUnderSiege) || (mainParty.AttachedTo.LastVisitedSettlement != null && mainParty.AttachedTo.LastVisitedSettlement.IsVillage && mainParty.AttachedTo.LastVisitedSettlement.Position.DistanceSquared(mainParty.AttachedTo.Position) < 1f))
			{
				return "army_wait_at_settlement";
			}
			if (mainParty.AttachedTo.CurrentSettlement != null && mainParty.AttachedTo.CurrentSettlement.IsUnderSiege)
			{
				if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting)
				{
					return "encounter_interrupted_siege_preparations";
				}
				return "menu_siege_strategies";
			}
			return "army_wait";
		}
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
						if (MobileParty.MainParty.IsCurrentlyAtSea)
						{
							if (!currentSettlement.SiegeEvent.IsBlockadeActive || currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
							{
								return "naval_town_outside";
							}
							return "join_encounter";
						}
						return "join_siege_event";
					}
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
				if (currentSettlement.Party.MapEvent != null)
				{
					if (MobileParty.MainParty.MapFaction == currentSettlement.MapFaction)
					{
						return "encounter";
					}
					return "join_encounter";
				}
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
			if (currentSettlement.IsHideout)
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

	public override bool IsPlunderMenu(string gameMenuId)
	{
		return gameMenuId == "raiding_village";
	}
}
