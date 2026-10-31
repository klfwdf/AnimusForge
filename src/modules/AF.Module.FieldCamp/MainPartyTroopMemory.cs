using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Persistent memory identity for regular (non-hero) soldiers of the player's main
/// party, keyed by troop type: af_nonhero:troop:&lt;troop&gt;|party:party_string_id:&lt;main&gt;.
/// It joins the existing non-hero memory pipeline (history, daily draft, compressed
/// blocks, overview) used by all three channels, with the same retention rules.
/// Unlike wilderness parties the key carries no faction/lord suffix, so it survives
/// the player changing kingdoms, and it also applies inside settlements and the camp.
/// Prisoners and other parties' soldiers are never matched.
/// </summary>
internal static class MainPartyTroopMemory
{
	internal static bool TryResolve(Hero targetHero, CharacterObject targetCharacter, int agentIndex, out string memoryId, out string memoryName)
	{
		memoryId = "";
		memoryName = "";
		if (targetHero != null || targetCharacter?.HeroObject != null)
		{
			return false;
		}
		MobileParty main = MobileParty.MainParty;
		if (main == null || string.IsNullOrWhiteSpace(main.StringId))
		{
			return false;
		}
		CharacterObject character = targetCharacter;
		Agent agent = agentIndex >= 0 ? FindAgent(agentIndex) : null;
		if (agent != null)
		{
			if (!IsMainPartySoldierAgent(agent))
			{
				return false;
			}
			character ??= agent.Character as CharacterObject;
		}
		else if (agentIndex >= 0)
		{
			// The agent has despawned (left the scene, died) after the request began:
			// keep the canonical id only if this soldier was recorded as ours.
			if (!WasMainPartyAgent(agentIndex, character))
			{
				return false;
			}
		}
		else if (!IsMainPartyMapConversation(character))
		{
			// No agent: only a map conversation whose party is the main party proves ownership.
			// A shared troop type alone is not enough (looters in both parties, caravans).
			return false;
		}
		return TryBuild(character, main, out memoryId, out memoryName);
	}

	/// <summary>Memory id for a troop type of the main party when no conversation is involved (camp facts).</summary>
	internal static bool TryResolveForTroop(CharacterObject troop, out string memoryId, out string memoryName)
	{
		memoryId = "";
		memoryName = "";
		MobileParty main = MobileParty.MainParty;
		if (troop == null || main == null || string.IsNullOrWhiteSpace(main.StringId) || !IsMainPartyRegular(troop))
		{
			return false;
		}
		return TryBuild(troop, main, out memoryId, out memoryName);
	}

	private static bool TryBuild(CharacterObject character, MobileParty main, out string memoryId, out string memoryName)
	{
		memoryId = "";
		memoryName = "";
		if (character == null || character.IsHero)
		{
			return false;
		}
		string troop = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(character.StringId);
		string party = NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(main.StringId);
		if (string.IsNullOrWhiteSpace(troop) || string.IsNullOrWhiteSpace(party))
		{
			return false;
		}
		memoryId = MyBehavior.BuildNonHeroMemoryIdForExternal("troop:" + troop + "|party:party_string_id:" + party);
		if (string.IsNullOrWhiteSpace(memoryId))
		{
			return false;
		}
		memoryName = (character.Name?.ToString() ?? "").Trim();
		if (memoryName.Length == 0)
		{
			memoryName = "NPC";
		}
		MyBehavior.MergeMainPartyTroopMemoryAliasesForExternal(memoryId, troop, party);
		return true;
	}

	// Agents proven to be ours while active, so a reply that lands after the agent
	// despawns keeps the same memory id. Cleared when a new mission starts.
	private static readonly Dictionary<int, string> RecentMainPartyAgents = new Dictionary<int, string>();
	private static Mission _recentAgentsMission;

	private static void RememberMainPartyAgent(Agent agent)
	{
		if (!ReferenceEquals(_recentAgentsMission, Mission.Current))
		{
			RecentMainPartyAgents.Clear();
			_recentAgentsMission = Mission.Current;
		}
		if (agent.Character is CharacterObject c)
		{
			RecentMainPartyAgents[agent.Index] = c.StringId;
		}
	}

	private static bool WasMainPartyAgent(int agentIndex, CharacterObject character)
	{
		return character != null
			&& ReferenceEquals(_recentAgentsMission, Mission.Current)
			&& RecentMainPartyAgents.TryGetValue(agentIndex, out string troopId)
			&& string.Equals(troopId, character.StringId, StringComparison.Ordinal);
	}

	private static bool IsMainPartyMapConversation(CharacterObject character)
	{
		try
		{
			return character != null
				&& !character.IsHero
				&& Mission.Current == null
				&& MobileParty.ConversationParty == MobileParty.MainParty
				&& IsMainPartyRegular(character);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsMainPartySoldierAgent(Agent agent)
	{
		try
		{
			if (agent == null || agent.Origin is PrisonerAgentOrigin)
			{
				return false;
			}
			if (!(agent.Character is CharacterObject character) || character.IsHero)
			{
				return false;
			}
			PartyBase party = agent.Origin?.BattleCombatant as PartyBase;
			bool ours = party != null && (ReferenceEquals(party, PartyBase.MainParty) || ReferenceEquals(party.MobileParty, MobileParty.MainParty));
			if (ours)
			{
				RememberMainPartyAgent(agent);
			}
			return ours;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMainPartyRegular(CharacterObject character)
	{
		try
		{
			return !character.IsHero && MobileParty.MainParty?.MemberRoster?.Contains(character) == true;
		}
		catch
		{
			return false;
		}
	}

	private static Agent FindAgent(int agentIndex)
	{
		try
		{
			return Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == agentIndex && a.IsActive());
		}
		catch
		{
			return null;
		}
	}
}
