using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.Library;

namespace Voxforge;

public class FloatingTextManager
{
	private static FloatingTextManager _instance;

	private readonly StringBuilder _streamBuffer = new StringBuilder();

	private int _processedLineCount = 0;

	private List<NpcDataPacket> _currentCandidates;

	private readonly List<(NpcDataPacket npc, string content)> _parsedResponses = new List<(NpcDataPacket, string)>();

	public Action<NpcDataPacket, string> OnNewLineReady;

	public Action<NpcDataPacket, string> OnPartialLineUpdated;

	public FloatingTextMissionView MissionView;

	private readonly object _lock = new object();

	private NpcDataPacket _pendingSpeaker = null;

	private readonly StringBuilder _pendingContent = new StringBuilder();

	private readonly Dictionary<string, int> _speakerRoundRobin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	public static FloatingTextManager Instance => _instance ?? (_instance = new FloatingTextManager());

	public void BeginStream(List<NpcDataPacket> candidates)
	{
		lock (_lock)
		{
			_streamBuffer.Clear();
			_processedLineCount = 0;
			_currentCandidates = candidates ?? new List<NpcDataPacket>();
			_parsedResponses.Clear();
			_pendingSpeaker = null;
			_pendingContent.Clear();
			if (MissionView == null)
			{
			}
		}
	}

	public void AppendChunk(string chunk)
	{
		if (string.IsNullOrEmpty(chunk))
		{
			return;
		}
		lock (_lock)
		{
			_streamBuffer.Append(chunk);
			TryParseNewLines(flush: false);
		}
	}

	public List<(NpcDataPacket npc, string content)> EndStream(string fullText = null)
	{
		lock (_lock)
		{
			if (!string.IsNullOrEmpty(fullText))
			{
				_parsedResponses.Clear();
				_pendingSpeaker = null;
				_pendingContent.Clear();
				_speakerRoundRobin.Clear();
				_streamBuffer.Clear();
				_streamBuffer.Append(fullText);
				_processedLineCount = 0;
			}
			TryParseNewLines(flush: true);
			return new List<(NpcDataPacket, string)>(_parsedResponses);
		}
	}

	private void TryParseNewLines(bool flush)
	{
		if (_currentCandidates == null || _currentCandidates.Count == 0)
		{
			return;
		}
		string text = _streamBuffer.ToString();
		string[] array = text.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		int num = (flush ? array.Length : ((array.Length != 0) ? (array.Length - 1) : 0));
		for (int i = _processedLineCount; i < num; i++)
		{
			string text2 = array[i].Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			foreach (string item in SplitCombinedSpeakerLine(text2))
			{
				ProcessSingleLine(item);
			}
		}
		if (!flush && array.Length != 0 && num < array.Length)
		{
			string rawLine = array[array.Length - 1];
			ProcessPartialLine(rawLine);
		}
		if (flush)
		{
			FlushPending();
		}
		_processedLineCount = num;
	}

	private void ProcessPartialLine(string rawLine)
	{
		if (string.IsNullOrWhiteSpace(rawLine))
		{
			return;
		}
		int num = rawLine.IndexOf(':');
		if (num == -1)
		{
			num = rawLine.IndexOf('：');
		}
		NpcDataPacket npcDataPacket = null;
		string text = "";
		if (num > 0 && num < 30)
		{
			string name = rawLine.Substring(0, num).Trim();
			text = rawLine.Substring(num + 1);
			npcDataPacket = ResolveCandidate(name, advanceRoundRobin: false);
		}
		if (npcDataPacket != null)
		{
			string text2 = CleanContent(text);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				EmitPartialUpdate(npcDataPacket, text2);
			}
		}
		else if (_pendingSpeaker != null)
		{
			string text3 = _pendingContent.ToString() + ((_pendingContent.Length > 0) ? " " : "") + rawLine;
			string content = CleanContent(text3);
			EmitPartialUpdate(_pendingSpeaker, content);
		}
	}

	private void ProcessSingleLine(string rawLine)
	{
		string text = rawLine;
		if (text.StartsWith("名字:") || text.StartsWith("Name:") || text.StartsWith("角色:"))
		{
			int num = text.IndexOf(':');
			if (num >= 0)
			{
				text = text.Substring(num + 1).Trim();
			}
		}
		int num2 = text.IndexOf(':');
		if (num2 == -1)
		{
			num2 = text.IndexOf('：');
		}
		NpcDataPacket npcDataPacket = null;
		string text2 = "";
		if (num2 > 0 && num2 < 30)
		{
			string name = text.Substring(0, num2).Trim();
			text2 = text.Substring(num2 + 1).Trim();
			npcDataPacket = ResolveCandidate(name, advanceRoundRobin: true);
		}
		if (npcDataPacket == null)
		{
			npcDataPacket = ResolveCandidate(text, advanceRoundRobin: true);
			if (npcDataPacket != null)
			{
				FlushPending();
				_pendingSpeaker = npcDataPacket;
				_pendingContent.Clear();

				// 兼容 AI 输出“角色名 内容”（缺少冒号）的情况，避免整行被误判或丢弃。
				string text5 = StripSpeakerPrefix(text, npcDataPacket.Name);
				if (!string.Equals(text5, text, StringComparison.Ordinal))
				{
					string text6 = CleanContent(text5);
					if (!string.IsNullOrWhiteSpace(text6))
					{
						_pendingContent.Append(text6);
						EmitPartialUpdate(_pendingSpeaker, text6);
						EmitLine(_pendingSpeaker, text6);
						_pendingSpeaker = null;
						_pendingContent.Clear();
					}
				}
				return;
			}
		}
		if (npcDataPacket != null)
		{
			FlushPending();
			string text3 = CleanContent(text2);
			_pendingSpeaker = npcDataPacket;
			_pendingContent.Clear();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				_pendingContent.Append(text3);
				EmitPartialUpdate(_pendingSpeaker, text3);
				EmitLine(_pendingSpeaker, text3);
				_pendingSpeaker = null;
				_pendingContent.Clear();
			}
		}
		else if (_pendingSpeaker != null)
		{
			string text4 = text;
			if (_pendingContent.Length > 0)
			{
				_pendingContent.Append(" ");
				text4 = " " + text4;
			}
			_pendingContent.Append(text);
			string content = CleanContent(_pendingContent.ToString());
			EmitPartialUpdate(_pendingSpeaker, content);
		}
	}

	private void FlushPending()
	{
		if (_pendingSpeaker != null && _pendingContent.Length > 0)
		{
			string text = CleanContent(_pendingContent.ToString());
			if (!string.IsNullOrWhiteSpace(text))
			{
				EmitLine(_pendingSpeaker, text);
			}
		}
		_pendingSpeaker = null;
		_pendingContent.Clear();
	}

	private void EmitLine(NpcDataPacket npc, string content)
	{
		_parsedResponses.Add((npc, content));
		try
		{
			OnNewLineReady?.Invoke(npc, content);
		}
		catch
		{
		}
	}

	private void EmitPartialUpdate(NpcDataPacket npc, string content)
	{
		try
		{
			OnPartialLineUpdated?.Invoke(npc, content);
		}
		catch
		{
		}
	}

	private static string NormalizeSpeakerToken(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return "";
		}
		string text = raw.Trim();
		text = text.Trim('[', ']', '【', '】', '(', ')', '（', '）', '"', '\'', '“', '”');
		text = Regex.Replace(text, "\\s+", " ").Trim();
		text = text.TrimEnd('：', ':', '，', ',', '。', '.', '！', '!', '？', '?', '；', ';', '、', '-', '_');
		return text.Trim();
	}

	private static string StripSpeakerPrefix(string rawLine, string speakerName)
	{
		string text = (rawLine ?? "").Trim();
		string text2 = NormalizeSpeakerToken(speakerName);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		string[] array = new string[3]
		{
			text2,
			"[" + text2 + "]",
			"【" + text2 + "】"
		};
		foreach (string text3 in array)
		{
			if (text.StartsWith(text3, StringComparison.OrdinalIgnoreCase))
			{
				string text4 = text.Substring(text3.Length).TrimStart();
				text4 = text4.TrimStart(':', '：', '-', '—', '–', ',', '，', '。', '!', '！', '?', '？', '、');
				return text4.TrimStart();
			}
		}
		return text;
	}

	private NpcDataPacket PickWithRoundRobin(List<NpcDataPacket> matches, string key, bool advanceRoundRobin)
	{
		if (matches == null || matches.Count == 0)
		{
			return null;
		}
		if (matches.Count == 1)
		{
			return matches[0];
		}
		int value = 0;
		_speakerRoundRobin.TryGetValue(key ?? "", out value);
		int index = ((value % matches.Count) + matches.Count) % matches.Count;
		if (advanceRoundRobin)
		{
			_speakerRoundRobin[key ?? ""] = (value + 1) % matches.Count;
		}
		return matches[index];
	}

	private NpcDataPacket ResolveCandidate(string name, bool advanceRoundRobin)
	{
		if (string.IsNullOrWhiteSpace(name) || _currentCandidates == null)
		{
			return null;
		}

		string trimmed = NormalizeSpeakerToken(name);
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return null;
		}

		List<CandidateNameEntry> normalizedCandidates = new List<CandidateNameEntry>();
		foreach (NpcDataPacket currentCandidate in _currentCandidates)
		{
			if (currentCandidate == null)
			{
				continue;
			}
			string normName = NormalizeSpeakerToken(currentCandidate.Name ?? "");
			if (string.IsNullOrWhiteSpace(normName))
			{
				continue;
			}
			normalizedCandidates.Add(new CandidateNameEntry
			{
				Npc = currentCandidate,
				NormName = normName
			});
		}

		List<NpcDataPacket> exactMatches = new List<NpcDataPacket>();
		foreach (CandidateNameEntry normalizedCandidate in normalizedCandidates)
		{
			if (string.Equals(normalizedCandidate.NormName, trimmed, StringComparison.OrdinalIgnoreCase))
			{
				exactMatches.Add(normalizedCandidate.Npc);
			}
		}
		if (exactMatches.Count > 0)
		{
			return PickWithRoundRobin(exactMatches, "exact:" + trimmed, advanceRoundRobin);
		}

		int bestPrefixLen = -1;
		string bestPrefixKey = "";
		List<NpcDataPacket> prefixMatches = new List<NpcDataPacket>();
		foreach (CandidateNameEntry normalizedCandidate2 in normalizedCandidates)
		{
			if (!trimmed.StartsWith(normalizedCandidate2.NormName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int normLen = normalizedCandidate2.NormName.Length;
			if (normLen > bestPrefixLen)
			{
				bestPrefixLen = normLen;
				bestPrefixKey = normalizedCandidate2.NormName;
				prefixMatches.Clear();
				prefixMatches.Add(normalizedCandidate2.Npc);
			}
			else if (normLen == bestPrefixLen)
			{
				prefixMatches.Add(normalizedCandidate2.Npc);
			}
		}
		if (prefixMatches.Count > 0)
		{
			return PickWithRoundRobin(prefixMatches, "prefix:" + bestPrefixKey, advanceRoundRobin);
		}

		int bestContainsLen = -1;
		string bestContainsKey = "";
		List<NpcDataPacket> containsMatches = new List<NpcDataPacket>();
		foreach (CandidateNameEntry normalizedCandidate3 in normalizedCandidates)
		{
			bool contains = normalizedCandidate3.NormName.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0
				|| trimmed.IndexOf(normalizedCandidate3.NormName, StringComparison.OrdinalIgnoreCase) >= 0;
			if (!contains)
			{
				continue;
			}
			int normLen2 = normalizedCandidate3.NormName.Length;
			if (normLen2 > bestContainsLen)
			{
				bestContainsLen = normLen2;
				bestContainsKey = normalizedCandidate3.NormName;
				containsMatches.Clear();
				containsMatches.Add(normalizedCandidate3.Npc);
			}
			else if (normLen2 == bestContainsLen)
			{
				containsMatches.Add(normalizedCandidate3.Npc);
			}
		}
		if (containsMatches.Count > 0)
		{
			return PickWithRoundRobin(containsMatches, "contains:" + bestContainsKey, advanceRoundRobin);
		}

		return null;
	}

	private List<string> SplitCombinedSpeakerLine(string rawLine)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(rawLine))
		{
			return list;
		}
		string text = rawLine.Trim();
		if (_currentCandidates == null || _currentCandidates.Count == 0)
		{
			list.Add(text);
			return list;
		}
		List<string> list2 = (from n in _currentCandidates
			where n != null
			select (n.Name ?? "").Trim() into n
			where !string.IsNullOrWhiteSpace(n)
			select n).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list2.Count == 0)
		{
			list.Add(text);
			return list;
		}
		SortedSet<int> sortedSet = new SortedSet<int> { 0 };
		foreach (string item in list2)
		{
			string[] array = new string[4]
			{
				item + ":",
				item + "：",
				"[" + item + "]:",
				"[" + item + "]："
			};
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				int num2 = 0;
				while (num2 < text.Length)
				{
					int num3 = text.IndexOf(text2, num2, StringComparison.OrdinalIgnoreCase);
					if (num3 < 0)
					{
						break;
					}
					if (num3 > 0 && IsSpeakerBoundaryBefore(text, num3))
					{
						sortedSet.Add(num3);
					}
					num2 = num3 + text2.Length;
				}
			}
		}
		if (sortedSet.Count <= 1)
		{
			list.Add(text);
			return list;
		}
		int[] array3 = sortedSet.ToArray();
		for (int num4 = 0; num4 < array3.Length; num4++)
		{
			int num5 = array3[num4];
			int num6 = ((num4 + 1 < array3.Length) ? array3[num4 + 1] : text.Length);
			if (num6 > num5)
			{
				string text3 = text.Substring(num5, num6 - num5).Trim();
				if (!string.IsNullOrWhiteSpace(text3))
				{
					list.Add(text3);
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(text);
		}
		return list;
	}

	private static bool IsSpeakerBoundaryBefore(string line, int startIndex)
	{
		if (string.IsNullOrEmpty(line))
		{
			return false;
		}
		if (startIndex <= 0)
		{
			return true;
		}
		if (startIndex > line.Length)
		{
			return false;
		}
		char c = line[startIndex - 1];
		if (char.IsWhiteSpace(c))
		{
			return true;
		}
		return "\"'“”‘’([【{<《「『，,。.!！?？;；:：、|/\\-—–".IndexOf(c) >= 0;
	}

	private sealed class CandidateNameEntry
	{
		public NpcDataPacket Npc;

		public string NormName;
	}

	private static string CleanContent(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = Regex.Replace(text, "\\（.*?\\）", "");
		text = Regex.Replace(text, "\\(.*?\\)", "");
		text = Regex.Replace(text, "\\*.*?\\*", "");
		return text.Trim();
	}

	public static void DisplayNpcMessage(string npcName, string content)
	{
		InformationManager.DisplayMessage(new InformationMessage("[" + npcName + "] " + content, new Color(1f, 0.8f, 0.2f)));
	}

	public static void DisplaySystemMessage(string text, Color color)
	{
		InformationManager.DisplayMessage(new InformationMessage(text, color));
	}
}
