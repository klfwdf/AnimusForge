using System;

namespace AnimusForge;

/// <summary>
/// Detached target identity for one prompt build / preprocess request: the six values the
/// retrieval owner keys eligibility, cache and mention scope on. Built by the host from game
/// objects on the calling thread; contains only strings and an index.
/// </summary>
internal readonly struct PromptRuntimeTargetBinding
{
	internal readonly string KingdomId;
	internal readonly string HeroId;
	internal readonly string CharacterId;
	internal readonly string TroopId;
	internal readonly string UnnamedRank;
	internal readonly int AgentIndex;

	internal PromptRuntimeTargetBinding(string kingdomId, string heroId, string characterId, string troopId, string unnamedRank, int agentIndex)
	{
		KingdomId = kingdomId ?? "";
		HeroId = heroId ?? "";
		CharacterId = characterId ?? "";
		TroopId = troopId ?? "";
		UnnamedRank = unnamedRank ?? "";
		AgentIndex = agentIndex;
	}

	internal static readonly PromptRuntimeTargetBinding Cleared = new PromptRuntimeTargetBinding("", "", "", "", "", -1);

	/// <summary>
	/// Legacy derivation: hero id falls back to the character's hero; troop id equals character id;
	/// unnamed rank is soldier/commoner only for a non-hero character.
	/// </summary>
	internal static PromptRuntimeTargetBinding Create(string kingdomId, string heroStringId, string characterStringId, string characterHeroStringId, bool hasHero, bool characterIsSoldier, int agentIndex)
	{
		string heroId = heroStringId ?? characterHeroStringId ?? "";
		string characterId = characterStringId ?? "";
		string unnamedRank = (!hasHero && characterStringId != null) ? (characterIsSoldier ? "soldier" : "commoner") : "";
		return new PromptRuntimeTargetBinding(kingdomId, heroId, characterId, characterId, unnamedRank, agentIndex);
	}

	/// <summary>Publish into the ambient retrieval context in the legacy setter order.</summary>
	internal void Apply(Action<string> setKingdom, Action<string> setHero, Action<string> setCharacter, Action<string> setTroop, Action<string> setUnnamedRank, Action<int> setAgentIndex)
	{
		setKingdom(KingdomId);
		setHero(HeroId);
		setCharacter(CharacterId);
		setTroop(TroopId);
		setUnnamedRank(UnnamedRank);
		setAgentIndex(AgentIndex);
	}
}
