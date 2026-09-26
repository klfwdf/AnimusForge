using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Modules.Economy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	private static string BuildSettlementMerchantTrustKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant_trust:" + text;
	}

	private static int NormalizeLlmTrustDeltaValue(int value)
	{
		if (value < -10)
		{
			return -10;
		}
		if (value > 10)
		{
			return 10;
		}
		return value;
	}

	private static int ClampPositiveLongToInt(long value)
	{
		if (value <= 0L)
		{
			return 0;
		}
		if (value > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)value;
	}

	private static double GetTrustCurveNormalizedPosition(double currentTrust)
	{
		double num = Math.Abs(currentTrust) / (double)TrustMax;
		if (num < 0.0)
		{
			return 0.0;
		}
		if (num > 1.0)
		{
			return 1.0;
		}
		return num;
	}

	private static double GetTrustDeltaScaleByCurrentTrust(double currentTrust)
	{
		double trustCurveNormalizedPosition = GetTrustCurveNormalizedPosition(currentTrust);
		double num = 1.0 + TrustCurveMaxScaleOffset * Math.Pow(trustCurveNormalizedPosition, TrustCurveExponent);
		if (currentTrust < 0.0)
		{
			return num;
		}
		return 1.0 / num;
	}

	private double ConvertRawTrustDeltaToUnits(int rawDelta, double currentTrust)
	{
		if (rawDelta == 0)
		{
			return 0.0;
		}
		return (double)rawDelta * (double)TrustGainUnitsPerPoint * GetTrustDeltaScaleByCurrentTrust(currentTrust);
	}

	private int ApplyProgressiveTrustDeltaUnits(Dictionary<string, int> carryStore, string trustKey, int currentTrust, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (carryStore == null || string.IsNullOrWhiteSpace(text) || rawDelta == 0)
		{
			return 0;
		}
		carryStore.TryGetValue(text, out var value);
		double num = (double)((long)currentTrust * (long)TrustGainUnitsPerPoint + (long)value);
		long num2 = (long)num;
		long min = (long)TrustMin * (long)TrustGainUnitsPerPoint;
		long max = (long)TrustMax * (long)TrustGainUnitsPerPoint;
		int num3 = Math.Sign(rawDelta);
		int num4 = Math.Abs(rawDelta);
		for (int i = 0; i < num4; i++)
		{
			if (num <= (double)min || num >= (double)max)
			{
				break;
			}
			double currentTrust2 = num / (double)TrustGainUnitsPerPoint;
			double num5 = ConvertRawTrustDeltaToUnits(num3, currentTrust2);
			if (Math.Abs(num5) < 0.0001)
			{
				continue;
			}
			double num6 = Math.Max((double)min, Math.Min((double)max, num + num5));
			if (Math.Abs(num6 - num) < 0.0001)
			{
				break;
			}
			num = num6;
		}
		long num7 = (long)num;
		appliedUnits = (int)(num7 - num2);
		int num8 = (int)(num7 / TrustGainUnitsPerPoint);
		int num9 = (int)(num7 % TrustGainUnitsPerPoint);
		if (num9 != 0)
		{
			carryStore[text] = num9;
		}
		else
		{
			carryStore.Remove(text);
		}
		return num8 - currentTrust;
	}

	private static long GetTrustTotalUnitsWithCarry(int currentTrust, int carryUnits)
	{
		return (long)currentTrust * (long)TrustGainUnitsPerPoint + (long)carryUnits;
	}

	private int ApplyPositiveTrustSourceUnitsProgressively(long currentUnits, int sourceUnits, out long finalUnits)
	{
		finalUnits = currentUnits;
		if (sourceUnits <= 0)
		{
			return 0;
		}
		double num = (double)currentUnits;
		long max = (long)TrustMax * (long)TrustGainUnitsPerPoint;
		for (int i = 0; i < sourceUnits && num < (double)max; i++)
		{
			double currentTrust = num / (double)TrustGainUnitsPerPoint;
			num = Math.Min((double)max, num + GetTrustDeltaScaleByCurrentTrust(currentTrust));
		}
		finalUnits = (long)num;
		return (int)(finalUnits - currentUnits);
	}

	private int ApplyDirectTrustDeltaUnits(string trustKey, int currentTrust, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || rawDelta == 0)
		{
			return 0;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		return ApplyProgressiveTrustDeltaUnits(_directTrustProgressCarry, text, currentTrust, rawDelta, out appliedUnits);
	}

	private int ApplyExactDirectTrustDeltaUnits(string trustKey, int currentTrust, int requestedUnits, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || requestedUnits == 0)
		{
			return 0;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_directTrustProgressCarry.TryGetValue(text, out var carryUnits);
		long currentUnits = GetTrustTotalUnitsWithCarry(currentTrust, carryUnits);
		long minUnits = (long)TrustMin * TrustGainUnitsPerPoint;
		long maxUnits = (long)TrustMax * TrustGainUnitsPerPoint;
		long finalUnits = Math.Max(minUnits, Math.Min(maxUnits, currentUnits + requestedUnits));
		appliedUnits = (int)(finalUnits - currentUnits);
		int finalTrust = (int)(finalUnits / TrustGainUnitsPerPoint);
		int finalCarry = (int)(finalUnits % TrustGainUnitsPerPoint);
		if (finalCarry == 0)
		{
			_directTrustProgressCarry.Remove(text);
		}
		else
		{
			_directTrustProgressCarry[text] = finalCarry;
		}
		return finalTrust - currentTrust;
	}

	private int ApplySettlementTrustUnits(Settlement settlement, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || rawDelta == 0)
		{
			return 0;
		}
		string text = BuildSettlementTrustCarryKey(settlement);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
		return ApplyProgressiveTrustDeltaUnits(_settlementTrustCentiCarry, text, settlementLocalPublicTrust, rawDelta, out appliedUnits);
	}

	private void ApplySettlementLocalTrustWholeDeltaDirect(Settlement settlement, int localTrustDelta, string reason)
	{
		if (settlement == null || localTrustDelta == 0)
		{
			return;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementPublicTrustKey = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementPublicTrustKey))
		{
			return;
		}
		int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
		int num = ClampTrust(settlementLocalPublicTrust + localTrustDelta);
		if (num == 0)
		{
			_publicTrust.Remove(settlementPublicTrustKey);
		}
		else
		{
			_publicTrust[settlementPublicTrustKey] = num;
		}
		Logger.Log("Trust", $"settlement={settlement.StringId} reason={reason} settlementTrust={settlementLocalPublicTrust}->{num} delta={localTrustDelta}");
	}

	private static string FormatTrustUnits(int units)
	{
		decimal d = (decimal)units / (decimal)TrustGainUnitsPerPoint;
		string text = d.ToString("0.######");
		if (text == "-0")
		{
			return "0";
		}
		return text;
	}

	private int AccumulateTradeTrustValueByKey(string carryKey, int addedValue)
	{
		string text = (carryKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || addedValue <= 0)
		{
			return 0;
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_tradeTrustValueCarry.TryGetValue(text, out var value);
		long num = Math.Max(0, value);
		long num2 = num + (long)addedValue;
		int num3 = ClampPositiveLongToInt(num2 / AutoTrustValuePerPoint);
		int num4 = (int)(num2 % AutoTrustValuePerPoint);
		if (num4 > 0)
		{
			_tradeTrustValueCarry[text] = num4;
		}
		else
		{
			_tradeTrustValueCarry.Remove(text);
		}
		return num3;
	}

	private int GetItemTrustValueForHeroGift(Hero hero, ItemObject item, int amount)
	{
		return ClampPositiveLongToInt(GetItemGuideValueForHeroGift(hero, item, amount));
	}

	private int GetItemTrustValueForMerchantGift(Settlement settlement, ItemObject item, int amount)
	{
		return ClampPositiveLongToInt(GetItemGuideValueForMerchantGift(settlement, item, amount));
	}

	private static bool IsPrisonerTrustGainBlocked(Hero npc)
	{
		return npc != null && npc.IsPrisoner;
	}

	private void ApplyAutoTrustGainFromHeroGiftValue(Hero giver, int addedValue, List<string> giverFacts, List<string> receiverFacts, string giverName)
	{
		if (giver == null || addedValue <= 0)
		{
			return;
		}
		if (IsPrisonerTrustGainBlocked(giver))
		{
			Logger.Log("Trust", $"npc={giver.StringId} reason=auto_gift_value_accumulated blocked=prisoner addedValue={addedValue}");
			return;
		}
		int num = AccumulateTradeTrustValueByKey(NormalizeHeroId(giver), addedValue);
		if (num <= 0)
		{
			return;
		}
		int num2 = AdjustTrust(giver, num, 0, "auto_gift_value_accumulated", out var appliedUnits);
		string text = (num2 > 0) ? $"，公共信任提升 {num2}" : "";
		string text2 = FormatTrustUnits(appliedUnits);
		giverFacts?.Add($"你因累计向玩家实际交付的价值达到阈值，对玩家的个人信任提升了 {text2}{text}。");
		receiverFacts?.Add($"{giverName} 因累计向你实际交付的价值达到阈值，对你的个人信任提升了 {text2}{text}。");
		string message = $"【信任变化】{giverName} 因累计向你实际交付的价值，对你的个人信任 +{text2}" + ((num2 > 0) ? $"，公共信任 +{num2}" : "");
		InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(4278242559u)));
		ShowRewardQuickInfo(message, giver);
	}

	private void ApplyAutoTrustGainFromMerchantGiftValue(Settlement settlement, SettlementMerchantKind kind, int addedValue, List<string> merchantFacts, List<string> playerFacts, string giverName, BasicCharacterObject giverCharacter = null)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || addedValue <= 0)
		{
			return;
		}
		int num = AccumulateTradeTrustValueByKey(BuildSettlementMerchantTrustKey(settlement, kind), addedValue);
		if (num <= 0)
		{
			return;
		}
		int num2 = AdjustSettlementMerchantTrust(settlement, kind, num, "merchant_auto_gift_value_accumulated", out var appliedUnits);
		string settlementMerchantDebtLabel = BuildSettlementMerchantDebtLabel(settlement, kind);
		string text = (num2 > 0) ? $"，公共信任提升 {num2}" : "";
		string text2 = FormatTrustUnits(appliedUnits);
		merchantFacts?.Add($"你因累计向玩家实际交付的价值达到阈值，对玩家的市场信任提升了 {text2}{text}。");
		playerFacts?.Add($"{giverName} 代表的{settlementMerchantDebtLabel}因累计向你实际交付的价值达到阈值，对你的市场信任提升了 {text2}{text}。");
		string message = $"【市场信任变化】{settlementMerchantDebtLabel} 对你的市场信任 +{text2}" + ((num2 > 0) ? $"，公共信任 +{num2}" : "");
		ShowRewardMessage(message, Color.FromUint(4278242559u), giverCharacter);
	}

	private static int TruncateDivisionTowardsZero(int dividend, int divisor, out int remainder)
	{
		remainder = 0;
		if (divisor == 0)
		{
			return 0;
		}
		int num = dividend / divisor;
		remainder = dividend % divisor;
		return num;
	}

	private int AccumulatePublicTrustProgressByKey(string publicTrustKey, int sourceUnits)
	{
		string text = (publicTrustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || sourceUnits == 0)
		{
			return 0;
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_publicTrustProgressCarry.TryGetValue(text, out var value);
		int num = value + sourceUnits;
		int num2 = TruncateDivisionTowardsZero(num, PublicTrustPoolPointsPerTrust * TrustGainUnitsPerPoint, out var remainder);
		if (remainder != 0)
		{
			_publicTrustProgressCarry[text] = remainder;
		}
		else
		{
			_publicTrustProgressCarry.Remove(text);
		}
		return num2;
	}

	private int AdjustPublicTrustByKey(string publicTrustKey, int publicDelta, string reason)
	{
		string text = (publicTrustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || publicDelta == 0)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		int num = 0;
		_publicTrust.TryGetValue(text, out num);
		num = ClampTrust(num);
		int num2 = ClampTrust(num + publicDelta);
		if (num2 == 0)
		{
			_publicTrust.Remove(text);
		}
		else
		{
			_publicTrust[text] = num2;
		}
		Logger.Log("Trust", $"publicKey={text} reason={reason} publicTrust={num}->{num2} delta={publicDelta}");
		return num2 - num;
	}

	private int ApplyPublicTrustPoolDeltaByKey(string publicTrustKey, int sourceUnits, string reason)
	{
		int num = AccumulatePublicTrustProgressByKey(publicTrustKey, sourceUnits);
		if (num == 0)
		{
			return 0;
		}
		return AdjustPublicTrustByKey(publicTrustKey, num, reason);
	}

	private static string BuildSettlementTrustCarryKey(Settlement settlement)
	{
		return BuildSettlementLocalPublicTrustKey(settlement);
	}

	private int AccumulateSettlementTrustCenti(Settlement settlement, int centiDelta)
	{
		string text = BuildSettlementTrustCarryKey(settlement);
		if (string.IsNullOrWhiteSpace(text) || centiDelta == 0)
		{
			return 0;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_settlementTrustCentiCarry.TryGetValue(text, out var value);
		int num = value + centiDelta;
		int num2 = TruncateDivisionTowardsZero(num, TrustGainUnitsPerPoint, out var remainder);
		if (remainder != 0)
		{
			_settlementTrustCentiCarry[text] = remainder;
		}
		else
		{
			_settlementTrustCentiCarry.Remove(text);
		}
		return num2;
	}

	private static int ComputeSettlementTrustCentiForTroop(CharacterObject troop, int count)
	{
		if (troop == null || count <= 0)
		{
			return 0;
		}
		int num = Math.Max(1, troop.Tier);
		return SettlementTrustUnitsPerTier * num * count;
	}

	private void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
	{
		try
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			if (playerMapEvent == null || !playerMapEvent.IsPlayerMapEvent)
			{
				return;
			}
			_currentBattlePlayerActualSettlementTrustUnits += ComputeSettlementTrustCentiForTroop(strikedTroop, 1);
		}
		catch
		{
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
	{
		try
		{
			// Debt promises already apply their own repayment/penalty rules and must not receive the generic quest trust reward.
			if (quest is DebtPromiseQuest)
			{
				return;
			}
			if (quest == null || details != QuestBase.QuestCompleteDetails.Success)
			{
				return;
			}
			Hero hero = null;
			try
			{
				hero = quest.QuestGiver;
			}
			catch
			{
				hero = null;
			}
			if (hero == null)
			{
				return;
			}
			if (IsPrisonerTrustGainBlocked(hero))
			{
				Logger.Log("Trust", $"quest={quest.StringId} giver={hero.StringId} completed=success trustGainBlocked=prisoner");
				return;
			}
			int num = AdjustTrust(hero, TrustGainOnQuestSuccess, 0, "quest_completed_success", out var appliedUnits);
			string text = FormatTrustUnits(appliedUnits);
			string text2 = hero.Name?.ToString() ?? "任务发布人";
			Logger.Log("Trust", $"quest={quest.StringId} giver={hero.StringId} completed=success personalGain={text} publicGain={num}");
			string message = $"【信任变化】完成{text2}交付的任务，个人信任 +{text}" + ((num > 0) ? $"，公共信任 +{num}" : "");
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(4278242559u)));
			ShowRewardQuickInfo(message, hero);
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[ERROR] quest completion trust reward failed: " + ex);
		}
	}

	private static bool IsPartyHostileForSettlementTrust(PartyBase party, Settlement settlement)
	{
		if (party == null || settlement == null)
		{
			return false;
		}
		try
		{
			if (party.MobileParty != null && party.MobileParty.IsBandit)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = party.MapFaction;
			IFaction mapFaction2 = settlement.MapFaction;
			return mapFaction != null && mapFaction2 != null && mapFaction.IsAtWarWith(mapFaction2);
		}
		catch
		{
			return false;
		}
	}

	private static IEnumerable<Settlement> GetNearbySettlementsForTrust(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return Enumerable.Empty<Settlement>();
		}
		float num = SettlementTrustBattleEffectRadius;
		float num2 = num * num;
		return Settlement.All.Where((Settlement x) => x != null && !x.IsHideout && x.Position.DistanceSquared(mapEvent.Position) < num2).ToList();
	}

	private static int ComputeSettlementTrustCentiFromRoster(TroopRoster roster)
	{
		if (roster == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			num += ComputeSettlementTrustCentiForTroop(elementCopyAtIndex.Character, elementCopyAtIndex.Number);
		}
		return num;
	}

	private static int ComputeSettlementTrustCentiFromBattleRosters(MapEventParty party, bool includeSurrenderedActiveTroops)
	{
		if (party == null)
		{
			return 0;
		}
		int num = ComputeSettlementTrustCentiFromRoster(party.DiedInBattle) + ComputeSettlementTrustCentiFromRoster(party.WoundedInBattle);
		if (!includeSurrenderedActiveTroops || party.Troops == null)
		{
			return num;
		}
		foreach (FlattenedTroopRosterElement troop in party.Troops)
		{
			if (troop.Troop != null && troop.State == RosterTroopState.Active)
			{
				num += ComputeSettlementTrustCentiForTroop(troop.Troop, 1);
			}
		}
		return num;
	}

	private int ComputeSettlementTrustCentiFromDefeatedHostileTroops(MapEvent mapEvent, Settlement settlement)
	{
		if (mapEvent == null || settlement == null || !mapEvent.HasWinner)
		{
			return 0;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
		if (mapEventSide?.Parties == null)
		{
			return 0;
		}
		bool flag = IsMapEventSideSurrendered(mapEventSide);
		int num = 0;
		foreach (MapEventParty party in mapEventSide.Parties)
		{
			if (party?.Party == null || !IsPartyHostileForSettlementTrust(party.Party, settlement))
			{
				continue;
			}
			num += ComputeSettlementTrustCentiFromBattleRosters(party, flag);
		}
		return num;
	}

	private static bool IsMapEventSideSurrendered(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			var field = typeof(MapEventSide).GetField("IsSurrendered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
			{
				return (bool)field.GetValue(side);
			}
		}
		catch
		{
		}
		return false;
	}

	private int ComputePlayerContributionSharePercentForWinningSide(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.HasWinner)
		{
			return 0;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.WinningSide);
		if (mapEventSide?.Parties == null)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		foreach (MapEventParty party in mapEventSide.Parties)
		{
			if (party == null)
			{
				continue;
			}
			int num3 = Math.Max(0, party.ContributionToBattle);
			num += num3;
			if (party.Party == PartyBase.MainParty)
			{
				num2 = num3;
			}
		}
		if (num <= 0 || num2 <= 0)
		{
			return 0;
		}
		return Math.Max(0, Math.Min(100, (int)Math.Round((double)(num2 * 100) / (double)num, MidpointRounding.AwayFromZero)));
	}

	private int AdjustSettlementLocalTrustInternal(Settlement settlement, int localTrustDelta, string reason)
	{
		if (settlement == null || localTrustDelta == 0)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementPublicTrustKey = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementPublicTrustKey))
		{
			return 0;
		}
		int settlementPublicTrust = GetSettlementLocalPublicTrust(settlement);
		int appliedUnits;
		int num2 = ApplySettlementTrustUnits(settlement, localTrustDelta, out appliedUnits);
		int num = ClampTrust(settlementPublicTrust + num2);
		ApplySettlementLocalTrustWholeDeltaDirect(settlement, num2, reason);
		int num3 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "external") + "_local_public_pool");
		Logger.Log("Trust", $"settlement={settlement.StringId} reason={reason} settlementTrust={settlementPublicTrust}->{num} rawDelta={localTrustDelta} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={num3}");
		return num3;
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			int currentBattlePlayerActualSettlementTrustCenti = _currentBattlePlayerActualSettlementTrustUnits;
			_currentBattlePlayerActualSettlementTrustUnits = 0;
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner)
			{
				return;
			}
			if (mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗未获胜，未获得定居点信任。", Color.FromUint(4294945365u)));
				return;
			}
			List<Settlement> nearbySettlements = GetNearbySettlementsForTrust(mapEvent).ToList();
			if (nearbySettlements.Count <= 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗附近没有可受影响的定居点。", Color.FromUint(4291611750u)));
				return;
			}
			List<string> list = new List<string>();
			int num = ComputePlayerContributionSharePercentForWinningSide(mapEvent);
			foreach (Settlement item in nearbySettlements)
			{
				int num2 = ComputeSettlementTrustCentiFromDefeatedHostileTroops(mapEvent, item);
				int num3 = Math.Max(0, currentBattlePlayerActualSettlementTrustCenti);
				int num4 = 0;
				if (num3 <= 0 && num > 0 && num2 > 0)
				{
					// Fallback for battles where the engine does not emit per-kill events reliably.
					num4 = (int)Math.Round((double)(num2 * num) / 100.0, MidpointRounding.AwayFromZero);
				}
				int num5 = Math.Max(num3, num4);
				int num6 = Math.Max(0, num2 - num5);
				int num7 = 0;
				if (num > 0 && num6 > 0)
				{
					num7 = (int)Math.Round((double)(num6 * num * SettlementTrustContributionSharePercent) / 10000.0, MidpointRounding.AwayFromZero);
				}
				string settlementTrustCarryKey = BuildSettlementTrustCarryKey(item);
				int num8 = 0;
				if (!string.IsNullOrWhiteSpace(settlementTrustCarryKey) && _settlementTrustCentiCarry != null)
				{
					_settlementTrustCentiCarry.TryGetValue(settlementTrustCarryKey, out num8);
				}
				long trustTotalUnitsWithCarry = GetTrustTotalUnitsWithCarry(GetSettlementLocalPublicTrust(item), num8);
				num5 = ApplyPositiveTrustSourceUnitsProgressively(trustTotalUnitsWithCarry, num5, out trustTotalUnitsWithCarry);
				num7 = ApplyPositiveTrustSourceUnitsProgressively(trustTotalUnitsWithCarry, num7, out trustTotalUnitsWithCarry);
				int num9 = num5 + num7;
				if (num9 <= 0)
				{
					continue;
				}
				int num10 = AccumulateSettlementTrustCenti(item, num9);
				int num11 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(item), num9, "battle_hostile_party_defeated_local_public_pool");
				string text = (num4 > 0) ? "估算实击" : "实击";
				if (num10 != 0)
				{
					ApplySettlementLocalTrustWholeDeltaDirect(item, num10, "battle_hostile_party_defeated");
					list.Add($"{item.Name}: 定居点信任 +{num10}" + ((num11 > 0) ? $"，公共信任 +{num11}" : "") + $"\n{text} {FormatTrustUnits(num5)}，分成 {FormatTrustUnits(num7)}，本次累计 {FormatTrustUnits(num9)}");
				}
				else
				{
					list.Add($"{item.Name}: 定居点信任累计 +{FormatTrustUnits(num9)}" + ((num11 > 0) ? $"，公共信任 +{num11}" : "") + $"\n{text} {FormatTrustUnits(num5)}，分成 {FormatTrustUnits(num7)}，未满1点");
				}
			}
			if (list.Count > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】\n" + string.Join("\n\n", list), Color.FromUint(4278242559u)));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗未击败附近定居点的敌对部队，因此没有获得定居点信任。", Color.FromUint(4291611750u)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[ERROR] OnMapEventEnded settlement trust failed: " + ex);
		}
	}

	private static bool TryResolveSettlementByLocalPublicTrustKey(string key, out Settlement settlement)
	{
		settlement = null;
		string text = (key ?? "").Trim();
		const string text2 = "public:settlement:";
		if (!text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text3 = text.Substring(text2.Length).Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			return false;
		}
		try
		{
			settlement = Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text3, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			settlement = null;
		}
		return settlement != null;
	}

	private void MigrateLegacySettlementSharedPublicCarryToUnifiedPool()
	{
		if (_settlementTrustSharedPublicCarry == null || _settlementTrustSharedPublicCarry.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item in _settlementTrustSharedPublicCarry.ToList())
		{
			if (item.Value == 0 || !TryResolveSettlementByLocalPublicTrustKey(item.Key, out var settlement))
			{
				continue;
			}
			string settlementSharedPublicTrustKey = BuildSettlementSharedPublicTrustKey(settlement);
			if (!string.IsNullOrWhiteSpace(settlementSharedPublicTrustKey))
			{
				ApplyPublicTrustPoolDeltaByKey(settlementSharedPublicTrustKey, item.Value * TrustGainUnitsPerPoint, "legacy_settlement_public_pool_migration");
			}
		}
		_settlementTrustSharedPublicCarry.Clear();
	}

	private static int ClampTrust(int value)
	{
		return EconomyTrustPolicy.Clamp(value);
	}

	public static int GetTrustLevelIndex(int trust)
	{
		return EconomyTrustPolicy.GetLevelIndex(trust);
	}

	public static string GetTrustLevelText(int trust)
	{
		return EconomyTrustPolicy.GetLevelText(trust);
	}

	public static string GetTrustBehaviorText(int trust)
	{
		return EconomyTrustPolicy.GetBehaviorText(trust);
	}

	public static string GetTrustActionGuideText(int trust)
	{
		return EconomyTrustPolicy.GetActionGuideText(trust);
	}

	private static string BuildNpcTrustKey(Hero npc)
	{
		string text = (npc?.StringId ?? "").Trim().ToLower();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	private static string BuildPublicTrustKey(Hero npc)
	{
		string text = "";
		try
		{
			text = (npc?.MapFaction?.StringId ?? "").Trim().ToLower();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Clan?.Kingdom?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Clan?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Culture?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:" + text;
	}

	private static string BuildSettlementLocalPublicTrustKey(Settlement settlement)
	{
		string text = (settlement?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:settlement:" + text;
	}

	private static string BuildSettlementPublicTrustKey(Settlement settlement)
	{
		return BuildSettlementLocalPublicTrustKey(settlement);
	}

	private static string BuildSettlementSharedPublicTrustKey(Settlement settlement)
	{
		string text = "";
		try
		{
			text = (settlement?.MapFaction?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (settlement?.OwnerClan?.Kingdom?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (settlement?.OwnerClan?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (settlement?.Culture?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:" + text;
	}

	private static string BuildSettlementFactionPublicTrustKey(Settlement settlement)
	{
		return BuildSettlementSharedPublicTrustKey(settlement);
	}

	private static string BuildPublicTrustLabel(Hero npc)
	{
		try
		{
			string text = npc?.Clan?.Kingdom?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		try
		{
			string text2 = npc?.MapFaction?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.Trim();
			}
		}
		catch
		{
		}
		try
		{
			string text3 = npc?.Clan?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3.Trim();
			}
		}
		catch
		{
		}
		return "其所属势力";
	}

	public int GetNpcTrust(Hero npc)
	{
		if (npc == null)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_npcTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetPublicTrust(Hero npc)
	{
		if (npc == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string text = BuildPublicTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetEffectiveTrust(Hero npc)
	{
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		return ClampTrust(npcTrust + publicTrust);
	}

	public int GetSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind)
	{
		if (settlement == null || kind == SettlementMerchantKind.None)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_npcTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	private int AdjustSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || kind == SettlementMerchantKind.None || personalDelta == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int settlementMerchantTrust = GetSettlementMerchantTrust(settlement, kind);
		int num2 = ApplyDirectTrustDeltaUnits(text, settlementMerchantTrust, personalDelta, out appliedUnits);
		int num = ClampTrust(settlementMerchantTrust + num2);
		if (num == 0)
		{
			_npcTrust.Remove(text);
		}
		else
		{
			_npcTrust[text] = num;
		}
		int num3 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "merchant") + "_public_pool");
		Logger.Log("Trust", $"settlement={settlement.StringId} market={kind} reason={reason} trust={settlementMerchantTrust}->{num} rawDelta={personalDelta} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={num3}");
		return num3;
	}

	private int AdjustSettlementMerchantTrustByExactUnits(Settlement settlement, SettlementMerchantKind kind, int personalUnits, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || kind == SettlementMerchantKind.None || personalUnits == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string trustKey = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(trustKey))
		{
			return 0;
		}
		int trustBefore = GetSettlementMerchantTrust(settlement, kind);
		int wholeDelta = ApplyExactDirectTrustDeltaUnits(trustKey, trustBefore, personalUnits, out appliedUnits);
		int trustAfter = ClampTrust(trustBefore + wholeDelta);
		if (trustAfter == 0)
		{
			_npcTrust.Remove(trustKey);
		}
		else
		{
			_npcTrust[trustKey] = trustAfter;
		}
		int publicDelta = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "merchant_exact_units") + "_public_pool");
		Logger.Log("Trust", $"settlement={settlement.StringId} market={kind} reason={reason} trust={trustBefore}->{trustAfter} requestedUnits={personalUnits} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={publicDelta}");
		return publicDelta;
	}

	public string BuildTrustStatusInlineForAI(Hero npc)
	{
		if (npc == null)
		{
			return EconomyPromptProjection.BuildTrustStatus(0, "中性观望", 6);
		}
		int effectiveTrust = GetEffectiveTrust(npc);
		return EconomyPromptProjection.BuildTrustStatus(
			effectiveTrust,
			GetTrustLevelText(effectiveTrust),
			GetTrustLevelIndex(effectiveTrust));
	}

	public string BuildTrustPromptForAI(Hero npc)
	{
		int effectiveTrust = GetEffectiveTrust(npc);
		return EconomyPromptProjection.BuildTrustPrompt(
			GetTrustBehaviorText(effectiveTrust),
			GetTrustActionGuideText(effectiveTrust));
	}

	public int GetSettlementTransferTalkTrust(Hero npc)
	{
		return GetEffectiveTrust(npc);
	}

	private int AdjustTrust(Hero npc, int personalDelta, int publicDelta, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (npc == null)
		{
			return 0;
		}
		if (IsPrisonerTrustGainBlocked(npc) && (personalDelta > 0 || publicDelta > 0))
		{
			int blockedPersonalDelta = Math.Max(0, personalDelta);
			int blockedPublicDelta = Math.Max(0, publicDelta);
			personalDelta = Math.Min(0, personalDelta);
			publicDelta = Math.Min(0, publicDelta);
			Logger.Log("Trust", $"npc={npc.StringId} reason={reason} blocked=prisoner positivePersonalDelta={blockedPersonalDelta} positivePublicDelta={blockedPublicDelta}");
			if (personalDelta == 0 && publicDelta == 0)
			{
				return 0;
			}
		}
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		int num = npcTrust;
		int num2 = publicTrust;
		string text = BuildPublicTrustKey(npc);
		if (personalDelta != 0)
		{
			if (_npcTrust == null)
			{
				_npcTrust = new Dictionary<string, int>();
			}
			string text2 = BuildNpcTrustKey(npc);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				int num6 = ApplyDirectTrustDeltaUnits(text2, npcTrust, personalDelta, out appliedUnits);
				num = ClampTrust(npcTrust + num6);
				if (num == 0)
				{
					_npcTrust.Remove(text2);
				}
				else
				{
					_npcTrust[text2] = num;
				}
			}
		}
		int num3 = 0;
		if (personalDelta != 0)
		{
			num3 += ApplyPublicTrustPoolDeltaByKey(text, appliedUnits, (reason ?? "external") + "_public_pool");
			num2 = GetPublicTrust(npc);
		}
		if (publicDelta != 0)
		{
			num3 += AdjustPublicTrustByKey(text, publicDelta, (reason ?? "external") + "_direct");
			num2 = GetPublicTrust(npc);
		}
		int num4 = ClampTrust(npcTrust + publicTrust);
		int num5 = ClampTrust(num + num2);
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={npcTrust}->{num} rawDelta={personalDelta} appliedDelta={FormatTrustUnits(appliedUnits)} public={publicTrust}->{num2} deltaPublic={num3} requestedPublicDelta={publicDelta} effective={num4}->{num5}");
		Logger.Obs("Trust", "change", new Dictionary<string, object>
		{
			["npcId"] = npc.StringId ?? "",
			["reason"] = reason ?? "",
			["personalBefore"] = npcTrust,
			["personalAfter"] = num,
			["publicBefore"] = publicTrust,
			["publicAfter"] = num2,
			["effectiveBefore"] = num4,
			["effectiveAfter"] = num5,
			["personalDelta"] = personalDelta,
			["appliedPersonalDelta"] = FormatTrustUnits(appliedUnits),
			["publicDelta"] = num3,
			["requestedPublicDelta"] = publicDelta
		});
		Logger.Metric("trust.change");
		return num3;
	}

	private int AdjustTrustByExactUnits(Hero npc, int personalUnits, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (npc == null || personalUnits == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string trustKey = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(trustKey))
		{
			return 0;
		}
		int trustBefore = GetNpcTrust(npc);
		int wholeDelta = ApplyExactDirectTrustDeltaUnits(trustKey, trustBefore, personalUnits, out appliedUnits);
		int trustAfter = ClampTrust(trustBefore + wholeDelta);
		if (trustAfter == 0)
		{
			_npcTrust.Remove(trustKey);
		}
		else
		{
			_npcTrust[trustKey] = trustAfter;
		}
		int publicDelta = ApplyPublicTrustPoolDeltaByKey(BuildPublicTrustKey(npc), appliedUnits, (reason ?? "exact_units") + "_public_pool");
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={trustBefore}->{trustAfter} requestedUnits={personalUnits} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={publicDelta}");
		return publicDelta;
	}

	public int AdjustPersonalTrustWholeDeltaForExternal(Hero npc, int exactDelta, string reason = "external_direct_whole")
	{
		if (npc == null || exactDelta == 0)
		{
			return 0;
		}
		if (exactDelta > 0 && IsPrisonerTrustGainBlocked(npc))
		{
			Logger.Log("Trust", $"npc={npc.StringId} reason={reason} blocked=prisoner positiveExactDelta={exactDelta}");
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		int num = ClampTrust(npcTrust + exactDelta);
		int num2 = num - npcTrust;
		if (num2 == 0)
		{
			return 0;
		}
		if (num == 0)
		{
			_npcTrust.Remove(text);
		}
		else
		{
			_npcTrust[text] = num;
		}
		int num3 = ClampTrust(npcTrust + publicTrust);
		int num4 = ClampTrust(num + publicTrust);
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={npcTrust}->{num} exactDelta={exactDelta} appliedExactDelta={num2} public={publicTrust}->{publicTrust} effective={num3}->{num4}");
		Logger.Obs("Trust", "change", new Dictionary<string, object>
		{
			["npcId"] = npc.StringId ?? "",
			["reason"] = reason ?? "",
			["personalBefore"] = npcTrust,
			["personalAfter"] = num,
			["publicBefore"] = publicTrust,
			["publicAfter"] = publicTrust,
			["effectiveBefore"] = num3,
			["effectiveAfter"] = num4,
			["personalDelta"] = exactDelta,
			["appliedPersonalDelta"] = num2.ToString(),
			["publicDelta"] = 0,
			["requestedPublicDelta"] = 0
		});
		Logger.Metric("trust.change");
		return num2;
	}

	public void AdjustTrustForExternal(Hero npc, int personalDelta, int publicDelta, string reason = "external")
	{
		AdjustTrust(npc, personalDelta, publicDelta, reason ?? "external", out _);
	}

	public void AdjustSettlementMerchantTrustForExternal(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason = "external")
	{
		AdjustSettlementMerchantTrust(settlement, kind, personalDelta, reason ?? "external", out _);
	}

	public int GetSettlementLocalPublicTrust(Settlement settlement)
	{
		if (settlement == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementPublicTrustKey = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementPublicTrustKey))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(settlementPublicTrustKey, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetSettlementPublicTrust(Settlement settlement)
	{
		return GetSettlementLocalPublicTrust(settlement);
	}

	public int GetSettlementSharedPublicTrust(Settlement settlement)
	{
		if (settlement == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementSharedPublicTrustKey = BuildSettlementSharedPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementSharedPublicTrustKey))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(settlementSharedPublicTrustKey, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetSettlementFactionPublicTrust(Settlement settlement)
	{
		return GetSettlementSharedPublicTrust(settlement);
	}

	public int GetSettlementMerchantEffectiveTrust(Settlement settlement, SettlementMerchantKind kind)
	{
		return ClampTrust(GetSettlementMerchantTrust(settlement, kind) + GetSettlementLocalPublicTrust(settlement) + GetSettlementSharedPublicTrust(settlement));
	}

	public void AdjustSettlementLocalPublicTrustForExternal(Settlement settlement, int publicDelta, string reason = "external")
	{
		AdjustSettlementLocalTrustInternal(settlement, publicDelta, reason);
	}

	public void AdjustSettlementPublicTrustForExternal(Settlement settlement, int publicDelta, string reason = "external")
	{
		AdjustSettlementLocalPublicTrustForExternal(settlement, publicDelta, reason);
	}

	private void AdjustSettlementSharedPublicTrust(Settlement settlement, int publicDelta, string reason)
	{
		if (settlement == null || publicDelta == 0)
		{
			return;
		}
		string settlementSharedPublicTrustKey = BuildSettlementSharedPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementSharedPublicTrustKey))
		{
			return;
		}
		AdjustPublicTrustByKey(settlementSharedPublicTrustKey, publicDelta, reason);
	}

	private void AdjustSettlementFactionPublicTrust(Settlement settlement, int publicDelta, string reason)
	{
		AdjustSettlementSharedPublicTrust(settlement, publicDelta, reason);
	}
}
