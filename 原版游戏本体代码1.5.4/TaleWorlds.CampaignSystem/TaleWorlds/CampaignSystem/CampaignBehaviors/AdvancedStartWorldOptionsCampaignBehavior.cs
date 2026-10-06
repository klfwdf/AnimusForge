using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class AdvancedStartWorldOptionsCampaignBehavior : CampaignBehaviorBase
{
	private enum WorldScenarioSeed
	{
		LastStand = 522134860,
		TwoFactionWar = 335872418,
		UnitedEmpire = 918273645,
		Invasion = 1517171341,
		AlternativeCalradia = 726028561,
		NordInvasion = 647125983
	}

	private static Kingdom PlayerSelectedKingdomInStartOptions
	{
		get
		{
			string playerSelectedKingdomId = Campaign.Current.AdvancedStartData.GetKingdomId();
			if (string.IsNullOrEmpty(playerSelectedKingdomId))
			{
				return null;
			}
			return Campaign.Current.Kingdoms.FirstOrDefaultQ((Kingdom k) => k.StringId.Equals(playerSelectedKingdomId));
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ApplyWorldScenarios();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public static void ApplyWorldScenarios()
	{
		switch (Campaign.Current.AdvancedStartData.GetScenario())
		{
		case "LastStand":
			OnLastStandScenarioSelected(GetRandomForScenario(WorldScenarioSeed.LastStand));
			break;
		case "twofactionwar":
			OnTwoFactionWarScenarioSelected(GetRandomForScenario(WorldScenarioSeed.TwoFactionWar));
			break;
		case "unitedempire":
			OnUnitedEmpireScenarioSelected(GetRandomForScenario(WorldScenarioSeed.UnitedEmpire));
			break;
		case "InvasionId":
			OnInvasionScenarioSelected(GetRandomForScenario(WorldScenarioSeed.Invasion));
			break;
		case "alternativecalradia":
			OnAlternativeCalradiaScenarioSelected(GetRandomForScenario(WorldScenarioSeed.AlternativeCalradia));
			break;
		case "nordinvasion":
			OnNordInvasionScenarioSelected(GetRandomForScenario(WorldScenarioSeed.NordInvasion));
			break;
		}
	}

	private static void GiveSettlementToClan(Clan clan, Settlement settlement)
	{
		if (settlement != null && settlement.OwnerClan != clan)
		{
			settlement.Town.IsOwnerUnassigned = false;
			settlement.Town.Governor = null;
			settlement.Town.OwnerClan = clan;
		}
	}

	private static Clan GetRandomNonPlayerClanOrRulingClan(Kingdom kingdom, MBFastRandom random)
	{
		List<Clan> list = new List<Clan>();
		foreach (Clan clan in kingdom.Clans)
		{
			if (clan != Clan.PlayerClan)
			{
				list.Add(clan);
			}
		}
		if (list.Count <= 0)
		{
			return kingdom.RulingClan;
		}
		return GetRandomElementInternal(list, random);
	}

	private static T GetRandomElementInternal<T>(IReadOnlyList<T> list, MBFastRandom random)
	{
		if (list.Count == 0)
		{
			return default(T);
		}
		return list[random.Next(list.Count)];
	}

	private static T GetRandomElementWithPredicateInternal<T>(IReadOnlyList<T> list, Func<T, bool> predicate, MBFastRandom random)
	{
		List<T> list2 = new List<T>();
		foreach (T item in list)
		{
			if (predicate(item))
			{
				list2.Add(item);
			}
		}
		if (list2.Count <= 0)
		{
			return default(T);
		}
		return list2[random.Next(list2.Count)];
	}

	private static MBFastRandom GetRandomForScenario(WorldScenarioSeed scenarioSeed)
	{
		return new MBFastRandom((uint)((ulong)Campaign.Current.Options.Seed ^ (ulong)scenarioSeed));
	}

	private static List<Settlement> GetFortificationNeighbors(Settlement settlement)
	{
		List<Settlement> list = new List<Settlement>();
		if (settlement?.Town == null)
		{
			return list;
		}
		foreach (Settlement neighborFortification in settlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
		{
			if (neighborFortification != null && neighborFortification.IsFortification)
			{
				list.Add(neighborFortification);
			}
		}
		return list;
	}

	private static void ChangeKingdomInternal(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
		FactionHelper.AdjustFactionStancesForClanJoiningKingdom(clan, newKingdom);
		if (oldKingdom != null)
		{
			clan.ClanLeaveKingdom(giveBackFiefs: true);
		}
		clan.Kingdom = newKingdom;
	}

	private static void HandleKingdomCleanup(MBFastRandom scenarioRandom)
	{
		string scenario = Campaign.Current.AdvancedStartData.GetScenario();
		foreach (Kingdom item in Kingdom.All.ToList())
		{
			if (!item.IsEliminated && item.Clans.Count == 0 && (item != PlayerSelectedKingdomInStartOptions || (scenario == "unitedempire" && item.Culture.StringId == "empire")))
			{
				item.DeactivateKingdom();
				Campaign.Current.FactionManager.RemoveFactionsFromCampaignWars(item);
			}
			if (!item.IsEliminated && (item.RulingClan == null || item.RulingClan.MapFaction != item))
			{
				item.RulingClan = GetRandomElementInternal(item.Clans, scenarioRandom);
			}
		}
	}

	private static void OnLastStandScenarioSelected(MBFastRandom scenarioRandom)
	{
		string lastStandKingdomId = Campaign.Current.AdvancedStartData.GetLastStandKingdom();
		Kingdom lastStandKingdom = Kingdom.All.Find((Kingdom t) => t.StringId == lastStandKingdomId);
		Settlement randomElementWithPredicateInternal = GetRandomElementWithPredicateInternal((from t in lastStandKingdom.Fiefs
			where t.IsTown
			select t.Settlement).ToList(), (Settlement x) => x.IsTown, scenarioRandom);
		GiveSettlementToClan(lastStandKingdom.RulingClan, randomElementWithPredicateInternal);
		Dictionary<Kingdom, int> dictionary = new Dictionary<Kingdom, int>();
		foreach (Town fief in lastStandKingdom.Fiefs)
		{
			foreach (Settlement neighborFortification in fief.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification.MapFaction is Kingdom kingdom && kingdom != lastStandKingdom)
				{
					if (!dictionary.TryGetValue(kingdom, out var value))
					{
						value = 0;
					}
					dictionary[kingdom] = value + 1;
				}
			}
		}
		Kingdom key = dictionary.MaxBy((KeyValuePair<Kingdom, int> x) => x.Value).Key;
		foreach (Clan item in lastStandKingdom.Clans.ToList())
		{
			if (item != lastStandKingdom.RulingClan && scenarioRandom.NextFloat() < 0.7f)
			{
				ChangeKingdomInternal(item, lastStandKingdom, key);
			}
		}
		foreach (Settlement item2 in lastStandKingdom.Fiefs.Select((Town s) => s.Settlement).ToList())
		{
			if (item2 != randomElementWithPredicateInternal)
			{
				GiveSettlementToClan(GetRandomNonPlayerClanOrRulingClan(key, scenarioRandom), item2);
			}
		}
		foreach (Kingdom item3 in Kingdom.All.Where((Kingdom x) => lastStandKingdom.IsAtWarWith(x)).ToList())
		{
			FactionManager.SetNeutral(lastStandKingdom, item3);
		}
		FactionManager.DeclareWar(lastStandKingdom, key);
		HandleKingdomCleanup(scenarioRandom);
	}

	private static void OnTwoFactionWarScenarioSelected(MBFastRandom scenarioRandom)
	{
		string kingdom1Id = Campaign.Current.AdvancedStartData.GetTwoFactionWarFaction1Id();
		string kingdom2Id = Campaign.Current.AdvancedStartData.GetTwoFactionWarFaction2Id();
		Kingdom kingdom = Kingdom.All.Find((Kingdom t) => t.StringId == kingdom1Id);
		Kingdom kingdom2 = Kingdom.All.Find((Kingdom t) => t.StringId == kingdom2Id);
		Kingdom obj = ((scenarioRandom.NextFloat() < 0.5f) ? kingdom : kingdom2);
		Kingdom invaderKingdom = ((obj == kingdom) ? kingdom2 : kingdom);
		int num = RoundRandomizedInternal((float)Settlement.All.CountQ((Settlement x) => x.IsFortification) / 2f, scenarioRandom);
		GrowKingdomByInvasion(obj, num, scenarioRandom, protectAllInvadedKingdomsFromElimination: true);
		List<Kingdom> list = Kingdom.All.ToList();
		list.Remove(kingdom);
		list.Remove(kingdom2);
		ShuffleInternal(list, scenarioRandom);
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			DestroyKingdomByDefection(list[num2], scenarioRandom);
		}
		GrowKingdomByInvasion(invaderKingdom, num, scenarioRandom, protectAllInvadedKingdomsFromElimination: true);
		FactionManager.DeclareWar(kingdom, kingdom2);
		HandleKingdomCleanup(scenarioRandom);
	}

	private static void OnUnitedEmpireScenarioSelected(MBFastRandom scenarioRandom)
	{
		string unifierKingdomId = Campaign.Current.AdvancedStartData.GetUnitedEmpireUnifierKingdomId();
		Kingdom unifierKingdom = Kingdom.All.Find((Kingdom t) => t.StringId == unifierKingdomId);
		MBList<Kingdom> mBList = Kingdom.All.WhereQ((Kingdom x) => unifierKingdom.IsAtWarWith(x)).ToMBList();
		Kingdom item = Kingdom.All.Find((Kingdom t) => t.StringId == "empire");
		Kingdom item2 = Kingdom.All.Find((Kingdom t) => t.StringId == "empire_w");
		Kingdom item3 = Kingdom.All.Find((Kingdom t) => t.StringId == "empire_s");
		List<Kingdom> list = new List<Kingdom> { item, item2, item3 };
		TextObject textObject = new TextObject("{=AIh1Ik4A}Calradian Empire");
		KingdomManager kingdomManager = Campaign.Current.KingdomManager;
		CultureObject culture = unifierKingdom.Culture;
		Clan rulingClan = unifierKingdom.RulingClan;
		TextObject encyclopediaRulerTitle = unifierKingdom.EncyclopediaRulerTitle;
		Kingdom kingdom = kingdomManager.CreateKingdom(textObject, textObject, culture, rulingClan, null, null, textObject, encyclopediaRulerTitle, "calradian_empire", new Banner(unifierKingdom.Banner), unifierKingdom.Color, unifierKingdom.Color2, unifierKingdom.PrimaryBannerColor, unifierKingdom.SecondaryBannerColor);
		foreach (PolicyObject item4 in unifierKingdom.ActivePolicies.ToList())
		{
			kingdom.AddPolicy(item4);
		}
		foreach (Kingdom item5 in list)
		{
			foreach (Clan item6 in item5.Clans.ToList())
			{
				ChangeKingdomInternal(item6, item6.Kingdom, kingdom);
			}
		}
		HandleKingdomCleanup(scenarioRandom);
		foreach (Kingdom item7 in mBList)
		{
			if (!kingdom.IsAtWarWith(item7) && !item7.IsEliminated)
			{
				FactionManager.DeclareWar(kingdom, item7);
			}
		}
	}

	private static void OnInvasionScenarioSelected(MBFastRandom scenarioRandom)
	{
		string invaderFactionId = Campaign.Current.AdvancedStartData.GetInvasionScenarioFactionId();
		Kingdom kingdom = Kingdom.All.Find((Kingdom t) => t.StringId == invaderFactionId);
		int num = RoundRandomizedInternal(scenarioRandom.NextFloatRanged((float)Town.AllFiefs.Count() * 0.6f, (float)Town.AllFiefs.Count() * 0.7f), scenarioRandom);
		GrowKingdomByInvasion(kingdom, num, scenarioRandom, protectAllInvadedKingdomsFromElimination: false);
		foreach (Town fief in kingdom.Fiefs)
		{
			foreach (Settlement fortificationNeighbor in GetFortificationNeighbors(fief.Settlement))
			{
				if (fortificationNeighbor.MapFaction != kingdom.MapFaction && !kingdom.IsAtWarWith(fortificationNeighbor.MapFaction))
				{
					FactionManager.DeclareWar(kingdom, fortificationNeighbor.MapFaction);
				}
			}
		}
		HandleKingdomCleanup(scenarioRandom);
	}

	private static void OnNordInvasionScenarioSelected(MBFastRandom scenarioRandom)
	{
		Kingdom kingdom = Kingdom.All.Find((Kingdom t) => t.StringId == "nord");
		int num = kingdom.Fiefs.Count + scenarioRandom.Next(10, 14);
		GrowKingdomByInvasion(kingdom, num, scenarioRandom, protectAllInvadedKingdomsFromElimination: false);
		foreach (Town fief in kingdom.Fiefs)
		{
			foreach (Settlement fortificationNeighbor in GetFortificationNeighbors(fief.Settlement))
			{
				if (fortificationNeighbor.MapFaction != kingdom.MapFaction && !kingdom.IsAtWarWith(fortificationNeighbor.MapFaction))
				{
					FactionManager.DeclareWar(kingdom, fortificationNeighbor.MapFaction);
				}
			}
		}
		HandleKingdomCleanup(scenarioRandom);
	}

	private static void OnAlternativeCalradiaScenarioSelected(MBFastRandom scenarioRandom)
	{
		(int, int) alternativeCalradiaDestroyRange = GetAlternativeCalradiaDestroyRange(Campaign.Current.AdvancedStartData.GetAlternativeCalradiaVariant());
		int num = scenarioRandom.Next(alternativeCalradiaDestroyRange.Item1, alternativeCalradiaDestroyRange.Item2 + 1);
		List<Kingdom> list = Kingdom.All.ToList();
		ShuffleInternal(list, scenarioRandom);
		if (PlayerSelectedKingdomInStartOptions != null)
		{
			list.Remove(PlayerSelectedKingdomInStartOptions);
		}
		for (int i = 0; i < num; i++)
		{
			Kingdom kingdomToDestroy = list[0];
			list.RemoveAt(0);
			DestroyKingdomByDefection(kingdomToDestroy, scenarioRandom);
		}
		if (PlayerSelectedKingdomInStartOptions != null)
		{
			list.Add(PlayerSelectedKingdomInStartOptions);
		}
		ShuffleInternal(list, scenarioRandom);
		foreach (Kingdom item in list)
		{
			float num2 = scenarioRandom.NextFloatRanged(0.2f, 0.4f);
			int num3 = item.Fiefs.Count * RoundRandomizedInternal(1f + num2, scenarioRandom);
			GrowKingdomByInvasion(item, num3, scenarioRandom, protectAllInvadedKingdomsFromElimination: true);
		}
		HandleKingdomCleanup(scenarioRandom);
		UpdateWarAndPeaceInTheWorld(scenarioRandom);
	}

	private static void UpdateWarAndPeaceInTheWorld(MBFastRandom scenarioRandom)
	{
		MBReadOnlyList<Kingdom> all = Kingdom.All;
		for (int i = 0; i < all.Count; i++)
		{
			for (int j = i + 1; j < all.Count; j++)
			{
				Kingdom kingdom = all[i];
				Kingdom kingdom2 = all[j];
				bool flag = AreKingdomsNeighbors(kingdom, kingdom2) && scenarioRandom.NextFloat() < 0.2f;
				bool flag2 = kingdom.IsAtWarWith(kingdom2);
				if (flag && !flag2)
				{
					FactionManager.DeclareWar(kingdom, kingdom2);
				}
				else if (!flag && flag2)
				{
					MakePeaceAction.Apply(kingdom, kingdom2);
				}
			}
		}
	}

	private static bool AreKingdomsNeighbors(Kingdom kingdom1, Kingdom kingdom2)
	{
		foreach (Town fief in kingdom1.Fiefs)
		{
			foreach (Settlement fortificationNeighbor in GetFortificationNeighbors(fief.Settlement))
			{
				if (fortificationNeighbor.MapFaction == kingdom2)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static (int Min, int Max) GetAlternativeCalradiaDestroyRange(string variant)
	{
		if (variant == "alternativecalradiafractured")
		{
			return (Min: 2, Max: 4);
		}
		if (variant == "alternativecalradiashattered")
		{
			return (Min: 3, Max: 5);
		}
		return (Min: 0, Max: 2);
	}

	private static void DestroyKingdomByDefection(Kingdom kingdomToDestroy, MBFastRandom scenarioRandom)
	{
		List<Kingdom> list = Kingdom.All.Where((Kingdom k) => !k.IsEliminated && k != kingdomToDestroy && AreKingdomsNeighbors(k, kingdomToDestroy)).ToList();
		bool flag = true;
		while (flag)
		{
			flag = false;
			int num = 0;
			Clan clan = null;
			Kingdom newKingdom = null;
			foreach (Clan item in kingdomToDestroy.Clans.ToList())
			{
				if (item.Kingdom != kingdomToDestroy)
				{
					continue;
				}
				Dictionary<Kingdom, int> dictionary = new Dictionary<Kingdom, int>();
				foreach (Town fief in item.Fiefs)
				{
					foreach (Settlement fortificationNeighbor in GetFortificationNeighbors(fief.Settlement))
					{
						Kingdom kingdom = (Kingdom)fortificationNeighbor.MapFaction;
						if (kingdom != kingdomToDestroy)
						{
							if (!dictionary.TryGetValue(kingdom, out var value))
							{
								value = 0;
							}
							dictionary[kingdom] = value + 1;
						}
					}
				}
				if (dictionary.Count != 0)
				{
					KeyValuePair<Kingdom, int> keyValuePair = dictionary.MaxBy((KeyValuePair<Kingdom, int> x) => x.Value);
					if (keyValuePair.Value > num)
					{
						num = keyValuePair.Value;
						clan = item;
						newKingdom = keyValuePair.Key;
					}
				}
			}
			if (clan != null)
			{
				ChangeKingdomInternal(clan, kingdomToDestroy, newKingdom);
				flag = true;
			}
		}
		while (kingdomToDestroy.Clans.Count > 0)
		{
			if (list.Count == 0)
			{
				Debug.FailedAssert("No neighbor kingdom found to receive remaining landlocked clans in Alternative Calradia, check this case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\AdvancedStartWorldOptionsCampaignBehavior.cs", "DestroyKingdomByDefection", 590);
				break;
			}
			Kingdom randomElementInternal = GetRandomElementInternal(list, scenarioRandom);
			ChangeKingdomInternal(kingdomToDestroy.Clans[0], kingdomToDestroy, randomElementInternal);
		}
		kingdomToDestroy.DeactivateKingdom();
		Campaign.Current.FactionManager.RemoveFactionsFromCampaignWars(kingdomToDestroy);
	}

	private static void GrowKingdomByInvasion(Kingdom invaderKingdom, float targetFiefCount, MBFastRandom scenarioRandom, bool protectAllInvadedKingdomsFromElimination)
	{
		while ((float)invaderKingdom.Fiefs.Count < targetFiefCount)
		{
			List<Settlement> list = new List<Settlement>();
			foreach (Town fief in invaderKingdom.Fiefs)
			{
				foreach (Settlement fortificationNeighbor in GetFortificationNeighbors(fief.Settlement))
				{
					if (fortificationNeighbor.MapFaction != invaderKingdom)
					{
						Kingdom kingdom = (Kingdom)fortificationNeighbor.MapFaction;
						if ((!protectAllInvadedKingdomsFromElimination && kingdom != PlayerSelectedKingdomInStartOptions) || fortificationNeighbor.OwnerClan != kingdom.RulingClan || fortificationNeighbor.OwnerClan.Fiefs.Count != 1)
						{
							list.Add(fortificationNeighbor);
						}
					}
				}
			}
			if (list.Count == 0)
			{
				break;
			}
			Settlement randomElementInternal = GetRandomElementInternal(list, scenarioRandom);
			Kingdom kingdom2 = randomElementInternal.OwnerClan.Kingdom;
			if ((protectAllInvadedKingdomsFromElimination || kingdom2 == PlayerSelectedKingdomInStartOptions) && randomElementInternal.OwnerClan == kingdom2.RulingClan)
			{
				GiveSettlementToClan(GetRandomElementInternal(GetFortificationNeighbors(randomElementInternal).WhereQ((Settlement f) => f.MapFaction == invaderKingdom).ToList(), scenarioRandom).OwnerClan, randomElementInternal);
				continue;
			}
			ChangeKingdomInternal(randomElementInternal.OwnerClan, kingdom2, invaderKingdom);
			if (kingdom2 != null && kingdom2.Clans.Count == 0)
			{
				kingdom2.DeactivateKingdom();
				Campaign.Current.FactionManager.RemoveFactionsFromCampaignWars(kingdom2);
			}
		}
	}

	private static int RoundRandomizedInternal(float value, MBFastRandom scenarioRandom)
	{
		int num = TaleWorlds.Library.MathF.Floor(value);
		float num2 = value - (float)num;
		if (scenarioRandom.NextFloat() < num2)
		{
			num++;
		}
		return num;
	}

	private static void ShuffleInternal<T>(List<T> list, MBFastRandom scenarioRandom)
	{
		for (int num = list.Count - 1; num >= 0; num--)
		{
			int index = scenarioRandom.Next(list.Count);
			T value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
	}
}
