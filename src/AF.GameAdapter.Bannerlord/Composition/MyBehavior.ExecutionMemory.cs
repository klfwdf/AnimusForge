using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class MyBehavior
{
    private readonly ExecutionWitnessObservationController ExecutionWitnesses;
    private readonly ExecutionTranscriptStore _executionTranscripts = new ExecutionTranscriptStore();




    private void SyncExecutionTranscripts(IDataStore dataStore) => CampaignExecutionTranscriptPersistenceAdapter.Sync(dataStore, _executionTranscripts, ResetExecutionMemoryRuntime);

private void ResetExecutionMemoryRuntime()
    {
        ExecutionWitnesses.ResetExecutionMemoryRuntime();
    }

internal void RecordExecutionSpeech(ExecutionRequest request, int sequence, SpeechCue cue, Agent speaker, IReadOnlyList<Agent> participants)
    {
        ExecutionWitnesses.RecordExecutionSpeech(request,sequence,cue,speaker,participants);
    }



internal void CompleteExecutionTranscript(ExecutionRequest request, bool deathCommitted, ExecutionActor actor = ExecutionActor.Undecided)
    {
        ExecutionWitnesses.CompleteExecutionTranscript(request,deathCommitted,actor);
    }


}
