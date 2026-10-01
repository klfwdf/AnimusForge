using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;

namespace AnimusForge;

internal static class KingdomDecisionCleanupSafePatch
{
	private static bool _patched;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		try
		{
			var target = AccessTools.Method(typeof(KingdomDecisionProposalBehavior), "UpdateKingdomDecisions", new[] { typeof(Kingdom) });
			if (target == null)
			{
				Logger.Log("KingdomDecisionSafety", "KingdomDecisionProposalBehavior.UpdateKingdomDecisions not found.");
				return;
			}
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(KingdomDecisionCleanupSafePatch), nameof(UpdateKingdomDecisionsPrefix)));
			Logger.Log("KingdomDecisionSafety", "UpdateKingdomDecisions eliminated-kingdom guard applied.");
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomDecisionSafety", "Failed to apply UpdateKingdomDecisions guard: " + ex.Message);
		}
	}

	public static bool UpdateKingdomDecisionsPrefix(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			Logger.Log("KingdomDecisionSafety", "Skipped UpdateKingdomDecisions for null kingdom.");
			return false;
		}
		if (!IsEliminatedSafe(kingdom))
		{
			return true;
		}

		int removed = ClearUnresolvedDecisionsSafe(kingdom);
		Logger.Log("KingdomDecisionSafety", "Skipped UpdateKingdomDecisions for eliminated kingdom=" + (kingdom.StringId ?? "") + " removedDecisions=" + removed);
		return false;
	}

	private static bool IsEliminatedSafe(Kingdom kingdom)
	{
		try
		{
			return kingdom?.IsEliminated == true;
		}
		catch
		{
			return true;
		}
	}

	private static int ClearUnresolvedDecisionsSafe(Kingdom kingdom)
	{
		int removed = 0;
		try
		{
			List<KingdomDecision> decisions = kingdom?.UnresolvedDecisions?.ToList() ?? new List<KingdomDecision>();
			foreach (KingdomDecision decision in decisions)
			{
				try
				{
					kingdom.RemoveDecision(decision);
					removed++;
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomDecisionSafety", "Failed to remove eliminated kingdom decision: " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomDecisionSafety", "Failed to enumerate eliminated kingdom decisions: " + ex.Message);
		}
		return removed;
	}
}
