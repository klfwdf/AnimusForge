using AnimusForge.Refactor.Runtime;
using System;
using AnimusForge.Refactor.Adapters;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private AnimusForge.Refactor.Adapters.MemorySummaryInputCaptureAdapter _memorySummaryInputCapture;
    private AnimusForge.Refactor.Adapters.MemorySummaryInputCaptureAdapter MemorySummaryInputCapture => _memorySummaryInputCapture ??= new AnimusForge.Refactor.Adapters.MemorySummaryInputCaptureAdapter(
        _memoryBusinessState, () => ReferenceEquals(Instance, this),
        () => ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this));
    // Runtime-only input. No Hero/Agent/Campaign or live collection escapes capture.
    // Full content is checked, not just a day/count or a Save-method-only revision.
    private MemorySummaryApplicationAdapter _memorySummaryApplication;
    private MemorySummaryApplicationAdapter MemorySummaryApplication => _memorySummaryApplication ??= new MemorySummaryApplicationAdapter(
        RunMemorySummaryRunCaptureAsync, MemorySummaryInputCapture.CaptureMemorySummaryInput, MemorySummaryInputCapture.IsMemorySummaryInputCurrent,
        () => ReferenceEquals(Instance, this),
        (system,user,area) => CallAuxiliaryGatewayDetailed(system,user,area,0,forceThinkingDisabled:true),
        MemoryEntityIdentityBannerlordAdapter.FindHeroById, MemoryRecallInputCaptureAdapter.ResolveCapturedMemoryLineSceneForPrompt,
        new MemorySummaryCommitCapabilities { State = _memoryBusinessState, Queue = () => MemoryQueueState, Record = () => _campaignCharacterRecordCapture });

    internal sealed class MemorySummaryInput
    {
        internal long Generation;
        internal MemorySummaryRunOwner.Lease Run;
        internal object QueueJob;
        internal object Job;
        internal string HeroId;
        internal string SystemPrompt;
        internal string UserPrompt;
        internal string SourceFingerprint;
        internal string ContextFingerprint;
        internal MemorySummaryContextDependencies Context = new MemorySummaryContextDependencies();
        internal int OverviewBlockCount;
        internal DailyMemoryDraft Draft;
        internal List<NpcActionEntry> Actions;
        internal List<CompressedMemoryBlock> Blocks;
        internal MemoryOverviewState Overview;
    }

    internal sealed class CapturedMemorySummaryResult
    {
        internal MemorySummaryInput Source;
        internal object Value;
        internal string Error = "";
        internal bool IsObsolete;
    }

    // Known private persistence-data models only; no game objects or polymorphic types.
    private static T CloneMemorySummarySource<T>(T value)
    {
        return MemorySummaryInputCaptureAdapter.CloneMemorySummarySource(value);
    }

    // Only dependency descriptors survive completion: never retain full source graphs here.
    // The raw fingerprint catches edits even when sanitization renders the same text.
    internal sealed class MemorySummaryContextDependencies
    {
        internal int DailySourceCharCount;
        internal int[] UnknownTextSceneDays = Array.Empty<int>();
        internal MemorySummarySceneDependency[] SceneHeader = Array.Empty<MemorySummarySceneDependency>();
        internal bool HasKnownSourceScene;
        internal string[] HeroIds = Array.Empty<string>();
        internal string[] ClanIds = Array.Empty<string>();
        internal string[] KingdomIds = Array.Empty<string>();
        internal NpcActionEntry[] SettlementLookups = Array.Empty<NpcActionEntry>();
    }

    internal sealed class MemorySummarySceneDependency
    {
        internal string Scene;
        internal int UnknownDay;
    }

    private static void DescribeMemorySummaryDailyContext(MemorySummaryInput input)
    {
        MemorySummaryInputCaptureAdapter.DescribeMemorySummaryDailyContext(input);
    }

    // Ephemeral, main-thread-only view. It must never be stored on the async input.
    internal sealed class MemorySummarySourceView
    {
        internal string HeroId;
        internal object Job;
        internal bool StatePresent;
        internal DailyMemoryDraft Draft;
        internal List<NpcActionEntry> Actions;
        internal MajorActionSummaryState MajorState;
        internal List<CompressedMemoryBlock> Blocks;
        internal MemoryOverviewState Overview;
    }

    private MemorySummarySourceView ReadMemorySummarySource(object queueJob, long generation)
    {
        return MemorySummaryInputCapture.ReadMemorySummarySource(queueJob, generation);
    }

    private static string[] CollectMemorySummaryLookupIds(IEnumerable<string> ids)
    {
        return MemorySummaryInputCaptureAdapter.CollectMemorySummaryLookupIds(ids);
    }

    private static void DescribeMemorySummaryMajorContext(MemorySummaryInput input, List<NpcActionEntry> added)
    {
        MemorySummaryInputCaptureAdapter.DescribeMemorySummaryMajorContext(input, added);
    }

    private static object CaptureMemorySummaryDailySceneContext(MemorySummaryContextDependencies context)
    {
        return MemorySummaryInputCaptureAdapter.CaptureMemorySummaryDailySceneContext(context);
    }

    private string CaptureMemorySummaryContextFingerprint(MemorySummaryInput input)
    {
        return MemorySummaryInputCapture.CaptureMemorySummaryContextFingerprint(input);
    }

    private MemorySummaryInput CaptureMemorySummaryInput(object queueJob, long generation, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null)
    {
        return MemorySummaryInputCapture.CaptureMemorySummaryInput(queueJob, generation, expectedJobFingerprint, run);
    }

    // Stream a single framed object into the digest. Avoid serializing a source
    // to a giant string only to escape/encode that string again inside identity.
    private static string ComputeMemorySummaryFingerprint(object identity)
    {
        return MemorySummaryInputCaptureAdapter.ComputeMemorySummaryFingerprint(identity);
    }

    private bool IsMemorySummaryInputCurrent(MemorySummaryInput input)
    {
        return MemorySummaryInputCapture.IsMemorySummaryInputCurrent(input);
    }

    private Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(object job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null) => MemorySummaryApplication.ExecuteAsync(job, maxAttempts, expectedJobFingerprint, run);
}
