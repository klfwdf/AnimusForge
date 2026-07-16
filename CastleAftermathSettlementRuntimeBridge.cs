using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Castle-only settlement consequence adapter. It owns its persistence and never reads or
/// writes the town GCCZ effect dictionaries in SiegeAiInterventionBehavior.
/// </summary>
internal static class CastleAftermathSettlementRuntimeBridge
{
	private static Dictionary<string, int> _annualEffectUntilDay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, float> _prosperityGrowthMultiplier = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, float> _lastObservedProsperity = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, float> _recruitmentSpeedMultiplier = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, float> _recruitQualityMultiplier = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, int> _serviceUntilDay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly List<string> PendingKeys = new List<string>();
	private static float _pendingLoyalty;
	private static float _pendingSecurity;
	private static float _pendingProsperity;
	private static int _pendingSettlementTrust;
	private static int _pendingVillageTrust;
	private static int _pendingNotableRelation;
	private static int _pendingNotableTrust;
	private static float _pendingProsperityMultiplier = 1f;
	private static float _pendingRecruitmentMultiplier = 1f;
	private static float _pendingRecruitQualityMultiplier = 1f;
	private static int _pendingServiceDays;
	private static bool _pendingDevastateEquivalent;

	internal static void SyncData(IDataStore dataStore)
	{
		dataStore?.SyncData("_gcczCastleAnnualEffectUntilDay_v1", ref _annualEffectUntilDay);
		dataStore?.SyncData("_gcczCastleProsperityGrowthMultiplier_v1", ref _prosperityGrowthMultiplier);
		dataStore?.SyncData("_gcczCastleLastObservedProsperity_v1", ref _lastObservedProsperity);
		dataStore?.SyncData("_gcczCastleRecruitmentSpeedMultiplier_v1", ref _recruitmentSpeedMultiplier);
		dataStore?.SyncData("_gcczCastleRecruitQualityMultiplier_v1", ref _recruitQualityMultiplier);
		dataStore?.SyncData("_gcczCastleServiceUntilDay_v1", ref _serviceUntilDay);
		_annualEffectUntilDay ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_prosperityGrowthMultiplier ??= new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		_lastObservedProsperity ??= new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		_recruitmentSpeedMultiplier ??= new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		_recruitQualityMultiplier ??= new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		_serviceUntilDay ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	internal static void ClearForNewGame()
	{
		_annualEffectUntilDay.Clear();
		_prosperityGrowthMultiplier.Clear();
		_lastObservedProsperity.Clear();
		_recruitmentSpeedMultiplier.Clear();
		_recruitQualityMultiplier.Clear();
		_serviceUntilDay.Clear();
		ResetSession("new_game");
	}

	internal static void ResetSession(string source)
	{
		PendingKeys.Clear();
		_pendingLoyalty = 0f;
		_pendingSecurity = 0f;
		_pendingProsperity = 0f;
		_pendingSettlementTrust = 0;
		_pendingVillageTrust = 0;
		_pendingNotableRelation = 0;
		_pendingNotableTrust = 0;
		_pendingProsperityMultiplier = 1f;
		_pendingRecruitmentMultiplier = 1f;
		_pendingRecruitQualityMultiplier = 1f;
		_pendingServiceDays = 0;
		_pendingDevastateEquivalent = false;
		Logger.Log("CastleAftermath", "Reset castle settlement effect ledger. Source=" + (source ?? "N/A"));
	}

	internal static void QueueAction(SiegeCastleActionKind action, bool singleLordTarget)
	{
		SiegeCastleSettlementEffectProfile profile = SiegeCastleSettlementEffectProfile.Build(action);
		if (profile.Key == "none")
		{
			return;
		}
		float scale = singleLordTarget ? 0.15f : 1f;
		PendingKeys.Add(profile.Key + (singleLordTarget ? ":lord" : ":group"));
		_pendingLoyalty += profile.LoyaltyDelta * scale;
		_pendingSecurity += profile.SecurityDelta * scale;
		_pendingProsperity += profile.ProsperityDelta * scale;
		_pendingSettlementTrust += ScaleInt(profile.SettlementPublicTrustDelta, scale);
		_pendingVillageTrust += ScaleInt(profile.BoundVillagePublicTrustDelta, scale);
		_pendingNotableRelation += ScaleInt(profile.NotableRelationDelta, scale);
		_pendingNotableTrust += ScaleInt(profile.NotableTrustDelta, scale);
		_pendingProsperityMultiplier = SiegeCastleSettlementEffectMath.CombineMultiplier(
			_pendingProsperityMultiplier,
			1f + (profile.ProsperityGrowthMultiplier - 1f) * scale);
		_pendingRecruitmentMultiplier = SiegeCastleSettlementEffectMath.CombineMultiplier(
			_pendingRecruitmentMultiplier,
			1f + (profile.RecruitmentSpeedMultiplier - 1f) * scale);
		_pendingRecruitQualityMultiplier = SiegeCastleSettlementEffectMath.CombineMultiplier(
			_pendingRecruitQualityMultiplier,
			1f + (profile.RecruitQualityMultiplier - 1f) * scale);
		_pendingServiceDays = Math.Max(_pendingServiceDays, profile.ServiceDays);
		_pendingDevastateEquivalent |= profile.ReachesNativeDevastateIntensity;
		Logger.Log("CastleAftermath", "Queued castle settlement effect. Action=" + action
			+ ", Key=" + profile.Key + ", Lord=" + singleLordTarget);
	}

	internal static void ApplyAfterNativeMercy(Settlement settlement, float prosperityBeforeMercy)
	{
		if (settlement?.IsCastle != true || settlement.Town == null)
		{
			ResetSession("invalid_castle_finalize");
			return;
		}
		try
		{
			float prosperityTopUp = 0f;
			float loyaltyDelta = SiegeCastleSettlementEffectMath.ClampLoyalty(_pendingLoyalty);
			float prosperityDelta = SiegeCastleSettlementEffectMath.ClampProsperity(_pendingProsperity);
			if (_pendingDevastateEquivalent)
			{
				loyaltyDelta = -30f;
				prosperityTopUp = SiegeCastleSettlementEffectMath.ResolveDevastateTopUp(
					prosperityBeforeMercy,
					settlement.Town.Prosperity);
				prosperityDelta = 0f;
			}

			settlement.Town.Loyalty += loyaltyDelta;
			settlement.Town.Security += SiegeCastleSettlementEffectMath.ClampSecurity(_pendingSecurity);
			settlement.Town.Prosperity = MathF.Max(0f, settlement.Town.Prosperity + prosperityDelta + prosperityTopUp);
			AdjustSettlementTrust(settlement, ClampTrustDelta(_pendingSettlementTrust), "gccz_castle_settlement");
			AdjustBoundVillageTrust(settlement, ClampTrustDelta(_pendingVillageTrust), "gccz_castle_bound_village");
			AdjustNotables(settlement, ClampNotableDelta(_pendingNotableRelation), ClampNotableDelta(_pendingNotableTrust));
			BeginAnnualEffects(settlement);

			Logger.Log("CastleAftermath", "Applied independent castle settlement effects. Settlement="
				+ (settlement.StringId ?? "N/A")
				+ ", Actions=" + string.Join(",", PendingKeys)
				+ ", Loyalty=" + loyaltyDelta.ToString("0.##")
				+ ", Security=" + SiegeCastleSettlementEffectMath.ClampSecurity(_pendingSecurity).ToString("0.##")
				+ ", Prosperity=" + (prosperityDelta + prosperityTopUp).ToString("0.##")
				+ ", DevastateEquivalent=" + _pendingDevastateEquivalent);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Apply independent castle settlement effects failed: " + ex);
		}
		finally
		{
			ResetSession("native_mercy_finalized");
		}
	}

	internal static void OnDailyTickTown(Town town)
	{
		Settlement settlement = town?.Settlement;
		if (settlement?.IsCastle != true || string.IsNullOrWhiteSpace(settlement.StringId))
		{
			return;
		}
		string key = settlement.StringId;
		int today = GetCurrentDay();
		if (!_annualEffectUntilDay.TryGetValue(key, out int untilDay) || today > untilDay)
		{
			ClearAnnualEffect(key);
			return;
		}
		ApplyProsperityGrowthEffect(town, key);
		ApplyRecruitmentEffect(settlement, key);
	}

	private static void BeginAnnualEffects(Settlement settlement)
	{
		string key = settlement?.StringId;
		if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null)
		{
			return;
		}
		bool hasAnnualEffect = Math.Abs(_pendingProsperityMultiplier - 1f) > 0.001f
			|| Math.Abs(_pendingRecruitmentMultiplier - 1f) > 0.001f
			|| Math.Abs(_pendingRecruitQualityMultiplier - 1f) > 0.001f;
		if (hasAnnualEffect)
		{
			int untilDay = GetCurrentDay() + Math.Max(1, CampaignTime.DaysInYear * SiegeCastleSettlementEffectProfile.EffectYears);
			_annualEffectUntilDay[key] = untilDay;
			_prosperityGrowthMultiplier[key] = Math.Max(0f, Math.Min(1.5f, _pendingProsperityMultiplier));
			_recruitmentSpeedMultiplier[key] = Math.Max(0f, Math.Min(SiegeCastleSettlementEffectProfile.MaximumRecruitmentMultiplier, _pendingRecruitmentMultiplier));
			_recruitQualityMultiplier[key] = Math.Max(0f, Math.Min(SiegeCastleSettlementEffectProfile.MaximumRecruitQualityMultiplier, _pendingRecruitQualityMultiplier));
			_lastObservedProsperity[key] = settlement.Town.Prosperity;
		}
		if (_pendingServiceDays > 0)
		{
			_serviceUntilDay[key] = GetCurrentDay() + _pendingServiceDays;
		}
	}

	private static void ApplyProsperityGrowthEffect(Town town, string key)
	{
		float current = town.Prosperity;
		if (!_lastObservedProsperity.TryGetValue(key, out float last))
		{
			_lastObservedProsperity[key] = current;
			return;
		}
		float growth = current - last;
		float multiplier = _prosperityGrowthMultiplier.TryGetValue(key, out float saved) ? saved : 1f;
		if (growth > 0.01f && Math.Abs(multiplier - 1f) > 0.001f)
		{
			town.Prosperity = MathF.Max(0f, current + growth * (multiplier - 1f));
			current = town.Prosperity;
		}
		_lastObservedProsperity[key] = current;
	}

	private static void ApplyRecruitmentEffect(Settlement castle, string key)
	{
		float speed = _recruitmentSpeedMultiplier.TryGetValue(key, out float savedSpeed) ? savedSpeed : 1f;
		float quality = _recruitQualityMultiplier.TryGetValue(key, out float savedQuality) ? savedQuality : 1f;
		foreach (Settlement source in EnumerateRecruitmentSettlements(castle))
		{
			foreach (Hero notable in (source.Notables ?? Enumerable.Empty<Hero>()).ToList())
			{
				ApplyRecruitmentEffectForNotable(notable, source, speed, quality);
			}
		}
	}

	private static void ApplyRecruitmentEffectForNotable(Hero notable, Settlement settlement, float speed, float quality)
	{
		if (notable == null || !notable.IsAlive || !notable.CanHaveRecruits
			|| notable.VolunteerTypes == null || Campaign.Current?.Models?.VolunteerModel == null)
		{
			return;
		}
		CharacterObject basic = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(notable);
		int slots = Math.Min(6, notable.VolunteerTypes.Length);
		for (int i = 0; i < slots; i++)
		{
			CharacterObject current = notable.VolunteerTypes[i];
			if (speed < 0.999f && current != null && MBRandom.RandomFloat < 1f - speed)
			{
				notable.VolunteerTypes[i] = null;
				continue;
			}
			if (speed > 1.001f && current == null)
			{
				float baseChance = Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(notable, i, settlement);
				if (MBRandom.RandomFloat < baseChance * (speed - 1f))
				{
					notable.VolunteerTypes[i] = basic;
					current = basic;
				}
			}
			if (current == null)
			{
				continue;
			}
			if (quality > 1.001f && current.UpgradeTargets != null && current.UpgradeTargets.Length > 0
				&& current.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier
				&& MBRandom.RandomFloat < (quality - 1f) * 0.15f)
			{
				notable.VolunteerTypes[i] = current.UpgradeTargets[MBRandom.RandomInt(current.UpgradeTargets.Length)];
			}
			else if (quality < 0.999f && current.Tier > (basic?.Tier ?? 0)
				&& MBRandom.RandomFloat < (1f - quality) * 0.10f)
			{
				notable.VolunteerTypes[i] = basic;
			}
		}
	}

	private static IEnumerable<Settlement> EnumerateRecruitmentSettlements(Settlement castle)
	{
		if (castle?.BoundVillages == null)
		{
			yield break;
		}
		foreach (Village village in castle.BoundVillages)
		{
			if (village?.Settlement != null)
			{
				yield return village.Settlement;
			}
		}
	}

	private static void AdjustSettlementTrust(Settlement settlement, int delta, string reason)
	{
		if (delta != 0 && RewardSystemBehavior.Instance != null)
		{
			RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(settlement, delta, reason);
		}
	}

	private static void AdjustBoundVillageTrust(Settlement settlement, int delta, string reason)
	{
		if (delta == 0 || RewardSystemBehavior.Instance == null || settlement?.BoundVillages == null)
		{
			return;
		}
		foreach (Village village in settlement.BoundVillages)
		{
			if (village?.Settlement != null)
			{
				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(village.Settlement, delta, reason);
			}
		}
	}

	private static void AdjustNotables(Settlement settlement, int relationDelta, int trustDelta)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Settlement source in EnumerateNotableSettlements(settlement))
		{
			foreach (Hero notable in (source.Notables ?? Enumerable.Empty<Hero>()).ToList())
			{
				if (notable == null || !notable.IsAlive || notable == Hero.MainHero
					|| string.IsNullOrWhiteSpace(notable.StringId) || !seen.Add(notable.StringId))
				{
					continue;
				}
				if (relationDelta != 0)
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, relationDelta, true, true);
				}
				if (trustDelta != 0 && RewardSystemBehavior.Instance != null)
				{
					RewardSystemBehavior.Instance.AdjustPersonalTrustWholeDeltaForExternal(notable, trustDelta, "gccz_castle_notable");
				}
			}
		}
	}

	private static IEnumerable<Settlement> EnumerateNotableSettlements(Settlement castle)
	{
		if (castle != null)
		{
			yield return castle;
		}
		foreach (Settlement village in EnumerateRecruitmentSettlements(castle))
		{
			yield return village;
		}
	}

	private static void ClearAnnualEffect(string key)
	{
		_annualEffectUntilDay.Remove(key);
		_prosperityGrowthMultiplier.Remove(key);
		_lastObservedProsperity.Remove(key);
		_recruitmentSpeedMultiplier.Remove(key);
		_recruitQualityMultiplier.Remove(key);
		if (_serviceUntilDay.TryGetValue(key, out int serviceUntil) && GetCurrentDay() > serviceUntil)
		{
			_serviceUntilDay.Remove(key);
		}
	}

	private static int ScaleInt(int value, float scale)
		=> value == 0 ? 0 : (int)MathF.Round(value * scale);

	private static int ClampTrustDelta(int value) => Math.Min(50, Math.Max(-50, value));

	private static int ClampNotableDelta(int value) => Math.Min(35, Math.Max(-35, value));

	private static int GetCurrentDay()
	{
		try
		{
			return (int)MathF.Floor(CampaignTime.Now.ToDays);
		}
		catch
		{
			return 0;
		}
	}
}
