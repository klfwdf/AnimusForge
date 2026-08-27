using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AnimusForge.ExpeditionParade.Campaign;

internal static class ParadeEligibilityService
{
	internal static bool CanStartParade(Settlement settlement, Hero player, out TextObject reason)
	{
		if (settlement == null || (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage))
		{
			reason = Text("当前地点不支持出征阅兵。");
			return false;
		}
		if (player == null || MobileParty.MainParty?.MemberRoster == null)
		{
			reason = Text("当前无法读取玩家主队伍。");
			return false;
		}
		if (!HasParadeAuthority(settlement, player))
		{
			reason = Text("只有领地所属玩家家族，或本国统治者，才能在这里举行阅兵。");
			return false;
		}
		if (settlement.IsUnderSiege)
		{
			reason = Text("聚落正在被围攻，不能举行阅兵。");
			return false;
		}
		if (settlement.IsVillage && settlement.Village?.VillageState != Village.VillageStates.Normal)
		{
			reason = Text("村庄正被劫掠、已被洗劫或尚未恢复。");
			return false;
		}
		if (settlement.IsUnderRaid || settlement.IsRaided)
		{
			reason = Text("聚落处于劫掠状态，不能举行阅兵。");
			return false;
		}
		if (TaleWorlds.MountAndBlade.Mission.Current != null || CampaignMission.Current != null)
		{
			reason = Text("当前已有互斥 Mission 活动。");
			return false;
		}
		if (PlayerEncounterCompat.GetCurrentMapEventSafe() != null || MobileParty.MainParty.MapEvent != null)
		{
			reason = Text("当前处于战斗、遭遇或其他地图事件中。");
			return false;
		}

		int healthyTroops = 0;
		for (int index = 0; index < MobileParty.MainParty.MemberRoster.Count; index++)
		{
			var element = MobileParty.MainParty.MemberRoster.GetElementCopyAtIndex(index);
			if (element.Character?.IsHero == false)
			{
				healthyTroops += System.Math.Max(0, element.Number - element.WoundedNumber);
			}
		}
		if (healthyTroops <= 0)
		{
			reason = Text("玩家主队伍中没有可展示的健康普通士兵。");
			return false;
		}

		reason = Text("权限与队伍条件满足；仍需在 Mission 内通过路线和 Agent 预算预检。");
		return true;
	}

	internal static bool HasParadeAuthority(Settlement settlement, Hero player)
	{
		Clan playerClan = player?.Clan ?? Clan.PlayerClan;
		Settlement authoritySettlement = settlement?.IsVillage == true ? settlement.Village?.Bound : settlement;
		Clan ownerClan = authoritySettlement?.OwnerClan;
		if (playerClan == null || ownerClan == null)
		{
			return false;
		}
		if (ownerClan == playerClan)
		{
			return true;
		}

		Kingdom playerKingdom = playerClan.Kingdom;
		bool isRuler = playerKingdom != null
			&& (playerKingdom.Leader == player || playerKingdom.RulingClan?.Leader == player);
		return isRuler && ownerClan.Kingdom == playerKingdom;
	}

	private static TextObject Text(string value)
	{
		return new TextObject("{=!}" + value);
	}
}
