using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
	private readonly SettlementCultureLegacyRecoveryPolicy _legacyRecoveryPolicy = new SettlementCultureLegacyRecoveryPolicy();
	private readonly HashSet<string> _reportedUnresolvedDeadNotables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private bool _restorePending;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
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
		if (behavior == null || !behavior._ledger.TryRecord(settlement.StringId, culture.StringId))
		{
			Logger.Log("GcczSettlementCulture", "Rejected culture mutation because the persistent override could not be recorded. Settlement="
				+ (settlement.StringId ?? "N/A") + ", Culture=" + (culture.StringId ?? "N/A") + ", Source=" + (source ?? "N/A"));
			return false;
		}

		settlement.Culture = culture;
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
		_reportedUnresolvedDeadNotables.Clear();
		_restorePending = false;
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		_reportedUnresolvedDeadNotables.Clear();
		RestoreRecordedCultures("game_loaded", finalAttempt: false);
	}

	private void OnGameLoadFinished()
	{
		RestoreRecordedCultures("game_load_finished", finalAttempt: true);
		RepairLegacySettlementState("game_load_finished");
	}

	private void OnDailyTickSettlement(Settlement settlement)
	{
		CleanupDeadNotableReferences(settlement, "daily_tick", out _);
	}

	private void RepairLegacySettlementState(string source)
	{
		int cleanedDeadNotables = 0;
		int unresolvedDeadNotables = 0;
		int recoveredCultureSplits = 0;
		foreach (Settlement settlement in Settlement.All.ToList())
		{
			int cleanedForSettlement = CleanupDeadNotableReferences(settlement, source, out int unresolvedForSettlement);
			cleanedDeadNotables += cleanedForSettlement;
			unresolvedDeadNotables += unresolvedForSettlement;
			if (cleanedForSettlement > 0 && TryRecoverLegacyCultureSplit(settlement, cleanedForSettlement, source))
			{
				recoveredCultureSplits++;
			}
		}

		GcczDiagnosticLog.Log("SettlementCulture", "legacy repair source=" + (source ?? "N/A")
			+ " cleanedDeadNotables=" + cleanedDeadNotables
			+ " unresolvedDeadNotables=" + unresolvedDeadNotables
			+ " recoveredCultureSplits=" + recoveredCultureSplits);
	}

	private int CleanupDeadNotableReferences(Settlement settlement, string source, out int unresolved)
	{
		unresolved = 0;
		if (settlement?.Notables == null)
		{
			return 0;
		}

		int cleaned = 0;
		List<Hero> deadNotables = settlement.Notables
			.Where(hero => hero != null && hero.IsDead && hero.IsNotable)
			.ToList();
		foreach (Hero notable in deadNotables)
		{
			try
			{
				if (notable.StayingInSettlement != settlement)
				{
					unresolved++;
					LogUnresolvedDeadNotableOnce(settlement, notable, source, "not_staying_in_settlement");
					continue;
				}

				LeaveSettlementAction.ApplyForCharacterOnly(notable);
				if (settlement.Notables.Contains(notable))
				{
					unresolved++;
					LogUnresolvedDeadNotableOnce(settlement, notable, source, "native_leave_did_not_refresh_cache");
					continue;
				}
				cleaned++;
			}
			catch (Exception ex)
			{
				unresolved++;
				LogUnresolvedDeadNotableOnce(settlement, notable, source, "native_leave_failed:" + ex.GetType().Name);
			}
		}

		if (cleaned > 0)
		{
			Logger.Log("GcczSettlementCulture", "Cleaned legacy dead notable references through native leave action. Settlement="
				+ (settlement.StringId ?? "N/A") + ", Count=" + cleaned + ", Source=" + (source ?? "N/A"));
		}
		return cleaned;
	}

	private bool TryRecoverLegacyCultureSplit(Settlement settlement, int cleanedDeadNotableCount, string source)
	{
		if (settlement == null)
		{
			return false;
		}

		bool hasExplicitOverride = _ledger.TryGetCultureId(settlement.StringId, out _);
		List<Hero> livingNotables = settlement.Notables?
			.Where(hero => hero != null && hero.IsAlive && hero.IsNotable)
			.ToList() ?? new List<Hero>();
		var facts = new SettlementCultureLegacyRecoveryFacts(
			settlement.IsTown,
			hasExplicitOverride,
			cleanedDeadNotableCount,
			settlement.Culture?.StringId,
			livingNotables.Select(hero => hero.Culture?.StringId));
		SettlementCultureLegacyRecoveryDecision decision = _legacyRecoveryPolicy.Evaluate(facts);
		if (!decision.ShouldRecordOverride)
		{
			return false;
		}

		CultureObject recoveredCulture = livingNotables
			.Select(hero => hero.Culture)
			.FirstOrDefault(culture => culture != null
				&& string.Equals(culture.StringId, decision.CultureId, StringComparison.OrdinalIgnoreCase));
		if (recoveredCulture == null || !_ledger.TryRecord(settlement.StringId, recoveredCulture.StringId))
		{
			return false;
		}

		string oldCultureId = settlement.Culture?.StringId ?? "N/A";
		settlement.Culture = recoveredCulture;
		Logger.Log("GcczSettlementCulture", "Recovered legacy split culture from unanimous replacement notables. Settlement="
			+ (settlement.StringId ?? "N/A") + ", OldCulture=" + oldCultureId
			+ ", NewCulture=" + recoveredCulture.StringId + ", Source=" + (source ?? "N/A"));
		GcczDiagnosticLog.Log("SettlementCulture", "legacy split recovered settlement="
			+ (settlement.StringId ?? "N/A") + " oldCulture=" + oldCultureId
			+ " newCulture=" + recoveredCulture.StringId + " cleanedDeadNotables=" + cleanedDeadNotableCount);
		return true;
	}

	private void LogUnresolvedDeadNotableOnce(Settlement settlement, Hero notable, string source, string reason)
	{
		string key = (settlement?.StringId ?? "N/A") + ":" + (notable?.StringId ?? "N/A");
		if (!_reportedUnresolvedDeadNotables.Add(key))
		{
			return;
		}
		Logger.Log("GcczSettlementCulture", "Legacy dead notable reference could not be cleaned safely. Settlement="
			+ (settlement?.StringId ?? "N/A") + ", Notable=" + (notable?.StringId ?? "N/A")
			+ ", Source=" + (source ?? "N/A") + ", Reason=" + (reason ?? "N/A"));
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
