using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SettlementTransferPromptEntry = AnimusForge.MyBehavior.SettlementTransferPromptEntry;
using PartyTransferEntrySection = AnimusForge.MyBehavior.PartyTransferEntrySection;
using PartyTransferPromptEntry = AnimusForge.MyBehavior.PartyTransferPromptEntry;
namespace AnimusForge;

internal static class PartyTransferProjectionOwner
{
	internal static long CalculatePartyTransferTotalValueForExternal(IEnumerable<PartyTransferPromptEntry> entries, bool isPrisoner)
	{
		long total = 0L;
		foreach (PartyTransferPromptEntry entry in entries ?? Enumerable.Empty<PartyTransferPromptEntry>())
		{
			if (entry == null || entry.Character == null || entry.Count <= 0)
			{
				continue;
			}
			int unitValue = isPrisoner ? Math.Max(1, entry.BuyPriceDenarsPerUnit) : Math.Max(1, entry.HirePriceDenarsPerUnit);
			total = TransferQuantitySpec.AddProduct(total, entry.IsHero ? 1 : entry.Count, unitValue);
		}
		return total;
	}

	internal static int ResolvePartyTransferRecruitMaxTier(int trustLevelIndex)
	{
		if (trustLevelIndex <= 4)
		{
			return 0;
		}
		if (trustLevelIndex == 5)
		{
			return 1;
		}
		if (trustLevelIndex == 6)
		{
			return 2;
		}
		if (trustLevelIndex == 7)
		{
			return 3;
		}
		if (trustLevelIndex == 8)
		{
			return 4;
		}
		return int.MaxValue;
	}

	internal static List<PartyTransferPromptEntry> BuildDisplayIndexedPartyTransferEntries(IEnumerable<PartyTransferPromptEntry> entries)
	{
		List<PartyTransferPromptEntry> list = new List<PartyTransferPromptEntry>();
		int num = 1;
		foreach (PartyTransferPromptEntry entry in entries ?? Enumerable.Empty<PartyTransferPromptEntry>())
		{
			if (entry == null)
			{
				continue;
			}
			list.Add(new PartyTransferPromptEntry
			{
				PromptIndex = num++,
				Section = entry.Section,
				Character = entry.Character,
				DisplayName = entry.DisplayName,
				Count = entry.Count,
				WoundedCount = entry.WoundedCount,
				WageDenarsPerDay = entry.WageDenarsPerDay,
				HirePriceDenarsPerUnit = entry.HirePriceDenarsPerUnit,
				BuyPriceDenarsPerUnit = entry.BuyPriceDenarsPerUnit,
				IsHero = entry.IsHero,
				OwnerParty = entry.OwnerParty,
				SourceSettlement = entry.SourceSettlement,
				VolunteerOwner = entry.VolunteerOwner,
				VolunteerSlotIndices = entry.VolunteerSlotIndices == null ? null : new List<int>(entry.VolunteerSlotIndices)
			});
		}
		return list;
	}
	internal static List<SettlementTransferPromptEntry> BuildDisplayIndexedSettlementTransferEntries(IEnumerable<SettlementTransferPromptEntry> entries, Func<SettlementTransferPromptEntry, bool> isValid)
	{
		List<SettlementTransferPromptEntry> list = new List<SettlementTransferPromptEntry>();
		int num = 1;
		foreach (SettlementTransferPromptEntry entry in entries ?? Enumerable.Empty<SettlementTransferPromptEntry>())
		{
			if (!isValid(entry))
			{
				continue;
			}
			list.Add(new SettlementTransferPromptEntry
			{
				PromptIndex = num++,
				Section = entry.Section,
				AssetKind = entry.AssetKind,
				Settlement = entry.Settlement,
				Workshop = entry.Workshop,
				CaravanParty = entry.CaravanParty,
				OwnerHero = entry.OwnerHero,
				SettlementId = entry.SettlementId,
				AssetId = entry.AssetId,
				DisplayName = entry.DisplayName,
				TypeLabel = entry.TypeLabel,
				DailyIncomeDenars = entry.DailyIncomeDenars,
				GuidePriceDenars = entry.GuidePriceDenars,
				OwnerClan = entry.OwnerClan
			});
		}
		return list;
	}

	internal static SettlementTransferPromptEntry FindSettlementTransferEntryByToken(IEnumerable<SettlementTransferPromptEntry> entries, string token, Func<SettlementTransferPromptEntry, bool> isValid, Func<SettlementTransferPromptEntry, string> assetId)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		List<SettlementTransferPromptEntry> list = (entries ?? Enumerable.Empty<SettlementTransferPromptEntry>()).Where(isValid).ToList();
		SettlementTransferPromptEntry settlementTransferPromptEntry = list.FirstOrDefault((SettlementTransferPromptEntry x) => string.Equals(assetId(x), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		settlementTransferPromptEntry = list.FirstOrDefault((SettlementTransferPromptEntry x) => string.Equals((x.SettlementId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		settlementTransferPromptEntry = list.FirstOrDefault((SettlementTransferPromptEntry x) => string.Equals((x.DisplayName ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		List<SettlementTransferPromptEntry> list2 = list.Where((SettlementTransferPromptEntry x) => !string.IsNullOrWhiteSpace(x.DisplayName) && x.DisplayName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list2.Count == 1)
		{
			return list2[0];
		}
		list2 = list.Where((SettlementTransferPromptEntry x) => !string.IsNullOrWhiteSpace(assetId(x)) && assetId(x).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list2.Count == 1)
		{
			return list2[0];
		}
		if (int.TryParse(text, out var result) && result > 0)
		{
			return list.FirstOrDefault((SettlementTransferPromptEntry x) => x.PromptIndex == result) ?? list.Skip(result - 1).FirstOrDefault();
		}
		return null;
	}

	internal static void AppendSettlementTransferPromptSection(StringBuilder sb, string header, IEnumerable<SettlementTransferPromptEntry> entries, bool showPromptIndex, Func<SettlementTransferPromptEntry, bool> isValid, Func<SettlementTransferPromptEntry, string> assetId)
	{
		if (sb == null)
		{
			return;
		}
		sb.AppendLine(header);
		List<SettlementTransferPromptEntry> list = (entries ?? Enumerable.Empty<SettlementTransferPromptEntry>()).Where(isValid).ToList();
		if (list.Count == 0)
		{
			sb.AppendLine(EconomyPromptTexts.Get("S643"));
			return;
		}
		foreach (SettlementTransferPromptEntry item in list)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (showPromptIndex && item.PromptIndex > 0)
			{
				stringBuilder.Append(item.PromptIndex).Append(". ");
			}
			stringBuilder.Append(EconomyPromptTexts.ProjectPartyAssetName(item.DisplayName))
				.Append(" | ID ").Append(string.IsNullOrWhiteSpace(assetId(item)) ? EconomyPromptTexts.Get("S624") : assetId(item))
				.Append(EconomyPromptTexts.Get("S644")).Append(string.IsNullOrWhiteSpace(item.TypeLabel) ? EconomyPromptTexts.Get("S645") : EconomyPromptTexts.ProjectSource(item.TypeLabel))
				.Append(EconomyPromptTexts.Get("S646")).Append(Math.Max(0, item.DailyIncomeDenars)).Append(EconomyPromptTexts.Get("S027"))
				.Append(EconomyPromptTexts.Get("S647")).Append(Math.Max(0, item.GuidePriceDenars)).Append(EconomyPromptTexts.Get("S027"));
			sb.AppendLine(stringBuilder.ToString());
		}
	}

	internal static void AppendPartyTransferPromptSection(StringBuilder sb, string header, IEnumerable<PartyTransferPromptEntry> entries, bool isPrisoner, bool showPromptIndex, Func<PartyTransferPromptEntry, string> sourceLabel, Func<PartyTransferPromptEntry, string> typeLabel)
	{
		if (sb == null)
		{
			return;
		}
		sb.AppendLine(header);
		List<PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<PartyTransferPromptEntry>()).ToList();
		if (list.Count == 0)
		{
			sb.AppendLine(EconomyPromptTexts.Get("S643"));
			return;
		}
		foreach (PartyTransferPromptEntry item in list)
		{
			if (item == null)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (showPromptIndex)
			{
				stringBuilder.Append(item.PromptIndex).Append(" ");
			}
			stringBuilder.Append(EconomyPromptTexts.ProjectPartyAssetName(item.DisplayName)).Append(EconomyPromptTexts.Get("S648")).Append(Math.Max(0, item.Count));
			if (isPrisoner)
			{
				if (item.IsHero)
				{
					stringBuilder.Append(EconomyPromptTexts.Get("S649"));
				}
				string source = EconomyPromptTexts.ProjectPrisonerSource(sourceLabel(item));
				if (!string.IsNullOrWhiteSpace(source))
				{
					stringBuilder.Append(EconomyPromptTexts.Get("S650")).Append(source);
				}
				stringBuilder.Append(EconomyPromptTexts.Get("S651")).Append(Math.Max(1, item.BuyPriceDenarsPerUnit)).Append(EconomyPromptTexts.Get("S652"));
			}
			else
			{
				stringBuilder.Append(EconomyPromptTexts.Get("S644")).Append(EconomyPromptTexts.ProjectSource(typeLabel(item)));
				if (item.Section == PartyTransferEntrySection.NpcVolunteers)
				{
					stringBuilder.Append(EconomyPromptTexts.Get("S653"));
				}
				if (item.WoundedCount > 0)
				{
					stringBuilder.Append(EconomyPromptTexts.Get("S654")).Append(item.WoundedCount);
				}
				stringBuilder.Append(EconomyPromptTexts.Get("S655")).Append(Math.Max(1, item.WageDenarsPerDay)).Append(EconomyPromptTexts.Get("S656"));
				stringBuilder.Append(EconomyPromptTexts.Get("S657")).Append(Math.Max(1, item.HirePriceDenarsPerUnit)).Append(EconomyPromptTexts.Get("S652"));
			}
			sb.AppendLine(stringBuilder.ToString());
		}
	}

	internal static void AppendPartyTransferHiddenTroopSection(StringBuilder sb, string header, IEnumerable<PartyTransferPromptEntry> entries, Func<PartyTransferPromptEntry, string> typeLabel, Func<PartyTransferPromptEntry, int> tier)
	{
		if (sb == null)
		{
			return;
		}
		List<PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<PartyTransferPromptEntry>()).Where((PartyTransferPromptEntry x) => x != null).ToList();
		if (list.Count == 0)
		{
			return;
		}
		sb.AppendLine(header);
		foreach (PartyTransferPromptEntry item in list)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(EconomyPromptTexts.ProjectPartyAssetName(item.DisplayName)).Append(EconomyPromptTexts.Get("S644")).Append(EconomyPromptTexts.ProjectSource(typeLabel(item))).Append(EconomyPromptTexts.Get("S658")).Append(tier(item)).Append(EconomyPromptTexts.Get("S648")).Append(Math.Max(0, item.Count));
			if (item.WoundedCount > 0)
			{
				stringBuilder.Append(EconomyPromptTexts.Get("S654")).Append(item.WoundedCount);
			}
			sb.AppendLine(stringBuilder.ToString());
		}
	}
	internal static long CalculateSettlementTransferTotalValueForExternal(IEnumerable<SettlementTransferPromptEntry> entries, Func<SettlementTransferPromptEntry, bool> isValid)
	{
		long total = 0L;
		foreach (SettlementTransferPromptEntry entry in entries ?? Enumerable.Empty<SettlementTransferPromptEntry>())
		{
			if (!isValid(entry))
			{
				continue;
			}
			total = TransferQuantitySpec.AddProduct(total, 1, Math.Max(0, entry.GuidePriceDenars));
		}
		return total;
	}
	internal static bool LooksLikeFixedAssetTransferIdForExternal(string assetToken)
	{
		string text = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 256 || text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
		{
			return false;
		}
		if (text.StartsWith("workshop@", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("caravan@", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase))
		{
			return text.Length > "settlement:".Length;
		}
		return text.StartsWith("town_", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("castle_", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("village_", StringComparison.OrdinalIgnoreCase);
	}
}
