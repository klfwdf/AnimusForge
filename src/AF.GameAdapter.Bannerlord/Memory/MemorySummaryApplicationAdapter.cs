using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
namespace AnimusForge;

// Detached request/retry/result lifecycle. All Hero reads/parsing stay in the accepted campaign dispatcher.
internal sealed class MemorySummaryApplicationAdapter
{
 private readonly Func<MemorySummaryRunOwner.Lease,long,Func<bool>,Task<bool>> _dispatch;
 private readonly Func<object,long,string,MemorySummaryRunOwner.Lease,MyBehavior.MemorySummaryInput> _capture;
 private readonly Func<MyBehavior.MemorySummaryInput,bool> _inputCurrent;
 private readonly Func<bool> _ownerCurrent;
 private readonly Func<string,string,string,Task<MyBehavior.ApiCallResult>> _call;
 private readonly Func<string,Hero> _findHero;
 private readonly Func<DailyMemoryLine,string> _resolveScene;
 private readonly MemorySummaryCommitCapabilities _commit;
 internal MemorySummaryApplicationAdapter(Func<MemorySummaryRunOwner.Lease,long,Func<bool>,Task<bool>> dispatch,
   Func<object,long,string,MemorySummaryRunOwner.Lease,MyBehavior.MemorySummaryInput> capture,
   Func<MyBehavior.MemorySummaryInput,bool> inputCurrent, Func<bool> ownerCurrent,
   Func<string,string,string,Task<MyBehavior.ApiCallResult>> call, Func<string,Hero> findHero,
   Func<DailyMemoryLine,string> resolveScene, MemorySummaryCommitCapabilities commit = null)
 { _dispatch=dispatch; _capture=capture; _inputCurrent=inputCurrent; _ownerCurrent=ownerCurrent; _call=call; _findHero=findHero; _resolveScene=resolveScene; _commit=commit; }
    internal async Task<MyBehavior.CapturedMemorySummaryResult> ExecuteAsync(object job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null)
    {
        var result = new MyBehavior.CapturedMemorySummaryResult();
        long generation = run?.Generation ?? SaveRuntimeGuard.CaptureGeneration();
        try
        {
            bool accepted = await _dispatch(run, generation, delegate
            {
                result.Source = _capture(job, generation, expectedJobFingerprint, run);
                return result.Source != null;
            });
            if (!accepted) { result.IsObsolete = true; return result; }
            var outcome = await MemorySummaryAttemptRunner.RunAsync(maxAttempts,
                () => _dispatch(run, generation, () => _inputCurrent(result.Source)),
                () => (run == null || run.IsCurrent) && _ownerCurrent() && SaveRuntimeGuard.IsCurrentGeneration(generation),
                async () =>
                {
                    var input = result.Source;
                    string area = job is MemorySummaryJob ? "CompressedMemory" : job is MajorActionSummaryJob ? "NpcMajorSummary" : "MemoryOverview";
                    var api = await _call(input.SystemPrompt, input.UserPrompt, area).ConfigureAwait(false);
                    bool parsed = false;
                    bool current = await _dispatch(run, generation, delegate
                    {
                        if (!_inputCurrent(input)) return false;
                        if (!api.Success) { result.Error = api.ErrorMessage ?? "API请求失败"; return true; }
                        var hero = _findHero(input.HeroId);
                        string error;
                        if (input.Job is MemorySummaryJob)
                        {
                            parsed = TryParseMemorySummaryResponse(api.Content, hero, input.Draft, line => BuildDailyMemoryLineForPrompt(line, _resolveScene), out var block, out error);
                            result.Value = block;
                            if (!parsed) result.Error = MemoryBusinessStateOwner.BuildSummaryJsonParseFailureMessage("总结格式解析失败", error, api.Content);
                        }
                        else if (input.Job is MajorActionSummaryJob major)
                        {
                            parsed = TryParseMajorActionSummaryResponse(api.Content, hero, major, input.Actions, out var state, out error);
                            result.Value = state;
                            if (!parsed) result.Error = MemoryBusinessStateOwner.BuildSummaryJsonParseFailureMessage("重大履历总结格式解析失败", error, api.Content);
                        }
                        else
                        {
                            parsed = TryParseMemoryOverviewResponse(api.Content, hero, (MemoryOverviewJob)input.Job,
                                input.Overview, input.Blocks, out var state, out error);
                            result.Value = state;
                            if (!parsed) result.Error = MemoryBusinessStateOwner.BuildSummaryJsonParseFailureMessage("记忆总览格式解析失败", error, api.Content);
                        }
                        return true;
                    });
                    if (!current) result.Value = null;
                    return new MemorySummaryAttemptRunner.Receipt(current, parsed, api.RetryAfterSeconds);
                }, milliseconds => Task.Delay(milliseconds));
            if (outcome == MemorySummaryAttemptRunner.Outcome.Obsolete)
            { result.IsObsolete = true; result.Value = null; return result; }
            if (outcome == MemorySummaryAttemptRunner.Outcome.Completed) return result;
        }
        catch (Exception ex) { result.Error = ex.Message; }
        finally
        {
            // Process may wait for other RPM waves before applying this receipt.
            // Retain only its identity/fingerprint, not every completed job's full
            // draft, rendered prompt and source blocks until the whole queue ends.
            if (result.Source != null)
            {
                result.Source.Draft = null;
                result.Source.Actions = null;
                result.Source.Blocks = null;
                result.Source.Overview = null;
                result.Source.SystemPrompt = null;
                result.Source.UserPrompt = null;
            }
        }
        return result;
    }

	internal static bool TryParseMemoryOverviewResponse(string content, Hero hero, MemoryOverviewJob job, MemoryOverviewState existingState, List<CompressedMemoryBlock> sourceBlocks, out MemoryOverviewState state, out string error)
	{
		state = null;
		error = "";
		try
		{
			if (!MemoryBusinessStateOwner.TryParseBestSummaryJsonObject(content, new string[4] { "summary_content", "summaryContent", "summary", "content" }, null, out var jObject, out error))
			{
				return false;
			}
			if (!MemorySummaryRules.TryReadSummary(jObject, out string summary, out error))
			{
				return false;
			}
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job?.HeroId ?? hero?.StringId);
			List<string> includedBlockIds = new List<string>();
			if (existingState != null && existingState.IncludedBlockIds != null)
			{
				includedBlockIds.AddRange(existingState.IncludedBlockIds);
			}
			foreach (CompressedMemoryBlock block in sourceBlocks ?? new List<CompressedMemoryBlock>())
			{
				if (block == null)
				{
					continue;
				}
				string blockId = (block.Id ?? MemoryRecordRules.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
				if (!string.IsNullOrWhiteSpace(blockId))
				{
					includedBlockIds.Add(blockId);
				}
			}
			state = new MemoryOverviewState
			{
				HeroId = heroId,
				HeroName = hero?.Name?.ToString() ?? job?.HeroName ?? "NPC",
				Summary = summary,
				IncludedBlockIds = includedBlockIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
				UpdatedUtcTicks = DateTime.UtcNow.Ticks,
				LastError = ""
			};
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	internal static bool TryParseMajorActionSummaryResponse(string content, Hero hero, MajorActionSummaryJob job, List<NpcActionEntry> allActions, out MajorActionSummaryState state, out string error)
	{
		state = null;
		error = "";
		try
		{
			if (!MemoryBusinessStateOwner.TryParseBestSummaryJsonObject(content, new string[4] { "summary_content", "summaryContent", "summary", "content" }, null, out var jObject, out error))
			{
				return false;
			}
			if (!MemorySummaryRules.TryReadSummary(jObject, out string summary, out error))
			{
				return false;
			}
			MemoryBusinessStateOwner.GetMajorActionMaxCursor(allActions, out var day, out var sequence);
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job?.HeroId ?? hero?.StringId);
			state = new MajorActionSummaryState
			{
				HeroId = heroId,
				HeroName = hero?.Name?.ToString() ?? job?.HeroName ?? "NPC",
				Summary = summary,
				LastSummarizedDay = day,
				LastSummarizedSequence = sequence,
				UpdatedUtcTicks = DateTime.UtcNow.Ticks,
				LastError = ""
			};
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	internal static bool TryParseMemorySummaryResponse(string content, Hero hero, DailyMemoryDraft draft, Func<DailyMemoryLine,string> renderDailyLine, out CompressedMemoryBlock block, out string error)
	{
		block = null;
		error = "";
		try
		{
			if (!MemoryBusinessStateOwner.TryParseBestSummaryJsonObject(content, new string[3] { "rich_title", "richTitle", "title" }, new string[4] { "summary_content", "summaryContent", "summary", "content" }, out var jObject, out error))
			{
				return false;
			}
			if (!MemorySummaryRules.TryReadDaily(jObject, MemoryRecordRules.StripMemoryTitleDateTime,
				raw => PlayerNotorietyBehavior.NormalizeMemoryPublicity(raw, RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0),
				PlayerNotorietyBehavior.RenderPlayerHistoryMaterialForExternal, out var fields, out error))
			{
				return false;
			}
			List<DailyMemoryLine> allLines = draft.Lines ?? new List<DailyMemoryLine>();
			List<DailyMemoryLine> ordered = allLines.Where((DailyMemoryLine x) => x != null).OrderBy((DailyMemoryLine x) => x.GameHour).ToList();
			int startHour = ordered.Select((DailyMemoryLine x) => MBMath.ClampInt(x.GameHour, 0, 23)).DefaultIfEmpty(0).Min();
			int endHour = ordered.Select((DailyMemoryLine x) => MBMath.ClampInt(x.GameHour, 0, 23)).DefaultIfEmpty(0).Max();
			List<string> scenes = ordered.Select((DailyMemoryLine x) => (x.Scene ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToList();
			List<string> afefLines = ordered.Where((DailyMemoryLine x) => x.IsAfef).Select(renderDailyLine).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
			string heroId = MemoryRecordRules.NormalizeMemoryHeroId(draft.HeroId);
			block = new CompressedMemoryBlock
			{
				Id = MemoryRecordRules.BuildCompressedMemoryBlockId(heroId, draft.GameDayIndex),
				HeroId = heroId,
				HeroName = hero?.Name?.ToString() ?? draft.HeroName ?? "NPC",
				GameDayIndex = draft.GameDayIndex,
				GameDate = draft.GameDate ?? "",
				StartHour = startHour,
				EndHour = endHour,
				Scenes = scenes,
				RichTitle = fields.Title,
				Summary = fields.Summary,
				AfefLines = afefLines,
				PlayerPublicity = fields.Publicity,
				PlayerHistoryMaterial = fields.History,
				PlayerPublicityReason = fields.Reason,
				WeeklyMaterialTriggers = MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(draft.WeeklyMaterialTriggers),
				CreatedUtcTicks = DateTime.UtcNow.Ticks
			};
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}
	internal static string BuildDailyMemoryLineForPrompt(DailyMemoryLine line, Func<DailyMemoryLine,string> resolveScene)
	{
		if (line == null || string.IsNullOrWhiteSpace(line.Text))
		{
			return "";
		}
		string text = (string.IsNullOrWhiteSpace(line.GameDate) ? ("第" + line.GameDayIndex + "日") : line.GameDate.Trim());
		string text2 = resolveScene(line);
		string text3 = string.IsNullOrWhiteSpace(line.Speaker) ? (line.IsAfef ? "AFEF" : "对话") : line.Speaker.Trim();
		return "[" + text + " " + MBMath.ClampInt(line.GameHour, 0, 23) + "时｜" + text2 + "｜" + text3 + "] " + line.Text.Trim();
	}
internal static string BuildMajorActionSummarySourceLine(Hero hero, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string date = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日");
		string text = NpcActionRecordOwner.RenderText(hero?.Name?.ToString()?.Trim(), entry.Text, CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker) + CampaignCharacterRecordCaptureAdapter.BuildNpcActionMetadataNarrativeSuffix(entry);
		return (date + "｜" + text).Trim();
	}

internal static int GetMajorActionSummaryTargetChars(MajorActionSummaryState state, Hero hero, List<NpcActionEntry> sourceActions)
	{
		int chars = 0;
		if (state != null && !string.IsNullOrWhiteSpace(state.Summary))
		{
			chars += CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker(state.Summary.Trim()).Length;
		}
		foreach (NpcActionEntry entry in sourceActions ?? new List<NpcActionEntry>())
		{
			chars += BuildMajorActionSummarySourceLine(hero, entry).Length;
		}
		if (chars <= 0)
		{
			return 350;
		}
		return MBMath.ClampInt(chars / 8, 180, 700);
	}

internal static string BuildMemoryOverviewBlockSourceText(CompressedMemoryBlock block)
	{
		if (block == null)
		{
			return "";
		}
		string date = string.IsNullOrWhiteSpace(block.GameDate) ? ("第" + block.GameDayIndex + "日") : block.GameDate.Trim();
		string title = MemoryRecordRules.StripMemoryTitleDateTime(block.RichTitle);
		if (string.IsNullOrWhiteSpace(title))
		{
			title = "往日对话记忆";
		}
		string scenes = block.Scenes != null && block.Scenes.Count > 0 ? string.Join(" / ", block.Scenes.Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)) : "地点未知";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("- 记忆块ID：" + ((block.Id ?? MemoryRecordRules.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim()));
		stringBuilder.AppendLine("  日期时间地点：" + date + " " + MemoryRecallContextOwner.FormatMemoryHourRange(block.StartHour, block.EndHour) + "｜" + scenes);
		stringBuilder.AppendLine("  标题：" + title);
		stringBuilder.AppendLine("  内容：" + (block.Summary ?? "").Trim());
		if (block.AfefLines != null && block.AfefLines.Count > 0)
		{
			stringBuilder.AppendLine("  AFEF事实参考：");
			foreach (string line in block.AfefLines)
			{
				if (!string.IsNullOrWhiteSpace(line))
				{
					stringBuilder.AppendLine("  " + line.Trim());
				}
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

internal async Task<MyBehavior.DailySummaryQueueResult> ExecuteDailySummaryQueueItemAsync(object item, MemorySummaryRunOwner.Lease run = null)
	{
		MyBehavior.DailySummaryQueueResult result = new MyBehavior.DailySummaryQueueResult();
		string expectedJobFingerprint = null;
		if (item is MemorySummaryPlanEntry planned)
		{
			expectedJobFingerprint = planned.JobFingerprint;
			item = planned.Job;
		}
		MemorySummaryJob memoryJob = item as MemorySummaryJob;
		if (memoryJob != null)
		{
			result.MemoryResult = await ExecuteMemorySummaryJobAsync(memoryJob, 3, expectedJobFingerprint, run);
			return result;
		}
		MajorActionSummaryJob majorJob = item as MajorActionSummaryJob;
		if (majorJob != null)
		{
			result.MajorActionResult = await ExecuteMajorActionSummaryJobAsync(majorJob, 3, expectedJobFingerprint, run);
			return result;
		}
		MemoryOverviewJob overviewJob = item as MemoryOverviewJob;
		if (overviewJob != null)
		{
			result.MemoryOverviewResult = await ExecuteMemoryOverviewJobAsync(overviewJob, 3, expectedJobFingerprint, run);
			return result;
		}
		return result;
	}

internal async Task<MyBehavior.MemorySummaryExecutionResult> ExecuteMemorySummaryJobAsync(MemorySummaryJob job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null)
	{
		MyBehavior.CapturedMemorySummaryResult captured = await ExecuteAsync(job, maxAttempts, expectedJobFingerprint, run);
		return new MyBehavior.MemorySummaryExecutionResult
		{
			Job = job,
			Source = captured.Source,
			Block = captured.Value as CompressedMemoryBlock,
			Error = captured.Error,
			IsObsolete = captured.IsObsolete
		};
	}

internal async Task<MyBehavior.MajorActionSummaryExecutionResult> ExecuteMajorActionSummaryJobAsync(MajorActionSummaryJob job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null)
	{
		MyBehavior.CapturedMemorySummaryResult captured = await ExecuteAsync(job, maxAttempts, expectedJobFingerprint, run);
		return new MyBehavior.MajorActionSummaryExecutionResult
		{
			Job = job,
			Source = captured.Source,
			State = captured.Value as MajorActionSummaryState,
			Error = captured.Error,
			IsObsolete = captured.IsObsolete
		};
	}

internal async Task<MyBehavior.MemoryOverviewExecutionResult> ExecuteMemoryOverviewJobAsync(MemoryOverviewJob job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null)
	{
		MyBehavior.CapturedMemorySummaryResult captured = await ExecuteAsync(job, maxAttempts, expectedJobFingerprint, run);
		return new MyBehavior.MemoryOverviewExecutionResult
		{
			Job = job,
			Source = captured.Source,
			State = captured.Value as MemoryOverviewState,
			Error = captured.Error,
			IsObsolete = captured.IsObsolete
		};
	}

internal static string BuildMemoryOverviewContextById(MemoryBusinessStateOwner memory, string memoryId, Func<string,bool> isEligible, Func<int> overviewStartCount)
	{
		string heroId = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (!isEligible(heroId))
		{
			return "";
		}
		List<CompressedMemoryBlock> blocks = memory.LoadBlocks(heroId);
		MemoryOverviewState state = memory.GetMemoryOverviewState(heroId);
		if (state != null && !string.IsNullOrWhiteSpace(state.LastError) && blocks != null && blocks.Count >= overviewStartCount() && memory.HasMemoryOverviewPendingBlocks(heroId, blocks))
		{
			return "【过往记忆总览】\n过往记忆总览正在整理，暂不可引用总览细节。";
		}
		if (state != null && !string.IsNullOrWhiteSpace(state.Summary))
		{
			return "【过往记忆总览】\n" + state.Summary.Trim();
		}
		if (blocks == null || blocks.Count < overviewStartCount())
		{
			return "";
		}
		return "【过往记忆总览】\n过往记忆总览正在整理，暂不可引用总览细节。";
	}

internal static void TryStartMemorySummaryQueue(MemoryBusinessStateOwner memory, MemorySummaryRunOwner runs, Func<bool> isBusy, Func<bool,Task> processQueue, double throttleSeconds, bool forceOverviewCandidateScan = false)
	{
		try
		{
			if (runs.IsRunning || isBusy())
			{
				return;
			}
			// Admission is O(1): eligibility can inspect every source record, so do it
			// only in the guarded planner, never again on each maintenance/entry call.
			bool hasMemoryJobs = (memory.DailyQueue?.Count ?? 0) > 0;
			bool hasMajorActionJobs = (memory.MajorQueue?.Count ?? 0) > 0;
			bool hasOverviewJobs = (memory.OverviewQueue?.Count ?? 0) > 0;
			if (!hasMemoryJobs && !hasMajorActionJobs && !hasOverviewJobs && memory.ShouldScanMemoryOverviewCandidates(forceOverviewCandidateScan, throttleSeconds))
			{
				using (PerfProbe.Scope("MyBehavior.TryStartMemorySummaryQueue.EnqueueMemoryOverviewForAllCandidates"))
				{
					if (forceOverviewCandidateScan)
					{
						memory.QueueAllMemoryOverviewCandidatesForDeferredScan();
					}
					else
					{
						memory.QueueDirtyMemoryOverviewCandidatesForDeferredScan();
					}
				}
				// This only enqueues candidate IDs; the existing budgeted scanner creates jobs later.
			}
			if (!hasMemoryJobs && !hasMajorActionJobs && !hasOverviewJobs)
			{
				return;
			}
			_ = processQueue(forceOverviewCandidateScan);
		}
		catch (Exception ex)
		{
			Logger.Log("CompressedMemory", "[ERROR] TryStartMemorySummaryQueue failed: " + ex.Message);
		}
	}

internal bool ApplyMemoryOverviewSuccess(MemoryOverviewJob job, MemoryOverviewState state)
	{
		if (job == null || state == null || string.IsNullOrWhiteSpace(state.Summary))
		{
			return false;
		}
		string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		Hero hero = _findHero(heroId);
		if (!MemoryBusinessStateOwner.IsNonHeroMemoryId(heroId) && !MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(hero))
		{
			// The target can disappear while the async API request is in flight; never write its stale result back.
			_commit.Queue().CancelUnavailableHeroCompressionWorkById(heroId, "memory_overview_apply");
			return false;
		}
		return _commit.State.ApplyOverview(job, state, heroId, () => Logger.Log("MemoryOverview", "summary_success hero=" + heroId + " blocks=" + (state.IncludedBlockIds?.Count ?? 0)));
	}

internal bool ApplyMajorActionSummarySuccess(MajorActionSummaryJob job, MajorActionSummaryState state)
	{
		if (job == null || state == null || string.IsNullOrWhiteSpace(state.Summary))
		{
			return false;
		}
		string heroId = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		if (!MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(_findHero(heroId)))
		{
			// Raw major actions remain for weekly reports, but an in-flight result cannot recreate derived memory for a removed Hero.
			_commit.Queue().CancelUnavailableHeroCompressionWorkById(heroId, "major_action_summary_apply");
			return false;
		}
		return _commit.State.ApplyMajor(job, state, heroId, () => Logger.Log("NpcMajorSummary", "summary_success hero=" + heroId + " day=" + state.LastSummarizedDay + " sequence=" + state.LastSummarizedSequence));
	}

internal bool ApplyMemorySummarySuccess(MemorySummaryJob job, CompressedMemoryBlock block)
	{
		if (job == null || block == null)
		{
			return false;
		}
		string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId);
		Hero hero = _findHero(memoryId);
		if (!MemoryBusinessStateOwner.IsNonHeroMemoryId(memoryId) && !MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(hero))
		{
			// The target can disappear while the async API request is in flight; drop the result and stale derived state.
			_commit.Queue().CancelUnavailableHeroCompressionWorkById(memoryId, "memory_summary_apply");
			return false;
		}
		return _commit.State.ApplyDaily(job, block, memoryId, new MemoryDailyCommitEffects
        {
            MarkOverviewDirty = id => _commit.Queue().MarkMemoryOverviewDirty(id),
            ClearNativeHistory = day => { if (hero != null) ShoutBehavior.ClearNativeConversationSessionHistoryForExternal(hero, hero.CharacterObject, hero.Name?.ToString(), day); },
            LogSuccess = () => Logger.Log("CompressedMemory", "summary_success hero=" + (job.HeroId ?? "") + " day=" + job.GameDayIndex + " title=" + (block.RichTitle ?? "")),
            RecordPublicMemory = value => PlayerNotorietyBehavior.RecordPublicMemoryForExternal(hero, hero?.CurrentSettlement ?? Settlement.CurrentSettlement, value.PlayerHistoryMaterial, value.PlayerPublicity, value.PlayerPublicityReason, value.GameDayIndex, value.GameDate),
            RecordPublicWeeklyMaterial = value => _commit.Record().RecordPublicDailyMemoryWeeklyMaterial(value),
            RecordWeeklyMaterial = value => _commit.Record().RecordWeeklyMemoryMaterialForBlock(value),
            EnqueueOverview = (id, name, blocks) => _commit.Queue().TryEnqueueMemoryOverviewForMemoryId(id, name, blocks)
        });
	}
}

internal sealed class MemorySummaryCommitCapabilities
{
 internal MemoryBusinessStateOwner State;
 internal Func<MemoryBusinessStateOwner> Queue;
 internal Func<CampaignCharacterRecordCaptureAdapter> Record;
}
