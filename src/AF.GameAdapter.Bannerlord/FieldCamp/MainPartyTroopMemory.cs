using System;
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
		if (agentIndex >= 0)
		{
			Agent agent = FindAgent(agentIndex);
			if (agent == null || !IsMainPartySoldierAgent(agent))
			{
				return false;
			}
			character ??= agent.Character as CharacterObject;
		}
		else if (character == null || !IsMainPartyRegular(character))
		{
			// Native conversation without an agent: only a troop the main party actually carries.
			return false;
		}
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
			return party != null && (ReferenceEquals(party, PartyBase.MainParty) || ReferenceEquals(party.MobileParty, MobileParty.MainParty));
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
