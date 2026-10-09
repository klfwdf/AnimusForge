using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior : CampaignBehaviorBase
{
	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyWorldMapOrderTagsForExternal(targetHero, null, -1, ref content, out generatedFacts, out notifications, out _);
	}

	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyWorldMapOrderTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out generatedFacts, out notifications, out _);
	}

	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications, out WorldMapOrderApplyResult result)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		result = new WorldMapOrderApplyResult();
		string original = content ?? "";
		try
		{
			List<PartyCommandEntry> commands = new List<PartyCommandEntry>();
			bool hasAnyWorldMapTag = false;
			WorldMapOrderSequenceCoordinator sequence = new WorldMapOrderSequenceCoordinator();
			foreach (Match match in WorldMapOrderTagRegex.Matches(original))
			{
				hasAnyWorldMapTag = true;
				if (!TryParseTag(match.Value, validateTargets: true, out PartyCommandEntry command, out bool isStop))
				{
					continue;
				}
				WorldMapOrderSequenceDecision sequenceDecision = sequence.Observe(isStop);
				if (sequenceDecision == WorldMapOrderSequenceDecision.AcceptLeadingStop)
				{
					continue;
				}
				if (sequenceDecision == WorldMapOrderSequenceDecision.RejectNonLeadingStop)
				{
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_StopOrder", "Invalid world-map command order: STOP is only valid as the first effective world-map tag in this round; the STOP was ignored."));
					Log("ignored non-leading STOP tag");
					continue;
				}
				if (IsKind(command, CommandKind.ClearHideout) && !CanOfferHideoutClearTarget(ResolveSettlementById(command.TargetId)))
				{
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_HideoutUnavailable", "The hideout-clear command was not accepted: the hideout was not found, already empty, occupied by a quest, in battle, or the safe interface was not ready."));
					continue;
				}
				if (command != null)
				{
					commands.Add(command);
				}
			}
			bool leadingStop = sequence.LeadingStop;
			content = StripWorldMapOrderTags(original);
			result.HadTag = hasAnyWorldMapTag;
			if (!hasAnyWorldMapTag)
			{
				return false;
			}
			WorldMapPartyCommandBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
			if (behavior == null)
			{
				notifications.Add(WorldMapUiText.Result("AF_WM_Admission_NotReady", "The world-map command system is not initialized."));
				return false;
			}
			targetHero = targetHero ?? targetCharacter?.HeroObject;
			int firstImplicitTaskIndex = commands.FindIndex(IsTaskCommandForImplicitPartyCreation);
			WorldMapOrderAdmissionRoute admissionRoute = WorldMapOrderCoordinator.SelectAdmissionRoute(
				targetHero != null,
				targetHero != null && IsHeroActuallyInPlayerMainPartyRoster(targetHero),
				firstImplicitTaskIndex >= 0,
				targetHero?.GovernorOf != null);
			if (admissionRoute == WorldMapOrderAdmissionRoute.NonHeroParty)
			{
				if (!TryResolveNonHeroPartyActorForExternal(targetCharacter, targetAgentIndex, out MobileParty nonHeroParty))
				{
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_NoParty", "World-map command failed: the current non-hero speaker has no field party that can be taken over."));
					return false;
				}
				string actorName = GetActorName(null, null, nonHeroParty);
				if (leadingStop)
				{
					behavior.StopQueueForParty(nonHeroParty, "tag_stop", out string stopFact);
					result.StopApplied = true;
					if (!string.IsNullOrWhiteSpace(stopFact))
					{
						generatedFacts.Add(stopFact);
					}
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_QueueCleared", "{ACTOR} cleared the previous world-map command queue.", ("ACTOR", actorName)));
				}
				List<PartyCommandEntry> nonHeroCommands = commands.Where(IsExecutableNonHeroPartyCommand).Select(CloneCommand).ToList();
				if (nonHeroCommands.Count == 0)
				{
					if (leadingStop)
					{
						result.Handled = true;
						return true;
					}
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_NonHeroLimit", "World-map command failed: a non-hero party can only execute party-level move, patrol, follow, and attack commands."));
					return false;
				}
				if (!behavior.TryAppendQueueForParty(nonHeroParty, nonHeroCommands, out string nonHeroFact, out string nonHeroMessage))
				{
					if (!string.IsNullOrWhiteSpace(nonHeroMessage))
					{
						notifications.Add(nonHeroMessage);
					}
					return false;
				}
				if (!string.IsNullOrWhiteSpace(nonHeroFact))
				{
					generatedFacts.Add(nonHeroFact);
				}
				notifications.Add(nonHeroMessage);
				result.Handled = true;
				result.AddedCommandCount = nonHeroCommands.Count;
				return true;
			}
			if (leadingStop)
			{
				behavior.StopQueue(targetHero, "tag_stop", out string stopFact);
				result.StopApplied = true;
				if (!string.IsNullOrWhiteSpace(stopFact))
				{
					generatedFacts.Add(stopFact);
				}
				notifications.Add(WorldMapUiText.Result("AF_WM_Admission_QueueCleared", "{ACTOR} cleared the previous world-map command queue.", ("ACTOR", GetHeroName(targetHero))));
			}
			if (commands.Count == 0)
			{
				if (leadingStop)
				{
					result.Handled = true;
					return true;
				}
				notifications.Add(WorldMapUiText.Result("AF_WM_Admission_NoTarget", "World-map command failed: there is no valid executable target ID."));
				return false;
			}
			if (admissionRoute == WorldMapOrderAdmissionRoute.CompanionPartyCreation)
			{
				int firstTaskIndex = firstImplicitTaskIndex;
				if (firstTaskIndex >= 0)
				{
					List<PartyCommandEntry> createCommands = commands.Skip(firstTaskIndex).Select(CloneCommand).ToList();
					if (firstTaskIndex > 0)
					{
						notifications.Add(WorldMapUiText.Result("AF_WM_Admission_CompanionOrder", "Invalid world-map command order: a merge command before party creation cannot run; processing starts at the first effective task command."));
					}
					bool opened = behavior.TryOpenCreateCompanionParty(targetHero, createCommands, out string createMessage);
					notifications.Add(createMessage);
					generatedFacts.Add("[AFEF NPC行为补充] " + GetHeroName(targetHero) + (opened
						? "接受了新的大地图任务；将先从玩家主队分兵，随后按输出顺序执行命令。"
						: "无法创建同伴部队：" + createMessage));
					result.Handled = opened;
					result.CompanionPartyCreationQueued = opened;
					result.AddedCommandCount = opened ? createCommands.Count : 0;
					return opened;
				}
			}
			if (admissionRoute == WorldMapOrderAdmissionRoute.GovernorExpedition)
			{
				int firstTaskIndex = commands.FindIndex(IsTaskCommandForImplicitPartyCreation);
				if (firstTaskIndex < 0)
				{
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_GovernorNoTask", "World-map command failed: a governor in a settlement needs at least one move, patrol, follow, or attack task to form an expedition."));
					return false;
				}
				List<PartyCommandEntry> expeditionCommands = commands.Skip(firstTaskIndex).Select(CloneCommand).ToList();
				if (firstTaskIndex > 0)
				{
					notifications.Add(WorldMapUiText.Result("AF_WM_Admission_GovernorOrder", "Invalid world-map command order: a merge command before governor party creation cannot run; processing starts at the first effective task command."));
				}
				bool accepted = behavior.TryStartGovernorExpeditionRequest(targetHero, expeditionCommands, out string expeditionMessage, out bool queuedForChannelExit);
				notifications.Add(expeditionMessage);
				generatedFacts.Add("[AFEF NPC行为补充] " + GetHeroName(targetHero) + (accepted
					? "接受了新的大地图任务；将从管辖地驻军抽调兵力组建临时远征队，任务结束后返城交还兵员并尝试复职。"
					: "无法组建总督远征队：" + expeditionMessage));
				result.Handled = accepted;
				result.GovernorExpeditionCreationQueued = accepted && queuedForChannelExit;
				result.AddedCommandCount = accepted ? expeditionCommands.Count : 0;
				return accepted;
			}
			if (!behavior.TryAppendQueue(targetHero, commands, out string fact, out string message, out int acceptedCommandCount))
			{
				if (!string.IsNullOrWhiteSpace(message))
				{
					notifications.Add(message);
				}
				return false;
			}
			if (!string.IsNullOrWhiteSpace(fact))
			{
				generatedFacts.Add(fact);
			}
			notifications.Add(message);
			result.Handled = true;
			result.AddedCommandCount = acceptedCommandCount;
			return true;
		}
		catch (Exception ex)
		{
			content = StripWorldMapOrderTags(original);
			notifications.Add(WorldMapUiText.Result("AF_WM_Admission_ProcessingFailed", "World-map command processing failed: {ERROR}", ("ERROR", ex.Message)));
			Log("apply tags failed: " + ex);
			return false;
		}
	}

	public static WorldMapOrderApplyResult ProcessWorldMapOrderTagsDispatch(Hero targetHero, ref string content)
	{
		return ProcessWorldMapOrderTagsDispatch(targetHero, null, -1, ref content);
	}

	public static WorldMapOrderApplyResult ProcessWorldMapOrderTagsDispatch(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content)
	{
		TryApplyWorldMapOrderTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out List<string> facts, out List<string> notifications, out WorldMapOrderApplyResult result);
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		if (result.Handled)
		{
			foreach (string fact in facts ?? new List<string>())
			{
				if (!string.IsNullOrWhiteSpace(fact))
				{
					if (targetHero != null)
					{
						MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
					}
					else if (TryResolveNonHeroPartyActorForExternal(targetCharacter, targetAgentIndex, out MobileParty party))
					{
						string memoryId = BuildPartyMemoryId(party);
						if (!string.IsNullOrWhiteSpace(memoryId))
						{
							MyBehavior.AppendExternalNonHeroDialogueHistory(memoryId, GetPartyName(party), null, null, fact);
						}
					}
				}
			}
		}
		foreach (string notification in notifications ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(notification))
			{
				bool failure = notification.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0
					|| notification.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
					|| notification.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0
					|| notification.IndexOf("cannot", StringComparison.OrdinalIgnoreCase) >= 0
					|| notification.IndexOf("not initialized", StringComparison.OrdinalIgnoreCase) >= 0;
				InformationManager.DisplayMessage(new InformationMessage(notification, failure ? new Color(1f, 0.45f, 0.25f) : new Color(0.4f, 1f, 0.4f)));
			}
		}
		return result;
	}
}
