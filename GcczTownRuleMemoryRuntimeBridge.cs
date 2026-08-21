using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Adapts live Bannerlord governance data and primitive save storage to the reusable GCCZ memory core.
/// </summary>
internal static class GcczTownRuleMemoryRuntimeBridge
{
	private const string StorageInitializedKey = "_gcczTownRuleMemoryStorageInitialized_v1";
	private const string RecordsBySettlementKey = "_gcczTownRuleMemoryRecordsBySettlement_v1";
	private static readonly SettlementRuleMemoryStore Store = new SettlementRuleMemoryStore();
	private static Dictionary<string, string> _serializedRecordsBySettlement =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static bool _storageInitialized;

	internal static void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}

		try
		{
			if (dataStore.IsSaving)
			{
				_serializedRecordsBySettlement = Store.Export()
					.ToDictionary(
						record => record.SettlementId,
						SettlementRuleMemoryCodec.Encode,
						StringComparer.OrdinalIgnoreCase);
				_storageInitialized = true;
				dataStore.SyncData(StorageInitializedKey, ref _storageInitialized);
				dataStore.SyncData(RecordsBySettlementKey, ref _serializedRecordsBySettlement);
				return;
			}

			if (!dataStore.IsLoading)
			{
				return;
			}

			bool initialized = false;
			Dictionary<string, string> serialized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			dataStore.SyncData(StorageInitializedKey, ref initialized);
			dataStore.SyncData(RecordsBySettlementKey, ref serialized);
			_storageInitialized = initialized;
			_serializedRecordsBySettlement = serialized == null
				? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				: new Dictionary<string, string>(serialized, StringComparer.OrdinalIgnoreCase);

			var restored = new List<SettlementRuleMemoryRecord>();
			int rejected = 0;
			foreach (KeyValuePair<string, string> entry in _serializedRecordsBySettlement)
			{
				if (SettlementRuleMemoryCodec.TryDecode(entry.Key, entry.Value, out SettlementRuleMemoryRecord record))
				{
					restored.Add(record);
				}
				else
				{
					rejected++;
				}
			}
			rejected += Store.Restore(restored);
			Logger.Log(
				"GcczTownRuleMemory",
				"Loaded settlement rule memory. Initialized=" + _storageInitialized
				+ ", Records=" + Store.Count
				+ ", Rejected=" + rejected);
		}
		catch (Exception ex)
		{
			Store.Clear();
			_serializedRecordsBySettlement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_storageInitialized = false;
			Logger.Log("GcczTownRuleMemory", "Settlement rule memory load failed; lazy migration will be used: " + ex.Message);
		}
	}

	internal static void ClearForNewGame()
	{
		Store.Clear();
		_serializedRecordsBySettlement.Clear();
		_storageInitialized = false;
	}

	internal static string BuildPromptContext(Settlement settlement, Clan previousOwner, bool activeTownStage)
	{
		if (!activeTownStage || settlement?.IsTown != true)
		{
			return string.Empty;
		}

		try
		{
			int currentDay = GetCurrentCampaignDay();
			SettlementRuleMemoryUpdate update = ObserveCurrentRule(settlement, previousOwner, currentDay);
			if (!update.Accepted)
			{
				return string.Empty;
			}

			return TownPromptComposer.BuildSettlementRuleMemoryContext(
				update.Record,
				currentDay,
				GcczTownPromptResourceProvider.GetCatalog());
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Settlement rule prompt context failed: " + ex.Message);
			return string.Empty;
		}
	}

	internal static void RefreshAfterRuntimeTransition(
		Settlement settlement,
		Clan previousOwner,
		bool activeTownStage,
		string source)
	{
		if (!activeTownStage || settlement?.IsTown != true)
		{
			return;
		}

		try
		{
			SettlementRuleMemoryUpdate update = ObserveCurrentRule(settlement, previousOwner, GetCurrentCampaignDay());
			if (update.Accepted)
			{
				Logger.Log(
					"GcczTownRuleMemory",
					"Refreshed settlement rule memory after runtime transition. Source=" + (source ?? "N/A")
					+ ", Settlement=" + settlement.StringId
					+ ", Ruler=" + (settlement.OwnerClan?.Leader?.StringId ?? "N/A")
					+ ", Culture=" + (settlement.Culture?.StringId ?? "N/A")
					+ ", RulerChanged=" + update.RulerChanged
					+ ", CultureChanged=" + update.CultureChanged);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Settlement rule runtime transition refresh failed: " + ex.Message);
		}
	}

	private static SettlementRuleMemoryUpdate ObserveCurrentRule(
		Settlement settlement,
		Clan previousOwner,
		int currentDay)
	{
		Hero currentRuler = settlement?.OwnerClan?.Leader;
		bool hasStoredRecord = Store.TryGet(settlement?.StringId, out _);
		Hero previousRuler = previousOwner?.Leader;
		if (!hasStoredRecord
			&& previousRuler != null
			&& !IsSameHero(previousRuler, currentRuler))
		{
			Store.Observe(BuildObservation(settlement, previousRuler, currentDay, true));
		}

		return Store.Observe(BuildObservation(
			settlement,
			currentRuler,
			currentDay,
			!Store.TryGet(settlement?.StringId, out _)));
	}

	private static SettlementRuleMemoryObservation BuildObservation(
		Settlement settlement,
		Hero ruler,
		int currentDay,
		bool useMinimumDurationFallback)
	{
		CultureObject culture = settlement?.Culture;
		return new SettlementRuleMemoryObservation(
			settlement?.StringId,
			settlement?.Name?.ToString(),
			ruler?.StringId,
			ruler?.Name?.ToString(),
			culture?.StringId,
			culture?.Name?.ToString(),
			ResolveRulerPersonality(ruler),
			currentDay,
			useMinimumDurationFallback);
	}

	private static string ResolveRulerPersonality(Hero ruler)
	{
		if (ruler == null || ruler == Hero.MainHero)
		{
			return string.Empty;
		}

		MyBehavior.GetNpcPersonaForExternal(ruler, out string personality, out _);
		return (personality ?? string.Empty).Trim();
	}

	private static bool IsSameHero(Hero first, Hero second)
	{
		return first == second
			|| (!string.IsNullOrWhiteSpace(first?.StringId)
				&& string.Equals(first.StringId, second?.StringId, StringComparison.OrdinalIgnoreCase));
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}
}
