using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Relay presentation: who the current scene speaker addresses. The relay loop registers the
// addressee before queueing a line; speech publication reads it once on the main thread.
public partial class ShoutBehavior
{
    // agentIndex of speaker -> agentIndex of the NPC being answered. Main thread only.
    private readonly Dictionary<int, int> _relaySpeechAddressees = new Dictionary<int, int>();

    internal void RegisterRelaySpeechAddressee(int speakerAgentIndex, int addresseeAgentIndex)
    {
        if (speakerAgentIndex < 0) return;
        if (addresseeAgentIndex < 0 || addresseeAgentIndex == speakerAgentIndex) _relaySpeechAddressees.Remove(speakerAgentIndex);
        else _relaySpeechAddressees[speakerAgentIndex] = addresseeAgentIndex;
    }

    internal void ClearRelaySpeechPresentation()
    {
        _relaySpeechAddressees.Clear();
        _j17SceneAttentionController?.ClearRelaySpeechGaze();
    }

    private bool ApplyRelaySpeechGazeForSpeaker(Agent speaker, List<NpcDataPacket> participants)
    {
        if (speaker == null || DuelSettings.LoadedForSceneRuntime()?.ShoutRelayMutualGaze == false)
        {
            return false;
        }
        Agent addressee = null;
        if (_relaySpeechAddressees.TryGetValue(speaker.Index, out int addresseeIndex))
        {
            addressee = FindMissionAgentByIndex(addresseeIndex);
        }
        if (addressee == null && (participants == null || participants.Count <= 1))
        {
            // A player-directed single reply keeps the existing look-at-player behavior.
            return false;
        }
        List<int> listeners = null;
        if (participants != null)
        {
            listeners = new List<int>(participants.Count);
            for (int i = 0; i < participants.Count; i++)
            {
                NpcDataPacket npc = participants[i];
                if (npc != null && npc.AgentIndex >= 0) listeners.Add(npc.AgentIndex);
            }
        }
        _j17SceneAttentionController.ApplyRelaySpeechGaze(speaker, addressee, listeners);
        return addressee != null;
    }

    private static Agent FindMissionAgentByIndex(int agentIndex)
    {
        var agents = Mission.Current?.Agents;
        if (agentIndex < 0 || agents == null) return null;
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent != null && agent.Index == agentIndex) return agent.IsActive() ? agent : null;
        }
        return null;
    }
}
