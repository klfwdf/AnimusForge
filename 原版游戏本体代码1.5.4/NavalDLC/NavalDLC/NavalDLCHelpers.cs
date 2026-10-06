using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Storyline;
using StoryMode;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC;

public static class NavalDLCHelpers
{
	public static ExplainedNumber GetAveragePartySizeLimitFromTemplate(PartyTemplateObject templateObject)
	{
		int num = 0;
		foreach (PartyTemplateStack stack in templateObject.Stacks)
		{
			num += (stack.MaxValue + stack.MinValue) / 2;
		}
		return new ExplainedNumber(num);
	}

	public static ExplainedNumber GetMaxPartySizeLimitFromTemplate(PartyTemplateObject templateObject)
	{
		int num = 0;
		foreach (PartyTemplateStack stack in templateObject.Stacks)
		{
			num += stack.MaxValue;
		}
		return new ExplainedNumber(num);
	}

	public static List<Ship> GetSetPieceBattleShips(PartyTemplateObject template, PartyBase party)
	{
		List<Ship> list = party.Ships.Where((Ship s) => s.IsUsedByQuest).ToList();
		int num = 0;
		foreach (ShipTemplateStack shipHull in template.ShipHulls)
		{
			num += shipHull.MaxValue;
		}
		int num2 = num - list.Count();
		if (num2 > 0)
		{
			foreach (Ship item in (from s in party.Ships
				where !s.IsUsedByQuest
				orderby s.FlagshipScore descending
				select s).ToList())
			{
				if (num2 > 0)
				{
					list.Add(item);
					num2--;
					continue;
				}
				break;
			}
		}
		return list;
	}

	public static bool IsNavalRaidMissionOpen()
	{
		if (Mission.Current != null && Mission.Current.IsNavalRaidBattle)
		{
			return true;
		}
		return false;
	}

	public static bool IsShipOrdersAvailable()
	{
		if (Mission.Current == null || !Mission.Current.IsNavalBattle)
		{
			return false;
		}
		if (Mission.Current.PlayerTeam?.PlayerOrderController == null)
		{
			return false;
		}
		if (Mission.Current.GetMissionBehavior<NavalShipsLogic>() == null)
		{
			return false;
		}
		MBReadOnlyList<Formation> selectedFormations = Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations;
		if (selectedFormations == null)
		{
			return false;
		}
		for (int i = 0; i < selectedFormations.Count; i++)
		{
			if (IsPlayerCaptainOfFormationShip(selectedFormations[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsPlayerCaptainOfFormationShip(Formation formation)
	{
		return IsAgentCaptainOfFormationShip(Agent.Main, formation);
	}

	public static bool IsAgentCaptainOfFormationShip(Agent agent, Formation formation)
	{
		NavalShipsLogic navalShipsLogic = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
		if (navalShipsLogic == null || !navalShipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var ship))
		{
			return false;
		}
		if (agent != null && ship.Captain == agent)
		{
			return true;
		}
		if (agent != null && agent.IsPlayerControlled && ship.Formation.Team.IsPlayerTeam)
		{
			return ship.Formation.Index == 0;
		}
		return false;
	}

	public static void SetCustomSailPatternOfPartyShips(MobileParty party, string sailId)
	{
		foreach (Ship ship in party.Ships)
		{
			ship.CustomSailPatternId = sailId;
		}
		MobileParty.MainParty.SetNavalVisualAsDirty();
	}

	public static void AddUpgradePiecesToPartyShips(MobileParty party, Dictionary<string, string> upgradePiecesBySlot, Figurehead figurehead = null)
	{
		foreach (Ship ship in party.Ships)
		{
			foreach (KeyValuePair<string, string> item in upgradePiecesBySlot)
			{
				if (ship.HasSlot(item.Key))
				{
					ship.EquipUpgradePiece(item.Key, MBObjectManager.Instance.GetObject<ShipUpgradePiece>(item.Value));
				}
			}
			if (figurehead != null)
			{
				ship.ChangeFigurehead(figurehead);
			}
		}
	}

	public static void AddSisterToClan()
	{
		StoryModeHeroes.LittleSister.Clan = Clan.PlayerClan;
		if (StoryModeHeroes.LittleSister.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			Settlement settlement = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement s) => s.OwnerClan.MapFaction == Clan.PlayerClan.MapFaction)?.Settlement;
			if (settlement == null)
			{
				settlement = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement s) => !Clan.PlayerClan.MapFaction.IsAtWarWith(s.OwnerClan.MapFaction))?.Settlement;
			}
			if (settlement == null)
			{
				settlement = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown);
			}
			if (Settlement.CurrentSettlement == settlement)
			{
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(StoryModeHeroes.LittleSister, settlement);
			}
			else
			{
				TeleportHeroAction.ApplyDelayedTeleportToSettlement(StoryModeHeroes.LittleSister, settlement);
			}
			StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(StoryModeHeroes.LittleSister);
		}
		else
		{
			StoryModeHeroes.LittleSister.ChangeState(Hero.CharacterStates.NotSpawned);
		}
		StoryModeHeroes.LittleSister.UpdateLastKnownClosestSettlement(NavalStorylineData.HomeSettlement);
		TextObject textObject = new TextObject("{=7XTkTi9B}{PLAYER_LITTLE_SISTER.NAME} is the little sister of {PLAYER.LINK}.");
		StringHelpers.SetCharacterProperties("PLAYER_LITTLE_SISTER", StoryModeHeroes.LittleSister.CharacterObject, textObject);
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
		StoryModeHeroes.LittleSister.EncyclopediaText = textObject;
	}
}
