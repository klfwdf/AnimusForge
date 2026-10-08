using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal static class CampaignExecutionTranscriptPersistenceAdapter
{
    internal static void Sync(IDataStore dataStore, ExecutionTranscriptStore transcripts, Action resetRuntime)
    {
        Dictionary<string, string> saved = dataStore.IsSaving
            ? CampaignSaveChunkHelper.FlattenStringDictionary(transcripts.Save(), "_af_executionTranscripts_v1", "ExecutionMemory")
            : new Dictionary<string, string>();
        dataStore.SyncData("_af_executionTranscripts_v1", ref saved);
        if (dataStore.IsLoading)
        {
            transcripts.Load(CampaignSaveChunkHelper.RestoreStringDictionary(saved, "ExecutionMemory"));
            resetRuntime();
        }
    }
}
