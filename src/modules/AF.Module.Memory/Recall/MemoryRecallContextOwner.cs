using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Request-frequency detached recall; game reads and popup delivery stay in the capture adapter.
internal static class MemoryRecallContextOwner
{
internal static string BuildMemoryRecallQueryText(string currentInput, string secondaryInput, IEnumerable<DailyMemoryDraft> drafts, string capturedScene)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = (currentInput ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.AppendLine(text);
		}
		string text2 = (secondaryInput ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			stringBuilder.AppendLine(text2);
		}
		string text3 = capturedScene;
		if (!string.IsNullOrWhiteSpace(text3))
		{
			stringBuilder.AppendLine(text3);
		}
		try
		{
			DailyMemoryDraft dailyMemoryDraft = (drafts ?? Enumerable.Empty<DailyMemoryDraft>()).OrderByDescending((DailyMemoryDraft x) => x.GameDayIndex).FirstOrDefault((DailyMemoryDraft x) => x?.Lines != null && x.Lines.Count > 0);
			if (dailyMemoryDraft?.Lines != null)
			{
				foreach (DailyMemoryLine item in dailyMemoryDraft.Lines.Where((DailyMemoryLine x) => x != null && !x.IsAfef && !string.IsNullOrWhiteSpace(x.Text)).Reverse().Take(6).Reverse())
				{
					stringBuilder.AppendLine(item.Text);
				}
			}
		}
		catch
		{
		}
		string text4 = stringBuilder.ToString().Trim();
		if (text4.Length > 1200)
		{
			text4 = text4.Substring(text4.Length - 1200);
		}
		return text4;
	}

internal static bool TryBuildMemoryRecallCandidates(List<CompressedMemoryBlock> blocks, string currentInput, string secondaryInput, List<DailyMemoryDraft> drafts, int candidateLimit, out List<MemoryRecallCandidate> candidates, out string error, long runtimeGeneration, MemoryRecallRequest snapshot)
	{
		Stopwatch sw = Stopwatch.StartNew();
		candidates = new List<MemoryRecallCandidate>();
		error = "";
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration) || (snapshot != null && !snapshot.IsCurrent())) { error = "memory_recall_stale_generation"; return false; }
		List<CompressedMemoryBlock> list = (blocks ?? new List<CompressedMemoryBlock>()).Where((CompressedMemoryBlock x) => x != null && (!string.IsNullOrWhiteSpace(x.RichTitle) || !string.IsNullOrWhiteSpace(x.Summary))).ToList();
		string debugHeroId = MemoryRecordRules.NormalizeMemoryHeroId(list.Select((CompressedMemoryBlock x) => x?.HeroId).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)));
		Logger.Log("Logic", "[MemoryPerf] recall_candidates_start hero=" + (debugHeroId ?? "") + " blocks=" + ((blocks ?? new List<CompressedMemoryBlock>()).Count) + " eligible=" + list.Count + " candidateLimit=" + candidateLimit + " currentLen=" + ((currentInput ?? "").Length) + " secondaryLen=" + ((secondaryInput ?? "").Length) + " drafts=" + (snapshot?.DraftCount ?? ((drafts ?? new List<DailyMemoryDraft>()).Count)));
		if (list.Count <= 0 || candidateLimit <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] recall_candidates_done hero=" + (debugHeroId ?? "") + " mode=empty selected=0 ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return true;
		}
		if (list.Count <= candidateLimit)
		{
			candidates = list.Select((CompressedMemoryBlock x) => new MemoryRecallCandidate
			{
				Block = x,
				Score = 1.0
			}).OrderBy((MemoryRecallCandidate x) => x.Block.GameDayIndex).ThenBy((MemoryRecallCandidate x) => x.Block.StartHour).ToList();
			AssignMemoryCandidateDisplayIds(candidates);
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] recall_candidates_done hero=" + (debugHeroId ?? "") + " mode=direct selected=" + candidates.Count + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return true;
		}
		try
		{
			OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
			string text = snapshot?.RecallQuery ?? BuildMemoryRecallQueryText(currentInput, secondaryInput, drafts, snapshot.Scene);
			Stopwatch querySw = Stopwatch.StartNew();
			if (instance == null || !instance.IsAvailable || string.IsNullOrWhiteSpace(text) || !instance.TryGetEmbedding(text, out var vector) || vector == null || vector.Length == 0)
			{
				querySw.Stop();
				error = "Local ONNX embedding is unavailable; rich-title RAG cannot select compressed memories above the candidate limit.";
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] recall_candidates_failed hero=" + (debugHeroId ?? "") + " reason=query_embedding_unavailable queryLen=" + ((text ?? "").Length) + " queryMs=" + Math.Round(querySw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
				snapshot.RecordBlockingFailure("Compressed memory recall blocked", error + "\n\nCheck the embedding model. The request may use recent local memory blocks instead.", runtimeGeneration);
                snapshot.SetBlockingDisplay("AF_MEMORY_UI_RecallBlockedTitle", "Compressed memory recall blocked",
                    MemoryFailureUiText.T("AF_MEMORY_UI_RecallEmbeddingUnavailable", "Local ONNX embedding is unavailable; rich-title RAG cannot select compressed memories above the candidate limit.\n\nCheck the embedding model. The request may use recent local memory blocks instead."), runtimeGeneration);
				return false;
			}
			querySw.Stop();
			List<MemoryRecallCandidate> list2 = new List<MemoryRecallCandidate>();
			Stopwatch titleSw = Stopwatch.StartNew();
			int scanned = 0;
			int titleEmbeddingFailed = 0;
			foreach (CompressedMemoryBlock block in list)
			{
                if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration) || (snapshot != null && !snapshot.IsCurrent())) { error = "memory_recall_stale_generation"; return false; }
				scanned++;
				string text2 = (block.RichTitle ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					titleEmbeddingFailed++;
					continue;
				}
				if (!instance.TryGetEmbedding(text2, out var vector2) || vector2 == null || vector2.Length == 0)
				{
					titleEmbeddingFailed++;
					continue;
				}
				int num = Math.Min(vector.Length, vector2.Length);
				double num2 = 0.0;
				for (int i = 0; i < num; i++)
				{
					num2 += (double)vector[i] * (double)vector2[i];
				}
				list2.Add(new MemoryRecallCandidate
				{
					Block = block,
					Score = num2
				});
			}
			titleSw.Stop();
			if (list2.Count <= 0)
			{
				error = "All compressed-memory rich-title embeddings failed.";
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] recall_candidates_failed hero=" + (debugHeroId ?? "") + " reason=title_embedding_empty scanned=" + scanned + " failed=" + titleEmbeddingFailed + " queryMs=" + Math.Round(querySw.Elapsed.TotalMilliseconds, 2) + " titleMs=" + Math.Round(titleSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
				snapshot.RecordBlockingFailure("Compressed memory recall blocked", error + "\n\nFix the embedding model, then retry.", runtimeGeneration);
                snapshot.SetBlockingDisplay("AF_MEMORY_UI_RecallBlockedTitle", "Compressed memory recall blocked",
                    MemoryFailureUiText.T("AF_MEMORY_UI_RecallTitleEmbeddingFailed", "Embedding failed for every compressed memory rich title.\n\nFix the embedding model, then retry."), runtimeGeneration);
				return false;
			}
			candidates = list2.OrderByDescending((MemoryRecallCandidate x) => x.Score).ThenByDescending((MemoryRecallCandidate x) => x.Block.GameDayIndex).Take(candidateLimit).OrderBy((MemoryRecallCandidate x) => x.Block.GameDayIndex).ThenBy((MemoryRecallCandidate x) => x.Block.StartHour).ToList();
			AssignMemoryCandidateDisplayIds(candidates);
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] recall_candidates_done hero=" + (debugHeroId ?? "") + " mode=onnx eligible=" + list.Count + " scanned=" + scanned + " titleOk=" + list2.Count + " titleFail=" + titleEmbeddingFailed + " selected=" + candidates.Count + " queryMs=" + Math.Round(querySw.Elapsed.TotalMilliseconds, 2) + " titleMs=" + Math.Round(titleSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] recall_candidates_failed hero=" + (debugHeroId ?? "") + " reason=exception type=" + ex.GetType().Name + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " msg=" + ex.Message);
			snapshot.RecordBlockingFailure("Compressed memory recall exception", error, runtimeGeneration);
            snapshot.SetBlockingDisplay("AF_MEMORY_UI_RecallExceptionTitle", "Compressed memory recall exception", MemoryFailureUiText.Error(error), runtimeGeneration);
			return false;
		}
	}

internal static void AssignMemoryCandidateDisplayIds(List<MemoryRecallCandidate> candidates)
	{
		if (candidates == null)
		{
			return;
		}
		for (int i = 0; i < candidates.Count; i++)
		{
			if (candidates[i] != null)
			{
				candidates[i].DisplayId = i + 1;
			}
		}
	}

internal static bool TrySelectMemoryIdsWithPreprocess(List<MemoryRecallCandidate> candidates, int finalCount, string currentInput, string secondaryInput, out List<int> selectedIds, out string error, long runtimeGeneration, MemoryRecallRequest snapshot)
	{
		Stopwatch sw = Stopwatch.StartNew();
		selectedIds = new List<int>();
		error = "";
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration) || (snapshot != null && !snapshot.IsCurrent())) { error = "memory_recall_stale_generation"; return false; }
		List<MemoryRecallCandidate> list = (candidates ?? new List<MemoryRecallCandidate>()).Where((MemoryRecallCandidate x) => x?.Block != null && x.DisplayId > 0).ToList();
		string debugHeroId = MemoryRecordRules.NormalizeMemoryHeroId(list.Select((MemoryRecallCandidate x) => x?.Block?.HeroId).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)));
		if (list.Count <= 0 || finalCount <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] memory_preprocess_done hero=" + (debugHeroId ?? "") + " mode=empty candidates=" + list.Count + " finalCount=" + finalCount + " selected=0 ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return true;
		}
		if (list.Count <= finalCount)
		{
			selectedIds = list.Select((MemoryRecallCandidate x) => x.DisplayId).ToList();
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] memory_preprocess_done hero=" + (debugHeroId ?? "") + " mode=direct candidates=" + list.Count + " finalCount=" + finalCount + " selected=" + selectedIds.Count + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return true;
		}
		string system = AIConfigHandler.StrictPreprocessJsonSystemPrompt;
		int mode = snapshot.PreprocessMode;
		StringBuilder memoryCandidates = new StringBuilder();
		foreach (MemoryRecallCandidate item in list)
		{
			CompressedMemoryBlock block = item.Block;
			string text = block.GameDate;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = AIConfigHandler.BuildMemoryPreprocessFallbackGameDateForExternal(block.GameDayIndex);
			}
			memoryCandidates.AppendLine(AIConfigHandler.BuildMemoryPreprocessCandidateLineForExternal(item.DisplayId, text, FormatCompressedMemoryAgeSuffix(block, snapshot.GameDay), FormatMemoryHourRange(block.StartHour, block.EndHour), block.RichTitle));
		}
		string user = AIConfigHandler.BuildMemoryPreprocessUserPromptForExternal(mode, finalCount, currentInput, secondaryInput, snapshot.Scene, memoryCandidates.ToString());
		object[] messages = new object[2]
		{
			new
			{
				role = "system",
				content = system
			},
			new
			{
				role = "user",
				content = user
			}
		};
		string content = "";
		Stopwatch apiSw = Stopwatch.StartNew();
		if (mode == 2)
		{
			string memoryError = "";
			bool memoryOk = false;
			Task memoryTask = Task.Run(delegate
			{
				memoryOk = AIConfigHandler.TryCallAuxiliarySimpleDialogueOnceForExternal(messages, 800, 0f, out content, out memoryError);
			});
			try
			{
				memoryTask.Wait();
			}
			catch (Exception ex)
			{
				apiSw.Stop();
				sw.Stop();
				error = ex.Message;
				Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=wait_exception type=" + ex.GetType().Name);
				snapshot.RecordBlockingFailure("Compressed memory preprocessing failed", "Memory preprocessing request exception: " + error, runtimeGeneration);
            snapshot.SetBlockingDisplay("AF_MEMORY_UI_PreprocessFailedTitle", "Compressed memory preprocessing failed",
                MemoryFailureUiText.T("AF_MEMORY_UI_PreprocessRequestException", "Memory preprocessing request exception: {ERROR}", ("ERROR", MemoryFailureUiText.Error(error))), runtimeGeneration);
				return false;
			}
			apiSw.Stop();
			if (!memoryOk)
			{
				error = memoryError;
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=api_failed error=" + (error ?? ""));
				snapshot.RecordBlockingFailure("Compressed memory preprocessing failed", "Memory preprocessing did not succeed: " + error + "\n\nFix the preprocessing API, then retry.", runtimeGeneration);
            snapshot.SetBlockingDisplay("AF_MEMORY_UI_PreprocessFailedTitle", "Compressed memory preprocessing failed",
                MemoryFailureUiText.T("AF_MEMORY_UI_PreprocessUnsuccessful", "Memory preprocessing did not succeed: {ERROR}\n\nFix the preprocessing API, then retry.", ("ERROR", MemoryFailureUiText.Error(error))), runtimeGeneration);
				return false;
			}
		}
		else if (!AIConfigHandler.TryCallAuxiliarySimpleDialogueOnceForExternal(messages, 800, 0f, out content, out error))
		{
			apiSw.Stop();
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=api_failed error=" + (error ?? ""));
			snapshot.RecordBlockingFailure("Compressed memory preprocessing failed", "Memory selection request failed: " + (error ?? "Unknown error") + "\n\nFix the preprocessing API, then retry.", runtimeGeneration);
            snapshot.SetBlockingDisplay("AF_MEMORY_UI_PreprocessFailedTitle", "Compressed memory preprocessing failed",
                MemoryFailureUiText.T("AF_MEMORY_UI_SelectionRequestFailed", "Memory selection request failed: {ERROR}\n\nFix the preprocessing API, then retry.", ("ERROR", MemoryFailureUiText.Error(error))), runtimeGeneration);
			return false;
		}
		else
		{
			apiSw.Stop();
		}
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration) || (snapshot != null && !snapshot.IsCurrent())) { error = "memory_recall_stale_generation"; return false; }
		if (!TryParseMemoryPreprocessIds(content, list.Select((MemoryRecallCandidate x) => x.DisplayId), finalCount, out selectedIds, out var parseError))
		{
			error = BuildMemoryPreprocessFormatError(snapshot, parseError, content);
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=format_error parseError=" + (parseError ?? "") + " responseLen=" + ((content ?? "").Length));
			snapshot.RecordBlockingFailure("Compressed memory preprocessing failed", error + "\n\nFix the preprocessing prompt or API output, then retry.", runtimeGeneration);
            snapshot.SetBlockingDisplay("AF_MEMORY_UI_PreprocessFailedTitle", "Compressed memory preprocessing failed",
                MemoryFailureUiText.T("AF_MEMORY_UI_PreprocessInvalidReply", "Memory preprocessing returned an invalid response: {ERROR}. Return only one JSON object containing rule_codes and memory_ids arrays.\n\nFull model reply:\n{REPLY}\n\nFix the preprocessing prompt or API output, then retry.",
                    ("ERROR", string.IsNullOrWhiteSpace(parseError) ? "unknown" : parseError.Trim()), ("REPLY", string.IsNullOrWhiteSpace(content) ? MemoryFailureUiText.T("AF_MEMORY_UI_NoModelReply", "No model reply was received or could be parsed") : MemoryFailureUiText.Raw(content.Trim()))), runtimeGeneration);
			throw new PreprocessFormatException(error);
		}
		if (selectedIds.Count < finalCount)
		{
			HashSet<int> selectedSet = new HashSet<int>(selectedIds);
			List<int> fill = list.OrderByDescending((MemoryRecallCandidate x) => x.Score).ThenByDescending((MemoryRecallCandidate x) => x.Block.GameDayIndex).Select((MemoryRecallCandidate x) => x.DisplayId).Where((int x) => !selectedSet.Contains(x)).Take(finalCount - selectedIds.Count).ToList();
			selectedIds.AddRange(fill);
		}
		if (selectedIds.Count > finalCount)
		{
			selectedIds = selectedIds.Take(finalCount).ToList();
		}
		sw.Stop();
		Logger.Log("Logic", "[MemoryPerf] memory_preprocess_done hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " selected=" + selectedIds.Count + " promptChars=" + user.Length + " responseLen=" + ((content ?? "").Length) + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		return true;
	}

internal static string BuildCompressedMemoryContextById(string memoryId, string currentInput, string secondaryInput, MemoryRecallRequest snapshot)
	{
		long runtimeGeneration = snapshot?.Generation ?? SaveRuntimeGuard.CaptureGeneration();
        if (snapshot == null || !snapshot.IsCurrent()) return "";
		Stopwatch sw = Stopwatch.StartNew();
		string heroId = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		List<CompressedMemoryBlock> list = snapshot.Blocks?.Where(x => x != null).ToList();
		if (list == null || list.Count <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] compressed_context_done hero=" + heroId + " blocks=0 candidates=0 final=0 chars=0 ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return "";
		}
		int finalCount = snapshot.FinalCount;
        if (finalCount <= 0) return "";
		int candidateLimit = snapshot.CandidateLimit;
		List<MemoryRecallCandidate> candidates;
		Logger.Log("Logic", "[MemoryPerf] compressed_context_start hero=" + heroId + " blocks=" + list.Count + " finalCount=" + finalCount + " candidateLimit=" + candidateLimit + " currentLen=" + ((currentInput ?? "").Length) + " secondaryLen=" + ((secondaryInput ?? "").Length));
		if (list.Count <= finalCount)
		{
			candidates = list.OrderBy((CompressedMemoryBlock x) => x.GameDayIndex).ThenBy((CompressedMemoryBlock x) => x.StartHour).Select((CompressedMemoryBlock x) => new MemoryRecallCandidate
			{
				Block = x,
				Score = 1.0
			}).ToList();
			AssignMemoryCandidateDisplayIds(candidates);
		}
		else
		{
			CompressedMemoryBlock latestBlock = list.OrderByDescending((CompressedMemoryBlock x) => x.GameDayIndex).ThenByDescending((CompressedMemoryBlock x) => x.EndHour).ThenByDescending((CompressedMemoryBlock x) => x.StartHour).ThenByDescending((CompressedMemoryBlock x) => x.CreatedUtcTicks).FirstOrDefault();
			string latestBlockId = (latestBlock?.Id ?? "").Trim();
			int selectableFinalCount = Math.Max(0, finalCount - ((latestBlock != null) ? 1 : 0));
			int selectableCandidateLimit = Math.Max(selectableFinalCount, candidateLimit - ((latestBlock != null) ? 1 : 0));
			List<CompressedMemoryBlock> selectableBlocks = list.Where((CompressedMemoryBlock x) => x != null && (latestBlock == null || !string.Equals((x.Id ?? "").Trim(), latestBlockId, StringComparison.OrdinalIgnoreCase))).ToList();
			candidates = new List<MemoryRecallCandidate>();
			List<DailyMemoryDraft> drafts = snapshot.Drafts;
            bool selectedNormally = true;
            try
            {
                if (selectableFinalCount > 0 && selectableBlocks.Count > 0)
                {
                    selectedNormally = TryBuildMemoryRecallCandidates(selectableBlocks, currentInput, secondaryInput, drafts,
                        selectableCandidateLimit, out candidates, out _, runtimeGeneration, snapshot);
                    if (selectedNormally && candidates.Count > selectableFinalCount)
                    {
                        selectedNormally = TrySelectMemoryIdsWithPreprocess(candidates, selectableFinalCount, currentInput,
                            secondaryInput, out var selectedIds, out _, runtimeGeneration, snapshot);
                        if (selectedNormally)
                        {
                            var selected = new HashSet<int>(selectedIds);
                            candidates = candidates.Where(x => selected.Contains(x.DisplayId)).ToList();
                        }
                    }
                }
            }
            catch (OperationCanceledException) { return ""; }
            catch (Exception ex)
            {
                selectedNormally = false;
                Logger.Log("CompressedMemory", "[WARN] selection failed hero=" + heroId + " type=" + ex.GetType().Name);
            }
            if (!snapshot.IsCurrent()) return "";
            if (!selectedNormally)
            {
                // Local request snapshot only: no extra provider call, retry, or live draft read.
                candidates = selectableBlocks.OrderByDescending(x => x.GameDayIndex).ThenByDescending(x => x.EndHour)
                    .ThenByDescending(x => x.StartHour).ThenByDescending(x => x.CreatedUtcTicks)
                    .GroupBy(x => (x.Id ?? "").Trim(), StringComparer.OrdinalIgnoreCase).Select(x => x.First())
                    .Take(selectableFinalCount).Select(x => new MemoryRecallCandidate { Block = x, Score = 1.0 }).ToList();
                snapshot.RecordBlockingFailure("Compressed memory recall fallback", "Selection failed; recent local memory blocks are retained for this request.", runtimeGeneration);
                snapshot.SetBlockingDisplay("AF_MEMORY_UI_RecallFallbackTitle", "Using recent memories",
                    MemoryFailureUiText.T("AF_MEMORY_UI_RecallFallback", "Memory selection failed. This request uses recent local memory blocks, including the latest block. No additional model request was made."), runtimeGeneration);
                Logger.Log("CompressedMemory", "[MemoryFallback] hero=" + heroId + " blocks=" + (candidates.Count + 1));
            }

			if (latestBlock != null)
			{
				candidates.Add(new MemoryRecallCandidate
				{
					Block = latestBlock,
					Score = double.MaxValue
				});
			}
		}
		candidates = candidates.OrderByDescending(x => x.Block.GameDayIndex).ThenByDescending(x => x.Block.EndHour)
            .ThenByDescending(x => x.Block.StartHour).ThenByDescending(x => x.Block.CreatedUtcTicks)
            .GroupBy(x => (x.Block.Id ?? "").Trim(), StringComparer.OrdinalIgnoreCase).Select(x => x.First()).Take(finalCount).ToList();
        if (candidates.Count <= 0)
        {
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] compressed_context_done hero=" + heroId + " blocks=" + list.Count + " candidates=0 final=0 chars=0 ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_060"));
		int num = 1;
		foreach (MemoryRecallCandidate candidate in candidates.OrderBy((MemoryRecallCandidate x) => x.Block.GameDayIndex).ThenBy((MemoryRecallCandidate x) => x.Block.StartHour))
		{
			CompressedMemoryBlock block = candidate.Block;
			if (block == null)
			{
				continue;
			}
			string text = string.IsNullOrWhiteSpace(block.GameDate) ? (MemoryTextResources.Prompt("MEM_Prompt_061") + block.GameDayIndex + MemoryTextResources.Prompt("MEM_Prompt_062")) : block.GameDate.Trim();
			string text2 = StripMemoryTitleDateTime(block.RichTitle);
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = MemoryTextResources.Prompt("MEM_Prompt_063");
			}
			stringBuilder.AppendLine(num + MemoryTextResources.Prompt("MEM_Prompt_064") + text + FormatCompressedMemoryAgeSuffix(block, snapshot.GameDay) + " " + FormatMemoryHourRange(block.StartHour, block.EndHour) + " " + text2);
			stringBuilder.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_ContentHeader"));
			stringBuilder.AppendLine((block.Summary ?? "").Trim());
			stringBuilder.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_065"));
			if (block.AfefLines != null && block.AfefLines.Count > 0)
			{
				foreach (string item in block.AfefLines)
				{
					if (!string.IsNullOrWhiteSpace(item))
					{
						stringBuilder.AppendLine(FormatPastAfefLineForPrompt(item));
					}
				}
			}
			else
			{
				stringBuilder.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_066"));
			}
			stringBuilder.AppendLine();
			num++;
		}
        if (snapshot == null || !snapshot.IsCurrent()) return "";
		string result = stringBuilder.ToString().TrimEnd();
		sw.Stop();
		Logger.Log("Logic", "[MemoryPerf] compressed_context_done hero=" + heroId + " blocks=" + list.Count + " candidates=" + candidates.Count + " final=" + (num - 1) + " chars=" + result.Length + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		return result;
	}

internal static string FormatPastAfefLineForPrompt(string text)
	{
		string value = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		value = MemorySystemFactProjection.ProjectRecordedFactForPrompt(value);
		if (value.StartsWith(MemoryTextResources.Raw("MEM_Raw_032"), StringComparison.Ordinal) || value.StartsWith(MemoryTextResources.Raw("MEM_Raw_033"), StringComparison.Ordinal))
		{
			return value;
		}
		return MemoryTextResources.Raw("MEM_Raw_032") + value;
	}

internal static bool TryParseMemoryPreprocessIds(string content, IEnumerable<int> allowedIds, int finalCount, out List<int> selectedIds, out string error)
	{
		HashSet<int> allowed = new HashSet<int>(allowedIds ?? Enumerable.Empty<int>());
		HashSet<int> seen = new HashSet<int>();
		selectedIds = new List<int>();
		error = "";
		if (allowed.Count <= 0)
		{
			error = "no_allowed_memory_ids";
			return false;
		}
		if (!AIConfigHandler.TryValidateStrictPreprocessJsonEnvelope(content, requireMemoryIds: true, requireMentionedEntities: false, out var jObject, out error))
		{
			return false;
		}
		JToken token = jObject["memory_ids"];
		if (token == null || token.Type == JTokenType.Null)
		{
			error = "missing_memory_ids";
			return false;
		}
		if (!(token is JArray jArray))
		{
			error = "memory_ids_not_array";
			return false;
		}
		if (jArray.Count <= 0)
		{
			return true;
		}
		foreach (JToken item in jArray)
		{
			if (item == null || item.Type == JTokenType.Null)
			{
				continue;
			}
			string raw = (item?.ToString() ?? "").Trim().TrimStart('#');
			if (!int.TryParse(raw, out var result))
			{
				error = "memory_id_not_integer";
				selectedIds.Clear();
				return false;
			}
			if (!allowed.Contains(result))
			{
				continue;
			}
			if (seen.Add(result))
			{
				selectedIds.Add(result);
			}
		}
		return true;
	}

internal static string FormatMemoryHourRange(int startHour, int endHour)
	{
		startHour = Math.Max(0, Math.Min(23, startHour));
		endHour = Math.Max(0, Math.Min(23, endHour));
		if (startHour == endHour)
		{
			return startHour + MemoryTextResources.Prompt("MEM_Prompt_067");
		}
		return startHour + "-" + endHour + MemoryTextResources.Prompt("MEM_Prompt_067");
	}

internal static string FormatCompressedMemoryAgeSuffix(CompressedMemoryBlock block, int capturedDay)
	{
		if (block == null)
		{
			return "";
		}
		int currentDay = capturedDay;
		int daysAgo = currentDay - block.GameDayIndex;
		if (daysAgo <= 0)
		{
			return MemoryTextResources.Prompt("MEM_Prompt_068");
		}
		return MemoryTextResources.FormatPrompt("AF_MEM_Prompt_AgeSuffix", daysAgo);
	}

internal static string StripMemoryTitleDateTime(string title)
	{
		return MemoryRecordRules.StripMemoryTitleDateTime(title);
	}

internal static string BuildMemoryPreprocessFormatError(MemoryRecallRequest request, string reason, string content)
	{
		string detail = "(API response format error) Invalid compressed-memory preprocessing response: " + (string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim()) + ". Return only one JSON object containing rule_codes and memory_ids arrays.";
		return request.FormatFailureDetail(detail, content);
	}
}

internal sealed class MemoryRecallRequest
{
    internal long Generation;
    internal System.Threading.CancellationToken Cancellation;
    internal Func<bool> IsCurrentOwner;
    internal bool IsCurrent() => !Cancellation.IsCancellationRequested && SaveRuntimeGuard.IsCurrentGeneration(Generation)
        && (IsCurrentOwner == null || IsCurrentOwner());
    internal int GameDay, FinalCount, CandidateLimit, PreprocessMode, BlockCount, DraftCount;
    internal string Scene, Overview, RecallQuery;
    internal List<CompressedMemoryBlock> Blocks;
    internal List<DailyMemoryDraft> Drafts;
    internal Func<string, string, string> FormatFailureDetail;
    internal string BlockingTitle, BlockingMessage;
    internal MemoryFailureDisplay BlockingDisplay;
    internal void SetBlockingDisplay(string titleId, string titleFallback, MemoryFailureUiText message, long generation)
    {
        if (generation != Generation) return;
        BlockingDisplay = new MemoryFailureDisplay(MemoryFailureUiText.T(titleId, titleFallback), message);
    }
    internal void RecordBlockingFailure(string title, string message, long generation)
    {
        if (generation != Generation) return;
        BlockingTitle = title; BlockingMessage = message;
    }
}
