using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200006C RID: 108
	[NullableContext(1)]
	[Nullable(0)]
	internal static class StringReferenceExtensions
	{
		// Token: 0x060005DF RID: 1503 RVA: 0x00019390 File Offset: 0x00017590
		public static int IndexOf(this StringReference s, char c, int startIndex, int length)
		{
			int num = Array.IndexOf<char>(s.Chars, c, s.StartIndex + startIndex, length);
			if (num == -1)
			{
				return -1;
			}
			return num - s.StartIndex;
		}

		// Token: 0x060005E0 RID: 1504 RVA: 0x000193C4 File Offset: 0x000175C4
		public static bool StartsWith(this StringReference s, string text)
		{
			if (text.Length > s.Length)
			{
				return false;
			}
			char[] chars = s.Chars;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] != chars[i + s.StartIndex])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060005E1 RID: 1505 RVA: 0x00019414 File Offset: 0x00017614
		public static bool EndsWith(this StringReference s, string text)
		{
			if (text.Length > s.Length)
			{
				return false;
			}
			char[] chars = s.Chars;
			int num = s.StartIndex + s.Length - text.Length;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] != chars[i + num])
				{
					return false;
				}
			}
			return true;
		}
	}
}
