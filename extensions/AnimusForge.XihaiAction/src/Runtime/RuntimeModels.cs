using System;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed class CapturedSceneActionEvent
    {
        public Guid EventId { get; set; }
        public SceneInputSource InputSource { get; set; }
        public Mission SourceMission { get; set; }
        public string RawText { get; set; }
        public Agent Player { get; set; }
        public Agent Speaker { get; set; }
        public Agent PrimaryTarget { get; set; }
        public Agent[] FramedTargets { get; set; } = Array.Empty<Agent>();
        public double SubmittedAtMissionTime { get; set; }
    }

    internal sealed class SessionAgentHandle
    {
        public long SessionGeneration { get; set; }
        public Mission Mission { get; set; }
        public Agent Agent { get; set; }
        public int AgentIndex { get; set; }

        public string StableId => SessionGeneration + ":" + AgentIndex;
    }

    internal sealed class TrustedOneShotRequest
    {
        public Guid RequestId { get; set; }
        public Guid OwnerToken { get; set; }
        public Mission Mission { get; set; }
        public Agent Target { get; set; }
        public string IntentKey { get; set; }
        public double SubmittedAtMissionTime { get; set; }
        public string DiagnosticSource { get; set; }
    }

    internal sealed class TrustedPlaybackCancellation
    {
        public Guid OwnerToken { get; set; }
        public Mission Mission { get; set; }
        public string Reason { get; set; }
    }

    internal sealed class DedicatedNpcSpeechResultV1
    {
        public int SpeakerAgentIndex { get; set; } = -1;
        public object AfBehavior { get; set; }
        public object AfNpcPacket { get; set; }
        public string Content { get; set; }
        public BattleSpeechCombinedNpcResponseV2 CombinedResponse { get; set; }
        public string Error { get; set; }
        public bool RequiresRegeneration { get; set; }
        public bool Succeeded => SpeakerAgentIndex >= 0 && AfBehavior != null &&
                                  AfNpcPacket != null && !string.IsNullOrWhiteSpace(Content);
    }

    /// <summary>
    /// Main-thread snapshot for the dedicated NPC speech path.  No Agent
    /// enumeration or AF packet extraction is performed by the network waiter.
    /// </summary>
    internal sealed class DedicatedNpcSpeechSnapshotV1
    {
        public Mission Mission { get; set; }
        public Guid SessionId { get; set; }
        public int SpeakerAgentIndex { get; set; } = -1;
        public string SpeakerName { get; set; }
        public object AfBehavior { get; set; }
        public object AfNpcPacket { get; set; }
        public string SceneDescription { get; set; }
        public Task<string> ResponseTask { get; set; }
        public BattleSpeechReplyPromptSnapshotV2 PromptSnapshot { get; set; }
    }
}
