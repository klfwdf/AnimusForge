using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

internal sealed class PolicyEffectActivationTransaction
{
	internal List<PolicyEffectInstanceSaveData> Instances { get; } = new List<PolicyEffectInstanceSaveData>();

	internal List<PolicyEffectExecutionReceipt> Receipts { get; } = new List<PolicyEffectExecutionReceipt>();

	internal List<PolicyEffectPreparedInstance> AppliedOneShots { get; } = new List<PolicyEffectPreparedInstance>();
}

internal sealed class PolicyEffectDailyExecutionOutcome
{
	internal PolicyEffectExecutionStatus Status { get; set; } = PolicyEffectExecutionStatus.Skipped;

	internal bool StateChanged { get; set; }

	internal bool Succeeded { get; set; }

	internal bool Failed { get; set; }

	internal bool Retryable { get; set; }

	internal int Attempts { get; set; }

	internal string Error { get; set; } = string.Empty;

	internal PolicyEffectInstanceSaveData Instance { get; set; }

	internal IPolicyEffectModule Module { get; set; }

	internal PolicyEffectPreparedInstance Prepared { get; set; }

	internal PolicyEffectTargetKind TargetKind { get; set; }

	internal string TargetId { get; set; } = string.Empty;

	internal int CampaignDay { get; set; }

	internal int AttemptWindowDay { get; set; }

	internal PolicyEffectExecutionReceipt AppliedReceipt { get; set; }

	internal PolicyEffectExecutionReceipt PreviousReceipt { get; set; }
}

internal sealed class PolicyEffectPendingDailyCompensationLeg
{
	internal PolicyEffectInstanceSaveData Instance { get; set; }

	internal IPolicyEffectModule Module { get; set; }

	internal PolicyEffectPreparedInstance Prepared { get; set; }

	internal PolicyEffectTargetKind TargetKind { get; set; }

	internal string TargetId { get; set; } = string.Empty;

	internal int CampaignDay { get; set; }

	internal int Order { get; set; }

	internal PolicyEffectExecutionReceipt AppliedReceipt { get; set; }

	internal JObject Progress { get; set; }
}

internal sealed class PolicyEffectScheduledExecutionLeg
{
	internal PolicyEffectInstanceSaveData Instance { get; set; }

	internal IPolicyEffectModule Module { get; set; }

	internal PolicyEffectPreparedInstance Prepared { get; set; }

	internal PolicyEffectExecutionReceipt Receipt { get; set; }

	internal PolicyEffectExecutionReceipt PreviousReceipt { get; set; }

	internal PolicyEffectLifecycleState PreviousLifecycleState { get; set; }
}

internal sealed partial class BannerlordPolicyEffectGameBridge : IPolicyEffectGameBridge
{
	internal static readonly BannerlordPolicyEffectGameBridge Instance = new BannerlordPolicyEffectGameBridge();

	private readonly Dictionary<string, Clan> _clansById = new Dictionary<string, Clan>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, Hero> _heroesById = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);

	private Campaign _cachedCampaign;

	private bool _targetCachesDirty = true;

	private BannerlordPolicyEffectGameBridge()
	{
	}

	internal void InvalidateTargetCaches()
	{
		_targetCachesDirty = true;
	}

	public bool TryAdjustKingdomStability(
		string kingdomId,
		int delta,
		string reason,
		out int actualDelta,
		out string error)
	{
		bool success = TryAdjustKingdomStability(
			kingdomId,
			delta,
			reason,
			out int beforeValue,
			out int afterValue,
			out error);
		actualDelta = afterValue - beforeValue;
		return success;
	}

	public bool TryAdjustKingdomStability(
		string kingdomId,
		int delta,
		string reason,
		out int beforeValue,
		out int afterValue,
		out string error)
	{
		beforeValue = 0;
		afterValue = 0;
		error = string.Empty;
		string normalizedId = (kingdomId ?? string.Empty).Trim();
		Kingdom kingdom = (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(
			item => item != null && string.Equals(item.StringId, normalizedId, StringComparison.OrdinalIgnoreCase));
		if (kingdom == null)
		{
			error = "kingdom not found: " + normalizedId;
			return false;
		}
		beforeValue = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		afterValue = beforeValue;
		if (delta == 0)
		{
			return true;
		}
		int initialValue = beforeValue;
		if (!MyBehavior.TryAdjustKingdomStabilityForExternal(
			kingdom,
			delta,
			reason ?? string.Empty,
			out _,
			out _))
		{
			// The host helper writes before logging. If a later logging/storage callback
			// throws, it can report false after the value changed; restore it here so a
			// failed bridge call remains side-effect free for transactional retries.
			int current = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
			if (current != initialValue)
			{
				MyBehavior.TryAdjustKingdomStabilityForExternal(
					kingdom,
					initialValue - current,
					(reason ?? string.Empty) + ":failed-call-compensation",
					out _,
					out _);
			}
			error = "kingdom stability bridge rejected delta=" + delta.ToString(CultureInfo.InvariantCulture);
			if (MyBehavior.GetKingdomStabilityValueForExternal(kingdom) != initialValue)
			{
				error += "; exact compensation failed";
			}
			afterValue = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
			return false;
		}
		afterValue = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		return true;
	}

	public bool TryRestoreKingdomStability(
		string kingdomId,
		int expectedAfterValue,
		int beforeValue,
		string reason,
		out int afterValue,
		out string error)
	{
		afterValue = 0;
		error = string.Empty;
		string normalizedId = (kingdomId ?? string.Empty).Trim();
		Kingdom kingdom = (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(
			item => item != null && string.Equals(item.StringId, normalizedId, StringComparison.OrdinalIgnoreCase));
		if (kingdom == null)
		{
			error = "kingdom not found for stability compensation: " + normalizedId;
			return false;
		}

		int current = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		if (current == beforeValue)
		{
			afterValue = current;
			return true;
		}
		if (current != expectedAfterValue)
		{
			afterValue = current;
			error = "kingdom stability changed before compensation: expected="
				+ expectedAfterValue.ToString(CultureInfo.InvariantCulture)
				+ ", actual=" + current.ToString(CultureInfo.InvariantCulture);
			return false;
		}

		bool adjusted = MyBehavior.TryAdjustKingdomStabilityForExternal(
			kingdom,
			beforeValue - current,
			reason ?? string.Empty,
			out _,
			out _);
		afterValue = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		if (afterValue == beforeValue)
		{
			// The host writes the value before dispatching its diagnostic callback. An
			// exception there may report false even though exact restoration completed.
			return true;
		}

		int failedAfterValue = afterValue;
		if (failedAfterValue != expectedAfterValue)
		{
			MyBehavior.TryAdjustKingdomStabilityForExternal(
				kingdom,
				expectedAfterValue - failedAfterValue,
				(reason ?? string.Empty) + ":failed-restore-revert",
				out _,
				out _);
			afterValue = MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		}
		error = "kingdom stability compensation did not restore the exact prior value"
			+ (adjusted ? string.Empty : "; stability bridge rejected restore");
		if (afterValue != expectedAfterValue)
		{
			error += "; failed restore changed the value to "
				+ afterValue.ToString(CultureInfo.InvariantCulture);
		}
		return false;
	}

	public bool TryChangeClanInfluence(
		string clanId,
		float delta,
		string reason,
		out float beforeValue,
		out float afterValue,
		out string error)
	{
		beforeValue = 0f;
		afterValue = 0f;
		error = string.Empty;
		_ = reason;
		if (!IsFinite(delta))
		{
			error = "clan influence delta must be finite";
			return false;
		}
		if (!TryResolveClan(clanId, out Clan clan))
		{
			error = "clan not found: " + (clanId ?? string.Empty).Trim();
			return false;
		}
		beforeValue = clan.Influence;
		if (clan.IsEliminated)
		{
			// A clan can be eliminated after a ScheduledOnce target set was frozen.
			// Keep the transaction deterministic by recording a zero actual change.
			afterValue = beforeValue;
			return true;
		}
		if (!IsFinite(beforeValue) || !IsFinite(beforeValue + delta))
		{
			afterValue = beforeValue;
			error = "clan influence change would produce a non-finite value";
			return false;
		}
		if (delta != 0f)
		{
			try
			{
				ChangeClanInfluenceAction.Apply(clan, delta);
			}
			catch (Exception ex)
			{
				float current = clan.Influence;
				if (IsFinite(current) && current != beforeValue)
				{
					try
					{
						ChangeClanInfluenceAction.Apply(clan, beforeValue - current);
					}
					catch
					{
						// Apply mutates before dispatching the event. Verify the value below.
					}
				}
				afterValue = clan.Influence;
				error = "clan influence action failed: " + ex.Message;
				if (!NearlyEqualInfluence(afterValue, beforeValue))
				{
					error += "; exact compensation failed";
				}
				return false;
			}
		}
		afterValue = clan.Influence;
		return IsFinite(afterValue);
	}

	public bool TryRestoreClanInfluence(
		string clanId,
		float expectedAfterValue,
		float beforeValue,
		string reason,
		out float afterValue,
		out string error)
	{
		afterValue = 0f;
		error = string.Empty;
		_ = reason;
		if (!IsFinite(expectedAfterValue) || !IsFinite(beforeValue))
		{
			error = "clan influence compensation values must be finite";
			return false;
		}
		if (!TryResolveClan(clanId, out Clan clan))
		{
			error = "clan not found for influence compensation: " + (clanId ?? string.Empty).Trim();
			return false;
		}
		float current = clan.Influence;
		if (NearlyEqualInfluence(current, beforeValue))
		{
			afterValue = current;
			return true;
		}
		if (!NearlyEqualInfluence(current, expectedAfterValue))
		{
			afterValue = current;
			error = "clan influence changed before compensation: expected="
				+ expectedAfterValue.ToString("0.######", CultureInfo.InvariantCulture)
				+ ", actual=" + current.ToString("0.######", CultureInfo.InvariantCulture);
			return false;
		}
		float compensation = beforeValue - current;
		if (compensation != 0f)
		{
			try
			{
				ChangeClanInfluenceAction.Apply(clan, compensation);
			}
			catch
			{
				// The vanilla action mutates influence before dispatching its event. The
				// exact post-value check below decides whether compensation succeeded.
			}
		}
		afterValue = clan.Influence;
		if (!NearlyEqualInfluence(afterValue, beforeValue))
		{
			error = "clan influence compensation did not restore the exact prior value";
			return false;
		}
		return true;
	}

	public bool TryChangeClanLeaderRelation(
		string actorHeroId,
		string targetClanId,
		int delta,
		string reason,
		out string targetHeroId,
		out int beforeValue,
		out int afterValue,
		out string error)
	{
		targetHeroId = string.Empty;
		beforeValue = 0;
		afterValue = 0;
		error = string.Empty;
		_ = reason;
		if (!TryResolveHero(actorHeroId, out Hero actor))
		{
			error = "policy actor hero not found or unavailable: " + (actorHeroId ?? string.Empty).Trim();
			return false;
		}
		if (actor.IsDead || actor.IsDisabled)
		{
			// Actor identity remains frozen on the instance, but a ruler who died or
			// became disabled before the due day no longer blocks the linked daily legs.
			return true;
		}
		if (!TryResolveClan(targetClanId, out Clan targetClan) || targetClan.IsEliminated)
		{
			// Membership can become stale between a structural refresh and execution.
			// Treat it as a skipped target rather than applying to an invalid clan.
			return true;
		}
		Hero target = targetClan.Leader;
		if (target == null || target.IsDead || target.IsDisabled || ReferenceEquals(actor, target))
		{
			return true;
		}
		targetHeroId = (target.StringId ?? string.Empty).Trim();
		if (targetHeroId.Length == 0)
		{
			return true;
		}
		GetEffectiveRelationHeroes(actor, target, out Hero effectiveActor, out Hero effectiveTarget);
		if (effectiveActor == null || effectiveTarget == null || ReferenceEquals(effectiveActor, effectiveTarget))
		{
			targetHeroId = string.Empty;
			return true;
		}
		beforeValue = CharacterRelationManager.GetHeroRelation(effectiveActor, effectiveTarget);
		if (delta != 0)
		{
			// Keep vanilla diplomacy scaling, positive randomized rounding and clamping.
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(actor, target, delta, showQuickNotification: false);
			}
			catch (Exception ex)
			{
				try
				{
					effectiveActor.SetPersonalRelation(effectiveTarget, beforeValue);
					afterValue = CharacterRelationManager.GetHeroRelation(effectiveActor, effectiveTarget);
				}
				catch
				{
					afterValue = int.MinValue;
				}
				error = "relation action failed: " + ex.Message;
				if (afterValue != beforeValue)
				{
					error += "; exact compensation failed";
				}
				return false;
			}
		}
		GetEffectiveRelationHeroes(actor, target, out effectiveActor, out effectiveTarget);
		afterValue = effectiveActor == null || effectiveTarget == null
			? beforeValue
			: CharacterRelationManager.GetHeroRelation(effectiveActor, effectiveTarget);
		return true;
	}

	public bool TryRestoreHeroRelation(
		string actorHeroId,
		string targetHeroId,
		int expectedAfterValue,
		int beforeValue,
		string reason,
		out int afterValue,
		out string error)
	{
		afterValue = 0;
		error = string.Empty;
		_ = reason;
		if (!TryResolveHero(actorHeroId, out Hero actor)
			|| !TryResolveHero(targetHeroId, out Hero target))
		{
			error = "relation compensation hero not found";
			return false;
		}
		GetEffectiveRelationHeroes(actor, target, out Hero effectiveActor, out Hero effectiveTarget);
		if (effectiveActor == null || effectiveTarget == null || ReferenceEquals(effectiveActor, effectiveTarget))
		{
			error = "relation compensation effective heroes are unavailable";
			return false;
		}
		int current = CharacterRelationManager.GetHeroRelation(effectiveActor, effectiveTarget);
		if (current == beforeValue)
		{
			afterValue = current;
			return true;
		}
		if (current != expectedAfterValue)
		{
			afterValue = current;
			error = "relation changed before compensation: expected="
				+ expectedAfterValue.ToString(CultureInfo.InvariantCulture)
				+ ", actual=" + current.ToString(CultureInfo.InvariantCulture);
			return false;
		}
		effectiveActor.SetPersonalRelation(effectiveTarget, beforeValue);
		afterValue = CharacterRelationManager.GetHeroRelation(effectiveActor, effectiveTarget);
		if (afterValue != beforeValue)
		{
			error = "relation compensation did not restore the exact prior value";
			return false;
		}
		int actualDelta = afterValue - current;
		if (actualDelta != 0)
		{
			try
			{
				CampaignEventDispatcher.Instance.OnHeroRelationChanged(
					effectiveActor,
					effectiveTarget,
					actualDelta,
					false,
					ChangeRelationAction.ChangeRelationDetail.Default,
					actor,
					target);
			}
			catch
			{
				// The relation value is already restored exactly. Do not make a retry
				// reapply compensation after a downstream notification listener failed.
			}
		}
		return true;
	}

	public bool TryReadHeroGold(
		string heroId,
		out bool available,
		out int gold,
		out string error)
	{
		available = false;
		gold = 0;
		error = string.Empty;
		if (!TryResolveHero(heroId, out Hero hero))
		{
			return true;
		}
		if (hero.IsDead || hero.IsDisabled)
		{
			return true;
		}
		gold = hero.Gold;
		available = true;
		return true;
	}

	public bool TryChangeHeroGoldExact(
		string heroId,
		int delta,
		string reason,
		out bool available,
		out int beforeValue,
		out int afterValue,
		out string error)
	{
		available = false;
		beforeValue = 0;
		afterValue = 0;
		error = string.Empty;
		_ = reason;
		if (delta == int.MinValue)
		{
			error = "hero gold delta cannot be int.MinValue";
			return false;
		}
		if (!TryResolveHero(heroId, out Hero hero) || hero.IsDead || hero.IsDisabled)
		{
			return true;
		}
		available = true;
		beforeValue = hero.Gold;
		afterValue = beforeValue;
		long expected = (long)beforeValue + delta;
		if (expected < 0 || expected > int.MaxValue)
		{
			error = expected < 0 ? "insufficient hero gold" : "hero gold overflow";
			return false;
		}
		if (delta == 0)
		{
			return true;
		}

		try
		{
			ApplyHeroGoldDelta(hero, delta);
		}
		catch (Exception ex)
		{
			afterValue = hero.Gold;
			if (afterValue != beforeValue)
			{
				TryRestoreHeroGoldValue(hero, afterValue, beforeValue);
				afterValue = hero.Gold;
			}
			error = "hero gold action failed: " + ex.Message;
			if (afterValue != beforeValue)
			{
				error += "; exact compensation failed";
			}
			return false;
		}

		afterValue = hero.Gold;
		if (afterValue != (int)expected)
		{
			int failedAfter = afterValue;
			TryRestoreHeroGoldValue(hero, failedAfter, beforeValue);
			afterValue = hero.Gold;
			error = "hero gold action did not apply the exact requested delta";
			if (afterValue != beforeValue)
			{
				error += "; exact compensation failed";
			}
			return false;
		}
		return true;
	}

	public bool TryRestoreHeroGold(
		string heroId,
		int expectedAfterValue,
		int beforeValue,
		string reason,
		out int afterValue,
		out string error)
	{
		afterValue = 0;
		error = string.Empty;
		_ = reason;
		if (!TryResolveHero(heroId, out Hero hero))
		{
			error = "hero not found for gold compensation: " + (heroId ?? string.Empty).Trim();
			return false;
		}
		int current = hero.Gold;
		if (current == beforeValue)
		{
			afterValue = current;
			return true;
		}
		if (current != expectedAfterValue)
		{
			afterValue = current;
			error = "hero gold changed before compensation: expected="
				+ expectedAfterValue.ToString(CultureInfo.InvariantCulture)
				+ ", actual=" + current.ToString(CultureInfo.InvariantCulture);
			return false;
		}
		if (!TryRestoreHeroGoldValue(hero, current, beforeValue))
		{
			afterValue = hero.Gold;
			error = "hero gold compensation did not restore the exact prior value";
			return false;
		}
		afterValue = hero.Gold;
		return true;
	}

	private static void ApplyHeroGoldDelta(Hero hero, int delta)
	{
		if (delta > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, hero, delta, false);
		}
		else if (delta < 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(hero, null, -delta, false);
		}
	}

	private static bool TryRestoreHeroGoldValue(Hero hero, int current, int target)
	{
		long compensation = (long)target - current;
		if (compensation < -int.MaxValue || compensation > int.MaxValue)
		{
			return false;
		}
		try
		{
			ApplyHeroGoldDelta(hero, (int)compensation);
		}
		catch
		{
			// GiveGoldAction mutates before notifying listeners. The exact value check
			// remains authoritative even when a downstream listener throws.
		}
		return hero.Gold == target;
	}

	private bool TryResolveClan(string clanId, out Clan clan)
	{
		clan = null;
		string normalizedId = (clanId ?? string.Empty).Trim();
		EnsureTargetCaches();
		if (normalizedId.Length > 0 && _clansById.TryGetValue(normalizedId, out clan) && clan != null)
		{
			return true;
		}
		RebuildTargetCaches();
		return normalizedId.Length > 0 && _clansById.TryGetValue(normalizedId, out clan) && clan != null;
	}

	private bool TryResolveHero(string heroId, out Hero hero)
	{
		hero = null;
		string normalizedId = (heroId ?? string.Empty).Trim();
		EnsureTargetCaches();
		if (normalizedId.Length > 0 && _heroesById.TryGetValue(normalizedId, out hero) && hero != null)
		{
			return true;
		}
		try
		{
			hero = normalizedId.Length > 0 ? Hero.Find(normalizedId) : null;
			if (hero != null)
			{
				CacheHero(hero);
				return true;
			}
		}
		catch
		{
			hero = null;
		}
		RebuildTargetCaches();
		return normalizedId.Length > 0 && _heroesById.TryGetValue(normalizedId, out hero) && hero != null;
	}

	private void EnsureTargetCaches()
	{
		if (_targetCachesDirty || !ReferenceEquals(_cachedCampaign, Campaign.Current))
		{
			RebuildTargetCaches();
		}
	}

	private void RebuildTargetCaches()
	{
		_clansById.Clear();
		_heroesById.Clear();
		_cachedCampaign = Campaign.Current;
		_targetCachesDirty = false;
		foreach (Clan clan in Clan.All ?? Enumerable.Empty<Clan>())
		{
			CacheClanAndHeroes(clan);
		}
		CacheClanAndHeroes(Clan.PlayerClan);
		CacheHero(Hero.MainHero);
	}

	private void CacheClanAndHeroes(Clan clan)
	{
		string clanId = (clan?.StringId ?? string.Empty).Trim();
		if (clan == null || clanId.Length == 0)
		{
			return;
		}
		_clansById[clanId] = clan;
		CacheHero(clan.Leader);
		foreach (Hero hero in clan.Heroes ?? Enumerable.Empty<Hero>())
		{
			CacheHero(hero);
		}
	}

	private void CacheHero(Hero hero)
	{
		string heroId = (hero?.StringId ?? string.Empty).Trim();
		if (hero != null && heroId.Length > 0)
		{
			_heroesById[heroId] = hero;
		}
	}

	private static void GetEffectiveRelationHeroes(
		Hero actor,
		Hero target,
		out Hero effectiveActor,
		out Hero effectiveTarget)
	{
		effectiveActor = null;
		effectiveTarget = null;
		Campaign.Current?.Models?.DiplomacyModel?.GetHeroesForEffectiveRelation(
			actor,
			target,
			out effectiveActor,
			out effectiveTarget);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static bool NearlyEqualInfluence(float left, float right)
	{
		return Math.Abs(left - right)
			<= Math.Max(0.0001f, Math.Max(Math.Abs(left), Math.Abs(right)) * 0.00001f);
	}

}

internal static class PolicyEffectActivationCoordinator
{
	private const int DailyExecutionMaxAttempts = 3;

	private const int ScheduledExecutionMaxAttempts = 3;

	private const string DailyRuntimeStateProperty = "daily";

	private const string ScheduledRuntimeStateProperty = "scheduledOnce";

	private const string LifecycleRuntimeStateProperty = "lifecycle";

	internal static bool TryActivate(
		IEnumerable<PolicyEffectInstanceSaveData> sourceInstances,
		IEnumerable<PolicyEffectExecutionReceipt> existingReceipts,
		IPolicyEffectGameBridge gameBridge,
		float campaignDay,
		out PolicyEffectActivationTransaction transaction,
		out string error)
	{
		transaction = new PolicyEffectActivationTransaction();
		error = string.Empty;
		Dictionary<string, PolicyEffectExecutionReceipt> receiptByInstance = (existingReceipts
			?? Enumerable.Empty<PolicyEffectExecutionReceipt>())
			.Where(item => item != null && !string.IsNullOrWhiteSpace(item.InstanceId))
			.GroupBy(item => item.InstanceId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData source in sourceInstances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			if (!TryPrepare(source, out IPolicyEffectModule module, out PolicyEffectPreparedInstance prepared, out error))
			{
				RollbackAppliedOneShots(transaction, gameBridge, campaignDay, out _);
				transaction = null;
				return false;
			}
			PolicyEffectInstanceSaveData normalizedSave = CreateSaveData(prepared.Instance, source);
			if (module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot
				&& (normalizedSave.TargetSet?.TargetPlans?.Count ?? 0) > 0
				&& !HasExecutableTargetForModule(module, normalizedSave.TargetSet))
			{
				normalizedSave.LifecycleState = PolicyEffectLifecycleState.Suspended;
				transaction.Instances.Add(normalizedSave);
				continue;
			}
			if (module is IPolicyEffectLifecycleModule
				&& !TryDispatchLifecycle(
					new[] { normalizedSave },
					PolicyEffectLifecycleEventKind.Activated,
					normalizedSave.InstanceId + ":activated",
					null,
					campaignDay,
					out _,
					out string activationLifecycleError))
			{
				error = "activated lifecycle failed: " + module.Id + " / "
					+ FirstNonEmpty(activationLifecycleError, "callback rejected activation");
				RollbackAppliedOneShots(transaction, gameBridge, campaignDay, out string rollbackError);
				if (!string.IsNullOrWhiteSpace(rollbackError))
				{
					error += "; rollback=" + rollbackError;
				}
				transaction = null;
				return false;
			}
			if (module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot)
			{
				normalizedSave.LifecycleState = PolicyEffectLifecycleState.Active;
				transaction.Instances.Add(normalizedSave);
				continue;
			}
			if (!(module is IOneShotPolicyEffectModule oneShot))
			{
				error = "one-shot module executor missing: " + module.Id;
				RollbackAppliedOneShots(transaction, gameBridge, campaignDay, out _);
				transaction = null;
				return false;
			}
			receiptByInstance.TryGetValue(source.InstanceId, out PolicyEffectExecutionReceipt existingReceipt);
			PolicyEffectExecutionContext context = new PolicyEffectExecutionContext
			{
				PreparedInstance = prepared,
				CampaignDay = campaignDay,
				GameBridge = gameBridge,
				ExistingReceipt = existingReceipt ?? source.ExecutionReceipt,
				IdempotencyKey = prepared.IdempotencyKey,
				Attempt = 1,
				RuntimeState = GetModuleRuntimeState(normalizedSave)
			};
			PolicyEffectExecutionResult execution = oneShot.ApplyOnce(context);
			if (execution?.Success != true || execution.Receipt == null)
			{
				error = "one-shot apply failed: " + module.Id + " / " + (execution?.Error ?? "missing receipt");
				RollbackAppliedOneShots(transaction, gameBridge, campaignDay, out string rollbackError);
				if (!string.IsNullOrWhiteSpace(rollbackError))
				{
					error += "; rollback=" + rollbackError;
				}
				transaction = null;
				return false;
			}
			normalizedSave.ExecutionReceipt = execution.Receipt;
			ApplyModuleRuntimeState(normalizedSave, execution.RuntimeState);
			normalizedSave.LifecycleState = PolicyEffectLifecycleState.Completed;
			transaction.Instances.Add(normalizedSave);
			transaction.Receipts.Add(execution.Receipt);
			if (execution.Status == PolicyEffectExecutionStatus.Applied)
			{
				transaction.AppliedOneShots.Add(prepared);
			}
		}
		if (!ReconcileMechanismLifecycleStates(transaction.Instances, out _, out error))
		{
			RollbackAppliedOneShots(transaction, gameBridge, campaignDay, out _);
			transaction = null;
			return false;
		}
		return true;
	}

	internal static bool ReconcileMechanismLifecycleStates(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		out bool changed,
		out string error)
	{
		changed = false;
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> allInstances = (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		List<PolicyEffectInstanceSaveData> activeCandidates = allInstances
			.Where(instance => instance.LifecycleState != PolicyEffectLifecycleState.Completed
				&& instance.LifecycleState != PolicyEffectLifecycleState.RolledBack)
			.ToList();

		foreach (PolicyEffectInstanceSaveData independent in activeCandidates
			.Where(instance => instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion
				&& instance.MechanismKind == PolicyEffectMechanismKind.Independent
				&& instance.LifecycleState != PolicyEffectLifecycleState.Failed))
		{
			if (!HasRefreshableTargetDefinition(independent.TargetSet)
				|| !PolicyEffectModuleCatalog.TryGet(independent.ModuleId, out IPolicyEffectModule module)
				|| module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot)
			{
				continue;
			}
			PolicyEffectLifecycleState desired = HasExecutableTargetForModule(module, independent.TargetSet)
				? PolicyEffectLifecycleState.Active
				: PolicyEffectLifecycleState.Suspended;
			if (independent.LifecycleState != desired)
			{
				independent.LifecycleState = desired;
				changed = true;
			}
		}

		foreach (IGrouping<string, PolicyEffectInstanceSaveData> group in allInstances
			.Where(instance => instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion
				&& instance.MechanismKind == PolicyEffectMechanismKind.Linked)
			.GroupBy(instance => (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty),
				StringComparer.Ordinal))
		{
			List<PolicyEffectInstanceSaveData> groupInstances = group.ToList();
			if (!PolicyEffectMechanismContract.TryValidateLinkedGroup(groupInstances, out string contractError)
				|| groupInstances.Any(instance => instance.LifecycleState == PolicyEffectLifecycleState.Failed
					|| instance.LifecycleState == PolicyEffectLifecycleState.RolledBack))
			{
				foreach (PolicyEffectInstanceSaveData instance in groupInstances)
				{
					if (instance.LifecycleState != PolicyEffectLifecycleState.Failed)
					{
						instance.LifecycleState = PolicyEffectLifecycleState.Failed;
						changed = true;
					}
				}
				PolicySystemLog.Failure("Effect", "linked-mechanism-contract-failed",
					string.IsNullOrWhiteSpace(contractError)
						? "linked mechanism contains a persisted failed leg"
						: contractError,
					"mechanism=" + (groupInstances.FirstOrDefault()?.MechanismId ?? string.Empty));
				continue;
			}
			List<Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule>> legs = new List<Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule>>();
			bool unavailable = false;
			foreach (PolicyEffectInstanceSaveData instance in groupInstances)
			{
				if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
					|| module?.Descriptor == null
					|| module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot)
				{
					unavailable = true;
					break;
				}
				legs.Add(Tuple.Create(instance, module));
			}
			if (unavailable)
			{
				foreach (PolicyEffectInstanceSaveData instance in groupInstances)
				{
					if (instance.LifecycleState != PolicyEffectLifecycleState.Failed)
					{
						instance.LifecycleState = PolicyEffectLifecycleState.Failed;
						changed = true;
					}
				}
				PolicySystemLog.Failure("Effect", "linked-mechanism-module-failed",
					"linked mechanism contains an unavailable or OneShot module",
					"mechanism=" + (groupInstances.FirstOrDefault()?.MechanismId ?? string.Empty));
				continue;
			}
			List<Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule>> runtimeLegs = legs
				.Where(leg => leg.Item1.LifecycleState != PolicyEffectLifecycleState.Completed
					&& leg.Item1.LifecycleState != PolicyEffectLifecycleState.RolledBack)
				.ToList();
			if (runtimeLegs.Count == 0)
			{
				continue;
			}
			bool ready = runtimeLegs.All(leg => HasExecutableTargetForModule(leg.Item2, leg.Item1.TargetSet));
			if (ready && HasSameModuleLinkedSideTargetOverlap(legs))
			{
				ready = false;
			}
			PolicyEffectLifecycleState desired = ready
				? PolicyEffectLifecycleState.Active
				: PolicyEffectLifecycleState.Suspended;
			foreach (Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule> leg in runtimeLegs)
			{
				if (leg.Item1.LifecycleState != desired)
				{
					leg.Item1.LifecycleState = desired;
					changed = true;
				}
			}
		}
		return true;
	}

	private static bool HasRefreshableTargetDefinition(PolicyEffectCanonicalTargetSet targetSet)
	{
		return targetSet != null
			&& ((targetSet.TargetPlans?.Count ?? 0) > 0
				|| (targetSet.SelectorIds?.Count ?? 0) > 0
				|| (targetSet.SelectorHandles ?? Enumerable.Empty<string>())
					.Any(handle => !string.IsNullOrWhiteSpace(handle))
				|| targetSet.FollowCurrentRulingClan);
	}

	private static bool HasSameModuleLinkedSideTargetOverlap(
		IReadOnlyList<Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule>> legs)
	{
		if (legs == null || legs.Count <= 1)
		{
			return false;
		}
		for (int leftIndex = 0; leftIndex < legs.Count; leftIndex++)
		{
			Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule> left = legs[leftIndex];
			if (left?.Item1 == null || left.Item2?.Descriptor == null)
			{
				continue;
			}
			bool leftIsSource = IsSourceRole(left.Item1.MechanismRole);
			bool leftIsDestination = IsDestinationRole(left.Item1.MechanismRole);
			if (!leftIsSource && !leftIsDestination)
			{
				continue;
			}
			for (int rightIndex = leftIndex + 1; rightIndex < legs.Count; rightIndex++)
			{
				Tuple<PolicyEffectInstanceSaveData, IPolicyEffectModule> right = legs[rightIndex];
				if (right?.Item1 == null
					|| !string.Equals(left.Item2.Id, right.Item2?.Id, StringComparison.Ordinal))
				{
					continue;
				}
				bool rightIsSource = IsSourceRole(right.Item1.MechanismRole);
				bool rightIsDestination = IsDestinationRole(right.Item1.MechanismRole);
				if ((leftIsSource == rightIsSource && leftIsDestination == rightIsDestination)
					|| (!leftIsSource && !leftIsDestination)
					|| (!rightIsSource && !rightIsDestination))
				{
					continue;
				}
				if (HasCanonicalTargetOverlap(left.Item2, left.Item1.TargetSet, right.Item1.TargetSet))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool HasCanonicalTargetOverlap(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		foreach (PolicyEffectTargetKind kind in module?.Descriptor?.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>())
		{
			if (HasTargetIdOverlap(GetTargetIds(left, kind), GetTargetIds(right, kind)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasExecutableTargetForModule(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet targetSet)
	{
		foreach (PolicyEffectTargetKind kind in module?.Descriptor?.TargetKinds
			?? Array.Empty<PolicyEffectTargetKind>())
		{
			switch (kind)
			{
				case PolicyEffectTargetKind.Settlement: if ((targetSet?.SettlementIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Town: if ((targetSet?.TownIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Village: if ((targetSet?.VillageIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Clan: if ((targetSet?.ClanIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Kingdom: if ((targetSet?.KingdomIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Hero: if ((targetSet?.HeroIds?.Count ?? 0) > 0) return true; break;
			}
		}
		return false;
	}

	private static bool IsSourceRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Source || role == PolicyEffectMechanismRole.Cost;
	}

	private static bool IsDestinationRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Destination || role == PolicyEffectMechanismRole.Beneficiary;
	}

	private static IEnumerable<string> GetTargetIds(
		PolicyEffectCanonicalTargetSet targetSet,
		PolicyEffectTargetKind kind)
	{
		if (targetSet == null)
		{
			return Array.Empty<string>();
		}
		switch (kind)
		{
			case PolicyEffectTargetKind.Settlement: return targetSet.SettlementIds;
			case PolicyEffectTargetKind.Town: return targetSet.TownIds;
			case PolicyEffectTargetKind.Village: return targetSet.VillageIds;
			case PolicyEffectTargetKind.Clan: return targetSet.ClanIds;
			case PolicyEffectTargetKind.Kingdom: return targetSet.KingdomIds;
			case PolicyEffectTargetKind.Hero: return targetSet.HeroIds;
			default: return Array.Empty<string>();
		}
	}

	private static bool HasTargetIdOverlap(IEnumerable<string> leftIds, IEnumerable<string> rightIds)
	{
		HashSet<string> right = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string value in rightIds ?? Array.Empty<string>())
		{
			string normalized = (value ?? string.Empty).Trim();
			if (normalized.Length > 0)
			{
				right.Add(normalized);
			}
		}
		if (right.Count == 0)
		{
			return false;
		}
		foreach (string value in leftIds ?? Array.Empty<string>())
		{
			string normalized = (value ?? string.Empty).Trim();
			if (normalized.Length > 0 && right.Contains(normalized))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool RollbackAppliedOneShots(
		PolicyEffectActivationTransaction transaction,
		IPolicyEffectGameBridge gameBridge,
		float campaignDay,
		out string error)
	{
		error = string.Empty;
		if (transaction == null)
		{
			return true;
		}
		List<string> failures = new List<string>();
		for (int index = transaction.AppliedOneShots.Count - 1; index >= 0; index--)
		{
			PolicyEffectPreparedInstance prepared = transaction.AppliedOneShots[index];
			if (!PolicyEffectModuleCatalog.TryGet(prepared?.Instance?.ModuleId, out IPolicyEffectModule module)
				|| !(module is IOneShotPolicyEffectModule oneShot))
			{
				failures.Add((prepared?.Instance?.ModuleId ?? "unknown") + ": executor missing");
				continue;
			}
			PolicyEffectExecutionReceipt receipt = transaction.Receipts.LastOrDefault(
				item => string.Equals(item?.InstanceId, prepared.Instance.InstanceId, StringComparison.Ordinal));
			PolicyEffectExecutionResult rollback = oneShot.RollbackOnce(new PolicyEffectExecutionContext
			{
				PreparedInstance = prepared,
				CampaignDay = campaignDay,
				GameBridge = gameBridge,
				ExistingReceipt = receipt,
				IdempotencyKey = prepared.IdempotencyKey,
				Attempt = 1
			});
			if (rollback?.Success != true)
			{
				failures.Add(module.Id + ": " + (rollback?.Error ?? "rollback failed"));
			}
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	internal static bool TryRollbackSavedOneShots(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		IEnumerable<PolicyEffectExecutionReceipt> receipts,
		IPolicyEffectGameBridge gameBridge,
		float campaignDay,
		out string error)
	{
		error = string.Empty;
		Dictionary<string, PolicyEffectExecutionReceipt> receiptByInstance = (receipts
			?? Enumerable.Empty<PolicyEffectExecutionReceipt>())
			.Where(receipt => receipt != null && !string.IsNullOrWhiteSpace(receipt.InstanceId))
			.GroupBy(receipt => receipt.InstanceId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
		List<string> failures = new List<string>();
		foreach (PolicyEffectInstanceSaveData instance in (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>()).Where(item => item != null).Reverse())
		{
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot)
			{
				continue;
			}
			receiptByInstance.TryGetValue(instance.InstanceId ?? string.Empty, out PolicyEffectExecutionReceipt receipt);
			receipt ??= instance.ExecutionReceipt;
			if (receipt == null
				|| receipt.Status == PolicyEffectExecutionStatus.RolledBack
				|| string.Equals(receipt.Message, "legacyAssumedCompleted", StringComparison.Ordinal))
			{
				continue;
			}
			string prepareError = string.Empty;
			if (!(module is IOneShotPolicyEffectModule oneShot)
				|| !TryPrepare(instance, out _, out PolicyEffectPreparedInstance prepared, out prepareError))
			{
				failures.Add(module.Id + ": " + FirstNonEmpty(prepareError, "rollback prepare failed"));
				continue;
			}
			PolicyEffectExecutionResult rollback = oneShot.RollbackOnce(new PolicyEffectExecutionContext
			{
				PreparedInstance = prepared,
				CampaignDay = campaignDay,
				GameBridge = gameBridge,
				ExistingReceipt = receipt,
				IdempotencyKey = prepared.IdempotencyKey,
				Attempt = 1,
				RuntimeState = GetModuleRuntimeState(instance)
			});
			if (rollback?.Success != true)
			{
				failures.Add(module.Id + ": " + (rollback?.Error ?? "rollback failed"));
			}
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	internal static bool TryExecuteScheduledOnce(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		IList<PolicyEffectExecutionReceipt> receipts,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		out List<string> completedInstanceIds,
		out bool stateChanged,
		out string error)
	{
		completedInstanceIds = new List<string>();
		stateChanged = false;
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> allInstances = (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(item => item != null)
			.ToList();
		if (!ReconcileMechanismLifecycleStates(allInstances, out bool preflightChanged, out string preflightError))
		{
			stateChanged = preflightChanged;
			error = FirstNonEmpty(preflightError, "scheduled mechanism preflight failed");
			return false;
		}
		stateChanged |= preflightChanged;
		if (HasPendingScheduledCompensation(allInstances))
		{
			error = "scheduled compensation is pending; resume compensation before executing daily effects";
			return false;
		}
		List<PolicyEffectScheduledExecutionLeg> due = new List<PolicyEffectScheduledExecutionLeg>();
		foreach (PolicyEffectInstanceSaveData instance in allInstances
			.OrderBy(item => item.PolicyId ?? string.Empty, StringComparer.Ordinal)
			.ThenBy(item => item.MechanismId ?? string.Empty, StringComparer.Ordinal)
			.ThenBy(item => item.InstanceId ?? string.Empty, StringComparer.Ordinal))
		{
			if (instance.LifecycleState != PolicyEffectLifecycleState.Active
				|| campaignDay <= instance.StartDay
				|| !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.ScheduledOnce)
			{
				continue;
			}
			if (!(module is IScheduledOncePolicyEffectModule))
			{
				instance.LifecycleState = PolicyEffectLifecycleState.Failed;
				stateChanged = true;
				error = module.Id + ": scheduled executor unavailable";
				return false;
			}
			if (!PolicyEffectSaveCodec.TryMigrateKnownModuleRuntimeState(instance, module, out string migrationError))
			{
				instance.LifecycleState = PolicyEffectLifecycleState.Failed;
				stateChanged = true;
				error = module.Id + ": " + FirstNonEmpty(migrationError, "scheduled runtime migration failed");
				return false;
			}
			if (!TryPrepare(instance, out _, out PolicyEffectPreparedInstance prepared, out string prepareError))
			{
				instance.LifecycleState = PolicyEffectLifecycleState.Failed;
				stateChanged = true;
				error = module.Id + ": " + FirstNonEmpty(prepareError, "scheduled prepare failed");
				return false;
			}
			PolicyEffectLifecycleState previousLifecycleState = instance.LifecycleState;
			FreezeScheduledTargets(
				instance,
				campaignDay,
				previousLifecycleState,
				instance.ExecutionReceipt);
			prepared.Instance.TargetSet = instance.TargetSet;
			due.Add(new PolicyEffectScheduledExecutionLeg
			{
				Instance = instance,
				Module = module,
				Prepared = prepared,
				PreviousReceipt = instance.ExecutionReceipt,
				PreviousLifecycleState = previousLifecycleState
			});
			stateChanged = true;
		}
		if (due.Count == 0)
		{
			return true;
		}

		List<PolicyEffectScheduledExecutionLeg> completedThisCall = new List<PolicyEffectScheduledExecutionLeg>();
		IEnumerable<IGrouping<string, PolicyEffectScheduledExecutionLeg>> groups = due.GroupBy(
			leg => leg.Instance.MechanismKind == PolicyEffectMechanismKind.Linked
				? "linked\u001f" + (leg.Instance.PolicyId ?? string.Empty) + "\u001f" + (leg.Instance.MechanismId ?? string.Empty)
				: "independent\u001f" + (leg.Instance.InstanceId ?? string.Empty),
			StringComparer.Ordinal);
		foreach (IGrouping<string, PolicyEffectScheduledExecutionLeg> group in groups)
		{
			List<PolicyEffectScheduledExecutionLeg> groupLegs = group.ToList();
			if (!TryExecuteScheduledGroup(
				groupLegs,
				receipts,
				gameBridge,
				campaignDay,
				out List<PolicyEffectScheduledExecutionLeg> appliedGroup,
				out bool compensationFailed,
				out string groupError))
			{
				List<string> rollbackErrors = new List<string>();
				if (!TryCompensateScheduledLegs(
					completedThisCall,
					receipts,
					gameBridge,
					campaignDay,
					out string rollbackError))
				{
					rollbackErrors.Add(rollbackError);
				}
				foreach (PolicyEffectScheduledExecutionLeg failedLeg in groupLegs)
				{
					// A scheduled transaction may be retried on a later game day. Keep the
					// pre-attempt lifecycle state and let the outer daily pipeline gate its
					// daily legs when this method reports failure.
					failedLeg.Instance.LifecycleState = compensationFailed
						? PolicyEffectLifecycleState.Failed
						: failedLeg.PreviousLifecycleState;
					JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(failedLeg.Instance));
					progress["failedDay"] = campaignDay;
					progress["lastError"] = FirstNonEmpty(groupError, "scheduled execution failed");
				}
				stateChanged = true;
				error = FirstNonEmpty(groupError, "scheduled transaction failed");
				if (rollbackErrors.Count > 0)
				{
					error += "; compensation=" + string.Join("; ", rollbackErrors);
				}
				completedInstanceIds.Clear();
				return false;
			}
			completedThisCall.AddRange(appliedGroup);
			completedInstanceIds.AddRange(appliedGroup.Select(leg => leg.Instance.InstanceId));
			stateChanged = true;
		}
		if (!ReconcileMechanismLifecycleStates(allInstances, out bool lifecycleChanged, out string lifecycleError))
		{
			if (!TryCompensateScheduledLegs(
				completedThisCall,
				receipts,
				gameBridge,
				campaignDay,
				out string compensationError))
			{
				lifecycleError += "; compensation=" + compensationError;
			}
			completedInstanceIds.Clear();
			stateChanged = true;
			error = FirstNonEmpty(lifecycleError, "scheduled mechanism reconciliation failed");
			return false;
		}
		stateChanged |= lifecycleChanged;
		return true;
	}

	internal static bool HasFrozenScheduledTargets(PolicyEffectInstanceSaveData instance)
	{
		if (instance == null
			|| !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
			|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.ScheduledOnce
			|| !(instance.RuntimeState is JObject root)
			|| !(root[PolicyEffectRuntimeStateEnvelope.FrameworkProperty] is JObject framework)
			|| !(framework[ScheduledRuntimeStateProperty] is JObject progress))
		{
			return false;
		}
		return ReadInt(progress, "frozenDay", int.MinValue) != int.MinValue;
	}

	internal static bool HasPendingDailyCompensation(
		IEnumerable<PolicyEffectInstanceSaveData> instances)
	{
		return (instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Any(HasPendingDailyCompensation);
	}

	internal static bool TryMarkDailyCompensationPending(
		PolicyEffectDailyExecutionOutcome outcome,
		PolicyEffectExecutionReceipt transactionPreviousReceipt,
		int order,
		string compensationError,
		out string error)
	{
		error = string.Empty;
		if (outcome?.Instance == null
			|| outcome.AppliedReceipt == null
			|| string.IsNullOrWhiteSpace(outcome.TargetId))
		{
			error = "daily compensation marker is incomplete";
			return false;
		}
		try
		{
			JObject root = EnsureRuntimeStateRoot(outcome.Instance);
			JObject daily = EnsureDailyProgress(root);
			if (!ReadBool(daily, "transactionPreviousReceiptCaptured", false))
			{
				daily["transactionPreviousReceiptCaptured"] = true;
				daily["transactionHadPreviousExecutionReceipt"] = transactionPreviousReceipt != null;
				if (transactionPreviousReceipt != null)
				{
					daily["transactionPreviousExecutionReceipt"] = JToken.FromObject(transactionPreviousReceipt);
				}
				else
				{
					daily.Remove("transactionPreviousExecutionReceipt");
				}
			}
			daily["compensationPending"] = true;
			if (ReadInt(daily, "pendingSinceDay", int.MinValue) == int.MinValue)
			{
				daily["pendingSinceDay"] = outcome.CampaignDay;
			}
			daily["lastCompensationAttemptDay"] = Math.Max(outcome.CampaignDay, outcome.AttemptWindowDay);
			daily["lastCompensationError"] = compensationError ?? string.Empty;
			daily["status"] = "compensationPending";

			JObject progress = EnsureDailyTargetProgress(
				root,
				outcome.TargetKind,
				outcome.TargetId);
			progress["compensationPending"] = true;
			progress["compensationOrder"] = Math.Max(0, order);
			progress["compensationCampaignDay"] = outcome.CampaignDay;
			progress["compensationAppliedReceipt"] = JToken.FromObject(outcome.AppliedReceipt);
			progress["lastCompensationAttemptDay"] = Math.Max(outcome.CampaignDay, outcome.AttemptWindowDay);
			progress["lastCompensationError"] = compensationError ?? string.Empty;
			progress["status"] = "compensationPending";
			if (!TrySynchronizePendingDailyReceipt(
				outcome.Instance,
				receipts: null,
				out string receiptError))
			{
				error = receiptError;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	internal static bool TryResumeDailyCompensation(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		IList<PolicyEffectExecutionReceipt> receipts,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		out bool stateChanged,
		out string error)
	{
		stateChanged = false;
		error = string.Empty;
		List<string> failures = new List<string>();
		HashSet<PolicyEffectInstanceSaveData> blockedInstances
			= new HashSet<PolicyEffectInstanceSaveData>();
		List<PolicyEffectInstanceSaveData> pendingInstances = (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(HasPendingDailyCompensation)
			.Where(instance => instance != null)
			.ToList();
		if (pendingInstances.Count == 0)
		{
			return true;
		}

		List<PolicyEffectPendingDailyCompensationLeg> pending
			= new List<PolicyEffectPendingDailyCompensationLeg>();
		foreach (PolicyEffectInstanceSaveData instance in pendingInstances)
		{
			JObject daily = EnsureDailyProgress(EnsureRuntimeStateRoot(instance));
			if (!ReadBool(daily, "transactionPreviousReceiptCaptured", false))
			{
				string failure = (instance.InstanceId ?? "unknown")
					+ ": pending daily compensation transaction receipt is missing";
				MarkDailyCompensationRootFailure(daily, campaignDay, failure);
				failures.Add(failure);
				blockedInstances.Add(instance);
				stateChanged = true;
				continue;
			}
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.DailyMutation
				|| !(module is ICompensatingDailyPolicyEffectModule))
			{
				string failure = (instance.ModuleId ?? "unknown")
					+ ": pending daily compensation executor unavailable";
				MarkDailyCompensationRootFailure(daily, campaignDay, failure);
				failures.Add(failure);
				blockedInstances.Add(instance);
				stateChanged = true;
				continue;
			}
			if (!TryPrepare(instance, out _, out PolicyEffectPreparedInstance prepared, out string prepareError))
			{
				string failure = module.Id + ": "
					+ FirstNonEmpty(prepareError, "pending daily compensation prepare failed");
				MarkDailyCompensationRootFailure(daily, campaignDay, failure);
				failures.Add(failure);
				blockedInstances.Add(instance);
				stateChanged = true;
				continue;
			}

			JObject targets = daily["targets"] as JObject;
			int added = 0;
			foreach (JProperty property in targets?.Properties() ?? Enumerable.Empty<JProperty>())
			{
				if (!(property.Value is JObject progress)
					|| !ReadBool(progress, "compensationPending", false))
				{
					continue;
				}
				if (!Enum.TryParse(
					ReadString(progress, "targetKind"),
					ignoreCase: true,
					out PolicyEffectTargetKind targetKind)
					|| string.IsNullOrWhiteSpace(ReadString(progress, "targetId"))
					|| ReadInt(progress, "compensationCampaignDay", int.MinValue) == int.MinValue
					|| !TryReadExecutionReceipt(progress["compensationAppliedReceipt"], out PolicyEffectExecutionReceipt appliedReceipt))
				{
					string failure = (instance.InstanceId ?? "unknown")
						+ ": pending daily compensation target marker is invalid: " + property.Name;
					MarkDailyCompensationTargetFailure(progress, campaignDay, failure);
					MarkDailyCompensationRootFailure(daily, campaignDay, failure);
					failures.Add(failure);
					blockedInstances.Add(instance);
					stateChanged = true;
					continue;
				}
				pending.Add(new PolicyEffectPendingDailyCompensationLeg
				{
					Instance = instance,
					Module = module,
					Prepared = prepared,
					TargetKind = targetKind,
					TargetId = ReadString(progress, "targetId").Trim(),
					CampaignDay = ReadInt(progress, "compensationCampaignDay", campaignDay),
					Order = Math.Max(0, ReadInt(progress, "compensationOrder", 0)),
					AppliedReceipt = appliedReceipt,
					Progress = progress
				});
				added++;
			}
			if (added == 0 && !HasPendingDailyTargetCompensation(daily))
			{
				string failure = (instance.InstanceId ?? "unknown")
					+ ": pending daily compensation has no target marker";
				MarkDailyCompensationRootFailure(daily, campaignDay, failure);
				failures.Add(failure);
				blockedInstances.Add(instance);
				stateChanged = true;
			}
		}

		foreach (PolicyEffectPendingDailyCompensationLeg leg in pending
			.OrderByDescending(item => item.Order)
			.ThenByDescending(item => item.Instance?.InstanceId, StringComparer.Ordinal)
			.ThenByDescending(item => item.TargetId, StringComparer.OrdinalIgnoreCase))
		{
			PolicyEffectDailyExecutionOutcome outcome = new PolicyEffectDailyExecutionOutcome
			{
				Instance = leg.Instance,
				Module = leg.Module,
				Prepared = leg.Prepared,
				TargetKind = leg.TargetKind,
				TargetId = leg.TargetId,
				CampaignDay = leg.CampaignDay,
				AttemptWindowDay = campaignDay,
				AppliedReceipt = leg.AppliedReceipt
			};
			bool compensated = TryCompensateDailyTargetAfterPersistenceFailure(
				outcome,
				gameBridge,
				out string compensationError);
			stateChanged = true;
			if (compensated)
			{
				leg.Progress["status"] = "compensated";
				leg.Progress["compensatedDay"] = leg.CampaignDay;
				leg.Progress["lastCompensationAttemptDay"] = campaignDay;
				ClearDailyTargetCompensationPending(leg.Progress);
			}
			else
			{
				string failure = (leg.Instance?.InstanceId ?? "unknown")
					+ "/" + leg.TargetKind + ":" + leg.TargetId + ": "
					+ FirstNonEmpty(compensationError, "daily compensation failed");
				MarkDailyCompensationTargetFailure(leg.Progress, campaignDay, failure);
				MarkDailyCompensationRootFailure(
					EnsureDailyProgress(EnsureRuntimeStateRoot(leg.Instance)),
					campaignDay,
					failure);
				failures.Add(failure);
			}
		}

		foreach (PolicyEffectInstanceSaveData instance in pendingInstances)
		{
			JObject daily = EnsureDailyProgress(EnsureRuntimeStateRoot(instance));
			if (blockedInstances.Contains(instance))
			{
				daily["compensationPending"] = true;
				daily["status"] = "compensationPending";
				if (HasPendingDailyTargetCompensation(daily)
					&& !TrySynchronizePendingDailyReceipt(instance, receipts, out string blockedReceiptError))
				{
					MarkDailyCompensationRootFailure(daily, campaignDay, blockedReceiptError);
					failures.Add((instance.InstanceId ?? "unknown") + ": " + blockedReceiptError);
				}
				continue;
			}
			if (HasPendingDailyTargetCompensation(daily))
			{
				daily["compensationPending"] = true;
				daily["status"] = "compensationPending";
				if (!TrySynchronizePendingDailyReceipt(instance, receipts, out string receiptError))
				{
					MarkDailyCompensationRootFailure(daily, campaignDay, receiptError);
					failures.Add((instance.InstanceId ?? "unknown") + ": " + receiptError);
				}
				continue;
			}

			if (!TryReadDailyTransactionPreviousReceipt(
				daily,
				out PolicyEffectExecutionReceipt previousReceipt,
				out string previousReceiptError))
			{
				MarkDailyCompensationRootFailure(daily, campaignDay, previousReceiptError);
				failures.Add((instance.InstanceId ?? "unknown") + ": " + previousReceiptError);
				continue;
			}
			ReplaceReceiptForInstance(receipts, instance.InstanceId, previousReceipt);
			instance.ExecutionReceipt = previousReceipt;
			daily["status"] = "compensated";
			daily["compensatedDay"] = campaignDay;
			ClearDailyCompensationPending(daily);
		}

		error = string.Join("; ", failures.Distinct(StringComparer.Ordinal));
		return failures.Count == 0
			&& !pendingInstances.Any(HasPendingDailyCompensation);
	}

	internal static bool HasPendingScheduledCompensation(
		IEnumerable<PolicyEffectInstanceSaveData> instances)
	{
		return (instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Any(HasPendingScheduledCompensation);
	}

	internal static bool TryResumeScheduledCompensation(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		IList<PolicyEffectExecutionReceipt> receipts,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		out bool stateChanged,
		out string error)
	{
		stateChanged = false;
		error = string.Empty;
		List<PolicyEffectScheduledExecutionLeg> pending = new List<PolicyEffectScheduledExecutionLeg>();
		foreach (PolicyEffectInstanceSaveData instance in (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>()).Where(HasPendingScheduledCompensation))
		{
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.ScheduledOnce
				|| !(module is IScheduledOncePolicyEffectModule))
			{
				error = (instance.ModuleId ?? "unknown")
					+ ": pending scheduled compensation executor unavailable";
				return false;
			}
			if (!TryPrepare(instance, out _, out PolicyEffectPreparedInstance prepared, out string prepareError))
			{
				error = module.Id + ": "
					+ FirstNonEmpty(prepareError, "pending scheduled compensation prepare failed");
				return false;
			}
			PolicyEffectExecutionReceipt appliedReceipt = instance.ExecutionReceipt
				?? (receipts ?? Array.Empty<PolicyEffectExecutionReceipt>()).LastOrDefault(
					receipt => string.Equals(receipt?.InstanceId, instance.InstanceId, StringComparison.Ordinal));
			if (appliedReceipt == null)
			{
				error = module.Id + ": pending scheduled compensation receipt missing";
				return false;
			}
			pending.Add(new PolicyEffectScheduledExecutionLeg
			{
				Instance = instance,
				Module = module,
				Prepared = prepared,
				Receipt = appliedReceipt,
				PreviousReceipt = ReadScheduledPreviousReceipt(instance),
				PreviousLifecycleState = ReadScheduledPreviousLifecycleState(
					instance,
					PolicyEffectLifecycleState.Active)
			});
		}
		if (pending.Count == 0)
		{
			return true;
		}
		bool success = TryCompensateScheduledLegs(
			pending,
			receipts,
			gameBridge,
			campaignDay,
			out error);
		stateChanged = true;
		return success;
	}

	internal static bool TryCompensateScheduledOnceAfterPersistenceFailure(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		IList<PolicyEffectExecutionReceipt> receipts,
		IEnumerable<string> completedInstanceIds,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		out bool stateChanged,
		out string error)
	{
		stateChanged = false;
		error = string.Empty;
		List<string> orderedIds = (completedInstanceIds ?? Enumerable.Empty<string>())
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id.Trim())
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (orderedIds.Count == 0)
		{
			return true;
		}
		Dictionary<string, PolicyEffectInstanceSaveData> instancesById = (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(item => item != null && !string.IsNullOrWhiteSpace(item.InstanceId))
			.GroupBy(item => item.InstanceId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
		List<PolicyEffectScheduledExecutionLeg> legs = new List<PolicyEffectScheduledExecutionLeg>();
		foreach (string instanceId in orderedIds)
		{
			if (!instancesById.TryGetValue(instanceId, out PolicyEffectInstanceSaveData instance))
			{
				error = "scheduled persistence compensation instance not found: " + instanceId;
				return false;
			}
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.ScheduledOnce
				|| !(module is IScheduledOncePolicyEffectModule))
			{
				error = (instance.ModuleId ?? "unknown") + ": scheduled persistence compensation executor unavailable";
				return false;
			}
			if (!TryPrepare(instance, out _, out PolicyEffectPreparedInstance prepared, out string prepareError))
			{
				error = (instance.ModuleId ?? "unknown") + ": "
					+ FirstNonEmpty(prepareError, "scheduled persistence compensation prepare failed");
				return false;
			}
			legs.Add(new PolicyEffectScheduledExecutionLeg
			{
				Instance = instance,
				Module = module,
				Prepared = prepared,
				Receipt = instance.ExecutionReceipt,
				PreviousReceipt = ReadScheduledPreviousReceipt(instance),
				PreviousLifecycleState = ReadScheduledPreviousLifecycleState(
					instance,
					PolicyEffectLifecycleState.Active)
			});
		}
		bool success = TryCompensateScheduledLegs(
			legs,
			receipts,
			gameBridge,
			campaignDay,
			out error);
		stateChanged = legs.Count > 0;
		return success;
	}

	private static bool TryExecuteScheduledGroup(
		IReadOnlyList<PolicyEffectScheduledExecutionLeg> groupLegs,
		IList<PolicyEffectExecutionReceipt> receipts,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		out List<PolicyEffectScheduledExecutionLeg> appliedGroup,
		out bool compensationFailed,
		out string error)
	{
		appliedGroup = new List<PolicyEffectScheduledExecutionLeg>();
		compensationFailed = false;
		error = string.Empty;
		if (!TryPreflightScheduledHeroGoldGroup(
			groupLegs,
			gameBridge,
			out bool skipGroup,
			out string preflightMessage,
			out error))
		{
			return false;
		}
		if (skipGroup)
		{
			foreach (PolicyEffectScheduledExecutionLeg leg in groupLegs)
			{
				PolicyEffectExecutionReceipt receipt = CreateScheduledReceipt(
					leg,
					campaignDay,
					PolicyEffectExecutionStatus.Skipped);
				PopulateSkippedHeroGoldReceipt(leg, gameBridge, receipt);
				receipt.Message = preflightMessage;
				leg.Receipt = receipt;
				leg.Instance.ExecutionReceipt = receipt;
				leg.Instance.LifecycleState = PolicyEffectLifecycleState.Completed;
				AddOrReplaceReceipt(receipts, receipt);
				JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(leg.Instance));
				progress["status"] = "completed";
				progress["completedDay"] = campaignDay;
				progress["skipReason"] = preflightMessage;
			}
			appliedGroup = groupLegs.ToList();
			return true;
		}
		int attempts = groupLegs.Count == 0
			? ScheduledExecutionMaxAttempts
			: groupLegs.Max(leg => ReadScheduledAttempts(leg.Instance, campaignDay));
		while (attempts < ScheduledExecutionMaxAttempts)
		{
			attempts++;
			List<PolicyEffectScheduledExecutionLeg> appliedAttempt = new List<PolicyEffectScheduledExecutionLeg>();
			PolicyEffectScheduledExecutionLeg failingLeg = null;
			PolicyEffectExecutionResult failingResult = null;
			foreach (PolicyEffectScheduledExecutionLeg leg in groupLegs)
			{
				JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(leg.Instance));
				progress["attemptDay"] = campaignDay;
				progress["attemptCount"] = attempts;
				progress["lastAttemptDay"] = campaignDay;
				string idempotencyKey = BuildScheduledIdempotencyKey(leg.Instance.InstanceId, campaignDay);
				progress["lastIdempotencyKey"] = idempotencyKey;
				PolicyEffectExecutionResult result;
				try
				{
					result = ((IScheduledOncePolicyEffectModule)leg.Module).ExecuteScheduledOnce(
						new PolicyEffectExecutionContext
						{
							PreparedInstance = leg.Prepared,
							CampaignDay = campaignDay,
							GameBridge = gameBridge,
							ExistingReceipt = leg.Instance.ExecutionReceipt,
							IdempotencyKey = idempotencyKey,
							Attempt = attempts,
							RuntimeState = GetModuleRuntimeState(leg.Instance)
						});
				}
				catch (Exception ex)
				{
					result = new PolicyEffectExecutionResult
					{
						Status = PolicyEffectExecutionStatus.Failed,
						Error = ex.Message,
						Retryable = false
					};
				}
				ApplyModuleRuntimeState(leg.Instance, result?.RuntimeState);
				if (result != null && (result.Success || result.Status == PolicyEffectExecutionStatus.Skipped))
				{
					leg.Receipt = result.Receipt ?? CreateScheduledReceipt(leg, campaignDay, result.Status);
					leg.Instance.ExecutionReceipt = leg.Receipt;
					AddOrReplaceReceipt(receipts, leg.Receipt);
					appliedAttempt.Add(leg);
					continue;
				}
				failingLeg = leg;
				failingResult = result;
				if (result?.Receipt != null)
				{
					leg.Receipt = result.Receipt;
					leg.Instance.ExecutionReceipt = result.Receipt;
					appliedAttempt.Add(leg);
				}
				break;
			}

			if (failingLeg == null)
			{
				foreach (PolicyEffectScheduledExecutionLeg leg in appliedAttempt)
				{
					leg.Instance.LifecycleState = PolicyEffectLifecycleState.Completed;
					JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(leg.Instance));
					progress["status"] = "completed";
					progress["completedDay"] = campaignDay;
					progress.Remove("lastError");
				}
				appliedGroup = appliedAttempt;
				return true;
			}

			if (!TryCompensateScheduledLegs(
				appliedAttempt,
				receipts,
				gameBridge,
				campaignDay,
				out string compensationError))
			{
				compensationFailed = true;
				error = FirstNonEmpty(failingResult?.Error, "scheduled execution failed")
					+ "; compensation=" + compensationError;
				return false;
			}
			string failure = FirstNonEmpty(failingResult?.Error, "scheduled module returned a fatal failure");
			foreach (PolicyEffectScheduledExecutionLeg leg in groupLegs)
			{
				EnsureScheduledProgress(EnsureRuntimeStateRoot(leg.Instance))["lastError"] = failure;
			}
			if (failingResult?.Retryable == true && attempts < ScheduledExecutionMaxAttempts)
			{
				continue;
			}
			error = failure;
			return false;
		}
		error = "scheduled retry attempts exhausted";
		return false;
	}

	private static bool TryPreflightScheduledHeroGoldGroup(
		IReadOnlyList<PolicyEffectScheduledExecutionLeg> groupLegs,
		IPolicyEffectGameBridge gameBridge,
		out bool skipGroup,
		out string skipReason,
		out string error)
	{
		skipGroup = false;
		skipReason = string.Empty;
		error = string.Empty;
		List<PolicyEffectScheduledExecutionLeg> legs = (groupLegs ?? Array.Empty<PolicyEffectScheduledExecutionLeg>()).ToList();
		List<PolicyEffectScheduledExecutionLeg> atomic = legs
			.Where(leg => leg?.Module is IAtomicHeroGoldPolicyEffectModule)
			.ToList();
		if (atomic.Count == 0)
		{
			return true;
		}
		if (gameBridge == null || atomic.Count != legs.Count)
		{
			error = "scheduled Hero gold group is incomplete or lacks game bridge";
			return false;
		}

		if (atomic.Any(leg => leg.Instance?.MechanismKind != PolicyEffectMechanismKind.Independent))
		{
			error = "scheduled Hero gold effects only support independent mechanisms";
			return false;
		}

		foreach (PolicyEffectScheduledExecutionLeg leg in atomic)
		{
			if (!((IAtomicHeroGoldPolicyEffectModule)leg.Module).TryReadDelta(leg.Prepared.Instance.Payload, out int delta))
			{
				error = "scheduled Hero gold payload is invalid";
				return false;
			}
			foreach (string heroId in NormalizeTargetIds(leg.Prepared.Instance.TargetSet?.HeroIds))
			{
				if (!TryPreflightHeroGoldValue(gameBridge, heroId, delta, out bool shouldSkip, out string reason, out error))
				{
					return false;
				}
				if (shouldSkip)
				{
					skipGroup = true;
					skipReason = reason;
					return true;
				}
			}
		}
		return true;
	}

	private static bool TryPreflightHeroGoldValue(
		IPolicyEffectGameBridge gameBridge,
		string heroId,
		int delta,
		out bool shouldSkip,
		out string skipReason,
		out string error)
	{
		shouldSkip = false;
		skipReason = string.Empty;
		error = string.Empty;
		if (!gameBridge.TryReadHeroGold(heroId, out bool available, out int before, out string bridgeError))
		{
			error = "hero gold preflight failed for " + heroId + ": " + bridgeError;
			return false;
		}
		long after = (long)before + delta;
		if (!available || after < 0 || after > int.MaxValue)
		{
			shouldSkip = true;
			skipReason = !available
				? "hero target unavailable: " + heroId
				: after < 0
					? "insufficient hero gold: " + heroId
					: "hero gold overflow: " + heroId;
		}
		return true;
	}

	private static List<string> NormalizeTargetIds(IEnumerable<string> values)
	{
		return (values ?? Array.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private static bool TryCompensateScheduledLegs(
		IEnumerable<PolicyEffectScheduledExecutionLeg> source,
		IList<PolicyEffectExecutionReceipt> receipts,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		out string error)
	{
		List<string> failures = new List<string>();
		foreach (PolicyEffectScheduledExecutionLeg leg in (source ?? Enumerable.Empty<PolicyEffectScheduledExecutionLeg>()).Reverse())
		{
			if (leg?.Instance == null || leg.Receipt == null || !(leg.Module is IScheduledOncePolicyEffectModule scheduled))
			{
				continue;
			}
			if (leg.Receipt.Status == PolicyEffectExecutionStatus.Skipped)
			{
				RemoveReceipt(receipts, leg.Receipt);
				AddOrReplaceReceipt(receipts, leg.PreviousReceipt);
				leg.Instance.ExecutionReceipt = leg.PreviousReceipt;
				leg.Instance.LifecycleState = leg.PreviousLifecycleState;
				continue;
			}
			PolicyEffectExecutionResult compensation;
			try
			{
				compensation = scheduled.CompensateScheduledOnce(new PolicyEffectExecutionContext
				{
					PreparedInstance = leg.Prepared,
					CampaignDay = campaignDay,
					GameBridge = gameBridge,
					ExistingReceipt = leg.Receipt,
					IdempotencyKey = leg.Prepared.IdempotencyKey + ":compensate:" + campaignDay.ToString(CultureInfo.InvariantCulture),
					Attempt = 1,
					RuntimeState = GetModuleRuntimeState(leg.Instance)
				});
			}
			catch (Exception ex)
			{
				compensation = new PolicyEffectExecutionResult
				{
					Status = PolicyEffectExecutionStatus.Failed,
					Error = ex.Message
				};
			}
			ApplyModuleRuntimeState(leg.Instance, compensation?.RuntimeState);
			if (compensation == null
				|| (!compensation.Success && compensation.Status != PolicyEffectExecutionStatus.Skipped))
			{
				string failure = FirstNonEmpty(compensation?.Error, "scheduled compensation failed");
				MarkScheduledCompensationPending(leg, receipts, campaignDay, failure);
				failures.Add(leg.Module.Id + ": " + failure);
				continue;
			}
			RemoveReceipt(receipts, leg.Receipt);
			AddOrReplaceReceipt(receipts, leg.PreviousReceipt);
			leg.Instance.ExecutionReceipt = leg.PreviousReceipt;
			leg.Instance.LifecycleState = leg.PreviousLifecycleState;
			JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(leg.Instance));
			progress["status"] = "compensated";
			progress["compensatedDay"] = campaignDay;
			ClearScheduledCompensationPending(progress);
			progress.Remove("completedDay");
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	internal static PolicyEffectDailyExecutionOutcome ExecuteDailyTarget(
		PolicyEffectInstanceSaveData instance,
		IPolicyEffectModule module,
		PolicyEffectPreparedInstance prepared,
		PolicyEffectTargetKind targetKind,
		string targetId,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay)
	{
		return ExecuteDailyTarget(
			instance,
			module,
			prepared,
			targetKind,
			targetId,
			gameBridge,
			campaignDay,
			campaignDay);
	}

	internal static PolicyEffectDailyExecutionOutcome ExecuteDailyTarget(
		PolicyEffectInstanceSaveData instance,
		IPolicyEffectModule module,
		PolicyEffectPreparedInstance prepared,
		PolicyEffectTargetKind targetKind,
		string targetId,
		IPolicyEffectGameBridge gameBridge,
		int campaignDay,
		int attemptWindowDay)
	{
		PolicyEffectDailyExecutionOutcome outcome = new PolicyEffectDailyExecutionOutcome
		{
			Instance = instance,
			Module = module,
			Prepared = prepared,
			TargetKind = targetKind,
			TargetId = (targetId ?? string.Empty).Trim(),
			CampaignDay = campaignDay,
			AttemptWindowDay = attemptWindowDay,
			PreviousReceipt = instance?.ExecutionReceipt
		};
		string normalizedTargetId = (targetId ?? string.Empty).Trim();
		if (instance == null
			|| instance.LifecycleState != PolicyEffectLifecycleState.Active
			|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.DailyMutation
			|| !(module is IDailyPolicyEffectModule dailyModule)
			|| prepared?.Instance == null
			|| !string.Equals(instance.InstanceId, prepared.Instance.InstanceId, StringComparison.Ordinal)
			|| normalizedTargetId.Length == 0)
		{
			return outcome;
		}
		if (!PolicyEffectSaveCodec.TryMigrateKnownModuleRuntimeState(instance, module, out string migrationError))
		{
			instance.LifecycleState = PolicyEffectLifecycleState.Failed;
			outcome.Status = PolicyEffectExecutionStatus.Failed;
			outcome.StateChanged = true;
			outcome.Failed = true;
			outcome.Error = migrationError;
			return outcome;
		}

		JObject root = EnsureRuntimeStateRoot(instance);
		JObject progress = EnsureDailyTargetProgress(root, targetKind, normalizedTargetId);
		if (ReadInt(progress, "lastSucceededDay", int.MinValue) == campaignDay)
		{
			outcome.Status = PolicyEffectExecutionStatus.AlreadyApplied;
			outcome.Succeeded = true;
			return outcome;
		}

		int storedAttemptWindowDay = ReadInt(
			progress,
			"attemptWindowDay",
			ReadInt(progress, "attemptDay", int.MinValue));
		int attempts = storedAttemptWindowDay == attemptWindowDay
			? Math.Max(0, ReadInt(progress, "attemptCount", 0))
			: 0;
		if (attempts >= DailyExecutionMaxAttempts)
		{
			bool retryable = ReadBool(progress, "lastRetryable", false);
			if (!retryable)
			{
				instance.LifecycleState = PolicyEffectLifecycleState.Failed;
			}
			outcome.Status = PolicyEffectExecutionStatus.Failed;
			outcome.StateChanged = !retryable;
			outcome.Failed = true;
			outcome.Retryable = retryable;
			outcome.Attempts = attempts;
			outcome.Error = FirstNonEmpty(ReadString(progress, "lastError"), "daily retry attempts exhausted");
			return outcome;
		}

		string idempotencyKey = BuildDailyIdempotencyKey(instance.InstanceId, targetKind, normalizedTargetId, campaignDay);
		while (attempts < DailyExecutionMaxAttempts)
		{
			attempts++;
			progress["attemptDay"] = campaignDay;
			progress["attemptWindowDay"] = attemptWindowDay;
			progress["attemptCount"] = attempts;
			progress["lastAttemptDay"] = attemptWindowDay;
			progress["logicalDay"] = campaignDay;
			progress["lastIdempotencyKey"] = idempotencyKey;
			outcome.StateChanged = true;
			outcome.Attempts = attempts;

			PolicyEffectExecutionResult result;
			try
			{
				result = dailyModule.ExecuteDaily(new PolicyEffectExecutionContext
				{
					PreparedInstance = prepared,
					CampaignDay = campaignDay,
					GameBridge = gameBridge,
					ExistingReceipt = instance.ExecutionReceipt,
					IdempotencyKey = idempotencyKey,
					TargetKind = targetKind,
					TargetId = normalizedTargetId,
					Attempt = attempts,
					RuntimeState = GetModuleRuntimeState(instance)
				});
			}
			catch (Exception ex)
			{
				result = new PolicyEffectExecutionResult
				{
					Status = PolicyEffectExecutionStatus.Failed,
					Error = ex.Message,
					Retryable = false
				};
			}

			ApplyModuleRuntimeState(instance, result?.RuntimeState);
			PolicyEffectExecutionStatus status = result?.Status ?? PolicyEffectExecutionStatus.Failed;
			bool succeeded = result != null && (result.Success || status == PolicyEffectExecutionStatus.Skipped);
			progress["lastStatus"] = status.ToString();
			if (succeeded)
			{
				if (result?.Receipt != null)
				{
					outcome.AppliedReceipt = result.Receipt;
					instance.ExecutionReceipt = result.Receipt;
				}
				progress["lastSucceededDay"] = campaignDay;
				progress.Remove("lastError");
				progress.Remove("lastRetryable");
				progress.Remove("failureKind");
				outcome.Status = status;
				outcome.Succeeded = true;
				return outcome;
			}

			string failure = FirstNonEmpty(result?.Error, "daily module returned a fatal failure");
			progress["lastError"] = failure;
			progress["lastRetryable"] = result?.Retryable == true;
			if (result?.Receipt?.Status == PolicyEffectExecutionStatus.Applied
				&& module is ICompensatingDailyPolicyEffectModule)
			{
				// A compensating daily module can fail after a partial mutation. Preserve
				// that exact target receipt and return immediately so the outer daily
				// transaction compensates it before any retry can run.
				outcome.AppliedReceipt = result.Receipt;
				instance.ExecutionReceipt = result.Receipt;
				progress["failureKind"] = "partialMutation";
				if (result.Retryable != true)
				{
					instance.LifecycleState = PolicyEffectLifecycleState.Failed;
				}
				outcome.Status = PolicyEffectExecutionStatus.Failed;
				outcome.Failed = true;
				outcome.Retryable = result.Retryable == true;
				outcome.Error = failure;
				return outcome;
			}
			if (result?.Retryable == true && attempts < DailyExecutionMaxAttempts)
			{
				continue;
			}

			progress["failedDay"] = campaignDay;
			progress["failureKind"] = result?.Retryable == true ? "retryWindowExhausted" : "fatal";
			if (result?.Retryable != true)
			{
				instance.LifecycleState = PolicyEffectLifecycleState.Failed;
			}
			outcome.Status = PolicyEffectExecutionStatus.Failed;
			outcome.Failed = true;
			outcome.Retryable = result?.Retryable == true;
			outcome.Error = failure;
			return outcome;
		}

		bool exhaustedRetryable = ReadBool(progress, "lastRetryable", false);
		if (!exhaustedRetryable)
		{
			instance.LifecycleState = PolicyEffectLifecycleState.Failed;
		}
		outcome.Status = PolicyEffectExecutionStatus.Failed;
		outcome.Failed = true;
		outcome.Retryable = exhaustedRetryable;
		outcome.Error = "daily retry attempts exhausted";
		return outcome;
	}

	internal static bool TryCompensateDailyTargetAfterPersistenceFailure(
		PolicyEffectDailyExecutionOutcome outcome,
		IPolicyEffectGameBridge gameBridge,
		out string error)
	{
		error = string.Empty;
		if (outcome?.Instance == null
			|| outcome.Prepared?.Instance == null
			|| outcome.AppliedReceipt == null
			|| !(outcome.Module is ICompensatingDailyPolicyEffectModule compensatingModule))
		{
			return true;
		}
		PolicyEffectExecutionResult compensation;
		try
		{
			compensation = compensatingModule.CompensateDaily(new PolicyEffectExecutionContext
			{
				PreparedInstance = outcome.Prepared,
				CampaignDay = outcome.CampaignDay,
				GameBridge = gameBridge,
				ExistingReceipt = outcome.AppliedReceipt,
				IdempotencyKey = BuildDailyIdempotencyKey(
					outcome.Instance.InstanceId,
					outcome.TargetKind,
					outcome.TargetId,
					outcome.CampaignDay) + ":compensate",
				TargetKind = outcome.TargetKind,
				TargetId = outcome.TargetId,
				Attempt = 1,
				RuntimeState = GetModuleRuntimeState(outcome.Instance)
			});
		}
		catch (Exception ex)
		{
			compensation = new PolicyEffectExecutionResult
			{
				Status = PolicyEffectExecutionStatus.Failed,
				Error = ex.Message
			};
		}
		ApplyModuleRuntimeState(outcome.Instance, compensation?.RuntimeState);
		if (compensation == null
			|| (!compensation.Success && compensation.Status != PolicyEffectExecutionStatus.Skipped))
		{
			error = FirstNonEmpty(compensation?.Error, "daily persistence compensation failed");
			return false;
		}
		outcome.Instance.ExecutionReceipt = outcome.PreviousReceipt;
		JObject progress = EnsureDailyTargetProgress(
			EnsureRuntimeStateRoot(outcome.Instance),
			outcome.TargetKind,
			outcome.TargetId);
		if (ReadInt(progress, "lastSucceededDay", int.MinValue) == outcome.CampaignDay)
		{
			progress.Remove("lastSucceededDay");
		}
		progress["compensatedDay"] = outcome.CampaignDay;
		return true;
	}

	internal static bool TryDispatchLifecycle(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		PolicyEffectLifecycleEventKind eventKind,
		string eventKey,
		IPolicyEffectGameBridge gameBridge,
		float campaignDay,
		out bool stateChanged,
		out string error)
	{
		stateChanged = false;
		error = string.Empty;
		// Kept in the signature for source compatibility. Lifecycle callbacks are pure
		// RuntimeState transitions; external game mechanics belong in OneShot/Daily.
		_ = gameBridge;
		List<string> failures = new List<string>();
		string normalizedEventKey = FirstNonEmpty(eventKey, campaignDay.ToString("0.###", CultureInfo.InvariantCulture));
		foreach (PolicyEffectInstanceSaveData instance in instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			if (instance == null
				|| instance.LifecycleState == PolicyEffectLifecycleState.Failed
				|| instance.LifecycleState == PolicyEffectLifecycleState.RolledBack
				|| !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| !(module is IPolicyEffectLifecycleModule lifecycleModule))
			{
				continue;
			}

			JObject root = EnsureRuntimeStateRoot(instance);
			JObject framework = EnsureObject(root, PolicyEffectRuntimeStateEnvelope.FrameworkProperty);
			JObject lifecycle = EnsureObject(framework, LifecycleRuntimeStateProperty);
			string stateKey = eventKind.ToString() + ":" + normalizedEventKey;
			if (string.Equals(ReadString(lifecycle[stateKey] as JObject, "status"), "completed", StringComparison.Ordinal))
			{
				continue;
			}
			if (!TryPrepare(instance, out _, out PolicyEffectPreparedInstance prepared, out string prepareError))
			{
				failures.Add(module.Id + ": " + FirstNonEmpty(prepareError, "lifecycle prepare failed"));
				continue;
			}

			string idempotencyKey = instance.InstanceId + ":lifecycle:" + eventKind + ":" + normalizedEventKey;
			PolicyEffectExecutionContext context = new PolicyEffectExecutionContext
			{
				PreparedInstance = prepared,
				CampaignDay = campaignDay,
				GameBridge = null,
				ExistingReceipt = instance.ExecutionReceipt,
				IdempotencyKey = idempotencyKey,
				Attempt = 1,
				RuntimeState = GetModuleRuntimeState(instance)
			};
			PolicyEffectExecutionResult result;
			try
			{
				switch (eventKind)
				{
					case PolicyEffectLifecycleEventKind.Activated:
						result = lifecycleModule.OnActivated(context);
						break;
					case PolicyEffectLifecycleEventKind.Renewed:
						result = lifecycleModule.OnRenewed(context);
						break;
					case PolicyEffectLifecycleEventKind.Expired:
						result = lifecycleModule.OnExpired(context);
						break;
					case PolicyEffectLifecycleEventKind.Abolished:
						result = lifecycleModule.OnAbolished(context);
						break;
					default:
						result = null;
						break;
				}
			}
			catch (Exception ex)
			{
				result = new PolicyEffectExecutionResult
				{
					Status = PolicyEffectExecutionStatus.Failed,
					Error = ex.Message
				};
			}

			ApplyModuleRuntimeState(instance, result?.RuntimeState);
			JObject eventState = lifecycle[stateKey] as JObject ?? new JObject();
			eventState["campaignDay"] = campaignDay;
			eventState["idempotencyKey"] = idempotencyKey;
			if (result != null && (result.Success || result.Status == PolicyEffectExecutionStatus.Skipped))
			{
				eventState["status"] = "completed";
				eventState.Remove("error");
			}
			else
			{
				eventState["status"] = "failed";
				eventState["error"] = FirstNonEmpty(result?.Error, "lifecycle callback failed");
				failures.Add(module.Id + ": " + eventState["error"]?.ToString());
			}
			lifecycle[stateKey] = eventState;
			stateChanged = true;
		}
		error = string.Join("; ", failures);
		return failures.Count == 0;
	}

	internal static bool TryPrepareSavedInstance(
		PolicyEffectInstanceSaveData source,
		out IPolicyEffectModule module,
		out PolicyEffectPreparedInstance prepared,
		out string error)
	{
		return TryPrepare(source, out module, out prepared, out error);
	}

	private static bool TryPrepare(
		PolicyEffectInstanceSaveData source,
		out IPolicyEffectModule module,
		out PolicyEffectPreparedInstance prepared,
		out string error)
	{
		module = null;
		prepared = null;
		if (!PolicyEffectSaveCodec.TryNormalizeInstance(source, out PolicyEffectNormalizedInstance normalized, out error)
			|| normalized.IsInert
			|| normalized.RuntimeInstance == null
			|| !PolicyEffectModuleCatalog.TryGet(normalized.RuntimeInstance.ModuleId, out module))
		{
			if (string.IsNullOrWhiteSpace(error))
			{
				error = normalized?.InertReason ?? "module instance is inert";
			}
			return false;
		}
		PolicyEffectInstance runtime = normalized.RuntimeInstance;
		string idempotencyKey = runtime.InstanceId + ":v" + source.PayloadSchemaVersion.ToString(CultureInfo.InvariantCulture);
		PolicyEffectPrepareResult prepare = module.Prepare(new PolicyEffectCompileContext
		{
			InstanceId = runtime.InstanceId,
			PolicyId = runtime.PolicyId,
			ActorHeroId = runtime.ActorHeroId,
			Module = module,
			TargetSet = runtime.TargetSet,
			Payload = runtime.Payload,
			IdempotencyKey = idempotencyKey,
			StartDay = runtime.StartDay,
			EndDay = runtime.EndDay,
			SourceScope = runtime.SourceScope,
			Reason = runtime.Reason
		}, runtime.Payload);
		if (prepare?.Success != true || prepare.PreparedInstance?.Instance == null)
		{
			error = prepare?.Error ?? "module prepare returned no instance";
			return false;
		}
		PolicyEffectInstance preparedRuntime = prepare.PreparedInstance.Instance;
		preparedRuntime.MechanismContractVersion = runtime.MechanismContractVersion;
		preparedRuntime.MechanismContractHash = runtime.MechanismContractHash ?? string.Empty;
		preparedRuntime.ExpectedMechanismLegIds = new List<string>(runtime.ExpectedMechanismLegIds ?? new List<string>());
		preparedRuntime.EffectPlanVersion = runtime.EffectPlanVersion;
		preparedRuntime.MechanismId = runtime.MechanismId;
		preparedRuntime.MechanismKind = runtime.MechanismKind;
		preparedRuntime.MechanismRole = runtime.MechanismRole;
		preparedRuntime.SourceOmitted = runtime.SourceOmitted;
		preparedRuntime.DestinationOmitted = runtime.DestinationOmitted;
		preparedRuntime.ActorHeroId = runtime.ActorHeroId;
		prepared = prepare.PreparedInstance;
		return true;
	}

	private static string BuildDailyIdempotencyKey(
		string instanceId,
		PolicyEffectTargetKind targetKind,
		string targetId,
		int campaignDay)
	{
		return (instanceId ?? string.Empty).Trim()
			+ ":daily:" + targetKind
			+ ":" + (targetId ?? string.Empty).Trim().ToUpperInvariant()
			+ ":" + campaignDay.ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildScheduledIdempotencyKey(string instanceId, int campaignDay)
	{
		return (instanceId ?? string.Empty).Trim()
			+ ":scheduledOnce:"
			+ campaignDay.ToString(CultureInfo.InvariantCulture);
	}

	private static int ReadScheduledAttempts(PolicyEffectInstanceSaveData instance, int campaignDay)
	{
		JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(instance));
		return ReadInt(progress, "attemptDay", int.MinValue) == campaignDay
			? Math.Max(0, ReadInt(progress, "attemptCount", 0))
			: 0;
	}

	private static void FreezeScheduledTargets(
		PolicyEffectInstanceSaveData instance,
		int campaignDay,
		PolicyEffectLifecycleState previousLifecycleState,
		PolicyEffectExecutionReceipt previousReceipt)
	{
		JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(instance));
		if (ReadInt(progress, "frozenDay", int.MinValue) != int.MinValue)
		{
			return;
		}
		instance.TargetSet = CloneCanonicalTargetSet(instance.TargetSet);
		progress["frozenDay"] = campaignDay;
		progress["previousLifecycleState"] = previousLifecycleState.ToString();
		progress["hadPreviousExecutionReceipt"] = previousReceipt != null;
		if (previousReceipt != null)
		{
			progress["previousExecutionReceipt"] = JToken.FromObject(previousReceipt);
		}
		else
		{
			progress.Remove("previousExecutionReceipt");
		}
		progress["status"] = "targetsFrozen";
	}

	private static bool HasPendingScheduledCompensation(PolicyEffectInstanceSaveData instance)
	{
		if (!(instance?.RuntimeState is JObject root)
			|| !(root[PolicyEffectRuntimeStateEnvelope.FrameworkProperty] is JObject framework)
			|| !(framework[ScheduledRuntimeStateProperty] is JObject progress))
		{
			return false;
		}
		return ReadBool(progress, "compensationPending", false);
	}

	private static void MarkScheduledCompensationPending(
		PolicyEffectScheduledExecutionLeg leg,
		IList<PolicyEffectExecutionReceipt> receipts,
		int campaignDay,
		string error)
	{
		if (leg?.Instance == null || leg.Receipt == null)
		{
			return;
		}
		leg.Instance.ExecutionReceipt = leg.Receipt;
		leg.Instance.LifecycleState = PolicyEffectLifecycleState.Completed;
		AddOrReplaceReceipt(receipts, leg.Receipt);
		JObject progress = EnsureScheduledProgress(EnsureRuntimeStateRoot(leg.Instance));
		progress["compensationPending"] = true;
		if (ReadInt(progress, "pendingSinceDay", int.MinValue) == int.MinValue)
		{
			progress["pendingSinceDay"] = campaignDay;
		}
		progress["lastCompensationAttemptDay"] = campaignDay;
		progress["lastCompensationError"] = error ?? string.Empty;
		progress["status"] = "compensationPending";
	}

	private static void ClearScheduledCompensationPending(JObject progress)
	{
		if (progress == null)
		{
			return;
		}
		progress.Remove("compensationPending");
		progress.Remove("pendingSinceDay");
		progress.Remove("lastCompensationAttemptDay");
		progress.Remove("lastCompensationError");
	}

	private static PolicyEffectExecutionReceipt ReadScheduledPreviousReceipt(
		PolicyEffectInstanceSaveData instance)
	{
		if (!(instance?.RuntimeState is JObject root)
			|| !(root[PolicyEffectRuntimeStateEnvelope.FrameworkProperty] is JObject framework)
			|| !(framework[ScheduledRuntimeStateProperty] is JObject progress)
			|| !ReadBool(progress, "hadPreviousExecutionReceipt", false)
			|| progress["previousExecutionReceipt"] == null)
		{
			return null;
		}
		try
		{
			return progress["previousExecutionReceipt"].ToObject<PolicyEffectExecutionReceipt>();
		}
		catch
		{
			return null;
		}
	}

	private static PolicyEffectLifecycleState ReadScheduledPreviousLifecycleState(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectLifecycleState fallback)
	{
		if (!(instance?.RuntimeState is JObject root)
			|| !(root[PolicyEffectRuntimeStateEnvelope.FrameworkProperty] is JObject framework)
			|| !(framework[ScheduledRuntimeStateProperty] is JObject progress)
			|| !Enum.TryParse(
				ReadString(progress, "previousLifecycleState"),
				ignoreCase: true,
				out PolicyEffectLifecycleState saved)
			|| saved == PolicyEffectLifecycleState.Completed
			|| saved == PolicyEffectLifecycleState.RolledBack
			|| saved == PolicyEffectLifecycleState.Failed)
		{
			return fallback;
		}
		return saved;
	}

	private static JObject EnsureScheduledProgress(JObject root)
	{
		JObject framework = EnsureObject(root, PolicyEffectRuntimeStateEnvelope.FrameworkProperty);
		return EnsureObject(framework, ScheduledRuntimeStateProperty);
	}

	private static PolicyEffectExecutionReceipt CreateScheduledReceipt(
		PolicyEffectScheduledExecutionLeg leg,
		int campaignDay,
		PolicyEffectExecutionStatus status)
	{
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = (leg?.Instance?.InstanceId ?? string.Empty) + ":scheduledOnce:"
				+ campaignDay.ToString(CultureInfo.InvariantCulture),
			InstanceId = leg?.Instance?.InstanceId ?? string.Empty,
			PolicyId = leg?.Instance?.PolicyId ?? string.Empty,
			ModuleId = leg?.Instance?.ModuleId ?? string.Empty,
			TargetSet = CloneCanonicalTargetSet(leg?.Instance?.TargetSet),
			Status = status,
			CampaignDay = campaignDay,
			Message = status == PolicyEffectExecutionStatus.Skipped ? "scheduled target set skipped" : string.Empty
		};
	}

	private static void PopulateSkippedHeroGoldReceipt(
		PolicyEffectScheduledExecutionLeg leg,
		IPolicyEffectGameBridge gameBridge,
		PolicyEffectExecutionReceipt receipt)
	{
		if (leg?.Module is not IAtomicHeroGoldPolicyEffectModule module
			|| receipt == null
			|| !module.TryReadDelta(leg.Prepared?.Instance?.Payload, out int delta))
		{
			return;
		}
		JArray targets = new JArray();
		foreach (string heroId in NormalizeTargetIds(leg.Prepared?.Instance?.TargetSet?.HeroIds))
		{
			bool available = false;
			int before = 0;
			string readError = string.Empty;
			bool read = gameBridge != null && gameBridge.TryReadHeroGold(
				heroId,
				out available,
				out before,
				out readError);
			targets.Add(new JObject
			{
				["heroId"] = heroId,
				["requestedDelta"] = delta,
				["available"] = read && available,
				["before"] = read ? before : 0,
				["after"] = read ? before : 0,
				["actualDelta"] = 0,
				["readError"] = readError ?? string.Empty
			});
		}
		receipt.RequestedValue = delta;
		receipt.AppliedValue = 0f;
		receipt.RequestedPayload = new JObject { ["value"] = delta };
		receipt.AppliedPayload = new JObject { ["targets"] = targets };
	}

	private static void AddOrReplaceReceipt(
		IList<PolicyEffectExecutionReceipt> receipts,
		PolicyEffectExecutionReceipt receipt)
	{
		if (receipts == null || receipt == null)
		{
			return;
		}
		for (int index = receipts.Count - 1; index >= 0; index--)
		{
			PolicyEffectExecutionReceipt existing = receipts[index];
			if (string.Equals(existing?.InstanceId, receipt.InstanceId, StringComparison.Ordinal))
			{
				receipts[index] = receipt;
				return;
			}
		}
		receipts.Add(receipt);
	}

	private static void RemoveReceipt(
		IList<PolicyEffectExecutionReceipt> receipts,
		PolicyEffectExecutionReceipt receipt)
	{
		if (receipts == null || receipt == null)
		{
			return;
		}
		for (int index = receipts.Count - 1; index >= 0; index--)
		{
			PolicyEffectExecutionReceipt existing = receipts[index];
			if (ReferenceEquals(existing, receipt)
				|| (!string.IsNullOrWhiteSpace(receipt.ReceiptId)
					&& string.Equals(existing?.ReceiptId, receipt.ReceiptId, StringComparison.Ordinal)))
			{
				receipts.RemoveAt(index);
			}
		}
	}

	private static PolicyEffectCanonicalTargetSet CloneCanonicalTargetSet(PolicyEffectCanonicalTargetSet source)
	{
		if (source == null)
		{
			return new PolicyEffectCanonicalTargetSet();
		}
		return new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = source.StructureVersion,
			SelectorHandles = new List<string>(source.SelectorHandles ?? new List<string>()),
			SelectorIds = new List<string>(source.SelectorIds ?? new List<string>()),
			TargetPlans = (source.TargetPlans ?? new List<PolicyTargetPlanSaveData>())
				.Select(PolicyTargetPlanResolver.Clone)
				.Where(plan => plan != null)
				.ToList(),
			SettlementIds = new List<string>(source.SettlementIds ?? new List<string>()),
			TownIds = new List<string>(source.TownIds ?? new List<string>()),
			VillageIds = new List<string>(source.VillageIds ?? new List<string>()),
			ClanIds = new List<string>(source.ClanIds ?? new List<string>()),
			KingdomIds = new List<string>(source.KingdomIds ?? new List<string>()),
			HeroIds = new List<string>(source.HeroIds ?? new List<string>()),
			ParentSettlementIds = new List<string>(source.ParentSettlementIds ?? new List<string>()),
			FollowCurrentRulingClan = source.FollowCurrentRulingClan
		};
	}

	private static JObject EnsureRuntimeStateRoot(PolicyEffectInstanceSaveData instance)
	{
		JObject root = instance.RuntimeState as JObject;
		if (root == null)
		{
			root = new JObject();
			if (instance.RuntimeState != null && instance.RuntimeState.Type != JTokenType.Null)
			{
				root[PolicyEffectRuntimeStateEnvelope.ModuleProperty] = instance.RuntimeState.DeepClone();
			}
			instance.RuntimeState = root;
		}
		JObject framework = EnsureObject(root, PolicyEffectRuntimeStateEnvelope.FrameworkProperty);
		framework["schemaVersion"] = PolicyEffectRuntimeStateEnvelope.FrameworkSchemaVersion;
		instance.StateSchemaVersion = Math.Max(1, instance.StateSchemaVersion);
		return root;
	}

	private static JObject EnsureDailyTargetProgress(
		JObject root,
		PolicyEffectTargetKind targetKind,
		string targetId)
	{
		JObject daily = EnsureDailyProgress(root);
		JObject targets = EnsureObject(daily, "targets");
		string targetKey = targetKind + ":" + (targetId ?? string.Empty).Trim().ToUpperInvariant();
		JObject progress = targets[targetKey] as JObject;
		if (progress == null)
		{
			progress = new JObject
			{
				["targetKind"] = targetKind.ToString(),
				["targetId"] = (targetId ?? string.Empty).Trim()
			};
			targets[targetKey] = progress;
		}
		return progress;
	}

	private static JObject EnsureDailyProgress(JObject root)
	{
		JObject framework = EnsureObject(root, PolicyEffectRuntimeStateEnvelope.FrameworkProperty);
		return EnsureObject(framework, DailyRuntimeStateProperty);
	}

	private static bool HasPendingDailyCompensation(PolicyEffectInstanceSaveData instance)
	{
		if (!(instance?.RuntimeState is JObject root)
			|| !(root[PolicyEffectRuntimeStateEnvelope.FrameworkProperty] is JObject framework)
			|| !(framework[DailyRuntimeStateProperty] is JObject daily))
		{
			return false;
		}
		return ReadBool(daily, "compensationPending", false)
			|| HasPendingDailyTargetCompensation(daily);
	}

	private static bool HasPendingDailyTargetCompensation(JObject daily)
	{
		if (!(daily?["targets"] is JObject targets))
		{
			return false;
		}
		return targets.Properties().Any(property =>
			property.Value is JObject progress
			&& ReadBool(progress, "compensationPending", false));
	}

	private static bool TrySynchronizePendingDailyReceipt(
		PolicyEffectInstanceSaveData instance,
		IList<PolicyEffectExecutionReceipt> receipts,
		out string error)
	{
		error = string.Empty;
		if (instance == null)
		{
			error = "pending daily compensation instance is missing";
			return false;
		}
		JObject daily = EnsureDailyProgress(EnsureRuntimeStateRoot(instance));
		if (!(daily["targets"] is JObject targets))
		{
			error = "pending daily compensation targets are missing";
			return false;
		}
		PolicyEffectExecutionReceipt selected = null;
		int selectedOrder = int.MinValue;
		foreach (JProperty property in targets.Properties())
		{
			if (!(property.Value is JObject progress)
				|| !ReadBool(progress, "compensationPending", false))
			{
				continue;
			}
			if (!TryReadExecutionReceipt(
				progress["compensationAppliedReceipt"],
				out PolicyEffectExecutionReceipt appliedReceipt))
			{
				error = "pending daily compensation receipt is invalid: " + property.Name;
				return false;
			}
			int order = Math.Max(0, ReadInt(progress, "compensationOrder", 0));
			if (selected == null || order > selectedOrder)
			{
				selected = appliedReceipt;
				selectedOrder = order;
			}
		}
		if (selected == null)
		{
			error = "pending daily compensation receipt is missing";
			return false;
		}
		instance.ExecutionReceipt = selected;
		ReplaceReceiptForInstance(receipts, instance.InstanceId, selected);
		return true;
	}

	private static bool TryReadDailyTransactionPreviousReceipt(
		JObject daily,
		out PolicyEffectExecutionReceipt receipt,
		out string error)
	{
		receipt = null;
		error = string.Empty;
		if (!ReadBool(daily, "transactionPreviousReceiptCaptured", false))
		{
			error = "pending daily compensation transaction receipt was not captured";
			return false;
		}
		if (!ReadBool(daily, "transactionHadPreviousExecutionReceipt", false))
		{
			return true;
		}
		if (!TryReadExecutionReceipt(
			daily?["transactionPreviousExecutionReceipt"],
			out receipt))
		{
			error = "pending daily compensation transaction receipt is invalid";
			return false;
		}
		return true;
	}

	private static bool TryReadExecutionReceipt(
		JToken token,
		out PolicyEffectExecutionReceipt receipt)
	{
		receipt = null;
		if (token == null || token.Type == JTokenType.Null)
		{
			return false;
		}
		try
		{
			receipt = token.ToObject<PolicyEffectExecutionReceipt>();
			return receipt != null;
		}
		catch
		{
			return false;
		}
	}

	private static void MarkDailyCompensationRootFailure(
		JObject daily,
		int campaignDay,
		string error)
	{
		if (daily == null)
		{
			return;
		}
		daily["compensationPending"] = true;
		daily["lastCompensationAttemptDay"] = campaignDay;
		daily["lastCompensationError"] = error ?? string.Empty;
		daily["status"] = "compensationPending";
	}

	private static void MarkDailyCompensationTargetFailure(
		JObject progress,
		int campaignDay,
		string error)
	{
		if (progress == null)
		{
			return;
		}
		progress["compensationPending"] = true;
		progress["lastCompensationAttemptDay"] = campaignDay;
		progress["lastCompensationError"] = error ?? string.Empty;
		progress["status"] = "compensationPending";
	}

	private static void ClearDailyTargetCompensationPending(JObject progress)
	{
		if (progress == null)
		{
			return;
		}
		progress.Remove("compensationPending");
		progress.Remove("compensationOrder");
		progress.Remove("compensationCampaignDay");
		progress.Remove("compensationAppliedReceipt");
		progress.Remove("lastCompensationError");
	}

	private static void ClearDailyCompensationPending(JObject daily)
	{
		if (daily == null)
		{
			return;
		}
		daily.Remove("compensationPending");
		daily.Remove("pendingSinceDay");
		daily.Remove("lastCompensationAttemptDay");
		daily.Remove("lastCompensationError");
		daily.Remove("transactionPreviousReceiptCaptured");
		daily.Remove("transactionHadPreviousExecutionReceipt");
		daily.Remove("transactionPreviousExecutionReceipt");
	}

	private static void ReplaceReceiptForInstance(
		IList<PolicyEffectExecutionReceipt> receipts,
		string instanceId,
		PolicyEffectExecutionReceipt replacement)
	{
		if (receipts == null)
		{
			return;
		}
		for (int index = receipts.Count - 1; index >= 0; index--)
		{
			if (string.Equals(receipts[index]?.InstanceId, instanceId, StringComparison.Ordinal))
			{
				receipts.RemoveAt(index);
			}
		}
		if (replacement != null)
		{
			receipts.Add(replacement);
		}
	}

	private static JObject EnsureObject(JObject parent, string propertyName)
	{
		JObject value = parent?[propertyName] as JObject;
		if (value == null)
		{
			value = new JObject();
			if (parent != null)
			{
				parent[propertyName] = value;
			}
		}
		return value;
	}

	private static JToken GetModuleRuntimeState(PolicyEffectInstanceSaveData instance)
	{
		if (instance?.RuntimeState == null || instance.RuntimeState.Type == JTokenType.Null)
		{
			return null;
		}
		if (!(instance.RuntimeState is JObject root))
		{
			return instance.RuntimeState.DeepClone();
		}
		if (root.TryGetValue(PolicyEffectRuntimeStateEnvelope.ModuleProperty, StringComparison.Ordinal, out JToken moduleState))
		{
			return moduleState?.DeepClone();
		}
		JObject legacyState = (JObject)root.DeepClone();
		legacyState.Remove(PolicyEffectRuntimeStateEnvelope.FrameworkProperty);
		return legacyState.HasValues ? legacyState : null;
	}

	private static void ApplyModuleRuntimeState(PolicyEffectInstanceSaveData instance, JToken moduleRuntimeState)
	{
		if (instance == null || moduleRuntimeState == null)
		{
			return;
		}
		JObject root = EnsureRuntimeStateRoot(instance);
		root[PolicyEffectRuntimeStateEnvelope.ModuleProperty] = moduleRuntimeState.DeepClone();
		if (PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module))
		{
			instance.StateSchemaVersion = module.Descriptor.RuntimeStateSchemaVersion;
		}
	}

	private static int ReadInt(JObject source, string propertyName, int fallback)
	{
		try
		{
			return source?[propertyName]?.Value<int>() ?? fallback;
		}
		catch
		{
			return fallback;
		}
	}

	private static bool ReadBool(JObject source, string propertyName, bool fallback)
	{
		try
		{
			return source?[propertyName]?.Value<bool>() ?? fallback;
		}
		catch
		{
			return fallback;
		}
	}

	private static string ReadString(JObject source, string propertyName)
	{
		return source?[propertyName]?.Type == JTokenType.String
			? source[propertyName].Value<string>() ?? string.Empty
			: source?[propertyName]?.ToString() ?? string.Empty;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
			?? string.Empty;
	}

	private static PolicyEffectInstanceSaveData CreateSaveData(
		PolicyEffectInstance instance,
		PolicyEffectInstanceSaveData source)
	{
		return new PolicyEffectInstanceSaveData
		{
			MechanismContractVersion = source.MechanismContractVersion,
			MechanismContractHash = source.MechanismContractHash ?? string.Empty,
			ExpectedMechanismLegIds = new List<string>(source.ExpectedMechanismLegIds ?? new List<string>()),
			EffectPlanVersion = instance.EffectPlanVersion,
			MechanismId = instance.MechanismId ?? string.Empty,
			MechanismKind = instance.MechanismKind,
			MechanismRole = instance.MechanismRole,
			SourceOmitted = instance.SourceOmitted,
			DestinationOmitted = instance.DestinationOmitted,
			InstanceId = instance.InstanceId,
			PolicyId = instance.PolicyId,
			ActorHeroId = instance.ActorHeroId,
			ModuleId = instance.ModuleId,
			SourceModuleId = instance.SourceModuleId,
			PayloadSchemaVersion = source.PayloadSchemaVersion,
			Payload = source.Payload?.DeepClone(),
			TargetSet = instance.TargetSet,
			LifecycleState = instance.LifecycleState,
			StateSchemaVersion = source.StateSchemaVersion,
			RuntimeState = source.RuntimeState?.DeepClone(),
			ExecutionReceipt = source.ExecutionReceipt,
			StartDay = instance.StartDay,
			EndDay = instance.EndDay,
			SourceScope = instance.SourceScope,
			Reason = instance.Reason
		};
	}
}
