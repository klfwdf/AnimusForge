using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library;

public static class SRTHelper
{
	public static class SrtParser
	{
		private static readonly string[] _delimiters = new string[3] { "-->", "- >", "->" };

		public static List<SubtitleItem> ParseStream(Stream subtitleStream, Encoding encoding)
		{
			if (!subtitleStream.CanRead || !subtitleStream.CanSeek)
			{
				throw new ArgumentException("Given subtitle file is not readable.");
			}
			subtitleStream.Position = 0L;
			StreamReader reader = new StreamReader(subtitleStream, encoding, detectEncodingFromByteOrderMarks: true);
			List<SubtitleItem> list = new List<SubtitleItem>();
			List<string> list2 = GetSrtSubTitleParts(reader).ToList();
			if (list2.Count > 0)
			{
				foreach (string item in list2)
				{
					List<string> list3 = (from s in item.Split(new string[1] { Environment.NewLine }, StringSplitOptions.None)
						select s.Trim() into l
						where !string.IsNullOrEmpty(l)
						select l).ToList();
					SubtitleItem subtitleItem = new SubtitleItem();
					foreach (string item2 in list3)
					{
						if (subtitleItem.StartTime == 0 && subtitleItem.EndTime == 0)
						{
							if (TryParseTimecodeLine(item2, out var startTc, out var endTc))
							{
								subtitleItem.StartTime = startTc;
								subtitleItem.EndTime = endTc;
							}
						}
						else
						{
							subtitleItem.Lines.Add(item2);
						}
					}
					if ((subtitleItem.StartTime != 0 || subtitleItem.EndTime != 0) && subtitleItem.Lines.Count > 0)
					{
						list.Add(subtitleItem);
					}
				}
				if (list.Count > 0)
				{
					return list;
				}
				throw new ArgumentException("Stream is not in a valid Srt format");
			}
			throw new FormatException("Parsing as srt returned no srt part.");
		}

		private static IEnumerable<string> GetSrtSubTitleParts(TextReader reader)
		{
			MBStringBuilder sb = default(MBStringBuilder);
			sb.Initialize(16, "GetSrtSubTitleParts");
			string text;
			while ((text = reader.ReadLine()) != null)
			{
				if (string.IsNullOrEmpty(text.Trim()))
				{
					string text2 = sb.ToStringAndRelease().TrimEnd(Array.Empty<char>());
					if (!string.IsNullOrEmpty(text2))
					{
						yield return text2;
					}
					sb.Initialize(16, "GetSrtSubTitleParts");
				}
				else
				{
					sb.AppendLine(text);
				}
			}
			if (sb.Length > 0)
			{
				yield return sb.ToStringAndRelease();
			}
			else
			{
				sb.Release();
			}
		}

		private static bool TryParseTimecodeLine(string line, out int startTc, out int endTc)
		{
			string[] array = line.Split(_delimiters, StringSplitOptions.None);
			if (array.Length != 2)
			{
				startTc = -1;
				endTc = -1;
				return false;
			}
			startTc = ParseSrtTimecode(array[0]);
			endTc = ParseSrtTimecode(array[1]);
			return true;
		}

		private static int ParseSrtTimecode(string s)
		{
			Match match = Regex.Match(s, "[0-9]+:[0-9]+:[0-9]+([,\\.][0-9]+)?");
			if (match.Success)
			{
				s = match.Value;
				if (TimeSpan.TryParse(s.Replace(',', '.'), out var result))
				{
					return (int)result.TotalMilliseconds;
				}
			}
			return -1;
		}
	}

	public static class StreamHelpers
	{
		public static Stream CopyStream(Stream inputStream)
		{
			MemoryStream memoryStream = new MemoryStream();
			int num;
			do
			{
				byte[] buffer = new byte[1024];
				num = inputStream.Read(buffer, 0, 1024);
				memoryStream.Write(buffer, 0, num);
			}
			while (inputStream.CanRead && num > 0);
			memoryStream.ToArray();
			return memoryStream;
		}
	}

	public class SubtitleItem
	{
		public int StartTime { get; set; }

		public int EndTime { get; set; }

		public List<string> Lines { get; set; }

		public SubtitleItem()
		{
			Lines = new List<string>();
		}

		public override string ToString()
		{
			TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, StartTime);
			TimeSpan timeSpan2 = new TimeSpan(0, 0, 0, 0, EndTime);
			return string.Format("{0} --> {1}: {2}", timeSpan.ToString("G"), timeSpan2.ToString("G"), string.Join(Environment.NewLine, Lines));
		}
	}
}
