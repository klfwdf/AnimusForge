using System;

namespace StbSharp
{
	// Token: 0x0200000B RID: 11
	public static class StbTrueType
	{
		// Token: 0x06000132 RID: 306 RVA: 0x00012470 File Offset: 0x00010670
		public unsafe static uint stbtt__find_table(byte* data, uint fontstart, string tag)
		{
			int num = (int)StbTrueType.ttUSHORT(data + fontstart + 4);
			uint num2 = fontstart + 12U;
			for (int i = 0; i < num; i++)
			{
				uint num3 = (uint)((ulong)num2 + (ulong)((long)(16 * i)));
				if ((char)data[num3] == tag[0] && (char)(data + num3)[1] == tag[1] && (char)(data + num3)[2] == tag[2] && (char)(data + num3)[3] == tag[3])
				{
					return StbTrueType.ttULONG(data + num3 + 8);
				}
			}
			return 0U;
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000124F0 File Offset: 0x000106F0
		public unsafe static bool stbtt_BakeFontBitmap(byte[] ttf, int offset, float pixel_height, byte[] pixels, int pw, int ph, int first_char, int num_chars, StbTrueType.stbtt_bakedchar[] chardata)
		{
			byte* data;
			if (ttf == null || ttf.Length == 0)
			{
				data = null;
			}
			else
			{
				data = &ttf[0];
			}
			byte* pixels2;
			if (pixels == null || pixels.Length == 0)
			{
				pixels2 = null;
			}
			else
			{
				pixels2 = &pixels[0];
			}
			StbTrueType.stbtt_bakedchar* chardata2;
			if (chardata == null || chardata.Length == 0)
			{
				chardata2 = null;
			}
			else
			{
				chardata2 = &chardata[0];
			}
			return StbTrueType.stbtt_BakeFontBitmap(data, offset, pixel_height, pixels2, pw, ph, first_char, num_chars, chardata2) != 0;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00012564 File Offset: 0x00010764
		public unsafe static byte stbtt__buf_get8(StbTrueType.stbtt__buf* b)
		{
			if (b->cursor >= b->size)
			{
				return 0;
			}
			int data = b->data;
			int cursor = b->cursor;
			b->cursor = cursor + 1;
			return *(data + cursor);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00012597 File Offset: 0x00010797
		public unsafe static byte stbtt__buf_peek8(StbTrueType.stbtt__buf* b)
		{
			if (b->cursor >= b->size)
			{
				return 0;
			}
			return b->data[b->cursor];
		}

		// Token: 0x06000136 RID: 310 RVA: 0x000125B7 File Offset: 0x000107B7
		public unsafe static void stbtt__buf_seek(StbTrueType.stbtt__buf* b, int o)
		{
			b->cursor = ((o > b->size || o < 0) ? b->size : o);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x000125D5 File Offset: 0x000107D5
		public unsafe static void stbtt__buf_skip(StbTrueType.stbtt__buf* b, int o)
		{
			StbTrueType.stbtt__buf_seek(b, b->cursor + o);
		}

		// Token: 0x06000138 RID: 312 RVA: 0x000125E8 File Offset: 0x000107E8
		public unsafe static uint stbtt__buf_get(StbTrueType.stbtt__buf* b, int n)
		{
			uint num = 0U;
			for (int i = 0; i < n; i++)
			{
				num = (num << 8) | (uint)StbTrueType.stbtt__buf_get8(b);
			}
			return num;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00012610 File Offset: 0x00010810
		public unsafe static StbTrueType.stbtt__buf stbtt__new_buf(void* p, ulong size)
		{
			return new StbTrueType.stbtt__buf
			{
				data = (byte*)p,
				size = (int)size,
				cursor = 0
			};
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00012640 File Offset: 0x00010840
		public unsafe static StbTrueType.stbtt__buf stbtt__buf_range(StbTrueType.stbtt__buf* b, int o, int s)
		{
			StbTrueType.stbtt__buf result = StbTrueType.stbtt__new_buf(null, 0UL);
			if (o < 0 || s < 0 || o > b->size || s > b->size - o)
			{
				return result;
			}
			result.data = b->data + o;
			result.size = s;
			return result;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00012690 File Offset: 0x00010890
		public unsafe static StbTrueType.stbtt__buf stbtt__cff_get_index(StbTrueType.stbtt__buf* b)
		{
			int cursor = b->cursor;
			int num = (int)StbTrueType.stbtt__buf_get(b, 2);
			if (num != 0)
			{
				int num2 = (int)StbTrueType.stbtt__buf_get8(b);
				StbTrueType.stbtt__buf_skip(b, num2 * num);
				StbTrueType.stbtt__buf_skip(b, (int)(StbTrueType.stbtt__buf_get(b, num2) - 1U));
			}
			return StbTrueType.stbtt__buf_range(b, cursor, b->cursor - cursor);
		}

		// Token: 0x0600013C RID: 316 RVA: 0x000126E0 File Offset: 0x000108E0
		public unsafe static uint stbtt__cff_int(StbTrueType.stbtt__buf* b)
		{
			int num = (int)StbTrueType.stbtt__buf_get8(b);
			if (num >= 32 && num <= 246)
			{
				return (uint)(num - 139);
			}
			if (num >= 247 && num <= 250)
			{
				return (uint)((num - 247) * 256 + (int)StbTrueType.stbtt__buf_get8(b) + 108);
			}
			if (num >= 251 && num <= 254)
			{
				return (uint)(-(num - 251) * 256 - (int)StbTrueType.stbtt__buf_get8(b) - 108);
			}
			if (num == 28)
			{
				return StbTrueType.stbtt__buf_get(b, 2);
			}
			if (num == 29)
			{
				return StbTrueType.stbtt__buf_get(b, 4);
			}
			return 0U;
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00012778 File Offset: 0x00010978
		public unsafe static void stbtt__cff_skip_operand(StbTrueType.stbtt__buf* b)
		{
			if (StbTrueType.stbtt__buf_peek8(b) == 30)
			{
				StbTrueType.stbtt__buf_skip(b, 1);
				while (b->cursor < b->size)
				{
					int num = (int)StbTrueType.stbtt__buf_get8(b);
					if ((num & 15) == 15 || num >> 4 == 15)
					{
						return;
					}
				}
				return;
			}
			StbTrueType.stbtt__cff_int(b);
		}

		// Token: 0x0600013E RID: 318 RVA: 0x000127C4 File Offset: 0x000109C4
		public unsafe static StbTrueType.stbtt__buf stbtt__dict_get(StbTrueType.stbtt__buf* b, int key)
		{
			StbTrueType.stbtt__buf_seek(b, 0);
			while (b->cursor < b->size)
			{
				int cursor = b->cursor;
				while (StbTrueType.stbtt__buf_peek8(b) >= 28)
				{
					StbTrueType.stbtt__cff_skip_operand(b);
				}
				int cursor2 = b->cursor;
				int num = (int)StbTrueType.stbtt__buf_get8(b);
				if (num == 12)
				{
					num = (int)StbTrueType.stbtt__buf_get8(b) | 256;
				}
				if (num == key)
				{
					return StbTrueType.stbtt__buf_range(b, cursor, cursor2 - cursor);
				}
			}
			return StbTrueType.stbtt__buf_range(b, 0, 0);
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00012838 File Offset: 0x00010A38
		public unsafe static void stbtt__dict_get_ints(StbTrueType.stbtt__buf* b, int key, int outcount, uint* _out_)
		{
			StbTrueType.stbtt__buf stbtt__buf = StbTrueType.stbtt__dict_get(b, key);
			int num = 0;
			while (num < outcount && stbtt__buf.cursor < stbtt__buf.size)
			{
				_out_[num] = StbTrueType.stbtt__cff_int(&stbtt__buf);
				num++;
			}
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00012876 File Offset: 0x00010A76
		public unsafe static int stbtt__cff_index_count(StbTrueType.stbtt__buf* b)
		{
			StbTrueType.stbtt__buf_seek(b, 0);
			return (int)StbTrueType.stbtt__buf_get(b, 2);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00012888 File Offset: 0x00010A88
		public unsafe static StbTrueType.stbtt__buf stbtt__cff_index_get(StbTrueType.stbtt__buf b, int i)
		{
			StbTrueType.stbtt__buf_seek(&b, 0);
			int num = (int)StbTrueType.stbtt__buf_get(&b, 2);
			int num2 = (int)StbTrueType.stbtt__buf_get8(&b);
			StbTrueType.stbtt__buf_skip(&b, i * num2);
			int num3 = (int)StbTrueType.stbtt__buf_get(&b, num2);
			int num4 = (int)StbTrueType.stbtt__buf_get(&b, num2);
			return StbTrueType.stbtt__buf_range(&b, 2 + (num + 1) * num2 + num3, num4 - num3);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x000128E4 File Offset: 0x00010AE4
		public unsafe static ushort ttUSHORT(byte* p)
		{
			return (ushort)((int)(*p) * 256 + (int)p[1]);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x000128F4 File Offset: 0x00010AF4
		public unsafe static short ttSHORT(byte* p)
		{
			return (short)((int)(*p) * 256 + (int)p[1]);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00012904 File Offset: 0x00010B04
		public unsafe static uint ttULONG(byte* p)
		{
			return (uint)(((int)(*p) << 24) + ((int)p[1] << 16) + ((int)p[2] << 8) + (int)p[3]);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00012904 File Offset: 0x00010B04
		public unsafe static int ttLONG(byte* p)
		{
			return ((int)(*p) << 24) + ((int)p[1] << 16) + ((int)p[2] << 8) + (int)p[3];
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00012920 File Offset: 0x00010B20
		public unsafe static int stbtt__isfont(byte* font)
		{
			if (*font == 49 && font[1] == 0 && font[2] == 0 && font[3] == 0)
			{
				return 1;
			}
			if ((char)(*font) == "typ1"[0] && (char)font[1] == "typ1"[1] && (char)font[2] == "typ1"[2] && (char)font[3] == "typ1"[3])
			{
				return 1;
			}
			if ((char)(*font) == "OTTO"[0] && (char)font[1] == "OTTO"[1] && (char)font[2] == "OTTO"[2] && (char)font[3] == "OTTO"[3])
			{
				return 1;
			}
			if (*font == 0 && font[1] == 1 && font[2] == 0 && font[3] == 0)
			{
				return 1;
			}
			if ((char)(*font) == "true"[0] && (char)font[1] == "true"[1] && (char)font[2] == "true"[2] && (char)font[3] == "true"[3])
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00012A30 File Offset: 0x00010C30
		public unsafe static int stbtt_GetFontOffsetForIndex_internal(byte* font_collection, int index)
		{
			if (StbTrueType.stbtt__isfont(font_collection) != 0)
			{
				if (index != 0)
				{
					return -1;
				}
				return 0;
			}
			else
			{
				if ((char)(*font_collection) != "ttcf"[0] || (char)font_collection[1] != "ttcf"[1] || (char)font_collection[2] != "ttcf"[2] || (char)font_collection[3] != "ttcf"[3] || (StbTrueType.ttULONG(font_collection + 4) != 65536U && StbTrueType.ttULONG(font_collection + 4) != 131072U))
				{
					return -1;
				}
				int num = StbTrueType.ttLONG(font_collection + 8);
				if (index >= num)
				{
					return -1;
				}
				return (int)StbTrueType.ttULONG(font_collection + 12 + index * 4);
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00012ACC File Offset: 0x00010CCC
		public unsafe static int stbtt_GetNumberOfFonts_internal(byte* font_collection)
		{
			if (StbTrueType.stbtt__isfont(font_collection) != 0)
			{
				return 1;
			}
			if ((char)(*font_collection) == "ttcf"[0] && (char)font_collection[1] == "ttcf"[1] && (char)font_collection[2] == "ttcf"[2] && (char)font_collection[3] == "ttcf"[3] && (StbTrueType.ttULONG(font_collection + 4) == 65536U || StbTrueType.ttULONG(font_collection + 4) == 131072U))
			{
				return StbTrueType.ttLONG(font_collection + 8);
			}
			return 0;
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00012B50 File Offset: 0x00010D50
		public unsafe static StbTrueType.stbtt__buf stbtt__get_subrs(StbTrueType.stbtt__buf cff, StbTrueType.stbtt__buf fontdict)
		{
			uint num = 0U;
			uint* ptr = stackalloc uint[(UIntPtr)8];
			*ptr = 0U;
			ptr[1] = 0U;
			StbTrueType.stbtt__buf stbtt__buf = default(StbTrueType.stbtt__buf);
			StbTrueType.stbtt__dict_get_ints(&fontdict, 18, 2, ptr);
			if (ptr[1] == 0U || *ptr == 0U)
			{
				return StbTrueType.stbtt__new_buf(null, 0UL);
			}
			stbtt__buf = StbTrueType.stbtt__buf_range(&cff, (int)ptr[1], (int)(*ptr));
			StbTrueType.stbtt__dict_get_ints(&stbtt__buf, 19, 1, &num);
			if (num == 0U)
			{
				return StbTrueType.stbtt__new_buf(null, 0UL);
			}
			StbTrueType.stbtt__buf_seek(&cff, (int)(ptr[1] + num));
			return StbTrueType.stbtt__cff_get_index(&cff);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00012BD4 File Offset: 0x00010DD4
		public unsafe static int stbtt_InitFont_internal(StbTrueType.stbtt_fontinfo* info, byte* data, int fontstart)
		{
			info->data = data;
			info->fontstart = fontstart;
			info->cff = StbTrueType.stbtt__new_buf(null, 0UL);
			uint num = StbTrueType.stbtt__find_table(data, (uint)fontstart, "cmap");
			info->loca = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "loca");
			info->head = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "head");
			info->glyf = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "glyf");
			info->hhea = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "hhea");
			info->hmtx = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "hmtx");
			info->kern = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "kern");
			info->gpos = (int)StbTrueType.stbtt__find_table(data, (uint)fontstart, "GPOS");
			if (num == 0U || info->head == 0 || info->hhea == 0 || info->hmtx == 0)
			{
				return 0;
			}
			if (info->glyf != 0)
			{
				if (info->loca == 0)
				{
					return 0;
				}
			}
			else
			{
				StbTrueType.stbtt__buf stbtt__buf = default(StbTrueType.stbtt__buf);
				StbTrueType.stbtt__buf fontdict = default(StbTrueType.stbtt__buf);
				uint num2 = 2U;
				uint num3 = 0U;
				uint num4 = 0U;
				uint num5 = 0U;
				uint num6 = StbTrueType.stbtt__find_table(data, (uint)fontstart, "CFF ");
				if (num6 == 0U)
				{
					return 0;
				}
				info->fontdicts = StbTrueType.stbtt__new_buf(null, 0UL);
				info->fdselect = StbTrueType.stbtt__new_buf(null, 0UL);
				info->cff = StbTrueType.stbtt__new_buf((void*)(data + num6), 536870912UL);
				stbtt__buf = info->cff;
				StbTrueType.stbtt__buf_skip(&stbtt__buf, 2);
				StbTrueType.stbtt__buf_seek(&stbtt__buf, (int)StbTrueType.stbtt__buf_get8(&stbtt__buf));
				StbTrueType.stbtt__cff_get_index(&stbtt__buf);
				fontdict = StbTrueType.stbtt__cff_index_get(StbTrueType.stbtt__cff_get_index(&stbtt__buf), 0);
				StbTrueType.stbtt__cff_get_index(&stbtt__buf);
				info->gsubrs = StbTrueType.stbtt__cff_get_index(&stbtt__buf);
				StbTrueType.stbtt__dict_get_ints(&fontdict, 17, 1, &num3);
				StbTrueType.stbtt__dict_get_ints(&fontdict, 262, 1, &num2);
				StbTrueType.stbtt__dict_get_ints(&fontdict, 292, 1, &num4);
				StbTrueType.stbtt__dict_get_ints(&fontdict, 293, 1, &num5);
				info->subrs = StbTrueType.stbtt__get_subrs(stbtt__buf, fontdict);
				if (num2 != 2U)
				{
					return 0;
				}
				if (num3 == 0U)
				{
					return 0;
				}
				if (num4 != 0U)
				{
					if (num5 == 0U)
					{
						return 0;
					}
					StbTrueType.stbtt__buf_seek(&stbtt__buf, (int)num4);
					info->fontdicts = StbTrueType.stbtt__cff_get_index(&stbtt__buf);
					info->fdselect = StbTrueType.stbtt__buf_range(&stbtt__buf, (int)num5, (int)((long)stbtt__buf.size - (long)((ulong)num5)));
				}
				StbTrueType.stbtt__buf_seek(&stbtt__buf, (int)num3);
				info->charstrings = StbTrueType.stbtt__cff_get_index(&stbtt__buf);
			}
			uint num7 = StbTrueType.stbtt__find_table(data, (uint)fontstart, "maxp");
			if (num7 != 0U)
			{
				info->numGlyphs = (int)StbTrueType.ttUSHORT(data + num7 + 4);
			}
			else
			{
				info->numGlyphs = 65535;
			}
			int num8 = (int)StbTrueType.ttUSHORT(data + num + 2);
			info->index_map = 0;
			for (int i = 0; i < num8; i++)
			{
				uint num9 = (uint)((ulong)(num + 4U) + (ulong)((long)(8 * i)));
				ushort num10 = StbTrueType.ttUSHORT(data + num9);
				if (num10 != 0)
				{
					if (num10 == 3)
					{
						num10 = StbTrueType.ttUSHORT(data + num9 + 2);
						if (num10 == 1 || num10 == 10)
						{
							info->index_map = (int)(num + StbTrueType.ttULONG(data + num9 + 4));
						}
					}
				}
				else
				{
					info->index_map = (int)(num + StbTrueType.ttULONG(data + num9 + 4));
				}
			}
			if (info->index_map == 0)
			{
				return 0;
			}
			info->indexToLocFormat = (int)StbTrueType.ttUSHORT(data + info->head + 50);
			return 1;
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00012EF8 File Offset: 0x000110F8
		public unsafe static int stbtt_FindGlyphIndex(StbTrueType.stbtt_fontinfo* info, int unicode_codepoint)
		{
			byte* data = info->data;
			uint index_map = (uint)info->index_map;
			ushort num = StbTrueType.ttUSHORT(data + index_map);
			if (num == 0)
			{
				int num2 = (int)StbTrueType.ttUSHORT(data + index_map + 2);
				if (unicode_codepoint < num2 - 6)
				{
					return (int)(data + index_map + 6)[unicode_codepoint];
				}
				return 0;
			}
			else if (num == 6)
			{
				uint num3 = (uint)StbTrueType.ttUSHORT(data + index_map + 6);
				uint num4 = (uint)StbTrueType.ttUSHORT(data + index_map + 8);
				if (unicode_codepoint >= (int)num3 && unicode_codepoint < (int)(num3 + num4))
				{
					return (int)StbTrueType.ttUSHORT(data + index_map + 10 + ((long)unicode_codepoint - (long)((ulong)num3)) * 2L);
				}
				return 0;
			}
			else
			{
				if (num == 2)
				{
					return 0;
				}
				if (num == 4)
				{
					ushort num5 = (ushort)(StbTrueType.ttUSHORT(data + index_map + 6) >> 1);
					ushort num6 = (ushort)(StbTrueType.ttUSHORT(data + index_map + 8) >> 1);
					ushort num7 = StbTrueType.ttUSHORT(data + index_map + 10);
					ushort num8 = (ushort)(StbTrueType.ttUSHORT(data + index_map + 12) >> 1);
					uint num9 = index_map + 14U;
					uint num10 = num9;
					if (unicode_codepoint > 65535)
					{
						return 0;
					}
					if (unicode_codepoint >= (int)StbTrueType.ttUSHORT(data + num10 + num8 * 2))
					{
						num10 += (uint)(num8 * 2);
					}
					num10 -= 2U;
					while (num7 != 0)
					{
						num6 = (ushort)(num6 >> 1);
						ushort num11 = StbTrueType.ttUSHORT(data + num10 + num6 * 2);
						if (unicode_codepoint > (int)num11)
						{
							num10 += (uint)(num6 * 2);
						}
						num7 -= 1;
					}
					num10 += 2U;
					ushort num12 = (ushort)(num10 - num9 >> 1);
					ushort num13 = StbTrueType.ttUSHORT(data + index_map + 14 + num5 * 2 + 2 + 2 * num12);
					if (unicode_codepoint < (int)num13)
					{
						return 0;
					}
					ushort num14 = StbTrueType.ttUSHORT(data + index_map + 14 + num5 * 6 + 2 + 2 * num12);
					if (num14 == 0)
					{
						return (int)((ushort)(unicode_codepoint + (int)StbTrueType.ttSHORT(data + index_map + 14 + num5 * 4 + 2 + 2 * num12)));
					}
					return (int)StbTrueType.ttUSHORT(data + num14 + (unicode_codepoint - (int)num13) * 2 + index_map + 14 + num5 * 6 + 2 + 2 * num12);
				}
				else
				{
					if (num == 12 || num == 13)
					{
						int num15 = (int)StbTrueType.ttULONG(data + index_map + 12);
						int i = 0;
						int num16 = num15;
						while (i < num16)
						{
							int num17 = i + (num16 - i >> 1);
							uint num18 = StbTrueType.ttULONG(data + index_map + 16 + num17 * 12);
							uint num19 = StbTrueType.ttULONG(data + index_map + 16 + num17 * 12 + 4);
							if (unicode_codepoint < (int)num18)
							{
								num16 = num17;
							}
							else if (unicode_codepoint > (int)num19)
							{
								i = num17 + 1;
							}
							else
							{
								uint num20 = StbTrueType.ttULONG(data + index_map + 16 + num17 * 12 + 8);
								if (num == 12)
								{
									return (int)((ulong)num20 + (ulong)((long)unicode_codepoint) - (ulong)num18);
								}
								return (int)num20;
							}
						}
						return 0;
					}
					return 0;
				}
			}
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00013166 File Offset: 0x00011366
		public unsafe static int stbtt_GetCodepointShape(StbTrueType.stbtt_fontinfo* info, int unicode_codepoint, StbTrueType.stbtt_vertex** vertices)
		{
			return StbTrueType.stbtt_GetGlyphShape(info, StbTrueType.stbtt_FindGlyphIndex(info, unicode_codepoint), vertices);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00013176 File Offset: 0x00011376
		public unsafe static void stbtt_setvertex(StbTrueType.stbtt_vertex* v, byte type, int x, int y, int cx, int cy)
		{
			v->type = type;
			v->x = (short)x;
			v->y = (short)y;
			v->cx = (short)cx;
			v->cy = (short)cy;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x000131A4 File Offset: 0x000113A4
		public unsafe static int stbtt__GetGlyfOffset(StbTrueType.stbtt_fontinfo* info, int glyph_index)
		{
			if (glyph_index >= info->numGlyphs)
			{
				return -1;
			}
			if (info->indexToLocFormat >= 2)
			{
				return -1;
			}
			int num;
			int num2;
			if (info->indexToLocFormat == 0)
			{
				num = info->glyf + (int)(StbTrueType.ttUSHORT(info->data + info->loca + glyph_index * 2) * 2);
				num2 = info->glyf + (int)(StbTrueType.ttUSHORT(info->data + info->loca + glyph_index * 2 + 2) * 2);
			}
			else
			{
				num = (int)((long)info->glyf + (long)((ulong)StbTrueType.ttULONG(info->data + info->loca + glyph_index * 4)));
				num2 = (int)((long)info->glyf + (long)((ulong)StbTrueType.ttULONG(info->data + info->loca + glyph_index * 4 + 4)));
			}
			if (num != num2)
			{
				return num;
			}
			return -1;
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00013260 File Offset: 0x00011460
		public unsafe static int stbtt_GetGlyphBox(StbTrueType.stbtt_fontinfo* info, int glyph_index, int* x0, int* y0, int* x1, int* y1)
		{
			if (info->cff.size != 0)
			{
				StbTrueType.stbtt__GetGlyphInfoT2(info, glyph_index, x0, y0, x1, y1);
			}
			else
			{
				int num = StbTrueType.stbtt__GetGlyfOffset(info, glyph_index);
				if (num < 0)
				{
					return 0;
				}
				if (x0 != null)
				{
					*x0 = (int)StbTrueType.ttSHORT(info->data + num + 2);
				}
				if (y0 != null)
				{
					*y0 = (int)StbTrueType.ttSHORT(info->data + num + 4);
				}
				if (x1 != null)
				{
					*x1 = (int)StbTrueType.ttSHORT(info->data + num + 6);
				}
				if (y1 != null)
				{
					*y1 = (int)StbTrueType.ttSHORT(info->data + num + 8);
				}
			}
			return 1;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x000132F5 File Offset: 0x000114F5
		public unsafe static int stbtt_GetCodepointBox(StbTrueType.stbtt_fontinfo* info, int codepoint, int* x0, int* y0, int* x1, int* y1)
		{
			return StbTrueType.stbtt_GetGlyphBox(info, StbTrueType.stbtt_FindGlyphIndex(info, codepoint), x0, y0, x1, y1);
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0001330C File Offset: 0x0001150C
		public unsafe static int stbtt_IsGlyphEmpty(StbTrueType.stbtt_fontinfo* info, int glyph_index)
		{
			if (info->cff.size != 0)
			{
				if (StbTrueType.stbtt__GetGlyphInfoT2(info, glyph_index, null, null, null, null) != 0)
				{
					return 0;
				}
				return 1;
			}
			else
			{
				int num = StbTrueType.stbtt__GetGlyfOffset(info, glyph_index);
				if (num < 0)
				{
					return 1;
				}
				if (StbTrueType.ttSHORT(info->data + num) != 0)
				{
					return 0;
				}
				return 1;
			}
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0001335C File Offset: 0x0001155C
		public unsafe static int stbtt__close_shape(StbTrueType.stbtt_vertex* vertices, int num_vertices, int was_off, int start_off, int sx, int sy, int scx, int scy, int cx, int cy)
		{
			if (start_off != 0)
			{
				if (was_off != 0)
				{
					StbTrueType.stbtt_setvertex(vertices + num_vertices++, 3, cx + scx >> 1, cy + scy >> 1, cx, cy);
				}
				StbTrueType.stbtt_setvertex(vertices + num_vertices++, 3, sx, sy, scx, scy);
			}
			else if (was_off != 0)
			{
				StbTrueType.stbtt_setvertex(vertices + num_vertices++, 3, sx, sy, cx, cy);
			}
			else
			{
				StbTrueType.stbtt_setvertex(vertices + num_vertices++, 2, sx, sy, 0, 0);
			}
			return num_vertices;
		}

		// Token: 0x06000153 RID: 339 RVA: 0x000133FC File Offset: 0x000115FC
		public unsafe static int stbtt__GetGlyphShapeTT(StbTrueType.stbtt_fontinfo* info, int glyph_index, StbTrueType.stbtt_vertex** pvertices)
		{
			byte* data = info->data;
			StbTrueType.stbtt_vertex* ptr = null;
			int num = 0;
			int num2 = StbTrueType.stbtt__GetGlyfOffset(info, glyph_index);
			*(IntPtr*)pvertices = (IntPtr)((UIntPtr)0);
			if (num2 < 0)
			{
				return 0;
			}
			short num3 = StbTrueType.ttSHORT(data + num2);
			if (num3 > 0)
			{
				byte b = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				byte* ptr2 = data + num2 + 10;
				int num7 = (int)StbTrueType.ttUSHORT(data + num2 + 10 + num3 * 2);
				byte* ptr3 = data + num2 + 10 + num3 * 2 + 2 + num7;
				int num8 = (int)(1 + StbTrueType.ttUSHORT(ptr2 + num3 * 2 - 2));
				int num9 = num8 + (int)(2 * num3);
				ptr = (StbTrueType.stbtt_vertex*)CRuntime.malloc((ulong)((long)(num9 * sizeof(StbTrueType.stbtt_vertex))));
				if (ptr == null)
				{
					return 0;
				}
				int num10 = 0;
				byte b2 = 0;
				int num11 = num9 - num8;
				for (int i = 0; i < num8; i++)
				{
					if (b2 == 0)
					{
						b = *(ptr3++);
						if ((b & 8) != 0)
						{
							b2 = *(ptr3++);
						}
					}
					else
					{
						b2 -= 1;
					}
					ptr[num11 + i].type = b;
				}
				int num12 = 0;
				for (int i = 0; i < num8; i++)
				{
					b = ptr[num11 + i].type;
					if ((b & 2) != 0)
					{
						short num13 = (short)(*(ptr3++));
						num12 += (int)(((b & 16) != 0) ? num13 : (-(int)num13));
					}
					else if ((b & 16) == 0)
					{
						num12 += (int)((short)((int)(*ptr3) * 256 + (int)ptr3[1]));
						ptr3 += 2;
					}
					ptr[num11 + i].x = (short)num12;
				}
				int num14 = 0;
				for (int i = 0; i < num8; i++)
				{
					b = ptr[num11 + i].type;
					if ((b & 4) != 0)
					{
						short num15 = (short)(*(ptr3++));
						num14 += (int)(((b & 32) != 0) ? num15 : (-(int)num15));
					}
					else if ((b & 32) == 0)
					{
						num14 += (int)((short)((int)(*ptr3) * 256 + (int)ptr3[1]));
						ptr3 += 2;
					}
					ptr[num11 + i].y = (short)num14;
				}
				num = 0;
				int scy;
				int scx;
				int num19;
				int num18;
				int num17;
				int num16 = (num17 = (num18 = (num19 = (scx = (scy = 0)))));
				for (int i = 0; i < num8; i++)
				{
					b = ptr[num11 + i].type;
					num12 = (int)ptr[num11 + i].x;
					num14 = (int)ptr[num11 + i].y;
					if (num10 == i)
					{
						if (i != 0)
						{
							num = StbTrueType.stbtt__close_shape(ptr, num, num5, num6, num17, num16, scx, scy, num18, num19);
						}
						num6 = (((b & 1) != 0) ? 0 : 1);
						if (num6 != 0)
						{
							scx = num12;
							scy = num14;
							if ((ptr[num11 + i + 1].type & 1) == 0)
							{
								num17 = num12 + (int)ptr[num11 + i + 1].x >> 1;
								num16 = num14 + (int)ptr[num11 + i + 1].y >> 1;
							}
							else
							{
								num17 = (int)ptr[num11 + i + 1].x;
								num16 = (int)ptr[num11 + i + 1].y;
								i++;
							}
						}
						else
						{
							num17 = num12;
							num16 = num14;
						}
						StbTrueType.stbtt_setvertex(ptr + num++, 1, num17, num16, 0, 0);
						num5 = 0;
						num10 = (int)(1 + StbTrueType.ttUSHORT(ptr2 + num4 * 2));
						num4++;
					}
					else if ((b & 1) == 0)
					{
						if (num5 != 0)
						{
							StbTrueType.stbtt_setvertex(ptr + num++, 3, num18 + num12 >> 1, num19 + num14 >> 1, num18, num19);
						}
						num18 = num12;
						num19 = num14;
						num5 = 1;
					}
					else
					{
						if (num5 != 0)
						{
							StbTrueType.stbtt_setvertex(ptr + num++, 3, num12, num14, num18, num19);
						}
						else
						{
							StbTrueType.stbtt_setvertex(ptr + num++, 2, num12, num14, 0, 0);
						}
						num5 = 0;
					}
				}
				num = StbTrueType.stbtt__close_shape(ptr, num, num5, num6, num17, num16, scx, scy, num18, num19);
			}
			else if (num3 == -1)
			{
				int num20 = 1;
				byte* ptr4 = data + num2 + 10;
				num = 0;
				ptr = null;
				while (num20 != 0)
				{
					StbTrueType.stbtt_vertex* ptr5 = null;
					StbTrueType.stbtt_vertex* ptr6 = null;
					float* ptr7 = stackalloc float[(UIntPtr)24];
					*ptr7 = 1f;
					ptr7[1] = 0f;
					ptr7[2] = 0f;
					ptr7[3] = 1f;
					ptr7[4] = 0f;
					ptr7[5] = 0f;
					ushort num21 = (ushort)StbTrueType.ttSHORT(ptr4);
					ptr4 += 2;
					ushort glyph_index2 = (ushort)StbTrueType.ttSHORT(ptr4);
					ptr4 += 2;
					if ((num21 & 2) != 0)
					{
						if ((num21 & 1) != 0)
						{
							ptr7[4] = (float)StbTrueType.ttSHORT(ptr4);
							ptr4 += 2;
							ptr7[5] = (float)StbTrueType.ttSHORT(ptr4);
							ptr4 += 2;
						}
						else
						{
							ptr7[4] = (float)(*(sbyte*)ptr4);
							ptr4++;
							ptr7[5] = (float)(*(sbyte*)ptr4);
							ptr4++;
						}
					}
					if ((num21 & 8) != 0)
					{
						*ptr7 = (ptr7[3] = (float)StbTrueType.ttSHORT(ptr4) / 16384f);
						ptr4 += 2;
						ptr7[1] = (ptr7[2] = 0f);
					}
					else if ((num21 & 64) != 0)
					{
						*ptr7 = (float)StbTrueType.ttSHORT(ptr4) / 16384f;
						ptr4 += 2;
						ptr7[1] = (ptr7[2] = 0f);
						ptr7[3] = (float)StbTrueType.ttSHORT(ptr4) / 16384f;
						ptr4 += 2;
					}
					else if ((num21 & 128) != 0)
					{
						*ptr7 = (float)StbTrueType.ttSHORT(ptr4) / 16384f;
						ptr4 += 2;
						ptr7[1] = (float)StbTrueType.ttSHORT(ptr4) / 16384f;
						ptr4 += 2;
						ptr7[2] = (float)StbTrueType.ttSHORT(ptr4) / 16384f;
						ptr4 += 2;
						ptr7[3] = (float)StbTrueType.ttSHORT(ptr4) / 16384f;
						ptr4 += 2;
					}
					float num22 = (float)CRuntime.sqrt((double)(*ptr7 * *ptr7 + ptr7[1] * ptr7[1]));
					float num23 = (float)CRuntime.sqrt((double)(ptr7[2] * ptr7[2] + ptr7[3] * ptr7[3]));
					int num24 = StbTrueType.stbtt_GetGlyphShape(info, (int)glyph_index2, &ptr5);
					if (num24 > 0)
					{
						for (int j = 0; j < num24; j++)
						{
							StbTrueType.stbtt_vertex* ptr8 = ptr5 + j;
							short num25 = ptr8->x;
							short num26 = ptr8->y;
							ptr8->x = (short)(num22 * (*ptr7 * (float)num25 + ptr7[2] * (float)num26 + ptr7[4]));
							ptr8->y = (short)(num23 * (ptr7[1] * (float)num25 + ptr7[3] * (float)num26 + ptr7[5]));
							num25 = ptr8->cx;
							num26 = ptr8->cy;
							ptr8->cx = (short)(num22 * (*ptr7 * (float)num25 + ptr7[2] * (float)num26 + ptr7[4]));
							ptr8->cy = (short)(num23 * (ptr7[1] * (float)num25 + ptr7[3] * (float)num26 + ptr7[5]));
						}
						ptr6 = (StbTrueType.stbtt_vertex*)CRuntime.malloc((ulong)((long)((num + num24) * sizeof(StbTrueType.stbtt_vertex))));
						if (ptr6 == null)
						{
							if (ptr != null)
							{
								CRuntime.free((void*)ptr);
							}
							if (ptr5 != null)
							{
								CRuntime.free((void*)ptr5);
							}
							return 0;
						}
						if (num > 0)
						{
							CRuntime.memcpy((void*)ptr6, (void*)ptr, (ulong)((long)(num * sizeof(StbTrueType.stbtt_vertex))));
						}
						CRuntime.memcpy((void*)(ptr6 + num), (void*)ptr5, (ulong)((long)(num24 * sizeof(StbTrueType.stbtt_vertex))));
						if (ptr != null)
						{
							CRuntime.free((void*)ptr);
						}
						ptr = ptr6;
						CRuntime.free((void*)ptr5);
						num += num24;
					}
					num20 = (int)(num21 & 32);
				}
			}
			*(IntPtr*)pvertices = ptr;
			return num;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00013C20 File Offset: 0x00011E20
		public unsafe static void stbtt__track_vertex(StbTrueType.stbtt__csctx* c, int x, int y)
		{
			if (x > c->max_x || c->started == 0)
			{
				c->max_x = x;
			}
			if (y > c->max_y || c->started == 0)
			{
				c->max_y = y;
			}
			if (x < c->min_x || c->started == 0)
			{
				c->min_x = x;
			}
			if (y < c->min_y || c->started == 0)
			{
				c->min_y = y;
			}
			c->started = 1;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00013C94 File Offset: 0x00011E94
		public unsafe static void stbtt__csctx_v(StbTrueType.stbtt__csctx* c, byte type, int x, int y, int cx, int cy, int cx1, int cy1)
		{
			if (c->bounds != 0)
			{
				StbTrueType.stbtt__track_vertex(c, x, y);
				if (type == 4)
				{
					StbTrueType.stbtt__track_vertex(c, cx, cy);
					StbTrueType.stbtt__track_vertex(c, cx1, cy1);
				}
			}
			else
			{
				StbTrueType.stbtt_setvertex(c->pvertices + c->num_vertices, type, x, y, cx, cy);
				c->pvertices[c->num_vertices].cx1 = (short)cx1;
				c->pvertices[c->num_vertices].cy1 = (short)cy1;
			}
			c->num_vertices = c->num_vertices + 1;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00013D32 File Offset: 0x00011F32
		public unsafe static void stbtt__csctx_close_shape(StbTrueType.stbtt__csctx* ctx)
		{
			if (ctx->first_x != ctx->x || ctx->first_y != ctx->y)
			{
				StbTrueType.stbtt__csctx_v(ctx, 2, (int)ctx->first_x, (int)ctx->first_y, 0, 0, 0, 0);
			}
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00013D6C File Offset: 0x00011F6C
		public unsafe static void stbtt__csctx_rmove_to(StbTrueType.stbtt__csctx* ctx, float dx, float dy)
		{
			StbTrueType.stbtt__csctx_close_shape(ctx);
			ctx->first_x = (ctx->x = ctx->x + dx);
			ctx->first_y = (ctx->y = ctx->y + dy);
			StbTrueType.stbtt__csctx_v(ctx, 1, (int)ctx->x, (int)ctx->y, 0, 0, 0, 0);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00013DCA File Offset: 0x00011FCA
		public unsafe static void stbtt__csctx_rline_to(StbTrueType.stbtt__csctx* ctx, float dx, float dy)
		{
			ctx->x = ctx->x + dx;
			ctx->y = ctx->y + dy;
			StbTrueType.stbtt__csctx_v(ctx, 2, (int)ctx->x, (int)ctx->y, 0, 0, 0, 0);
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00013E00 File Offset: 0x00012000
		public unsafe static void stbtt__csctx_rccurve_to(StbTrueType.stbtt__csctx* ctx, float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
		{
			float num = ctx->x + dx1;
			float num2 = ctx->y + dy1;
			float num3 = num + dx2;
			float num4 = num2 + dy2;
			ctx->x = num3 + dx3;
			ctx->y = num4 + dy3;
			StbTrueType.stbtt__csctx_v(ctx, 4, (int)ctx->x, (int)ctx->y, (int)num, (int)num2, (int)num3, (int)num4);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00013E60 File Offset: 0x00012060
		public unsafe static StbTrueType.stbtt__buf stbtt__get_subr(StbTrueType.stbtt__buf idx, int n)
		{
			int num = StbTrueType.stbtt__cff_index_count(&idx);
			int num2 = 107;
			if (num >= 33900)
			{
				num2 = 32768;
			}
			else if (num >= 1240)
			{
				num2 = 1131;
			}
			n += num2;
			if (n < 0 || n >= num)
			{
				return StbTrueType.stbtt__new_buf(null, 0UL);
			}
			return StbTrueType.stbtt__cff_index_get(idx, n);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00013EB8 File Offset: 0x000120B8
		public unsafe static StbTrueType.stbtt__buf stbtt__cid_get_glyph_subrs(StbTrueType.stbtt_fontinfo* info, int glyph_index)
		{
			StbTrueType.stbtt__buf fdselect = info->fdselect;
			int num = -1;
			StbTrueType.stbtt__buf_seek(&fdselect, 0);
			int num2 = (int)StbTrueType.stbtt__buf_get8(&fdselect);
			if (num2 == 0)
			{
				StbTrueType.stbtt__buf_skip(&fdselect, glyph_index);
				num = (int)StbTrueType.stbtt__buf_get8(&fdselect);
			}
			else if (num2 == 3)
			{
				int num3 = (int)StbTrueType.stbtt__buf_get(&fdselect, 2);
				int num4 = (int)StbTrueType.stbtt__buf_get(&fdselect, 2);
				for (int i = 0; i < num3; i++)
				{
					int num5 = (int)StbTrueType.stbtt__buf_get8(&fdselect);
					int num6 = (int)StbTrueType.stbtt__buf_get(&fdselect, 2);
					if (glyph_index >= num4 && glyph_index < num6)
					{
						num = num5;
						break;
					}
					num4 = num6;
				}
			}
			if (num == -1)
			{
				StbTrueType.stbtt__new_buf(null, 0UL);
			}
			return StbTrueType.stbtt__get_subrs(info->cff, StbTrueType.stbtt__cff_index_get(info->fontdicts, num));
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00013F70 File Offset: 0x00012170
		public unsafe static int stbtt__run_charstring(StbTrueType.stbtt_fontinfo* info, int glyph_index, StbTrueType.stbtt__csctx* c)
		{
			int num = 1;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			float* ptr = stackalloc float[(UIntPtr)192];
			StbTrueType.stbtt__buf* ptr2 = stackalloc StbTrueType.stbtt__buf[checked(unchecked((UIntPtr)10) * (UIntPtr)sizeof(StbTrueType.stbtt__buf))];
			StbTrueType.stbtt__buf stbtt__buf = info->subrs;
			StbTrueType.stbtt__buf stbtt__buf2 = default(StbTrueType.stbtt__buf);
			stbtt__buf2 = StbTrueType.stbtt__cff_index_get(info->charstrings, glyph_index);
			while (stbtt__buf2.cursor < stbtt__buf2.size)
			{
				int num6 = 0;
				int num7 = 1;
				int num8 = (int)StbTrueType.stbtt__buf_get8(&stbtt__buf2);
				float num24;
				switch (num8)
				{
				case 1:
				case 3:
				case 18:
				case 23:
					num2 += num4 / 2;
					break;
				case 2:
				case 9:
				case 13:
				case 15:
				case 16:
				case 17:
				case 28:
					goto IL_8F5;
				case 4:
					num = 0;
					if (num4 < 1)
					{
						return 0;
					}
					StbTrueType.stbtt__csctx_rmove_to(c, 0f, ptr[num4 - 1]);
					break;
				case 5:
					if (num4 < 2)
					{
						return 0;
					}
					while (num6 + 1 < num4)
					{
						StbTrueType.stbtt__csctx_rline_to(c, ptr[num6], ptr[num6 + 1]);
						num6 += 2;
					}
					break;
				case 6:
				case 7:
				{
					int num9 = 0;
					if (num8 == 7)
					{
						if (num4 < 1)
						{
							return 0;
						}
						num9 = 1;
					}
					if (num4 < 1)
					{
						return 0;
					}
					for (;;)
					{
						if (num9 == 0)
						{
							if (num6 >= num4)
							{
								break;
							}
							StbTrueType.stbtt__csctx_rline_to(c, ptr[num6], 0f);
							num6++;
						}
						else
						{
							num9 = 0;
						}
						if (num6 >= num4)
						{
							break;
						}
						StbTrueType.stbtt__csctx_rline_to(c, 0f, ptr[num6]);
						num6++;
					}
					break;
				}
				case 8:
					if (num4 < 6)
					{
						return 0;
					}
					while (num6 + 5 < num4)
					{
						StbTrueType.stbtt__csctx_rccurve_to(c, ptr[num6], ptr[num6 + 1], ptr[num6 + 2], ptr[num6 + 3], ptr[num6 + 4], ptr[num6 + 5]);
						num6 += 6;
					}
					break;
				case 10:
				case 29:
				{
					if (num8 == 10 && num5 == 0)
					{
						if (info->fdselect.size != 0)
						{
							stbtt__buf = StbTrueType.stbtt__cid_get_glyph_subrs(info, glyph_index);
						}
						num5 = 1;
					}
					if (num4 < 1)
					{
						return 0;
					}
					int n = (int)ptr[(IntPtr)(--num4) * 4];
					if (num3 >= 10)
					{
						return 0;
					}
					ptr2[(IntPtr)(num3++) * (IntPtr)sizeof(StbTrueType.stbtt__buf)] = stbtt__buf2;
					stbtt__buf2 = StbTrueType.stbtt__get_subr((num8 == 10) ? stbtt__buf : info->gsubrs, n);
					if (stbtt__buf2.size == 0)
					{
						return 0;
					}
					stbtt__buf2.cursor = 0;
					num7 = 0;
					break;
				}
				case 11:
					if (num3 <= 0)
					{
						return 0;
					}
					stbtt__buf2 = ptr2[(IntPtr)(--num3) * (IntPtr)sizeof(StbTrueType.stbtt__buf)];
					num7 = 0;
					break;
				case 12:
					switch (StbTrueType.stbtt__buf_get8(&stbtt__buf2))
					{
					case 34:
					{
						if (num4 < 7)
						{
							return 0;
						}
						float num10 = *ptr;
						float num11 = ptr[1];
						float num12 = ptr[2];
						float num13 = ptr[3];
						float num14 = ptr[4];
						float num15 = ptr[5];
						float num16 = ptr[6];
						StbTrueType.stbtt__csctx_rccurve_to(c, num10, 0f, num11, num12, num13, 0f);
						StbTrueType.stbtt__csctx_rccurve_to(c, num14, 0f, num15, -num12, num16, 0f);
						break;
					}
					case 35:
					{
						if (num4 < 13)
						{
							return 0;
						}
						float num10 = *ptr;
						float num17 = ptr[1];
						float num11 = ptr[2];
						float num12 = ptr[3];
						float num13 = ptr[4];
						float num18 = ptr[5];
						float num14 = ptr[6];
						float num19 = ptr[7];
						float num15 = ptr[8];
						float num20 = ptr[9];
						float num16 = ptr[10];
						float num21 = ptr[11];
						StbTrueType.stbtt__csctx_rccurve_to(c, num10, num17, num11, num12, num13, num18);
						StbTrueType.stbtt__csctx_rccurve_to(c, num14, num19, num15, num20, num16, num21);
						break;
					}
					case 36:
					{
						if (num4 < 9)
						{
							return 0;
						}
						float num10 = *ptr;
						float num17 = ptr[1];
						float num11 = ptr[2];
						float num12 = ptr[3];
						float num13 = ptr[4];
						float num14 = ptr[5];
						float num15 = ptr[6];
						float num20 = ptr[7];
						float num16 = ptr[8];
						StbTrueType.stbtt__csctx_rccurve_to(c, num10, num17, num11, num12, num13, 0f);
						StbTrueType.stbtt__csctx_rccurve_to(c, num14, 0f, num15, num20, num16, -(num17 + num12 + num20));
						break;
					}
					case 37:
					{
						if (num4 < 11)
						{
							return 0;
						}
						float num10 = *ptr;
						float num17 = ptr[1];
						float num11 = ptr[2];
						float num12 = ptr[3];
						float num13 = ptr[4];
						float num18 = ptr[5];
						float num14 = ptr[6];
						float num19 = ptr[7];
						float num15 = ptr[8];
						float num20 = ptr[9];
						float num21;
						float num16 = (num21 = ptr[10]);
						float num22 = num10 + num11 + num13 + num14 + num15;
						float num23 = num17 + num12 + num18 + num19 + num20;
						if (CRuntime.fabs((double)num22) > CRuntime.fabs((double)num23))
						{
							num21 = -num23;
						}
						else
						{
							num16 = -num22;
						}
						StbTrueType.stbtt__csctx_rccurve_to(c, num10, num17, num11, num12, num13, num18);
						StbTrueType.stbtt__csctx_rccurve_to(c, num14, num19, num15, num20, num16, num21);
						break;
					}
					default:
						return 0;
					}
					break;
				case 14:
					StbTrueType.stbtt__csctx_close_shape(c);
					return 1;
				case 19:
				case 20:
					if (num != 0)
					{
						num2 += num4 / 2;
					}
					num = 0;
					StbTrueType.stbtt__buf_skip(&stbtt__buf2, (num2 + 7) / 8);
					break;
				case 21:
					num = 0;
					if (num4 < 2)
					{
						return 0;
					}
					StbTrueType.stbtt__csctx_rmove_to(c, ptr[num4 - 2], ptr[num4 - 1]);
					break;
				case 22:
					num = 0;
					if (num4 < 1)
					{
						return 0;
					}
					StbTrueType.stbtt__csctx_rmove_to(c, ptr[num4 - 1], 0f);
					break;
				case 24:
					if (num4 < 8)
					{
						return 0;
					}
					while (num6 + 5 < num4 - 2)
					{
						StbTrueType.stbtt__csctx_rccurve_to(c, ptr[num6], ptr[num6 + 1], ptr[num6 + 2], ptr[num6 + 3], ptr[num6 + 4], ptr[num6 + 5]);
						num6 += 6;
					}
					if (num6 + 1 >= num4)
					{
						return 0;
					}
					StbTrueType.stbtt__csctx_rline_to(c, ptr[num6], ptr[num6 + 1]);
					break;
				case 25:
					if (num4 < 8)
					{
						return 0;
					}
					while (num6 + 1 < num4 - 6)
					{
						StbTrueType.stbtt__csctx_rline_to(c, ptr[num6], ptr[num6 + 1]);
						num6 += 2;
					}
					if (num6 + 5 >= num4)
					{
						return 0;
					}
					StbTrueType.stbtt__csctx_rccurve_to(c, ptr[num6], ptr[num6 + 1], ptr[num6 + 2], ptr[num6 + 3], ptr[num6 + 4], ptr[num6 + 5]);
					break;
				case 26:
				case 27:
					if (num4 < 4)
					{
						return 0;
					}
					num24 = 0f;
					if ((num4 & 1) != 0)
					{
						num24 = ptr[num6];
						num6++;
					}
					while (num6 + 3 < num4)
					{
						if (num8 == 27)
						{
							StbTrueType.stbtt__csctx_rccurve_to(c, ptr[num6], num24, ptr[num6 + 1], ptr[num6 + 2], ptr[num6 + 3], 0f);
						}
						else
						{
							StbTrueType.stbtt__csctx_rccurve_to(c, num24, ptr[num6], ptr[num6 + 1], ptr[num6 + 2], 0f, ptr[num6 + 3]);
						}
						num24 = 0f;
						num6 += 4;
					}
					break;
				case 30:
				case 31:
				{
					int num25 = 0;
					if (num8 == 31)
					{
						if (num4 < 4)
						{
							return 0;
						}
						num25 = 1;
					}
					for (;;)
					{
						if (num25 == 0)
						{
							if (num6 + 3 >= num4)
							{
								break;
							}
							StbTrueType.stbtt__csctx_rccurve_to(c, 0f, ptr[num6], ptr[num6 + 1], ptr[num6 + 2], ptr[num6 + 3], (num4 - num6 == 5) ? ptr[num6 + 4] : 0f);
							num6 += 4;
						}
						else
						{
							num25 = 0;
						}
						if (num6 + 3 >= num4)
						{
							break;
						}
						StbTrueType.stbtt__csctx_rccurve_to(c, ptr[num6], 0f, ptr[num6 + 1], ptr[num6 + 2], (num4 - num6 == 5) ? ptr[num6 + 4] : 0f, ptr[num6 + 3]);
						num6 += 4;
					}
					break;
				}
				default:
					goto IL_8F5;
				}
				IL_962:
				if (num7 != 0)
				{
					num4 = 0;
					continue;
				}
				continue;
				IL_8F5:
				if (num8 != 255 && num8 != 28 && (num8 < 32 || num8 > 254))
				{
					return 0;
				}
				if (num8 == 255)
				{
					num24 = StbTrueType.stbtt__buf_get(&stbtt__buf2, 4) / 65536f;
				}
				else
				{
					StbTrueType.stbtt__buf_skip(&stbtt__buf2, -1);
					num24 = (float)((short)StbTrueType.stbtt__cff_int(&stbtt__buf2));
				}
				if (num4 >= 48)
				{
					return 0;
				}
				ptr[(IntPtr)(num4++) * 4] = num24;
				num7 = 0;
				goto IL_962;
			}
			return 0;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000148FC File Offset: 0x00012AFC
		public unsafe static int stbtt__GetGlyphShapeT2(StbTrueType.stbtt_fontinfo* info, int glyph_index, StbTrueType.stbtt_vertex** pvertices)
		{
			StbTrueType.stbtt__csctx stbtt__csctx = default(StbTrueType.stbtt__csctx);
			stbtt__csctx.bounds = 1;
			StbTrueType.stbtt__csctx stbtt__csctx2 = default(StbTrueType.stbtt__csctx);
			if (StbTrueType.stbtt__run_charstring(info, glyph_index, &stbtt__csctx) != 0)
			{
				*(IntPtr*)pvertices = CRuntime.malloc((ulong)((long)(stbtt__csctx.num_vertices * sizeof(StbTrueType.stbtt_vertex))));
				stbtt__csctx2.pvertices = *(IntPtr*)pvertices;
				if (StbTrueType.stbtt__run_charstring(info, glyph_index, &stbtt__csctx2) != 0)
				{
					return stbtt__csctx2.num_vertices;
				}
			}
			*(IntPtr*)pvertices = (IntPtr)((UIntPtr)0);
			return 0;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00014964 File Offset: 0x00012B64
		public unsafe static int stbtt__GetGlyphInfoT2(StbTrueType.stbtt_fontinfo* info, int glyph_index, int* x0, int* y0, int* x1, int* y1)
		{
			StbTrueType.stbtt__csctx stbtt__csctx = default(StbTrueType.stbtt__csctx);
			stbtt__csctx.bounds = 1;
			int num = StbTrueType.stbtt__run_charstring(info, glyph_index, &stbtt__csctx);
			if (x0 != null)
			{
				*x0 = ((num != 0) ? stbtt__csctx.min_x : 0);
			}
			if (y0 != null)
			{
				*y0 = ((num != 0) ? stbtt__csctx.min_y : 0);
			}
			if (x1 != null)
			{
				*x1 = ((num != 0) ? stbtt__csctx.max_x : 0);
			}
			if (y1 != null)
			{
				*y1 = ((num != 0) ? stbtt__csctx.max_y : 0);
			}
			if (num == 0)
			{
				return 0;
			}
			return stbtt__csctx.num_vertices;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x000149E7 File Offset: 0x00012BE7
		public unsafe static int stbtt_GetGlyphShape(StbTrueType.stbtt_fontinfo* info, int glyph_index, StbTrueType.stbtt_vertex** pvertices)
		{
			if (info->cff.size == 0)
			{
				return StbTrueType.stbtt__GetGlyphShapeTT(info, glyph_index, pvertices);
			}
			return StbTrueType.stbtt__GetGlyphShapeT2(info, glyph_index, pvertices);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00014A08 File Offset: 0x00012C08
		public unsafe static void stbtt_GetGlyphHMetrics(StbTrueType.stbtt_fontinfo* info, int glyph_index, int* advanceWidth, int* leftSideBearing)
		{
			ushort num = StbTrueType.ttUSHORT(info->data + info->hhea + 34);
			if (glyph_index < (int)num)
			{
				if (advanceWidth != null)
				{
					*advanceWidth = (int)StbTrueType.ttSHORT(info->data + info->hmtx + 4 * glyph_index);
				}
				if (leftSideBearing != null)
				{
					*leftSideBearing = (int)StbTrueType.ttSHORT(info->data + info->hmtx + 4 * glyph_index + 2);
					return;
				}
			}
			else
			{
				if (advanceWidth != null)
				{
					*advanceWidth = (int)StbTrueType.ttSHORT(info->data + info->hmtx + 4 * (num - 1));
				}
				if (leftSideBearing != null)
				{
					*leftSideBearing = (int)StbTrueType.ttSHORT(info->data + info->hmtx + 4 * num + 2 * (glyph_index - (int)num));
				}
			}
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00014AB0 File Offset: 0x00012CB0
		public unsafe static int stbtt__GetGlyphKernInfoAdvance(StbTrueType.stbtt_fontinfo* info, int glyph1, int glyph2)
		{
			byte* ptr = info->data + info->kern;
			if (info->kern == 0)
			{
				return 0;
			}
			if (StbTrueType.ttUSHORT(ptr + 2) < 1)
			{
				return 0;
			}
			if (StbTrueType.ttUSHORT(ptr + 8) != 1)
			{
				return 0;
			}
			int i = 0;
			int num = (int)(StbTrueType.ttUSHORT(ptr + 10) - 1);
			uint num2 = (uint)((glyph1 << 16) | glyph2);
			while (i <= num)
			{
				int num3 = i + num >> 1;
				uint num4 = StbTrueType.ttULONG(ptr + 18 + num3 * 6);
				if (num2 < num4)
				{
					num = num3 - 1;
				}
				else
				{
					if (num2 <= num4)
					{
						return (int)StbTrueType.ttSHORT(ptr + 22 + num3 * 6);
					}
					i = num3 + 1;
				}
			}
			return 0;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00014B4C File Offset: 0x00012D4C
		public unsafe static int stbtt__GetCoverageIndex(byte* coverageTable, int glyph)
		{
			ushort num = StbTrueType.ttUSHORT(coverageTable);
			if (num != 1)
			{
				if (num == 2)
				{
					int num2 = (int)StbTrueType.ttUSHORT(coverageTable + 2);
					byte* ptr = coverageTable + 4;
					int i = 0;
					int num3 = num2 - 1;
					while (i <= num3)
					{
						int num4 = i + num3 >> 1;
						byte* ptr2 = ptr + 6 * num4;
						int num5 = (int)StbTrueType.ttUSHORT(ptr2);
						int num6 = (int)StbTrueType.ttUSHORT(ptr2 + 2);
						if (glyph < num5)
						{
							num3 = num4 - 1;
						}
						else
						{
							if (glyph <= num6)
							{
								return (int)StbTrueType.ttUSHORT(ptr2 + 4) + glyph - num5;
							}
							i = num4 + 1;
						}
					}
				}
			}
			else
			{
				int num7 = (int)StbTrueType.ttUSHORT(coverageTable + 2);
				int j = 0;
				int num8 = num7 - 1;
				while (j <= num8)
				{
					byte* ptr3 = coverageTable + 4;
					int num9 = j + num8 >> 1;
					int num10 = (int)StbTrueType.ttUSHORT(ptr3 + 2 * num9);
					if (glyph < num10)
					{
						num8 = num9 - 1;
					}
					else
					{
						if (glyph <= num10)
						{
							return num9;
						}
						j = num9 + 1;
					}
				}
			}
			return -1;
		}

		// Token: 0x06000163 RID: 355 RVA: 0x00014C24 File Offset: 0x00012E24
		public unsafe static int stbtt__GetGlyphClass(byte* classDefTable, int glyph)
		{
			ushort num = StbTrueType.ttUSHORT(classDefTable);
			if (num != 1)
			{
				if (num == 2)
				{
					ushort num2 = StbTrueType.ttUSHORT(classDefTable + 2);
					byte* ptr = classDefTable + 4;
					int i = 0;
					int num3 = (int)(num2 - 1);
					while (i <= num3)
					{
						int num4 = i + num3 >> 1;
						byte* ptr2 = ptr + 6 * num4;
						int num5 = (int)StbTrueType.ttUSHORT(ptr2);
						int num6 = (int)StbTrueType.ttUSHORT(ptr2 + 2);
						if (glyph < num5)
						{
							num3 = num4 - 1;
						}
						else
						{
							if (glyph <= num6)
							{
								return (int)StbTrueType.ttUSHORT(ptr2 + 4);
							}
							i = num4 + 1;
						}
					}
					classDefTable = ptr + 6 * num2;
				}
			}
			else
			{
				ushort num7 = StbTrueType.ttUSHORT(classDefTable + 2);
				ushort num8 = StbTrueType.ttUSHORT(classDefTable + 4);
				byte* ptr3 = classDefTable + 6;
				if (glyph >= (int)num7 && glyph < (int)(num7 + num8))
				{
					return (int)StbTrueType.ttUSHORT(ptr3 + 2 * (glyph - (int)num7));
				}
				classDefTable = ptr3 + 2 * num8;
			}
			return -1;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00014CF4 File Offset: 0x00012EF4
		public unsafe static int stbtt__GetGlyphGPOSInfoAdvance(StbTrueType.stbtt_fontinfo* info, int glyph1, int glyph2)
		{
			if (info->gpos == 0)
			{
				return 0;
			}
			byte* ptr = info->data + info->gpos;
			if (StbTrueType.ttUSHORT(ptr) != 1)
			{
				return 0;
			}
			if (StbTrueType.ttUSHORT(ptr + 2) != 0)
			{
				return 0;
			}
			ushort num = StbTrueType.ttUSHORT(ptr + 8);
			byte* ptr2 = ptr + num;
			ushort num2 = StbTrueType.ttUSHORT(ptr2);
			for (int i = 0; i < (int)num2; i++)
			{
				ushort num3 = StbTrueType.ttUSHORT(ptr2 + 2 + 2 * i);
				byte* ptr3 = ptr2 + num3;
				ushort num4 = StbTrueType.ttUSHORT(ptr3);
				ushort num5 = StbTrueType.ttUSHORT(ptr3 + 4);
				byte* ptr4 = ptr3 + 6;
				if (num4 == 2)
				{
					for (int j = 0; j < (int)num5; j++)
					{
						ushort num6 = StbTrueType.ttUSHORT(ptr4 + 2 * j);
						byte* ptr5 = ptr3 + num6;
						ushort num7 = StbTrueType.ttUSHORT(ptr5);
						ushort num8 = StbTrueType.ttUSHORT(ptr5 + 2);
						int num9 = StbTrueType.stbtt__GetCoverageIndex(ptr5 + num8, glyph1);
						if (num9 != -1)
						{
							if (num7 != 1)
							{
								if (num7 == 2)
								{
									ushort num10 = StbTrueType.ttUSHORT(ptr5 + 4);
									ushort num11 = StbTrueType.ttUSHORT(ptr5 + 6);
									ushort num12 = StbTrueType.ttUSHORT(ptr5 + 8);
									ushort num13 = StbTrueType.ttUSHORT(ptr5 + 10);
									int num14 = StbTrueType.stbtt__GetGlyphClass(ptr5 + num12, glyph1);
									int num15 = StbTrueType.stbtt__GetGlyphClass(ptr5 + num13, glyph2);
									ushort num16 = StbTrueType.ttUSHORT(ptr5 + 12);
									ushort num17 = StbTrueType.ttUSHORT(ptr5 + 14);
									if (num10 != 4)
									{
										return 0;
									}
									if (num11 != 0)
									{
										return 0;
									}
									if (num14 >= 0 && num14 < (int)num16 && num15 >= 0 && num15 < (int)num17)
									{
										return (int)StbTrueType.ttSHORT(ptr5 + 16 + 2 * (num14 * (int)num17) + 2 * num15);
									}
								}
							}
							else
							{
								ushort num18 = StbTrueType.ttUSHORT(ptr5 + 4);
								ushort num19 = StbTrueType.ttUSHORT(ptr5 + 6);
								int num20 = 2;
								StbTrueType.ttUSHORT(ptr5 + 8);
								ushort num21 = StbTrueType.ttUSHORT(ptr5 + 10 + 2 * num9);
								byte* ptr6 = ptr5 + num21;
								ushort num22 = StbTrueType.ttUSHORT(ptr6);
								byte* ptr7 = ptr6 + 2;
								if (num18 != 4)
								{
									return 0;
								}
								if (num19 != 0)
								{
									return 0;
								}
								int num23 = (int)(num22 - 1);
								int k = 0;
								while (k <= num23)
								{
									int num24 = k + num23 >> 1;
									byte* ptr8 = ptr7 + (2 + num20) * num24;
									int num25 = (int)StbTrueType.ttUSHORT(ptr8);
									if (glyph2 < num25)
									{
										num23 = num24 - 1;
									}
									else
									{
										if (glyph2 <= num25)
										{
											return (int)StbTrueType.ttSHORT(ptr8 + 2);
										}
										k = num24 + 1;
									}
								}
							}
						}
					}
				}
			}
			return 0;
		}

		// Token: 0x06000165 RID: 357 RVA: 0x00014F44 File Offset: 0x00013144
		public unsafe static int stbtt_GetGlyphKernAdvance(StbTrueType.stbtt_fontinfo* info, int g1, int g2)
		{
			int num = 0;
			if (info->gpos != 0)
			{
				num += StbTrueType.stbtt__GetGlyphGPOSInfoAdvance(info, g1, g2);
			}
			if (info->kern != 0)
			{
				num += StbTrueType.stbtt__GetGlyphKernInfoAdvance(info, g1, g2);
			}
			return num;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00014F7A File Offset: 0x0001317A
		public unsafe static int stbtt_GetCodepointKernAdvance(StbTrueType.stbtt_fontinfo* info, int ch1, int ch2)
		{
			if (info->kern == 0 && info->gpos == 0)
			{
				return 0;
			}
			return StbTrueType.stbtt_GetGlyphKernAdvance(info, StbTrueType.stbtt_FindGlyphIndex(info, ch1), StbTrueType.stbtt_FindGlyphIndex(info, ch2));
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00014FA2 File Offset: 0x000131A2
		public unsafe static void stbtt_GetCodepointHMetrics(StbTrueType.stbtt_fontinfo* info, int codepoint, int* advanceWidth, int* leftSideBearing)
		{
			StbTrueType.stbtt_GetGlyphHMetrics(info, StbTrueType.stbtt_FindGlyphIndex(info, codepoint), advanceWidth, leftSideBearing);
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00014FB4 File Offset: 0x000131B4
		public unsafe static void stbtt_GetFontVMetrics(StbTrueType.stbtt_fontinfo* info, int* ascent, int* descent, int* lineGap)
		{
			if (ascent != null)
			{
				*ascent = (int)StbTrueType.ttSHORT(info->data + info->hhea + 4);
			}
			if (descent != null)
			{
				*descent = (int)StbTrueType.ttSHORT(info->data + info->hhea + 6);
			}
			if (lineGap != null)
			{
				*lineGap = (int)StbTrueType.ttSHORT(info->data + info->hhea + 8);
			}
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00015014 File Offset: 0x00013214
		public unsafe static int stbtt_GetFontVMetricsOS2(StbTrueType.stbtt_fontinfo* info, int* typoAscent, int* typoDescent, int* typoLineGap)
		{
			int num = (int)StbTrueType.stbtt__find_table(info->data, (uint)info->fontstart, "OS/2");
			if (num == 0)
			{
				return 0;
			}
			if (typoAscent != null)
			{
				*typoAscent = (int)StbTrueType.ttSHORT(info->data + num + 68);
			}
			if (typoDescent != null)
			{
				*typoDescent = (int)StbTrueType.ttSHORT(info->data + num + 70);
			}
			if (typoLineGap != null)
			{
				*typoLineGap = (int)StbTrueType.ttSHORT(info->data + num + 72);
			}
			return 1;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00015084 File Offset: 0x00013284
		public unsafe static void stbtt_GetFontBoundingBox(StbTrueType.stbtt_fontinfo* info, int* x0, int* y0, int* x1, int* y1)
		{
			*x0 = (int)StbTrueType.ttSHORT(info->data + info->head + 36);
			*y0 = (int)StbTrueType.ttSHORT(info->data + info->head + 38);
			*x1 = (int)StbTrueType.ttSHORT(info->data + info->head + 40);
			*y1 = (int)StbTrueType.ttSHORT(info->data + info->head + 42);
		}

		// Token: 0x0600016B RID: 363 RVA: 0x000150F0 File Offset: 0x000132F0
		public unsafe static float stbtt_ScaleForPixelHeight(StbTrueType.stbtt_fontinfo* info, float height)
		{
			int num = (int)(StbTrueType.ttSHORT(info->data + info->hhea + 4) - StbTrueType.ttSHORT(info->data + info->hhea + 6));
			return height / (float)num;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0001512C File Offset: 0x0001332C
		public unsafe static float stbtt_ScaleForMappingEmToPixels(StbTrueType.stbtt_fontinfo* info, float pixels)
		{
			int num = (int)StbTrueType.ttUSHORT(info->data + info->head + 18);
			return pixels / (float)num;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00015154 File Offset: 0x00013354
		public unsafe static void stbtt_FreeShape(StbTrueType.stbtt_fontinfo* info, StbTrueType.stbtt_vertex* v)
		{
			CRuntime.free((void*)v);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0001515C File Offset: 0x0001335C
		public unsafe static void stbtt_GetGlyphBitmapBoxSubpixel(StbTrueType.stbtt_fontinfo* font, int glyph, float scale_x, float scale_y, float shift_x, float shift_y, int* ix0, int* iy0, int* ix1, int* iy1)
		{
			int num = 0;
			int num2 = 0;
			int num3;
			int num4;
			if (StbTrueType.stbtt_GetGlyphBox(font, glyph, &num, &num2, &num3, &num4) == 0)
			{
				if (ix0 != null)
				{
					*ix0 = 0;
				}
				if (iy0 != null)
				{
					*iy0 = 0;
				}
				if (ix1 != null)
				{
					*ix1 = 0;
				}
				if (iy1 != null)
				{
					*iy1 = 0;
					return;
				}
			}
			else
			{
				if (ix0 != null)
				{
					*ix0 = (int)CRuntime.floor((double)((float)num * scale_x + shift_x));
				}
				if (iy0 != null)
				{
					*iy0 = (int)CRuntime.floor((double)((float)(-(float)num4) * scale_y + shift_y));
				}
				if (ix1 != null)
				{
					*ix1 = (int)CRuntime.ceil((double)((float)num3 * scale_x + shift_x));
				}
				if (iy1 != null)
				{
					*iy1 = (int)CRuntime.ceil((double)((float)(-(float)num2) * scale_y + shift_y));
				}
			}
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0001520C File Offset: 0x0001340C
		public unsafe static void stbtt_GetGlyphBitmapBox(StbTrueType.stbtt_fontinfo* font, int glyph, float scale_x, float scale_y, int* ix0, int* iy0, int* ix1, int* iy1)
		{
			StbTrueType.stbtt_GetGlyphBitmapBoxSubpixel(font, glyph, scale_x, scale_y, 0f, 0f, ix0, iy0, ix1, iy1);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00015238 File Offset: 0x00013438
		public unsafe static void stbtt_GetCodepointBitmapBoxSubpixel(StbTrueType.stbtt_fontinfo* font, int codepoint, float scale_x, float scale_y, float shift_x, float shift_y, int* ix0, int* iy0, int* ix1, int* iy1)
		{
			StbTrueType.stbtt_GetGlyphBitmapBoxSubpixel(font, StbTrueType.stbtt_FindGlyphIndex(font, codepoint), scale_x, scale_y, shift_x, shift_y, ix0, iy0, ix1, iy1);
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00015264 File Offset: 0x00013464
		public unsafe static void stbtt_GetCodepointBitmapBox(StbTrueType.stbtt_fontinfo* font, int codepoint, float scale_x, float scale_y, int* ix0, int* iy0, int* ix1, int* iy1)
		{
			StbTrueType.stbtt_GetCodepointBitmapBoxSubpixel(font, codepoint, scale_x, scale_y, 0f, 0f, ix0, iy0, ix1, iy1);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00015290 File Offset: 0x00013490
		public unsafe static void* stbtt__hheap_alloc(StbTrueType.stbtt__hheap* hh, ulong size, void* userdata)
		{
			if (hh->first_free != null)
			{
				void* first_free = hh->first_free;
				hh->first_free = *(IntPtr*)first_free;
				return first_free;
			}
			if (hh->num_remaining_in_head_chunk == 0)
			{
				int num = ((size < 32UL) ? 2000 : ((size < 128UL) ? 800 : 100));
				StbTrueType.stbtt__hheap_chunk* ptr = (StbTrueType.stbtt__hheap_chunk*)CRuntime.malloc((ulong)((long)sizeof(StbTrueType.stbtt__hheap_chunk) + (long)(size * (ulong)((long)num))));
				if (ptr == null)
				{
					return null;
				}
				ptr->next = hh->head;
				hh->head = ptr;
				hh->num_remaining_in_head_chunk = num;
			}
			hh->num_remaining_in_head_chunk = hh->num_remaining_in_head_chunk - 1;
			return (void*)(hh->head + 1 + size * (ulong)((long)hh->num_remaining_in_head_chunk) / (ulong)sizeof(StbTrueType.stbtt__hheap_chunk));
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00015337 File Offset: 0x00013537
		public unsafe static void stbtt__hheap_free(StbTrueType.stbtt__hheap* hh, void* p)
		{
			*(IntPtr*)p = hh->first_free;
			hh->first_free = p;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00015348 File Offset: 0x00013548
		public unsafe static void stbtt__hheap_cleanup(StbTrueType.stbtt__hheap* hh, void* userdata)
		{
			StbTrueType.stbtt__hheap_chunk* next;
			for (StbTrueType.stbtt__hheap_chunk* ptr = hh->head; ptr != null; ptr = next)
			{
				next = ptr->next;
				CRuntime.free((void*)ptr);
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00015374 File Offset: 0x00013574
		public unsafe static StbTrueType.stbtt__active_edge* stbtt__new_active(StbTrueType.stbtt__hheap* hh, StbTrueType.stbtt__edge* e, int off_x, float start_point, void* userdata)
		{
			StbTrueType.stbtt__active_edge* ptr = (StbTrueType.stbtt__active_edge*)StbTrueType.stbtt__hheap_alloc(hh, (ulong)((long)sizeof(StbTrueType.stbtt__active_edge)), userdata);
			float num = (e->x1 - e->x0) / (e->y1 - e->y0);
			if (ptr == null)
			{
				return ptr;
			}
			ptr->fdx = num;
			ptr->fdy = ((num != 0f) ? (1f / num) : 0f);
			ptr->fx = e->x0 + num * (start_point - e->y0);
			ptr->fx -= (float)off_x;
			ptr->direction = ((e->invert != 0) ? 1f : (-1f));
			ptr->sy = e->y0;
			ptr->ey = e->y1;
			ptr->next = null;
			return ptr;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0001543C File Offset: 0x0001363C
		public unsafe static void stbtt__handle_clipped_edge(float* scanline, int x, StbTrueType.stbtt__active_edge* e, float x0, float y0, float x1, float y1)
		{
			if (y0 == y1)
			{
				return;
			}
			if (y0 > e->ey)
			{
				return;
			}
			if (y1 < e->sy)
			{
				return;
			}
			if (y0 < e->sy)
			{
				x0 += (x1 - x0) * (e->sy - y0) / (y1 - y0);
				y0 = e->sy;
			}
			if (y1 > e->ey)
			{
				x1 += (x1 - x0) * (e->ey - y1) / (y1 - y0);
				y1 = e->ey;
			}
			if (x0 != (float)x && x0 != (float)(x + 1) && x0 > (float)x)
			{
				float num = (float)(x + 1);
			}
			if (x0 <= (float)x && x1 <= (float)x)
			{
				scanline[x] += e->direction * (y1 - y0);
				return;
			}
			if (x0 < (float)(x + 1) || x1 < (float)(x + 1))
			{
				scanline[x] += e->direction * (y1 - y0) * (1f - (x0 - (float)x + (x1 - (float)x)) / 2f);
			}
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00015538 File Offset: 0x00013738
		public unsafe static void stbtt__fill_active_edges_new(float* scanline, float* scanline_fill, int len, StbTrueType.stbtt__active_edge* e, float y_top)
		{
			float num = y_top + 1f;
			while (e != null)
			{
				if (e->fdx == 0f)
				{
					float num2 = e->fx;
					if (num2 < (float)len)
					{
						if (num2 >= 0f)
						{
							StbTrueType.stbtt__handle_clipped_edge(scanline, (int)num2, e, num2, y_top, num2, num);
							StbTrueType.stbtt__handle_clipped_edge(scanline_fill - 1, (int)num2 + 1, e, num2, y_top, num2, num);
						}
						else
						{
							StbTrueType.stbtt__handle_clipped_edge(scanline_fill - 1, 0, e, num2, y_top, num2, num);
						}
					}
				}
				else
				{
					float num3 = e->fx;
					float num4 = e->fdx;
					float num5 = num3 + num4;
					float num6 = e->fdy;
					float num7;
					float num8;
					if (e->sy > y_top)
					{
						num7 = num3 + num4 * (e->sy - y_top);
						num8 = e->sy;
					}
					else
					{
						num7 = num3;
						num8 = y_top;
					}
					float num9;
					float num10;
					if (e->ey < num)
					{
						num9 = num3 + num4 * (e->ey - y_top);
						num10 = e->ey;
					}
					else
					{
						num9 = num5;
						num10 = num;
					}
					if (num7 >= 0f && num9 >= 0f && num7 < (float)len && num9 < (float)len)
					{
						if ((int)num7 == (int)num9)
						{
							int num11 = (int)num7;
							float num12 = num10 - num8;
							scanline[num11] += e->direction * (1f - (num7 - (float)num11 + (num9 - (float)num11)) / 2f) * num12;
							scanline_fill[num11] += e->direction * num12;
						}
						else
						{
							if (num7 > num9)
							{
								num8 = num - (num8 - y_top);
								num10 = num - (num10 - y_top);
								float num13 = num8;
								num8 = num10;
								num10 = num13;
								float num14 = num9;
								num9 = num7;
								num7 = num14;
								num4 = -num4;
								num6 = -num6;
								float num15 = num3;
								num3 = num5;
								num5 = num15;
							}
							int num16 = (int)num7;
							int num17 = (int)num9;
							float num18 = ((float)(num16 + 1) - num3) * num6 + y_top;
							float num19 = e->direction;
							float num20 = num19 * (num18 - num8);
							scanline[num16] += num20 * (1f - (num7 - (float)num16 + (float)(num16 + 1 - num16)) / 2f);
							float num21 = num19 * num6;
							for (int i = num16 + 1; i < num17; i++)
							{
								scanline[i] += num20 + num21 / 2f;
								num20 += num21;
							}
							num18 += num6 * (float)(num17 - (num16 + 1));
							scanline[num17] += num20 + num19 * (1f - ((float)(num17 - num17) + (num9 - (float)num17)) / 2f) * (num10 - num18);
							scanline_fill[num17] += num19 * (num10 - num8);
						}
					}
					else
					{
						for (int j = 0; j < len; j++)
						{
							float num22 = y_top;
							float num23 = (float)j;
							float num24 = (float)(j + 1);
							float num25 = num5;
							float num26 = num;
							float num27 = ((float)j - num3) / num4 + y_top;
							float num28 = ((float)(j + 1) - num3) / num4 + y_top;
							if (num3 < num23 && num25 > num24)
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num23, num27);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num23, num27, num24, num28);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num24, num28, num25, num26);
							}
							else if (num25 < num23 && num3 > num24)
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num24, num28);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num24, num28, num23, num27);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num23, num27, num25, num26);
							}
							else if (num3 < num23 && num25 > num23)
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num23, num27);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num23, num27, num25, num26);
							}
							else if (num25 < num23 && num3 > num23)
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num23, num27);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num23, num27, num25, num26);
							}
							else if (num3 < num24 && num25 > num24)
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num24, num28);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num24, num28, num25, num26);
							}
							else if (num25 < num24 && num3 > num24)
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num24, num28);
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num24, num28, num25, num26);
							}
							else
							{
								StbTrueType.stbtt__handle_clipped_edge(scanline, j, e, num3, num22, num25, num26);
							}
						}
					}
				}
				e = e->next;
			}
		}

		// Token: 0x06000178 RID: 376 RVA: 0x000159DC File Offset: 0x00013BDC
		public unsafe static void stbtt__rasterize_sorted_edges(StbTrueType.stbtt__bitmap* result, StbTrueType.stbtt__edge* e, int n, int vsubsample, int off_x, int off_y, void* userdata)
		{
			StbTrueType.stbtt__active_edge* ptr = null;
			int i = 0;
			float* ptr2 = stackalloc float[(UIntPtr)516];
			float* ptr3;
			if (result->w > 64)
			{
				ptr3 = (float*)CRuntime.malloc((ulong)((long)((result->w * 2 + 1) * 4)));
			}
			else
			{
				ptr3 = ptr2;
			}
			float* ptr4 = ptr3 + result->w;
			int num = off_y;
			e[n].y0 = (float)(off_y + result->h) + 1f;
			StbTrueType.stbtt__hheap stbtt__hheap;
			while (i < result->h)
			{
				float num2 = (float)num + 0f;
				float num3 = (float)num + 1f;
				StbTrueType.stbtt__active_edge** ptr5 = &ptr;
				CRuntime.memset((void*)ptr3, 0, (ulong)((long)(result->w * 4)));
				CRuntime.memset((void*)ptr4, 0, (ulong)((long)((result->w + 1) * 4)));
				while (*(IntPtr*)ptr5 != (IntPtr)((UIntPtr)0))
				{
					StbTrueType.stbtt__active_edge* ptr6 = *(IntPtr*)ptr5;
					if (ptr6->ey <= num2)
					{
						*(IntPtr*)ptr5 = ptr6->next;
						ptr6->direction = 0f;
						StbTrueType.stbtt__hheap_free(&stbtt__hheap, (void*)ptr6);
					}
					else
					{
						ptr5 = &((IntPtr*)ptr5)->next;
					}
				}
				while (e->y0 <= num3)
				{
					if (e->y0 != e->y1)
					{
						StbTrueType.stbtt__active_edge* ptr7 = StbTrueType.stbtt__new_active(&stbtt__hheap, e, off_x, num2, userdata);
						if (ptr7 != null)
						{
							ptr7->next = ptr;
							ptr = ptr7;
						}
					}
					e++;
				}
				if (ptr != null)
				{
					StbTrueType.stbtt__fill_active_edges_new(ptr3, ptr4 + 1, result->w, ptr, num2);
				}
				float num4 = 0f;
				for (int j = 0; j < result->w; j++)
				{
					num4 += ptr4[j];
					int num5 = (int)(CRuntime.fabs((double)(ptr3[j] + num4)) * 255f + 0.5f);
					if (num5 > 255)
					{
						num5 = 255;
					}
					result->pixels[i * result->stride + j] = (byte)num5;
				}
				ptr5 = &ptr;
				while (*(IntPtr*)ptr5 != (IntPtr)((UIntPtr)0))
				{
					StbTrueType.stbtt__active_edge* ptr8 = *(IntPtr*)ptr5;
					ptr8->fx += ptr8->fdx;
					ptr5 = &((IntPtr*)ptr5)->next;
				}
				num++;
				i++;
			}
			StbTrueType.stbtt__hheap_cleanup(&stbtt__hheap, userdata);
			if (ptr3 != ptr2)
			{
				CRuntime.free((void*)ptr3);
			}
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00015C0C File Offset: 0x00013E0C
		public unsafe static void stbtt__sort_edges_ins_sort(StbTrueType.stbtt__edge* p, int n)
		{
			for (int i = 1; i < n; i++)
			{
				StbTrueType.stbtt__edge stbtt__edge = p[i];
				StbTrueType.stbtt__edge* ptr = &stbtt__edge;
				int j;
				for (j = i; j > 0; j--)
				{
					StbTrueType.stbtt__edge* ptr2 = p + (j - 1);
					if (ptr->y0 >= ptr2->y0)
					{
						break;
					}
					p[j] = p[j - 1];
				}
				if (i != j)
				{
					p[j] = stbtt__edge;
				}
			}
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00015CA8 File Offset: 0x00013EA8
		public unsafe static void stbtt__sort_edges_quicksort(StbTrueType.stbtt__edge* p, int n)
		{
			while (n > 12)
			{
				StbTrueType.stbtt__edge stbtt__edge = default(StbTrueType.stbtt__edge);
				int num = n >> 1;
				int num2 = ((p->y0 < p[num].y0) ? 1 : 0);
				int num3 = ((p[num].y0 < p[n - 1].y0) ? 1 : 0);
				if (num2 != num3)
				{
					int num4 = ((((p->y0 < p[n - 1].y0) ? 1 : 0) == num3) ? 0 : (n - 1));
					stbtt__edge = p[num4];
					p[num4] = p[num];
					p[num] = stbtt__edge;
				}
				stbtt__edge = *p;
				*p = p[num];
				p[num] = stbtt__edge;
				int num5 = 1;
				int num6 = n - 1;
				for (;;)
				{
					num5 = num5;
					while (p[num5].y0 < p->y0)
					{
						num5++;
					}
					num6 = num6;
					while (p->y0 < p[num6].y0)
					{
						num6--;
					}
					if (num5 >= num6)
					{
						break;
					}
					stbtt__edge = p[num5];
					p[num5] = p[num6];
					p[num6] = stbtt__edge;
					num5++;
					num6--;
				}
				if (num6 < n - num5)
				{
					StbTrueType.stbtt__sort_edges_quicksort(p, num6);
					p += num5;
					n -= num5;
				}
				else
				{
					StbTrueType.stbtt__sort_edges_quicksort(p + num5, n - num5);
					n = num6;
				}
			}
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00015E99 File Offset: 0x00014099
		public unsafe static void stbtt__sort_edges(StbTrueType.stbtt__edge* p, int n)
		{
			StbTrueType.stbtt__sort_edges_quicksort(p, n);
			StbTrueType.stbtt__sort_edges_ins_sort(p, n);
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00015EAC File Offset: 0x000140AC
		public unsafe static void stbtt__rasterize(StbTrueType.stbtt__bitmap* result, StbTrueType.stbtt__point* pts, int* wcount, int windings, float scale_x, float scale_y, float shift_x, float shift_y, int off_x, int off_y, int invert, void* userdata)
		{
			float num = ((invert != 0) ? (-scale_y) : scale_y);
			int num2 = 1;
			int num3 = 0;
			for (int i = 0; i < windings; i++)
			{
				num3 += wcount[i];
			}
			StbTrueType.stbtt__edge* ptr = (StbTrueType.stbtt__edge*)CRuntime.malloc((ulong)((long)(sizeof(StbTrueType.stbtt__edge) * (num3 + 1))));
			if (ptr == null)
			{
				return;
			}
			num3 = 0;
			int num4 = 0;
			for (int i = 0; i < windings; i++)
			{
				StbTrueType.stbtt__point* ptr2 = pts + num4;
				num4 += wcount[i];
				int num5 = wcount[i] - 1;
				int j = 0;
				while (j < wcount[i])
				{
					int num6 = j;
					int num7 = num5;
					if (ptr2[num5].y != ptr2[j].y)
					{
						ptr[num3].invert = 0;
						if ((invert != 0 && ptr2[num5].y > ptr2[j].y) || (invert == 0 && ptr2[num5].y < ptr2[j].y))
						{
							ptr[num3].invert = 1;
							num6 = num5;
							num7 = j;
						}
						ptr[num3].x0 = ptr2[num6].x * scale_x + shift_x;
						ptr[num3].y0 = (ptr2[num6].y * num + shift_y) * (float)num2;
						ptr[num3].x1 = ptr2[num7].x * scale_x + shift_x;
						ptr[num3].y1 = (ptr2[num7].y * num + shift_y) * (float)num2;
						num3++;
					}
					num5 = j++;
				}
			}
			StbTrueType.stbtt__sort_edges(ptr, num3);
			StbTrueType.stbtt__rasterize_sorted_edges(result, ptr, num3, num2, off_x, off_y, userdata);
			CRuntime.free((void*)ptr);
		}

		// Token: 0x0600017D RID: 381 RVA: 0x000160CD File Offset: 0x000142CD
		public unsafe static void stbtt__add_point(StbTrueType.stbtt__point* points, int n, float x, float y)
		{
			if (points == null)
			{
				return;
			}
			points[n].x = x;
			points[n].y = y;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x000160FC File Offset: 0x000142FC
		public unsafe static int stbtt__tesselate_curve(StbTrueType.stbtt__point* points, int* num_points, float x0, float y0, float x1, float y1, float x2, float y2, float objspace_flatness_squared, int n)
		{
			float num = (x0 + 2f * x1 + x2) / 4f;
			float num2 = (y0 + 2f * y1 + y2) / 4f;
			float num3 = (x0 + x2) / 2f - num;
			float num4 = (y0 + y2) / 2f - num2;
			if (n > 16)
			{
				return 1;
			}
			if (num3 * num3 + num4 * num4 > objspace_flatness_squared)
			{
				StbTrueType.stbtt__tesselate_curve(points, num_points, x0, y0, (x0 + x1) / 2f, (y0 + y1) / 2f, num, num2, objspace_flatness_squared, n + 1);
				StbTrueType.stbtt__tesselate_curve(points, num_points, num, num2, (x1 + x2) / 2f, (y1 + y2) / 2f, x2, y2, objspace_flatness_squared, n + 1);
			}
			else
			{
				StbTrueType.stbtt__add_point(points, *num_points, x2, y2);
				(*num_points)++;
			}
			return 1;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x000161D8 File Offset: 0x000143D8
		public unsafe static void stbtt__tesselate_cubic(StbTrueType.stbtt__point* points, int* num_points, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float objspace_flatness_squared, int n)
		{
			float num = x1 - x0;
			float num2 = y1 - y0;
			float num3 = x2 - x1;
			float num4 = y2 - y1;
			float num5 = x3 - x2;
			float num6 = y3 - y2;
			float num7 = x3 - x0;
			float num8 = y3 - y0;
			float num9 = (float)(CRuntime.sqrt((double)(num * num + num2 * num2)) + CRuntime.sqrt((double)(num3 * num3 + num4 * num4)) + CRuntime.sqrt((double)(num5 * num5 + num6 * num6)));
			float num10 = (float)CRuntime.sqrt((double)(num7 * num7 + num8 * num8));
			float num11 = num9 * num9 - num10 * num10;
			if (n > 16)
			{
				return;
			}
			if (num11 > objspace_flatness_squared)
			{
				float num12 = (x0 + x1) / 2f;
				float num13 = (y0 + y1) / 2f;
				float num14 = (x1 + x2) / 2f;
				float num15 = (y1 + y2) / 2f;
				float num16 = (x2 + x3) / 2f;
				float num17 = (y2 + y3) / 2f;
				float num18 = (num12 + num14) / 2f;
				float num19 = (num13 + num15) / 2f;
				float num20 = (num14 + num16) / 2f;
				float num21 = (num15 + num17) / 2f;
				float num22 = (num18 + num20) / 2f;
				float num23 = (num19 + num21) / 2f;
				StbTrueType.stbtt__tesselate_cubic(points, num_points, x0, y0, num12, num13, num18, num19, num22, num23, objspace_flatness_squared, n + 1);
				StbTrueType.stbtt__tesselate_cubic(points, num_points, num22, num23, num20, num21, num16, num17, x3, y3, objspace_flatness_squared, n + 1);
				return;
			}
			StbTrueType.stbtt__add_point(points, *num_points, x3, y3);
			(*num_points)++;
		}

		// Token: 0x06000180 RID: 384 RVA: 0x0001637C File Offset: 0x0001457C
		public unsafe static StbTrueType.stbtt__point* stbtt_FlattenCurves(StbTrueType.stbtt_vertex* vertices, int num_verts, float objspace_flatness, int** contour_lengths, int* num_contours, void* userdata)
		{
			StbTrueType.stbtt__point* ptr = null;
			int num = 0;
			float num2 = objspace_flatness * objspace_flatness;
			int num3 = 0;
			int num4 = 0;
			for (int i = 0; i < num_verts; i++)
			{
				if (vertices[i].type == 1)
				{
					num3++;
				}
			}
			*num_contours = num3;
			if (num3 == 0)
			{
				return null;
			}
			*(IntPtr*)contour_lengths = CRuntime.malloc((ulong)((long)(4 * num3)));
			if (*(IntPtr*)contour_lengths == (IntPtr)((UIntPtr)0))
			{
				*num_contours = 0;
				return null;
			}
			for (int j = 0; j < 2; j++)
			{
				float num5 = 0f;
				float num6 = 0f;
				if (j == 1)
				{
					ptr = (StbTrueType.stbtt__point*)CRuntime.malloc((ulong)((long)(num * sizeof(StbTrueType.stbtt__point))));
					if (ptr == null)
					{
						CRuntime.free((void*)ptr);
						CRuntime.free(*(IntPtr*)contour_lengths);
						*(IntPtr*)contour_lengths = (IntPtr)((UIntPtr)0);
						*num_contours = 0;
						return null;
					}
				}
				num = 0;
				num3 = -1;
				for (int i = 0; i < num_verts; i++)
				{
					switch (vertices[i].type)
					{
					case 1:
						if (num3 >= 0)
						{
							*(*(IntPtr*)contour_lengths + (IntPtr)num3 * 4) = num - num4;
						}
						num3++;
						num4 = num;
						num5 = (float)vertices[i].x;
						num6 = (float)vertices[i].y;
						StbTrueType.stbtt__add_point(ptr, num++, num5, num6);
						break;
					case 2:
						num5 = (float)vertices[i].x;
						num6 = (float)vertices[i].y;
						StbTrueType.stbtt__add_point(ptr, num++, num5, num6);
						break;
					case 3:
						StbTrueType.stbtt__tesselate_curve(ptr, &num, num5, num6, (float)vertices[i].cx, (float)vertices[i].cy, (float)vertices[i].x, (float)vertices[i].y, num2, 0);
						num5 = (float)vertices[i].x;
						num6 = (float)vertices[i].y;
						break;
					case 4:
						StbTrueType.stbtt__tesselate_cubic(ptr, &num, num5, num6, (float)vertices[i].cx, (float)vertices[i].cy, (float)vertices[i].cx1, (float)vertices[i].cy1, (float)vertices[i].x, (float)vertices[i].y, num2, 0);
						num5 = (float)vertices[i].x;
						num6 = (float)vertices[i].y;
						break;
					}
				}
				*(*(IntPtr*)contour_lengths + (IntPtr)num3 * 4) = num - num4;
			}
			return ptr;
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00016644 File Offset: 0x00014844
		public unsafe static void stbtt_Rasterize(StbTrueType.stbtt__bitmap* result, float flatness_in_pixels, StbTrueType.stbtt_vertex* vertices, int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert, void* userdata)
		{
			float num = ((scale_x > scale_y) ? scale_y : scale_x);
			int windings = 0;
			int* ptr = null;
			StbTrueType.stbtt__point* ptr2 = StbTrueType.stbtt_FlattenCurves(vertices, num_verts, flatness_in_pixels / num, &ptr, &windings, userdata);
			if (ptr2 != null)
			{
				StbTrueType.stbtt__rasterize(result, ptr2, ptr, windings, scale_x, scale_y, shift_x, shift_y, x_off, y_off, invert, userdata);
				CRuntime.free((void*)ptr);
				CRuntime.free((void*)ptr2);
			}
		}

		// Token: 0x06000182 RID: 386 RVA: 0x000166A6 File Offset: 0x000148A6
		public unsafe static void stbtt_FreeBitmap(byte* bitmap, void* userdata)
		{
			CRuntime.free((void*)bitmap);
		}

		// Token: 0x06000183 RID: 387 RVA: 0x000166B0 File Offset: 0x000148B0
		public unsafe static byte* stbtt_GetGlyphBitmapSubpixel(StbTrueType.stbtt_fontinfo* info, float scale_x, float scale_y, float shift_x, float shift_y, int glyph, int* width, int* height, int* xoff, int* yoff)
		{
			StbTrueType.stbtt__bitmap stbtt__bitmap = default(StbTrueType.stbtt__bitmap);
			StbTrueType.stbtt_vertex* ptr;
			int num_verts = StbTrueType.stbtt_GetGlyphShape(info, glyph, &ptr);
			if (scale_x == 0f)
			{
				scale_x = scale_y;
			}
			if (scale_y == 0f)
			{
				if (scale_x == 0f)
				{
					CRuntime.free((void*)ptr);
					return null;
				}
				scale_y = scale_x;
			}
			int num;
			int num2;
			int num3;
			int num4;
			StbTrueType.stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale_x, scale_y, shift_x, shift_y, &num, &num2, &num3, &num4);
			stbtt__bitmap.w = num3 - num;
			stbtt__bitmap.h = num4 - num2;
			stbtt__bitmap.pixels = null;
			if (width != null)
			{
				*width = stbtt__bitmap.w;
			}
			if (height != null)
			{
				*height = stbtt__bitmap.h;
			}
			if (xoff != null)
			{
				*xoff = num;
			}
			if (yoff != null)
			{
				*yoff = num2;
			}
			if (stbtt__bitmap.w != 0 && stbtt__bitmap.h != 0)
			{
				stbtt__bitmap.pixels = (byte*)CRuntime.malloc((ulong)((long)(stbtt__bitmap.w * stbtt__bitmap.h)));
				if (stbtt__bitmap.pixels != null)
				{
					stbtt__bitmap.stride = stbtt__bitmap.w;
					StbTrueType.stbtt_Rasterize(&stbtt__bitmap, 0.35f, ptr, num_verts, scale_x, scale_y, shift_x, shift_y, num, num2, 1, info->userdata);
				}
			}
			CRuntime.free((void*)ptr);
			return stbtt__bitmap.pixels;
		}

		// Token: 0x06000184 RID: 388 RVA: 0x000167E4 File Offset: 0x000149E4
		public unsafe static byte* stbtt_GetGlyphBitmap(StbTrueType.stbtt_fontinfo* info, float scale_x, float scale_y, int glyph, int* width, int* height, int* xoff, int* yoff)
		{
			return StbTrueType.stbtt_GetGlyphBitmapSubpixel(info, scale_x, scale_y, 0f, 0f, glyph, width, height, xoff, yoff);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x00016810 File Offset: 0x00014A10
		public unsafe static void stbtt_MakeGlyphBitmapSubpixel(StbTrueType.stbtt_fontinfo* info, byte* output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int glyph)
		{
			StbTrueType.stbtt_vertex* ptr;
			int num_verts = StbTrueType.stbtt_GetGlyphShape(info, glyph, &ptr);
			StbTrueType.stbtt__bitmap stbtt__bitmap = default(StbTrueType.stbtt__bitmap);
			int x_off;
			int y_off;
			StbTrueType.stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale_x, scale_y, shift_x, shift_y, &x_off, &y_off, null, null);
			stbtt__bitmap.pixels = output;
			stbtt__bitmap.w = out_w;
			stbtt__bitmap.h = out_h;
			stbtt__bitmap.stride = out_stride;
			if (stbtt__bitmap.w != 0 && stbtt__bitmap.h != 0)
			{
				StbTrueType.stbtt_Rasterize(&stbtt__bitmap, 0.35f, ptr, num_verts, scale_x, scale_y, shift_x, shift_y, x_off, y_off, 1, info->userdata);
			}
			CRuntime.free((void*)ptr);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x000168AC File Offset: 0x00014AAC
		public unsafe static void stbtt_MakeGlyphBitmap(StbTrueType.stbtt_fontinfo* info, byte* output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, int glyph)
		{
			StbTrueType.stbtt_MakeGlyphBitmapSubpixel(info, output, out_w, out_h, out_stride, scale_x, scale_y, 0f, 0f, glyph);
		}

		// Token: 0x06000187 RID: 391 RVA: 0x000168D8 File Offset: 0x00014AD8
		public unsafe static byte* stbtt_GetCodepointBitmapSubpixel(StbTrueType.stbtt_fontinfo* info, float scale_x, float scale_y, float shift_x, float shift_y, int codepoint, int* width, int* height, int* xoff, int* yoff)
		{
			return StbTrueType.stbtt_GetGlyphBitmapSubpixel(info, scale_x, scale_y, shift_x, shift_y, StbTrueType.stbtt_FindGlyphIndex(info, codepoint), width, height, xoff, yoff);
		}

		// Token: 0x06000188 RID: 392 RVA: 0x00016904 File Offset: 0x00014B04
		public unsafe static void stbtt_MakeCodepointBitmapSubpixelPrefilter(StbTrueType.stbtt_fontinfo* info, byte* output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int oversample_x, int oversample_y, float* sub_x, float* sub_y, int codepoint)
		{
			StbTrueType.stbtt_MakeGlyphBitmapSubpixelPrefilter(info, output, out_w, out_h, out_stride, scale_x, scale_y, shift_x, shift_y, oversample_x, oversample_y, sub_x, sub_y, StbTrueType.stbtt_FindGlyphIndex(info, codepoint));
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00016938 File Offset: 0x00014B38
		public unsafe static void stbtt_MakeCodepointBitmapSubpixel(StbTrueType.stbtt_fontinfo* info, byte* output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int codepoint)
		{
			StbTrueType.stbtt_MakeGlyphBitmapSubpixel(info, output, out_w, out_h, out_stride, scale_x, scale_y, shift_x, shift_y, StbTrueType.stbtt_FindGlyphIndex(info, codepoint));
		}

		// Token: 0x0600018A RID: 394 RVA: 0x00016964 File Offset: 0x00014B64
		public unsafe static byte* stbtt_GetCodepointBitmap(StbTrueType.stbtt_fontinfo* info, float scale_x, float scale_y, int codepoint, int* width, int* height, int* xoff, int* yoff)
		{
			return StbTrueType.stbtt_GetCodepointBitmapSubpixel(info, scale_x, scale_y, 0f, 0f, codepoint, width, height, xoff, yoff);
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00016990 File Offset: 0x00014B90
		public unsafe static void stbtt_MakeCodepointBitmap(StbTrueType.stbtt_fontinfo* info, byte* output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, int codepoint)
		{
			StbTrueType.stbtt_MakeCodepointBitmapSubpixel(info, output, out_w, out_h, out_stride, scale_x, scale_y, 0f, 0f, codepoint);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x000169BC File Offset: 0x00014BBC
		public unsafe static int stbtt_BakeFontBitmap_internal(byte* data, int offset, float pixel_height, byte* pixels, int pw, int ph, int first_char, int num_chars, StbTrueType.stbtt_bakedchar* chardata)
		{
			StbTrueType.stbtt_fontinfo stbtt_fontinfo = default(StbTrueType.stbtt_fontinfo);
			stbtt_fontinfo.userdata = null;
			if (StbTrueType.stbtt_InitFont(&stbtt_fontinfo, data, offset) == 0)
			{
				return -1;
			}
			CRuntime.memset((void*)pixels, 0, (ulong)((long)(pw * ph)));
			int num2;
			int num = (num2 = 1);
			int num3 = 1;
			float num4 = StbTrueType.stbtt_ScaleForPixelHeight(&stbtt_fontinfo, pixel_height);
			for (int i = 0; i < num_chars; i++)
			{
				int num5 = StbTrueType.stbtt_FindGlyphIndex(&stbtt_fontinfo, first_char + i);
				int num6;
				int num7;
				StbTrueType.stbtt_GetGlyphHMetrics(&stbtt_fontinfo, num5, &num6, &num7);
				int num8;
				int num9;
				int num10;
				int num11;
				StbTrueType.stbtt_GetGlyphBitmapBox(&stbtt_fontinfo, num5, num4, num4, &num8, &num9, &num10, &num11);
				int num12 = num10 - num8;
				int num13 = num11 - num9;
				if (num2 + num12 + 1 >= pw)
				{
					num = num3;
					num2 = 1;
				}
				if (num + num13 + 1 >= ph)
				{
					return -i;
				}
				StbTrueType.stbtt_MakeGlyphBitmap(&stbtt_fontinfo, pixels + num2 + num * pw, num12, num13, pw, num4, num4, num5);
				chardata[i].x0 = (ushort)((short)num2);
				chardata[i].y0 = (ushort)((short)num);
				chardata[i].x1 = (ushort)((short)(num2 + num12));
				chardata[i].y1 = (ushort)((short)(num + num13));
				chardata[i].xadvance = num4 * (float)num6;
				chardata[i].xoff = (float)num8;
				chardata[i].yoff = (float)num9;
				num2 = num2 + num12 + 1;
				if (num + num13 + 1 > num3)
				{
					num3 = num + num13 + 1;
				}
			}
			return num3;
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00016B54 File Offset: 0x00014D54
		public unsafe static void stbtt_GetBakedQuad(StbTrueType.stbtt_bakedchar* chardata, int pw, int ph, int char_index, float* xpos, float* ypos, StbTrueType.stbtt_aligned_quad* q, int opengl_fillrule)
		{
			float num = ((opengl_fillrule != 0) ? 0f : (-0.5f));
			float num2 = 1f / (float)pw;
			float num3 = 1f / (float)ph;
			StbTrueType.stbtt_bakedchar* ptr = chardata + char_index;
			int num4 = (int)CRuntime.floor((double)(*xpos + ptr->xoff + 0.5f));
			int num5 = (int)CRuntime.floor((double)(*ypos + ptr->yoff + 0.5f));
			q->x0 = (float)num4 + num;
			q->y0 = (float)num5 + num;
			q->x1 = (float)(num4 + (int)ptr->x1 - (int)ptr->x0) + num;
			q->y1 = (float)(num5 + (int)ptr->y1 - (int)ptr->y0) + num;
			q->s0 = (float)ptr->x0 * num2;
			q->t0 = (float)ptr->y0 * num3;
			q->s1 = (float)ptr->x1 * num2;
			q->t1 = (float)ptr->y1 * num3;
			*xpos += ptr->xadvance;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00016C66 File Offset: 0x00014E66
		public unsafe static void stbrp_init_target(StbTrueType.stbrp_context* con, int pw, int ph, StbTrueType.stbrp_node* nodes, int num_nodes)
		{
			con->width = pw;
			con->height = ph;
			con->x = 0;
			con->y = 0;
			con->bottom_y = 0;
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00016C8C File Offset: 0x00014E8C
		public unsafe static void stbrp_pack_rects(StbTrueType.stbrp_context* con, StbTrueType.stbrp_rect* rects, int num_rects)
		{
			for (int i = 0; i < num_rects; i++)
			{
				if (con->x + rects[i].w > con->width)
				{
					con->x = 0;
					con->y = con->bottom_y;
				}
				if (con->y + rects[i].h > con->height)
				{
					IL_110:
					while (i < num_rects)
					{
						rects[i].was_packed = 0;
						i++;
					}
					return;
				}
				rects[i].x = con->x;
				rects[i].y = con->y;
				rects[i].was_packed = 1;
				con->x = con->x + rects[i].w;
				if (con->y + rects[i].h > con->bottom_y)
				{
					con->bottom_y = con->y + rects[i].h;
				}
			}
			goto IL_110;
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00016DB0 File Offset: 0x00014FB0
		public unsafe static int stbtt_PackBegin(StbTrueType.stbtt_pack_context* spc, byte* pixels, int pw, int ph, int stride_in_bytes, int padding, void* alloc_context)
		{
			StbTrueType.stbrp_context* ptr = (StbTrueType.stbrp_context*)CRuntime.malloc((ulong)((long)sizeof(StbTrueType.stbrp_context)));
			int num = pw - padding;
			StbTrueType.stbrp_node* ptr2 = (StbTrueType.stbrp_node*)CRuntime.malloc((ulong)((long)(sizeof(StbTrueType.stbrp_node) * num)));
			if (ptr == null || ptr2 == null)
			{
				if (ptr != null)
				{
					CRuntime.free((void*)ptr);
				}
				if (ptr2 != null)
				{
					CRuntime.free((void*)ptr2);
				}
				return 0;
			}
			spc->user_allocator_context = alloc_context;
			spc->width = pw;
			spc->height = ph;
			spc->pixels = pixels;
			spc->pack_info = (void*)ptr;
			spc->nodes = (void*)ptr2;
			spc->padding = padding;
			spc->stride_in_bytes = ((stride_in_bytes != 0) ? stride_in_bytes : pw);
			spc->h_oversample = 1U;
			spc->v_oversample = 1U;
			StbTrueType.stbrp_init_target(ptr, pw - padding, ph - padding, ptr2, num);
			if (pixels != null)
			{
				CRuntime.memset((void*)pixels, 0, (ulong)((long)(pw * ph)));
			}
			return 1;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00016E71 File Offset: 0x00015071
		public unsafe static void stbtt_PackEnd(StbTrueType.stbtt_pack_context* spc)
		{
			CRuntime.free(spc->nodes);
			CRuntime.free(spc->pack_info);
		}

		// Token: 0x06000192 RID: 402 RVA: 0x00016E89 File Offset: 0x00015089
		public unsafe static void stbtt_PackSetOversampling(StbTrueType.stbtt_pack_context* spc, uint h_oversample, uint v_oversample)
		{
			if (h_oversample <= 8U)
			{
				spc->h_oversample = h_oversample;
			}
			if (v_oversample <= 8U)
			{
				spc->v_oversample = v_oversample;
			}
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00016EA4 File Offset: 0x000150A4
		public unsafe static void stbtt__h_prefilter(byte* pixels, int w, int h, int stride_in_bytes, uint kernel_width)
		{
			byte* ptr = stackalloc byte[(UIntPtr)8];
			int num = (int)((long)w - (long)((ulong)kernel_width));
			CRuntime.memset((void*)ptr, 0, 8UL);
			for (int i = 0; i < h; i++)
			{
				CRuntime.memset((void*)ptr, 0, (ulong)kernel_width);
				uint num2 = 0U;
				int j;
				switch (kernel_width)
				{
				case 2U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j];
						pixels[j] = (byte)(num2 / 2U);
					}
					break;
				case 3U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j];
						pixels[j] = (byte)(num2 / 3U);
					}
					break;
				case 4U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j];
						pixels[j] = (byte)(num2 / 4U);
					}
					break;
				case 5U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j];
						pixels[j] = (byte)(num2 / 5U);
					}
					break;
				default:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j];
						pixels[j] = (byte)(num2 / kernel_width);
					}
					break;
				}
				while (j < w)
				{
					num2 -= (uint)ptr[j & 7];
					pixels[j] = (byte)(num2 / kernel_width);
					j++;
				}
				pixels += stride_in_bytes;
			}
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00017048 File Offset: 0x00015248
		public unsafe static void stbtt__v_prefilter(byte* pixels, int w, int h, int stride_in_bytes, uint kernel_width)
		{
			byte* ptr = stackalloc byte[(UIntPtr)8];
			int num = (int)((long)h - (long)((ulong)kernel_width));
			CRuntime.memset((void*)ptr, 0, 8UL);
			for (int i = 0; i < w; i++)
			{
				CRuntime.memset((void*)ptr, 0, (ulong)kernel_width);
				uint num2 = 0U;
				int j;
				switch (kernel_width)
				{
				case 2U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j * stride_in_bytes] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j * stride_in_bytes];
						pixels[j * stride_in_bytes] = (byte)(num2 / 2U);
					}
					break;
				case 3U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j * stride_in_bytes] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j * stride_in_bytes];
						pixels[j * stride_in_bytes] = (byte)(num2 / 3U);
					}
					break;
				case 4U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j * stride_in_bytes] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j * stride_in_bytes];
						pixels[j * stride_in_bytes] = (byte)(num2 / 4U);
					}
					break;
				case 5U:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j * stride_in_bytes] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j * stride_in_bytes];
						pixels[j * stride_in_bytes] = (byte)(num2 / 5U);
					}
					break;
				default:
					for (j = 0; j <= num; j++)
					{
						num2 += (uint)(pixels[j * stride_in_bytes] - ptr[j & 7]);
						ptr[((long)j + (long)((ulong)kernel_width)) & 7L] = pixels[j * stride_in_bytes];
						pixels[j * stride_in_bytes] = (byte)(num2 / kernel_width);
					}
					break;
				}
				while (j < h)
				{
					num2 -= (uint)ptr[j & 7];
					pixels[j * stride_in_bytes] = (byte)(num2 / kernel_width);
					j++;
				}
				pixels++;
			}
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0001720C File Offset: 0x0001540C
		public static float stbtt__oversample_shift(int oversample)
		{
			if (oversample == 0)
			{
				return 0f;
			}
			return (float)(-(float)(oversample - 1)) / (2f * (float)oversample);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00017228 File Offset: 0x00015428
		public unsafe static int stbtt_PackFontRangesGatherRects(StbTrueType.stbtt_pack_context* spc, StbTrueType.stbtt_fontinfo* info, StbTrueType.stbtt_pack_range* ranges, int num_ranges, StbTrueType.stbrp_rect* rects)
		{
			int num = 0;
			for (int i = 0; i < num_ranges; i++)
			{
				float num2 = ranges[i].font_size;
				float num3 = ((num2 > 0f) ? StbTrueType.stbtt_ScaleForPixelHeight(info, num2) : StbTrueType.stbtt_ScaleForMappingEmToPixels(info, -num2));
				ranges[i].h_oversample = (byte)spc->h_oversample;
				ranges[i].v_oversample = (byte)spc->v_oversample;
				for (int j = 0; j < ranges[i].num_chars; j++)
				{
					int unicode_codepoint = ((ranges[i].array_of_unicode_codepoints == null) ? (ranges[i].first_unicode_codepoint_in_range + j) : ranges[i].array_of_unicode_codepoints[j]);
					int glyph = StbTrueType.stbtt_FindGlyphIndex(info, unicode_codepoint);
					int num4;
					int num5;
					int num6;
					int num7;
					StbTrueType.stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, num3 * spc->h_oversample, num3 * spc->v_oversample, 0f, 0f, &num4, &num5, &num6, &num7);
					rects[num].w = (int)((long)(num6 - num4 + spc->padding) + (long)((ulong)spc->h_oversample) - 1L);
					rects[num].h = (int)((long)(num7 - num5 + spc->padding) + (long)((ulong)spc->v_oversample) - 1L);
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000197 RID: 407 RVA: 0x000173A4 File Offset: 0x000155A4
		public unsafe static void stbtt_MakeGlyphBitmapSubpixelPrefilter(StbTrueType.stbtt_fontinfo* info, byte* output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int prefilter_x, int prefilter_y, float* sub_x, float* sub_y, int glyph)
		{
			StbTrueType.stbtt_MakeGlyphBitmapSubpixel(info, output, out_w - (prefilter_x - 1), out_h - (prefilter_y - 1), out_stride, scale_x, scale_y, shift_x, shift_y, glyph);
			if (prefilter_x > 1)
			{
				StbTrueType.stbtt__h_prefilter(output, out_w, out_h, out_stride, (uint)prefilter_x);
			}
			if (prefilter_y > 1)
			{
				StbTrueType.stbtt__v_prefilter(output, out_w, out_h, out_stride, (uint)prefilter_y);
			}
			*sub_x = StbTrueType.stbtt__oversample_shift(prefilter_x);
			*sub_y = StbTrueType.stbtt__oversample_shift(prefilter_y);
		}

		// Token: 0x06000198 RID: 408 RVA: 0x0001740C File Offset: 0x0001560C
		public unsafe static int stbtt_PackFontRangesRenderIntoRects(StbTrueType.stbtt_pack_context* spc, StbTrueType.stbtt_fontinfo* info, StbTrueType.stbtt_pack_range* ranges, int num_ranges, StbTrueType.stbrp_rect* rects)
		{
			int result = 1;
			int h_oversample = (int)spc->h_oversample;
			int v_oversample = (int)spc->v_oversample;
			int num = 0;
			for (int i = 0; i < num_ranges; i++)
			{
				float num2 = ranges[i].font_size;
				float num3 = ((num2 > 0f) ? StbTrueType.stbtt_ScaleForPixelHeight(info, num2) : StbTrueType.stbtt_ScaleForMappingEmToPixels(info, -num2));
				spc->h_oversample = (uint)ranges[i].h_oversample;
				spc->v_oversample = (uint)ranges[i].v_oversample;
				float num4 = 1f / spc->h_oversample;
				float num5 = 1f / spc->v_oversample;
				float num6 = StbTrueType.stbtt__oversample_shift((int)spc->h_oversample);
				float num7 = StbTrueType.stbtt__oversample_shift((int)spc->v_oversample);
				for (int j = 0; j < ranges[i].num_chars; j++)
				{
					StbTrueType.stbrp_rect* ptr = rects + num;
					if (ptr->was_packed != 0)
					{
						StbTrueType.stbtt_packedchar* ptr2 = ranges[i].chardata_for_range + j;
						int unicode_codepoint = ((ranges[i].array_of_unicode_codepoints == null) ? (ranges[i].first_unicode_codepoint_in_range + j) : ranges[i].array_of_unicode_codepoints[j]);
						int num8 = StbTrueType.stbtt_FindGlyphIndex(info, unicode_codepoint);
						int padding = spc->padding;
						ptr->x += padding;
						ptr->y += padding;
						ptr->w -= padding;
						ptr->h -= padding;
						int num9;
						int num10;
						StbTrueType.stbtt_GetGlyphHMetrics(info, num8, &num9, &num10);
						int num11;
						int num12;
						int num13;
						int num14;
						StbTrueType.stbtt_GetGlyphBitmapBox(info, num8, num3 * spc->h_oversample, num3 * spc->v_oversample, &num11, &num12, &num13, &num14);
						StbTrueType.stbtt_MakeGlyphBitmapSubpixel(info, spc->pixels + ptr->x + ptr->y * spc->stride_in_bytes, (int)((long)ptr->w - (long)((ulong)spc->h_oversample) + 1L), (int)((long)ptr->h - (long)((ulong)spc->v_oversample) + 1L), spc->stride_in_bytes, num3 * spc->h_oversample, num3 * spc->v_oversample, 0f, 0f, num8);
						if (spc->h_oversample > 1U)
						{
							StbTrueType.stbtt__h_prefilter(spc->pixels + ptr->x + ptr->y * spc->stride_in_bytes, ptr->w, ptr->h, spc->stride_in_bytes, spc->h_oversample);
						}
						if (spc->v_oversample > 1U)
						{
							StbTrueType.stbtt__v_prefilter(spc->pixels + ptr->x + ptr->y * spc->stride_in_bytes, ptr->w, ptr->h, spc->stride_in_bytes, spc->v_oversample);
						}
						ptr2->x0 = (ushort)((short)ptr->x);
						ptr2->y0 = (ushort)((short)ptr->y);
						ptr2->x1 = (ushort)((short)(ptr->x + ptr->w));
						ptr2->y1 = (ushort)((short)(ptr->y + ptr->h));
						ptr2->xadvance = num3 * (float)num9;
						ptr2->xoff = (float)num11 * num4 + num6;
						ptr2->yoff = (float)num12 * num5 + num7;
						ptr2->xoff2 = (float)(num11 + ptr->w) * num4 + num6;
						ptr2->yoff2 = (float)(num12 + ptr->h) * num5 + num7;
					}
					else
					{
						result = 0;
					}
					num++;
				}
			}
			spc->h_oversample = (uint)h_oversample;
			spc->v_oversample = (uint)v_oversample;
			return result;
		}

		// Token: 0x06000199 RID: 409 RVA: 0x000177B4 File Offset: 0x000159B4
		public unsafe static void stbtt_PackFontRangesPackRects(StbTrueType.stbtt_pack_context* spc, StbTrueType.stbrp_rect* rects, int num_rects)
		{
			StbTrueType.stbrp_pack_rects((StbTrueType.stbrp_context*)spc->pack_info, rects, num_rects);
		}

		// Token: 0x0600019A RID: 410 RVA: 0x000177C4 File Offset: 0x000159C4
		public unsafe static int stbtt_PackFontRanges(StbTrueType.stbtt_pack_context* spc, byte* fontdata, int font_index, StbTrueType.stbtt_pack_range* ranges, int num_ranges)
		{
			StbTrueType.stbtt_fontinfo stbtt_fontinfo = default(StbTrueType.stbtt_fontinfo);
			for (int i = 0; i < num_ranges; i++)
			{
				for (int j = 0; j < ranges[i].num_chars; j++)
				{
					ranges[i].chardata_for_range[j].x0 = (ranges[i].chardata_for_range[j].y0 = (ranges[i].chardata_for_range[j].x1 = (ranges[i].chardata_for_range[j].y1 = 0)));
				}
			}
			int num = 0;
			for (int i = 0; i < num_ranges; i++)
			{
				num += ranges[i].num_chars;
			}
			StbTrueType.stbrp_rect* ptr = (StbTrueType.stbrp_rect*)CRuntime.malloc((ulong)((long)(sizeof(StbTrueType.stbrp_rect) * num)));
			if (ptr == null)
			{
				return 0;
			}
			stbtt_fontinfo.userdata = spc->user_allocator_context;
			StbTrueType.stbtt_InitFont(&stbtt_fontinfo, fontdata, StbTrueType.stbtt_GetFontOffsetForIndex(fontdata, font_index));
			num = StbTrueType.stbtt_PackFontRangesGatherRects(spc, &stbtt_fontinfo, ranges, num_ranges, ptr);
			StbTrueType.stbtt_PackFontRangesPackRects(spc, ptr, num);
			int result = StbTrueType.stbtt_PackFontRangesRenderIntoRects(spc, &stbtt_fontinfo, ranges, num_ranges, ptr);
			CRuntime.free((void*)ptr);
			return result;
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00017920 File Offset: 0x00015B20
		public unsafe static int stbtt_PackFontRange(StbTrueType.stbtt_pack_context* spc, byte* fontdata, int font_index, float font_size, int first_unicode_codepoint_in_range, int num_chars_in_range, StbTrueType.stbtt_packedchar* chardata_for_range)
		{
			StbTrueType.stbtt_pack_range stbtt_pack_range = default(StbTrueType.stbtt_pack_range);
			stbtt_pack_range.first_unicode_codepoint_in_range = first_unicode_codepoint_in_range;
			stbtt_pack_range.array_of_unicode_codepoints = null;
			stbtt_pack_range.num_chars = num_chars_in_range;
			stbtt_pack_range.chardata_for_range = chardata_for_range;
			stbtt_pack_range.font_size = font_size;
			return StbTrueType.stbtt_PackFontRanges(spc, fontdata, font_index, &stbtt_pack_range, 1);
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00017970 File Offset: 0x00015B70
		public unsafe static void stbtt_GetPackedQuad(StbTrueType.stbtt_packedchar* chardata, int pw, int ph, int char_index, float* xpos, float* ypos, StbTrueType.stbtt_aligned_quad* q, int align_to_integer)
		{
			float num = 1f / (float)pw;
			float num2 = 1f / (float)ph;
			StbTrueType.stbtt_packedchar* ptr = chardata + char_index;
			if (align_to_integer != 0)
			{
				float num3 = (float)((int)CRuntime.floor((double)(*xpos + ptr->xoff + 0.5f)));
				float num4 = (float)((int)CRuntime.floor((double)(*ypos + ptr->yoff + 0.5f)));
				q->x0 = num3;
				q->y0 = num4;
				q->x1 = num3 + ptr->xoff2 - ptr->xoff;
				q->y1 = num4 + ptr->yoff2 - ptr->yoff;
			}
			else
			{
				q->x0 = *xpos + ptr->xoff;
				q->y0 = *ypos + ptr->yoff;
				q->x1 = *xpos + ptr->xoff2;
				q->y1 = *ypos + ptr->yoff2;
			}
			q->s0 = (float)ptr->x0 * num;
			q->t0 = (float)ptr->y0 * num2;
			q->s1 = (float)ptr->x1 * num;
			q->t1 = (float)ptr->y1 * num2;
			*xpos += ptr->xadvance;
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00017AB4 File Offset: 0x00015CB4
		public unsafe static int stbtt__ray_intersect_bezier(float* orig, float* ray, float* q0, float* q1, float* q2, float* hits)
		{
			float num = q0[1] * *ray - *q0 * ray[1];
			float num2 = q1[1] * *ray - *q1 * ray[1];
			float num3 = q2[1] * *ray - *q2 * ray[1];
			float num4 = orig[1] * *ray - *orig * ray[1];
			float num5 = num - 2f * num2 + num3;
			float num6 = num2 - num;
			float num7 = num - num4;
			float num8 = 0f;
			float num9 = 0f;
			int num10 = 0;
			if ((double)num5 != 0.0)
			{
				float num11 = num6 * num6 - num5 * num7;
				if ((double)num11 > 0.0)
				{
					float num12 = -1f / num5;
					float num13 = (float)CRuntime.sqrt((double)num11);
					num8 = (num6 + num13) * num12;
					num9 = (num6 - num13) * num12;
					if ((double)num8 >= 0.0 && (double)num8 <= 1.0)
					{
						num10 = 1;
					}
					if ((double)num13 > 0.0 && (double)num9 >= 0.0 && (double)num9 <= 1.0)
					{
						if (num10 == 0)
						{
							num8 = num9;
						}
						num10++;
					}
				}
			}
			else
			{
				num8 = num7 / (-2f * num6);
				if ((double)num8 >= 0.0 && (double)num8 <= 1.0)
				{
					num10 = 1;
				}
			}
			if (num10 == 0)
			{
				return 0;
			}
			float num14 = 1f / (*ray * *ray + ray[1] * ray[1]);
			float num15 = *ray * num14;
			float num16 = ray[1] * num14;
			float num17 = *q0 * num15 + q0[1] * num16;
			float num18 = *q1 * num15 + q1[1] * num16;
			float num19 = *q2 * num15 + q2[1] * num16;
			float num20 = *orig * num15 + orig[1] * num16;
			float num21 = num18 - num17;
			float num22 = num19 - num17;
			float num23 = num17 - num20;
			*hits = num23 + num8 * (2f - 2f * num8) * num21 + num8 * num8 * num22;
			hits[1] = num5 * num8 + num6;
			if (num10 > 1)
			{
				hits[2] = num23 + num9 * (2f - 2f * num9) * num21 + num9 * num9 * num22;
				hits[3] = num5 * num9 + num6;
				return 2;
			}
			return 1;
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00017D0F File Offset: 0x00015F0F
		public unsafe static int equal(float* a, float* b)
		{
			if (*a != *b || a[1] != b[1])
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00017D24 File Offset: 0x00015F24
		public unsafe static int stbtt__compute_crossings_x(float x, float y, int nverts, StbTrueType.stbtt_vertex* verts)
		{
			float* ptr = stackalloc float[(UIntPtr)8];
			float* ptr2 = stackalloc float[(UIntPtr)8];
			*ptr2 = 1f;
			ptr2[1] = 0f;
			int num = 0;
			*ptr = x;
			ptr[1] = y;
			float num2 = (float)CRuntime.fmod((double)y, 1.0);
			if (num2 < 0.01f)
			{
				y += 0.01f;
			}
			else if (num2 > 0.99f)
			{
				y -= 0.01f;
			}
			ptr[1] = y;
			for (int i = 0; i < nverts; i++)
			{
				if (verts[i].type == 2)
				{
					int x2 = (int)verts[i - 1].x;
					int y2 = (int)verts[i - 1].y;
					int x3 = (int)verts[i].x;
					int y3 = (int)verts[i].y;
					if (y > (float)((y2 < y3) ? y2 : y3) && y < (float)((y2 < y3) ? y3 : y2) && x > (float)((x2 < x3) ? x2 : x3) && (y - (float)y2) / (float)(y3 - y2) * (float)(x3 - x2) + (float)x2 < x)
					{
						num += ((y2 < y3) ? 1 : (-1));
					}
				}
				if (verts[i].type == 3)
				{
					int x4 = (int)verts[i - 1].x;
					int y4 = (int)verts[i - 1].y;
					int num3 = (int)verts[i].cx;
					int num4 = (int)verts[i].cy;
					int x5 = (int)verts[i].x;
					int y5 = (int)verts[i].y;
					int num5 = ((x4 < ((num3 < x5) ? num3 : x5)) ? x4 : ((num3 < x5) ? num3 : x5));
					int num6 = ((y4 < ((num4 < y5) ? num4 : y5)) ? y4 : ((num4 < y5) ? num4 : y5));
					int num7 = ((y4 < ((num4 < y5) ? y5 : num4)) ? ((num4 < y5) ? y5 : num4) : y4);
					if (y > (float)num6 && y < (float)num7 && x > (float)num5)
					{
						float* ptr3 = stackalloc float[(UIntPtr)8];
						float* ptr4 = stackalloc float[(UIntPtr)8];
						float* ptr5 = stackalloc float[(UIntPtr)8];
						float* ptr6 = stackalloc float[(UIntPtr)16];
						*ptr3 = (float)x4;
						ptr3[1] = (float)y4;
						*ptr4 = (float)num3;
						ptr4[1] = (float)num4;
						*ptr5 = (float)x5;
						ptr5[1] = (float)y5;
						if (StbTrueType.equal(ptr3, ptr4) != 0 || StbTrueType.equal(ptr4, ptr5) != 0)
						{
							x4 = (int)verts[i - 1].x;
							y4 = (int)verts[i - 1].y;
							num3 = (int)verts[i].x;
							num4 = (int)verts[i].y;
							if (y > (float)((y4 < num4) ? y4 : num4) && y < (float)((y4 < num4) ? num4 : y4) && x > (float)((x4 < num3) ? x4 : num3) && (y - (float)y4) / (float)(num4 - y4) * (float)(num3 - x4) + (float)x4 < x)
							{
								num += ((y4 < num4) ? 1 : (-1));
							}
						}
						else
						{
							int num8 = StbTrueType.stbtt__ray_intersect_bezier(ptr, ptr2, ptr3, ptr4, ptr5, ptr6);
							if (num8 >= 1 && *ptr6 < 0f)
							{
								num += ((ptr6[1] < 0f) ? (-1) : 1);
							}
							if (num8 >= 2 && ptr6[2] < 0f)
							{
								num += ((ptr6[3] < 0f) ? (-1) : 1);
							}
						}
					}
				}
			}
			return num;
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x000180D4 File Offset: 0x000162D4
		public static float stbtt__cuberoot(float x)
		{
			if (x < 0f)
			{
				return -(float)CRuntime.pow((double)(-(double)x), 0.3333333432674408);
			}
			return (float)CRuntime.pow((double)x, 0.3333333432674408);
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00018104 File Offset: 0x00016304
		public unsafe static int stbtt__solve_cubic(float a, float b, float c, float* r)
		{
			float num = -a / 3f;
			float num2 = b - a * a / 3f;
			float num3 = a * (2f * a * a - 9f * b) / 27f + c;
			float num4 = num2 * num2 * num2;
			float num5 = num3 * num3 + 4f * num4 / 27f;
			if (num5 >= 0f)
			{
				float num6 = (float)CRuntime.sqrt((double)num5);
				float num7 = (-num3 + num6) / 2f;
				float num8 = (-num3 - num6) / 2f;
				num7 = StbTrueType.stbtt__cuberoot(num7);
				num8 = StbTrueType.stbtt__cuberoot(num8);
				*r = num + num7 + num8;
				return 1;
			}
			float num9 = (float)CRuntime.sqrt((double)(-(double)num2 / 3f));
			float num10 = (float)CRuntime.acos(-CRuntime.sqrt((double)(-27f / num4)) * (double)num3 / 2.0) / 3f;
			float num11 = (float)CRuntime.cos((double)num10);
			float num12 = (float)CRuntime.cos((double)num10 - 1.570796) * 1.7320508f;
			*r = num + num9 * 2f * num11;
			r[1] = num - num9 * (num11 + num12);
			r[2] = num - num9 * (num11 - num12);
			return 3;
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00018244 File Offset: 0x00016444
		public unsafe static byte* stbtt_GetGlyphSDF(StbTrueType.stbtt_fontinfo* info, float scale, int glyph, int padding, byte onedge_value, float pixel_dist_scale, int* width, int* height, int* xoff, int* yoff)
		{
			float num = scale;
			float num2 = scale;
			if (num == 0f)
			{
				num = num2;
			}
			if (num2 == 0f)
			{
				if (num == 0f)
				{
					return null;
				}
				num2 = num;
			}
			int num3;
			int num4;
			int num5;
			int num6;
			StbTrueType.stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale, scale, 0f, 0f, &num3, &num4, &num5, &num6);
			if (num3 == num5 || num4 == num6)
			{
				return null;
			}
			num3 -= padding;
			num4 -= padding;
			num5 += padding;
			num6 += padding;
			int num7 = num5 - num3;
			int num8 = num6 - num4;
			if (width != null)
			{
				*width = num7;
			}
			if (height != null)
			{
				*height = num8;
			}
			if (xoff != null)
			{
				*xoff = num3;
			}
			if (yoff != null)
			{
				*yoff = num4;
			}
			num2 = -num2;
			StbTrueType.stbtt_vertex* ptr;
			int num9 = StbTrueType.stbtt_GetGlyphShape(info, glyph, &ptr);
			byte* ptr2 = (byte*)CRuntime.malloc((ulong)((long)(num7 * num8)));
			float* ptr3 = (float*)CRuntime.malloc((ulong)((long)(num9 * 4)));
			int i = 0;
			int num10 = num9 - 1;
			while (i < num9)
			{
				if (ptr[i].type == 2)
				{
					float num11 = (float)ptr[i].x * num;
					float num12 = (float)ptr[i].y * num2;
					float num13 = (float)ptr[num10].x * num;
					float num14 = (float)ptr[num10].y * num2;
					float num15 = (float)CRuntime.sqrt((double)((num13 - num11) * (num13 - num11) + (num14 - num12) * (num14 - num12)));
					ptr3[i] = ((num15 == 0f) ? 0f : (1f / num15));
				}
				else if (ptr[i].type == 3)
				{
					float num16 = (float)ptr[num10].x * num;
					float num17 = (float)ptr[num10].y * num2;
					float num18 = (float)ptr[i].cx * num;
					float num19 = (float)ptr[i].cy * num2;
					float num20 = (float)ptr[i].x * num;
					float num21 = (float)ptr[i].y * num2;
					float num22 = num20 - 2f * num18 + num16;
					float num23 = num21 - 2f * num19 + num17;
					if (num22 * num22 + num23 * num23 != 0f)
					{
						ptr3[i] = 1f / (num22 * num22 + num23 * num23);
					}
					else
					{
						ptr3[i] = 0f;
					}
				}
				else
				{
					ptr3[i] = 0f;
				}
				num10 = i++;
			}
			for (int j = num4; j < num6; j++)
			{
				for (int k = num3; k < num5; k++)
				{
					float num24 = 999999f;
					float num25 = (float)k + 0.5f;
					float num26 = (float)j + 0.5f;
					float num27 = num25 / num;
					float num28 = num26 / num2;
					int num29 = StbTrueType.stbtt__compute_crossings_x(num27, num28, num9, ptr);
					for (i = 0; i < num9; i++)
					{
						float num30 = (float)ptr[i].x * num;
						float num31 = (float)ptr[i].y * num2;
						float num32 = (num30 - num25) * (num30 - num25) + (num31 - num26) * (num31 - num26);
						if (num32 < num24 * num24)
						{
							num24 = (float)CRuntime.sqrt((double)num32);
						}
						if (ptr[i].type == 2)
						{
							float num33 = (float)ptr[i - 1].x * num;
							float num34 = (float)ptr[i - 1].y * num2;
							float num35 = CRuntime.fabs((double)((num33 - num30) * (num31 - num26) - (num34 - num31) * (num30 - num25))) * ptr3[i];
							if (num35 < num24)
							{
								float num36 = num33 - num30;
								float num37 = num34 - num31;
								float num38 = num30 - num25;
								float num39 = num31 - num26;
								float num40 = -(num38 * num36 + num39 * num37) / (num36 * num36 + num37 * num37);
								if (num40 >= 0f && num40 <= 1f)
								{
									num24 = num35;
								}
							}
						}
						else if (ptr[i].type == 3)
						{
							float num41 = (float)ptr[i - 1].x * num;
							float num42 = (float)ptr[i - 1].y * num2;
							float num43 = (float)ptr[i].cx * num;
							float num44 = (float)ptr[i].cy * num2;
							float num45 = ((((num30 < num43) ? num30 : num43) < num41) ? ((num30 < num43) ? num30 : num43) : num41);
							float num46 = ((((num31 < num44) ? num31 : num44) < num42) ? ((num31 < num44) ? num31 : num44) : num42);
							float num47 = ((((num30 < num43) ? num43 : num30) < num41) ? num41 : ((num30 < num43) ? num43 : num30));
							float num48 = ((((num31 < num44) ? num44 : num31) < num42) ? num42 : ((num31 < num44) ? num44 : num31));
							if (num25 > num45 - num24 && num25 < num47 + num24 && num26 > num46 - num24 && num26 < num48 + num24)
							{
								int num49 = 0;
								float num50 = num43 - num30;
								float num51 = num44 - num31;
								float num52 = num30 - 2f * num43 + num41;
								float num53 = num31 - 2f * num44 + num42;
								float num54 = num30 - num25;
								float num55 = num31 - num26;
								float* ptr4 = stackalloc float[(UIntPtr)12];
								float num56 = ptr3[i];
								if ((double)num56 == 0.0)
								{
									float num57 = 3f * (num50 * num52 + num51 * num53);
									float num58 = 2f * (num50 * num50 + num51 * num51) + (num54 * num52 + num55 * num53);
									float num59 = num54 * num50 + num55 * num51;
									if ((double)num57 == 0.0)
									{
										if ((double)num58 != 0.0)
										{
											ptr4[(IntPtr)(num49++) * 4] = -num59 / num58;
										}
									}
									else
									{
										float num60 = num58 * num58 - 4f * num57 * num59;
										if (num60 < 0f)
										{
											num49 = 0;
										}
										else
										{
											float num61 = (float)CRuntime.sqrt((double)num60);
											*ptr4 = (-num58 - num61) / (2f * num57);
											ptr4[1] = (-num58 + num61) / (2f * num57);
											num49 = 2;
										}
									}
								}
								else
								{
									float num62 = 3f * (num50 * num52 + num51 * num53) * num56;
									float num63 = (2f * (num50 * num50 + num51 * num51) + (num54 * num52 + num55 * num53)) * num56;
									float num64 = (num54 * num50 + num55 * num51) * num56;
									num49 = StbTrueType.stbtt__solve_cubic(num62, num63, num64, ptr4);
								}
								if (num49 >= 1 && *ptr4 >= 0f && *ptr4 <= 1f)
								{
									float num65 = *ptr4;
									float num66 = 1f - num65;
									float num67 = num66 * num66 * num30 + 2f * num65 * num66 * num43 + num65 * num65 * num41;
									float num68 = num66 * num66 * num31 + 2f * num65 * num66 * num44 + num65 * num65 * num42;
									num32 = (num67 - num25) * (num67 - num25) + (num68 - num26) * (num68 - num26);
									if (num32 < num24 * num24)
									{
										num24 = (float)CRuntime.sqrt((double)num32);
									}
								}
								if (num49 >= 2 && ptr4[1] >= 0f && ptr4[1] <= 1f)
								{
									float num65 = ptr4[1];
									float num66 = 1f - num65;
									float num67 = num66 * num66 * num30 + 2f * num65 * num66 * num43 + num65 * num65 * num41;
									float num68 = num66 * num66 * num31 + 2f * num65 * num66 * num44 + num65 * num65 * num42;
									num32 = (num67 - num25) * (num67 - num25) + (num68 - num26) * (num68 - num26);
									if (num32 < num24 * num24)
									{
										num24 = (float)CRuntime.sqrt((double)num32);
									}
								}
								if (num49 >= 3 && ptr4[2] >= 0f && ptr4[2] <= 1f)
								{
									float num65 = ptr4[2];
									float num66 = 1f - num65;
									float num67 = num66 * num66 * num30 + 2f * num65 * num66 * num43 + num65 * num65 * num41;
									float num68 = num66 * num66 * num31 + 2f * num65 * num66 * num44 + num65 * num65 * num42;
									num32 = (num67 - num25) * (num67 - num25) + (num68 - num26) * (num68 - num26);
									if (num32 < num24 * num24)
									{
										num24 = (float)CRuntime.sqrt((double)num32);
									}
								}
							}
						}
					}
					if (num29 == 0)
					{
						num24 = -num24;
					}
					float num69 = (float)onedge_value + pixel_dist_scale * num24;
					if (num69 < 0f)
					{
						num69 = 0f;
					}
					else if (num69 > 255f)
					{
						num69 = 255f;
					}
					ptr2[(j - num4) * num7 + (k - num3)] = (byte)num69;
				}
			}
			CRuntime.free((void*)ptr3);
			CRuntime.free((void*)ptr);
			return ptr2;
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x00018BFC File Offset: 0x00016DFC
		public unsafe static byte* stbtt_GetCodepointSDF(StbTrueType.stbtt_fontinfo* info, float scale, int codepoint, int padding, byte onedge_value, float pixel_dist_scale, int* width, int* height, int* xoff, int* yoff)
		{
			return StbTrueType.stbtt_GetGlyphSDF(info, scale, StbTrueType.stbtt_FindGlyphIndex(info, codepoint), padding, onedge_value, pixel_dist_scale, width, height, xoff, yoff);
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x000166A6 File Offset: 0x000148A6
		public unsafe static void stbtt_FreeSDF(byte* bitmap, void* userdata)
		{
			CRuntime.free((void*)bitmap);
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00018C28 File Offset: 0x00016E28
		public unsafe static int stbtt__CompareUTF8toUTF16_bigendian_prefix(byte* s1, int len1, byte* s2, int len2)
		{
			int num = 0;
			while (len2 != 0)
			{
				ushort num2 = (ushort)((int)(*s2) * 256 + (int)s2[1]);
				if (num2 < 128)
				{
					if (num >= len1)
					{
						return -1;
					}
					if ((ushort)s1[num++] != num2)
					{
						return -1;
					}
				}
				else if (num2 < 2048)
				{
					if (num + 1 >= len1)
					{
						return -1;
					}
					if ((int)s1[num++] != 192 + (num2 >> 6))
					{
						return -1;
					}
					if ((ushort)s1[num++] != 128 + (num2 & 63))
					{
						return -1;
					}
				}
				else if (num2 >= 55296 && num2 < 56320)
				{
					ushort num3 = (ushort)((int)s2[2] * 256 + (int)s2[3]);
					if (num + 3 >= len1)
					{
						return -1;
					}
					uint num4 = (uint)(((int)(num2 - 55296) << 10) + (int)(num3 - 56320) + 65536);
					if ((uint)s1[num++] != 240U + (num4 >> 18))
					{
						return -1;
					}
					if ((uint)s1[num++] != 128U + ((num4 >> 12) & 63U))
					{
						return -1;
					}
					if ((uint)s1[num++] != 128U + ((num4 >> 6) & 63U))
					{
						return -1;
					}
					if ((uint)s1[num++] != 128U + (num4 & 63U))
					{
						return -1;
					}
					s2 += 2;
					len2 -= 2;
				}
				else
				{
					if (num2 >= 56320 && num2 < 57344)
					{
						return -1;
					}
					if (num + 2 >= len1)
					{
						return -1;
					}
					if ((int)s1[num++] != 224 + (num2 >> 12))
					{
						return -1;
					}
					if ((int)s1[num++] != 128 + ((num2 >> 6) & 63))
					{
						return -1;
					}
					if ((ushort)s1[num++] != 128 + (num2 & 63))
					{
						return -1;
					}
				}
				s2 += 2;
				len2 -= 2;
			}
			return num;
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x00018DC7 File Offset: 0x00016FC7
		public unsafe static int stbtt_CompareUTF8toUTF16_bigendian_internal(sbyte* s1, int len1, sbyte* s2, int len2)
		{
			if (len1 != StbTrueType.stbtt__CompareUTF8toUTF16_bigendian_prefix((byte*)s1, len1, (byte*)s2, len2))
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00018DD8 File Offset: 0x00016FD8
		public unsafe static sbyte* stbtt_GetFontNameString(StbTrueType.stbtt_fontinfo* font, int* length, int platformID, int encodingID, int languageID, int nameID)
		{
			byte* data = font->data;
			uint fontstart = (uint)font->fontstart;
			uint num = StbTrueType.stbtt__find_table(data, fontstart, "name");
			if (num == 0U)
			{
				return null;
			}
			int num2 = (int)StbTrueType.ttUSHORT(data + num + 2);
			int num3 = (int)(num + (uint)StbTrueType.ttUSHORT(data + num + 4));
			for (int i = 0; i < num2; i++)
			{
				uint num4 = (uint)((ulong)(num + 6U) + (ulong)((long)(12 * i)));
				if (platformID == (int)StbTrueType.ttUSHORT(data + num4) && encodingID == (int)StbTrueType.ttUSHORT(data + num4 + 2) && languageID == (int)StbTrueType.ttUSHORT(data + num4 + 4) && nameID == (int)StbTrueType.ttUSHORT(data + num4 + 6))
				{
					*length = (int)StbTrueType.ttUSHORT(data + num4 + 8);
					return (sbyte*)(data + num3 + StbTrueType.ttUSHORT(data + num4 + 10));
				}
			}
			return null;
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x00018EA0 File Offset: 0x000170A0
		public unsafe static int stbtt__matchpair(byte* fc, uint nm, byte* name, int nlen, int target_id, int next_id)
		{
			int num = (int)StbTrueType.ttUSHORT(fc + nm + 2);
			int num2 = (int)(nm + (uint)StbTrueType.ttUSHORT(fc + nm + 4));
			for (int i = 0; i < num; i++)
			{
				uint num3 = (uint)((ulong)(nm + 6U) + (ulong)((long)(12 * i)));
				if ((int)StbTrueType.ttUSHORT(fc + num3 + 6) == target_id)
				{
					int num4 = (int)StbTrueType.ttUSHORT(fc + num3);
					int num5 = (int)StbTrueType.ttUSHORT(fc + num3 + 2);
					int num6 = (int)StbTrueType.ttUSHORT(fc + num3 + 4);
					if (num4 == 0 || (num4 == 3 && num5 == 1) || (num4 == 3 && num5 == 10))
					{
						int num7 = (int)StbTrueType.ttUSHORT(fc + num3 + 8);
						int num8 = (int)StbTrueType.ttUSHORT(fc + num3 + 10);
						int num9 = StbTrueType.stbtt__CompareUTF8toUTF16_bigendian_prefix(name, nlen, fc + num2 + num8, num7);
						if (num9 >= 0)
						{
							if (i + 1 < num && (int)StbTrueType.ttUSHORT(fc + num3 + 12 + 6) == next_id && (int)StbTrueType.ttUSHORT(fc + num3 + 12) == num4 && (int)StbTrueType.ttUSHORT(fc + num3 + 12 + 2) == num5 && (int)StbTrueType.ttUSHORT(fc + num3 + 12 + 4) == num6)
							{
								num7 = (int)StbTrueType.ttUSHORT(fc + num3 + 12 + 8);
								num8 = (int)StbTrueType.ttUSHORT(fc + num3 + 12 + 10);
								if (num7 == 0)
								{
									if (num9 == nlen)
									{
										return 1;
									}
								}
								else if (num9 < nlen && name[num9] == 32)
								{
									num9++;
									if (StbTrueType.stbtt_CompareUTF8toUTF16_bigendian_internal((sbyte*)(name + num9), nlen - num9, (sbyte*)(fc + num2 + num8), num7) != 0)
									{
										return 1;
									}
								}
							}
							else if (num9 == nlen)
							{
								return 1;
							}
						}
					}
				}
			}
			return 0;
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x00019024 File Offset: 0x00017224
		public unsafe static int stbtt__matches(byte* fc, uint offset, byte* name, int flags)
		{
			int nlen = (int)CRuntime.strlen((sbyte*)name);
			if (StbTrueType.stbtt__isfont(fc + offset) == 0)
			{
				return 0;
			}
			if (flags != 0)
			{
				uint num = StbTrueType.stbtt__find_table(fc, offset, "head");
				if ((int)(StbTrueType.ttUSHORT(fc + num + 44) & 7) != (flags & 7))
				{
					return 0;
				}
			}
			uint num2 = StbTrueType.stbtt__find_table(fc, offset, "name");
			if (num2 == 0U)
			{
				return 0;
			}
			if (flags != 0)
			{
				if (StbTrueType.stbtt__matchpair(fc, num2, name, nlen, 16, -1) != 0)
				{
					return 1;
				}
				if (StbTrueType.stbtt__matchpair(fc, num2, name, nlen, 1, -1) != 0)
				{
					return 1;
				}
				if (StbTrueType.stbtt__matchpair(fc, num2, name, nlen, 3, -1) != 0)
				{
					return 1;
				}
			}
			else
			{
				if (StbTrueType.stbtt__matchpair(fc, num2, name, nlen, 16, 17) != 0)
				{
					return 1;
				}
				if (StbTrueType.stbtt__matchpair(fc, num2, name, nlen, 1, 2) != 0)
				{
					return 1;
				}
				if (StbTrueType.stbtt__matchpair(fc, num2, name, nlen, 3, -1) != 0)
				{
					return 1;
				}
			}
			return 0;
		}

		// Token: 0x060001AA RID: 426 RVA: 0x000190E0 File Offset: 0x000172E0
		public unsafe static int stbtt_FindMatchingFont_internal(byte* font_collection, sbyte* name_utf8, int flags)
		{
			int num = 0;
			int num2;
			for (;;)
			{
				num2 = StbTrueType.stbtt_GetFontOffsetForIndex(font_collection, num);
				if (num2 < 0)
				{
					break;
				}
				if (StbTrueType.stbtt__matches(font_collection, (uint)num2, (byte*)name_utf8, flags) != 0)
				{
					return num2;
				}
				num++;
			}
			return num2;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00019110 File Offset: 0x00017310
		public unsafe static int stbtt_BakeFontBitmap(byte* data, int offset, float pixel_height, byte* pixels, int pw, int ph, int first_char, int num_chars, StbTrueType.stbtt_bakedchar* chardata)
		{
			return StbTrueType.stbtt_BakeFontBitmap_internal(data, offset, pixel_height, pixels, pw, ph, first_char, num_chars, chardata);
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00019131 File Offset: 0x00017331
		public unsafe static int stbtt_GetFontOffsetForIndex(byte* data, int index)
		{
			return StbTrueType.stbtt_GetFontOffsetForIndex_internal(data, index);
		}

		// Token: 0x060001AD RID: 429 RVA: 0x0001913A File Offset: 0x0001733A
		public unsafe static int stbtt_GetNumberOfFonts(byte* data)
		{
			return StbTrueType.stbtt_GetNumberOfFonts_internal(data);
		}

		// Token: 0x060001AE RID: 430 RVA: 0x00019142 File Offset: 0x00017342
		public unsafe static int stbtt_InitFont(StbTrueType.stbtt_fontinfo* info, byte* data, int offset)
		{
			return StbTrueType.stbtt_InitFont_internal(info, data, offset);
		}

		// Token: 0x060001AF RID: 431 RVA: 0x0001914C File Offset: 0x0001734C
		public unsafe static int stbtt_FindMatchingFont(byte* fontdata, sbyte* name, int flags)
		{
			return StbTrueType.stbtt_FindMatchingFont_internal(fontdata, name, flags);
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00019156 File Offset: 0x00017356
		public unsafe static int stbtt_CompareUTF8toUTF16_bigendian(sbyte* s1, int len1, sbyte* s2, int len2)
		{
			return StbTrueType.stbtt_CompareUTF8toUTF16_bigendian_internal(s1, len1, s2, len2);
		}

		// Token: 0x04000074 RID: 116
		public const int STBTT_vmove = 1;

		// Token: 0x04000075 RID: 117
		public const int STBTT_vline = 2;

		// Token: 0x04000076 RID: 118
		public const int STBTT_vcurve = 3;

		// Token: 0x04000077 RID: 119
		public const int STBTT_vcubic = 4;

		// Token: 0x04000078 RID: 120
		public const int STBTT_PLATFORM_ID_UNICODE = 0;

		// Token: 0x04000079 RID: 121
		public const int STBTT_PLATFORM_ID_MAC = 1;

		// Token: 0x0400007A RID: 122
		public const int STBTT_PLATFORM_ID_ISO = 2;

		// Token: 0x0400007B RID: 123
		public const int STBTT_PLATFORM_ID_MICROSOFT = 3;

		// Token: 0x0400007C RID: 124
		public const int STBTT_UNICODE_EID_UNICODE_1_0 = 0;

		// Token: 0x0400007D RID: 125
		public const int STBTT_UNICODE_EID_UNICODE_1_1 = 1;

		// Token: 0x0400007E RID: 126
		public const int STBTT_UNICODE_EID_ISO_10646 = 2;

		// Token: 0x0400007F RID: 127
		public const int STBTT_UNICODE_EID_UNICODE_2_0_BMP = 3;

		// Token: 0x04000080 RID: 128
		public const int STBTT_UNICODE_EID_UNICODE_2_0_FULL = 4;

		// Token: 0x04000081 RID: 129
		public const int STBTT_MS_EID_SYMBOL = 0;

		// Token: 0x04000082 RID: 130
		public const int STBTT_MS_EID_UNICODE_BMP = 1;

		// Token: 0x04000083 RID: 131
		public const int STBTT_MS_EID_SHIFTJIS = 2;

		// Token: 0x04000084 RID: 132
		public const int STBTT_MS_EID_UNICODE_FULL = 10;

		// Token: 0x04000085 RID: 133
		public const int STBTT_MAC_EID_ROMAN = 0;

		// Token: 0x04000086 RID: 134
		public const int STBTT_MAC_EID_ARABIC = 4;

		// Token: 0x04000087 RID: 135
		public const int STBTT_MAC_EID_JAPANESE = 1;

		// Token: 0x04000088 RID: 136
		public const int STBTT_MAC_EID_HEBREW = 5;

		// Token: 0x04000089 RID: 137
		public const int STBTT_MAC_EID_CHINESE_TRAD = 2;

		// Token: 0x0400008A RID: 138
		public const int STBTT_MAC_EID_GREEK = 6;

		// Token: 0x0400008B RID: 139
		public const int STBTT_MAC_EID_KOREAN = 3;

		// Token: 0x0400008C RID: 140
		public const int STBTT_MAC_EID_RUSSIAN = 7;

		// Token: 0x0400008D RID: 141
		public const int STBTT_MS_LANG_ENGLISH = 1033;

		// Token: 0x0400008E RID: 142
		public const int STBTT_MS_LANG_ITALIAN = 1040;

		// Token: 0x0400008F RID: 143
		public const int STBTT_MS_LANG_CHINESE = 2052;

		// Token: 0x04000090 RID: 144
		public const int STBTT_MS_LANG_JAPANESE = 1041;

		// Token: 0x04000091 RID: 145
		public const int STBTT_MS_LANG_DUTCH = 1043;

		// Token: 0x04000092 RID: 146
		public const int STBTT_MS_LANG_KOREAN = 1042;

		// Token: 0x04000093 RID: 147
		public const int STBTT_MS_LANG_FRENCH = 1036;

		// Token: 0x04000094 RID: 148
		public const int STBTT_MS_LANG_RUSSIAN = 1049;

		// Token: 0x04000095 RID: 149
		public const int STBTT_MS_LANG_GERMAN = 1031;

		// Token: 0x04000096 RID: 150
		public const int STBTT_MS_LANG_SPANISH = 1033;

		// Token: 0x04000097 RID: 151
		public const int STBTT_MS_LANG_HEBREW = 1037;

		// Token: 0x04000098 RID: 152
		public const int STBTT_MS_LANG_SWEDISH = 1053;

		// Token: 0x04000099 RID: 153
		public const int STBTT_MAC_LANG_ENGLISH = 0;

		// Token: 0x0400009A RID: 154
		public const int STBTT_MAC_LANG_JAPANESE = 11;

		// Token: 0x0400009B RID: 155
		public const int STBTT_MAC_LANG_ARABIC = 12;

		// Token: 0x0400009C RID: 156
		public const int STBTT_MAC_LANG_KOREAN = 23;

		// Token: 0x0400009D RID: 157
		public const int STBTT_MAC_LANG_DUTCH = 4;

		// Token: 0x0400009E RID: 158
		public const int STBTT_MAC_LANG_RUSSIAN = 32;

		// Token: 0x0400009F RID: 159
		public const int STBTT_MAC_LANG_FRENCH = 1;

		// Token: 0x040000A0 RID: 160
		public const int STBTT_MAC_LANG_SPANISH = 6;

		// Token: 0x040000A1 RID: 161
		public const int STBTT_MAC_LANG_GERMAN = 2;

		// Token: 0x040000A2 RID: 162
		public const int STBTT_MAC_LANG_SWEDISH = 5;

		// Token: 0x040000A3 RID: 163
		public const int STBTT_MAC_LANG_HEBREW = 10;

		// Token: 0x040000A4 RID: 164
		public const int STBTT_MAC_LANG_CHINESE_SIMPLIFIED = 33;

		// Token: 0x040000A5 RID: 165
		public const int STBTT_MAC_LANG_ITALIAN = 3;

		// Token: 0x040000A6 RID: 166
		public const int STBTT_MAC_LANG_CHINESE_TRAD = 19;

		// Token: 0x0200002C RID: 44
		public struct stbtt__buf
		{
			// Token: 0x040001C2 RID: 450
			public unsafe byte* data;

			// Token: 0x040001C3 RID: 451
			public int cursor;

			// Token: 0x040001C4 RID: 452
			public int size;
		}

		// Token: 0x0200002D RID: 45
		public struct stbtt_bakedchar
		{
			// Token: 0x040001C5 RID: 453
			public ushort x0;

			// Token: 0x040001C6 RID: 454
			public ushort y0;

			// Token: 0x040001C7 RID: 455
			public ushort x1;

			// Token: 0x040001C8 RID: 456
			public ushort y1;

			// Token: 0x040001C9 RID: 457
			public float xoff;

			// Token: 0x040001CA RID: 458
			public float yoff;

			// Token: 0x040001CB RID: 459
			public float xadvance;
		}

		// Token: 0x0200002E RID: 46
		public struct stbtt_aligned_quad
		{
			// Token: 0x040001CC RID: 460
			public float x0;

			// Token: 0x040001CD RID: 461
			public float y0;

			// Token: 0x040001CE RID: 462
			public float s0;

			// Token: 0x040001CF RID: 463
			public float t0;

			// Token: 0x040001D0 RID: 464
			public float x1;

			// Token: 0x040001D1 RID: 465
			public float y1;

			// Token: 0x040001D2 RID: 466
			public float s1;

			// Token: 0x040001D3 RID: 467
			public float t1;
		}

		// Token: 0x0200002F RID: 47
		public struct stbtt_packedchar
		{
			// Token: 0x040001D4 RID: 468
			public ushort x0;

			// Token: 0x040001D5 RID: 469
			public ushort y0;

			// Token: 0x040001D6 RID: 470
			public ushort x1;

			// Token: 0x040001D7 RID: 471
			public ushort y1;

			// Token: 0x040001D8 RID: 472
			public float xoff;

			// Token: 0x040001D9 RID: 473
			public float yoff;

			// Token: 0x040001DA RID: 474
			public float xadvance;

			// Token: 0x040001DB RID: 475
			public float xoff2;

			// Token: 0x040001DC RID: 476
			public float yoff2;
		}

		// Token: 0x02000030 RID: 48
		public struct stbtt_pack_range
		{
			// Token: 0x040001DD RID: 477
			public float font_size;

			// Token: 0x040001DE RID: 478
			public int first_unicode_codepoint_in_range;

			// Token: 0x040001DF RID: 479
			public unsafe int* array_of_unicode_codepoints;

			// Token: 0x040001E0 RID: 480
			public int num_chars;

			// Token: 0x040001E1 RID: 481
			public unsafe StbTrueType.stbtt_packedchar* chardata_for_range;

			// Token: 0x040001E2 RID: 482
			public byte h_oversample;

			// Token: 0x040001E3 RID: 483
			public byte v_oversample;
		}

		// Token: 0x02000031 RID: 49
		public struct stbtt_pack_context
		{
			// Token: 0x040001E4 RID: 484
			public unsafe void* user_allocator_context;

			// Token: 0x040001E5 RID: 485
			public unsafe void* pack_info;

			// Token: 0x040001E6 RID: 486
			public int width;

			// Token: 0x040001E7 RID: 487
			public int height;

			// Token: 0x040001E8 RID: 488
			public int stride_in_bytes;

			// Token: 0x040001E9 RID: 489
			public int padding;

			// Token: 0x040001EA RID: 490
			public uint h_oversample;

			// Token: 0x040001EB RID: 491
			public uint v_oversample;

			// Token: 0x040001EC RID: 492
			public unsafe byte* pixels;

			// Token: 0x040001ED RID: 493
			public unsafe void* nodes;
		}

		// Token: 0x02000032 RID: 50
		public struct stbtt_fontinfo
		{
			// Token: 0x040001EE RID: 494
			public unsafe void* userdata;

			// Token: 0x040001EF RID: 495
			public unsafe byte* data;

			// Token: 0x040001F0 RID: 496
			public int fontstart;

			// Token: 0x040001F1 RID: 497
			public int numGlyphs;

			// Token: 0x040001F2 RID: 498
			public int loca;

			// Token: 0x040001F3 RID: 499
			public int head;

			// Token: 0x040001F4 RID: 500
			public int glyf;

			// Token: 0x040001F5 RID: 501
			public int hhea;

			// Token: 0x040001F6 RID: 502
			public int hmtx;

			// Token: 0x040001F7 RID: 503
			public int kern;

			// Token: 0x040001F8 RID: 504
			public int gpos;

			// Token: 0x040001F9 RID: 505
			public int index_map;

			// Token: 0x040001FA RID: 506
			public int indexToLocFormat;

			// Token: 0x040001FB RID: 507
			public StbTrueType.stbtt__buf cff;

			// Token: 0x040001FC RID: 508
			public StbTrueType.stbtt__buf charstrings;

			// Token: 0x040001FD RID: 509
			public StbTrueType.stbtt__buf gsubrs;

			// Token: 0x040001FE RID: 510
			public StbTrueType.stbtt__buf subrs;

			// Token: 0x040001FF RID: 511
			public StbTrueType.stbtt__buf fontdicts;

			// Token: 0x04000200 RID: 512
			public StbTrueType.stbtt__buf fdselect;
		}

		// Token: 0x02000033 RID: 51
		public struct stbtt_vertex
		{
			// Token: 0x04000201 RID: 513
			public short x;

			// Token: 0x04000202 RID: 514
			public short y;

			// Token: 0x04000203 RID: 515
			public short cx;

			// Token: 0x04000204 RID: 516
			public short cy;

			// Token: 0x04000205 RID: 517
			public short cx1;

			// Token: 0x04000206 RID: 518
			public short cy1;

			// Token: 0x04000207 RID: 519
			public byte type;

			// Token: 0x04000208 RID: 520
			public byte padding;
		}

		// Token: 0x02000034 RID: 52
		public struct stbtt__bitmap
		{
			// Token: 0x04000209 RID: 521
			public int w;

			// Token: 0x0400020A RID: 522
			public int h;

			// Token: 0x0400020B RID: 523
			public int stride;

			// Token: 0x0400020C RID: 524
			public unsafe byte* pixels;
		}

		// Token: 0x02000035 RID: 53
		public struct stbtt__csctx
		{
			// Token: 0x0400020D RID: 525
			public int bounds;

			// Token: 0x0400020E RID: 526
			public int started;

			// Token: 0x0400020F RID: 527
			public float first_x;

			// Token: 0x04000210 RID: 528
			public float first_y;

			// Token: 0x04000211 RID: 529
			public float x;

			// Token: 0x04000212 RID: 530
			public float y;

			// Token: 0x04000213 RID: 531
			public int min_x;

			// Token: 0x04000214 RID: 532
			public int max_x;

			// Token: 0x04000215 RID: 533
			public int min_y;

			// Token: 0x04000216 RID: 534
			public int max_y;

			// Token: 0x04000217 RID: 535
			public unsafe StbTrueType.stbtt_vertex* pvertices;

			// Token: 0x04000218 RID: 536
			public int num_vertices;
		}

		// Token: 0x02000036 RID: 54
		public struct stbtt__hheap_chunk
		{
			// Token: 0x04000219 RID: 537
			public unsafe StbTrueType.stbtt__hheap_chunk* next;
		}

		// Token: 0x02000037 RID: 55
		public struct stbtt__hheap
		{
			// Token: 0x0400021A RID: 538
			public unsafe StbTrueType.stbtt__hheap_chunk* head;

			// Token: 0x0400021B RID: 539
			public unsafe void* first_free;

			// Token: 0x0400021C RID: 540
			public int num_remaining_in_head_chunk;
		}

		// Token: 0x02000038 RID: 56
		public struct stbtt__edge
		{
			// Token: 0x0400021D RID: 541
			public float x0;

			// Token: 0x0400021E RID: 542
			public float y0;

			// Token: 0x0400021F RID: 543
			public float x1;

			// Token: 0x04000220 RID: 544
			public float y1;

			// Token: 0x04000221 RID: 545
			public int invert;
		}

		// Token: 0x02000039 RID: 57
		public struct stbtt__active_edge
		{
			// Token: 0x04000222 RID: 546
			public unsafe StbTrueType.stbtt__active_edge* next;

			// Token: 0x04000223 RID: 547
			public float fx;

			// Token: 0x04000224 RID: 548
			public float fdx;

			// Token: 0x04000225 RID: 549
			public float fdy;

			// Token: 0x04000226 RID: 550
			public float direction;

			// Token: 0x04000227 RID: 551
			public float sy;

			// Token: 0x04000228 RID: 552
			public float ey;
		}

		// Token: 0x0200003A RID: 58
		public struct stbtt__point
		{
			// Token: 0x04000229 RID: 553
			public float x;

			// Token: 0x0400022A RID: 554
			public float y;
		}

		// Token: 0x0200003B RID: 59
		public struct stbrp_context
		{
			// Token: 0x0400022B RID: 555
			public int width;

			// Token: 0x0400022C RID: 556
			public int height;

			// Token: 0x0400022D RID: 557
			public int x;

			// Token: 0x0400022E RID: 558
			public int y;

			// Token: 0x0400022F RID: 559
			public int bottom_y;
		}

		// Token: 0x0200003C RID: 60
		public struct stbrp_node
		{
			// Token: 0x04000230 RID: 560
			public byte x;
		}

		// Token: 0x0200003D RID: 61
		public struct stbrp_rect
		{
			// Token: 0x04000231 RID: 561
			public int x;

			// Token: 0x04000232 RID: 562
			public int y;

			// Token: 0x04000233 RID: 563
			public int id;

			// Token: 0x04000234 RID: 564
			public int w;

			// Token: 0x04000235 RID: 565
			public int h;

			// Token: 0x04000236 RID: 566
			public int was_packed;
		}
	}
}
