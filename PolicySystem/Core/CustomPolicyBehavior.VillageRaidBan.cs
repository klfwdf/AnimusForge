using System;
using System.Collections.Generic;
using System.Reflection;
using AnimusForge.PolicyEffects;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private const string PolicyVillageRaidBanHarmonyId = "com.AnimusForge.custompolicy.villageraidban";

	private static bool _policyVillageRaidBanPatchesApplied;

	private static void ApplyPolicyVillageRaidBanPatchesOnce()
	{
		if (_policyVillageRaidBanPatchesApplied)
		{
			return;
		}

		try
		{
#if BANNERLORD_1_4_OR_GREATER
			Type[] setMoveParameterTypes =
			{
				typeof(Settlement),
				typeof(MobileParty.NavigationType),
				typeof(bool)
			};
#else
			Type[] setMoveParameterTypes =
			{
				typeof(Settlement),
				typeof(MobileParty.NavigationType)
			};
#endif
			MethodInfo setMoveRaidSettlement = AccessTools.Method(
				typeof(MobileParty),
				nameof(MobileParty.SetMoveRaidSettlement),
				setMoveParameterTypes);
			MethodInfo applyStartRaid = AccessTools.Method(
				typeof(StartBattleAction),
				nameof(StartBattleAction.ApplyStartRaid),
				new[] { typeof(MobileParty), typeof(Settlement) });
			MethodInfo updateRaid = AccessTools.DeclaredMethod(typeof(RaidEventComponent), "Update");
			ValidatePolicyVillageRaidPatchTarget(
				setMoveRaidSettlement,
				setMoveParameterTypes,
				typeof(MobileParty).FullName + ".SetMoveRaidSettlement");
			ValidatePolicyVillageRaidPatchTarget(
				applyStartRaid,
				new[] { typeof(MobileParty), typeof(Settlement) },
				typeof(StartBattleAction).FullName + ".ApplyStartRaid");
			ValidatePolicyVillageRaidPatchTarget(
				updateRaid,
				new[] { typeof(bool).MakeByRefType() },
				typeof(RaidEventComponent).FullName + ".Update");

			Harmony harmony = new Harmony(PolicyVillageRaidBanHarmonyId);
			harmony.Patch(
				setMoveRaidSettlement,
				prefix: new HarmonyMethod(
					typeof(CustomPolicyBehavior),
					nameof(Patch_PolicyVillageRaidSetMove_Prefix)));
			harmony.Patch(
				applyStartRaid,
				prefix: new HarmonyMethod(
					typeof(CustomPolicyBehavior),
					nameof(Patch_PolicyVillageRaidStart_Prefix)));
			harmony.Patch(
				updateRaid,
				prefix: new HarmonyMethod(
					typeof(CustomPolicyBehavior),
					nameof(Patch_PolicyVillageRaidUpdate_Prefix)));
			_policyVillageRaidBanPatchesApplied = true;
			PolicySystemLog.Write(
				"Effect",
				"village-raid-ban-patches-applied",
				"AF policy effects now block issuer-kingdom lord parties at raid order, start, and update boundaries");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Effect", "village-raid-ban-patches-failed", ex.ToString());
		}
	}

	private static void ValidatePolicyVillageRaidPatchTarget(
		MethodInfo target,
		IReadOnlyList<Type> expectedParameterTypes,
		string displayName)
	{
		ParameterInfo[] parameters = target?.GetParameters();
		if (target == null || parameters == null || parameters.Length != expectedParameterTypes.Count)
		{
			throw new MissingMethodException(displayName);
		}
		for (int index = 0; index < parameters.Length; index++)
		{
			if (parameters[index].ParameterType != expectedParameterTypes[index])
			{
				throw new MissingMethodException(displayName);
			}
		}
	}

	private static bool Patch_PolicyVillageRaidSetMove_Prefix(
		MobileParty __instance,
		Settlement settlement)
	{
		if (!ShouldBlockPolicyVillageRaid(__instance, settlement))
		{
			return true;
		}
		CancelPolicyVillageRaidOrder(__instance);
		return false;
	}

	private static bool Patch_PolicyVillageRaidStart_Prefix(
		MobileParty attackerParty,
		Settlement settlement)
	{
		if (!ShouldBlockPolicyVillageRaid(attackerParty, settlement))
		{
			return true;
		}
		CancelPolicyVillageRaidOrder(attackerParty);
		return false;
	}

	private static bool Patch_PolicyVillageRaidUpdate_Prefix(
		RaidEventComponent __instance,
		ref bool finish)
	{
		MobileParty attackerParty = __instance?.AttackerSide?.LeaderParty?.MobileParty;
		if (!ShouldBlockPolicyVillageRaid(attackerParty, __instance?.MapEventSettlement))
		{
			return true;
		}
		finish = true;
		return false;
	}

	private static bool ShouldBlockPolicyVillageRaid(
		MobileParty attackerParty,
		Settlement targetSettlement)
	{
		if (attackerParty == null
			|| !attackerParty.IsLordParty
			|| targetSettlement?.IsVillage != true
			|| targetSettlement.Village == null)
		{
			return false;
		}

		Kingdom attackerKingdom = attackerParty.MapFaction as Kingdom;
		string kingdomId = attackerKingdom?.StringId;
		CustomPolicyBehavior behavior = Instance;
		if (behavior == null || string.IsNullOrEmpty(kingdomId))
		{
			return false;
		}

		return HasPolicyVillageRaidBlock(
			behavior._policyEffectRuntimeIndex.GetContributions(
				PolicyEffectHook.KingdomVillageRaidBlock,
				PolicyEffectTargetKind.Kingdom,
				kingdomId));
	}

	internal static bool HasPolicyVillageRaidBlock(
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions)
	{
		if (contributions == null || contributions.Count == 0)
		{
			return false;
		}
		for (int index = 0; index < contributions.Count; index++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[index];
			if (contribution != null
				&& contribution.Aggregation == PolicyEffectAggregationKind.AnyBlock
				&& contribution.Value > 0f
				&& !float.IsNaN(contribution.Value)
				&& !float.IsInfinity(contribution.Value))
			{
				return true;
			}
		}
		return false;
	}

	private static void CancelPolicyVillageRaidOrder(MobileParty attackerParty)
	{
		if (attackerParty != null && attackerParty.MapEvent == null)
		{
			attackerParty.SetMoveModeHold();
		}
	}
}
