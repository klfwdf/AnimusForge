using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class SceneRelayTargetAdapter
{
    private readonly SceneRelayTargetAdapterPorts _ports;
    internal SceneRelayTargetAdapter(SceneRelayTargetAdapterPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal static bool CanNpcParticipateInAutoGroupRelay(NpcDataPacket participant, Dictionary<int, Hero> resolvedHeroes)
	{
		if (participant == null || participant.AgentIndex < 0)
		{
			return false;
		}
		bool canSpeak = true;
		string statusLine = "";
		bool hasStatus;
		if (participant.IsHero)
		{
			Hero hero = null;
			if (resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(participant.AgentIndex, out hero);
			}
			hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
		}
		else
		{
			hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(participant.UnnamedKey, participant.Name, GetSceneNpcPatienceNameForPrompt(participant), out statusLine, out canSpeak);
		}
		return !hasStatus || canSpeak;
	}

	internal static bool IsSceneRelayAudienceEntrySpatiallyEligible(SceneShoutConversationScope scope, SceneShoutAudienceEntry entry, Agent liveAgent)
	{
		if (scope == null || liveAgent == null)
		{
			return false;
		}
		if (entry.IsFramed)
		{
			return true;
		}
		try
		{
			Vec3 position = liveAgent.Position;
			if (entry.IsFromPrimaryAnchor)
			{
				float primaryDistanceSquared = position.DistanceSquared(scope.PrimaryAnchorPosition);
				if (!float.IsNaN(primaryDistanceSquared) && !float.IsInfinity(primaryDistanceSquared) && primaryDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightFromFixedAnchor(scope.PrimaryAnchorPosition, liveAgent))
				{
					return true;
				}
			}
			if (entry.IsFromPlayerAnchor)
			{
				float playerDistanceSquared = position.DistanceSquared(scope.PlayerAnchorPosition);
				if (!float.IsNaN(playerDistanceSquared) && !float.IsInfinity(playerDistanceSquared) && playerDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightFromFixedAnchor(scope.PlayerAnchorPosition, liveAgent))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static SceneRelayEligibilitySnapshot BuildSceneRelayEligibilitySnapshot(SceneShoutConversationScope scope, Dictionary<int, NpcDataPacket> audienceByAgentIndex, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch)
	{
		SceneRelayEligibilitySnapshot snapshot = new SceneRelayEligibilitySnapshot();
		if (scope == null || audienceByAgentIndex == null || !scope.IsCurrent(Mission.Current, conversationEpoch))
		{
			return snapshot;
		}
		HashSet<string> patienceLines = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < scope.Entries.Count; i++)
		{
			SceneShoutAudienceEntry entry = scope.Entries[i];
			if (!audienceByAgentIndex.TryGetValue(entry.AgentIndex, out var npc) || npc == null)
			{
				continue;
			}
			Agent liveAgent = entry.AgentReference;
			SceneShoutLiveValidationResult validation = scope.ValidateLiveAgent(Mission.Current, conversationEpoch, entry.AgentIndex, liveAgent, requireActiveSpeaker: true, out var validatedEntry);
			if (validation != SceneShoutLiveValidationResult.Valid || !IsSceneRelayAudienceEntrySpatiallyEligible(scope, validatedEntry, liveAgent))
			{
				continue;
			}
			bool canSpeak = true;
			string statusLine = "";
			bool hasStatus;
			if (npc.IsHero)
			{
				Hero hero = null;
				resolvedHeroes?.TryGetValue(npc.AgentIndex, out hero);
				hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
			}
			else
			{
				hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, GetSceneNpcPatienceNameForPrompt(npc), out statusLine, out canSpeak);
			}
			if (hasStatus && !string.IsNullOrWhiteSpace(statusLine) && patienceLines.Add(statusLine.Trim()))
			{
				snapshot.PatienceStatusLines.Add(statusLine.Trim());
			}
			if (!hasStatus || canSpeak)
			{
				snapshot.Candidates.Add(npc);
			}
		}
		return snapshot;
	}

	internal static NpcDataPacket ResolveLiveSceneRelayTarget(SceneShoutConversationScope scope, Dictionary<int, NpcDataPacket> audienceByAgentIndex, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, int relayTargetAgentIndex)
	{
		if (relayTargetAgentIndex < 0 || scope == null || audienceByAgentIndex == null || !audienceByAgentIndex.TryGetValue(relayTargetAgentIndex, out var npc) || npc == null)
		{
			return null;
		}
		if (!scope.TryGetEntry(relayTargetAgentIndex, out var entry))
		{
			return null;
		}
		Agent liveAgent = entry.AgentReference;
		if (scope.ValidateLiveAgent(Mission.Current, conversationEpoch, relayTargetAgentIndex, liveAgent, requireActiveSpeaker: true, out var validatedEntry) != SceneShoutLiveValidationResult.Valid || !IsSceneRelayAudienceEntrySpatiallyEligible(scope, validatedEntry, liveAgent))
		{
			return null;
		}
		return CanNpcParticipateInAutoGroupRelay(npc, resolvedHeroes) ? npc : null;
	}

	internal int ResolveAutoGroupRelayTargetAgentIndex(int relayTargetAgentIndex, int currentSpeakerAgentIndex, List<NpcDataPacket> participants, Dictionary<int, Hero> resolvedHeroes)
	{
		if (relayTargetAgentIndex < 0 || participants == null || participants.Count == 0)
		{
			return -1;
		}
		NpcDataPacket npcDataPacket = participants.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == relayTargetAgentIndex);
		if (npcDataPacket == null || !CanNpcParticipateInAutoGroupRelay(npcDataPacket, resolvedHeroes))
		{
			return -1;
		}
		return npcDataPacket.AgentIndex;
	}


}

internal sealed class SceneRelayTargetAdapterPorts
{

}
