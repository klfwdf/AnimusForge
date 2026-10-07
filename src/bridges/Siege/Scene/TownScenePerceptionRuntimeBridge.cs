using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Read-only main-thread capture for GCCZ town speech. No Tick scan, combat calls or save keys.
/// Rendering after capture uses detached strings only; each live Agent receives a distinct identity.
/// </summary>
internal static class TownScenePerceptionRuntimeBridge
{
    private sealed class Identity { internal string Key; }
    private static readonly object Sync = new object();
    private static readonly TownScenePerceptionSession Session = new TownScenePerceptionSession();
    private static ConditionalWeakTable<Agent, Identity> Identities = new ConditionalWeakTable<Agent, Identity>();
    private static readonly Dictionary<int, string> SpeakerKeys = new Dictionary<int, string>();
    private static Mission _mission;
    private static string _settlementId = "";
    private static int _nextIdentity;

    internal static void Begin(Mission mission, Settlement settlement)
    {
        lock (Sync)
        {
            EndScene();
            if (mission == null || !ReferenceEquals(mission, Mission.Current) || settlement?.IsTown != true
                || CampaignMission.Current?.Location?.StringId != "center") return;
            _mission = mission;
            _settlementId = settlement.StringId ?? "";
            Session.Begin(_settlementId);
        }
    }
    internal static void EndScene()
    {
        lock (Sync)
        {
            Session.EndScene();
            SpeakerKeys.Clear();
            Identities = new ConditionalWeakTable<Agent, Identity>();
            _mission = null;
            _settlementId = "";
            _nextIdentity = 0;
        }
    }
    private static string Key(Agent agent)
    {
        return Identities.GetValue(agent, _ => new Identity { Key = "scene_npc:" + (++_nextIdentity) }).Key;
    }
    private static bool IsCurrent()
    {
        return _mission != null && ReferenceEquals(_mission, Mission.Current) && !_mission.IsMissionEnding;
    }
    private static bool IsLivingHuman(Agent agent)
    {
        return agent != null && agent.IsHuman && agent.IsActive()
            && agent.State != AgentState.Killed && agent.State != AgentState.Unconscious;
    }
    // Called from scene_persona_capture or immediate-reaction preparation, both on the game thread.
    internal static void CaptureForSpeaker(int agentIndex)
    {
        lock (Sync)
        {
            try
            {
                if (!IsCurrent()) { SpeakerKeys.Remove(agentIndex); return; }
                Agent speaker = null;
                foreach (Agent agent in _mission.Agents)
                    if (agent != null && agent.Index == agentIndex) { speaker = agent; break; }
                if (!IsLivingHuman(speaker) || speaker.IsMainAgent) { SpeakerKeys.Remove(agentIndex); return; }
                string speakerKey = Key(speaker);
                SpeakerKeys[agentIndex] = speakerKey;
                var visible = new List<TownPerceivedPerson>();
                float radiusSquared = TownScenePerceptionSession.ObservationRadius * TownScenePerceptionSession.ObservationRadius;
                foreach (Agent agent in _mission.Agents)
                {
                    if (!IsLivingHuman(agent) || ReferenceEquals(agent, speaker)
                        || speaker.Position.DistanceSquared(agent.Position) > radiusSquared
                        || !ShoutUtils.HasShoutLineOfSightBetweenAgents(speaker, agent, failClosed: true)) continue;
                    visible.Add(new TownPerceivedPerson(Key(agent), agent.IsMainAgent ? "玩家" : agent.Name));
                }
                Session.Observe(_settlementId, speakerKey, visible);
            }
            catch (Exception ex)
            {
                SpeakerKeys.Remove(agentIndex); // Do not publish a stale present-person list after a failed capture.
                Logger.Log("GcczPerception", "Speaker capture failed: " + ex.Message);
            }
        }
    }
    internal static void RecordRemoval(Agent removed, Agent attacker, AgentState state)
    {
        lock (Sync)
        {
            try
            {
                if (!IsCurrent() || removed == null || !removed.IsHuman || !ReferenceEquals(removed.Mission, _mission)) return;
                string removedKey = Key(removed);
                var person = new TownPerceivedPerson(removedKey, removed.IsMainAgent ? "玩家" : removed.Name);
                TownObservedRemoval removal = state == AgentState.Killed ? TownObservedRemoval.Killed
                    : state == AgentState.Unconscious ? TownObservedRemoval.Unconscious : TownObservedRemoval.Left;
                float radiusSquared = TownScenePerceptionSession.ObservationRadius * TownScenePerceptionSession.ObservationRadius;
                foreach (Agent witness in _mission.Agents)
                {
                    if (!IsLivingHuman(witness) || witness.IsMainAgent || ReferenceEquals(witness, removed)
                        || witness.Position.DistanceSquared(removed.Position) > radiusSquared
                        // The removed victim may already be inactive; test sight from its fixed position.
                        || !ShoutUtils.HasShoutLineOfSightFromFixedAnchor(removed.Position, witness)) continue;
                    string key = Key(witness);
                    SpeakerKeys[witness.Index] = key;
                    string cause = IsLivingHuman(attacker)
                        && ShoutUtils.HasShoutLineOfSightBetweenAgents(witness, attacker, failClosed: true)
                            ? (attacker.IsMainAgent ? "玩家" : attacker.Name) : "";
                    Session.RecordRemoval(_settlementId, person, removal, key, cause);
                }
                Session.ForgetObserver(removedKey);
                if (SpeakerKeys.TryGetValue(removed.Index, out string currentKey) && currentKey == removedKey)
                    SpeakerKeys.Remove(removed.Index);
            }
            catch (Exception ex) { Logger.Log("GcczPerception", "Removal capture failed: " + ex.Message); }
        }
    }
    internal static string GetSpeakerIdentity(int agentIndex)
    {
        lock (Sync) return SpeakerKeys.TryGetValue(agentIndex, out string key) ? key : "";
    }
    internal static string BuildContext(int agentIndex, bool ownedIncident, TownPromptTextCatalog text)
    {
        lock (Sync)
            return SpeakerKeys.TryGetValue(agentIndex, out string key)
                ? Session.BuildPrompt(_settlementId, key, ownedIncident, text) : "";
    }
}
