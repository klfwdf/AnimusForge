using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultKingdomCreationModel : KingdomCreationModel
{
	public override int MinimumClanTierToCreateKingdom => 4;

	public override int MinimumNumberOfSettlementsOwnedToCreateKingdom => 1;

	public override int MinimumTroopCountToCreateKingdom => 100;

	public override int MaximumNumberOfInitialPolicies => 4;

	public override bool IsPlayerKingdomCreationPossible(out List<TextObject> explanations)
	{
		bool result = true;
		explanations = new List<TextObject>();
		if (Hero.MainHero.MapFaction.IsKingdomFaction)
		{
			result = false;
			TextObject item = new TextObject("{=w5b79MmE}Player clan should be independent.");
			explanations.Add(item);
		}
		if (Clan.PlayerClan.Tier < MinimumClanTierToCreateKingdom)
		{
			result = false;
			TextObject textObject = new TextObject("{=j0UDi2AN}Clan tier should be at least {TIER}.");
			textObject.SetTextVariable("TIER", MinimumClanTierToCreateKingdom);
			explanations.Add(textObject);
		}
		if (Clan.PlayerClan.Settlements.Count((Settlement t) => t.IsTown || t.IsCastle) < MinimumNumberOfSettlementsOwnedToCreateKingdom)
		{
			result = false;
			TextObject textObject2 = new TextObject("{=YsGSgaba}Number of towns or castles you own should be at least {SETTLEMENT_COUNT}.");
			textObject2.SetTextVariable("SETTLEMENT_COUNT", MinimumNumberOfSettlementsOwnedToCreateKingdom);
			explanations.Add(textObject2);
		}
		if (Clan.PlayerClan.Fiefs.Sum((Town t) => t.GarrisonParty?.MemberRoster?.TotalHealthyCount ?? 0) + Clan.PlayerClan.WarPartyComponents.Sum((WarPartyComponent t) => t.MobileParty.MemberRoster.TotalHealthyCount) < MinimumTroopCountToCreateKingdom)
		{
			result = false;
			TextObject textObject3 = new TextObject("{=K2txLdOS}You should have at least {TROOP_COUNT} men ready to fight.");
			textObject3.SetTextVariable("TROOP_COUNT", MinimumTroopCountToCreateKingdom);
			explanations.Add(textObject3);
		}
		return result;
	}

	public override bool IsPlayerKingdomAbdicationPossible(out List<TextObject> explanations)
	{
		explanations = new List<TextObject>();
		bool num = Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan;
		bool flag = MobileParty.MainParty.MapEvent != null || MobileParty.MainParty.SiegeEvent != null;
		bool flag2 = num && !Clan.PlayerClan.Kingdom.UnresolvedDecisions.IsEmpty();
		if (!num)
		{
			explanations.Add(new TextObject("{=s1ERZ4ZR}You must be the king"));
		}
		if (flag)
		{
			explanations.Add(new TextObject("{=uaMmmhRV}You must conclude your current encounter"));
		}
		if (flag2)
		{
			explanations.Add(new TextObject("{=etKrpcHe}You must resolve pending decisions"));
		}
		if (num && !flag)
		{
			return !flag2;
		}
		return false;
	}

	public override IEnumerable<CultureObject> GetAvailablePlayerKingdomCultures()
	{
		List<CultureObject> list = new List<CultureObject>();
		list.Add(Clan.PlayerClan.Culture);
		foreach (Settlement item in Clan.PlayerClan.Settlements.Where((Settlement t) => t.IsTown || t.IsCastle))
		{
			if (!list.Contains(item.Culture))
			{
				list.Add(item.Culture);
			}
		}
		foreach (CultureObject item2 in list)
		{
			yield return item2;
		}
	}
}
