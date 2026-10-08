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
internal static class ConversationActionContextBannerlordAdapter
{
internal static bool IsEscortedPrisonerAgentIdentityMatch(Agent liveAgent, int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter)
	{
		// Custom prisoner agents can use a mission representation whose Character
		// identity is not the same object exposed by the native conversation adapter.
		// The escort registry is the authoritative, bounded identity bridge for them.
		if (liveAgent == null
			|| !NoblePrisonerEscortBehavior.TryGetEscortedHero(targetAgentIndex, out Hero escortedHero, out Agent escortedAgent)
			|| !ReferenceEquals(escortedAgent, liveAgent))
		{
			return false;
		}
		Hero expectedTargetHero = expectedHero ?? expectedCharacter?.HeroObject;
		string expectedHeroId = (expectedTargetHero?.StringId ?? "").Trim();
		string escortedHeroId = (escortedHero?.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(expectedHeroId)
			&& string.Equals(escortedHeroId, expectedHeroId, StringComparison.OrdinalIgnoreCase);
	}
internal static bool IsSceneResponseTargetAvailableForActionDispatch(int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter, out string unavailableReason)
	{
		unavailableReason = "";
		if (targetAgentIndex < 0)
		{
			// Map/tableau conversations and non-scene channels have no live Agent to validate here.
			return true;
		}
		var agents = Mission.Current?.Agents;
		if (agents == null)
		{
			unavailableReason = "mission_or_agents_missing";
			return false;
		}
		Agent liveAgent;
		try
		{
			liveAgent = agents.FirstOrDefault((Agent agent) => agent != null && agent.Index == targetAgentIndex);
		}
		catch
		{
			unavailableReason = "agent_lookup_failed";
			return false;
		}
		if (!CanAgentParticipateInSceneSpeech(liveAgent))
		{
			unavailableReason = "agent_unavailable";
			return false;
		}
		try
		{
			if (IsEscortedPrisonerAgentIdentityMatch(liveAgent, targetAgentIndex, expectedHero, expectedCharacter))
			{
				return true;
			}
			CharacterObject liveCharacter = liveAgent.Character as CharacterObject;
			if (expectedHero != null)
			{
				Hero liveHero = liveCharacter?.HeroObject;
				if (liveHero == null || !string.Equals(liveHero.StringId, expectedHero.StringId, StringComparison.OrdinalIgnoreCase))
				{
					unavailableReason = "agent_identity_changed";
					return false;
				}
			}
			else if (expectedCharacter != null && (liveCharacter == null || !string.Equals(liveCharacter.StringId, expectedCharacter.StringId, StringComparison.OrdinalIgnoreCase)))
			{
				unavailableReason = "agent_identity_changed";
				return false;
			}
		}
		catch
		{
			unavailableReason = "agent_identity_check_failed";
			return false;
		}
		return true;
	}
internal static bool IsNativeConversationResponseTargetAvailableForActionDispatch(int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter, out string unavailableReason)
	{
		// A negative index means this conversation adapter did not capture a concrete
		// scene Agent; it does not prove that a target that was once present despawned.
		// The shared guard still fails closed for every captured Agent index, covering
		// the async knockout/removal race without suppressing valid prisoner adapters.
		return IsSceneResponseTargetAvailableForActionDispatch(targetAgentIndex, expectedHero, expectedCharacter, out unavailableReason);
	}
internal static IEconomyRewardDebtMainThreadPort CreateEconomyReplayPortForExternal(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		string displayName,
		string expectedInteractionSubjectId)
	{
		Hero resolvedHero = targetHero ?? targetCharacter?.HeroObject;
		string expectedSubjectId = !string.IsNullOrWhiteSpace(expectedInteractionSubjectId)
			? expectedInteractionSubjectId.Trim()
			: resolvedHero?.StringId
			?? targetCharacter?.StringId
			?? (targetAgentIndex >= 0 ? "agent:" + targetAgentIndex : string.Empty);
		if (resolvedHero != null)
		{
			return RewardSystemBehavior.CreateEconomyRewardDebtMainThreadPortForExternal();
		}
		if (TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out PartyBase party))
		{
			return RewardSystemBehavior.CreatePartyEconomyRewardDebtMainThreadPortForExternal(
				party,
				targetCharacter,
				expectedSubjectId,
				displayName);
		}
		Settlement settlement = Settlement.CurrentSettlement;
		if (targetCharacter != null && settlement != null)
		{
			return RewardSystemBehavior.CreateMerchantEconomyRewardDebtMainThreadPortForExternal(
				targetCharacter,
				settlement,
				expectedSubjectId,
				displayName);
		}
		return null;
	}
internal static string ResolveDetachedInteractionSubjectId(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		NpcDataPacket npc)
	{
		if (!string.IsNullOrWhiteSpace(targetHero?.StringId))
		{
			return targetHero.StringId;
		}
		if (TryResolveWildernessNonHeroMemoryForExternal(
			npc,
			null,
			targetCharacter,
			targetAgentIndex,
			out string nonHeroMemoryId,
			out _)
			&& !string.IsNullOrWhiteSpace(nonHeroMemoryId))
		{
			return nonHeroMemoryId.Trim();
		}
		return targetCharacter?.StringId
			?? (targetAgentIndex >= 0 ? "agent:" + targetAgentIndex : string.Empty);
	}
}
