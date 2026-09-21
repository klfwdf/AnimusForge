using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Live, main-thread asset authorization for Economy actions. Detached plans
/// carry tokens only; this owner revalidates current Hero/Party/Merchant lists.
/// </summary>
public partial class RewardSystemBehavior
{
	private static bool TryParseSettlementMerchantPromptStringId(string promptStringId, out string itemId, out string modifierId)
	{
		itemId = "";
		modifierId = "";
		if (string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		string text = promptStringId.Trim();
		int num = text.IndexOf('@');
		if (num < 0)
		{
			itemId = text;
			return !string.IsNullOrWhiteSpace(itemId);
		}
		itemId = text.Substring(0, num).Trim();
		modifierId = text.Substring(num + 1).Trim();
		return !string.IsNullOrWhiteSpace(itemId);
	}

	private static bool TryParseNotableMarketPromptStringId(string promptStringId, out string settlementPromptStringId)
	{
		settlementPromptStringId = "";
		if (string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		string text = promptStringId.Trim();
		if (!text.StartsWith(NotableMarketPromptPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		settlementPromptStringId = text.Substring(NotableMarketPromptPrefix.Length).Trim();
		return TryParseSettlementMerchantPromptStringId(settlementPromptStringId, out var itemId, out var _unused) && !string.IsNullOrWhiteSpace(itemId);
	}

	private static string GetRewardItemTransferKey(RewardItemInfo item)
	{
		string key = (item?.PromptStringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(key) ? (item?.StringId ?? "").Trim() : key;
	}

	public static bool IsGoldAssetTokenForExternal(string token)
	{
		string text = (token ?? "").Trim();
		return string.Equals(text, "GOLD", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "钱", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "金币", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "第纳尔", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "DENAR", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "DENARS", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "MONEY", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "COIN", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "COINS", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsValidGeneratedRpAssetNameForExternal(string assetToken)
	{
		string text = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 160 || IsGoldAssetTokenForExternal(text) || TransferQuantitySpec.IsAllValue(text))
		{
			return false;
		}
		if (Regex.IsMatch(text, "^\\d+$", RegexOptions.CultureInvariant)
			|| text.IndexOfAny(new char[2] { '\r', '\n' }) >= 0)
		{
			return false;
		}
		// Finite GIVE_ASSET values are literal RP item labels. Keep every printable symbol,
		// including "[ROT]", and avoid world-wide entity scans on this postprocess path.
		// A real fixed asset is resolved first from its explicit runtime entry.
		return true;
	}

	private static bool TryResolveExactAuthorizedRewardItem(IEnumerable<RewardItemInfo> authorizedItems, string assetToken, out RewardItemInfo item, out string transferKey)
	{
		item = null;
		transferKey = "";
		string token = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(token))
		{
			return false;
		}
		List<RewardItemInfo> candidates = (authorizedItems ?? Enumerable.Empty<RewardItemInfo>())
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.ToList();
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.PromptStringId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: false, out item, out transferKey))
		{
			return true;
		}
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.StringId ?? x.Item?.StringId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey))
		{
			return true;
		}
		return TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.Name ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey);
	}

	private static bool TrySelectAuthorizedRewardItem(IEnumerable<RewardItemInfo> source, bool allowSharedBaseItemKey, out RewardItemInfo item, out string transferKey)
	{
		item = null;
		transferKey = "";
		List<RewardItemInfo> matches = (source ?? Enumerable.Empty<RewardItemInfo>()).Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0).ToList();
		if (matches.Count == 0)
		{
			return false;
		}
		List<string> keys = matches.Select(GetRewardItemTransferKey).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (keys.Count != 1)
		{
			if (!allowSharedBaseItemKey)
			{
				return false;
			}
			List<string> baseItemKeys = matches.Select((RewardItemInfo x) => (x.StringId ?? x.Item?.StringId ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (baseItemKeys.Count != 1)
			{
				return false;
			}
			item = matches[0];
			transferKey = baseItemKeys[0];
			return true;
		}
		string selectedKey = keys[0];
		item = matches.FirstOrDefault((RewardItemInfo x) => string.Equals(GetRewardItemTransferKey(x), selectedKey, StringComparison.OrdinalIgnoreCase));
		transferKey = selectedKey;
		return item != null;
	}

	private bool TryResolveAuthorizedHeroRewardItem(Hero giver, string assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey)
	{
		authorizedItems = GetHeroInventoryItems(giver);
		if (TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey))
		{
			Logger.Log("Logic", "[Reward] GIVE_ASSET authorization_live source=hero_inventory token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=True");
			return true;
		}
		authorizedItems = BuildHeroRewardPostprocessItems(giver);
		if (authorizedItems.Count > 0)
		{
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, giver, giver?.CharacterObject, -1, authorizedItems);
		}
		bool resolved = TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey);
		Logger.Log("Logic", "[Reward] GIVE_ASSET authorization_live source=hero token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=" + resolved);
		return resolved;
	}

	private bool TryResolveAuthorizedPartyRewardItem(PartyBase giverParty, BasicCharacterObject giverCharacter, string assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey)
	{
		authorizedItems = BuildPartyRewardPostprocessItems(giverParty);
		if (authorizedItems.Count > 0)
		{
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PartyRewardItemsAllSnapshotScope, null, giverCharacter as CharacterObject, -1, authorizedItems);
		}
		bool resolved = TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey);
		Logger.Log("Logic", "[RewardParty] GIVE_ASSET authorization_live token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=" + resolved);
		return resolved;
	}

	private bool TryResolveAuthorizedMerchantRewardItem(CharacterObject giverCharacter, string assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey)
	{
		authorizedItems = BuildSettlementMerchantPostprocessItems(giverCharacter);
		if (authorizedItems.Count > 0)
		{
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, giverCharacter, -1, authorizedItems);
		}
		bool resolved = TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey);
		Logger.Log("Logic", "[RewardMerchant] GIVE_ASSET authorization_live token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=" + resolved);
		return resolved;
	}

	private int ResolveAllRewardItemAmount(string itemToken, IEnumerable<RewardItemInfo> contextItems)
	{
		string token = (itemToken ?? "").Trim();
		List<RewardItemInfo> items = (contextItems ?? Enumerable.Empty<RewardItemInfo>()).Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0).ToList();
		if (string.IsNullOrWhiteSpace(token) || items.Count == 0)
		{
			return 0;
		}
		string resolvedKey = token;
		if (!items.Any((RewardItemInfo x) => string.Equals(GetRewardItemTransferKey(x), token, StringComparison.OrdinalIgnoreCase)) && TryResolveRewardItemByNameOrId(token, items, out var resolution, "all_count"))
		{
			resolvedKey = GetRewardItemTransferKey(resolution?.Info);
		}
		long total = items.Where((RewardItemInfo x) => string.Equals(GetRewardItemTransferKey(x), resolvedKey, StringComparison.OrdinalIgnoreCase)).Sum((RewardItemInfo x) => (long)Math.Max(0, x.Count));
		return (int)Math.Min(int.MaxValue, Math.Max(0L, total));
	}
}
