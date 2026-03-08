using System;

namespace StbSharp
{
	// Token: 0x02000002 RID: 2
	public struct FontBakerCharacterRange
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002058 File Offset: 0x00000258
		public char Start { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002061 File Offset: 0x00000261
		// (set) Token: 0x06000004 RID: 4 RVA: 0x00002069 File Offset: 0x00000269
		public char End { get; private set; }

		// Token: 0x06000005 RID: 5 RVA: 0x00002072 File Offset: 0x00000272
		public FontBakerCharacterRange(char start, char end)
		{
			this.Start = start;
			this.End = end;
		}

		// Token: 0x04000001 RID: 1
		public static readonly FontBakerCharacterRange BasicLatin = new FontBakerCharacterRange(' ', '\u007f');

		// Token: 0x04000002 RID: 2
		public static readonly FontBakerCharacterRange Latin1Supplement = new FontBakerCharacterRange('\u00a0', 'ÿ');

		// Token: 0x04000003 RID: 3
		public static readonly FontBakerCharacterRange LatinExtendedA = new FontBakerCharacterRange('Ā', 'ſ');

		// Token: 0x04000004 RID: 4
		public static readonly FontBakerCharacterRange LatinExtendedB = new FontBakerCharacterRange('ƀ', 'ɏ');

		// Token: 0x04000005 RID: 5
		public static readonly FontBakerCharacterRange Cyrillic = new FontBakerCharacterRange('Ѐ', 'ӿ');

		// Token: 0x04000006 RID: 6
		public static readonly FontBakerCharacterRange CyrillicSupplement = new FontBakerCharacterRange('Ԁ', 'ԯ');

		// Token: 0x04000007 RID: 7
		public static readonly FontBakerCharacterRange Hiragana = new FontBakerCharacterRange('\u3040', 'ゟ');

		// Token: 0x04000008 RID: 8
		public static readonly FontBakerCharacterRange Katakana = new FontBakerCharacterRange('゠', 'ヿ');
	}
}
