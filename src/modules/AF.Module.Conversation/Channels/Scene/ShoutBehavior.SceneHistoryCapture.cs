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

namespace AnimusForge;
public partial class ShoutBehavior {
private void RecordExtraFactToSceneHistoryCaptured(string extraFact, List<NpcDataPacket> nearbyData)
    {
        SceneHistoryPromptCapture.RecordExtraFactToSceneHistoryCaptured(extraFact, nearbyData);
    }
private void AppendNativeConversationSessionLineToSceneHistoryCaptured(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, int targetAgentIndexOverride = -1, NpcDataPacket targetNpc = null, int playerTargetAgentIndexOverride = -1, string playerTargetNameOverride = null)
	{
        SceneHistoryPromptCapture.AppendNativeConversationSessionLineToSceneHistoryCaptured(targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, targetAgentIndexOverride, targetNpc, playerTargetAgentIndexOverride, playerTargetNameOverride);
    }
private void RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(long eventSequence)
	{
        SceneHistoryPromptCapture.RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(eventSequence);
    }
private void AppendActionAfefFactToSceneHistoryInOrderCaptured(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory = true)
    {
        SceneHistoryPromptCapture.AppendActionAfefFactToSceneHistoryInOrderCaptured(targetAgentIndex, fact, mirrorToNativeSharedHistory);
    }
private bool RecordPlayerMessageCaptured(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "", Dictionary<int, Agent> audienceAgentsByIndex = null, bool requireMemoryReceipt = false)
    {
        return SceneHistoryPromptCapture.RecordPlayerMessageCaptured(text, nearbyData, primaryTargetAgentIndex, primaryTargetName, audienceAgentsByIndex, requireMemoryReceipt);
    }
private void PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(
		string personalizedExtraFact,
		int personalizedAgentIndex)
	{
        SceneHistoryPromptCapture.PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(personalizedExtraFact, personalizedAgentIndex);
    }
private bool RecordResponseForAllNearbySafeCaptured(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false)
    {
        return SceneHistoryPromptCapture.RecordResponseForAllNearbySafeCaptured(nearbyData, speakerAgentIndex, speakerName, response, requireMemoryReceipt);
    }
private void RecordSystemFactForNearbySafeCaptured(List<NpcDataPacket> nearbyData, string factText)
    {
        SceneHistoryPromptCapture.RecordSystemFactForNearbySafeCaptured(nearbyData, factText);
    }
}
