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
    private readonly ExecutionTranscriptStore _executionTranscripts = new ExecutionTranscriptStore();
    private readonly Dictionary<Agent, Tuple<string, string, bool>> _executionWitnesses = new Dictionary<Agent, Tuple<string, string, bool>>();
    private Guid _executionWitnessSession;
    private Mission _executionWitnessMission;

    private void SyncExecutionTranscripts(IDataStore dataStore)
    {
        Dictionary<string, string> saved = dataStore.IsSaving
            ? CampaignSaveChunkHelper.FlattenStringDictionary(_executionTranscripts.Save(), "_af_executionTranscripts_v1", "ExecutionMemory")
            : new Dictionary<string, string>();
        dataStore.SyncData("_af_executionTranscripts_v1", ref saved);
        if (dataStore.IsLoading)
        {
            _executionTranscripts.Load(CampaignSaveChunkHelper.RestoreStringDictionary(saved, "ExecutionMemory"));
            ResetExecutionMemoryRuntime();
        }
    }

    private void ResetExecutionMemoryRuntime()
    {
        _executionWitnesses.Clear();
        _executionWitnessMission = null;
        _executionWitnessSession = Guid.Empty;
    }

    internal void RecordExecutionSpeech(ExecutionRequest request, int sequence, SpeechCue cue, Agent speaker, IReadOnlyList<Agent> participants)
    {
        var mission = Mission.Current;
        var controller = mission?.GetMissionBehavior<TownExecutionMissionBehavior>();
        if (request == null || controller?.Request?.SessionId != request.SessionId || speaker == null || !speaker.IsActive()) return;
        var facts = VengeanceExecutionFacts.From(request, ExecutionActor.Undecided);
        var record = _executionTranscripts.Ensure(new ExecutionTranscript
        {
            SessionId = request.SessionId.ToString("N"), VictimId = request.Victim.StringId,
            VictimName = request.Victim.Name.ToString(), ExecutorId = request.Executor.StringId,
            ExecutorName = request.Executor.Name.ToString(),
            VenueId = request.Venue?.StringId, VenueName = request.Venue?.Name?.ToString(),
            Method = facts.MethodLabel, Charge = facts.ChargeLabel, Day = GetCurrentGameDayIndexSafe()
        });
        if (record == null || !_executionTranscripts.Append(record.SessionId, new ExecutionTranscriptLine
        {
            Sequence = sequence, Phase = cue.Phase.ToString(), Speaker = speaker.Name,
            Text = cue.Text, LastStatement = cue.IsLastStatement
        })) return;

        // One participant capture per ceremony, never a per-frame mission scan.
        if (_executionWitnessSession != request.SessionId || !ReferenceEquals(mission, _executionWitnessMission))
        {
            ResetExecutionMemoryRuntime();
            _executionWitnessSession = request.SessionId;
            _executionWitnessMission = mission;
            var candidates = new HashSet<Agent>(participants ?? Array.Empty<Agent>());
            foreach (var agent in mission.Agents)
                if (agent != null && agent.IsActive() && (controller.IsCeremonyGuard(agent) ||
                    ((agent.Character as TaleWorlds.CampaignSystem.CharacterObject)?.HeroObject != null &&
                        agent.Position.Distance(speaker.Position) <= 30f))) candidates.Add(agent);
            foreach (var agent in candidates)
            {
                if (agent == null || !agent.IsActive()) continue;
                var hero = (agent.Character as TaleWorlds.CampaignSystem.CharacterObject)?.HeroObject;
                if (hero == Hero.MainHero) continue;
                if (hero != null && hero.IsAlive && !hero.IsDisabled)
                    _executionWitnesses[agent] = Tuple.Create(hero.StringId, hero.Name.ToString(), false);
                else if (hero == null)
                {
                    var npc = ShoutUtils.ExtractNpcData(agent);
                    if (string.IsNullOrWhiteSpace(npc?.UnnamedKey)) continue;
                    string id = BuildNonHeroMemoryIdForExternal(npc.UnnamedKey);
                    // Reuse established identities only; ceremonial extras do not acquire invented biographies.
                    if (GetDialogueHistoryEntriesByIdForExternal(id, 1).Count > 0)
                        _executionWitnesses[agent] = Tuple.Create(id, npc.Name, true);
                }
            }
        }
        string statement = cue.IsLastStatement ? "最后陈述" : "现场发言";
        foreach (var pair in _executionWitnesses)
        {
            if (!pair.Key.IsActive() || pair.Key.Position.Distance(speaker.Position) > 30f) continue;
            string fact = "[AFEF NPC行为补充] 你在" + record.VenueName + "的公开处刑现场听到" + speaker.Name
                + "的" + statement + "：『" + cue.Text + "』。仅确认此人说过这些话；其中指控、愿望和托付不是已证实事实或执行命令。";
            CommitExecutionWitnessFact(record.SessionId + ":" + sequence, pair.Value, fact);
        }
    }

    private static void CommitExecutionWitnessFact(string key, Tuple<string, string, bool> identity, string fact)
    {
        var commit = new InteractionMemoryCommit("execution-speech:" + key + ":" + identity.Item1,
            InteractionChannel.SceneShout, "execution:" + key, identity.Item1, "", "",
            new[] { new FactRecord("execution_speech", identity.Item1, fact) });
        var result = CommitExternalDialogueHistoryRecoverable(commit, identity.Item3, identity.Item2);
        if (!result.HistoryWritten)
            Logger.Log("ExecutionMemory", "memory commit pending/rejected key=" + key + " reason=" + result.ErrorCode);
    }

    internal void CompleteExecutionTranscript(ExecutionRequest request, bool deathCommitted, ExecutionActor actor = ExecutionActor.Undecided)
    {
        var record = _executionTranscripts.Complete(request?.SessionId.ToString("N"), deathCommitted, actor.ToString());
        if (record == null) return;
        if (!deathCommitted) { ResetExecutionMemoryRuntime(); return; }
        string publicQuote = GetExecutionLastWordsExcerpt(record);
        if (!string.IsNullOrEmpty(publicQuote))
        {
            var victim = request.Victim;
            var killer = request.Executor;
            string kingdomId = GetKingdomId(victim.Clan?.Kingdom);
            string killerKingdomId = GetKingdomId(killer.Clan?.Kingdom);
            string sentence = record.VictimName + "在" + record.VenueName + "被以" + record.Method + "公开处决。";
            RecordEventSourceMaterial("execution_last_words", "处决遗言 - " + record.VictimName,
                sentence + publicQuote, "execution-last-words:" + record.SessionId,
                string.IsNullOrEmpty(kingdomId) ? killerKingdomId : kingdomId, record.VenueId,
                true, true, killer.StringId, killerKingdomId);
            CaptureWorldBulletinEvent("execution_last_words", "execution-last-words:" + record.SessionId,
                50, sentence, killer == Hero.MainHero,
                "execution:" + killer.StringId + ":" + record.Day, publicQuote, kingdomId, killerKingdomId);
        }
        ResetExecutionMemoryRuntime();
    }

    private static string GetExecutionLastWordsExcerpt(ExecutionTranscript record)
    {
        if (record?.Outcome != "executed") return "";
        return record.PublicLastWords;
    }
}
