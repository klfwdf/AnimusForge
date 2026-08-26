using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Persists explicit GCCZ settlement culture changes because Settlement.Culture is not saveable.
/// </summary>
public sealed class GcczSettlementCulturePersistenceBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_gcczSettlementCultureOverrides_v1";
	private readonly SettlementCultureOverrideLedger _ledger = new SettlementCultureOverrideLedger();
	private bool _restorePending;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}

		if (dataStore.IsSaving)
		{
			Dictionary<string, string> saved = _ledger.CopyEntries();
			dataStore.SyncData(StorageKey, ref saved);
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}

		Dictionary<string, string> loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(StorageKey, ref loaded);
		int rejected = _ledger.Restore(loaded);
		_restorePending = _ledger.Count > 0;
		Logger.Log("GcczSettlementCulture", "Loaded settlement culture overrides. Entries=" + _ledger.Count + ", Rejected=" + rejected);
	}

	internal static bool ApplyAndRemember(Settlement settlement, CultureObject culture, string source)
	{
		if (settlement == null || culture == null)
		{
			return false;
		}

		GcczSettlementCulturePersistenceBehavior behavior = Campaign.Current?.GetCampaignBehavior<GcczSettlementCulturePersistenceBehavior>();
		bool recorded = behavior?._ledger.TryRecord(settlement.StringId, culture.StringId) == true;
		settlement.Culture = culture;
		if (!recorded)
		{
			Logger.Log("GcczSettlementCulture", "Applied culture without persistent override. Settlement="
				+ (settlement.StringId ?? "N/A") + ", Culture=" + (culture.StringId ?? "N/A") + ", Source=" + (source ?? "N/A"));
			return false;
		}

		GcczDiagnosticLog.Log("SettlementCulture", "recorded settlement=" + settlement.StringId
			+ " culture=" + culture.StringId + " source=" + (source ?? "N/A"));
		return true;
	}

	internal static int ImportCommittedColonizationSnapshot(TownColonizationSnapshot snapshot, string source)
	{
		if (snapshot?.State != TownColonizationState.Committed
			|| string.IsNullOrWhiteSpace(snapshot.SettlementId)
			|| string.IsNullOrWhiteSpace(snapshot.TargetCultureId))
		{
			return 0;
		}

		GcczSettlementCulturePersistenceBehavior behavior = Campaign.Current?.GetCampaignBehavior<GcczSettlementCulturePersistenceBehavior>();
		if (behavior == null)
		{
			return 0;
		}

		int imported = behavior._ledger.TryRecordIfMissing(snapshot.SettlementId, snapshot.TargetCultureId) ? 1 : 0;
		Settlement settlement = Settlement.Find(snapshot.SettlementId);
		if (settlement?.BoundVillages != null)
		{
			foreach (Village village in settlement.BoundVillages)
			{
				if (village?.Settlement != null
					&& behavior._ledger.TryRecordIfMissing(village.Settlement.StringId, snapshot.TargetCultureId))
				{
					imported++;
				}
			}
		}

		if (imported <= 0)
		{
			return 0;
		}

		behavior._restorePending = true;
		behavior.RestoreRecordedCultures(source ?? "legacy_colonization_import", finalAttempt: false);
		Logger.Log("GcczSettlementCulture", "Imported committed colonization culture overrides. Settlement="
			+ snapshot.SettlementId + ", Culture=" + snapshot.TargetCultureId + ", Entries=" + imported);
		return imported;
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		_ledger.Clear();
		_restorePending = false;
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		RestoreRecordedCultures("game_loaded", finalAttempt: false);
	}

	private void OnGameLoadFinished()
	{
		RestoreRecordedCultures("game_load_finished", finalAttempt: true);
	}

	private void RestoreRecordedCultures(string source, bool finalAttempt)
	{
		if (!_restorePending)
		{
			return;
		}

		int applied = 0;
		int alreadyCurrent = 0;
		int unavailable = 0;
		foreach (KeyValuePair<string, string> entry in _ledger.CopyEntries())
		{
			try
			{
				Settlement settlement = Settlement.Find(entry.Key);
				CultureObject culture = Game.Current?.ObjectManager?.GetObject<CultureObject>(entry.Value);
				if (settlement == null || culture == null)
				{
					unavailable++;
					continue;
				}

				if (settlement.Culture == culture)
				{
					alreadyCurrent++;
					continue;
				}

				settlement.Culture = culture;
				applied++;
			}
			catch (Exception ex)
			{
				unavailable++;
				Logger.Log("GcczSettlementCulture", "Culture override restore skipped one entry. Settlement="
					+ entry.Key + ", Culture=" + entry.Value + ", Error=" + ex.Message);
			}
		}

		GcczDiagnosticLog.Log("SettlementCulture", "restored source=" + (source ?? "N/A")
			+ " applied=" + applied + " current=" + alreadyCurrent + " unavailable=" + unavailable);
		if (finalAttempt)
		{
			_restorePending = false;
		}
	}
}
