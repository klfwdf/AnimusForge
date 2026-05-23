using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library
{
	// Token: 0x02000090 RID: 144
	public static class SRTHelper
	{
		// Token: 0x020000E4 RID: 228
		public static class SrtParser
		{
			// Token: 0x060007A0 RID: 1952 RVA: 0x000192C8 File Offset: 0x000174C8
			public static List<SRTHelper.SubtitleItem> ParseStream(Stream subtitleStream, Encoding encoding)
			{
				if (!subtitleStream.CanRead || !subtitleStream.CanSeek)
				{
					throw new ArgumentException("Given subtitle file is not readable.");
				}
				subtitleStream.Position = 0L;
				TextReader reader = new StreamReader(subtitleStream, encoding, true);
				List<SRTHelper.SubtitleItem> list = new List<SRTHelper.SubtitleItem>();
				List<string> list2 = SRTHelper.SrtParser.GetSrtSubTitleParts(reader).ToList<string>();
				if (list2.Count <= 0)
				{
					throw new FormatException("Parsing as srt returned no srt part.");
				}
				foreach (string text in list2)
				{
					List<string> list3 = (from s in text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
						select s.Trim() into l
						where !string.IsNullOrEmpty(l)
						select l).ToList<string>();
					SRTHelper.SubtitleItem subtitleItem = new SRTHelper.SubtitleItem();
					foreach (string text2 in list3)
					{
						if (subtitleItem.StartTime == 0 && subtitleItem.EndTime == 0)
						{
							int startTime;
							int endTime;
							if (SRTHelper.SrtParser.TryParseTimecodeLine(text2, out startTime, out endTime))
							{
								subtitleItem.StartTime = startTime;
								subtitleItem.EndTime = endTime;
							}
						}
						else
						{
							subtitleItem.Lines.Add(text2);
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

			// Token: 0x060007A1 RID: 1953 RVA: 0x00019494 File Offset: 0x00017694
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
						sb.AppendLine<string>(text);
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
				yield break;
			}

			// Token: 0x060007A2 RID: 1954 RVA: 0x000194A4 File Offset: 0x000176A4
			private static bool TryParseTimecodeLine(string line, out int startTc, out int endTc)
			{
				string[] array = line.Split(SRTHelper.SrtParser._delimiters, StringSplitOptions.None);
				if (array.Length != 2)
				{
					startTc = -1;
					endTc = -1;
					return false;
				}
				startTc = SRTHelper.SrtParser.ParseSrtTimecode(array[0]);
				endTc = SRTHelper.SrtParser.ParseSrtTimecode(array[1]);
				return true;
			}

			// Token: 0x060007A3 RID: 1955 RVA: 0x000194E4 File Offset: 0x000176E4
			private static int ParseSrtTimecode(string s)
			{
				Match match = Regex.Match(s, "[0-9]+:[0-9]+:[0-9]+([,\\.][0-9]+)?");
				if (match.Success)
				{
					s = match.Value;
					TimeSpan timeSpan;
					if (TimeSpan.TryParse(s.Replace(',', '.'), out timeSpan))
					{
						return (int)timeSpan.TotalMilliseconds;
					}
				}
				return -1;
			}

			// Token: 0x040002ED RID: 749
			private static readonly string[] _delimiters = new string[] { "-->", "- >", "->" };
		}

		// Token: 0x020000E5 RID: 229
		public static class StreamHelpers
		{
			// Token: 0x060007A5 RID: 1957 RVA: 0x00019550 File Offset: 0x00017750
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

		// Token: 0x020000E6 RID: 230
		public class SubtitleItem
		{
			// Token: 0x17000105 RID: 261
			// (get) Token: 0x060007A6 RID: 1958 RVA: 0x00019599 File Offset: 0x00017799
			// (set) Token: 0x060007A7 RID: 1959 RVA: 0x000195A1 File Offset: 0x000177A1
			public int StartTime { get; set; }

			// Token: 0x17000106 RID: 262
			// (get) Token: 0x060007A8 RID: 1960 RVA: 0x000195AA File Offset: 0x000177AA
			// (set) Token: 0x060007A9 RID: 1961 RVA: 0x000195B2 File Offset: 0x000177B2
			public int EndTime { get; set; }

			// Token: 0x17000107 RID: 263
			// (get) Token: 0x060007AA RID: 1962 RVA: 0x000195BB File Offset: 0x000177BB
			// (set) Token: 0x060007AB RID: 1963 RVA: 0x000195C3 File Offset: 0x000177C3
			public List<string> Lines { get; set; }

			// Token: 0x060007AC RID: 1964 RVA: 0x000195CC File Offset: 0x000177CC
			public SubtitleItem()
			{
				this.Lines = new List<string>();
			}

			// Token: 0x060007AD RID: 1965 RVA: 0x000195E0 File Offset: 0x000177E0
			public override string ToString()
			{
				TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, this.StartTime);
				TimeSpan timeSpan2 = new TimeSpan(0, 0, 0, 0, this.EndTime);
				return string.Format("{0} --> {1}: {2}", timeSpan.ToString("G"), timeSpan2.ToString("G"), string.Join(Environment.NewLine, this.Lines));
			}
		}
	}
}
