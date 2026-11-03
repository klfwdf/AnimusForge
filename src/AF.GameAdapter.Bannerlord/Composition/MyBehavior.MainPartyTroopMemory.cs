using System;
using System.Collections.Generic;

namespace AnimusForge;

public partial class MyBehavior
{
	// One alias scan per canonical id per session; the scan walks the memory
	// dictionaries once, so it never runs on the request hot path again.
	private readonly HashSet<string> _mergedMainPartyTroopMemoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Folds earlier main-party soldier records of the same troop into the canonical
	/// troop key: the faction/lord-suffixed wilderness keys
	/// (troop:&lt;t&gt;:kingdom:*|party:party_string_id:&lt;main&gt;) written before
	/// field camp step 3. Only ids that carry exactly this troop and this main party match.
	/// </summary>
	public static void MergeMainPartyTroopMemoryAliasesForExternal(string canonicalMemoryId, string troopKey, string partyKey)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => owner.MergeMainPartyTroopMemoryAliases(canonicalMemoryId, troopKey, partyKey), nameof(MergeMainPartyTroopMemoryAliasesForExternal)))
		{
			return;
		}
		try
		{
			Instance?.MergeMainPartyTroopMemoryAliases(canonicalMemoryId, troopKey, partyKey);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] merge main-party troop memory failed: " + ex.Message);
		}
	}

	private void MergeMainPartyTroopMemoryAliases(string canonicalMemoryId, string troopKey, string partyKey)
	{
		string target = MemoryRecordRules.NormalizeMemoryHeroId(canonicalMemoryId);
		if (string.IsNullOrWhiteSpace(target) || !_mergedMainPartyTroopMemoryIds.Add(target))
		{
			return;
		}
		string troopPrefix = MemoryRecordRules.NormalizeMemoryHeroId(MemoryBusinessStateOwner.NonHeroMemoryIdPrefix + "troop:" + troopKey + ":");
		string partySuffix = ("|party:party_string_id:" + partyKey).ToLowerInvariant();
		foreach (string alias in CollectNonHeroMemoryAliasesByPrefix(troopPrefix, target))
		{
			if (!alias.EndsWith(partySuffix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace("stage=merge_main_party_troop source=" + alias + " target=" + target);
			_memoryIdentityMerge(alias, target);
		}
	}
}
