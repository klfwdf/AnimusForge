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
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration)) { error = "memory_recall_stale_generation"; return false; }
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
				error = "本地 ONNX embedding 不可用，无法对超过候选上限的压缩记忆执行富标题 RAG。";
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] recall_candidates_failed hero=" + (debugHeroId ?? "") + " reason=query_embedding_unavailable queryLen=" + ((text ?? "").Length) + " queryMs=" + Math.Round(querySw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
				snapshot.RecordBlockingFailure("压缩记忆召回被阻塞", error + "\n\n请修复 embedding 模型或降低记忆块数量后重试。系统不会静默改用日期或全文兜底。", runtimeGeneration);
				return false;
			}
			querySw.Stop();
			List<MemoryRecallCandidate> list2 = new List<MemoryRecallCandidate>();
			Stopwatch titleSw = Stopwatch.StartNew();
			int scanned = 0;
			int titleEmbeddingFailed = 0;
			foreach (CompressedMemoryBlock block in list)
			{
                if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration)) { error = "memory_recall_stale_generation"; return false; }
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
				error = "压缩记忆富标题 embedding 全部失败。";
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] recall_candidates_failed hero=" + (debugHeroId ?? "") + " reason=title_embedding_empty scanned=" + scanned + " failed=" + titleEmbeddingFailed + " queryMs=" + Math.Round(querySw.Elapsed.TotalMilliseconds, 2) + " titleMs=" + Math.Round(titleSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
				snapshot.RecordBlockingFailure("压缩记忆召回被阻塞", error + "\n\n请修复 embedding 模型后重试。", runtimeGeneration);
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
			snapshot.RecordBlockingFailure("压缩记忆召回异常", error, runtimeGeneration);
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
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration)) { error = "memory_recall_stale_generation"; return false; }
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
				memoryOk = AIConfigHandler.TryCallAuxiliarySimpleDialogue(messages, 800, 0f, out content, out memoryError);
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
				snapshot.RecordBlockingFailure("压缩记忆前处理失败", "记忆前处理请求异常：" + error, runtimeGeneration);
				return false;
			}
			apiSw.Stop();
			if (!memoryOk)
			{
				error = memoryError;
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=api_failed error=" + (error ?? ""));
				snapshot.RecordBlockingFailure("压缩记忆前处理失败", "记忆前处理没有成功：" + error + "\n\n请修复前处理 API 后重试。", runtimeGeneration);
				return false;
			}
		}
		else if (!AIConfigHandler.TryCallAuxiliarySimpleDialogue(messages, 800, 0f, out content, out error))
		{
			apiSw.Stop();
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=api_failed error=" + (error ?? ""));
			snapshot.RecordBlockingFailure("压缩记忆前处理失败", "记忆筛选请求失败：" + (error ?? "未知错误") + "\n\n请修复前处理 API 后重试。", runtimeGeneration);
			return false;
		}
		else
		{
			apiSw.Stop();
		}
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration)) { error = "memory_recall_stale_generation"; return false; }
		if (!TryParseMemoryPreprocessIds(content, list.Select((MemoryRecallCandidate x) => x.DisplayId), finalCount, out selectedIds, out var parseError))
		{
			error = BuildMemoryPreprocessFormatError(snapshot, parseError, content);
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] memory_preprocess_failed hero=" + (debugHeroId ?? "") + " mode=" + mode + " candidates=" + list.Count + " finalCount=" + finalCount + " promptChars=" + user.Length + " apiMs=" + Math.Round(apiSw.Elapsed.TotalMilliseconds, 2) + " totalMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " reason=format_error parseError=" + (parseError ?? "") + " responseLen=" + ((content ?? "").Length));
			snapshot.RecordBlockingFailure("压缩记忆前处理失败", error + "\n\n请修复前处理提示词或 API 输出后重试。", runtimeGeneration);
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
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration)) return "";
		Stopwatch sw = Stopwatch.StartNew();
		string heroId = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		List<CompressedMemoryBlock> list = snapshot.Blocks;
		if (list == null || list.Count <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] compressed_context_done hero=" + heroId + " blocks=0 candidates=0 final=0 chars=0 ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return "";
		}
		int finalCount = snapshot.FinalCount;
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
			if (selectableFinalCount > 0 && selectableBlocks.Count > 0)
			{
				if (!TryBuildMemoryRecallCandidates( selectableBlocks, currentInput, secondaryInput, drafts, selectableCandidateLimit, out candidates, out var error, runtimeGeneration, snapshot))
				{
					Logger.Log("CompressedMemory", "[ERROR] recall failed hero=" + heroId + " error=" + error);
					sw.Stop();
					Logger.Log("Logic", "[MemoryPerf] compressed_context_failed hero=" + heroId + " reason=recall_failed blocks=" + list.Count + " selectable=" + selectableBlocks.Count + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
					return "";
				}
				if (candidates.Count > selectableFinalCount)
				{
					if (!TrySelectMemoryIdsWithPreprocess(candidates, selectableFinalCount, currentInput, secondaryInput, out var selectedIds, out var error2, runtimeGeneration, snapshot))
					{
						Logger.Log("CompressedMemory", "[ERROR] preprocess failed hero=" + heroId + " error=" + error2);
						sw.Stop();
						Logger.Log("Logic", "[MemoryPerf] compressed_context_failed hero=" + heroId + " reason=preprocess_failed blocks=" + list.Count + " candidates=" + candidates.Count + " selectableFinal=" + selectableFinalCount + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
						return "";
					}
					HashSet<int> selected = new HashSet<int>(selectedIds);
					candidates = candidates.Where((MemoryRecallCandidate x) => selected.Contains(x.DisplayId)).OrderBy((MemoryRecallCandidate x) => x.Block.GameDayIndex).ThenBy((MemoryRecallCandidate x) => x.Block.StartHour).ToList();
				}
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
		if (candidates.Count <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] compressed_context_done hero=" + heroId + " blocks=" + list.Count + " candidates=0 final=0 chars=0 ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("你想起之前的对话与互动");
		int num = 1;
		foreach (MemoryRecallCandidate candidate in candidates.OrderBy((MemoryRecallCandidate x) => x.Block.GameDayIndex).ThenBy((MemoryRecallCandidate x) => x.Block.StartHour))
		{
			CompressedMemoryBlock block = candidate.Block;
			if (block == null)
			{
				continue;
			}
			string text = string.IsNullOrWhiteSpace(block.GameDate) ? ("第" + block.GameDayIndex + "日") : block.GameDate.Trim();
			string text2 = StripMemoryTitleDateTime(block.RichTitle);
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "往日对话记忆";
			}
			stringBuilder.AppendLine(num + "#标题：" + text + FormatCompressedMemoryAgeSuffix(block, snapshot.GameDay) + " " + FormatMemoryHourRange(block.StartHour, block.EndHour) + " " + text2);
			stringBuilder.AppendLine("内容：");
			stringBuilder.AppendLine((block.Summary ?? "").Trim());
			stringBuilder.AppendLine("AFEF行为补充：");
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
				stringBuilder.AppendLine("无");
			}
			stringBuilder.AppendLine();
			num++;
		}
        if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration)) return "";
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
		if (value.StartsWith("【过往行为】", StringComparison.Ordinal) || value.StartsWith("【当下行为】", StringComparison.Ordinal))
		{
			return value;
		}
		return "【过往行为】" + value;
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
			return startHour + "时";
		}
		return startHour + "-" + endHour + "时";
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
			return "（今天）";
		}
		return "（" + daysAgo + "天前）";
	}

internal static string StripMemoryTitleDateTime(string title)
	{
		return MemoryRecordRules.StripMemoryTitleDateTime(title);
	}

internal static string BuildMemoryPreprocessFormatError(MemoryRecallRequest request, string reason, string content)
	{
		string detail = "（API响应格式错误）压缩记忆前处理返回格式错误：" + (string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim()) + "。必须只输出一个 JSON 对象，并包含 rule_codes 和 memory_ids 数组。";
		return request.FormatFailureDetail(detail, content);
	}
}

internal sealed class MemoryRecallRequest
{
    internal long Generation;
    internal int GameDay, FinalCount, CandidateLimit, PreprocessMode, BlockCount, DraftCount;
    internal string Scene, Overview, RecallQuery;
    internal List<CompressedMemoryBlock> Blocks;
    internal List<DailyMemoryDraft> Drafts;
    internal Func<string, string, string> FormatFailureDetail;
    internal string BlockingTitle, BlockingMessage;
    internal void RecordBlockingFailure(string title, string message, long generation)
    {
        if (generation != Generation) return;
        BlockingTitle = title; BlockingMessage = message;
    }
}
