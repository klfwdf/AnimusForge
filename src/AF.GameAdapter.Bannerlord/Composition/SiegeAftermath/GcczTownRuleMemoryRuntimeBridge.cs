using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
					_serializedRecordsBySettlement = CampaignSaveChunkHelper.FlattenStringDictionary(
						SettlementRuleMemorySaveCodec.Encode(Store.Export()), RecordsBySettlementKey, "GcczTownRuleMemory");
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
			SettlementRuleMemorySaveDecodeResult decoded = SettlementRuleMemorySaveCodec.Decode(
				CampaignSaveChunkHelper.RestoreStringDictionary(serialized, "GcczTownRuleMemory"));
			int rejected = decoded.RejectedCount;
			lock (Gate)
			{
				_storageInitialized = initialized;
				_serializedRecordsBySettlement = serialized == null
					? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
					: new Dictionary<string, string>(serialized, StringComparer.OrdinalIgnoreCase);
				rejected += Store.Restore(decoded.Records);
			}
			GcczTownRuleMemoryGenerationBridge.Reset();
			DrainChangedSettlementIds();
			Logger.Log(
				"GcczTownRuleMemory",
				"Loaded town rule memory. Initialized=" + _storageInitialized
				+ ", Records=" + decoded.Records.Count
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
				record.CurrentRule.Narrative,
				false,
				out record);
		}
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
				if (update.CultureChanged)
					Store.TryRecordConfirmedEvent(settlement.StringId, new SettlementRuleMemoryFact(
						"culture:" + update.Record.CurrentRule.Evolution.Revision,
						"城镇文化已变更为“" + settlement.Culture?.Name + "”（当时领主：“" + settlement.OwnerClan?.Leader?.Name + "”）。",
						GetCurrentCampaignDay()));
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

	private static bool TryStoreGeneratedNarrative(SettlementRuleMemoryRecord expected, int requestDay, string narrative)
	{
		// Main-thread completion: refresh live ruler/culture/personality before source acceptance.
		Settlement settlement = Settlement.Find(expected.SettlementId);
		if (settlement?.IsTown != true) return false;
		lock (Gate)
		{
			ObserveCurrentRule(settlement, null, GetCurrentCampaignDay());
			return Store.TryStoreGeneratedNarrative(expected, GetCurrentCampaignDay(), narrative);
		}
	}

	internal static void RecordConfirmedEvent(Settlement settlement, string eventId, string fact)
	{
		if (settlement?.IsTown != true || string.IsNullOrWhiteSpace(fact)) return;
		try
		{
			int day = GetCurrentCampaignDay();
			lock (Gate)
			{
				ObserveCurrentRule(settlement, null, day);
				Store.TryRecordConfirmedEvent(settlement.StringId, new SettlementRuleMemoryFact(eventId,
					"当时领主：“" + settlement.OwnerClan?.Leader?.Name + "”。" + fact, day));
			}
		}
		catch (Exception ex) { Logger.Log("GcczTownRuleMemory", "Confirmed event capture failed: " + ex.Message); }
	}

	internal static void ObserveOwnerChange(Settlement settlement, Hero oldOwner, Hero newOwner)
	{
		// A grant may name a clan member; the authoritative town ruler is its owning clan's leader.
		newOwner = settlement?.OwnerClan?.Leader ?? newOwner;
		if (settlement?.IsTown != true || newOwner == null || GcczTownRuleMemoryRulerAdapter.IsSameHero(oldOwner, newOwner)) return;
		try
		{
			int day = GetCurrentCampaignDay();
			lock (Gate)
			{
				if (!Store.TryGet(settlement.StringId, out _) && oldOwner != null)
					Store.Observe(GcczTownRuleMemoryRulerAdapter.CreateObservation(settlement, oldOwner, day, true));
				var update = Store.Observe(GcczTownRuleMemoryRulerAdapter.CreateObservation(settlement, newOwner, day, false));
				if (update.RulerChanged || update.Initialized)
				{
					Store.TryRecordConfirmedEvent(settlement.StringId, new SettlementRuleMemoryFact(
						"owner:" + update.Record.CurrentRule.Evolution.Revision,
						"城镇领主由“" + (oldOwner?.Name?.ToString() ?? "未知") + "”变为“" + newOwner.Name + "”。", day));
					ChangedSettlementIds.Enqueue(settlement.StringId);
				}
			}
		}
		catch (Exception ex) { Logger.Log("GcczTownRuleMemory", "Owner event capture failed: " + ex.Message); }
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
