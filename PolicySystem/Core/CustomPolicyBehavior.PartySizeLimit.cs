using System;
using System.Collections.Generic;
using System.Reflection;
using AnimusForge.PolicyEffects;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private const string PolicyPartySizeLimitHarmonyId = "com.AnimusForge.custompolicy.partysizelimit";
	private const string PolicyPartySizeLimitClanLeaderModuleId = "partySizeLimitClanLeader";
	private const string PolicyPartySizeLimitClanLordsModuleId = "partySizeLimitClanLords";
	private const int PolicyPartySizeLimitMinimumValue = -200;
	private const int PolicyPartySizeLimitMaximumValue = 200;

	private static bool _policyPartySizeLimitPatchesApplied;

	private static void ApplyPolicyPartySizeLimitPatchesOnce()
	{
		if (_policyPartySizeLimitPatchesApplied || Campaign.Current?.Models?.PartySizeLimitModel == null)
		{
			return;
		}

		try
		{
			MethodInfo sizeLimitGetter = AccessTools.PropertyGetter(typeof(PartyBase), nameof(PartyBase.PartySizeLimit));
			MethodInfo explainerGetter = AccessTools.PropertyGetter(typeof(PartyBase), nameof(PartyBase.PartySizeLimitExplainer));
			if (sizeLimitGetter == null || sizeLimitGetter.ReturnType != typeof(int))
			{
				throw new MissingMethodException(typeof(PartyBase).FullName, "get_PartySizeLimit()");
			}
			if (explainerGetter == null || explainerGetter.ReturnType != typeof(ExplainedNumber))
			{
				throw new MissingMethodException(typeof(PartyBase).FullName, "get_PartySizeLimitExplainer()");
			}

			object activeModel = Campaign.Current.Models.PartySizeLimitModel;
			MethodInfo assumedPartySize = AccessTools.Method(
				activeModel.GetType(),
				nameof(PartySizeLimitModel.GetAssumedPartySizeForLordParty),
				new[] { typeof(Hero), typeof(IFaction), typeof(Clan) })?.GetDeclaredMember();
			if (assumedPartySize == null || assumedPartySize.ReturnType != typeof(int))
			{
				throw new MissingMethodException(
					activeModel.GetType().FullName,
					"GetAssumedPartySizeForLordParty(Hero, IFaction, Clan)");
			}

			Harmony harmony = new Harmony(PolicyPartySizeLimitHarmonyId);
			harmony.Patch(
				sizeLimitGetter,
				postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_PolicyPartySizeLimit_Postfix)));
			harmony.Patch(
				explainerGetter,
				postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_PolicyPartySizeLimitExplainer_Postfix)));
			harmony.Patch(
				assumedPartySize,
				postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_PolicyAssumedLordPartySize_Postfix)));
			_policyPartySizeLimitPatchesApplied = true;
			PolicySystemLog.Write(
				"Effect",
				"party-size-limit-patches-applied",
				"AF policy effects now participate in cached party size limits, explanations and assumed lord party sizes");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Effect", "party-size-limit-patches-failed", ex.ToString());
		}
	}

	private static void Patch_PolicyPartySizeLimit_Postfix(PartyBase __instance, ref int __result)
	{
		if (!TryGetEligiblePolicyLordParty(__instance, out _, out Clan clan, out Hero leader))
		{
			return;
		}
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = GetPolicyPartySizeLimitContributions(clan);
		__result = CalculatePolicyPartySizeAdjustedLimit(
			__result,
			contributions,
			ReferenceEquals(leader, clan.Leader));
	}

	private static void Patch_PolicyPartySizeLimitExplainer_Postfix(
		PartyBase __instance,
		ref ExplainedNumber __result)
	{
		if (!TryGetEligiblePolicyLordParty(__instance, out _, out Clan clan, out Hero leader))
		{
			return;
		}
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = GetPolicyPartySizeLimitContributions(clan);
		AddPolicyPartySizeLimitExplanations(
			contributions,
			ReferenceEquals(leader, clan.Leader),
			ref __result);
	}

	private static void Patch_PolicyAssumedLordPartySize_Postfix(
		Hero __0,
		IFaction __1,
		Clan __2,
		ref int __result)
	{
		_ = __1;
		Hero leader = __0;
		Clan clan = __2;
		if (!IsEligiblePolicyPartySizeHero(leader)
			|| clan == null
			|| clan.IsEliminated
			|| !ReferenceEquals(leader.Clan, clan))
		{
			return;
		}
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = GetPolicyPartySizeLimitContributions(clan);
		__result = CalculatePolicyPartySizeAdjustedLimit(
			__result,
			contributions,
			ReferenceEquals(leader, clan.Leader));
	}

	private static IReadOnlyList<PolicyEffectRuntimeContribution> GetPolicyPartySizeLimitContributions(Clan clan)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		string clanId = (clan?.StringId ?? string.Empty).Trim();
		return behavior == null || clanId.Length == 0
			? Array.Empty<PolicyEffectRuntimeContribution>()
			: behavior._policyEffectRuntimeIndex.GetContributions(
				PolicyEffectHook.PartyMemberSizeLimit,
				PolicyEffectTargetKind.Clan,
				clanId);
	}

	internal static int CalculatePolicyPartySizeAdjustedLimit(
		int originalLimit,
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions,
		bool isClanLeader)
	{
		long totalDelta = 0L;
		bool hasContribution = false;
		for (int index = 0; index < (contributions?.Count ?? 0); index++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[index];
			if (!TryReadApplicablePolicyPartySizeDelta(contribution, isClanLeader, out int delta))
			{
				continue;
			}
			hasContribution = true;
			totalDelta += delta;
		}
		if (!hasContribution)
		{
			return originalLimit;
		}
		long adjusted = (long)originalLimit + totalDelta;
		if (adjusted <= 1L)
		{
			return 1;
		}
		return adjusted >= int.MaxValue ? int.MaxValue : (int)adjusted;
	}

	private static void AddPolicyPartySizeLimitExplanations(
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions,
		bool isClanLeader,
		ref ExplainedNumber result)
	{
		bool hasContribution = false;
		for (int index = 0; index < (contributions?.Count ?? 0); index++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[index];
			if (!TryReadApplicablePolicyPartySizeDelta(contribution, isClanLeader, out int delta))
			{
				continue;
			}
			hasContribution = true;
			result.Add(delta, BuildPolicyPartySizeLimitExplanation(contribution));
		}
		if (!hasContribution)
		{
			return;
		}
		if (result.ResultNumber < 1f)
		{
			result.Add(1f - result.ResultNumber, new TextObject("自定义政策部队上限最低值"));
		}
		else if (result.ResultNumber > int.MaxValue)
		{
			result.Add(int.MaxValue - result.ResultNumber, new TextObject("自定义政策部队上限安全值"));
		}
	}

	private static bool TryReadApplicablePolicyPartySizeDelta(
		PolicyEffectRuntimeContribution contribution,
		bool isClanLeader,
		out int delta)
	{
		delta = 0;
		if (contribution == null
			|| contribution.Hook != PolicyEffectHook.PartyMemberSizeLimit
			|| contribution.TargetKind != PolicyEffectTargetKind.Clan
			|| contribution.Aggregation != PolicyEffectAggregationKind.Additive
			|| float.IsNaN(contribution.Value)
			|| float.IsInfinity(contribution.Value)
			|| contribution.Value < PolicyPartySizeLimitMinimumValue
			|| contribution.Value > PolicyPartySizeLimitMaximumValue)
		{
			return false;
		}
		double rounded = Math.Round(contribution.Value, MidpointRounding.AwayFromZero);
		if (Math.Abs(contribution.Value - rounded) > 0.0001d)
		{
			return false;
		}
		if (string.Equals(contribution.ModuleId, PolicyPartySizeLimitClanLeaderModuleId, StringComparison.Ordinal))
		{
			if (!isClanLeader)
			{
				return false;
			}
		}
		else if (!string.Equals(contribution.ModuleId, PolicyPartySizeLimitClanLordsModuleId, StringComparison.Ordinal))
		{
			return false;
		}
		delta = (int)rounded;
		return delta != 0;
	}

	private static TextObject BuildPolicyPartySizeLimitExplanation(PolicyEffectRuntimeContribution contribution)
	{
		string policyName = (contribution?.DisplayName ?? string.Empty)
			.Replace("{", string.Empty)
			.Replace("}", string.Empty)
			.Trim();
		if (policyName.Length > 48)
		{
			policyName = policyName.Substring(0, 47).TrimEnd() + "…";
		}
		return new TextObject("【" + (policyName.Length == 0 ? "未命名政策" : policyName) + "】");
	}

	private static bool TryGetEligiblePolicyLordParty(
		PartyBase partyBase,
		out MobileParty party,
		out Clan clan,
		out Hero leader)
	{
		party = partyBase?.IsMobile == true ? partyBase.MobileParty : null;
		clan = party?.ActualClan;
		leader = party?.LeaderHero;
		if (clan == null
			|| clan.IsEliminated
			|| !IsEligiblePolicyPartySizeHero(leader)
			|| party == null
			|| !party.IsActive
			|| party.IsDisbanding
			|| party.Party == null
			|| !ReferenceEquals(party.Party, partyBase)
			|| !party.IsLordParty
			|| party.IsMilitia
			|| party.IsGarrison
			|| party.LordPartyComponent == null
			|| !ReferenceEquals(party.LeaderHero, leader)
			|| !ReferenceEquals(party.ActualClan, clan)
			|| !ReferenceEquals(leader.Clan, clan))
		{
			return false;
		}
		try
		{
			return ReferenceEquals(leader.PartyBelongedTo, party)
				&& !CourierDeliveryBehavior.IsCourierParty(party);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsEligiblePolicyPartySizeHero(Hero hero)
	{
		return hero != null
			&& hero.IsActive
			&& !hero.IsDead
			&& !hero.IsDisabled
			&& !hero.IsPrisoner;
	}
}
