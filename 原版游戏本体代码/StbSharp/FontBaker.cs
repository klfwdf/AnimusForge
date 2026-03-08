using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x02000003 RID: 3
	public class FontBaker
	{
		// Token: 0x06000007 RID: 7 RVA: 0x0000212C File Offset: 0x0000032C
		public unsafe void Begin(byte[] pixels, int pw, int ph)
		{
			if (this._beginCalled)
			{
				throw new Exception("Call End() before calling Begin again");
			}
			this._beginCalled = true;
			this.result.Clear();
			this._handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
			fixed (StbTrueType.stbtt_pack_context* ptr = &this.pc)
			{
				StbTrueType.stbtt_PackBegin(ptr, (byte*)this._handle.AddrOfPinnedObject().ToPointer(), pw, ph, pw, 1, null);
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002198 File Offset: 0x00000398
		public unsafe void Add(byte[] ttf, float pixel_height, IEnumerable<FontBakerCharacterRange> ranges)
		{
			fixed (StbTrueType.stbtt_pack_context* ptr = &this.pc)
			{
				StbTrueType.stbtt_pack_context* spc = ptr;
				fixed (byte[] array = ttf)
				{
					byte* fontdata;
					if (ttf == null || array.Length == 0)
					{
						fontdata = null;
					}
					else
					{
						fontdata = &array[0];
					}
					foreach (FontBakerCharacterRange fontBakerCharacterRange in ranges)
					{
						if (fontBakerCharacterRange.Start <= fontBakerCharacterRange.End)
						{
							StbTrueType.stbtt_packedchar[] array2 = new StbTrueType.stbtt_packedchar[(int)(fontBakerCharacterRange.End - fontBakerCharacterRange.Start + '\u0001')];
							try
							{
								StbTrueType.stbtt_packedchar[] array3;
								StbTrueType.stbtt_packedchar* chardata_for_range;
								if ((array3 = array2) == null || array3.Length == 0)
								{
									chardata_for_range = null;
								}
								else
								{
									chardata_for_range = &array3[0];
								}
								StbTrueType.stbtt_PackFontRange(spc, fontdata, 0, pixel_height, (int)fontBakerCharacterRange.Start, (int)(fontBakerCharacterRange.End - fontBakerCharacterRange.Start + '\u0001'), chardata_for_range);
							}
							finally
							{
								StbTrueType.stbtt_packedchar[] array3 = null;
							}
							for (int i = 0; i < array2.Length; i++)
							{
								this.result[(char)(i + (int)fontBakerCharacterRange.Start)] = array2[i];
							}
						}
					}
				}
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000022C0 File Offset: 0x000004C0
		public unsafe Dictionary<char, StbTrueType.stbtt_packedchar> End()
		{
			fixed (StbTrueType.stbtt_pack_context* ptr = &this.pc)
			{
				StbTrueType.stbtt_PackEnd(ptr);
			}
			return this.result;
		}

		// Token: 0x0400000B RID: 11
		private StbTrueType.stbtt_pack_context pc;

		// Token: 0x0400000C RID: 12
		private bool _beginCalled;

		// Token: 0x0400000D RID: 13
		private GCHandle _handle;

		// Token: 0x0400000E RID: 14
		private readonly Dictionary<char, StbTrueType.stbtt_packedchar> result = new Dictionary<char, StbTrueType.stbtt_packedchar>();
	}
}
