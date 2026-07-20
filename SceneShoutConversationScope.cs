using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

[Flags]
internal enum SceneShoutAudienceOriginFlags : byte
{
	None = 0,
	Framed = 1,
	PrimaryAnchor10Meters = 2,
	PlayerAnchor10Meters = 4
}

internal enum SceneShoutLiveValidationResult : byte
{
	Valid = 0,
	MissionMismatch,
	EpochMismatch,
	NotInAudience,
	AgentMissing,
	AgentIndexMismatch,
	AgentReferenceMismatch,
	AgentMissionMismatch,
	CharacterIdentityMismatch,
	NotHuman,
	Inactive
}

internal readonly struct SceneShoutCharacterIdentitySnapshot
{
	internal BasicCharacterObject CharacterReference { get; }

	internal BasicCharacterObject OriginTroopReference { get; }

	internal string CharacterStringId { get; }

	internal string OriginTroopStringId { get; }

	internal bool IsHero { get; }

	internal SceneShoutCharacterIdentitySnapshot(Agent agent)
	{
		BasicCharacterObject character = agent?.Character;
		BasicCharacterObject originTroop = null;
		try
		{
			originTroop = agent?.Origin?.Troop;
		}
		catch
		{
			originTroop = null;
		}

		CharacterReference = character;
		OriginTroopReference = originTroop;
		CharacterStringId = character?.StringId ?? string.Empty;
		OriginTroopStringId = originTroop?.StringId ?? string.Empty;
		IsHero = character?.IsHero == true;
	}

	internal bool Matches(Agent liveAgent)
	{
		if (liveAgent == null)
		{
			return false;
		}

		BasicCharacterObject liveCharacter;
		BasicCharacterObject liveOriginTroop = null;
		try
		{
			liveCharacter = liveAgent.Character;
			liveOriginTroop = liveAgent.Origin?.Troop;
		}
		catch
		{
			return false;
		}

		return ReferenceEquals(CharacterReference, liveCharacter)
			&& ReferenceEquals(OriginTroopReference, liveOriginTroop)
			&& string.Equals(CharacterStringId, liveCharacter?.StringId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(OriginTroopStringId, liveOriginTroop?.StringId ?? string.Empty, StringComparison.Ordinal)
			&& IsHero == (liveCharacter?.IsHero == true);
	}
}

internal readonly struct SceneShoutAudienceEntry
{
	internal int AgentIndex { get; }

	internal Agent AgentReference { get; }

	internal SceneShoutCharacterIdentitySnapshot CharacterIdentity { get; }

	internal SceneShoutAudienceOriginFlags OriginFlags { get; }

	internal bool IsPrimary { get; }

	internal Vec3 PositionAtCapture { get; }

	internal float PrimaryAnchorDistanceSquaredAtCapture { get; }

	internal float PlayerAnchorDistanceSquaredAtCapture { get; }

	internal bool IsFramed => (OriginFlags & SceneShoutAudienceOriginFlags.Framed) != 0;

	internal bool IsFromPrimaryAnchor => (OriginFlags & SceneShoutAudienceOriginFlags.PrimaryAnchor10Meters) != 0;

	internal bool IsFromPlayerAnchor => (OriginFlags & SceneShoutAudienceOriginFlags.PlayerAnchor10Meters) != 0;

	internal SceneShoutAudienceEntry(
		Agent agent,
		SceneShoutAudienceOriginFlags originFlags,
		bool isPrimary,
		Vec3 primaryAnchorPosition,
		Vec3 playerAnchorPosition)
	{
		AgentIndex = agent.Index;
		AgentReference = agent;
		CharacterIdentity = new SceneShoutCharacterIdentitySnapshot(agent);
		OriginFlags = originFlags;
		IsPrimary = isPrimary;
		PositionAtCapture = agent.Position;
		PrimaryAnchorDistanceSquaredAtCapture = GetFiniteDistanceSquared(PositionAtCapture, primaryAnchorPosition);
		PlayerAnchorDistanceSquaredAtCapture = GetFiniteDistanceSquared(PositionAtCapture, playerAnchorPosition);
	}

	internal bool HasOrigin(SceneShoutAudienceOriginFlags origin)
	{
		return origin != SceneShoutAudienceOriginFlags.None && (OriginFlags & origin) == origin;
	}

	internal bool MatchesLiveIdentity(Agent liveAgent)
	{
		return liveAgent != null
			&& liveAgent.Index == AgentIndex
			&& ReferenceEquals(AgentReference, liveAgent)
			&& CharacterIdentity.Matches(liveAgent);
	}

	private static float GetFiniteDistanceSquared(Vec3 left, Vec3 right)
	{
		float distanceSquared = left.DistanceSquared(right);
		return float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared) || distanceSquared < 0f
			? float.PositiveInfinity
			: distanceSquared;
	}
}

/// <summary>
/// Immutable audience snapshot for one player-driven scene shout.
/// Spatial queries and LOS checks are intentionally owned by the caller; this type only
/// merges their results and validates live agents supplied through an O(1) lookup path.
/// </summary>
internal sealed class SceneShoutConversationScope
{
	internal const float AnchorRadiusMeters = 10f;

	internal const float AnchorRadiusSquared = AnchorRadiusMeters * AnchorRadiusMeters;

	private readonly SceneShoutAudienceEntry[] _entries;

	private readonly ReadOnlyCollection<SceneShoutAudienceEntry> _readOnlyEntries;

	private readonly Dictionary<int, int> _entryIndexByAgentIndex;

	internal Mission Mission { get; }

	internal int ConversationEpoch { get; }

	internal int PrimaryAgentIndex { get; }

	internal int PlayerAgentIndex { get; }

	internal Vec3 PrimaryAnchorPosition { get; }

	internal Vec3 PlayerAnchorPosition { get; }

	internal SceneShoutAudienceEntry Primary { get; }

	internal IReadOnlyList<SceneShoutAudienceEntry> Entries => _readOnlyEntries;

	internal int Count => _entries.Length;

	internal int FramedCount { get; }

	internal int PrimaryAnchorCount { get; }

	internal int PlayerAnchorCount { get; }

	private SceneShoutConversationScope(
		Mission mission,
		int conversationEpoch,
		Agent primaryAgent,
		Agent playerAgent,
		Vec3 primaryAnchorPosition,
		Vec3 playerAnchorPosition,
		SceneShoutAudienceEntry[] entries)
	{
		Mission = mission;
		ConversationEpoch = conversationEpoch;
		PrimaryAgentIndex = primaryAgent.Index;
		PlayerAgentIndex = playerAgent?.Index ?? -1;
		PrimaryAnchorPosition = primaryAnchorPosition;
		PlayerAnchorPosition = playerAnchorPosition;
		_entries = entries;
		_readOnlyEntries = Array.AsReadOnly(entries);
		_entryIndexByAgentIndex = new Dictionary<int, int>(entries.Length);

		SceneShoutAudienceEntry primary = default;
		int framedCount = 0;
		int primaryAnchorCount = 0;
		int playerAnchorCount = 0;
		for (int i = 0; i < entries.Length; i++)
		{
			SceneShoutAudienceEntry entry = entries[i];
			_entryIndexByAgentIndex.Add(entry.AgentIndex, i);
			if (entry.IsPrimary)
			{
				primary = entry;
			}
			if (entry.IsFramed)
			{
				framedCount++;
			}
			if (entry.IsFromPrimaryAnchor)
			{
				primaryAnchorCount++;
			}
			if (entry.IsFromPlayerAnchor)
			{
				playerAnchorCount++;
			}
		}

		Primary = primary;
		FramedCount = framedCount;
		PrimaryAnchorCount = primaryAnchorCount;
		PlayerAnchorCount = playerAnchorCount;
	}

	internal static bool TryCreate(
		Mission mission,
		int conversationEpoch,
		Agent primaryAgent,
		Agent playerAgent,
		Vec3 primaryAnchorPosition,
		Vec3 playerAnchorPosition,
		IReadOnlyList<Agent> framedAgents,
		IReadOnlyList<Agent> primaryAnchorAgents,
		IReadOnlyList<Agent> playerAnchorAgents,
		out SceneShoutConversationScope scope)
	{
		scope = null;
		if (mission == null
			|| primaryAgent == null
			|| playerAgent == null
			|| conversationEpoch < 0
			|| !IsCaptureCandidateValid(mission, playerAgent, primaryAgent))
		{
			return false;
		}

		int estimatedCount = 1 + GetCount(framedAgents) + GetCount(primaryAnchorAgents) + GetCount(playerAnchorAgents);
		Dictionary<int, AudienceEntryBuilder> builders = new Dictionary<int, AudienceEntryBuilder>(estimatedCount);
		List<int> stableOrder = new List<int>(estimatedCount);

		AddOrMerge(
			mission,
			playerAgent,
			primaryAgent,
			SceneShoutAudienceOriginFlags.PrimaryAnchor10Meters,
			isPrimary: true,
			builders,
			stableOrder);
		AddSource(mission, playerAgent, framedAgents, SceneShoutAudienceOriginFlags.Framed, primaryAgent, builders, stableOrder);
		AddSource(mission, playerAgent, primaryAnchorAgents, SceneShoutAudienceOriginFlags.PrimaryAnchor10Meters, primaryAgent, builders, stableOrder);
		AddSource(mission, playerAgent, playerAnchorAgents, SceneShoutAudienceOriginFlags.PlayerAnchor10Meters, primaryAgent, builders, stableOrder);

		if (!builders.TryGetValue(primaryAgent.Index, out AudienceEntryBuilder primaryBuilder)
			|| !ReferenceEquals(primaryBuilder.Agent, primaryAgent))
		{
			return false;
		}

		SceneShoutAudienceEntry[] entries = new SceneShoutAudienceEntry[stableOrder.Count];
		try
		{
			for (int i = 0; i < stableOrder.Count; i++)
			{
				AudienceEntryBuilder builder = builders[stableOrder[i]];
				entries[i] = new SceneShoutAudienceEntry(
					builder.Agent,
					builder.OriginFlags,
					builder.IsPrimary,
					primaryAnchorPosition,
					playerAnchorPosition);
			}
		}
		catch
		{
			return false;
		}

		scope = new SceneShoutConversationScope(
			mission,
			conversationEpoch,
			primaryAgent,
			playerAgent,
			primaryAnchorPosition,
			playerAnchorPosition,
			entries);
		return true;
	}

	internal bool IsCurrent(Mission currentMission, int currentConversationEpoch)
	{
		return ReferenceEquals(Mission, currentMission) && ConversationEpoch == currentConversationEpoch;
	}

	internal bool Contains(int agentIndex)
	{
		return _entryIndexByAgentIndex.ContainsKey(agentIndex);
	}

	internal bool TryGetEntry(int agentIndex, out SceneShoutAudienceEntry entry)
	{
		if (_entryIndexByAgentIndex.TryGetValue(agentIndex, out int index))
		{
			entry = _entries[index];
			return true;
		}

		entry = default;
		return false;
	}

	internal SceneShoutLiveValidationResult ValidateLiveAgent(
		Mission currentMission,
		int currentConversationEpoch,
		int agentIndex,
		Agent liveAgent,
		bool requireActiveSpeaker,
		out SceneShoutAudienceEntry entry)
	{
		entry = default;
		if (!ReferenceEquals(Mission, currentMission))
		{
			return SceneShoutLiveValidationResult.MissionMismatch;
		}
		if (ConversationEpoch != currentConversationEpoch)
		{
			return SceneShoutLiveValidationResult.EpochMismatch;
		}
		if (!TryGetEntry(agentIndex, out entry))
		{
			return SceneShoutLiveValidationResult.NotInAudience;
		}
		if (liveAgent == null)
		{
			return SceneShoutLiveValidationResult.AgentMissing;
		}
		if (liveAgent.Index != agentIndex)
		{
			return SceneShoutLiveValidationResult.AgentIndexMismatch;
		}
		if (!ReferenceEquals(entry.AgentReference, liveAgent))
		{
			return SceneShoutLiveValidationResult.AgentReferenceMismatch;
		}

		try
		{
			if (!ReferenceEquals(liveAgent.Mission, Mission))
			{
				return SceneShoutLiveValidationResult.AgentMissionMismatch;
			}
			if (!entry.CharacterIdentity.Matches(liveAgent))
			{
				return SceneShoutLiveValidationResult.CharacterIdentityMismatch;
			}
			if (!liveAgent.IsHuman)
			{
				return SceneShoutLiveValidationResult.NotHuman;
			}
			if (requireActiveSpeaker
				&& (!liveAgent.IsActive() || liveAgent.State != AgentState.Active || liveAgent.Health <= 0f))
			{
				return SceneShoutLiveValidationResult.Inactive;
			}
		}
		catch
		{
			return SceneShoutLiveValidationResult.Inactive;
		}

		return SceneShoutLiveValidationResult.Valid;
	}

	private static int GetCount(IReadOnlyCollection<Agent> agents)
	{
		return agents?.Count ?? 0;
	}

	private static void AddSource(
		Mission mission,
		Agent playerAgent,
		IReadOnlyList<Agent> agents,
		SceneShoutAudienceOriginFlags origin,
		Agent primaryAgent,
		Dictionary<int, AudienceEntryBuilder> builders,
		List<int> stableOrder)
	{
		if (agents == null)
		{
			return;
		}

		for (int i = 0; i < agents.Count; i++)
		{
			Agent agent = agents[i];
			AddOrMerge(
				mission,
				playerAgent,
				agent,
				origin,
				ReferenceEquals(agent, primaryAgent),
				builders,
				stableOrder);
		}
	}

	private static void AddOrMerge(
		Mission mission,
		Agent playerAgent,
		Agent agent,
		SceneShoutAudienceOriginFlags origin,
		bool isPrimary,
		Dictionary<int, AudienceEntryBuilder> builders,
		List<int> stableOrder)
	{
		if (!IsCaptureCandidateValid(mission, playerAgent, agent))
		{
			return;
		}

		int agentIndex = agent.Index;
		if (builders.TryGetValue(agentIndex, out AudienceEntryBuilder existing))
		{
			if (!ReferenceEquals(existing.Agent, agent))
			{
				return;
			}
			existing.OriginFlags |= origin;
			existing.IsPrimary |= isPrimary;
			builders[agentIndex] = existing;
			return;
		}

		builders.Add(agentIndex, new AudienceEntryBuilder
		{
			Agent = agent,
			OriginFlags = origin,
			IsPrimary = isPrimary
		});
		stableOrder.Add(agentIndex);
	}

	private static bool IsCaptureCandidateValid(Mission mission, Agent playerAgent, Agent agent)
	{
		if (mission == null || agent == null || ReferenceEquals(agent, playerAgent) || agent.Index < 0)
		{
			return false;
		}

		try
		{
			return ReferenceEquals(agent.Mission, mission)
				&& agent.Character != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& agent.State == AgentState.Active
				&& agent.Health > 0f;
		}
		catch
		{
			return false;
		}
	}

	private struct AudienceEntryBuilder
	{
		internal Agent Agent;

		internal SceneShoutAudienceOriginFlags OriginFlags;

		internal bool IsPrimary;
	}
}
