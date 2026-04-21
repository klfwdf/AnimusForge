using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.Library;

namespace AnimusForge;

public class FloatingTextManager
{
	private sealed class CandidateNameEntry
	{
		public NpcDataPacket Npc;

		public string NormName;
	}

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
			if (MissionView != null)
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
				string text3 = StripSpeakerPrefix(text, npcDataPacket);
				if (!string.Equals(text3, text, StringComparison.Ordinal))
				{
					string text4 = CleanContent(text3);
					if (!string.IsNullOrWhiteSpace(text4))
					{
						_pendingContent.Append(text4);
						EmitPartialUpdate(_pendingSpeaker, text4);
						EmitLine(_pendingSpeaker, text4);
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
			string text5 = CleanContent(text2);
			_pendingSpeaker = npcDataPacket;
			_pendingContent.Clear();
			if (!string.IsNullOrWhiteSpace(text5))
			{
				_pendingContent.Append(text5);
				EmitPartialUpdate(_pendingSpeaker, text5);
				EmitLine(_pendingSpeaker, text5);
				_pendingSpeaker = null;
				_pendingContent.Clear();
			}
		}
		else if (_pendingSpeaker != null)
		{
			string text6 = text;
			if (_pendingContent.Length > 0)
			{
				_pendingContent.Append(" ");
				text6 = " " + text6;
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
		string[] array2 = array;
		foreach (string text3 in array2)
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

	private static string StripSpeakerPrefix(string rawLine, NpcDataPacket npc)
	{
		string text = (rawLine ?? "").Trim();
		foreach (string candidateNameVariant in GetCandidateNameVariants(npc))
		{
			string text2 = StripSpeakerPrefix(text, candidateNameVariant);
			if (!string.Equals(text2, text, StringComparison.Ordinal))
			{
				return text2;
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
		int index = (value % matches.Count + matches.Count) % matches.Count;
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
		string text = NormalizeSpeakerToken(name);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		List<CandidateNameEntry> list = new List<CandidateNameEntry>();
		foreach (NpcDataPacket currentCandidate in _currentCandidates)
		{
			if (currentCandidate == null)
			{
				continue;
			}
			foreach (string candidateNameVariant in GetCandidateNameVariants(currentCandidate))
			{
				string text2 = NormalizeSpeakerToken(candidateNameVariant);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.Add(new CandidateNameEntry
					{
						Npc = currentCandidate,
						NormName = text2
					});
				}
			}
		}
		List<NpcDataPacket> list2 = new List<NpcDataPacket>();
		foreach (CandidateNameEntry item in list)
		{
			if (string.Equals(item.NormName, text, StringComparison.OrdinalIgnoreCase))
			{
				list2.Add(item.Npc);
			}
		}
		if (list2.Count > 0)
		{
			return PickWithRoundRobin(list2, "exact:" + text, advanceRoundRobin);
		}
		int num = -1;
		string text3 = "";
		List<NpcDataPacket> list3 = new List<NpcDataPacket>();
		foreach (CandidateNameEntry item2 in list)
		{
			if (text.StartsWith(item2.NormName, StringComparison.OrdinalIgnoreCase))
			{
				int length = item2.NormName.Length;
				if (length > num)
				{
					num = length;
					text3 = item2.NormName;
					list3.Clear();
					list3.Add(item2.Npc);
				}
				else if (length == num)
				{
					list3.Add(item2.Npc);
				}
			}
		}
		if (list3.Count > 0)
		{
			return PickWithRoundRobin(list3, "prefix:" + text3, advanceRoundRobin);
		}
		int num2 = -1;
		string text4 = "";
		List<NpcDataPacket> list4 = new List<NpcDataPacket>();
		foreach (CandidateNameEntry item3 in list)
		{
			if (item3.NormName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(item3.NormName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				int length2 = item3.NormName.Length;
				if (length2 > num2)
				{
					num2 = length2;
					text4 = item3.NormName;
					list4.Clear();
					list4.Add(item3.Npc);
				}
				else if (length2 == num2)
				{
					list4.Add(item3.Npc);
				}
			}
		}
		if (list4.Count > 0)
		{
			return PickWithRoundRobin(list4, "contains:" + text4, advanceRoundRobin);
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
			from alias in GetCandidateNameVariants(n)
			let normalizedAlias = (alias ?? "").Trim()
			where !string.IsNullOrWhiteSpace(normalizedAlias)
			select normalizedAlias).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
			string[] array3 = array2;
			foreach (string text2 in array3)
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
		int[] array4 = sortedSet.ToArray();
		for (int num4 = 0; num4 < array4.Length; num4++)
		{
			int num5 = array4[num4];
			int num6 = ((num4 + 1 < array4.Length) ? array4[num4 + 1] : text.Length);
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

	private static IEnumerable<string> GetCandidateNameVariants(NpcDataPacket npc)
	{
		if (npc == null)
		{
			yield break;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array = new string[3] { npc.Name, npc.PromptGivenName, npc.PromptDisplayName };
		for (int i = 0; i < array.Length; i++)
		{
			string text = (array[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
			{
				yield return text;
			}
		}
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
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		InformationManager.DisplayMessage(new InformationMessage("[" + npcName + "] " + content, new Color(1f, 0.8f, 0.2f, 1f)));
	}

	public static void DisplaySystemMessage(string text, Color color)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		InformationManager.DisplayMessage(new InformationMessage(text, color));
	}
}
