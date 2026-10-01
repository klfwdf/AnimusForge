using System;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public partial class MyBehavior
{
    // Runtime-only encoding boundary for PRIVATE persistence DTOs. They stay on
    // their original owner/type; this schema never becomes a save/public format.
    // Every field (including unrendered data) is intentional. Reflection-driven
    // adversarial tests fail when a future DTO field is omitted here.
    private static string ComputeMemorySummarySourceFingerprint(MemorySummarySourceView source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        using (var writer = new MemorySourceFingerprintWriter())
        {
            writer.Write("AF.Memory.Raw.v1");
            if (source.Job is MemorySummaryJob daily)
            {
                writer.Write(1);
                WriteMemorySourceJob(writer, daily);
                WriteMemorySourceDraft(writer, source.Draft);
            }
            else if (source.Job is MajorActionSummaryJob major)
            {
                writer.Write(2);
                WriteMemorySourceMajorJob(writer, major);
                writer.WriteList(source.Actions, WriteMemorySourceAction);
                WriteMemorySourceMajorState(writer, source.MajorState);
                writer.Write(source.StatePresent);
            }
            else if (source.Job is MemoryOverviewJob overview)
            {
                writer.Write(3);
                WriteMemorySourceOverviewJob(writer, overview);
                writer.WriteList(source.Blocks, WriteMemorySourceBlock);
                WriteMemorySourceOverviewState(writer, source.Overview);
                writer.Write(source.StatePresent);
            }
            else throw new ArgumentException("Unsupported memory source job.", nameof(source));
            return writer.Finish();
        }
    }

    private static void WriteMemorySourceString(MemorySourceFingerprintWriter writer, string value) => writer.Write(value);

    private static void WriteMemorySourceJob(MemorySourceFingerprintWriter writer, MemorySummaryJob value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.GameDayIndex);
        writer.Write(value.GameDate);
        writer.Write(value.RetryCount);
        writer.Write(value.LastError);
    }

    private static void WriteMemorySourceDraft(MemorySourceFingerprintWriter writer, DailyMemoryDraft value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.GameDayIndex);
        writer.Write(value.GameDate);
        writer.Write(value.HasLlmDialogue);
        writer.Write(value.QueuedForSummary);
        writer.Write(value.SummaryRetryCount);
        writer.Write(value.LastSummaryError);
        writer.WriteList(value.Lines, WriteMemorySourceLine);
        writer.WriteList(value.WeeklyMaterialTriggers, WriteMemorySourceTrigger);
    }

    private static void WriteMemorySourceLine(MemorySourceFingerprintWriter writer, DailyMemoryLine value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.GameDayIndex);
        writer.Write(value.GameDate);
        writer.Write(value.GameHour);
        writer.Write(value.Scene);
        writer.Write(value.Speaker);
        writer.Write(value.Text);
        writer.Write(value.SceneSessionId);
        writer.Write(value.DialogueSessionId);
        writer.Write(value.TargetAgentIndex);
        writer.Write(value.TargetName);
        writer.Write(value.MemorySessionKey);
        writer.Write(value.IsAfef);
        writer.Write(value.IsLlmDialogue);
        writer.Write(value.MemoryCommitId);
        writer.Write(value.MemoryCommitPart);
        writer.Write(value.MemoryCommitHash);
        writer.Write(value.MemoryCommitOriginGameDay);
        writer.Write(value.MemoryCommitOriginGameDate);
    }

    private static void WriteMemorySourceTrigger(MemorySourceFingerprintWriter writer, WeeklyMemoryMaterialTrigger value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.MemoryId);
        writer.Write(value.NpcName);
        writer.Write(value.GameDayIndex);
        writer.Write(value.GameDate);
        writer.Write(value.SceneSessionId);
        writer.Write(value.DialogueSessionId);
        writer.Write(value.TargetAgentIndex);
        writer.Write(value.FootholdKingdomId);
        writer.Write(value.FootholdSettlementId);
        writer.Write(value.NormalizedTagText);
        writer.WriteList(value.Tags, WriteMemorySourceString);
        writer.Write(value.EstimatedValueDenars);
        writer.Write(value.TriggerReason);
        writer.Write(value.StableKey);
        writer.Write(value.OutcomeReceiptId);
        writer.Write(value.OutcomeCandidateHash);
        writer.Write(value.OutcomePayloadHash);
        writer.Write(value.OutcomeActionFingerprint);
        writer.Write(value.OutcomeTurnFingerprint);
        writer.Write(value.CreatedUtcTicks);
    }

    private static void WriteMemorySourceMajorJob(MemorySourceFingerprintWriter writer, MajorActionSummaryJob value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.TriggerGameDayIndex);
        writer.Write(value.TriggerGameDate);
        writer.Write(value.RetryCount);
        writer.Write(value.LastError);
    }

    private static void WriteMemorySourceAction(MemorySourceFingerprintWriter writer, NpcActionEntry value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.Day);
        writer.Write(value.Order);
        writer.Write(value.Sequence);
        writer.Write(value.GameDate);
        writer.Write(value.Text);
        writer.Write(value.StableKey);
        writer.Write(value.ActionKind);
        writer.Write(value.ActorHeroId);
        writer.Write(value.ActorClanId);
        writer.Write(value.ActorKingdomId);
        writer.Write(value.TargetHeroId);
        writer.Write(value.TargetClanId);
        writer.Write(value.TargetKingdomId);
        writer.Write(value.SettlementId);
        writer.Write(value.SettlementName);
        writer.Write(value.SettlementOwnerHeroId);
        writer.Write(value.SettlementOwnerClanId);
        writer.Write(value.SettlementOwnerKingdomId);
        writer.Write(value.PreviousSettlementOwnerHeroId);
        writer.Write(value.PreviousSettlementOwnerClanId);
        writer.Write(value.PreviousSettlementOwnerKingdomId);
        writer.Write(value.LocationText);
        writer.Write(value.Won);
        writer.Write(value.IsMajor);
        writer.WriteList(value.RelatedHeroIds, WriteMemorySourceString);
        writer.WriteList(value.RelatedClanIds, WriteMemorySourceString);
        writer.WriteList(value.RelatedKingdomIds, WriteMemorySourceString);
    }

    private static void WriteMemorySourceMajorState(MemorySourceFingerprintWriter writer, MajorActionSummaryState value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.Summary);
        writer.Write(value.LastSummarizedDay);
        writer.Write(value.LastSummarizedSequence);
        writer.Write(value.UpdatedUtcTicks);
        writer.Write(value.LastError);
    }

    private static void WriteMemorySourceOverviewJob(MemorySourceFingerprintWriter writer, MemoryOverviewJob value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.TriggerGameDayIndex);
        writer.Write(value.TriggerGameDate);
        writer.Write(value.RetryCount);
        writer.Write(value.LastError);
    }

    private static void WriteMemorySourceBlock(MemorySourceFingerprintWriter writer, CompressedMemoryBlock value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.Id);
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.GameDayIndex);
        writer.Write(value.GameDate);
        writer.Write(value.StartHour);
        writer.Write(value.EndHour);
        writer.WriteList(value.Scenes, WriteMemorySourceString);
        writer.Write(value.RichTitle);
        writer.Write(value.Summary);
        writer.WriteList(value.AfefLines, WriteMemorySourceString);
        writer.Write(value.PlayerPublicity);
        writer.Write(value.PlayerHistoryMaterial);
        writer.Write(value.PlayerPublicityReason);
        writer.Write(value.CreatedUtcTicks);
        writer.WriteList(value.WeeklyMaterialTriggers, WriteMemorySourceTrigger);
    }

    private static void WriteMemorySourceOverviewState(MemorySourceFingerprintWriter writer, MemoryOverviewState value)
    {
        writer.Write(value != null);
        if (value == null) return;
        writer.Write(value.HeroId);
        writer.Write(value.HeroName);
        writer.Write(value.Summary);
        writer.WriteList(value.IncludedBlockIds, WriteMemorySourceString);
        writer.Write(value.UpdatedUtcTicks);
        writer.Write(value.LastError);
    }
}
