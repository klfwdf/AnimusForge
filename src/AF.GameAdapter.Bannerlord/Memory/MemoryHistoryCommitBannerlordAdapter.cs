using PartyTransferPromptEntry=AnimusForge.MyBehavior.PartyTransferPromptEntry;
using SettlementTransferPromptEntry=AnimusForge.MyBehavior.SettlementTransferPromptEntry;
using System.Diagnostics;
using System.Threading;
using AnimusForge.Refactor.Runtime;
using System.Globalization;
using AnimusForge.Refactor.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using static AnimusForge.MemoryBusinessStateOwner;
using static AnimusForge.HistoryArchiveRecallOwner;
using DialogueDay = AnimusForge.MyBehavior.DialogueDay;
namespace AnimusForge;

// History query/commit boundary. The only saved containers belong to the provided memory owner.
internal sealed class MemoryHistoryCommitBannerlordAdapter
{
 private readonly MemoryBusinessStateOwner _memory;
 private readonly MemoryDailyAppendCapabilities _dailyAppend;
 private readonly Func<MemoryRecoveryStateOwner> _recovery;
 private Dictionary<string,List<DialogueDay>> _dialogueHistory { get => _memory.History; set => _memory.History=value; }
 private Dictionary<string,List<DailyMemoryDraft>> _dailyMemoryDrafts => _memory.Drafts;
 private static string NormalizeMemoryHeroId(string id) => MemoryRecordRules.NormalizeMemoryHeroId(id);
 internal MemoryHistoryCommitBannerlordAdapter(MemoryBusinessStateOwner memory, Func<MemoryRecoveryStateOwner> recovery = null)
 {
  _memory=memory;
  _recovery=recovery;
  _dailyAppend = new MemoryDailyAppendCapabilities(MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory,
   () => (int)CampaignTime.Now.ToDays, () => CampaignTime.Now.ToString(),
   AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.GetCurrentHourOfDaySafeForPrompt,
   () => _memory.GetOrStartActiveNativeConversationMemorySessionId(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory),
   AnimusForge.Refactor.Adapters.SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel,
   MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe,
   PlayerNotorietyBehavior.NoteConversationLineForExternal, LogNonHeroMemoryTrace);
 }

internal MemoryCommitResult CommitDialogueHistoryWithScene(string memoryId, bool isNonHero, string npcName, string playerText, string aiText, string extraFact, int sceneSessionId, int playerTargetAgentIndex, string playerTargetName)
 {
  try
  {
			string normalizedMemoryId = NormalizeMemoryHeroId(memoryId);
			if (string.IsNullOrEmpty(normalizedMemoryId) || isNonHero != IsNonHeroMemoryId(normalizedMemoryId))
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_identity_invalid");
			}
			if (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact))
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_empty_commit");
			}
			Hero hero = isNonHero ? null : (Hero.Find(memoryId.Trim()) ?? MemoryEntityIdentityBannerlordAdapter.FindHeroById(normalizedMemoryId));
			if (!isNonHero && !MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(hero))
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_target_ineligible");
			}
			bool accepted = isNonHero
				? AppendDialogueHistoryById(normalizedMemoryId, npcName, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName)
				: AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName);
			return accepted
				? new MemoryCommitResult(MemoryCommitStatus.Applied)
				: new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_write_unconfirmed");
  }
  catch { return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_append_failed"); }
 }

internal MemoryCommitResult CommitPreparedDialogueHistoryRecovery(InteractionMemoryRecoverySeed seed,string recoveryId,string payloadHash)
 { return _recovery().CommitPrepared(seed,recoveryId,payloadHash,CompleteInitialInteractionMemoryNotorietyOutcome); }

internal List<DialogueDay> LoadDialogueHistory(Hero hero)
	{
		if (hero == null)
		{
			return new List<DialogueDay>();
		}
		return LoadDialogueHistoryById(hero.StringId);
	}

internal List<DialogueDay> LoadDialogueHistoryById(string memoryId)
	{
		if (_dialogueHistory == null)
		{
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
		}
		string stringId = NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrEmpty(stringId))
		{
			return new List<DialogueDay>();
		}
		if (_dialogueHistory.TryGetValue(stringId, out var value) && value != null)
		{
			if (RemoveExpiredSingleUseNpcFactLines(value))
			{
				SaveDialogueHistoryById(stringId, value);
			}
			if (IsNonHeroMemoryId(stringId))
			{
				// Keep verbose-only diagnostics from adding a full history-line count to normal history opens.
				LogNonHeroMemoryTrace(() => "stage=load_dialogue_hit memoryId=" + stringId + " days=" + value.Count + " lines=" + CountDialogueHistoryLines(value));
			}
			return value;
		}
		if (IsNonHeroMemoryId(stringId))
		{
			LogNonHeroMemoryTrace("stage=load_dialogue_miss memoryId=" + stringId + " knownDialogueOwners=" + CountNonHeroDialogueHistoryOwners() + " knownDailyOwners=" + CountNonHeroDailyDraftOwners() + " sample=" + BuildNonHeroMemorySampleIds());
		}
		return new List<DialogueDay>();
	}

internal void SaveDialogueHistoryById(string memoryId, List<DialogueDay> records)
	{
		if (records != null)
		{
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			string stringId = NormalizeMemoryHeroId(memoryId);
			if (!string.IsNullOrEmpty(stringId))
			{
				_dialogueHistory[stringId] = records;
				if (IsNonHeroMemoryId(stringId))
				{
					LogNonHeroMemoryTrace("stage=save_dialogue_in_memory memoryId=" + stringId + " days=" + records.Count + " lines=" + CountDialogueHistoryLines(records));
				}
			}
		}
	}

internal static bool RemoveExpiredSingleUseNpcFactLines(List<DialogueDay> records)
	{
		if (records == null || records.Count == 0)
		{
			return false;
		}
		var flat = DialogueHistoryLedger.Flatten(records, d => d.GameDayIndex, d => d.GameDate, d => d.Lines);
		var kept = DialogueHistoryLedger.ExpireSingleUseFacts(flat, IsSingleUseNpcFactLine, IsMeaningfulDirectConversationLine);
		if (kept == null)
		{
			return false;
		}
		List<DialogueDay> memoryCommitMarkerSource = records.ToList();
		records.Clear();
		records.AddRange(DialogueHistoryLedger.Regroup(kept, (day, date) => new DialogueDay { GameDayIndex = day, GameDate = date }, d => d.GameDayIndex, d => d.Lines));
		MemoryRecoveryStateOwner.CopyMemoryCommitMarkers(memoryCommitMarkerSource, records);
		return true;
	}

internal string GetLatestNpcDialogueUtterance(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		try
		{
			string sceneText = GetLatestSceneNpcDialogueUtteranceFallback(targetAgentIndex);
			if (!string.IsNullOrWhiteSpace(sceneText))
			{
				return sceneText;
			}
		}
		catch
		{
		}
		try
		{
			string nativeText = ShoutBehavior.GetLatestNativeConversationNpcUtteranceForExternal(targetHero, targetCharacter);
			if (!string.IsNullOrWhiteSpace(nativeText))
			{
				return nativeText;
			}
		}
		catch
		{
		}
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (hero == null)
		{
			return "";
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			if (list == null || list.Count == 0)
			{
				return "";
			}
			for (int i = list.Count - 1; i >= 0; i--)
			{
				DialogueDay dialogueDay = list[i];
				if (dialogueDay?.Lines == null || dialogueDay.Lines.Count <= 0)
				{
					continue;
				}
				for (int num = dialogueDay.Lines.Count - 1; num >= 0; num--)
				{
					string text = (dialogueDay.Lines[num] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !IsActiveSceneSessionHistoryLine(text) && !IsSceneShoutObserverHistoryLine(text) && !IsLoreInjectionHistoryLine(text) && !IsSystemFactLine(text) && !IsPlayerTurnStartLine(text))
					{
						return StripSpeakerPrefixForRecall(text);
					}
				}
			}
		}
		catch
		{
		}
		return "";
	}

internal static string GetLatestSceneNpcDialogueUtteranceFallback(int targetAgentIndex)
	{
		if (targetAgentIndex < 0)
		{
			return "";
		}
		string text = ShoutBehavior.GetLatestSceneNpcUtteranceForExternal(targetAgentIndex);
		return string.IsNullOrWhiteSpace(text) ? "" : StripSpeakerPrefixForRecall(text);
	}

internal static bool IsActiveSceneSessionHistoryLine(string line)
	{
		if (Mission.Current == null || !ShoutUtils.IsInValidScene())
		{
			return false;
		}
		if (!DialogueHistoryLedger.TryStripSceneSessionMarker(line, out var _, out var sceneSessionId))
		{
			return false;
		}
		return sceneSessionId == ShoutBehavior.GetCurrentSceneHistorySessionIdForExternal();
	}

internal static bool IsSingleUseNpcFactLine(string line)
	{
		return DialogueHistoryLedger.IsSingleUseNpcFactLine(line, IsFirstMeetingNpcFactBody);
	}

internal static bool IsFirstMeetingNpcFactBody(string text)
	{
		string value = (text ?? "").Trim();
		return value.StartsWith("你第一次见到", StringComparison.Ordinal)
			|| value.StartsWith("你第一次与玩家", StringComparison.Ordinal)
			|| value.StartsWith("你第一次与面前此人", StringComparison.Ordinal)
			|| value.StartsWith("你第一次与面前这个", StringComparison.Ordinal);
	}

internal static int CountDialogueHistoryLines(IEnumerable<DialogueDay> records)
	{
		try
		{
			return (records ?? Enumerable.Empty<DialogueDay>()).Sum((DialogueDay day) => day?.Lines?.Count ?? 0);
		}
		catch
		{
			return 0;
		}
	}

internal int CountNonHeroDialogueHistoryOwners()
	{
		try
		{
			return (_dialogueHistory ?? new Dictionary<string, List<DialogueDay>>()).Keys.Count(IsNonHeroMemoryId);
		}
		catch
		{
			return 0;
		}
	}

internal int CountNonHeroDailyDraftOwners()
	{
		try
		{
			return (_dailyMemoryDrafts ?? new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase)).Keys.Count(IsNonHeroMemoryId);
		}
		catch
		{
			return 0;
		}
	}

internal string BuildNonHeroMemorySampleIds(int maxCount = 5)
	{
		try
		{
			List<string> ids = new List<string>();
			foreach (string key in (_dialogueHistory ?? new Dictionary<string, List<DialogueDay>>()).Keys)
			{
				string id = NormalizeMemoryHeroId(key);
				if (!string.IsNullOrWhiteSpace(id) && IsNonHeroMemoryId(id))
				{
					ids.Add(id);
				}
			}
			foreach (string key in (_dailyMemoryDrafts ?? new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase)).Keys)
			{
				string id = NormalizeMemoryHeroId(key);
				if (!string.IsNullOrWhiteSpace(id) && IsNonHeroMemoryId(id))
				{
					ids.Add(id);
				}
			}
			ids = ids.Distinct(StringComparer.OrdinalIgnoreCase).Take(Math.Max(1, maxCount)).ToList();
			return ids.Count == 0 ? "(none)" : string.Join(",", ids);
		}
		catch
		{
			return "(trace_failed)";
		}
	}

internal static string StripSpeakerPrefixForRecall(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		if (text.StartsWith("- ", StringComparison.Ordinal))
		{
			text = text.Substring(2).TrimStart();
		}
		if (text.StartsWith("—— ", StringComparison.Ordinal))
		{
			return "";
		}
		if (text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
		{
			return text.Substring("[AFEF玩家行为补充]".Length).Trim();
		}
		if (text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			return text.Substring("[AFEF NPC行为补充]".Length).Trim();
		}
		if (AnimusForge.Refactor.Adapters.MemoryRecallInputCaptureAdapter.TryStripPlayerSpeechPrefix(text, out var stripped))
		{
			return stripped;
		}
		text = ShoutUtils.StripNamePrefixedLineSafely(text, 20);
		return text;
	}

internal static void LogNonHeroMemoryTrace(string message)
	{
		try
		{
			if (Logger.IsVerboseModLogicEnabled)
			{
				Logger.Log("Logic", "[NonHeroMemoryTrace] " + (message ?? ""));
			}
		}
		catch
		{
		}
	}

internal static void LogNonHeroMemoryTrace(Func<string> messageFactory)
	{
		try
		{
			if (Logger.IsVerboseModLogicEnabled)
			{
				Logger.Log("Logic", "[NonHeroMemoryTrace] " + (messageFactory?.Invoke() ?? ""));
			}
		}
		catch
		{
		}
	}
internal List<AnimusForgeDialogueHistoryEntry> GetDialogueHistoryEntries(Hero hero, int maxLines)
	{
		return GetDialogueHistoryEntriesById(CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero), maxLines);
	}

internal List<AnimusForgeDialogueHistoryEntry> GetDialogueHistoryEntriesById(string memoryId, int maxLines)
	{
		List<AnimusForgeDialogueHistoryEntry> result = new List<AnimusForgeDialogueHistoryEntry>();
		string normalizedMemoryId = NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(normalizedMemoryId))
		{
			return result;
		}
		try
		{
			List<DialogueDay> records = LoadDialogueHistoryById(normalizedMemoryId);
			if (records == null || records.Count == 0)
			{
				if (IsNonHeroMemoryId(normalizedMemoryId))
				{
					LogNonHeroMemoryTrace("stage=entries_read memoryId=" + normalizedMemoryId + " days=0 lines=0 entries=0 maxLines=" + maxLines);
				}
				return result;
			}
			int limit = Math.Max(1, Math.Min(260, maxLines <= 0 ? 260 : maxLines));
			// The popup displays only the newest bounded window.  Walk the exact chronological ordering backwards,
			// stop once that window is full, then reverse it below so callers retain the previous oldest-to-newest result.
			// Sorting the day list remains necessary because save migration/merge paths do not promise storage order.
			List<DialogueDay> orderedRecords = records.OrderBy((DialogueDay d) => d?.GameDayIndex ?? 0).ToList();
			for (int recordIndex = orderedRecords.Count - 1; recordIndex >= 0 && result.Count < limit; recordIndex--)
			{
				DialogueDay record = orderedRecords[recordIndex];
				if (record?.Lines == null)
				{
					continue;
				}
				// Lines are written in chronological order.  Reverse iteration avoids classifying old history that
				// cannot survive the newest-limit trim, while preserving every accepted line after result.Reverse().
				for (int lineIndex = record.Lines.Count - 1; lineIndex >= 0 && result.Count < limit; lineIndex--)
				{
					string rawLine = record.Lines[lineIndex];
					string line = (rawLine ?? "").Trim();
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					DialogueHistoryLedger.TryStripSceneSessionMarker(line, out line, out var _);
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					ClassifyDialogueHistoryLine(line, out var speaker, out var text, out var kind);
					result.Add(new AnimusForgeDialogueHistoryEntry
					{
						GameDayIndex = record.GameDayIndex,
						GameDate = record.GameDate ?? "",
						Speaker = speaker,
						Text = text,
						Kind = kind,
						MemoryId = normalizedMemoryId,
						LineOrdinal = lineIndex
					});
				}
			}
			// The reverse walk collected the same newest records in newest-to-oldest order; restore the UI/LLM-facing order.
			result.Reverse();
			if (IsNonHeroMemoryId(normalizedMemoryId))
			{
				// Keep the tail-read fast when verbose diagnostics are disabled; the line total is diagnostic-only.
				LogNonHeroMemoryTrace(() => "stage=entries_read memoryId=" + normalizedMemoryId + " days=" + records.Count + " lines=" + CountDialogueHistoryLines(records) + " entries=" + result.Count + " maxLines=" + maxLines);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[ERROR] Export entries failed: " + ex.Message);
		}
		return result;
	}

internal static void ClassifyDialogueHistoryLine(string line, out string speaker, out string text, out string kind)
	{
		string value = (line ?? "").Trim();
		speaker = "记录";
		text = value;
		kind = "system";
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		if (value.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
		{
			speaker = "AFEF 玩家行为";
			text = value.Substring("[AFEF玩家行为补充]".Length).Trim();
			kind = "afef_player";
			return;
		}
		if (value.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			speaker = "AFEF NPC行为";
			text = value.Substring("[AFEF NPC行为补充]".Length).Trim();
			kind = "afef_npc";
			return;
		}
		if (value.StartsWith("[场景喊话]", StringComparison.Ordinal))
		{
			speaker = "场景喊话";
			text = value.Substring("[场景喊话]".Length).Trim();
			kind = "scene";
			return;
		}
		int colonIndex = ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(value);
		if (colonIndex > 0)
		{
			speaker = value.Substring(0, colonIndex).Trim();
			text = value.Substring(colonIndex + 1).Trim();
			kind = ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker) ? "player" : "npc";
			return;
		}
		if (value.IndexOf("玩家", StringComparison.Ordinal) >= 0)
		{
			speaker = "玩家";
			kind = "player";
		}
	}

internal int CountNonHeroDialogueHistoryLines()
	{
		try
		{
			return (_dialogueHistory ?? new Dictionary<string, List<DialogueDay>>()).Where((KeyValuePair<string, List<DialogueDay>> item) => IsNonHeroMemoryId(item.Key)).Sum((KeyValuePair<string, List<DialogueDay>> item) => CountDialogueHistoryLines(item.Value));
		}
		catch
		{
			return 0;
		}
	}

internal void SaveDialogueHistory(Hero hero, List<DialogueDay> records)
	{
		if (hero != null && records != null)
		{
			SaveDialogueHistoryById(hero.StringId, records);
		}
	}

internal bool IsDialogueHistoryPublished(string normalizedMemoryId, List<DialogueDay> records)
	{
		return records != null && _dialogueHistory != null
			&& _dialogueHistory.TryGetValue(normalizedMemoryId, out var published)
			&& ReferenceEquals(published, records);
	}

internal static bool IsFirstMeetingNpcFactLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		if (text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			text = text.Substring("[AFEF NPC行为补充]".Length).Trim();
		}
		return IsFirstMeetingNpcFactBody(text);
	}

internal static bool HasDialogueHistoryLine(List<DialogueDay> records, string targetLine)
	{
		string text = (targetLine ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || records == null || records.Count == 0)
		{
			return false;
		}
		bool isFirstMeetingFact = IsFirstMeetingNpcFactLine(text);
		foreach (DialogueDay record in records)
		{
			if (record?.Lines == null)
			{
				continue;
			}
			foreach (string line in record.Lines)
			{
				string text2 = (line ?? "").Trim();
				DialogueHistoryLedger.TryStripSceneSessionMarker(text2, out text2, out var _);
				if (string.Equals(text2, text, StringComparison.Ordinal))
				{
					return true;
				}
				if (isFirstMeetingFact && IsFirstMeetingNpcFactLine(text2))
				{
					return true;
				}
			}
		}
		return false;
	}
internal bool AppendDailyMemoryLineById(string memoryId, string memoryName, string speaker, string text, bool isAfef, bool isLlmDialogue, int sceneSessionId = -1, int targetAgentIndex = -1, string targetName = null) => _memory.AppendDailyMemoryLineById(memoryId, memoryName, speaker, text, isAfef, isLlmDialogue, sceneSessionId, targetAgentIndex, targetName, _dailyAppend);
internal bool AppendDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId = -1, int playerTargetAgentIndex = -1, string playerTargetName = null)
	{
		if (hero == null || (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact)))
		{
			return false;
		}
		try
		{
			string npcNameForMemory = (hero.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(npcNameForMemory))
			{
				npcNameForMemory = "NPC";
			}
			return AppendDialogueHistoryById(CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero), npcNameForMemory, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 追加失败: " + ex.Message);
			return false;
		}
	}

internal bool AppendDialogueHistoryById(string memoryId, string npcNameForMemory, string playerText, string aiText, string extraFact, int sceneSessionId = -1, int playerTargetAgentIndex = -1, string playerTargetName = null)
	{
		string normalizedMemoryId = NormalizeMemoryHeroId(memoryId);
		if (!MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(normalizedMemoryId) || (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact)))
		{
			if (IsNonHeroMemoryId(normalizedMemoryId))
			{
				LogNonHeroMemoryTrace("stage=append_skip memoryId=" + normalizedMemoryId + " eligible=" + MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(normalizedMemoryId) + " playerLen=" + ((playerText ?? "").Length) + " aiLen=" + ((aiText ?? "").Length) + " factLen=" + ((extraFact ?? "").Length));
			}
			return false;
		}
		try
		{
			npcNameForMemory = (npcNameForMemory ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(npcNameForMemory))
			{
				npcNameForMemory = "NPC";
			}
			bool dailyAccepted = true;
			if (!string.IsNullOrWhiteSpace(playerText))
			{
				Hero memoryHero = MemoryEntityIdentityBannerlordAdapter.FindHeroById(normalizedMemoryId);
				dailyAccepted &= AppendDailyMemoryLineById(normalizedMemoryId, npcNameForMemory, AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(memoryHero), AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerAddressedInputForName(npcNameForMemory, playerText, memoryHero, playerTargetName), isAfef: false, isLlmDialogue: true, sceneSessionId: sceneSessionId, targetAgentIndex: playerTargetAgentIndex, targetName: playerTargetName);
			}
			if (!string.IsNullOrWhiteSpace(extraFact))
			{
				dailyAccepted &= AppendDailyMemoryLineById(normalizedMemoryId, npcNameForMemory, "AFEF", DialogueHistoryLedger.NormalizeAfefFact(extraFact), isAfef: true, isLlmDialogue: false, sceneSessionId: sceneSessionId);
			}
			if (!string.IsNullOrWhiteSpace(aiText))
			{
				dailyAccepted &= AppendDailyMemoryLineById(normalizedMemoryId, npcNameForMemory, npcNameForMemory, DialogueHistoryLedger.NormalizeNpcLine(npcNameForMemory, aiText), isAfef: false, isLlmDialogue: true, sceneSessionId: sceneSessionId);
			}
			List<DialogueDay> list = LoadDialogueHistoryById(normalizedMemoryId);
			int beforeLines = CountDialogueHistoryLines(list);
			int dayIndex = (int)CampaignTime.Now.ToDays;
			string gameDate = CampaignTime.Now.ToString();
			string text = npcNameForMemory;
			DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
			if (dialogueDay == null)
			{
				dialogueDay = new DialogueDay
				{
					GameDayIndex = dayIndex,
					GameDate = gameDate
				};
				list.Add(dialogueDay);
			}
			if (!string.IsNullOrWhiteSpace(playerText))
			{
				string text2 = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerAddressedInputForName(npcNameForMemory, playerText, null, playerTargetName);
				dialogueDay.Lines.Add((sceneSessionId >= 0) ? DialogueHistoryLedger.TagSceneSession(text2, sceneSessionId) : text2);
			}
			if (!string.IsNullOrWhiteSpace(extraFact))
			{
				dialogueDay.Lines.Add(DialogueHistoryLedger.TagSceneSession(DialogueHistoryLedger.NormalizeAfefFact(extraFact), sceneSessionId));
			}
			if (!string.IsNullOrWhiteSpace(aiText))
			{
				dialogueDay.Lines.Add(DialogueHistoryLedger.TagSceneSession(DialogueHistoryLedger.NormalizeNpcLine(text, aiText), sceneSessionId));
			}
			if (!string.IsNullOrWhiteSpace(aiText))
			{
				RemoveExpiredSingleUseNpcFactLines(list);
			}
			var trimmed = DialogueHistoryLedger.TrimToNewest(DialogueHistoryLedger.Flatten(list, d => d.GameDayIndex, d => d.GameDate, d => d.Lines), DialogueHistoryLedger.MaxLines);
			List<DialogueDay> list3 = DialogueHistoryLedger.Regroup(trimmed, (day, date) => new DialogueDay { GameDayIndex = day, GameDate = date }, d => d.GameDayIndex, d => d.Lines);
			MemoryRecoveryStateOwner.CopyMemoryCommitMarkers(list, list3);
			SaveDialogueHistoryById(normalizedMemoryId, list3);
			if (IsNonHeroMemoryId(normalizedMemoryId))
			{
				LogNonHeroMemoryTrace("stage=append_commit memoryId=" + normalizedMemoryId + " memoryName=" + npcNameForMemory + " beforeLines=" + beforeLines + " afterLines=" + CountDialogueHistoryLines(list3) + " days=" + list3.Count + " playerLen=" + ((playerText ?? "").Length) + " aiLen=" + ((aiText ?? "").Length) + " factLen=" + ((extraFact ?? "").Length) + " sceneSession=" + sceneSessionId);
			}
			return dailyAccepted && IsDialogueHistoryPublished(normalizedMemoryId, list3);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 追加失败: " + ex.Message);
			return false;
		}
	}
internal void TryEnsureFirstMeetingNpcFactForConversation(Hero hero)
	{
		GetFirstMeetingNpcFactTextForPromptIfNeededInternal(hero, persistToHistory: true);
	}

internal string GetFirstMeetingNpcFactTextForPromptIfNeededInternal(Hero hero, bool persistToHistory)
	{
		if (hero == null || Hero.MainHero == null)
		{
			return "";
		}
		string text = BuildFirstMeetingNpcFactText(hero);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		List<DialogueDay> list = LoadDialogueHistory(hero);
		if (HasDialogueHistoryLine(list, text) || HasMeaningfulDirectConversationHistory(list) || MemoryBusinessStateOwner.HasMeaningfulConversationHistoryIncludingActiveScene(list))
		{
			return "";
		}
		if (persistToHistory)
		{
			AppendDialogueHistory(hero, null, null, text);
		}
		return text;
	}

internal static string BuildFirstMeetingNpcFactText()
	{
		return BuildFirstMeetingNpcFactText(AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.ResolveCurrentPlayerIdentityObserverForPrompt());
	}

internal static string BuildFirstMeetingNpcFactText(Hero observer)
	{
		AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.TryBuildPlayerPublicDisplayNameForPrompt(observer, out var displayName, out var isCompleteIdentity);
		string text = (displayName ?? "").Trim();
		if (isCompleteIdentity && !string.Equals(text, "玩家", StringComparison.Ordinal))
		{
			return "[AFEF NPC行为补充] 你第一次与玩家（" + text + "）当面交谈；你已经知道他的公开身份和已公开履历，但此前没有直接见过他。";
		}
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "玩家", StringComparison.Ordinal))
		{
			return "[AFEF NPC行为补充] 你第一次与面前这个" + text + "见面；你只知道他的可见外貌称呼，不知道他的真实姓名、背景和来历。";
		}
		return "[AFEF NPC行为补充] 你第一次与玩家见面；除可见外貌与装备外，你不了解他的姓名、背景和来历。";
	}

internal static string NormalizeFirstMeetingNpcFactForPrompt(string line)
	{
		return IsFirstMeetingNpcFactLine(line) ? BuildFirstMeetingNpcFactText() : "";
	}

internal static bool HasMeaningfulDirectConversationHistory(List<DialogueDay> records)
	{
		if (records == null || records.Count == 0)
		{
			return false;
		}
		foreach (DialogueDay record in records)
		{
			if (record?.Lines == null)
			{
				continue;
			}
			foreach (string line in record.Lines)
			{
				string text = (line ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !IsActiveSceneSessionHistoryLine(text) && !IsSystemFactLine(text) && !IsLoreInjectionHistoryLine(text))
				{
					return true;
				}
			}
		}
		return false;
	}


internal static string ResolveInteractionMemoryOriginGameDate(int originDay, int currentDay)
    {
        if (originDay == currentDay) return MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDateTextSafe();
        try
        {
            // CampaignTime.Days is an absolute campaign date in both supported APIs.
            // Do not relabel delayed delivery with the current date or invent a calendar.
            string date = CampaignTime.Days(Math.Max(0, originDay)).ToString();
            if (!string.IsNullOrWhiteSpace(date)) return date.Trim();
        }
        catch { }
        return "第 " + Math.Max(0, originDay).ToString(CultureInfo.InvariantCulture) + " 日";
    }

internal InteractionMemoryRecoverySeed BuildInteractionMemoryRecoverySeed(InteractionMemoryCommit commit,string normalizedMemoryId,bool isNonHero,string npcName,Hero hero) {
 string memoryName=string.IsNullOrWhiteSpace(npcName)?hero?.Name?.ToString()??"NPC":npcName.Trim();if(string.IsNullOrWhiteSpace(memoryName))memoryName="NPC";
 Hero memoryHero=hero??MemoryEntityIdentityBannerlordAdapter.FindHeroById(normalizedMemoryId);string userText=(commit.UserText??string.Empty).Trim();string assistantText=(commit.AssistantText??string.Empty).Trim();string factsText=MemoryRecoverySeedRules.Facts(commit);
 string renderedUser=string.IsNullOrWhiteSpace(userText)?string.Empty:AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerAddressedInputForName(memoryName,userText,memoryHero,commit.TargetName);
 string renderedFact=MemoryRecoverySeedRules.RenderInteractionMemoryFact(factsText);string renderedAssistant=MemoryRecoverySeedRules.RenderInteractionMemoryAssistant(memoryName,assistantText);
 int currentDay=MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();int originDay=MemoryRecoverySeedRules.OriginDay(commit,currentDay);int originHour=MemoryRecoverySeedRules.OriginHour(commit,MemoryRecoverySeedRules.HasDetachedProvenance(commit)?0:AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.GetCurrentHourOfDaySafeForPrompt());
 string originDate=!string.IsNullOrWhiteSpace(commit.CapturedGameDate)?commit.CapturedGameDate:ResolveInteractionMemoryOriginGameDate(originDay,currentDay);string originScene=!string.IsNullOrWhiteSpace(commit.LocationId)?commit.LocationId.Trim():AnimusForge.Refactor.Adapters.SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
 int sceneSessionId=commit.Channel==InteractionChannel.SceneShout?Math.Max(-1,commit.SceneSessionId):-1;int dialogueSessionId=commit.Channel==InteractionChannel.NativeConversation?_memory.GetOrStartActiveNativeConversationMemorySessionId(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory):-1;
 string memorySessionKey=BuildInteractionMemoryRecoverySessionKey(commit,sceneSessionId,dialogueSessionId);
 return MemoryRecoverySeedRules.Build(commit,normalizedMemoryId,isNonHero,new MemoryRecoverySeedCapture{Name=memoryName,User=renderedUser,Fact=renderedFact,Assistant=renderedAssistant,Day=originDay,Date=originDate,Hour=originHour,Scene=originScene,SceneSessionId=sceneSessionId,DialogueSessionId=dialogueSessionId,SessionKey=memorySessionKey,PlayerName=string.IsNullOrWhiteSpace(renderedUser)?string.Empty:AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(memoryHero)});
}

internal string BuildInteractionMemoryRecoverySessionKey(InteractionMemoryCommit commit,int sceneSessionId,int dialogueSessionId) => MemoryRecoverySeedRules.BuildInteractionMemoryRecoverySessionKey(commit,sceneSessionId,dialogueSessionId,_memory.BuildCurrentMemorySessionKey(sceneSessionId,dialogueSessionId));
internal bool TryPrepareExternalDialogueHistoryRecovery(InteractionMemoryCommit commit,bool isNonHero,string npcName,
        out InteractionMemoryRecoverySeed seed,out string recoveryId,out string payloadHash,out string errorCode,out MemoryCommitStatus failureStatus)
    {
        seed=null;recoveryId=string.Empty;payloadHash=string.Empty;errorCode=string.Empty;failureStatus=MemoryCommitStatus.Rejected;
        if (Interlocked.Read(ref _recovery().LoadedGeneration)
            != SaveRuntimeGuard.CurrentGeneration
            || Volatile.Read(ref _recovery().LoadConfirmed) == 0)
        {
            failureStatus = MemoryCommitStatus.Failed;
            errorCode = "memory_recovery_not_activated";
            return false;
        }
        string normalizedMemoryId = MemoryRecordRules.NormalizeMemoryHeroId(commit.SubjectId);
        if (string.IsNullOrEmpty(normalizedMemoryId)
            || isNonHero != IsNonHeroMemoryId(normalizedMemoryId))
        {
            errorCode = "memory_identity_invalid";
            return false;
        }
        if (string.IsNullOrWhiteSpace(commit.UserText)
            && string.IsNullOrWhiteSpace(commit.AssistantText)
            && !(commit.ConfirmedFacts ?? Array.Empty<FactRecord>()).Any(fact =>
                fact != null && !string.IsNullOrWhiteSpace(fact.Text)))
        {
            errorCode = "memory_empty_commit";
            return false;
        }
        Hero hero = isNonHero
            ? null
            : (Hero.Find(commit.SubjectId.Trim()) ?? MemoryEntityIdentityBannerlordAdapter.FindHeroById(normalizedMemoryId));
        if (!isNonHero && !MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(hero))
        {
            errorCode = "memory_target_ineligible";
            return false;
        }
        seed = BuildInteractionMemoryRecoverySeed(
            commit,
            normalizedMemoryId,
            isNonHero,
            npcName,
            hero);
        if (!InteractionMemoryRecoveryLedger.TryBuildRecoveryIdentity(
            seed,
            out recoveryId,
            out payloadHash,
            out errorCode))
        {
            failureStatus = MemoryCommitStatus.Failed;
            return false;
        }
        return true;
    }

internal void CompleteInitialInteractionMemoryNotorietyOutcome(InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash)
    {
        var receipt = InteractionMemoryAuxiliaryCompletionCoordinator.CompleteInitial(seed, recoveryId, payloadHash,
            HasPublishedDailyInteractionMemoryComponent, NotifyInitialMemoryNotorietyComponent);
        if (receipt.HasAttempt)
            Logger.Log("MemoryRecovery", "auxiliary_outcome recovery=" + recoveryId
                + " notoriety_line=confirmed count=" + receipt.Accepted + " duplicate=" + receipt.Duplicate
                + " unavailable=" + receipt.Unavailable + " weekly=not_replayed_or_consumed");
    }

internal static MemoryAuxiliaryReceiptOutcome NotifyInitialMemoryNotorietyComponent(InteractionMemoryRecoverySeed seed, string recoveryId, string payloadHash, string part)
    {
        var status = PlayerNotorietyBehavior.NoteConversationLineRecoverableForExternal(seed.SubjectId, seed.MemorySessionKey,
            seed.RuntimeGeneration, seed.SaveGeneration, seed.OriginGameDay, seed.OriginGameHour, recoveryId, payloadHash, part);
        return status == NotorietyConversationOutcomeOperationStatus.Accepted ? MemoryAuxiliaryReceiptOutcome.Accepted
            : status == NotorietyConversationOutcomeOperationStatus.Duplicate ? MemoryAuxiliaryReceiptOutcome.Duplicate
            : MemoryAuxiliaryReceiptOutcome.Unavailable;
    }

internal bool HasPublishedDailyInteractionMemoryComponent(string subjectId, string recoveryId, string payloadHash, string part) => _recovery().HasPublishedDailyInteractionMemoryComponent(subjectId, recoveryId, payloadHash, part);

internal InteractionMemoryRecoveryLookupStatus GetExternalDialogueHistoryRecoveryStatus(
        string recoveryId,
        string expectedSubjectId,
        string expectedPayloadHash)
    {
        try
        {
            InteractionMemoryRecoveryLedger ledger = _recovery().EnsureInteractionMemoryRecoveryLedger();
            if (ledger.IsDisabled)
            {
                return InteractionMemoryRecoveryLookupStatus.Disabled;
            }
            if (Interlocked.Read(ref _recovery().LoadedGeneration)
                    != SaveRuntimeGuard.CurrentGeneration
                || Volatile.Read(ref _recovery().LoadConfirmed) == 0)
            {
                return InteractionMemoryRecoveryLookupStatus.Unavailable;
            }
            return ledger.GetLookupStatus(
                recoveryId,
                expectedSubjectId,
                expectedPayloadHash);
        }
        catch
        {
            return InteractionMemoryRecoveryLookupStatus.Unavailable;
        }
    }

internal List<ConversationMessage> BuildUncompressedMemoryRoleMessages(Hero hero, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false)
	{
		if (hero == null)
		{
			return new List<ConversationMessage>();
		}
		string heroName = (hero.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(heroName))
		{
			heroName = "NPC";
		}
		return BuildUncompressedMemoryRoleMessagesById(CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero), heroName, targetAgentIndex, includeCurrentActiveSceneSession);
	}

internal List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById(string memoryId, string memoryName, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false)
	{
		Stopwatch sw = Stopwatch.StartNew();
        long generation = SaveRuntimeGuard.CaptureGeneration();
		List<ConversationMessage> result = new List<ConversationMessage>();
		string normalizedMemoryId = NormalizeMemoryHeroId(memoryId);
		if (!MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(normalizedMemoryId))
		{
			return result;
		}
		List<DailyMemoryDraft> drafts = _memory.LoadDrafts(normalizedMemoryId);
		if (drafts == null || drafts.Count <= 0)
		{
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] uncompressed_memory_done hero=" + normalizedMemoryId + " drafts=0 messages=0 agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			if (IsNonHeroMemoryId(normalizedMemoryId))
			{
				LogNonHeroMemoryTrace("stage=uncompressed_build_done memoryId=" + normalizedMemoryId + " drafts=0 draftLines=0 messages=0 agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			}
			return result;
		}
		int currentDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
		int currentSceneSessionId = includeCurrentActiveSceneSession ? -1 : GetCurrentSceneSessionIdForDailyMemorySuppression();
		int currentDialogueSessionId = _memory.GetCurrentNativeConversationMemorySessionIdForSuppression(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory);
		string currentMemorySessionKey = !includeCurrentActiveSceneSession && (currentSceneSessionId >= 0 || currentDialogueSessionId >= 0)
			? _memory.BuildCurrentMemorySessionKey(currentSceneSessionId, currentDialogueSessionId)
			: "";
        var snapshot = new UncompressedMemoryPromptSnapshot {
            CurrentDay = currentDay, CurrentMemorySessionKey = currentMemorySessionKey,
            TargetAgentIndex = targetAgentIndex, MemoryName = string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName.Trim(),
            HistoryLineMinimum = DuelSettings.DailyConversationHistoryLineLimitMin,
            HistoryLineMaximum = DuelSettings.DailyConversationHistoryLineLimitMax
        };
        bool needsCurrentScene = false;
        foreach (DailyMemoryDraft draft in drafts)
        {
            if (draft == null) continue;
            var captured = new UncompressedMemoryDraftSnapshot {
                GameDayIndex = draft.GameDayIndex, HasLlmDialogue = draft.HasLlmDialogue,
                HasCompressedBlock = draft.GameDayIndex != currentDay && _memory.HasCompressedMemoryBlock(normalizedMemoryId, draft.GameDayIndex)
            };
            // Already-compressed prior days cannot contribute lines; do not copy their payload.
            captured.Lines = captured.HasCompressedBlock ? null : draft.Lines?.Select(line => line?.CopyForSummary()).ToList();
            snapshot.Drafts.Add(captured);
            if (captured.Lines != null && captured.Lines.Any(line => line != null && line.GameDayIndex == currentDay
                && !string.IsNullOrWhiteSpace(line.Text)
                && UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(line.Scene))) needsCurrentScene = true;
        }
        if (needsCurrentScene)
        {
            try { snapshot.CurrentScene = AnimusForge.Refactor.Adapters.SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel(); } catch { }
        }
        int historyLineLimit = DuelSettings.GetDailyConversationHistoryLineLimitForExternal();
        snapshot.HistoryLineLimit = historyLineLimit;
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return result;
        result = UncompressedMemoryMessageAssemblyOwner.Assemble(snapshot, out int rawMessageCount);
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return new List<ConversationMessage>();
		sw.Stop();
		Logger.Log("Logic", "[MemoryPerf] uncompressed_memory_done hero=" + normalizedMemoryId + " drafts=" + drafts.Count + " rawMessages=" + rawMessageCount + " messages=" + result.Count + " historyLineLimit=" + historyLineLimit + " agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		if (IsNonHeroMemoryId(normalizedMemoryId))
		{
			LogNonHeroMemoryTrace("stage=uncompressed_build_done memoryId=" + normalizedMemoryId + " drafts=" + drafts.Count + " draftLines=" + CountDailyMemoryDraftLines(drafts) + " rawMessages=" + rawMessageCount + " messages=" + result.Count + " historyLineLimit=" + historyLineLimit + " agent=" + targetAgentIndex + " includeCurrentSession=" + includeCurrentActiveSceneSession + " currentDay=" + currentDay + " suppressedSceneSession=" + currentSceneSessionId + " suppressedDialogueSession=" + currentDialogueSessionId + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
		}
		return result;
	}

internal static int GetCurrentSceneSessionIdForDailyMemorySuppression()
	{
		try
		{
			if (Mission.Current?.Scene == null)
			{
				return -1;
			}
			try
			{
				if (!ShoutUtils.IsInValidScene())
				{
					return -1;
				}
				return ShoutBehavior.GetCurrentSceneHistorySessionIdForExternal();
			}
			catch
			{
				return -1;
			}
		}
		catch
		{
			return -1;
		}
	}

internal int GetLastMeaningfulDialogueDayInternal(Hero hero)
	{
		if (hero == null)
		{
			return -1;
		}
		List<DialogueDay> history = LoadDialogueHistory(hero);
		int latestMeaningfulDay = -1;
		if (history == null)
		{
			return latestMeaningfulDay;
		}
		for (int dayIndex = 0; dayIndex < history.Count; dayIndex++)
		{
			DialogueDay dialogueDay = history[dayIndex];
			if (dialogueDay?.Lines == null || dialogueDay.GameDayIndex < latestMeaningfulDay)
			{
				continue;
			}
			for (int lineIndex = 0; lineIndex < dialogueDay.Lines.Count; lineIndex++)
			{
				if (IsMeaningfulDialogueLineForProactiveLetter(dialogueDay.Lines[lineIndex]))
				{
					latestMeaningfulDay = dialogueDay.GameDayIndex;
					break;
				}
			}
		}
		return latestMeaningfulDay;
	}

internal bool TryGetLatestMeaningfulDialogueInternal(Hero hero, out string stableKey, out string dialogueText, out int day)
	{
		stableKey = "";
		dialogueText = "";
		day = -1;
		if (hero == null)
		{
			return false;
		}
		List<DialogueDay> history = LoadDialogueHistory(hero) ?? new List<DialogueDay>();
		foreach (var record in history
			.Select((value, index) => new { Value = value, Index = index })
			.OrderByDescending(x => x.Value?.GameDayIndex ?? -1)
			.ThenByDescending(x => x.Index))
		{
			DialogueDay dialogueDay = record.Value;
			if (dialogueDay?.Lines == null)
			{
				continue;
			}
			for (int lineIndex = dialogueDay.Lines.Count - 1; lineIndex >= 0; lineIndex--)
			{
				string line = (dialogueDay.Lines[lineIndex] ?? "").Trim();
				if (!IsMeaningfulDialogueLineForProactiveLetter(line))
				{
					continue;
				}
				string heroId = (hero.StringId ?? "hero").Trim();
				stableKey = "dialogue:" + heroId + ":" + dialogueDay.GameDayIndex + ":" + record.Index + ":" + lineIndex;
				dialogueText = line;
				day = dialogueDay.GameDayIndex;
				return true;
			}
		}
		return false;
	}

internal bool TryGetLatestCompressedMemoryInternal(Hero hero, out string stableKey, out string gameDate, out string memoryText, out int day)
	{
		stableKey = "";
		gameDate = "";
		memoryText = "";
		day = -1;
		if (hero == null)
		{
			return false;
		}
		CompressedMemoryBlock block = _memory.LoadBlocks(CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero))
			.Where(x => x != null && (!string.IsNullOrWhiteSpace(x.RichTitle) || !string.IsNullOrWhiteSpace(x.Summary)))
			.OrderByDescending(x => x.GameDayIndex)
			.ThenByDescending(x => x.EndHour)
			.ThenByDescending(x => x.StartHour)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.FirstOrDefault();
		if (block == null)
		{
			return false;
		}
		string title = (block.RichTitle ?? "").Trim();
		string summary = (block.Summary ?? "").Trim();
		memoryText = string.IsNullOrWhiteSpace(title) ? summary : (string.IsNullOrWhiteSpace(summary) ? title : (title + "：" + summary));
		if (string.IsNullOrWhiteSpace(memoryText))
		{
			return false;
		}
		string heroId = (hero.StringId ?? "hero").Trim();
		stableKey = "memory:" + heroId + ":" + ((block.Id ?? MemoryRecordRules.BuildCompressedMemoryBlockId(heroId, block.GameDayIndex)).Trim());
		gameDate = (block.GameDate ?? "").Trim();
		day = block.GameDayIndex;
		return true;
	}

internal bool TryGetLatestNpcRecentActionInternal(Hero hero, out string stableKey, out string actionText, out int day)
	{
		stableKey = "";
		actionText = "";
		day = -1;
		if (hero == null)
		{
			return false;
		}
		string heroKey = CampaignCharacterRecordCaptureAdapter.GetNpcActionHeroKey(hero);
		if (string.IsNullOrWhiteSpace(heroKey)
			|| _memory.RecentActions == null
			|| !_memory.RecentActions.TryGetValue(heroKey, out List<NpcActionEntry> entries)
			|| entries == null)
		{
			return false;
		}
		NpcActionEntry latest = entries
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.Order)
			.ThenByDescending(x => x.Sequence)
			.FirstOrDefault();
		if (latest == null)
		{
			return false;
		}
		stableKey = string.IsNullOrWhiteSpace(latest.StableKey)
			? ((latest.ActionKind ?? "event") + ":" + latest.Day + ":" + latest.Order + ":" + latest.Sequence)
			: latest.StableKey.Trim();
		actionText = latest.Text.Trim();
		day = latest.Day;
		return !string.IsNullOrWhiteSpace(stableKey) && !string.IsNullOrWhiteSpace(actionText);
	}

internal static bool IsMeaningfulDialogueLineForProactiveLetter(string line)
	{
		string text = (line ?? "").Trim();
		return !string.IsNullOrWhiteSpace(text)
			&& !IsActiveSceneSessionHistoryLine(text)
			&& !MemoryBusinessStateOwner.IsSceneShoutObserverHistoryLine(text)
			&& !MemoryBusinessStateOwner.IsLoreInjectionHistoryLine(text)
			&& !HistoryArchiveRecallOwner.IsSystemFactLine(text)
			&& !MemoryBusinessStateOwner.IsPlayerTurnStartLine(text);
	}


internal void AppendLoreToHistory(Hero hero, string loreText)
	{
		if (hero == null)
		{
			return;
		}
		loreText = (loreText ?? "").Trim();
		if (string.IsNullOrEmpty(loreText))
		{
			return;
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			int dayIndex = (int)CampaignTime.Now.ToDays;
			string gameDate = CampaignTime.Now.ToString();
			DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d != null && d.GameDayIndex == dayIndex);
			if (dialogueDay == null)
			{
				dialogueDay = new DialogueDay
				{
					GameDayIndex = dayIndex,
					GameDate = gameDate
				};
				list.Add(dialogueDay);
			}
			if (dialogueDay.Lines == null)
			{
				dialogueDay.Lines = new List<string>();
			}
			string text = loreText.Replace("\r", "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			bool flag = text.IndexOf("【触发相关话题/背景】", StringComparison.OrdinalIgnoreCase) >= 0;
			bool flag2 = text.IndexOf("【玩家触发了（", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("【以下是关于（", StringComparison.OrdinalIgnoreCase) >= 0;
			if (!flag && !flag2)
			{
				string text2 = "";
				try
				{
					text2 = (hero?.Name?.ToString() ?? "").Trim();
				}
				catch
				{
					text2 = "";
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "该NPC";
				}
				string text4 = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(hero);
				if (string.IsNullOrWhiteSpace(text4))
				{
					text4 = "玩家";
				}
				text = "【以下是关于（相关）的背景知识，" + text2 + "可酌情参考，但不要假设" + text4 + "提起过此话题】\n" + text;
			}
			string text3 = text;
			if (dialogueDay.Lines.Count > 0)
			{
				string a = dialogueDay.Lines[dialogueDay.Lines.Count - 1] ?? "";
				if (string.Equals(a, text3, StringComparison.Ordinal))
				{
					return;
				}
			}
			dialogueDay.Lines.Add(text3);
			int num = 0;
			foreach (DialogueDay item in list)
			{
				num += (item?.Lines?.Count).GetValueOrDefault();
			}
			while (num > 260)
			{
				DialogueDay dialogueDay2 = list.FirstOrDefault((DialogueDay d) => d != null && d.Lines != null && d.Lines.Count > 0);
				if (dialogueDay2 == null)
				{
					break;
				}
				dialogueDay2.Lines.RemoveAt(0);
				num--;
				if (dialogueDay2.Lines.Count == 0 && dialogueDay2.GameDayIndex != dayIndex
					&& (dialogueDay2.MemoryCommitMarkers == null || dialogueDay2.MemoryCommitMarkers.Count == 0))
				{
					list.Remove(dialogueDay2);
				}
			}
			SaveDialogueHistory(hero, list);
		}
		catch
		{
		}
	}


internal bool DeleteDialogueHistoryLine(string memoryId, int gameDayIndex, int lineOrdinal, string expectedText, out string status)
	{
		string id = NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(id) || gameDayIndex < 0 || lineOrdinal < 0)
		{
			status = "这条记录不能删除。";
			return false;
		}
		List<DialogueDay> records = LoadDialogueHistoryById(id);
		DialogueDay day = records?.FirstOrDefault(x => x != null && x.GameDayIndex == gameDayIndex);
		// The ordinal comes from the panel's snapshot; re-check the text so a stale list can never delete a different line.
		if (day?.Lines == null || lineOrdinal >= day.Lines.Count || !DisplayTextMatches(day.Lines[lineOrdinal], expectedText))
		{
			status = "记录已变化，请重新打开历史后再删除。";
			return false;
		}
		string rawLine = day.Lines[lineOrdinal];
		string syncKey = MemoryDeveloperEditOwner.NormalizeDialogueHistoryLineForDailyMemorySync(rawLine);
		day.Lines.RemoveAt(lineOrdinal);
		if (day.Lines.Count == 0 && (day.MemoryCommitMarkers == null || day.MemoryCommitMarkers.Count == 0))
		{
			records.Remove(day);
		}
		SaveDialogueHistoryById(id, records);
		bool draftRemoved = RemoveMatchingDraftLine(id, gameDayIndex, syncKey);
		Logger.Log("DialogueHistory", "deleted line memoryId=" + id + " day=" + gameDayIndex + " ordinal=" + lineOrdinal + " draftRemoved=" + draftRemoved);
		status = draftRemoved ? "已删除该条记录。" : "已删除该条记录（该日记忆已压缩，压缩内容不受影响）。";
		return true;
	}

internal static bool DisplayTextMatches(string rawLine, string expectedText)
	{
		string line = (rawLine ?? "").Trim();
		DialogueHistoryLedger.TryStripSceneSessionMarker(line, out line, out var _);
		ClassifyDialogueHistoryLine(line, out var _, out var text, out var _);
		return string.Equals((text ?? "").Trim(), (expectedText ?? "").Trim(), StringComparison.Ordinal);
	}

internal bool RemoveMatchingDraftLine(string memoryId, int gameDayIndex, string syncKey)
	{
		if (string.IsNullOrWhiteSpace(syncKey))
		{
			return false;
		}
		List<DailyMemoryDraft> drafts = _memory.LoadDrafts(memoryId);
		DailyMemoryDraft draft = FindDevDailyMemoryDraft(drafts, gameDayIndex);
		int index = draft?.Lines == null ? -1 : draft.Lines.FindIndex(x => x != null
			&& string.Equals(MemoryDeveloperEditOwner.NormalizeDialogueHistoryLineForDailyMemorySync(x.Text), syncKey, StringComparison.Ordinal));
		if (index < 0)
		{
			return false;
		}
		DailyMemoryLine removed = draft.Lines[index];
		draft.Lines.RemoveAt(index);
		// Sanitize keeps a stale HasLlmDialogue; recompute from what is left.
		draft.HasLlmDialogue = draft.Lines.Any(x => x != null && x.IsLlmDialogue && !x.IsAfef && !string.IsNullOrWhiteSpace(x.Text));
		_memory.SaveDrafts(memoryId, drafts);
		if (FindDevDailyMemoryDraft(_memory.LoadDrafts(memoryId), gameDayIndex) == null)
		{
			_memory.DailyQueue?.RemoveAll(x => x != null && x.GameDayIndex == gameDayIndex
				&& string.Equals(NormalizeMemoryHeroId(x.HeroId), memoryId, StringComparison.OrdinalIgnoreCase));
		}
		// Same lookup as the host's other memory-id resolution: indexed Find first, full scan only on a case mismatch.
		Hero hero = CampaignCharacterRecordCaptureAdapter.ResolveMemoryHero(memoryId);
		if (hero != null)
		{
			ShoutBehavior.SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(hero, hero.CharacterObject, hero.Name?.ToString(), gameDayIndex,
				BuildNativeConversationHistoryEntriesForDailyMemoryEdit(hero, new[] { removed }),
				new List<AnimusForgeDialogueHistoryEntry>(), "dialogueui_delete");
		}
		return true;
	}

internal static DailyMemoryDraft FindDevDailyMemoryDraft(List<DailyMemoryDraft> drafts, int dayIndex)
	{
		return (drafts ?? new List<DailyMemoryDraft>()).FirstOrDefault((DailyMemoryDraft x) => x != null && x.GameDayIndex == dayIndex);
	}

internal static List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEntriesForDailyMemoryEdit(Hero npc, IEnumerable<DailyMemoryLine> lines)
	{
		List<AnimusForgeDialogueHistoryEntry> result = new List<AnimusForgeDialogueHistoryEntry>();
		string npcName = (npc?.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(npcName))
		{
			npcName = "NPC";
		}
		foreach (DailyMemoryLine line in lines ?? Enumerable.Empty<DailyMemoryLine>())
		{
            int currentDay = 0;
            string currentScene = "";
            if (line != null && !string.IsNullOrWhiteSpace(line.Text)
                && UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(line.Scene))
            {
                try
                {
                    currentDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
                    if (line.GameDayIndex == currentDay) currentScene = AnimusForge.Refactor.Adapters.SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
                }
                catch { }
            }
            ConversationMessage message = UncompressedMemoryMessageAssemblyOwner.BuildUncompressedMemoryConversationMessage(
                line, npcName, line?.TargetAgentIndex ?? -1, currentDay, currentScene);
			if (message == null || string.IsNullOrWhiteSpace(message.Content))
			{
				continue;
			}
			string role = (message.Role ?? "").Trim();
			string kind = string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)
				? "fact"
				: (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase) ? "player" : "npc");
			result.Add(new AnimusForgeDialogueHistoryEntry
			{
				GameDayIndex = message.GameDayIndex,
				GameDate = message.GameDate ?? "",
				GameHour = message.GameHour,
				Scene = message.Scene ?? "",
				Speaker = message.SpeakerName ?? "",
				TargetAgentIndex = message.TargetAgentIndex,
				TargetName = message.TargetName ?? "",
				Text = message.Content ?? "",
				Kind = kind
			});
		}
		return result;
	}


internal void MarkWeeklyMemoryMaterialTriggerInternal(Hero targetHero, string nonHeroMemoryId, string npcName, string normalizedTagText, int sceneSessionId, int nativeDialogueSessionId, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<PartyTransferPromptEntry> partyTransferTroopOptions, List<PartyTransferPromptEntry> partyTransferPrisonerOptions, List<SettlementTransferPromptEntry> settlementTransferNpcOptions, bool suppressImplicitDialogueSession = false, List<PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null)
	{
		string memoryId = !string.IsNullOrWhiteSpace(nonHeroMemoryId) ? MemoryRecordRules.NormalizeMemoryHeroId(nonHeroMemoryId) : CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(targetHero);
		if (!MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(memoryId))
		{
			return;
		}
		string tagText = WeeklyMemoryMaterialPolicy.NormalizeWeeklyMemoryMaterialTagText(normalizedTagText);
		List<string> tags = WeeklyMemoryMaterialPolicy.ExtractWeeklyMemoryMaterialTags(tagText);
		if (tags.Count == 0)
		{
			return;
		}
		int day = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
		string gameDate = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDateTextSafe();
		List<DailyMemoryDraft> drafts = _memory.LoadDrafts(memoryId);
		DailyMemoryDraft draft = drafts.FirstOrDefault((DailyMemoryDraft x) => x != null && x.GameDayIndex == day);
        var valuePort = WeeklyMaterialValueBannerlordAdapter.Capture(targetHero, rewardOptions,
            partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions,
            partyTransferAllTroopOptions, partyTransferAllPrisonerOptions);
		WeeklyMemoryMaterialEvaluation evaluation = WeeklyMemoryMaterialPolicy.EvaluateWeeklyMemoryMaterialTags(tags, tag => WeeklyMemoryMaterialValuePolicy.EstimateWeeklyMemoryMaterialTagValue(tag, valuePort));
		WeeklyMemoryMaterialPolicy.TryApplyPlayerTransferredValueToWeeklyMemoryMaterialEvaluation(evaluation, tags, draft, npcName, sceneSessionId, nativeDialogueSessionId);
		if (evaluation == null || !evaluation.Eligible)
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] tags_not_eligible memory=" + memoryId + " value=" + (evaluation?.EstimatedValueDenars ?? 0L) + " threshold=" + WeeklyMemoryMaterialPolicy.ValueThresholdDenars + " tags=" + string.Join("|", tags));
			return;
		}
		if (!MemoryEntityIdentityBannerlordAdapter.ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var footholdKingdomId, out var footholdSettlementId))
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] foothold_kingdom_missing memory=" + memoryId + " tags=" + string.Join("|", tags));
			return;
		}
		if (!suppressImplicitDialogueSession && sceneSessionId < 0 && nativeDialogueSessionId < 0)
		{
			nativeDialogueSessionId = _memory.GetOrStartActiveNativeConversationMemorySessionId(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory);
		}
		WeeklyMemoryMaterialTrigger trigger = WeeklyMemoryMaterialPolicy.CreateTrigger(memoryId,
            npcName, day, gameDate, sceneSessionId, nativeDialogueSessionId, targetAgentIndex,
            footholdKingdomId, footholdSettlementId, tagText, evaluation, DateTime.UtcNow.Ticks);
        bool attached = _memory.StageOrAttachWeeklyTrigger(trigger, day);
        Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial] trigger_" + (attached ? "attached" : "pending")
            + " memory=" + memoryId + " day=" + day + " kingdom=" + footholdKingdomId
            + " value=" + evaluation.EstimatedValueDenars + " tags=" + string.Join("|", evaluation.Tags));
	}

internal static void MarkWeeklyMemoryMaterialTriggerForScene(Hero targetHero, CharacterObject targetCharacter, string npcName, string normalizedTags, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<MyBehavior.PartyTransferPromptEntry> partyTransferTroopOptions, List<MyBehavior.PartyTransferPromptEntry> partyTransferPrisonerOptions, List<MyBehavior.SettlementTransferPromptEntry> settlementTransferNpcOptions, bool forceLooseSession = false, List<MyBehavior.PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(normalizedTags))
			{
				return;
			}
			Hero memoryHero = targetHero ?? targetCharacter?.HeroObject;
			string nonHeroMemoryId = "";
			string memoryName = (npcName ?? "").Trim();
			if (memoryHero == null)
			{
				if (AnimusForge.Refactor.Adapters.SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(null, targetHero, targetCharacter, targetAgentIndex, out var resolvedMemoryId, out var resolvedMemoryName))
				{
					nonHeroMemoryId = resolvedMemoryId;
					if (string.IsNullOrWhiteSpace(memoryName))
					{
						memoryName = resolvedMemoryName;
					}
				}
			}
			if (memoryHero == null && string.IsNullOrWhiteSpace(nonHeroMemoryId))
			{
				return;
			}
			int sceneSessionId = forceLooseSession ? -1 : ShoutBehavior.TryGetCurrentSceneHistorySessionIdForHistoryPersistence();
			MyBehavior.MarkWeeklyMemoryMaterialTriggerWithAllSnapshotsForExternal(memoryHero, nonHeroMemoryId, string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName, normalizedTags, sceneSessionId, -1, targetAgentIndex, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, suppressImplicitDialogueSession: forceLooseSession, partyTransferAllTroopOptions: partyTransferAllTroopOptions, partyTransferAllPrisonerOptions: partyTransferAllPrisonerOptions);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WeeklyMemoryMaterial] mark failed: " + ex.Message);
		}
	}
}
