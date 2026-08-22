using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Adapts live Bannerlord governance data, AF generation, and primitive save storage to the reusable town-memory core.
/// </summary>
internal static class GcczTownRuleMemoryRuntimeBridge
{
	private const string StorageInitializedKey = "_gcczTownRuleMemoryStorageInitialized_v1";
	private const string RecordsBySettlementKey = "_gcczTownRuleMemoryRecordsBySettlement_v1";
	private static readonly object Gate = new object();
	private static readonly SettlementRuleMemoryStore Store = new SettlementRuleMemoryStore();
	private static readonly ConcurrentQueue<string> ChangedSettlementIds = new ConcurrentQueue<string>();
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
				lock (Gate)
				{
					_serializedRecordsBySettlement = Store.Export()
						.ToDictionary(
							record => record.SettlementId,
							SettlementRuleMemoryCodec.Encode,
							StringComparer.OrdinalIgnoreCase);
					_storageInitialized = true;
				}
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
			var restored = new List<SettlementRuleMemoryRecord>();
			int rejected = 0;
			foreach (KeyValuePair<string, string> entry in serialized ?? new Dictionary<string, string>())
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
			lock (Gate)
			{
				_storageInitialized = initialized;
				_serializedRecordsBySettlement = serialized == null
					? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
					: new Dictionary<string, string>(serialized, StringComparer.OrdinalIgnoreCase);
				rejected += Store.Restore(restored);
			}
			GcczTownRuleMemoryGenerationBridge.Reset();
			DrainChangedSettlementIds();
			Logger.Log(
				"GcczTownRuleMemory",
				"Loaded town rule memory. Initialized=" + _storageInitialized
				+ ", Records=" + restored.Count
				+ ", Rejected=" + rejected);
		}
		catch (Exception ex)
		{
			ClearRuntimeState();
			Logger.Log("GcczTownRuleMemory", "Town rule memory load failed; lazy migration will be used: " + ex.Message);
		}
	}

	internal static void ClearForNewGame()
	{
		ClearRuntimeState();
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
			SettlementRuleMemoryUpdate update;
			lock (Gate)
			{
				update = ObserveCurrentRule(settlement, previousOwner, currentDay);
			}
			if (!update.Accepted)
			{
				return string.Empty;
			}

			QueueCurrentNarrativeGeneration(update.Record, currentDay, false);
			return TownPromptComposer.BuildSettlementRuleMemoryContext(
				update.Record,
				currentDay,
				GcczTownPromptResourceProvider.GetCatalog());
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Town rule prompt context failed: " + ex.Message);
			return string.Empty;
		}
	}

	internal static string BuildLocalDialoguePromptContext(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex)
	{
		try
		{
			Settlement settlement = GcczTownRuleMemorySpeakerResolver.ResolveCurrentTownScene();
			CharacterObject character = GcczTownRuleMemorySpeakerResolver.ResolveTargetCharacter(targetCharacter, targetAgentIndex);
			Hero hero = targetHero ?? character?.HeroObject;
			if (settlement == null || !GcczTownRuleMemorySpeakerResolver.IsEligible(settlement, hero, character))
			{
				return string.Empty;
			}

			int currentDay = GetCurrentCampaignDay();
			SettlementRuleMemoryUpdate update;
			lock (Gate)
			{
				update = ObserveCurrentRule(settlement, null, currentDay);
			}
			if (!update.Accepted)
			{
				return string.Empty;
			}
			QueueCurrentNarrativeGeneration(update.Record, currentDay, false);
			return TownPromptComposer.BuildSettlementRuleMemoryContext(
				update.Record,
				currentDay,
				GcczTownPromptResourceProvider.GetCatalog());
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Local town dialogue memory failed: " + ex.Message);
			return string.Empty;
		}
	}

	internal static string BuildEncyclopediaText(Settlement settlement, out bool generationPending)
	{
		generationPending = false;
		if (settlement?.IsTown != true)
		{
			return string.Empty;
		}

		try
		{
			int currentDay = GetCurrentCampaignDay();
			SettlementRuleMemoryUpdate update;
			lock (Gate)
			{
				update = ObserveCurrentRule(settlement, null, currentDay);
			}
			if (!update.Accepted)
			{
				return string.Empty;
			}

			generationPending = string.IsNullOrWhiteSpace(update.Record.CurrentRule?.Narrative);
			QueueCurrentNarrativeGeneration(update.Record, currentDay, false);
			return TownPromptComposer.BuildSettlementRuleMemoryEncyclopediaText(
				update.Record,
				currentDay,
				generationPending,
				GcczTownPromptResourceProvider.GetCatalog());
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Town encyclopedia memory failed: " + ex.Message);
			return string.Empty;
		}
	}

	internal static SettlementRuleMemoryRecord GetOrCreateCurrentTownRecord(Settlement settlement)
	{
		if (settlement?.IsTown != true)
		{
			return null;
		}
		lock (Gate)
		{
			return ObserveCurrentRule(settlement, null, GetCurrentCampaignDay()).Record;
		}
	}

	internal static bool TrySetManualNarrative(Settlement settlement, string rulerId, int ruleStartDay, string narrative)
	{
		if (settlement?.IsTown != true)
		{
			return false;
		}
		lock (Gate)
		{
			bool manual = !string.IsNullOrWhiteSpace(narrative);
			bool updated = Store.TrySetNarrative(settlement.StringId, rulerId, ruleStartDay, narrative, manual, out _);
			if (updated)
			{
				ChangedSettlementIds.Enqueue(settlement.StringId);
			}
			return updated;
		}
	}

	internal static bool RequestCurrentNarrativeRegeneration(Settlement settlement)
	{
		SettlementRuleMemoryRecord record = GetOrCreateCurrentTownRecord(settlement);
		if (record?.CurrentRule == null)
		{
			return false;
		}
		lock (Gate)
		{
			Store.TrySetNarrative(
				settlement.StringId,
				record.CurrentRule.RulerId,
				record.CurrentRule.RuleStartDay,
				string.Empty,
				false,
				out record);
		}
		GcczTownRuleMemoryGenerationBridge.AllowImmediateRetry(record);
		ChangedSettlementIds.Enqueue(settlement.StringId);
		QueueCurrentNarrativeGeneration(record, GetCurrentCampaignDay(), true);
		return true;
	}

	internal static bool TryDequeueChangedSettlementId(out string settlementId)
	{
		return ChangedSettlementIds.TryDequeue(out settlementId);
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
			SettlementRuleMemoryUpdate update;
			lock (Gate)
			{
				update = ObserveCurrentRule(settlement, previousOwner, GetCurrentCampaignDay());
			}
			if (update.Accepted)
			{
				ChangedSettlementIds.Enqueue(settlement.StringId);
				Logger.Log(
					"GcczTownRuleMemory",
					"Refreshed town rule memory after runtime transition. Source=" + (source ?? "N/A")
					+ ", Settlement=" + settlement.StringId
					+ ", Ruler=" + (settlement.OwnerClan?.Leader?.StringId ?? "N/A")
					+ ", Culture=" + (settlement.Culture?.StringId ?? "N/A")
					+ ", RulerChanged=" + update.RulerChanged
					+ ", CultureChanged=" + update.CultureChanged);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Town rule runtime transition refresh failed: " + ex.Message);
		}
	}

	private static void QueueCurrentNarrativeGeneration(
		SettlementRuleMemoryRecord record,
		int currentDay,
		bool force)
	{
		GcczTownRuleMemoryGenerationBridge.Queue(
			record,
			currentDay,
			force,
			TryStoreGeneratedNarrative,
			ChangedSettlementIds.Enqueue);
	}

	private static bool TryStoreGeneratedNarrative(
		string settlementId,
		string rulerId,
		int ruleStartDay,
		string narrative)
	{
		lock (Gate)
		{
			return Store.TryGet(settlementId, out SettlementRuleMemoryRecord current)
				&& current.CurrentRule != null
				&& string.Equals(current.CurrentRule.RulerId, rulerId, StringComparison.OrdinalIgnoreCase)
				&& current.CurrentRule.RuleStartDay == ruleStartDay
				&& string.IsNullOrWhiteSpace(current.CurrentRule.Narrative)
				&& Store.TrySetNarrative(
					settlementId,
					rulerId,
					ruleStartDay,
					narrative,
					false,
					out _);
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
			&& !GcczTownRuleMemoryRulerAdapter.IsSameHero(previousRuler, currentRuler))
		{
			Store.Observe(GcczTownRuleMemoryRulerAdapter.CreateObservation(settlement, previousRuler, currentDay, true));
		}

		return Store.Observe(GcczTownRuleMemoryRulerAdapter.CreateObservation(
			settlement,
			currentRuler,
			currentDay,
			!Store.TryGet(settlement?.StringId, out _)));
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

	private static void DrainChangedSettlementIds()
	{
		while (ChangedSettlementIds.TryDequeue(out _))
		{
		}
	}

	private static void ClearRuntimeState()
	{
		lock (Gate)
		{
			Store.Clear();
			_serializedRecordsBySettlement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_storageInitialized = false;
		}
		GcczTownRuleMemoryGenerationBridge.Reset();
		DrainChangedSettlementIds();
	}
}
