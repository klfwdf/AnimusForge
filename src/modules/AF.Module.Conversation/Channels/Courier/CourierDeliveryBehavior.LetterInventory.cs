using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private void EnsureCourierLetterInventoryData()
	{
		if (_courierLetterInventoryRecords == null)
		{
			_courierLetterInventoryRecords = new Dictionary<string, CourierLetterInventoryRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_courierLetterInventoryStorage == null)
		{
			_courierLetterInventoryStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private CourierLetterInventoryRecord NormalizeCourierLetterInventoryRecord(string fallbackKey, CourierLetterInventoryRecord record)
	{
		if (record == null)
		{
			return null;
		}
		string itemStringId = (record.ItemStringId ?? fallbackKey ?? "").Trim();
		string displayName = AnimusForgeTextInputSanitizer.SanitizeMultiline(record.DisplayName ?? "", AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
		if (string.IsNullOrWhiteSpace(itemStringId) || string.IsNullOrWhiteSpace(displayName))
		{
			return null;
		}
		string letterBody = AnimusForgeTextInputSanitizer.SanitizeMultiline(record.LetterBody ?? "", AnimusForgeTextInputSanitizer.MaxCourierLetterChars).Trim();
		if (TrySplitCourierLetterInventoryDisplayName(displayName, out string title, out string legacyBody))
		{
			displayName = title;
			if (string.IsNullOrWhiteSpace(letterBody))
			{
				letterBody = legacyBody;
			}
		}
		letterBody = CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(letterBody));
		record.ItemStringId = itemStringId;
		record.Key = itemStringId;
		record.DisplayName = displayName;
		record.LetterBody = letterBody;
		record.TemplateStringId = (record.TemplateStringId ?? "").Trim();
		record.SenderHeroId = (record.SenderHeroId ?? "").Trim();
		record.SenderName = (record.SenderName ?? "").Trim();
		record.Amount = Math.Max(0, Math.Min(record.Amount, 99));
		record.LastSeenDay = Math.Max(0, record.LastSeenDay);
		return record;
	}

	private void RememberCourierLetterInventoryRecord(string itemStringId, string displayName, string letterBody, string templateStringId, uint objectId, Hero sender, string senderName, bool isReply, int amount)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			RewardSystemBehavior.TryPrimeGeneratedInventoryItemForExternal(itemStringId, displayName, templateStringId, objectId, out string normalizedItemStringId, out string normalizedTemplateStringId, out uint normalizedObjectId, "courier_letter_remember");
			if (!string.IsNullOrWhiteSpace(normalizedItemStringId))
			{
				itemStringId = normalizedItemStringId;
			}
			if (!string.IsNullOrWhiteSpace(normalizedTemplateStringId))
			{
				templateStringId = normalizedTemplateStringId;
			}
			if (normalizedObjectId != 0u)
			{
				objectId = normalizedObjectId;
			}
			CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(itemStringId, new CourierLetterInventoryRecord
			{
				ItemStringId = itemStringId,
				DisplayName = displayName,
				LetterBody = letterBody,
				TemplateStringId = templateStringId,
				ObjectId = objectId,
				SenderHeroId = SafeHeroId(sender),
				SenderName = string.IsNullOrWhiteSpace(senderName) ? (sender?.Name?.ToString() ?? "NPC") : senderName.Trim(),
				IsReply = isReply,
				Amount = Math.Max(1, amount),
				LastSeenDay = GetCourierInventoryDayIndex()
			});
			if (record != null)
			{
				_courierLetterInventoryRecords[record.Key] = record;
			}
		}
		catch (Exception ex)
		{
			Log("remember courier letter inventory failed item=" + (itemStringId ?? "") + " error=" + ex.Message);
		}
	}

	private void DiscoverCourierLetterInventoryRecordsFromPlayerRoster(string reason)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			ItemRoster roster = PartyBase.MainParty?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
			if (roster == null)
			{
				return;
			}
			int discovered = 0;
			for (int i = 0; i < roster.Count; i++)
			{
				ItemRosterElement element = roster.GetElementCopyAtIndex(i);
				ItemObject item = element.EquipmentElement.Item;
				if (item == null || element.Amount <= 0)
				{
					continue;
				}
				string itemStringId = (item.StringId ?? "").Trim();
				if (!IsGeneratedRewardInventoryItemId(itemStringId))
				{
					continue;
				}
				string displayName = element.EquipmentElement.GetModifiedItemName()?.ToString() ?? item.Name?.ToString() ?? "";
				displayName = AnimusForgeTextInputSanitizer.SanitizeMultiline(displayName, AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
				if (!LooksLikeCourierLetterInventoryDisplayName(displayName))
				{
					continue;
				}
				CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(itemStringId, new CourierLetterInventoryRecord
				{
					ItemStringId = itemStringId,
					DisplayName = displayName,
					ObjectId = item.Id.InternalValue,
					SenderName = ExtractCourierLetterInventorySenderName(displayName),
					IsReply = IsCourierReplyInventoryDisplayName(displayName),
					Amount = Math.Max(1, CountItemInRoster(roster, itemStringId)),
					LastSeenDay = GetCourierInventoryDayIndex()
				});
				if (record == null)
				{
					continue;
				}
				RewardSystemBehavior.TryPrimeGeneratedInventoryItemForExternal(record.ItemStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, out string normalizedItemStringId, out string normalizedTemplateStringId, out uint normalizedObjectId, "courier_letter_discover_" + (reason ?? ""));
				if (!string.IsNullOrWhiteSpace(normalizedItemStringId))
				{
					record.ItemStringId = normalizedItemStringId;
					record.Key = normalizedItemStringId;
				}
				if (!string.IsNullOrWhiteSpace(normalizedTemplateStringId))
				{
					record.TemplateStringId = normalizedTemplateStringId;
				}
				if (normalizedObjectId != 0u)
				{
					record.ObjectId = normalizedObjectId;
				}
				if (_courierLetterInventoryRecords.TryGetValue(record.Key, out CourierLetterInventoryRecord existing) && existing != null)
				{
					record.Amount = Math.Max(record.Amount, existing.Amount);
					if (string.IsNullOrWhiteSpace(record.SenderHeroId))
					{
						record.SenderHeroId = existing.SenderHeroId;
					}
					if (string.IsNullOrWhiteSpace(record.SenderName))
					{
						record.SenderName = existing.SenderName;
					}
					if (string.IsNullOrWhiteSpace(record.LetterBody))
					{
						record.LetterBody = existing.LetterBody;
					}
				}
				_courierLetterInventoryRecords[record.Key] = record;
				discovered++;
			}
			if (discovered > 0)
			{
				Log("discover courier letter inventory reason=" + (reason ?? "") + " discovered=" + discovered + " tracked=" + _courierLetterInventoryRecords.Count);
			}
		}
		catch (Exception ex)
		{
			Log("discover courier letter inventory failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void DiscoverCourierLetterInventoryRecordsFromGeneratedRewardManifest(string reason)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			Log("skip courier letter inventory manifest import reason=" + (reason ?? "") + " tracked=" + _courierLetterInventoryRecords.Count + " disabled=cross_save_guard");
		}
		catch (Exception ex)
		{
			Log("import courier letter inventory from generated manifest failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void RefreshCourierLetterInventoryRecordsFromPlayerRoster(string reason)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			ItemRoster roster = PartyBase.MainParty?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
			foreach (string key in _courierLetterInventoryRecords.Keys.ToList())
			{
				CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(key, _courierLetterInventoryRecords[key]);
				if (record == null)
				{
					_courierLetterInventoryRecords.Remove(key);
					continue;
				}
				int amount = CountCourierLetterRecordInRoster(roster, record, out ItemObject rosterItem);
				if (amount <= 0)
				{
					_courierLetterInventoryRecords.Remove(key);
					continue;
				}
				if (rosterItem != null && rosterItem.Id.InternalValue != 0u)
				{
					record.ObjectId = rosterItem.Id.InternalValue;
				}
				RewardSystemBehavior.TryPrimeGeneratedInventoryItemForExternal(record.ItemStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, out string normalizedItemStringId, out string normalizedTemplateStringId, out uint normalizedObjectId, "courier_letter_refresh_" + (reason ?? ""));
				if (!string.IsNullOrWhiteSpace(normalizedItemStringId) && !string.Equals(record.ItemStringId, normalizedItemStringId, StringComparison.OrdinalIgnoreCase))
				{
					_courierLetterInventoryRecords.Remove(key);
					record.ItemStringId = normalizedItemStringId;
					record.Key = normalizedItemStringId;
				}
				if (!string.IsNullOrWhiteSpace(normalizedTemplateStringId))
				{
					record.TemplateStringId = normalizedTemplateStringId;
				}
				if (normalizedObjectId != 0u)
				{
					record.ObjectId = normalizedObjectId;
				}
				record.Amount = amount;
				record.LastSeenDay = GetCourierInventoryDayIndex();
				_courierLetterInventoryRecords[record.Key] = record;
			}
		}
		catch (Exception ex)
		{
			Log("refresh courier letter inventory failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	internal void RestoreCourierLetterInventoryItemsAfterGeneratedRewardRestore(string reason)
	{
		DiscoverCourierLetterInventoryRecordsFromGeneratedRewardManifest((reason ?? "reward_restore") + "_manifest_merge");
		PrimeCourierLetterInventoryRecords(reason ?? "reward_restore");
		RestoreCourierLetterInventoryItems(reason ?? "reward_restore");
		ScheduleCourierLetterInventoryRestoreRetries(reason ?? "reward_restore", 4);
	}

	private void ScheduleCourierLetterInventoryRestoreRetries(string reason, int attempts)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			_courierLetterInventoryRestoreRetryRemaining = 0;
			_nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;
			Log("skip courier letter inventory restore retry reason=" + (reason ?? "") + " requested=" + Math.Max(0, attempts) + " records=" + _courierLetterInventoryRecords.Count + " disabled=discard_guard");
		}
		catch (Exception ex)
		{
			Log("schedule courier letter inventory restore retry failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void ProcessCourierLetterInventoryRestoreRetry()
	{
		if (_courierLetterInventoryRestoreRetryRemaining <= 0)
		{
			return;
		}
		long now = DateTime.UtcNow.Ticks;
		if (_nextCourierLetterInventoryRestoreRetryUtcTicks > 0L && now < _nextCourierLetterInventoryRestoreRetryUtcTicks)
		{
			return;
		}
		_courierLetterInventoryRestoreRetryRemaining--;
		_nextCourierLetterInventoryRestoreRetryUtcTicks = DateTime.UtcNow.AddSeconds(1.5).Ticks;
		string reason = "load_retry_" + _courierLetterInventoryRestoreRetryRemaining.ToString();
		DiscoverCourierLetterInventoryRecordsFromGeneratedRewardManifest(reason + "_manifest_merge");
		PrimeCourierLetterInventoryRecords(reason);
		RestoreCourierLetterInventoryItems(reason);
		if (_courierLetterInventoryRestoreRetryRemaining <= 0)
		{
			_nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;
		}
	}

	private void PrimeCourierLetterInventoryRecords(string reason)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			foreach (string key in _courierLetterInventoryRecords.Keys.ToList())
			{
				CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(key, _courierLetterInventoryRecords[key]);
				if (record == null || record.Amount <= 0)
				{
					_courierLetterInventoryRecords.Remove(key);
					continue;
				}
				if (!RewardSystemBehavior.TryPrimeGeneratedInventoryItemForExternal(record.ItemStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, out string normalizedItemStringId, out string normalizedTemplateStringId, out uint normalizedObjectId, "courier_letter_prime_" + (reason ?? "")))
				{
					_courierLetterInventoryRecords[record.Key] = record;
					continue;
				}
				if (!string.IsNullOrWhiteSpace(normalizedItemStringId) && !string.Equals(record.ItemStringId, normalizedItemStringId, StringComparison.OrdinalIgnoreCase))
				{
					_courierLetterInventoryRecords.Remove(key);
					record.ItemStringId = normalizedItemStringId;
					record.Key = normalizedItemStringId;
				}
				if (!string.IsNullOrWhiteSpace(normalizedTemplateStringId))
				{
					record.TemplateStringId = normalizedTemplateStringId;
				}
				if (normalizedObjectId != 0u)
				{
					record.ObjectId = normalizedObjectId;
				}
				record.LastSeenDay = GetCourierInventoryDayIndex();
				_courierLetterInventoryRecords[record.Key] = record;
			}
		}
		catch (Exception ex)
		{
			Log("prime courier letter inventory failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void RestoreCourierLetterInventoryItems(string reason)
	{
		try
		{
			EnsureCourierLetterInventoryData();
			if (_courierLetterInventoryRecords.Count == 0)
			{
				Log("restore courier letter inventory skipped: records=0 reason=" + (reason ?? ""));
				return;
			}
			ItemRoster roster = PartyBase.MainParty?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
			if (roster == null)
			{
				Log("restore courier letter inventory skipped: player roster missing reason=" + (reason ?? ""));
				return;
			}
			int restored = 0;
			foreach (string key in _courierLetterInventoryRecords.Keys.ToList())
			{
				CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(key, _courierLetterInventoryRecords[key]);
				if (record == null || record.Amount <= 0)
				{
					_courierLetterInventoryRecords.Remove(key);
					continue;
				}
				RewardSystemBehavior.TryPrimeGeneratedInventoryItemForExternal(record.ItemStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, out string primedItemStringId, out string primedTemplateStringId, out uint primedObjectId, "courier_letter_restore_prime_" + (reason ?? ""));
				if (!string.IsNullOrWhiteSpace(primedItemStringId) && !string.Equals(record.ItemStringId, primedItemStringId, StringComparison.OrdinalIgnoreCase))
				{
					_courierLetterInventoryRecords.Remove(key);
					record.ItemStringId = primedItemStringId;
					record.Key = primedItemStringId;
				}
				if (!string.IsNullOrWhiteSpace(primedTemplateStringId))
				{
					record.TemplateStringId = primedTemplateStringId;
				}
				if (primedObjectId != 0u)
				{
					record.ObjectId = primedObjectId;
				}
				int current = CountCourierLetterRecordInRoster(roster, record, out _);
				int missing = Math.Max(0, record.Amount - current);
				Log("restore courier letter inventory check reason=" + (reason ?? "") + " item=" + record.ItemStringId + " expected=" + record.Amount + " current=" + current + " missing=" + missing);
				if (missing <= 0)
				{
					_courierLetterInventoryRecords[record.Key] = record;
					continue;
				}
				int generated = RewardSystemBehavior.GenerateKnownInventoryItemToRosterForExternal(roster, record.ItemStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, missing, out _, out string restoredItemId, out string restoredTemplateStringId, out uint restoredObjectId, "courier_letter_restore");
				if (generated <= 0 || string.IsNullOrWhiteSpace(restoredItemId))
				{
					Log("restore courier letter inventory failed item=" + record.ItemStringId + " missing=" + missing + " reason=" + (reason ?? ""));
					continue;
				}
				restored += generated;
				if (!string.Equals(record.ItemStringId, restoredItemId, StringComparison.OrdinalIgnoreCase))
				{
					_courierLetterInventoryRecords.Remove(key);
					record.ItemStringId = restoredItemId;
					record.Key = restoredItemId;
				}
				if (!string.IsNullOrWhiteSpace(restoredTemplateStringId))
				{
					record.TemplateStringId = restoredTemplateStringId;
				}
				if (restoredObjectId != 0u)
				{
					record.ObjectId = restoredObjectId;
				}
				record.Amount = CountCourierLetterRecordInRoster(roster, record, out _);
				record.LastSeenDay = GetCourierInventoryDayIndex();
				_courierLetterInventoryRecords[record.Key] = record;
			}
			if (restored > 0)
			{
				Log("restore courier letter inventory reason=" + (reason ?? "") + " restored=" + restored);
				InformationManager.DisplayMessage(new InformationMessage("已恢复库存中的信件物品 x" + restored + "。", Colors.Green));
			}
		}
		catch (Exception ex)
		{
			Log("restore courier letter inventory failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void AddCourierLetterToPlayerInventory(CourierSession session, Hero sender, string senderName, string letterText, bool isReply)
	{
		try
		{
			string body = CourierVisibleLetterSanitizer.Clean(AnimusForgeTextInputSanitizer.SanitizeMultiline(StripCourierActionTags(letterText ?? ""), AnimusForgeTextInputSanitizer.MaxCourierLetterChars));
			if (string.IsNullOrWhiteSpace(body))
			{
				return;
			}
			ItemRoster roster = PartyBase.MainParty?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
			if (roster == null)
			{
				Log("courier letter inventory item skipped: player item roster missing session=" + (session?.Id ?? "") + " reply=" + isReply);
				InformationManager.DisplayMessage(new InformationMessage("信件物品未加入库存：找不到玩家库存。", Colors.Red));
				return;
			}
			string name = string.IsNullOrWhiteSpace(senderName) ? (sender?.Name?.ToString() ?? "NPC") : senderName.Trim();
			string displayName = BuildCourierLetterInventoryTitle(name, isReply);
			string identityKey = "courier_letter|" + (session?.Id ?? "") + "|" + displayName + "|" + body;
			int senderClanTier = Math.Max(0, sender?.Clan?.Tier ?? 0);
			string templateItemId = senderClanTier >= CourierLetterVelvetClanTier ? CourierLetterVelvetTemplateItemId : CourierLetterLinenTemplateItemId;
			string itemName = null;
			string itemStringId = "";
			int generated = RewardSystemBehavior.GenerateNamedInventoryItemToRosterForExternal(roster, displayName, 1, out itemStringId, out itemName, "courier_letter_inventory", identityKey, templateItemId);
			if (generated <= 0 || string.IsNullOrWhiteSpace(itemStringId))
			{
				Log("courier letter inventory item create failed session=" + (session?.Id ?? "") + " reply=" + isReply);
				InformationManager.DisplayMessage(new InformationMessage("信件物品生成失败，未加入库存。", Colors.Red));
				return;
			}
			uint objectId = 0u;
			int after = CountItemInRoster(roster, itemStringId, out ItemObject generatedItem);
			objectId = generatedItem?.Id.InternalValue ?? 0u;
			if (after <= 0)
			{
				Log("courier letter inventory item add not observed session=" + (session?.Id ?? "") + " item=" + itemStringId + " generated=" + generated + " after=" + after + " reply=" + isReply);
				InformationManager.DisplayMessage(new InformationMessage("信件物品生成了，但未能写入玩家库存。", Colors.Red));
				return;
			}
			RewardSystemBehavior.TryPrimeGeneratedInventoryItemForExternal(itemStringId, displayName, templateItemId, objectId, out string normalizedItemStringId, out string templateStringId, out uint normalizedObjectId, "courier_letter_added_prime");
			if (!string.IsNullOrWhiteSpace(normalizedItemStringId))
			{
				itemStringId = normalizedItemStringId;
			}
			if (normalizedObjectId != 0u)
			{
				objectId = normalizedObjectId;
			}
			Instance?.RememberCourierLetterInventoryRecord(itemStringId, displayName, body, templateStringId, objectId, sender, name, isReply, after);
			InformationManager.DisplayMessage(new InformationMessage("信件已放入玩家库存。", Colors.Green));
			Log("courier letter inventory item added session=" + (session?.Id ?? "") + " item=" + itemStringId + " generated=" + generated + " after=" + after + " reply=" + isReply + " clanTier=" + senderClanTier + " template=" + templateItemId + " nameLen=" + displayName.Length + " itemName=" + (itemName ?? ""));
		}
		catch (Exception ex)
		{
			Log("courier letter inventory item add failed session=" + (session?.Id ?? "") + " reply=" + isReply + " error=" + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("信件物品加入库存失败：" + ex.Message, Colors.Red));
		}
	}

	private static int CountItemInRoster(ItemRoster roster, string itemStringId)
	{
		return CountItemInRoster(roster, itemStringId, out _);
	}

	private static bool TryFindCourierLetterGeneratedItemInRoster(ItemRoster roster, string displayName, out string itemStringId, out uint objectId, out int amount)
	{
		itemStringId = "";
		objectId = 0u;
		amount = 0;
		if (roster == null || string.IsNullOrWhiteSpace(displayName))
		{
			return false;
		}
		string expected = AnimusForgeTextInputSanitizer.SanitizeMultiline(displayName, AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
		if (string.IsNullOrWhiteSpace(expected))
		{
			return false;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(i);
			ItemObject item = element.EquipmentElement.Item;
			if (item == null || element.Amount <= 0)
			{
				continue;
			}
			string currentItemStringId = (item.StringId ?? "").Trim();
			if (!IsGeneratedRewardInventoryItemId(currentItemStringId))
			{
				continue;
			}
			string name = element.EquipmentElement.GetModifiedItemName()?.ToString() ?? item.Name?.ToString() ?? "";
			name = AnimusForgeTextInputSanitizer.SanitizeMultiline(name, AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
			if (!string.Equals(name, expected, StringComparison.Ordinal))
			{
				continue;
			}
			itemStringId = currentItemStringId;
			objectId = item.Id.InternalValue;
			amount = CountItemInRoster(roster, currentItemStringId);
			return true;
		}
		return false;
	}

	private static int CountItemInRoster(ItemRoster roster, string itemStringId, out ItemObject firstItem)
	{
		firstItem = null;
		if (roster == null || string.IsNullOrWhiteSpace(itemStringId))
		{
			return 0;
		}
		int count = 0;
		string key = itemStringId.Trim();
		for (int i = 0; i < roster.Count; i++)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(i);
			if (element.EquipmentElement.Item != null && string.Equals((element.EquipmentElement.Item.StringId ?? "").Trim(), key, StringComparison.OrdinalIgnoreCase))
			{
				if (firstItem == null)
				{
					firstItem = element.EquipmentElement.Item;
				}
				count += element.Amount;
			}
		}
		return count;
	}

	private static int CountCourierLetterRecordInRoster(ItemRoster roster, CourierLetterInventoryRecord record, out ItemObject firstItem)
	{
		firstItem = null;
		if (roster == null || record == null)
		{
			return 0;
		}
		string key = (record.ItemStringId ?? "").Trim();
		uint objectId = record.ObjectId;
		if (string.IsNullOrWhiteSpace(key) && objectId == 0u)
		{
			return 0;
		}
		int count = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(i);
			ItemObject item = element.EquipmentElement.Item;
			if (item == null || element.Amount <= 0)
			{
				continue;
			}
			bool stringMatches = !string.IsNullOrWhiteSpace(key) && string.Equals((item.StringId ?? "").Trim(), key, StringComparison.OrdinalIgnoreCase);
			bool objectMatches = objectId != 0u && item.Id.InternalValue == objectId;
			if (!stringMatches && !objectMatches)
			{
				continue;
			}
			if (firstItem == null)
			{
				firstItem = item;
			}
			count += element.Amount;
		}
		return count;
	}

	private static bool IsGeneratedRewardInventoryItemId(string itemStringId)
	{
		string key = (itemStringId ?? "").Trim();
		return key.StartsWith("af_generated_reward_", StringComparison.OrdinalIgnoreCase) && !key.StartsWith("af_generated_reward_pending_", StringComparison.OrdinalIgnoreCase);
	}

	private static bool LooksLikeCourierLetterInventoryDisplayName(string displayName)
	{
		string text = (displayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return IsCourierReplyInventoryDisplayName(text)
			|| (text.StartsWith("\u6765\u81ea", StringComparison.Ordinal)
				&& (text.Contains("\u7684\u4fe1\uff1a") || GetCourierLetterInventoryFirstLine(text).EndsWith("\u7684\u4fe1", StringComparison.Ordinal)));
	}

	private static bool IsCourierReplyInventoryDisplayName(string displayName)
	{
		string text = (displayName ?? "").Trim();
		return text.Contains("\u7684\u56de\u4fe1\uff1a") || GetCourierLetterInventoryFirstLine(text).EndsWith("\u7684\u56de\u4fe1", StringComparison.Ordinal);
	}

	public static string GetCourierLetterTransferDisplayTitleForExternal(string displayName)
	{
		string text = (displayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || !LooksLikeCourierLetterInventoryDisplayName(text))
		{
			return text;
		}
		return TrySplitCourierLetterInventoryDisplayName(text, out string title, out _) ? title : GetCourierLetterInventoryFirstLine(text);
	}

	public static string GetCourierLetterTransferFactDescriptionForExternal(string itemStringId, uint objectId, string displayName)
	{
		string original = (displayName ?? "").Trim();
		string title = GetCourierLetterTransferDisplayTitleForExternal(original);
		string letterBody = "";
		if (!TryGetCourierLetterInventoryDetailForExternal(itemStringId, objectId, out letterBody)
			&& TrySplitCourierLetterInventoryDisplayName(original, out string legacyTitle, out string legacyBody))
		{
			title = legacyTitle;
			letterBody = legacyBody;
		}
		if (!string.IsNullOrWhiteSpace(letterBody))
		{
			if (string.IsNullOrWhiteSpace(title))
			{
				title = "信件";
			}
			return BuildInventoryDetailFactDescription(title, "信件正文", letterBody);
		}
		// Courier letters remain the authoritative detail when an item happens to
		// match both formats. RP introductions are only appended for non-letter
		// generated items so every give/show request uses the same fact shape.
		if (RewardSystemBehavior.TryGetGeneratedRpItemIntroductionForExternal(
			itemStringId,
			objectId,
			out string introduction))
		{
			return BuildInventoryDetailFactDescription(title, "物品介绍", introduction);
		}
		return original;
	}

	private static string BuildInventoryDetailFactDescription(
		string title,
		string detailLabel,
		string detail)
	{
		string detailText = (detail ?? "").Trim();
		if (string.IsNullOrWhiteSpace(detailText))
		{
			return (title ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(title))
		{
			title = "物品";
		}
		string factBody = Regex.Replace(detailText, "\\s+", " ");
		return title.Trim() + "（" + detailLabel + "：" + factBody + "）";
	}

	public static bool TryGetCourierLetterInventoryDetailForExternal(string itemStringId, uint objectId, out string letterBody)
	{
		letterBody = "";
		try
		{
			CourierDeliveryBehavior instance = Instance;
			if (instance == null)
			{
				return false;
			}
			instance.EnsureCourierLetterInventoryData();
			CourierLetterInventoryRecord record = null;
			string key = (itemStringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key))
			{
				instance._courierLetterInventoryRecords.TryGetValue(key, out record);
			}
			if (record == null && objectId != 0u)
			{
				record = instance._courierLetterInventoryRecords.Values.FirstOrDefault(x => x != null && x.ObjectId == objectId);
			}
			record = instance.NormalizeCourierLetterInventoryRecord(key, record);
			if (record == null || string.IsNullOrWhiteSpace(record.LetterBody))
			{
				return false;
			}
			letterBody = record.LetterBody;
			return true;
		}
		catch (Exception ex)
		{
			Log("resolve courier letter inventory detail failed item=" + (itemStringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static string ExtractCourierLetterInventorySenderName(string displayName)
	{
		string text = (displayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int replyIndex = text.IndexOf("\u7684\u56de\u4fe1\uff1a", StringComparison.Ordinal);
		if (replyIndex > 0)
		{
			return text.Substring(0, replyIndex).Trim();
		}
		if (text.StartsWith("\u6765\u81ea", StringComparison.Ordinal))
		{
			int senderStart = "\u6765\u81ea".Length;
			int senderEnd = text.IndexOf("\u7684\u4fe1\uff1a", senderStart, StringComparison.Ordinal);
			if (senderEnd > senderStart)
			{
				return text.Substring(senderStart, senderEnd - senderStart).Trim();
			}
			string firstLine = GetCourierLetterInventoryFirstLine(text);
			if (firstLine.EndsWith("\u7684\u4fe1", StringComparison.Ordinal) && firstLine.Length > senderStart + "\u7684\u4fe1".Length)
			{
				return firstLine.Substring(senderStart, firstLine.Length - senderStart - "\u7684\u4fe1".Length).Trim();
			}
		}
		string replyTitle = GetCourierLetterInventoryFirstLine(text);
		if (replyTitle.EndsWith("\u7684\u56de\u4fe1", StringComparison.Ordinal))
		{
			return replyTitle.Substring(0, replyTitle.Length - "\u7684\u56de\u4fe1".Length).Trim();
		}
		return "";
	}

	private static int GetCourierInventoryDayIndex()
	{
		return Math.Max(0, (int)Math.Floor(NowDays()));
	}

	private static string BuildCourierLetterInventoryTitle(string senderName, bool isReply)
	{
		string name = string.IsNullOrWhiteSpace(senderName) ? "NPC" : senderName.Trim();
		return isReply ? name + "的回信" : "来自" + name + "的信";
	}

	private static bool TrySplitCourierLetterInventoryDisplayName(string displayName, out string title, out string body)
	{
		title = "";
		body = "";
		string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(displayName ?? "", AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
		if (string.IsNullOrWhiteSpace(text) || !LooksLikeCourierLetterInventoryDisplayName(text))
		{
			return false;
		}
		int lineBreakIndex = text.IndexOfAny(new char[2] { '\r', '\n' });
		string firstLine = lineBreakIndex >= 0 ? text.Substring(0, lineBreakIndex).Trim() : text;
		if (lineBreakIndex >= 0)
		{
			body = text.Substring(lineBreakIndex).Trim();
		}
		title = firstLine.TrimEnd(':', '\uff1a').Trim();
		return !string.IsNullOrWhiteSpace(title);
	}

	private static string GetCourierLetterInventoryFirstLine(string displayName)
	{
		string text = (displayName ?? "").Trim();
		int lineBreakIndex = text.IndexOfAny(new char[2] { '\r', '\n' });
		return lineBreakIndex >= 0 ? text.Substring(0, lineBreakIndex).Trim().TrimEnd(':', '\uff1a').Trim() : text.TrimEnd(':', '\uff1a').Trim();
	}
}
