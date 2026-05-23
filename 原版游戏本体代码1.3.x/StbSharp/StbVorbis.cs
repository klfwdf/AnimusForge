using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x0200000C RID: 12
	public static class StbVorbis
	{
		// Token: 0x060001B1 RID: 433 RVA: 0x00019164 File Offset: 0x00017364
		public static uint get_bits(StbVorbis.stb_vorbis f, int n)
		{
			if (f.valid_bits < 0)
			{
				return 0U;
			}
			if (f.valid_bits < n)
			{
				if (n > 24)
				{
					return StbVorbis.get_bits(f, 24) + (StbVorbis.get_bits(f, n - 24) << 24);
				}
				if (f.valid_bits == 0)
				{
					f.acc = 0U;
				}
				while (f.valid_bits < n)
				{
					int num = StbVorbis.get8_packet_raw(f);
					if (num == -1)
					{
						f.valid_bits = -1;
						return 0U;
					}
					f.acc += (uint)((uint)num << f.valid_bits);
					f.valid_bits += 8;
				}
			}
			if (f.valid_bits < 0)
			{
				return 0U;
			}
			uint result = (uint)((ulong)f.acc & (ulong)((long)((1 << n) - 1)));
			f.acc >>= n;
			f.valid_bits -= n;
			return result;
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x00019234 File Offset: 0x00017434
		public unsafe static short[] decode_vorbis_from_memory(byte[] input, out int sampleRate, out int chan)
		{
			short* ptr = null;
			int num;
			fixed (byte[] array = input)
			{
				byte* mem;
				if (input == null || array.Length == 0)
				{
					mem = null;
				}
				else
				{
					mem = &array[0];
				}
				int num2;
				int num3;
				num = StbVorbis.stb_vorbis_decode_memory(mem, input.Length, &num2, &num3, ref ptr);
				chan = num2;
				sampleRate = num3;
			}
			short[] array2 = new short[num];
			Marshal.Copy(new IntPtr((void*)ptr), array2, 0, array2.Length);
			CRuntime.free((void*)ptr);
			return array2;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00019294 File Offset: 0x00017494
		public static int error(StbVorbis.stb_vorbis f, int e)
		{
			f.error = e;
			if (f.eof == 0 && e != 1)
			{
				f.error = e;
			}
			throw new Exception("Vorbis Error: " + e);
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x000192C8 File Offset: 0x000174C8
		public unsafe static void* make_block_array(void* mem, int count, int size)
		{
			sbyte* ptr = (sbyte*)((byte*)mem + (IntPtr)count * (IntPtr)sizeof(void*));
			for (int i = 0; i < count; i++)
			{
				*(IntPtr*)((byte*)mem + (IntPtr)i * (IntPtr)sizeof(void*)) = ptr;
				ptr += size;
			}
			return mem;
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x00019304 File Offset: 0x00017504
		public unsafe static void* setup_malloc(StbVorbis.stb_vorbis f, int sz)
		{
			sz = (sz + 3) & -4;
			f.setup_memory_required += (uint)sz;
			if (f.alloc.alloc_buffer != null)
			{
				void* result = (void*)(f.alloc.alloc_buffer + f.setup_offset);
				if (f.setup_offset + sz > f.temp_offset)
				{
					return null;
				}
				f.setup_offset += sz;
				return result;
			}
			else
			{
				if (sz == 0)
				{
					return null;
				}
				return CRuntime.malloc((ulong)((long)sz));
			}
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x00019379 File Offset: 0x00017579
		public unsafe static void setup_free(StbVorbis.stb_vorbis f, void* p)
		{
			if (f.alloc.alloc_buffer != null)
			{
				return;
			}
			CRuntime.free(p);
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x00019394 File Offset: 0x00017594
		public unsafe static void* setup_temp_malloc(StbVorbis.stb_vorbis f, int sz)
		{
			sz = (sz + 3) & -4;
			if (f.alloc.alloc_buffer == null)
			{
				return CRuntime.malloc((ulong)((long)sz));
			}
			if (f.temp_offset - sz < f.setup_offset)
			{
				return null;
			}
			f.temp_offset -= sz;
			return (void*)(f.alloc.alloc_buffer + f.temp_offset);
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x000193F3 File Offset: 0x000175F3
		public unsafe static void setup_temp_free(StbVorbis.stb_vorbis f, void* p, int sz)
		{
			if (f.alloc.alloc_buffer != null)
			{
				f.temp_offset += (sz + 3) & -4;
				return;
			}
			CRuntime.free(p);
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00019420 File Offset: 0x00017620
		public static void crc32_init()
		{
			for (int i = 0; i < 256; i++)
			{
				uint num = (uint)((uint)i << 24);
				for (int j = 0; j < 8; j++)
				{
					num = (uint)((ulong)((ulong)num << 1) ^ (ulong)((num >= 2147483648U) ? 79764919L : 0L));
				}
				StbVorbis._crc_table[i] = num;
			}
		}

		// Token: 0x060001BA RID: 442 RVA: 0x0001946E File Offset: 0x0001766E
		public static uint crc32_update(uint crc, byte b)
		{
			return (crc << 8) ^ StbVorbis._crc_table[(int)((uint)b ^ (crc >> 24))];
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00019480 File Offset: 0x00017680
		public static uint bit_reverse(uint n)
		{
			n = ((n & 2863311530U) >> 1) | ((n & 1431655765U) << 1);
			n = ((n & 3435973836U) >> 2) | ((n & 858993459U) << 2);
			n = ((n & 4042322160U) >> 4) | ((n & 252645135U) << 4);
			n = ((n & 4278255360U) >> 8) | ((n & 16711935U) << 8);
			return (n >> 16) | (n << 16);
		}

		// Token: 0x060001BC RID: 444 RVA: 0x000194EA File Offset: 0x000176EA
		public static float square(float x)
		{
			return x * x;
		}

		// Token: 0x060001BD RID: 445 RVA: 0x000194F0 File Offset: 0x000176F0
		public static int ilog(int n)
		{
			if (n < 0)
			{
				return 0;
			}
			if (n < 16384)
			{
				if (n < 16)
				{
					return (int)StbVorbis.log2_4[n];
				}
				if (n < 512)
				{
					return (int)(5 + StbVorbis.log2_4[n >> 5]);
				}
				return (int)(10 + StbVorbis.log2_4[n >> 10]);
			}
			else if (n < 16777216)
			{
				if (n < 524288)
				{
					return (int)(15 + StbVorbis.log2_4[n >> 15]);
				}
				return (int)(20 + StbVorbis.log2_4[n >> 20]);
			}
			else
			{
				if (n < 536870912)
				{
					return (int)(25 + StbVorbis.log2_4[n >> 25]);
				}
				return (int)(30 + StbVorbis.log2_4[n >> 30]);
			}
		}

		// Token: 0x060001BE RID: 446 RVA: 0x0001958C File Offset: 0x0001778C
		public static float float32_unpack(uint x)
		{
			uint num = x & 2097151U;
			bool flag = (x & 2147483648U) != 0U;
			uint num2 = (x & 2145386496U) >> 21;
			return (float)CRuntime.ldexp((double)((float)(flag ? (-num) : num)), (int)(num2 - 788U));
		}

		// Token: 0x060001BF RID: 447 RVA: 0x000195CE File Offset: 0x000177CE
		public unsafe static void add_entry(StbVorbis.Codebook* c, uint huff_code, int symbol, int count, int len, uint* values)
		{
			if (c->sparse == 0)
			{
				c->codewords[symbol] = huff_code;
				return;
			}
			c->codewords[count] = huff_code;
			c->codeword_lengths[count] = (byte)len;
			values[count] = (uint)symbol;
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00019608 File Offset: 0x00017808
		public unsafe static int compute_codewords(StbVorbis.Codebook* c, byte* len, int n, uint* values)
		{
			int num = 0;
			uint* ptr = stackalloc uint[(UIntPtr)128];
			CRuntime.memset((void*)ptr, 0, 128L);
			int num2 = 0;
			while (num2 < n && len[num2] >= 255)
			{
				num2++;
			}
			if (num2 == n)
			{
				return 1;
			}
			StbVorbis.add_entry(c, 0U, num2, num++, (int)len[num2], values);
			for (int i = 1; i <= (int)len[num2]; i++)
			{
				ptr[i] = 1U << 32 - i;
			}
			for (int i = num2 + 1; i < n; i++)
			{
				int num3 = (int)len[i];
				if (num3 != 255)
				{
					while (num3 > 0 && ptr[num3] == 0U)
					{
						num3--;
					}
					if (num3 == 0)
					{
						return 0;
					}
					uint num4 = ptr[num3];
					ptr[num3] = 0U;
					StbVorbis.add_entry(c, StbVorbis.bit_reverse(num4), i, num++, (int)len[i], values);
					if (num3 != (int)len[i])
					{
						for (int j = (int)len[i]; j > num3; j--)
						{
							ptr[j] = (uint)((ulong)num4 + (ulong)(1L << ((32 - j) & 31)));
						}
					}
				}
			}
			return 1;
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x0001971C File Offset: 0x0001791C
		public unsafe static void compute_accelerated_huffman(StbVorbis.Codebook* c)
		{
			for (int i = 0; i < 1024; i++)
			{
				*((ref c->fast_huffman.FixedElementField) + (IntPtr)i * 2) = -1;
			}
			int num = ((c->sparse != 0) ? c->sorted_entries : c->entries);
			if (num > 32767)
			{
				num = 32767;
			}
			for (int i = 0; i < num; i++)
			{
				if (c->codeword_lengths[i] <= 10)
				{
					for (uint num2 = ((c->sparse != 0) ? StbVorbis.bit_reverse(c->sorted_codewords[i]) : c->codewords[i]); num2 < 1024U; num2 += 1U << (int)c->codeword_lengths[i])
					{
						*((ref c->fast_huffman.FixedElementField) + (IntPtr)((ulong)num2 * 2UL)) = (short)i;
					}
				}
			}
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x000197E4 File Offset: 0x000179E4
		public unsafe static int uint32_compare(void* p, void* q)
		{
			uint num = *(uint*)p;
			uint num2 = *(uint*)q;
			if (num < num2)
			{
				return -1;
			}
			if (num <= num2)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x00019804 File Offset: 0x00017A04
		public unsafe static int include_in_sort(StbVorbis.Codebook* c, byte len)
		{
			if (c->sparse != 0)
			{
				return 1;
			}
			if (len == 255)
			{
				return 0;
			}
			if (len > 10)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00019824 File Offset: 0x00017A24
		public unsafe static void compute_sorted_huffman(StbVorbis.Codebook* c, byte* lengths, uint* values)
		{
			if (c->sparse == 0)
			{
				int num = 0;
				for (int i = 0; i < c->entries; i++)
				{
					if (StbVorbis.include_in_sort(c, lengths[i]) != 0)
					{
						c->sorted_codewords[(IntPtr)(num++) * 4] = StbVorbis.bit_reverse(c->codewords[i]);
					}
				}
			}
			else
			{
				for (int i = 0; i < c->sorted_entries; i++)
				{
					c->sorted_codewords[i] = StbVorbis.bit_reverse(c->codewords[i]);
				}
			}
			CRuntime.qsort((void*)c->sorted_codewords, (ulong)((long)c->sorted_entries), 4UL, new CRuntime.QSortComparer(StbVorbis.uint32_compare));
			c->sorted_codewords[c->sorted_entries] = uint.MaxValue;
			int num2 = ((c->sparse != 0) ? c->sorted_entries : c->entries);
			for (int i = 0; i < num2; i++)
			{
				int num3 = (int)((c->sparse != 0) ? lengths[values[i]] : lengths[i]);
				if (StbVorbis.include_in_sort(c, (byte)num3) != 0)
				{
					uint num4 = StbVorbis.bit_reverse(c->codewords[i]);
					int num5 = 0;
					int j = c->sorted_entries;
					while (j > 1)
					{
						int num6 = num5 + (j >> 1);
						if (c->sorted_codewords[num6] <= num4)
						{
							num5 = num6;
							j -= j >> 1;
						}
						else
						{
							j >>= 1;
						}
					}
					if (c->sparse != 0)
					{
						c->sorted_values[num5] = (int)values[i];
						c->codeword_lengths[num5] = (byte)num3;
					}
					else
					{
						c->sorted_values[num5] = i;
					}
				}
			}
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x000199B8 File Offset: 0x00017BB8
		public unsafe static int vorbis_validate(byte* data)
		{
			byte* ptr = stackalloc byte[(UIntPtr)6];
			*ptr = 118;
			ptr[1] = 111;
			ptr[2] = 114;
			ptr[3] = 98;
			ptr[4] = 105;
			ptr[5] = 115;
			if (CRuntime.memcmp((void*)data, (void*)ptr, 6UL) != 0)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x000199FC File Offset: 0x00017BFC
		public static int lookup1_values(int entries, int dim)
		{
			int num = (int)CRuntime.floor(CRuntime.exp((double)((float)CRuntime.log((double)((float)entries)) / (float)dim)));
			if ((int)CRuntime.floor(CRuntime.pow((double)((float)num + 1f), (double)dim)) <= entries)
			{
				num++;
			}
			return num;
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00019A44 File Offset: 0x00017C44
		public unsafe static void compute_twiddle_factors(int n, float* A, float* B, float* C)
		{
			int num = n >> 2;
			int num2 = n >> 3;
			int i;
			int num3 = (i = 0);
			while (i < num)
			{
				A[num3] = (float)CRuntime.cos((double)((float)(4 * i) * 3.1415927f / (float)n));
				A[num3 + 1] = (float)(-(float)CRuntime.sin((double)((float)(4 * i) * 3.1415927f / (float)n)));
				B[num3] = (float)CRuntime.cos((double)((float)(num3 + 1) * 3.1415927f / (float)n / 2f)) * 0.5f;
				B[num3 + 1] = (float)CRuntime.sin((double)((float)(num3 + 1) * 3.1415927f / (float)n / 2f)) * 0.5f;
				i++;
				num3 += 2;
			}
			num3 = (i = 0);
			while (i < num2)
			{
				C[num3] = (float)CRuntime.cos((double)((float)(2 * (num3 + 1)) * 3.1415927f / (float)n));
				C[num3 + 1] = (float)(-(float)CRuntime.sin((double)((float)(2 * (num3 + 1)) * 3.1415927f / (float)n)));
				i++;
				num3 += 2;
			}
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00019B4C File Offset: 0x00017D4C
		public unsafe static void compute_window(int n, float* window)
		{
			int num = n >> 1;
			for (int i = 0; i < num; i++)
			{
				window[i] = (float)CRuntime.sin(1.5707963705062866 * (double)StbVorbis.square((float)CRuntime.sin(((double)i + 0.5) / (double)num * 0.5 * 3.1415927410125732)));
			}
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00019BB4 File Offset: 0x00017DB4
		public unsafe static void compute_bitreverse(int n, ushort* rev)
		{
			int num = StbVorbis.ilog(n) - 1;
			int num2 = n >> 3;
			for (int i = 0; i < num2; i++)
			{
				rev[i] = (ushort)(StbVorbis.bit_reverse((uint)i) >> 32 - num + 3 << 2);
			}
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00019BF4 File Offset: 0x00017DF4
		public unsafe static int init_blocksize(StbVorbis.stb_vorbis f, int b, int n)
		{
			int num = n >> 1;
			int num2 = n >> 2;
			int num3 = n >> 3;
			f.A[b] = (float*)StbVorbis.setup_malloc(f, 4 * num);
			f.B[b] = (float*)StbVorbis.setup_malloc(f, 4 * num);
			f.C[b] = (float*)StbVorbis.setup_malloc(f, 4 * num2);
			if (f.A[b] == null || f.B[b] == null || f.C[b] == null)
			{
				return StbVorbis.error(f, 3);
			}
			StbVorbis.compute_twiddle_factors(n, f.A[b], f.B[b], f.C[b]);
			f.window[b] = (float*)StbVorbis.setup_malloc(f, 4 * num);
			if (f.window[b] == null)
			{
				return StbVorbis.error(f, 3);
			}
			StbVorbis.compute_window(n, f.window[b]);
			f.bit_reverse[b] = (ushort*)StbVorbis.setup_malloc(f, 2 * num3);
			if (f.bit_reverse[b] == null)
			{
				return StbVorbis.error(f, 3);
			}
			StbVorbis.compute_bitreverse(n, f.bit_reverse[b]);
			return 1;
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00019CF4 File Offset: 0x00017EF4
		public unsafe static void neighbors(ushort* x, int n, int* plow, int* phigh)
		{
			int num = -1;
			int num2 = 65536;
			for (int i = 0; i < n; i++)
			{
				if ((int)x[i] > num && x[i] < x[n])
				{
					*plow = i;
					num = (int)x[i];
				}
				if ((int)x[i] < num2 && x[i] > x[n])
				{
					*phigh = i;
					num2 = (int)x[i];
				}
			}
		}

		// Token: 0x060001CC RID: 460 RVA: 0x00019D60 File Offset: 0x00017F60
		public unsafe static int point_compare(void* p, void* q)
		{
			if (((StbVorbis.stbv__floor_ordering*)p)->x < ((StbVorbis.stbv__floor_ordering*)q)->x)
			{
				return -1;
			}
			if (((StbVorbis.stbv__floor_ordering*)p)->x <= ((StbVorbis.stbv__floor_ordering*)q)->x)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060001CD RID: 461 RVA: 0x00019D94 File Offset: 0x00017F94
		public unsafe static byte get8(StbVorbis.stb_vorbis z)
		{
			if (z.stream >= z.stream_end)
			{
				z.eof = 1;
				return 0;
			}
			byte* stream = z.stream;
			z.stream = stream + 1;
			return *stream;
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00019DCA File Offset: 0x00017FCA
		public static uint get32(StbVorbis.stb_vorbis f)
		{
			return (uint)((int)StbVorbis.get8(f) + ((int)StbVorbis.get8(f) << 8) + ((int)StbVorbis.get8(f) << 16) + ((int)StbVorbis.get8(f) << 24));
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00019DEF File Offset: 0x00017FEF
		public unsafe static int getn(StbVorbis.stb_vorbis z, byte* data, int n)
		{
			if (z.stream + n != z.stream_end)
			{
				z.eof = 1;
				return 0;
			}
			CRuntime.memcpy((void*)data, (void*)z.stream, (ulong)((long)n));
			z.stream += n;
			return 1;
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00019E27 File Offset: 0x00018027
		public static void skip(StbVorbis.stb_vorbis z, int n)
		{
			z.stream += n;
			if (z.stream >= z.stream_end)
			{
				z.eof = 1;
			}
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x00019E4C File Offset: 0x0001804C
		public static int set_file_offset(StbVorbis.stb_vorbis f, uint loc)
		{
			if (f.push_mode != 0)
			{
				return 0;
			}
			f.eof = 0;
			if (f.stream_start + loc >= f.stream_end || f.stream_start + loc < f.stream_start)
			{
				f.stream = f.stream_end;
				f.eof = 1;
				return 0;
			}
			f.stream = f.stream_start + loc;
			return 1;
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x00019EB1 File Offset: 0x000180B1
		public static int capture_pattern(StbVorbis.stb_vorbis f)
		{
			if (79 != StbVorbis.get8(f))
			{
				return 0;
			}
			if (103 != StbVorbis.get8(f))
			{
				return 0;
			}
			if (103 != StbVorbis.get8(f))
			{
				return 0;
			}
			if (83 != StbVorbis.get8(f))
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x00019EE4 File Offset: 0x000180E4
		public unsafe static int start_page_no_capturepattern(StbVorbis.stb_vorbis f)
		{
			if (StbVorbis.get8(f) != 0)
			{
				return StbVorbis.error(f, 31);
			}
			f.page_flag = StbVorbis.get8(f);
			uint num = StbVorbis.get32(f);
			uint num2 = StbVorbis.get32(f);
			StbVorbis.get32(f);
			uint last_page = StbVorbis.get32(f);
			f.last_page = (int)last_page;
			StbVorbis.get32(f);
			f.segment_count = (int)StbVorbis.get8(f);
			if (StbVorbis.getn(f, f.segments, f.segment_count) == 0)
			{
				return StbVorbis.error(f, 10);
			}
			f.end_seg_with_known_loc = -2;
			if (num != 4294967295U || num2 != 4294967295U)
			{
				int num3 = f.segment_count - 1;
				while (num3 >= 0 && f.segments[num3] >= 255)
				{
					num3--;
				}
				if (num3 >= 0)
				{
					f.end_seg_with_known_loc = num3;
					f.known_loc_for_packet = num;
				}
			}
			if (f.first_decode != 0)
			{
				StbVorbis.ProbedPage probedPage = default(StbVorbis.ProbedPage);
				int num4 = 0;
				for (int i = 0; i < f.segment_count; i++)
				{
					num4 += (int)f.segments[i];
				}
				num4 += 27 + f.segment_count;
				probedPage.page_start = f.first_audio_page_offset;
				probedPage.page_end = (uint)((ulong)probedPage.page_start + (ulong)((long)num4));
				probedPage.last_decoded_sample = num;
				f.p_first = probedPage;
			}
			f.next_seg = 0;
			return 1;
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x0001A024 File Offset: 0x00018224
		public static int start_page(StbVorbis.stb_vorbis f)
		{
			if (StbVorbis.capture_pattern(f) == 0)
			{
				return StbVorbis.error(f, 30);
			}
			return StbVorbis.start_page_no_capturepattern(f);
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0001A040 File Offset: 0x00018240
		public static int start_packet(StbVorbis.stb_vorbis f)
		{
			while (f.next_seg == -1)
			{
				if (StbVorbis.start_page(f) == 0)
				{
					return 0;
				}
				if ((f.page_flag & 1) != 0)
				{
					return StbVorbis.error(f, 32);
				}
			}
			f.last_seg = 0;
			f.valid_bits = 0;
			f.packet_bytes = 0;
			f.bytes_in_seg = 0;
			return 1;
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0001A094 File Offset: 0x00018294
		public static int maybe_start_packet(StbVorbis.stb_vorbis f)
		{
			if (f.next_seg == -1)
			{
				int num = (int)StbVorbis.get8(f);
				if (f.eof != 0)
				{
					return 0;
				}
				if (79 != num)
				{
					return StbVorbis.error(f, 30);
				}
				if (103 != StbVorbis.get8(f))
				{
					return StbVorbis.error(f, 30);
				}
				if (103 != StbVorbis.get8(f))
				{
					return StbVorbis.error(f, 30);
				}
				if (83 != StbVorbis.get8(f))
				{
					return StbVorbis.error(f, 30);
				}
				if (StbVorbis.start_page_no_capturepattern(f) == 0)
				{
					return 0;
				}
				if ((f.page_flag & 1) != 0)
				{
					f.last_seg = 0;
					f.bytes_in_seg = 0;
					return StbVorbis.error(f, 32);
				}
			}
			return StbVorbis.start_packet(f);
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0001A138 File Offset: 0x00018338
		public unsafe static int next_segment(StbVorbis.stb_vorbis f)
		{
			if (f.last_seg != 0)
			{
				return 0;
			}
			if (f.next_seg == -1)
			{
				f.last_seg_which = f.segment_count - 1;
				if (StbVorbis.start_page(f) == 0)
				{
					f.last_seg = 1;
					return 0;
				}
				if ((f.page_flag & 1) == 0)
				{
					return StbVorbis.error(f, 32);
				}
			}
			int segments = f.segments;
			int next_seg = f.next_seg;
			f.next_seg = next_seg + 1;
			int num = (int)(*(segments + next_seg));
			if (num < 255)
			{
				f.last_seg = 1;
				f.last_seg_which = f.next_seg - 1;
			}
			if (f.next_seg >= f.segment_count)
			{
				f.next_seg = -1;
			}
			f.bytes_in_seg = (byte)num;
			return num;
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0001A1E0 File Offset: 0x000183E0
		public static int get8_packet_raw(StbVorbis.stb_vorbis f)
		{
			if (f.bytes_in_seg == 0)
			{
				if (f.last_seg != 0)
				{
					return -1;
				}
				if (StbVorbis.next_segment(f) == 0)
				{
					return -1;
				}
			}
			f.bytes_in_seg -= 1;
			f.packet_bytes++;
			return (int)StbVorbis.get8(f);
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0001A22C File Offset: 0x0001842C
		public static int get8_packet(StbVorbis.stb_vorbis f)
		{
			int result = StbVorbis.get8_packet_raw(f);
			f.valid_bits = 0;
			return result;
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0001A23B File Offset: 0x0001843B
		public static void flush_packet(StbVorbis.stb_vorbis f)
		{
			while (StbVorbis.get8_packet_raw(f) != -1)
			{
			}
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0001A248 File Offset: 0x00018448
		public static void prep_huffman(StbVorbis.stb_vorbis f)
		{
			if (f.valid_bits <= 24)
			{
				if (f.valid_bits == 0)
				{
					f.acc = 0U;
				}
				while (f.last_seg == 0 || f.bytes_in_seg != 0)
				{
					int num = StbVorbis.get8_packet_raw(f);
					if (num == -1)
					{
						return;
					}
					f.acc += (uint)((uint)num << f.valid_bits);
					f.valid_bits += 8;
					if (f.valid_bits > 24)
					{
						return;
					}
				}
				return;
			}
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0001A2BC File Offset: 0x000184BC
		public unsafe static int codebook_decode_scalar_raw(StbVorbis.stb_vorbis f, StbVorbis.Codebook* c)
		{
			StbVorbis.prep_huffman(f);
			if (c->codewords == null && c->sorted_codewords == null)
			{
				return -1;
			}
			if (!((c->entries > 8) ? (c->sorted_codewords != null) : (c->codewords == null)))
			{
				int i = 0;
				while (i < c->entries)
				{
					if (c->codeword_lengths[i] != 255 && (ulong)c->codewords[i] == ((ulong)f.acc & (ulong)((long)((1 << (int)c->codeword_lengths[i]) - 1))))
					{
						if (f.valid_bits >= (int)c->codeword_lengths[i])
						{
							f.acc >>= (int)c->codeword_lengths[i];
							f.valid_bits -= (int)c->codeword_lengths[i];
							return i;
						}
						f.valid_bits = 0;
						return -1;
					}
					else
					{
						i++;
					}
				}
				StbVorbis.error(f, 21);
				f.valid_bits = 0;
				return -1;
			}
			uint num = StbVorbis.bit_reverse(f.acc);
			int num2 = 0;
			int j = c->sorted_entries;
			while (j > 1)
			{
				int num3 = num2 + (j >> 1);
				if (c->sorted_codewords[num3] <= num)
				{
					num2 = num3;
					j -= j >> 1;
				}
				else
				{
					j >>= 1;
				}
			}
			if (c->sparse == 0)
			{
				num2 = c->sorted_values[num2];
			}
			int num4 = (int)c->codeword_lengths[num2];
			if (f.valid_bits >= num4)
			{
				f.acc >>= num4;
				f.valid_bits -= num4;
				return num2;
			}
			f.valid_bits = 0;
			return -1;
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0001A450 File Offset: 0x00018650
		public unsafe static int codebook_decode_scalar(StbVorbis.stb_vorbis f, StbVorbis.Codebook* c)
		{
			if (f.valid_bits < 10)
			{
				StbVorbis.prep_huffman(f);
			}
			int num = (int)(f.acc & 1023U);
			num = (int)(*((ref c->fast_huffman.FixedElementField) + (IntPtr)num * 2));
			if (num < 0)
			{
				return StbVorbis.codebook_decode_scalar_raw(f, c);
			}
			f.acc >>= (int)c->codeword_lengths[num];
			f.valid_bits -= (int)c->codeword_lengths[num];
			if (f.valid_bits < 0)
			{
				f.valid_bits = 0;
				return -1;
			}
			return num;
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0001A4DC File Offset: 0x000186DC
		public unsafe static int codebook_decode_start(StbVorbis.stb_vorbis f, StbVorbis.Codebook* c)
		{
			int num = -1;
			if (c->lookup_type == 0)
			{
				StbVorbis.error(f, 21);
			}
			else
			{
				num = StbVorbis.codebook_decode_scalar(f, c);
				byte sparse = c->sparse;
				if (num < 0)
				{
					if (f.bytes_in_seg == 0 && f.last_seg != 0)
					{
						return num;
					}
					StbVorbis.error(f, 21);
				}
			}
			return num;
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0001A530 File Offset: 0x00018730
		public unsafe static int codebook_decode(StbVorbis.stb_vorbis f, StbVorbis.Codebook* c, float* output, int len)
		{
			int num = StbVorbis.codebook_decode_start(f, c);
			if (num < 0)
			{
				return 0;
			}
			if (len > c->dimensions)
			{
				len = c->dimensions;
			}
			num *= c->dimensions;
			if (c->sequence_p != 0)
			{
				float num2 = 0f;
				for (int i = 0; i < len; i++)
				{
					float num3 = c->multiplicands[num + i] + num2;
					output[i] += num3;
					num2 = num3 + c->minimum_value;
				}
			}
			else
			{
				float num4 = 0f;
				for (int i = 0; i < len; i++)
				{
					output[i] += c->multiplicands[num + i] + num4;
				}
			}
			return 1;
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x0001A5DC File Offset: 0x000187DC
		public unsafe static int codebook_decode_step(StbVorbis.stb_vorbis f, StbVorbis.Codebook* c, float* output, int len, int step)
		{
			int num = StbVorbis.codebook_decode_start(f, c);
			float num2 = 0f;
			if (num < 0)
			{
				return 0;
			}
			if (len > c->dimensions)
			{
				len = c->dimensions;
			}
			num *= c->dimensions;
			for (int i = 0; i < len; i++)
			{
				float num3 = c->multiplicands[num + i] + num2;
				output[i * step] += num3;
				if (c->sequence_p != 0)
				{
					num2 = num3;
				}
			}
			return 1;
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0001A650 File Offset: 0x00018850
		public unsafe static int codebook_decode_deinterleave_repeat(StbVorbis.stb_vorbis f, StbVorbis.Codebook* c, float** outputs, int ch, int* c_inter_p, int* p_inter_p, int len, int total_decode)
		{
			int num = *c_inter_p;
			int num2 = *p_inter_p;
			int num3 = c->dimensions;
			if (c->lookup_type == 0)
			{
				return StbVorbis.error(f, 21);
			}
			while (total_decode > 0)
			{
				float num4 = 0f;
				int num5 = StbVorbis.codebook_decode_scalar(f, c);
				if (num5 < 0)
				{
					if (f.bytes_in_seg == 0 && f.last_seg != 0)
					{
						return 0;
					}
					return StbVorbis.error(f, 21);
				}
				else
				{
					if (num + num2 * ch + num3 > len * ch)
					{
						num3 = len * ch - (num2 * ch - num);
					}
					num5 *= c->dimensions;
					if (c->sequence_p != 0)
					{
						for (int i = 0; i < num3; i++)
						{
							float num6 = c->multiplicands[num5 + i] + num4;
							if (*(IntPtr*)(outputs + (IntPtr)num * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) != (IntPtr)((UIntPtr)0))
							{
								*(*(IntPtr*)(outputs + (IntPtr)num * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) + (IntPtr)num2 * 4) += num6;
							}
							if (++num == ch)
							{
								num = 0;
								num2++;
							}
							num4 = num6;
						}
					}
					else
					{
						for (int i = 0; i < num3; i++)
						{
							float num7 = c->multiplicands[num5 + i] + num4;
							if (*(IntPtr*)(outputs + (IntPtr)num * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) != (IntPtr)((UIntPtr)0))
							{
								*(*(IntPtr*)(outputs + (IntPtr)num * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) + (IntPtr)num2 * 4) += num7;
							}
							if (++num == ch)
							{
								num = 0;
								num2++;
							}
						}
					}
					total_decode -= num3;
				}
			}
			*c_inter_p = num;
			*p_inter_p = num2;
			return 1;
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x0001A7A8 File Offset: 0x000189A8
		public static int predict_point(int x, int x0, int x1, int y0, int y1)
		{
			int num = y1 - y0;
			int num2 = x1 - x0;
			int num3 = CRuntime.abs(num) * (x - x0) / num2;
			if (num >= 0)
			{
				return y0 + num3;
			}
			return y0 - num3;
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0001A7D4 File Offset: 0x000189D4
		public unsafe static void draw_line(float* output, int x0, int y0, int x1, int y1, int n)
		{
			int num = y1 - y0;
			int num2 = x1 - x0;
			int num3 = CRuntime.abs(num);
			int num4 = y0;
			int num5 = 0;
			int num6 = num / num2;
			int num7;
			if (num < 0)
			{
				num7 = num6 - 1;
			}
			else
			{
				num7 = num6 + 1;
			}
			num3 -= CRuntime.abs(num6) * num2;
			if (x1 > n)
			{
				x1 = n;
			}
			if (x0 < x1)
			{
				output[x0] *= StbVorbis.inverse_db_table[num4];
				for (int i = x0 + 1; i < x1; i++)
				{
					num5 += num3;
					if (num5 >= num2)
					{
						num5 -= num2;
						num4 += num7;
					}
					else
					{
						num4 += num6;
					}
					output[i] *= StbVorbis.inverse_db_table[num4];
				}
			}
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0001A878 File Offset: 0x00018A78
		public unsafe static int residue_decode(StbVorbis.stb_vorbis f, StbVorbis.Codebook* book, float* target, int offset, int n, int rtype)
		{
			if (rtype == 0)
			{
				int num = n / book->dimensions;
				for (int i = 0; i < num; i++)
				{
					if (StbVorbis.codebook_decode_step(f, book, target + offset + i, n - offset - i, num) == 0)
					{
						return 0;
					}
				}
			}
			else
			{
				int i = 0;
				while (i < n)
				{
					if (StbVorbis.codebook_decode(f, book, target + offset, n - i) == 0)
					{
						return 0;
					}
					i += book->dimensions;
					offset += book->dimensions;
				}
			}
			return 1;
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0001A8F0 File Offset: 0x00018AF0
		public unsafe static void decode_residue(StbVorbis.stb_vorbis f, float** residue_buffers, int ch, int n, int rn, byte* do_not_decode)
		{
			StbVorbis.Residue residue = f.residue_config[rn];
			int num = (int)f.residue_types[rn];
			int num2 = (int)residue.classbook;
			int dimensions = f.codebooks[num2].dimensions;
			int num3 = (int)((long)(residue.end - residue.begin) / (long)((ulong)residue.part_size));
			int temp_offset = f.temp_offset;
			byte*** ptr = (byte***)StbVorbis.make_block_array((f.alloc.alloc_buffer != null) ? StbVorbis.setup_temp_malloc(f, f.channels * (sizeof(void*) + num3 * sizeof(byte*))) : CRuntime.malloc((ulong)((long)(f.channels * (sizeof(void*) + num3 * sizeof(byte*))))), f.channels, num3 * sizeof(byte*));
			for (int i = 0; i < ch; i++)
			{
				if (do_not_decode[i] == 0)
				{
					CRuntime.memset(*(IntPtr*)(residue_buffers + (IntPtr)i * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)), 0, (ulong)((long)(4 * n)));
				}
			}
			if (num == 2 && ch != 1)
			{
				int j = 0;
				while (j < ch && do_not_decode[j] != 0)
				{
					j++;
				}
				if (j != ch)
				{
					for (int k = 0; k < 8; k++)
					{
						int l = 0;
						int num4 = 0;
						if (ch == 2)
						{
							while (l < num3)
							{
								int num5 = (int)((ulong)residue.begin + (ulong)((long)l * (long)((ulong)residue.part_size)));
								int num6 = num5 & 1;
								int num7 = num5 >> 1;
								if (k == 0)
								{
									StbVorbis.Codebook* ptr2 = f.codebooks + residue.classbook;
									int num8 = StbVorbis.codebook_decode_scalar(f, ptr2);
									if (ptr2->sparse != 0)
									{
										num8 = ptr2->sorted_values[num8];
									}
									if (num8 == -1)
									{
										goto IL_656;
									}
									*(*(IntPtr*)ptr + (IntPtr)num4 * (IntPtr)sizeof(byte*)) = *(IntPtr*)(residue.classdata + (IntPtr)num8 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*));
								}
								int i = 0;
								while (i < dimensions && l < num3)
								{
									int num9 = (int)((ulong)residue.begin + (ulong)((long)l * (long)((ulong)residue.part_size)));
									int num10 = (int)(*(*(*(IntPtr*)ptr + (IntPtr)num4 * (IntPtr)sizeof(byte*)) + (IntPtr)i));
									int num11 = (int)residue.residue_books[num10, k];
									if (num11 >= 0)
									{
										StbVorbis.Codebook* c = f.codebooks + num11;
										if (StbVorbis.codebook_decode_deinterleave_repeat(f, c, residue_buffers, ch, &num6, &num7, n, (int)residue.part_size) == 0)
										{
											goto IL_656;
										}
									}
									else
									{
										num9 += (int)residue.part_size;
										num6 = num9 & 1;
										num7 = num9 >> 1;
									}
									i++;
									l++;
								}
								num4++;
							}
						}
						else if (ch == 1)
						{
							while (l < num3)
							{
								int num12 = (int)((ulong)residue.begin + (ulong)((long)l * (long)((ulong)residue.part_size)));
								int num13 = 0;
								int num14 = num12;
								if (k == 0)
								{
									StbVorbis.Codebook* ptr3 = f.codebooks + residue.classbook;
									int num15 = StbVorbis.codebook_decode_scalar(f, ptr3);
									if (ptr3->sparse != 0)
									{
										num15 = ptr3->sorted_values[num15];
									}
									if (num15 == -1)
									{
										goto IL_656;
									}
									*(*(IntPtr*)ptr + (IntPtr)num4 * (IntPtr)sizeof(byte*)) = *(IntPtr*)(residue.classdata + (IntPtr)num15 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*));
								}
								int i = 0;
								while (i < dimensions && l < num3)
								{
									int num16 = (int)((ulong)residue.begin + (ulong)((long)l * (long)((ulong)residue.part_size)));
									byte b = *(*(*(IntPtr*)ptr + (IntPtr)num4 * (IntPtr)sizeof(byte*)) + (IntPtr)i);
									int num17 = (int)residue.residue_books[num2, k];
									if (num17 >= 0)
									{
										StbVorbis.Codebook* c2 = f.codebooks + num17;
										if (StbVorbis.codebook_decode_deinterleave_repeat(f, c2, residue_buffers, ch, &num13, &num14, n, (int)residue.part_size) == 0)
										{
											goto IL_656;
										}
									}
									else
									{
										num16 += (int)residue.part_size;
										num13 = 0;
										num14 = num16;
									}
									i++;
									l++;
								}
								num4++;
							}
						}
						else
						{
							while (l < num3)
							{
								int num18 = (int)((ulong)residue.begin + (ulong)((long)l * (long)((ulong)residue.part_size)));
								int num19 = num18 % ch;
								int num20 = num18 / ch;
								if (k == 0)
								{
									StbVorbis.Codebook* ptr4 = f.codebooks + residue.classbook;
									int num21 = StbVorbis.codebook_decode_scalar(f, ptr4);
									if (ptr4->sparse != 0)
									{
										num21 = ptr4->sorted_values[num21];
									}
									if (num21 == -1)
									{
										goto IL_656;
									}
									*(*(IntPtr*)ptr + (IntPtr)num4 * (IntPtr)sizeof(byte*)) = *(IntPtr*)(residue.classdata + (IntPtr)num21 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*));
								}
								int i = 0;
								while (i < dimensions && l < num3)
								{
									int num22 = (int)((ulong)residue.begin + (ulong)((long)l * (long)((ulong)residue.part_size)));
									byte b2 = *(*(*(IntPtr*)ptr + (IntPtr)num4 * (IntPtr)sizeof(byte*)) + (IntPtr)i);
									int num23 = (int)residue.residue_books[num2, k];
									if (num23 >= 0)
									{
										StbVorbis.Codebook* c3 = f.codebooks + num23;
										if (StbVorbis.codebook_decode_deinterleave_repeat(f, c3, residue_buffers, ch, &num19, &num20, n, (int)residue.part_size) == 0)
										{
											goto IL_656;
										}
									}
									else
									{
										num22 += (int)residue.part_size;
										num19 = num22 % ch;
										num20 = num22 / ch;
									}
									i++;
									l++;
								}
								num4++;
							}
						}
					}
				}
			}
			else
			{
				for (int k = 0; k < 8; k++)
				{
					int m = 0;
					int num24 = 0;
					while (m < num3)
					{
						if (k == 0)
						{
							for (int j = 0; j < ch; j++)
							{
								if (do_not_decode[j] == 0)
								{
									StbVorbis.Codebook* ptr5 = f.codebooks + residue.classbook;
									int num25 = StbVorbis.codebook_decode_scalar(f, ptr5);
									if (ptr5->sparse != 0)
									{
										num25 = ptr5->sorted_values[num25];
									}
									if (num25 == -1)
									{
										goto IL_656;
									}
									*(*(IntPtr*)(ptr + (IntPtr)j * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) + (IntPtr)num24 * (IntPtr)sizeof(byte*)) = *(IntPtr*)(residue.classdata + (IntPtr)num25 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*));
								}
							}
						}
						int i = 0;
						while (i < dimensions && m < num3)
						{
							for (int j = 0; j < ch; j++)
							{
								if (do_not_decode[j] == 0)
								{
									num2 = (int)(*(*(*(IntPtr*)(ptr + (IntPtr)j * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) + (IntPtr)num24 * (IntPtr)sizeof(byte*)) + (IntPtr)i));
									int num26 = (int)residue.residue_books[num2, k];
									if (num26 >= 0)
									{
										float* target = *(IntPtr*)(residue_buffers + (IntPtr)j * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*));
										int offset = (int)((ulong)residue.begin + (ulong)((long)m * (long)((ulong)residue.part_size)));
										int part_size = (int)residue.part_size;
										StbVorbis.Codebook* book = f.codebooks + num26;
										if (StbVorbis.residue_decode(f, book, target, offset, part_size, num) == 0)
										{
											goto IL_656;
										}
									}
								}
							}
							i++;
							m++;
						}
						num24++;
					}
				}
			}
			IL_656:
			CRuntime.free((void*)ptr);
			f.temp_offset = temp_offset;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0001AF64 File Offset: 0x00019164
		public unsafe static void imdct_step3_iter0_loop(int n, float* e, int i_off, int k_off, float* A)
		{
			float* ptr = e + i_off;
			float* ptr2 = ptr + k_off;
			for (int i = n >> 2; i > 0; i--)
			{
				float num = *ptr - *ptr2;
				float num2 = ptr[-1] - ptr2[-1];
				*ptr += *ptr2;
				ptr[-1] += ptr2[-1];
				*ptr2 = num * *A - num2 * A[1];
				ptr2[-1] = num2 * *A + num * A[1];
				A += 8;
				num = ptr[-2] - ptr2[-2];
				num2 = ptr[-3] - ptr2[-3];
				ptr[-2] += ptr2[-2];
				ptr[-3] += ptr2[-3];
				ptr2[-2] = num * *A - num2 * A[1];
				ptr2[-3] = num2 * *A + num * A[1];
				A += 8;
				num = ptr[-4] - ptr2[-4];
				num2 = ptr[-5] - ptr2[-5];
				ptr[-4] += ptr2[-4];
				ptr[-5] += ptr2[-5];
				ptr2[-4] = num * *A - num2 * A[1];
				ptr2[-5] = num2 * *A + num * A[1];
				A += 8;
				num = ptr[-6] - ptr2[-6];
				num2 = ptr[-7] - ptr2[-7];
				ptr[-6] += ptr2[-6];
				ptr[-7] += ptr2[-7];
				ptr2[-6] = num * *A - num2 * A[1];
				ptr2[-7] = num2 * *A + num * A[1];
				A += 8;
				ptr -= 8;
				ptr2 -= 8;
			}
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0001B198 File Offset: 0x00019398
		public unsafe static void imdct_step3_inner_r_loop(int lim, float* e, int d0, int k_off, float* A, int k1)
		{
			float* ptr = e + d0;
			float* ptr2 = ptr + k_off;
			for (int i = lim >> 2; i > 0; i--)
			{
				float num = *ptr - *ptr2;
				float num2 = ptr[-1] - ptr2[-1];
				*ptr += *ptr2;
				ptr[-1] += ptr2[-1];
				*ptr2 = num * *A - num2 * A[1];
				ptr2[-1] = num2 * *A + num * A[1];
				A += k1;
				num = ptr[-2] - ptr2[-2];
				num2 = ptr[-3] - ptr2[-3];
				ptr[-2] += ptr2[-2];
				ptr[-3] += ptr2[-3];
				ptr2[-2] = num * *A - num2 * A[1];
				ptr2[-3] = num2 * *A + num * A[1];
				A += k1;
				num = ptr[-4] - ptr2[-4];
				num2 = ptr[-5] - ptr2[-5];
				ptr[-4] += ptr2[-4];
				ptr[-5] += ptr2[-5];
				ptr2[-4] = num * *A - num2 * A[1];
				ptr2[-5] = num2 * *A + num * A[1];
				A += k1;
				num = ptr[-6] - ptr2[-6];
				num2 = ptr[-7] - ptr2[-7];
				ptr[-6] += ptr2[-6];
				ptr[-7] += ptr2[-7];
				ptr2[-6] = num * *A - num2 * A[1];
				ptr2[-7] = num2 * *A + num * A[1];
				ptr -= 8;
				ptr2 -= 8;
				A += k1;
			}
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0001B3E0 File Offset: 0x000195E0
		public unsafe static void imdct_step3_inner_s_loop(int n, float* e, int i_off, int k_off, float* A, int a_off, int k0)
		{
			float num = *A;
			float num2 = A[1];
			float num3 = A[a_off];
			float num4 = A[a_off + 1];
			float num5 = A[a_off * 2];
			float num6 = A[a_off * 2 + 1];
			float num7 = A[a_off * 3];
			float num8 = A[a_off * 3 + 1];
			float* ptr = e + i_off;
			float* ptr2 = ptr + k_off;
			for (int i = n; i > 0; i--)
			{
				float num9 = *ptr - *ptr2;
				float num10 = ptr[-1] - ptr2[-1];
				*ptr += *ptr2;
				ptr[-1] = ptr[-1] + ptr2[-1];
				*ptr2 = num9 * num - num10 * num2;
				ptr2[-1] = num10 * num + num9 * num2;
				num9 = ptr[-2] - ptr2[-2];
				num10 = ptr[-3] - ptr2[-3];
				ptr[-2] = ptr[-2] + ptr2[-2];
				ptr[-3] = ptr[-3] + ptr2[-3];
				ptr2[-2] = num9 * num3 - num10 * num4;
				ptr2[-3] = num10 * num3 + num9 * num4;
				num9 = ptr[-4] - ptr2[-4];
				num10 = ptr[-5] - ptr2[-5];
				ptr[-4] = ptr[-4] + ptr2[-4];
				ptr[-5] = ptr[-5] + ptr2[-5];
				ptr2[-4] = num9 * num5 - num10 * num6;
				ptr2[-5] = num10 * num5 + num9 * num6;
				num9 = ptr[-6] - ptr2[-6];
				num10 = ptr[-7] - ptr2[-7];
				ptr[-6] = ptr[-6] + ptr2[-6];
				ptr[-7] = ptr[-7] + ptr2[-7];
				ptr2[-6] = num9 * num7 - num10 * num8;
				ptr2[-7] = num10 * num7 + num9 * num8;
				ptr -= k0;
				ptr2 -= k0;
			}
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0001B698 File Offset: 0x00019898
		public unsafe static void iter_54(float* z)
		{
			float num = *z - z[-4];
			float num2 = *z + z[-4];
			float num3 = z[-2] + z[-6];
			float num4 = z[-2] - z[-6];
			*z = num2 + num3;
			z[-2] = num2 - num3;
			float num5 = z[-3] - z[-7];
			z[-4] = num + num5;
			z[-6] = num - num5;
			float num6 = z[-1] - z[-5];
			float num7 = z[-1] + z[-5];
			float num8 = z[-3] + z[-7];
			z[-1] = num7 + num8;
			z[-3] = num7 - num8;
			z[-5] = num6 - num4;
			z[-7] = num6 + num4;
		}

		// Token: 0x060001EA RID: 490 RVA: 0x0001B794 File Offset: 0x00019994
		public unsafe static void imdct_step3_inner_s_loop_ld654(int n, float* e, int i_off, float* A, int base_n)
		{
			int num = base_n >> 3;
			float num2 = A[num];
			float* ptr = e + i_off;
			float* ptr2 = ptr - 16 * n;
			while (ptr != ptr2)
			{
				float num3 = *ptr - ptr[-8];
				float num4 = ptr[-1] - ptr[-9];
				*ptr += ptr[-8];
				ptr[-1] = ptr[-1] + ptr[-9];
				ptr[-8] = num3;
				ptr[-9] = num4;
				num3 = ptr[-2] - ptr[-10];
				num4 = ptr[-3] - ptr[-11];
				ptr[-2] = ptr[-2] + ptr[-10];
				ptr[-3] = ptr[-3] + ptr[-11];
				ptr[-10] = (num3 + num4) * num2;
				ptr[-11] = (num4 - num3) * num2;
				num3 = ptr[-12] - ptr[-4];
				num4 = ptr[-5] - ptr[-13];
				ptr[-4] = ptr[-4] + ptr[-12];
				ptr[-5] = ptr[-5] + ptr[-13];
				ptr[-12] = num4;
				ptr[-13] = num3;
				num3 = ptr[-14] - ptr[-6];
				num4 = ptr[-7] - ptr[-15];
				ptr[-6] = ptr[-6] + ptr[-14];
				ptr[-7] = ptr[-7] + ptr[-15];
				ptr[-14] = (num3 + num4) * num2;
				ptr[-15] = (num3 - num4) * num2;
				StbVorbis.iter_54(ptr);
				StbVorbis.iter_54(ptr - 8);
				ptr -= 16;
			}
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0001B9AC File Offset: 0x00019BAC
		public unsafe static void inverse_mdct(float* buffer, int n, StbVorbis.stb_vorbis f, int blocktype)
		{
			int num = n >> 1;
			int num2 = n >> 2;
			int num3 = n >> 3;
			int temp_offset = f.temp_offset;
			float* ptr = (float*)((f.alloc.alloc_buffer != null) ? StbVorbis.setup_temp_malloc(f, num * 4) : CRuntime.malloc((ulong)((long)(num * 4))));
			float* ptr2 = null;
			float* ptr3 = null;
			float* ptr4 = f.A[blocktype];
			float* ptr5 = ptr + (num - 2);
			float* ptr6 = ptr4;
			float* ptr7 = buffer;
			float* ptr8 = buffer + num;
			while (ptr7 != ptr8)
			{
				ptr5[1] = *ptr7 * *ptr6 - ptr7[2] * ptr6[1];
				*ptr5 = *ptr7 * ptr6[1] + ptr7[2] * *ptr6;
				ptr5 -= 2;
				ptr6 += 2;
				ptr7 += 4;
			}
			ptr7 = buffer + (num - 3);
			while (ptr5 >= ptr)
			{
				ptr5[1] = -ptr7[2] * *ptr6 - -(*ptr7) * ptr6[1];
				*ptr5 = -ptr7[2] * ptr6[1] + -(*ptr7) * *ptr6;
				ptr5 -= 2;
				ptr6 += 2;
				ptr7 -= 4;
			}
			ptr3 = ptr;
			float* ptr9 = ptr4 + (num - 8);
			float* ptr10 = ptr3 + num2;
			float* ptr11 = ptr3;
			float* ptr12 = buffer + num2;
			float* ptr13 = buffer;
			while (ptr9 >= ptr4)
			{
				float num4 = ptr10[1] - ptr11[1];
				float num5 = *ptr10 - *ptr11;
				ptr12[1] = ptr10[1] + ptr11[1];
				*ptr12 = *ptr10 + *ptr11;
				ptr13[1] = num4 * ptr9[4] - num5 * ptr9[5];
				*ptr13 = num5 * ptr9[4] + num4 * ptr9[5];
				num4 = ptr10[3] - ptr11[3];
				num5 = ptr10[2] - ptr11[2];
				ptr12[3] = ptr10[3] + ptr11[3];
				ptr12[2] = ptr10[2] + ptr11[2];
				ptr13[3] = num4 * *ptr9 - num5 * ptr9[1];
				ptr13[2] = num5 * *ptr9 + num4 * ptr9[1];
				ptr9 -= 8;
				ptr12 += 4;
				ptr13 += 4;
				ptr10 += 4;
				ptr11 += 4;
			}
			int num6 = StbVorbis.ilog(n) - 1;
			StbVorbis.imdct_step3_iter0_loop(n >> 4, buffer, num - 1, -(n >> 3), ptr4);
			StbVorbis.imdct_step3_iter0_loop(n >> 4, buffer, num - 1 - num2, -(n >> 3), ptr4);
			StbVorbis.imdct_step3_inner_r_loop(n >> 5, buffer, num - 1, -(n >> 4), ptr4, 16);
			StbVorbis.imdct_step3_inner_r_loop(n >> 5, buffer, num - 1 - num3, -(n >> 4), ptr4, 16);
			StbVorbis.imdct_step3_inner_r_loop(n >> 5, buffer, num - 1 - num3 * 2, -(n >> 4), ptr4, 16);
			StbVorbis.imdct_step3_inner_r_loop(n >> 5, buffer, num - 1 - num3 * 3, -(n >> 4), ptr4, 16);
			int i;
			for (i = 2; i < num6 - 3 >> 1; i++)
			{
				int num7 = n >> i + 2;
				int num8 = num7 >> 1;
				int num9 = 1 << i + 1;
				for (int j = 0; j < num9; j++)
				{
					StbVorbis.imdct_step3_inner_r_loop(n >> i + 4, buffer, num - 1 - num7 * j, -num8, ptr4, 1 << i + 3);
				}
			}
			while (i < num6 - 6)
			{
				int num10 = n >> i + 2;
				int num11 = 1 << i + 3;
				int num12 = num10 >> 1;
				int num13 = n >> i + 6;
				int n2 = 1 << i + 1;
				float* ptr14 = ptr4;
				int num14 = num - 1;
				for (int k = num13; k > 0; k--)
				{
					StbVorbis.imdct_step3_inner_s_loop(n2, buffer, num14, -num12, ptr14, num11, num10);
					ptr14 += num11 * 4;
					num14 -= 8;
				}
				i++;
			}
			StbVorbis.imdct_step3_inner_s_loop_ld654(n >> 5, buffer, num - 1, ptr4, n);
			ushort* ptr15 = f.bit_reverse[blocktype];
			float* ptr16 = ptr3 + (num2 - 4);
			float* ptr17 = ptr3 + (num - 4);
			while (ptr16 >= ptr3)
			{
				int num15 = (int)(*ptr15);
				ptr17[3] = buffer[num15];
				ptr17[2] = buffer[num15 + 1];
				ptr16[3] = buffer[num15 + 2];
				ptr16[2] = buffer[num15 + 3];
				num15 = (int)ptr15[1];
				ptr17[1] = buffer[num15];
				*ptr17 = buffer[num15 + 1];
				ptr16[1] = buffer[num15 + 2];
				*ptr16 = buffer[num15 + 3];
				ptr16 -= 4;
				ptr17 -= 4;
				ptr15 += 2;
			}
			float* ptr18 = f.C[blocktype];
			float* ptr19 = ptr3;
			float* ptr20 = ptr3 + num - 4;
			while (ptr19 < ptr20)
			{
				float num16 = *ptr19 - ptr20[2];
				float num17 = ptr19[1] + ptr20[3];
				float num18 = ptr18[1] * num16 + *ptr18 * num17;
				float num19 = ptr18[1] * num17 - *ptr18 * num16;
				float num20 = *ptr19 + ptr20[2];
				float num21 = ptr19[1] - ptr20[3];
				*ptr19 = num20 + num18;
				ptr19[1] = num21 + num19;
				ptr20[2] = num20 - num18;
				ptr20[3] = num19 - num21;
				num16 = ptr19[2] - *ptr20;
				num17 = ptr19[3] + ptr20[1];
				num18 = ptr18[3] * num16 + ptr18[2] * num17;
				num19 = ptr18[3] * num17 - ptr18[2] * num16;
				num20 = ptr19[2] + *ptr20;
				num21 = ptr19[3] - ptr20[1];
				ptr19[2] = num20 + num18;
				ptr19[3] = num21 + num19;
				*ptr20 = num20 - num18;
				ptr20[1] = num19 - num21;
				ptr18 += 4;
				ptr19 += 4;
				ptr20 -= 4;
			}
			float* ptr21 = f.B[blocktype] + num - 8;
			float* ptr22 = ptr + num - 8;
			float* ptr23 = buffer;
			float* ptr24 = buffer + (num - 4);
			float* ptr25 = buffer + num;
			float* ptr26 = buffer + (n - 4);
			while (ptr22 >= ptr3)
			{
				float num22 = ptr22[6] * ptr21[7] - ptr22[7] * ptr21[6];
				float num23 = -ptr22[6] * ptr21[6] - ptr22[7] * ptr21[7];
				*ptr23 = num22;
				ptr24[3] = -num22;
				*ptr25 = num23;
				ptr26[3] = num23;
				float num24 = ptr22[4] * ptr21[5] - ptr22[5] * ptr21[4];
				float num25 = -ptr22[4] * ptr21[4] - ptr22[5] * ptr21[5];
				ptr23[1] = num24;
				ptr24[2] = -num24;
				ptr25[1] = num25;
				ptr26[2] = num25;
				num22 = ptr22[2] * ptr21[3] - ptr22[3] * ptr21[2];
				num23 = -ptr22[2] * ptr21[2] - ptr22[3] * ptr21[3];
				ptr23[2] = num22;
				ptr24[1] = -num22;
				ptr25[2] = num23;
				ptr26[1] = num23;
				num24 = *ptr22 * ptr21[1] - ptr22[1] * *ptr21;
				num25 = -(*ptr22) * *ptr21 - ptr22[1] * ptr21[1];
				ptr23[3] = num24;
				*ptr24 = -num24;
				ptr25[3] = num25;
				*ptr26 = num25;
				ptr21 -= 8;
				ptr22 -= 8;
				ptr23 += 4;
				ptr25 += 4;
				ptr24 -= 4;
				ptr26 -= 4;
			}
			CRuntime.free((void*)ptr);
			f.temp_offset = temp_offset;
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0001C25E File Offset: 0x0001A45E
		public unsafe static float* get_window(StbVorbis.stb_vorbis f, int len)
		{
			len <<= 1;
			if (len == f.blocksize_0)
			{
				return f.window[0];
			}
			if (len == f.blocksize_1)
			{
				return f.window[1];
			}
			return null;
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0001C28C File Offset: 0x0001A48C
		public unsafe static int do_floor(StbVorbis.stb_vorbis f, StbVorbis.Mapping* map, int i, int n, float* target, short* finalY, byte* step2_flag)
		{
			int num = n >> 1;
			int mux = (int)map->chan[i].mux;
			int num2 = (int)(*((ref map->submap_floor.FixedElementField) + mux));
			if (f.floor_types[num2] == 0)
			{
				return StbVorbis.error(f, 21);
			}
			StbVorbis.Floor1* ptr = &f.floor_config[num2].floor1;
			int num3 = 0;
			int num4 = (int)(*finalY * (short)ptr->floor1_multiplier);
			for (int j = 1; j < ptr->values; j++)
			{
				int k = (int)(*((ref ptr->sorted_order.FixedElementField) + j));
				if (finalY[k] >= 0)
				{
					int num5 = (int)(finalY[k] * (short)ptr->floor1_multiplier);
					int num6 = (int)(*((ref ptr->Xlist.FixedElementField) + (IntPtr)k * 2));
					if (num3 != num6)
					{
						StbVorbis.draw_line(target, num3, num4, num6, num5, num);
					}
					num3 = num6;
					num4 = num5;
				}
			}
			if (num3 < num)
			{
				for (int k = num3; k < num; k++)
				{
					target[k] *= StbVorbis.inverse_db_table[num4];
				}
			}
			return 1;
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0001C3A0 File Offset: 0x0001A5A0
		public unsafe static int vorbis_decode_initial(StbVorbis.stb_vorbis f, int* p_left_start, int* p_left_end, int* p_right_start, int* p_right_end, int* mode)
		{
			f.channel_buffer_start = (f.channel_buffer_end = 0);
			while (f.eof == 0)
			{
				if (StbVorbis.maybe_start_packet(f) == 0)
				{
					return 0;
				}
				if (StbVorbis.get_bits(f, 1) != 0U)
				{
					if (f.push_mode != 0)
					{
						return StbVorbis.error(f, 35);
					}
					while (-1 != StbVorbis.get8_packet(f))
					{
					}
				}
				else
				{
					int num = (int)StbVorbis.get_bits(f, StbVorbis.ilog(f.mode_count - 1));
					if (num == -1)
					{
						return 0;
					}
					if (num >= f.mode_count)
					{
						return 0;
					}
					*mode = num;
					StbVorbis.Mode* ptr = f.mode_config + num;
					int num2;
					int num3;
					int num4;
					if (ptr->blockflag != 0)
					{
						num2 = f.blocksize_1;
						num3 = (int)StbVorbis.get_bits(f, 1);
						num4 = (int)StbVorbis.get_bits(f, 1);
					}
					else
					{
						num4 = (num3 = 0);
						num2 = f.blocksize_0;
					}
					int num5 = num2 >> 1;
					if (ptr->blockflag != 0 && num3 == 0)
					{
						*p_left_start = num2 - f.blocksize_0 >> 2;
						*p_left_end = num2 + f.blocksize_0 >> 2;
					}
					else
					{
						*p_left_start = 0;
						*p_left_end = num5;
					}
					if (ptr->blockflag != 0 && num4 == 0)
					{
						*p_right_start = num2 * 3 - f.blocksize_0 >> 2;
						*p_right_end = num2 * 3 + f.blocksize_0 >> 2;
					}
					else
					{
						*p_right_start = num5;
						*p_right_end = num2;
					}
					return 1;
				}
			}
			return 0;
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0001C4C0 File Offset: 0x0001A6C0
		public unsafe static int vorbis_decode_packet_rest(StbVorbis.stb_vorbis f, int* len, StbVorbis.Mode* m, int left_start, int left_end, int right_start, int right_end, int* p_left)
		{
			int* ptr = stackalloc int[(UIntPtr)1024];
			int* ptr2 = stackalloc int[(UIntPtr)1024];
			int num = f.blocksize[(int)m->blockflag];
			StbVorbis.Mapping* ptr3 = f.mapping + m->mapping;
			int num2 = num >> 1;
			for (int i = 0; i < f.channels; i++)
			{
				int mux = (int)ptr3->chan[i].mux;
				ptr[i] = 0;
				int num3 = (int)(*((ref ptr3->submap_floor.FixedElementField) + mux));
				if (f.floor_types[num3] == 0)
				{
					return StbVorbis.error(f, 21);
				}
				StbVorbis.Floor1* ptr4 = &f.floor_config[num3].floor1;
				if (StbVorbis.get_bits(f, 1) != 0U)
				{
					byte* ptr5 = stackalloc byte[(UIntPtr)256];
					IntPtr intPtr = stackalloc byte[(UIntPtr)16];
					*intPtr = 256;
					*(intPtr + 4) = 128;
					*(intPtr + (IntPtr)2 * 4) = 86;
					*(intPtr + (IntPtr)3 * 4) = 64;
					int num4 = *(intPtr + (IntPtr)(ptr4->floor1_multiplier - 1) * 4);
					int num5 = 2;
					short* ptr6 = f.finalY[i];
					*ptr6 = (short)StbVorbis.get_bits(f, StbVorbis.ilog(num4) - 1);
					ptr6[1] = (short)StbVorbis.get_bits(f, StbVorbis.ilog(num4) - 1);
					for (int j = 0; j < (int)ptr4->partitions; j++)
					{
						int num6 = (int)(*((ref ptr4->partition_class_list.FixedElementField) + j));
						int num7 = (int)(*((ref ptr4->class_dimensions.FixedElementField) + num6));
						int num8 = (int)(*((ref ptr4->class_subclasses.FixedElementField) + num6));
						int num9 = (1 << num8) - 1;
						int num10 = 0;
						if (num8 != 0)
						{
							StbVorbis.Codebook* ptr7 = f.codebooks + *((ref ptr4->class_masterbooks.FixedElementField) + num6);
							num10 = StbVorbis.codebook_decode_scalar(f, ptr7);
							if (ptr7->sparse != 0)
							{
								num10 = ptr7->sorted_values[num10];
							}
						}
						for (int k = 0; k < num7; k++)
						{
							int num11 = (int)(*((ref ptr4->subclass_books.FixedElementField) + (IntPtr)(num6 * 8 + (num10 & num9)) * 2));
							num10 >>= num8;
							if (num11 >= 0)
							{
								StbVorbis.Codebook* ptr8 = f.codebooks + num11;
								int num12 = StbVorbis.codebook_decode_scalar(f, ptr8);
								if (ptr8->sparse != 0)
								{
									num12 = ptr8->sorted_values[num12];
								}
								ptr6[(IntPtr)(num5++) * 2] = (short)num12;
							}
							else
							{
								ptr6[(IntPtr)(num5++) * 2] = 0;
							}
						}
					}
					if (f.valid_bits == -1)
					{
						ptr[i] = 1;
					}
					else
					{
						*ptr5 = (ptr5[1] = 1);
						for (int j = 2; j < ptr4->values; j++)
						{
							int num13 = (int)(*((ref ptr4->neighbors.FixedElementField) + j * 2));
							int num14 = (int)(*((ref ptr4->neighbors.FixedElementField) + (j * 2 + 1)));
							int num15 = StbVorbis.predict_point((int)(*((ref ptr4->Xlist.FixedElementField) + (IntPtr)j * 2)), (int)(*((ref ptr4->Xlist.FixedElementField) + (IntPtr)num13 * 2)), (int)(*((ref ptr4->Xlist.FixedElementField) + (IntPtr)num14 * 2)), (int)ptr6[num13], (int)ptr6[num14]);
							int num16 = (int)ptr6[j];
							int num17 = num4 - num15;
							int num18 = num15;
							int num19;
							if (num17 < num18)
							{
								num19 = num17 * 2;
							}
							else
							{
								num19 = num18 * 2;
							}
							if (num16 != 0)
							{
								ptr5[num13] = (ptr5[num14] = 1);
								ptr5[j] = 1;
								if (num16 >= num19)
								{
									if (num17 > num18)
									{
										ptr6[j] = (short)(num16 - num18 + num15);
									}
									else
									{
										ptr6[j] = (short)(num15 - num16 + num17 - 1);
									}
								}
								else if ((num16 & 1) != 0)
								{
									ptr6[j] = (short)(num15 - (num16 + 1 >> 1));
								}
								else
								{
									ptr6[j] = (short)(num15 + (num16 >> 1));
								}
							}
							else
							{
								ptr5[j] = 0;
								ptr6[j] = (short)num15;
							}
						}
						for (int j = 0; j < ptr4->values; j++)
						{
							if (ptr5[j] == 0)
							{
								ptr6[j] = -1;
							}
						}
					}
				}
				else
				{
					ptr[i] = 1;
				}
			}
			CRuntime.memcpy((void*)ptr2, (void*)ptr, (ulong)((long)(4 * f.channels)));
			for (int i = 0; i < (int)ptr3->coupling_steps; i++)
			{
				if (ptr[ptr3->chan[i].magnitude] == 0 || ptr[ptr3->chan[i].angle] == 0)
				{
					ptr[ptr3->chan[i].magnitude] = (ptr[ptr3->chan[i].angle] = 0);
				}
			}
			for (int i = 0; i < (int)ptr3->submaps; i++)
			{
				float** ptr9 = stackalloc float*[checked(unchecked((UIntPtr)16) * (UIntPtr)sizeof(float*))];
				byte* ptr10 = stackalloc byte[(UIntPtr)256];
				int num20 = 0;
				for (int j = 0; j < f.channels; j++)
				{
					if ((int)ptr3->chan[j].mux == i)
					{
						if (ptr[j] != 0)
						{
							ptr10[num20] = 1;
							*(IntPtr*)(ptr9 + (IntPtr)num20 * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) = (IntPtr)((UIntPtr)0);
						}
						else
						{
							ptr10[num20] = 0;
							*(IntPtr*)(ptr9 + (IntPtr)num20 * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) = f.channel_buffers[j];
						}
						num20++;
					}
				}
				int rn = (int)(*((ref ptr3->submap_residue.FixedElementField) + i));
				StbVorbis.decode_residue(f, ptr9, num20, num2, rn, ptr10);
			}
			for (int i = (int)(ptr3->coupling_steps - 1); i >= 0; i--)
			{
				int num21 = num >> 1;
				float* ptr11 = f.channel_buffers[(int)ptr3->chan[i].magnitude];
				float* ptr12 = f.channel_buffers[(int)ptr3->chan[i].angle];
				for (int j = 0; j < num21; j++)
				{
					float num22;
					float num23;
					if (ptr11[j] > 0f)
					{
						if (ptr12[j] > 0f)
						{
							num22 = ptr11[j];
							num23 = ptr11[j] - ptr12[j];
						}
						else
						{
							num23 = ptr11[j];
							num22 = ptr11[j] + ptr12[j];
						}
					}
					else if (ptr12[j] > 0f)
					{
						num22 = ptr11[j];
						num23 = ptr11[j] + ptr12[j];
					}
					else
					{
						num23 = ptr11[j];
						num22 = ptr11[j] - ptr12[j];
					}
					ptr11[j] = num22;
					ptr12[j] = num23;
				}
			}
			for (int i = 0; i < f.channels; i++)
			{
				if (ptr2[i] != 0)
				{
					CRuntime.memset((void*)f.channel_buffers[i], 0, (ulong)((long)(4 * num2)));
				}
				else
				{
					StbVorbis.do_floor(f, ptr3, i, num, f.channel_buffers[i], f.finalY[i], null);
				}
			}
			for (int i = 0; i < f.channels; i++)
			{
				StbVorbis.inverse_mdct(f.channel_buffers[i], num, f, (int)m->blockflag);
			}
			StbVorbis.flush_packet(f);
			if (f.first_decode != 0)
			{
				f.current_loc = (uint)(-(uint)num2);
				f.discard_samples_deferred = num - right_end;
				f.current_loc_valid = 1;
				f.first_decode = 0;
			}
			else if (f.discard_samples_deferred != 0)
			{
				if (f.discard_samples_deferred >= right_start - left_start)
				{
					f.discard_samples_deferred -= right_start - left_start;
					left_start = right_start;
					*p_left = left_start;
				}
				else
				{
					left_start += f.discard_samples_deferred;
					*p_left = left_start;
					f.discard_samples_deferred = 0;
				}
			}
			else if (f.previous_length == 0)
			{
				int current_loc_valid = f.current_loc_valid;
			}
			if (f.last_seg_which == f.end_seg_with_known_loc)
			{
				if (f.current_loc_valid != 0 && (f.page_flag & 4) != 0)
				{
					uint num24 = (uint)((ulong)f.known_loc_for_packet - (ulong)((long)(num - right_end)));
					if ((ulong)num24 < (ulong)f.current_loc + (ulong)((long)(right_end - left_start)))
					{
						if (num24 < f.current_loc)
						{
							*len = 0;
						}
						else
						{
							*len = (int)(num24 - f.current_loc);
						}
						*len += left_start;
						if (*len > right_end)
						{
							*len = right_end;
						}
						f.current_loc += (uint)(*len);
						return 1;
					}
				}
				f.current_loc = (uint)((ulong)f.known_loc_for_packet - (ulong)((long)(num2 - left_start)));
				f.current_loc_valid = 1;
			}
			if (f.current_loc_valid != 0)
			{
				f.current_loc += (uint)(right_start - left_start);
			}
			sbyte* alloc_buffer = f.alloc.alloc_buffer;
			UIntPtr uintPtr = (UIntPtr)0;
			*len = right_end;
			return 1;
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x0001CD30 File Offset: 0x0001AF30
		public unsafe static int vorbis_decode_packet(StbVorbis.stb_vorbis f, int* len, int* p_left, int* p_right)
		{
			int left_end;
			int right_end;
			int num;
			if (StbVorbis.vorbis_decode_initial(f, p_left, &left_end, p_right, &right_end, &num) == 0)
			{
				return 0;
			}
			return StbVorbis.vorbis_decode_packet_rest(f, len, f.mode_config + num, *p_left, left_end, *p_right, right_end, p_left);
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0001CD70 File Offset: 0x0001AF70
		public unsafe static int vorbis_finish_frame(StbVorbis.stb_vorbis f, int len, int left, int right)
		{
			if (f.previous_length != 0)
			{
				int previous_length = f.previous_length;
				float* ptr = StbVorbis.get_window(f, previous_length);
				for (int i = 0; i < f.channels; i++)
				{
					for (int j = 0; j < previous_length; j++)
					{
						f.channel_buffers[i][left + j] = f.channel_buffers[i][left + j] * ptr[j] + f.previous_window[i][j] * ptr[previous_length - 1 - j];
					}
				}
			}
			int previous_length2 = f.previous_length;
			f.previous_length = len - right;
			for (int k = 0; k < f.channels; k++)
			{
				int num = 0;
				while (right + num < len)
				{
					f.previous_window[k][num] = f.channel_buffers[k][right + num];
					num++;
				}
			}
			if (previous_length2 == 0)
			{
				return 0;
			}
			if (len < right)
			{
				right = len;
			}
			f.samples_output += (uint)(right - left);
			return right - left;
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x0001CE78 File Offset: 0x0001B078
		public unsafe static int vorbis_pump_first_frame(StbVorbis.stb_vorbis f)
		{
			int len;
			int left;
			int right;
			int num = StbVorbis.vorbis_decode_packet(f, &len, &left, &right);
			if (num != 0)
			{
				StbVorbis.vorbis_finish_frame(f, len, left, right);
			}
			return num;
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x0001CEA4 File Offset: 0x0001B0A4
		public unsafe static int is_whole_packet_present(StbVorbis.stb_vorbis f, int end_page)
		{
			int i = f.next_seg;
			int num = 1;
			byte* ptr = f.stream;
			if (i != -1)
			{
				while (i < f.segment_count)
				{
					ptr += f.segments[i];
					if (f.segments[i] < 255)
					{
						break;
					}
					i++;
				}
				if (end_page != 0 && i < f.segment_count - 1)
				{
					return StbVorbis.error(f, 21);
				}
				if (i == f.segment_count)
				{
					i = -1;
				}
				if (ptr != f.stream_end)
				{
					return StbVorbis.error(f, 1);
				}
				num = 0;
			}
			while (i == -1)
			{
				if (ptr + 26 >= f.stream_end)
				{
					return StbVorbis.error(f, 1);
				}
				if (CRuntime.memcmp(ptr, StbVorbis.ogg_page_header, 4UL) != 0)
				{
					return StbVorbis.error(f, 21);
				}
				if (ptr[4] != 0)
				{
					return StbVorbis.error(f, 21);
				}
				if (num != 0)
				{
					if (f.previous_length != 0 && (ptr[5] & 1) != 0)
					{
						return StbVorbis.error(f, 21);
					}
				}
				else if ((ptr[5] & 1) == 0)
				{
					return StbVorbis.error(f, 21);
				}
				int num2 = (int)ptr[26];
				byte* ptr2 = ptr + 27;
				ptr = ptr2 + num2;
				if (ptr != f.stream_end)
				{
					return StbVorbis.error(f, 1);
				}
				for (i = 0; i < num2; i++)
				{
					ptr += ptr2[i];
					if (ptr2[i] < 255)
					{
						break;
					}
				}
				if (end_page != 0 && i < num2 - 1)
				{
					return StbVorbis.error(f, 21);
				}
				if (i == num2)
				{
					i = -1;
				}
				if (ptr != f.stream_end)
				{
					return StbVorbis.error(f, 1);
				}
				num = 0;
			}
			return 1;
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0001D00C File Offset: 0x0001B20C
		public unsafe static int start_decoder(StbVorbis.stb_vorbis f)
		{
			byte* ptr = stackalloc byte[(UIntPtr)6];
			int num = 0;
			int num2 = 0;
			if (StbVorbis.start_page(f) == 0)
			{
				return 0;
			}
			if ((f.page_flag & 2) == 0)
			{
				return StbVorbis.error(f, 34);
			}
			if ((f.page_flag & 4) != 0)
			{
				return StbVorbis.error(f, 34);
			}
			if ((f.page_flag & 1) != 0)
			{
				return StbVorbis.error(f, 34);
			}
			if (f.segment_count != 1)
			{
				return StbVorbis.error(f, 34);
			}
			if (*f.segments != 30)
			{
				return StbVorbis.error(f, 34);
			}
			if (StbVorbis.get8(f) != 1)
			{
				return StbVorbis.error(f, 34);
			}
			if (StbVorbis.getn(f, ptr, 6) == 0)
			{
				return StbVorbis.error(f, 10);
			}
			if (StbVorbis.vorbis_validate(ptr) == 0)
			{
				return StbVorbis.error(f, 34);
			}
			if (StbVorbis.get32(f) != 0U)
			{
				return StbVorbis.error(f, 34);
			}
			f.channels = (int)StbVorbis.get8(f);
			if (f.channels == 0)
			{
				return StbVorbis.error(f, 34);
			}
			if (f.channels > 16)
			{
				return StbVorbis.error(f, 5);
			}
			f.sample_rate = StbVorbis.get32(f);
			if (f.sample_rate == 0U)
			{
				return StbVorbis.error(f, 34);
			}
			StbVorbis.get32(f);
			StbVorbis.get32(f);
			StbVorbis.get32(f);
			byte b = StbVorbis.get8(f);
			int num3 = (int)(b & 15);
			int num4 = b >> 4;
			f.blocksize_0 = 1 << num3;
			f.blocksize_1 = 1 << num4;
			if (num3 < 6 || num3 > 13)
			{
				return StbVorbis.error(f, 20);
			}
			if (num4 < 6 || num4 > 13)
			{
				return StbVorbis.error(f, 20);
			}
			if (num3 > num4)
			{
				return StbVorbis.error(f, 20);
			}
			b = StbVorbis.get8(f);
			if ((b & 1) == 0)
			{
				return StbVorbis.error(f, 34);
			}
			if (StbVorbis.start_page(f) == 0)
			{
				return 0;
			}
			if (StbVorbis.start_packet(f) == 0)
			{
				return 0;
			}
			int num5;
			do
			{
				num5 = StbVorbis.next_segment(f);
				StbVorbis.skip(f, num5);
				f.bytes_in_seg = 0;
			}
			while (num5 != 0);
			if (StbVorbis.start_packet(f) == 0)
			{
				return 0;
			}
			if (f.push_mode != 0 && StbVorbis.is_whole_packet_present(f, 1) == 0)
			{
				if (f.error == 21)
				{
					f.error = 20;
				}
				return 0;
			}
			StbVorbis.crc32_init();
			if (StbVorbis.get8_packet(f) != 5)
			{
				return StbVorbis.error(f, 20);
			}
			for (int i = 0; i < 6; i++)
			{
				ptr[i] = (byte)StbVorbis.get8_packet(f);
			}
			if (StbVorbis.vorbis_validate(ptr) == 0)
			{
				return StbVorbis.error(f, 20);
			}
			f.codebook_count = (int)(StbVorbis.get_bits(f, 8) + 1U);
			f.codebooks = (StbVorbis.Codebook*)StbVorbis.setup_malloc(f, sizeof(StbVorbis.Codebook) * f.codebook_count);
			if (f.codebooks == null)
			{
				return StbVorbis.error(f, 3);
			}
			CRuntime.memset((void*)f.codebooks, 0, (ulong)((long)(sizeof(StbVorbis.Codebook) * f.codebook_count)));
			for (int i = 0; i < f.codebook_count; i++)
			{
				int num6 = 0;
				StbVorbis.Codebook* ptr2 = f.codebooks + i;
				b = (byte)StbVorbis.get_bits(f, 8);
				if (b != 66)
				{
					return StbVorbis.error(f, 20);
				}
				b = (byte)StbVorbis.get_bits(f, 8);
				if (b != 67)
				{
					return StbVorbis.error(f, 20);
				}
				b = (byte)StbVorbis.get_bits(f, 8);
				if (b != 86)
				{
					return StbVorbis.error(f, 20);
				}
				b = (byte)StbVorbis.get_bits(f, 8);
				ptr2->dimensions = (int)((StbVorbis.get_bits(f, 8) << 8) + (uint)b);
				b = (byte)StbVorbis.get_bits(f, 8);
				byte b2 = (byte)StbVorbis.get_bits(f, 8);
				ptr2->entries = (int)((ulong)((ulong)StbVorbis.get_bits(f, 8) << 16) + (ulong)((long)((long)b2 << 8)) + (ulong)b);
				int num7 = (int)StbVorbis.get_bits(f, 1);
				ptr2->sparse = (byte)((num7 != 0) ? 0U : StbVorbis.get_bits(f, 1));
				if (ptr2->dimensions == 0 && ptr2->entries != 0)
				{
					return StbVorbis.error(f, 20);
				}
				byte* ptr3;
				if (ptr2->sparse != 0)
				{
					ptr3 = (byte*)StbVorbis.setup_temp_malloc(f, ptr2->entries);
				}
				else
				{
					ptr3 = (ptr2->codeword_lengths = (byte*)StbVorbis.setup_malloc(f, ptr2->entries));
				}
				if (ptr3 == null)
				{
					return StbVorbis.error(f, 3);
				}
				if (num7 != 0)
				{
					int j = 0;
					int num8 = (int)(StbVorbis.get_bits(f, 5) + 1U);
					while (j < ptr2->entries)
					{
						int n = ptr2->entries - j;
						int num9 = (int)StbVorbis.get_bits(f, StbVorbis.ilog(n));
						if (j + num9 > ptr2->entries)
						{
							return StbVorbis.error(f, 20);
						}
						CRuntime.memset((void*)(ptr3 + j), num8, (ulong)((long)num9));
						j += num9;
						num8++;
					}
				}
				else
				{
					for (int k = 0; k < ptr2->entries; k++)
					{
						if (ptr2->sparse == 0 || StbVorbis.get_bits(f, 1) != 0U)
						{
							ptr3[k] = (byte)(StbVorbis.get_bits(f, 5) + 1U);
							num6++;
							if (ptr3[k] == 32)
							{
								return StbVorbis.error(f, 20);
							}
						}
						else
						{
							ptr3[k] = byte.MaxValue;
						}
					}
				}
				if (ptr2->sparse != 0 && num6 >= ptr2->entries >> 2)
				{
					if (ptr2->entries > (int)f.setup_temp_memory_required)
					{
						f.setup_temp_memory_required = (uint)ptr2->entries;
					}
					ptr2->codeword_lengths = (byte*)StbVorbis.setup_malloc(f, ptr2->entries);
					if (ptr2->codeword_lengths == null)
					{
						return StbVorbis.error(f, 3);
					}
					CRuntime.memcpy((void*)ptr2->codeword_lengths, (void*)ptr3, (ulong)((long)ptr2->entries));
					StbVorbis.setup_temp_free(f, (void*)ptr3, ptr2->entries);
					ptr3 = ptr2->codeword_lengths;
					ptr2->sparse = 0;
				}
				int num10;
				if (ptr2->sparse != 0)
				{
					num10 = num6;
				}
				else
				{
					num10 = 0;
					for (int k = 0; k < ptr2->entries; k++)
					{
						if (ptr3[k] > 10 && ptr3[k] != 255)
						{
							num10++;
						}
					}
				}
				ptr2->sorted_entries = num10;
				uint* ptr4 = null;
				if (ptr2->sparse == 0)
				{
					ptr2->codewords = (uint*)StbVorbis.setup_malloc(f, 4 * ptr2->entries);
					if (ptr2->codewords == null)
					{
						return StbVorbis.error(f, 3);
					}
				}
				else
				{
					if (ptr2->sorted_entries != 0)
					{
						ptr2->codeword_lengths = (byte*)StbVorbis.setup_malloc(f, ptr2->sorted_entries);
						if (ptr2->codeword_lengths == null)
						{
							return StbVorbis.error(f, 3);
						}
						ptr2->codewords = (uint*)StbVorbis.setup_temp_malloc(f, 4 * ptr2->sorted_entries);
						if (ptr2->codewords == null)
						{
							return StbVorbis.error(f, 3);
						}
						ptr4 = (uint*)StbVorbis.setup_temp_malloc(f, 4 * ptr2->sorted_entries);
						if (ptr4 == null)
						{
							return StbVorbis.error(f, 3);
						}
					}
					uint num11 = (uint)(ptr2->entries + 8 * ptr2->sorted_entries);
					if (num11 > f.setup_temp_memory_required)
					{
						f.setup_temp_memory_required = num11;
					}
				}
				if (StbVorbis.compute_codewords(ptr2, ptr3, ptr2->entries, ptr4) == 0)
				{
					if (ptr2->sparse != 0)
					{
						StbVorbis.setup_temp_free(f, (void*)ptr4, 0);
					}
					return StbVorbis.error(f, 20);
				}
				if (ptr2->sorted_entries != 0)
				{
					ptr2->sorted_codewords = (uint*)StbVorbis.setup_malloc(f, 4 * (ptr2->sorted_entries + 1));
					if (ptr2->sorted_codewords == null)
					{
						return StbVorbis.error(f, 3);
					}
					ptr2->sorted_values = (int*)StbVorbis.setup_malloc(f, 4 * (ptr2->sorted_entries + 1));
					if (ptr2->sorted_values == null)
					{
						return StbVorbis.error(f, 3);
					}
					ptr2->sorted_values += 4;
					ptr2->sorted_values[-1] = -1;
					StbVorbis.compute_sorted_huffman(ptr2, ptr3, ptr4);
				}
				if (ptr2->sparse != 0)
				{
					StbVorbis.setup_temp_free(f, (void*)ptr4, 4 * ptr2->sorted_entries);
					StbVorbis.setup_temp_free(f, (void*)ptr2->codewords, 4 * ptr2->sorted_entries);
					StbVorbis.setup_temp_free(f, (void*)ptr3, ptr2->entries);
					ptr2->codewords = null;
				}
				StbVorbis.compute_accelerated_huffman(ptr2);
				ptr2->lookup_type = (byte)StbVorbis.get_bits(f, 4);
				if (ptr2->lookup_type > 2)
				{
					return StbVorbis.error(f, 20);
				}
				if (ptr2->lookup_type > 0)
				{
					ptr2->minimum_value = StbVorbis.float32_unpack(StbVorbis.get_bits(f, 32));
					ptr2->delta_value = StbVorbis.float32_unpack(StbVorbis.get_bits(f, 32));
					ptr2->value_bits = (byte)(StbVorbis.get_bits(f, 4) + 1U);
					ptr2->sequence_p = (byte)StbVorbis.get_bits(f, 1);
					if (ptr2->lookup_type == 1)
					{
						ptr2->lookup_values = (uint)StbVorbis.lookup1_values(ptr2->entries, ptr2->dimensions);
					}
					else
					{
						ptr2->lookup_values = (uint)(ptr2->entries * ptr2->dimensions);
					}
					if (ptr2->lookup_values == 0U)
					{
						return StbVorbis.error(f, 20);
					}
					ushort* ptr5 = (ushort*)StbVorbis.setup_temp_malloc(f, (int)(2U * ptr2->lookup_values));
					if (ptr5 == null)
					{
						return StbVorbis.error(f, 3);
					}
					for (int k = 0; k < (int)ptr2->lookup_values; k++)
					{
						int num12 = (int)StbVorbis.get_bits(f, (int)ptr2->value_bits);
						if (num12 == -1)
						{
							StbVorbis.setup_temp_free(f, (void*)ptr5, (int)(2U * ptr2->lookup_values));
							return StbVorbis.error(f, 20);
						}
						ptr5[k] = (ushort)num12;
					}
					if (ptr2->lookup_type == 1)
					{
						int sparse = (int)ptr2->sparse;
						float num13 = 0f;
						if (sparse != 0)
						{
							if (ptr2->sorted_entries == 0)
							{
								goto IL_AA6;
							}
							ptr2->multiplicands = (float*)StbVorbis.setup_malloc(f, 4 * ptr2->sorted_entries * ptr2->dimensions);
						}
						else
						{
							ptr2->multiplicands = (float*)StbVorbis.setup_malloc(f, 4 * ptr2->entries * ptr2->dimensions);
						}
						if (ptr2->multiplicands == null)
						{
							StbVorbis.setup_temp_free(f, (void*)ptr5, (int)(2U * ptr2->lookup_values));
							return StbVorbis.error(f, 3);
						}
						int num14 = ((sparse != 0) ? ptr2->sorted_entries : ptr2->entries);
						for (int k = 0; k < num14; k++)
						{
							uint num15 = (uint)((sparse != 0) ? ptr2->sorted_values[k] : k);
							uint num16 = 1U;
							for (int l = 0; l < ptr2->dimensions; l++)
							{
								int num17 = (int)(num15 / num16 % ptr2->lookup_values);
								float num18 = (float)ptr5[num17];
								num18 = (float)ptr5[num17] * ptr2->delta_value + ptr2->minimum_value + num13;
								ptr2->multiplicands[k * ptr2->dimensions + l] = num18;
								if (ptr2->sequence_p != 0)
								{
									num13 = num18;
								}
								if (l + 1 < ptr2->dimensions)
								{
									if (num16 > 4294967295U / ptr2->lookup_values)
									{
										StbVorbis.setup_temp_free(f, (void*)ptr5, (int)(2U * ptr2->lookup_values));
										return StbVorbis.error(f, 20);
									}
									num16 *= ptr2->lookup_values;
								}
							}
						}
						ptr2->lookup_type = 2;
					}
					else
					{
						float num19 = 0f;
						ptr2->multiplicands = (float*)StbVorbis.setup_malloc(f, (int)(4U * ptr2->lookup_values));
						if (ptr2->multiplicands == null)
						{
							StbVorbis.setup_temp_free(f, (void*)ptr5, (int)(2U * ptr2->lookup_values));
							return StbVorbis.error(f, 3);
						}
						for (int k = 0; k < (int)ptr2->lookup_values; k++)
						{
							float num20 = (float)ptr5[k] * ptr2->delta_value + ptr2->minimum_value + num19;
							ptr2->multiplicands[k] = num20;
							if (ptr2->sequence_p != 0)
							{
								num19 = num20;
							}
						}
					}
					IL_AA6:
					StbVorbis.setup_temp_free(f, (void*)ptr5, (int)(2U * ptr2->lookup_values));
				}
			}
			b = (byte)(StbVorbis.get_bits(f, 6) + 1U);
			for (int i = 0; i < (int)b; i++)
			{
				if (StbVorbis.get_bits(f, 16) != 0U)
				{
					return StbVorbis.error(f, 20);
				}
			}
			f.floor_count = (int)(StbVorbis.get_bits(f, 6) + 1U);
			f.floor_config = (StbVorbis.Floor*)StbVorbis.setup_malloc(f, f.floor_count * sizeof(StbVorbis.Floor));
			if (f.floor_config == null)
			{
				return StbVorbis.error(f, 3);
			}
			for (int i = 0; i < f.floor_count; i++)
			{
				f.floor_types[i] = (ushort)StbVorbis.get_bits(f, 16);
				if (f.floor_types[i] > 1)
				{
					return StbVorbis.error(f, 20);
				}
				if (f.floor_types[i] == 0)
				{
					StbVorbis.Floor0* ptr6 = &f.floor_config[i].floor0;
					ptr6->order = (byte)StbVorbis.get_bits(f, 8);
					ptr6->rate = (ushort)StbVorbis.get_bits(f, 16);
					ptr6->bark_map_size = (ushort)StbVorbis.get_bits(f, 16);
					ptr6->amplitude_bits = (byte)StbVorbis.get_bits(f, 6);
					ptr6->amplitude_offset = (byte)StbVorbis.get_bits(f, 8);
					ptr6->number_of_books = (byte)(StbVorbis.get_bits(f, 4) + 1U);
					for (int k = 0; k < (int)ptr6->number_of_books; k++)
					{
						*((ref ptr6->book_list.FixedElementField) + k) = (byte)StbVorbis.get_bits(f, 8);
					}
					return StbVorbis.error(f, 4);
				}
				StbVorbis.stbv__floor_ordering* ptr7 = stackalloc StbVorbis.stbv__floor_ordering[checked(unchecked((UIntPtr)250) * (UIntPtr)sizeof(StbVorbis.stbv__floor_ordering))];
				StbVorbis.Floor1* ptr8 = &f.floor_config[i].floor1;
				int num21 = -1;
				ptr8->partitions = (byte)StbVorbis.get_bits(f, 5);
				for (int k = 0; k < (int)ptr8->partitions; k++)
				{
					*((ref ptr8->partition_class_list.FixedElementField) + k) = (byte)StbVorbis.get_bits(f, 4);
					if ((int)(*((ref ptr8->partition_class_list.FixedElementField) + k)) > num21)
					{
						num21 = (int)(*((ref ptr8->partition_class_list.FixedElementField) + k));
					}
				}
				for (int k = 0; k <= num21; k++)
				{
					*((ref ptr8->class_dimensions.FixedElementField) + k) = (byte)(StbVorbis.get_bits(f, 3) + 1U);
					*((ref ptr8->class_subclasses.FixedElementField) + k) = (byte)StbVorbis.get_bits(f, 2);
					if (*((ref ptr8->class_subclasses.FixedElementField) + k) != 0)
					{
						*((ref ptr8->class_masterbooks.FixedElementField) + k) = (byte)StbVorbis.get_bits(f, 8);
						if ((int)(*((ref ptr8->class_masterbooks.FixedElementField) + k)) >= f.codebook_count)
						{
							return StbVorbis.error(f, 20);
						}
					}
					for (int l = 0; l < 1 << (int)(*((ref ptr8->class_subclasses.FixedElementField) + k)); l++)
					{
						*((ref ptr8->subclass_books.FixedElementField) + (IntPtr)(k * 8 + l) * 2) = (short)(StbVorbis.get_bits(f, 8) - 1U);
						if ((int)(*((ref ptr8->subclass_books.FixedElementField) + (IntPtr)(k * 8 + l) * 2)) >= f.codebook_count)
						{
							return StbVorbis.error(f, 20);
						}
					}
				}
				ptr8->floor1_multiplier = (byte)(StbVorbis.get_bits(f, 2) + 1U);
				ptr8->rangebits = (byte)StbVorbis.get_bits(f, 4);
				ptr8->Xlist.FixedElementField = 0;
				*((ref ptr8->Xlist.FixedElementField) + 2) = (ushort)(1 << (int)ptr8->rangebits);
				ptr8->values = 2;
				for (int k = 0; k < (int)ptr8->partitions; k++)
				{
					int num22 = (int)(*((ref ptr8->partition_class_list.FixedElementField) + k));
					for (int l = 0; l < (int)(*((ref ptr8->class_dimensions.FixedElementField) + num22)); l++)
					{
						*((ref ptr8->Xlist.FixedElementField) + (IntPtr)ptr8->values * 2) = (ushort)StbVorbis.get_bits(f, (int)ptr8->rangebits);
						ptr8->values++;
					}
				}
				for (int k = 0; k < ptr8->values; k++)
				{
					ptr7[k].x = *((ref ptr8->Xlist.FixedElementField) + (IntPtr)k * 2);
					ptr7[k].id = (ushort)k;
				}
				CRuntime.qsort((void*)ptr7, (ulong)((long)ptr8->values), (ulong)((long)sizeof(StbVorbis.stbv__floor_ordering)), new CRuntime.QSortComparer(StbVorbis.point_compare));
				for (int k = 0; k < ptr8->values; k++)
				{
					*((ref ptr8->sorted_order.FixedElementField) + k) = (byte)ptr7[k].id;
				}
				for (int k = 2; k < ptr8->values; k++)
				{
					int num23;
					int num24;
					StbVorbis.neighbors(&ptr8->Xlist.FixedElementField, k, &num23, &num24);
					*((ref ptr8->neighbors.FixedElementField) + k * 2) = (byte)num23;
					*((ref ptr8->neighbors.FixedElementField) + (k * 2 + 1)) = (byte)num24;
				}
				if (ptr8->values > num2)
				{
					num2 = ptr8->values;
				}
			}
			f.residue_count = (int)(StbVorbis.get_bits(f, 6) + 1U);
			f.residue_config = new StbVorbis.Residue[f.residue_count];
			for (int i = 0; i < f.residue_config.Length; i++)
			{
				f.residue_config[i] = new StbVorbis.Residue();
			}
			if (f.residue_config == null)
			{
				return StbVorbis.error(f, 3);
			}
			for (int i = 0; i < f.residue_count; i++)
			{
				byte* ptr9 = stackalloc byte[(UIntPtr)64];
				StbVorbis.Residue residue = f.residue_config[i];
				f.residue_types[i] = (ushort)StbVorbis.get_bits(f, 16);
				if (f.residue_types[i] > 2)
				{
					return StbVorbis.error(f, 20);
				}
				residue.begin = StbVorbis.get_bits(f, 24);
				residue.end = StbVorbis.get_bits(f, 24);
				if (residue.end < residue.begin)
				{
					return StbVorbis.error(f, 20);
				}
				residue.part_size = StbVorbis.get_bits(f, 24) + 1U;
				residue.classifications = (byte)(StbVorbis.get_bits(f, 6) + 1U);
				residue.classbook = (byte)StbVorbis.get_bits(f, 8);
				if ((int)residue.classbook >= f.codebook_count)
				{
					return StbVorbis.error(f, 20);
				}
				for (int k = 0; k < (int)residue.classifications; k++)
				{
					byte b3 = 0;
					byte b4 = (byte)StbVorbis.get_bits(f, 3);
					if (StbVorbis.get_bits(f, 1) != 0U)
					{
						b3 = (byte)StbVorbis.get_bits(f, 5);
					}
					ptr9[k] = b3 * 8 + b4;
				}
				residue.residue_books = new short[(int)residue.classifications, 8];
				if (residue.residue_books == null)
				{
					return StbVorbis.error(f, 3);
				}
				for (int k = 0; k < (int)residue.classifications; k++)
				{
					for (int l = 0; l < 8; l++)
					{
						if (((int)ptr9[k] & (1 << l)) != 0)
						{
							residue.residue_books[k, l] = (short)StbVorbis.get_bits(f, 8);
							if ((int)residue.residue_books[k, l] >= f.codebook_count)
							{
								return StbVorbis.error(f, 20);
							}
						}
						else
						{
							residue.residue_books[k, l] = -1;
						}
					}
				}
				residue.classdata = (byte**)StbVorbis.setup_malloc(f, sizeof(byte*) * f.codebooks[residue.classbook].entries);
				if (residue.classdata == null)
				{
					return StbVorbis.error(f, 3);
				}
				CRuntime.memset((void*)residue.classdata, 0, (ulong)((long)(sizeof(byte*) * f.codebooks[residue.classbook].entries)));
				for (int k = 0; k < f.codebooks[residue.classbook].entries; k++)
				{
					int dimensions = f.codebooks[residue.classbook].dimensions;
					int num25 = k;
					*(IntPtr*)(residue.classdata + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) = StbVorbis.setup_malloc(f, dimensions);
					if (*(IntPtr*)(residue.classdata + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) == (IntPtr)((UIntPtr)0))
					{
						return StbVorbis.error(f, 3);
					}
					for (int l = dimensions - 1; l >= 0; l--)
					{
						*(*(IntPtr*)(residue.classdata + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)l) = (byte)(num25 % (int)residue.classifications);
						num25 /= (int)residue.classifications;
					}
				}
			}
			f.mapping_count = (int)(StbVorbis.get_bits(f, 6) + 1U);
			f.mapping = (StbVorbis.Mapping*)StbVorbis.setup_malloc(f, f.mapping_count * sizeof(StbVorbis.Mapping));
			if (f.mapping == null)
			{
				return StbVorbis.error(f, 3);
			}
			CRuntime.memset((void*)f.mapping, 0, (ulong)((long)(f.mapping_count * sizeof(StbVorbis.Mapping))));
			for (int i = 0; i < f.mapping_count; i++)
			{
				StbVorbis.Mapping* ptr10 = f.mapping + i;
				if (StbVorbis.get_bits(f, 16) != 0U)
				{
					return StbVorbis.error(f, 20);
				}
				ptr10->chan = (StbVorbis.MappingChannel*)StbVorbis.setup_malloc(f, f.channels * sizeof(StbVorbis.MappingChannel));
				if (ptr10->chan == null)
				{
					return StbVorbis.error(f, 3);
				}
				if (StbVorbis.get_bits(f, 1) != 0U)
				{
					ptr10->submaps = (byte)(StbVorbis.get_bits(f, 4) + 1U);
				}
				else
				{
					ptr10->submaps = 1;
				}
				if ((int)ptr10->submaps > num)
				{
					num = (int)ptr10->submaps;
				}
				if (StbVorbis.get_bits(f, 1) != 0U)
				{
					ptr10->coupling_steps = (ushort)(StbVorbis.get_bits(f, 8) + 1U);
					for (int l = 0; l < (int)ptr10->coupling_steps; l++)
					{
						ptr10->chan[l].magnitude = (byte)StbVorbis.get_bits(f, StbVorbis.ilog(f.channels - 1));
						ptr10->chan[l].angle = (byte)StbVorbis.get_bits(f, StbVorbis.ilog(f.channels - 1));
						if ((int)ptr10->chan[l].magnitude >= f.channels)
						{
							return StbVorbis.error(f, 20);
						}
						if ((int)ptr10->chan[l].angle >= f.channels)
						{
							return StbVorbis.error(f, 20);
						}
						if (ptr10->chan[l].magnitude == ptr10->chan[l].angle)
						{
							return StbVorbis.error(f, 20);
						}
					}
				}
				else
				{
					ptr10->coupling_steps = 0;
				}
				if (StbVorbis.get_bits(f, 2) != 0U)
				{
					return StbVorbis.error(f, 20);
				}
				if (ptr10->submaps > 1)
				{
					for (int k = 0; k < f.channels; k++)
					{
						ptr10->chan[k].mux = (byte)StbVorbis.get_bits(f, 4);
						if (ptr10->chan[k].mux >= ptr10->submaps)
						{
							return StbVorbis.error(f, 20);
						}
					}
				}
				else
				{
					for (int k = 0; k < f.channels; k++)
					{
						ptr10->chan[k].mux = 0;
					}
				}
				for (int k = 0; k < (int)ptr10->submaps; k++)
				{
					uint num26 = StbVorbis.get_bits(f, 8);
					*((ref ptr10->submap_floor.FixedElementField) + k) = (byte)StbVorbis.get_bits(f, 8);
					*((ref ptr10->submap_residue.FixedElementField) + k) = (byte)StbVorbis.get_bits(f, 8);
					if ((int)(*((ref ptr10->submap_floor.FixedElementField) + k)) >= f.floor_count)
					{
						return StbVorbis.error(f, 20);
					}
					if ((int)(*((ref ptr10->submap_residue.FixedElementField) + k)) >= f.residue_count)
					{
						return StbVorbis.error(f, 20);
					}
				}
			}
			f.mode_count = (int)(StbVorbis.get_bits(f, 6) + 1U);
			for (int i = 0; i < f.mode_count; i++)
			{
				StbVorbis.Mode* ptr11 = f.mode_config + i;
				ptr11->blockflag = (byte)StbVorbis.get_bits(f, 1);
				ptr11->windowtype = (ushort)StbVorbis.get_bits(f, 16);
				ptr11->transformtype = (ushort)StbVorbis.get_bits(f, 16);
				ptr11->mapping = (byte)StbVorbis.get_bits(f, 8);
				if (ptr11->windowtype != 0)
				{
					return StbVorbis.error(f, 20);
				}
				if (ptr11->transformtype != 0)
				{
					return StbVorbis.error(f, 20);
				}
				if ((int)ptr11->mapping >= f.mapping_count)
				{
					return StbVorbis.error(f, 20);
				}
			}
			StbVorbis.flush_packet(f);
			f.previous_length = 0;
			for (int i = 0; i < f.channels; i++)
			{
				f.channel_buffers[i] = (float*)StbVorbis.setup_malloc(f, 4 * f.blocksize_1);
				f.previous_window[i] = (float*)StbVorbis.setup_malloc(f, 4 * f.blocksize_1 / 2);
				f.finalY[i] = (short*)StbVorbis.setup_malloc(f, 2 * num2);
				if (f.channel_buffers[i] == null || f.previous_window[i] == null || f.finalY[i] == null)
				{
					return StbVorbis.error(f, 3);
				}
			}
			if (StbVorbis.init_blocksize(f, 0, f.blocksize_0) == 0)
			{
				return 0;
			}
			if (StbVorbis.init_blocksize(f, 1, f.blocksize_1) == 0)
			{
				return 0;
			}
			f.blocksize[0] = f.blocksize_0;
			f.blocksize[1] = f.blocksize_1;
			uint num27 = (uint)(f.blocksize_1 * 4 >> 1);
			int num28 = 0;
			for (int m = 0; m < f.residue_count; m++)
			{
				StbVorbis.Residue residue2 = f.residue_config[m];
				int num29 = (int)((long)(residue2.end - residue2.begin) / (long)((ulong)residue2.part_size));
				if (num29 > num28)
				{
					num28 = num29;
				}
			}
			uint temp_memory_required = (uint)(f.channels * (sizeof(void*) + num28 * sizeof(char*)));
			f.temp_memory_required = temp_memory_required;
			if (num27 > f.temp_memory_required)
			{
				f.temp_memory_required = num27;
			}
			f.first_decode = 1;
			sbyte* alloc_buffer = f.alloc.alloc_buffer;
			UIntPtr uintPtr = (UIntPtr)0;
			f.first_audio_page_offset = StbVorbis.stb_vorbis_get_file_offset(f);
			return 1;
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0001E8A4 File Offset: 0x0001CAA4
		public unsafe static void vorbis_deinit(StbVorbis.stb_vorbis p)
		{
			int i;
			if (p.residue_config != null)
			{
				for (i = 0; i < p.residue_count; i++)
				{
					StbVorbis.Residue residue = p.residue_config[i];
					if (residue.classdata != null)
					{
						for (int j = 0; j < p.codebooks[residue.classbook].entries; j++)
						{
							StbVorbis.setup_free(p, *(IntPtr*)(residue.classdata + (IntPtr)j * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)));
						}
						StbVorbis.setup_free(p, (void*)residue.classdata);
					}
				}
			}
			if (p.codebooks != null)
			{
				for (i = 0; i < p.codebook_count; i++)
				{
					StbVorbis.Codebook* ptr = p.codebooks + i;
					StbVorbis.setup_free(p, (void*)ptr->codeword_lengths);
					StbVorbis.setup_free(p, (void*)ptr->multiplicands);
					StbVorbis.setup_free(p, (void*)ptr->codewords);
					StbVorbis.setup_free(p, (void*)ptr->sorted_codewords);
					StbVorbis.setup_free(p, (void*)((ptr->sorted_values != null) ? (ptr->sorted_values - 1) : null));
				}
				StbVorbis.setup_free(p, (void*)p.codebooks);
			}
			StbVorbis.setup_free(p, (void*)p.floor_config);
			if (p.mapping != null)
			{
				for (i = 0; i < p.mapping_count; i++)
				{
					StbVorbis.setup_free(p, (void*)p.mapping[i].chan);
				}
				StbVorbis.setup_free(p, (void*)p.mapping);
			}
			i = 0;
			while (i < p.channels && i < 16)
			{
				StbVorbis.setup_free(p, (void*)p.channel_buffers[i]);
				StbVorbis.setup_free(p, (void*)p.previous_window[i]);
				StbVorbis.setup_free(p, (void*)p.finalY[i]);
				i++;
			}
			for (i = 0; i < 2; i++)
			{
				StbVorbis.setup_free(p, (void*)p.A[i]);
				StbVorbis.setup_free(p, (void*)p.B[i]);
				StbVorbis.setup_free(p, (void*)p.C[i]);
				StbVorbis.setup_free(p, (void*)p.window[i]);
				StbVorbis.setup_free(p, (void*)p.bit_reverse[i]);
			}
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0001EA89 File Offset: 0x0001CC89
		public static void stb_vorbis_close(StbVorbis.stb_vorbis p)
		{
			if (p == null)
			{
				return;
			}
			StbVorbis.vorbis_deinit(p);
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0001EA98 File Offset: 0x0001CC98
		public unsafe static void vorbis_init(StbVorbis.stb_vorbis p, StbVorbis.stb_vorbis_alloc* z)
		{
			if (z != null)
			{
				p.alloc = *z;
				p.alloc.alloc_buffer_length_in_bytes = (p.alloc.alloc_buffer_length_in_bytes + 3) & -4;
				p.temp_offset = p.alloc.alloc_buffer_length_in_bytes;
			}
			p.eof = 0;
			p.error = 0;
			p.stream = null;
			p.codebooks = null;
			p.page_crc_tests = -1;
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0001EB07 File Offset: 0x0001CD07
		public static int stb_vorbis_get_sample_offset(StbVorbis.stb_vorbis f)
		{
			if (f.current_loc_valid != 0)
			{
				return (int)f.current_loc;
			}
			return -1;
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x0001EB1C File Offset: 0x0001CD1C
		public static StbVorbis.stb_vorbis_info stb_vorbis_get_info(StbVorbis.stb_vorbis f)
		{
			return new StbVorbis.stb_vorbis_info
			{
				channels = f.channels,
				sample_rate = f.sample_rate,
				setup_memory_required = f.setup_memory_required,
				setup_temp_memory_required = f.setup_temp_memory_required,
				temp_memory_required = f.temp_memory_required,
				max_frame_size = f.blocksize_1 >> 1
			};
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0001EB82 File Offset: 0x0001CD82
		public static int stb_vorbis_get_error(StbVorbis.stb_vorbis f)
		{
			int error = f.error;
			f.error = 0;
			return error;
		}

		// Token: 0x060001FB RID: 507 RVA: 0x0001EB91 File Offset: 0x0001CD91
		public static StbVorbis.stb_vorbis vorbis_alloc(StbVorbis.stb_vorbis f)
		{
			return new StbVorbis.stb_vorbis();
		}

		// Token: 0x060001FC RID: 508 RVA: 0x0001EB98 File Offset: 0x0001CD98
		public static void stb_vorbis_flush_pushdata(StbVorbis.stb_vorbis f)
		{
			f.previous_length = 0;
			f.page_crc_tests = 0;
			f.discard_samples_deferred = 0;
			f.current_loc_valid = 0;
			f.first_decode = 0;
			f.samples_output = 0U;
			f.channel_buffer_start = 0;
			f.channel_buffer_end = 0;
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0001EBD4 File Offset: 0x0001CDD4
		public unsafe static int vorbis_search_for_page_pushdata(StbVorbis.stb_vorbis f, byte* data, int data_len)
		{
			int i;
			for (i = 0; i < f.page_crc_tests; i++)
			{
				f.scan[i].bytes_done = 0;
			}
			if (f.page_crc_tests < 4)
			{
				if (data_len < 4)
				{
					return 0;
				}
				data_len -= 3;
				for (i = 0; i < data_len; i++)
				{
					if (data[i] == 79 && CRuntime.memcmp(data + i, StbVorbis.ogg_page_header, 4UL) == 0)
					{
						if (i + 26 >= data_len || i + 27 + (int)data[i + 26] >= data_len)
						{
							data_len = i;
							break;
						}
						int num = (int)(27 + data[i + 26]);
						int j;
						for (j = 0; j < (int)data[i + 26]; j++)
						{
							num += (int)data[i + 27 + j];
						}
						uint num2 = 0U;
						for (j = 0; j < 22; j++)
						{
							num2 = StbVorbis.crc32_update(num2, data[i + j]);
						}
						while (j < 26)
						{
							num2 = StbVorbis.crc32_update(num2, 0);
							j++;
						}
						int num3 = f.page_crc_tests;
						f.page_crc_tests = num3 + 1;
						int num4 = num3;
						f.scan[num4].bytes_left = num - j;
						f.scan[num4].crc_so_far = num2;
						f.scan[num4].goal_crc = (uint)((int)data[i + 22] + ((int)data[i + 23] << 8) + ((int)data[i + 24] << 16) + ((int)data[i + 25] << 24));
						if (data[i + 27 + (int)data[i + 26] - 1] == 255)
						{
							f.scan[num4].sample_loc = uint.MaxValue;
						}
						else
						{
							f.scan[num4].sample_loc = (uint)((int)data[i + 6] + ((int)data[i + 7] << 8) + ((int)data[i + 8] << 16) + ((int)data[i + 9] << 24));
						}
						f.scan[num4].bytes_done = i + j;
						if (f.page_crc_tests == 4)
						{
							break;
						}
					}
				}
			}
			i = 0;
			while (i < f.page_crc_tests)
			{
				int bytes_done = f.scan[i].bytes_done;
				int num5 = f.scan[i].bytes_left;
				if (num5 > data_len - bytes_done)
				{
					num5 = data_len - bytes_done;
				}
				uint num6 = f.scan[i].crc_so_far;
				for (int k = 0; k < num5; k++)
				{
					num6 = StbVorbis.crc32_update(num6, data[bytes_done + k]);
				}
				StbVorbis.CRCscan[] scan = f.scan;
				int num7 = i;
				scan[num7].bytes_left = scan[num7].bytes_left - num5;
				f.scan[i].crc_so_far = num6;
				if (f.scan[i].bytes_left == 0)
				{
					if (f.scan[i].crc_so_far == f.scan[i].goal_crc)
					{
						data_len = bytes_done + num5;
						f.page_crc_tests = -1;
						f.previous_length = 0;
						f.next_seg = -1;
						f.current_loc = f.scan[i].sample_loc;
						f.current_loc_valid = ((f.current_loc != uint.MaxValue) ? 1 : 0);
						return data_len;
					}
					StbVorbis.CRCscan[] scan2 = f.scan;
					int num8 = i;
					StbVorbis.CRCscan[] scan3 = f.scan;
					int num3 = f.page_crc_tests - 1;
					f.page_crc_tests = num3;
					scan2[num8] = scan3[num3];
				}
				else
				{
					i++;
				}
			}
			return data_len;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0001EF14 File Offset: 0x0001D114
		public unsafe static int stb_vorbis_decode_frame_pushdata(StbVorbis.stb_vorbis f, byte* data, int data_len, int* channels, ref float*[] output, int* samples)
		{
			if (f.push_mode == 0)
			{
				return StbVorbis.error(f, 2);
			}
			if (f.page_crc_tests >= 0)
			{
				*samples = 0;
				return StbVorbis.vorbis_search_for_page_pushdata(f, data, data_len);
			}
			f.stream = data;
			f.stream_end = data + data_len;
			f.error = 0;
			if (StbVorbis.is_whole_packet_present(f, 0) == 0)
			{
				*samples = 0;
				return 0;
			}
			int num;
			int num2;
			int right;
			if (StbVorbis.vorbis_decode_packet(f, &num, &num2, &right) != 0)
			{
				num = StbVorbis.vorbis_finish_frame(f, num, num2, right);
				for (int i = 0; i < f.channels; i++)
				{
					f.outputs[i] = f.channel_buffers[i] + num2;
				}
				if (channels != null)
				{
					*channels = f.channels;
				}
				*samples = num;
				output = f.outputs;
				return (int)((long)(f.stream - data));
			}
			int error = f.error;
			if (error == 35)
			{
				f.error = 0;
				while (StbVorbis.get8_packet(f) != -1 && f.eof == 0)
				{
				}
				*samples = 0;
				return (int)((long)(f.stream - data));
			}
			if (error == 32 && f.previous_length == 0)
			{
				f.error = 0;
				while (StbVorbis.get8_packet(f) != -1 && f.eof == 0)
				{
				}
				*samples = 0;
				return (int)((long)(f.stream - data));
			}
			StbVorbis.stb_vorbis_flush_pushdata(f);
			f.error = error;
			*samples = 0;
			return 1;
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0001F060 File Offset: 0x0001D260
		public unsafe static StbVorbis.stb_vorbis stb_vorbis_open_pushdata(byte* data, int data_len, int* data_used, int* error, StbVorbis.stb_vorbis_alloc* alloc)
		{
			StbVorbis.stb_vorbis stb_vorbis = new StbVorbis.stb_vorbis();
			StbVorbis.vorbis_init(stb_vorbis, alloc);
			stb_vorbis.stream = data;
			stb_vorbis.stream_end = data + data_len;
			stb_vorbis.push_mode = 1;
			if (StbVorbis.start_decoder(stb_vorbis) == 0)
			{
				if (stb_vorbis.eof != 0)
				{
					*error = 1;
				}
				else
				{
					*error = stb_vorbis.error;
				}
				return null;
			}
			StbVorbis.stb_vorbis stb_vorbis2 = StbVorbis.vorbis_alloc(stb_vorbis);
			if (stb_vorbis2 != null)
			{
				stb_vorbis2 = stb_vorbis;
				*data_used = (int)((long)(stb_vorbis2.stream - data));
				*error = 0;
				return stb_vorbis2;
			}
			StbVorbis.vorbis_deinit(stb_vorbis);
			return null;
		}

		// Token: 0x06000200 RID: 512 RVA: 0x0001F0D7 File Offset: 0x0001D2D7
		public static uint stb_vorbis_get_file_offset(StbVorbis.stb_vorbis f)
		{
			if (f.push_mode != 0)
			{
				return 0U;
			}
			return (uint)((long)(f.stream - f.stream_start));
		}

		// Token: 0x06000201 RID: 513 RVA: 0x0001F0F4 File Offset: 0x0001D2F4
		public unsafe static uint vorbis_find_page(StbVorbis.stb_vorbis f, uint* end, uint* last)
		{
			while (f.eof == 0)
			{
				if (StbVorbis.get8(f) == 79)
				{
					uint num = StbVorbis.stb_vorbis_get_file_offset(f);
					if (num - 25U > f.stream_len)
					{
						return 0U;
					}
					int num2 = 1;
					while (num2 < 4 && StbVorbis.get8(f) == StbVorbis.ogg_page_header[num2])
					{
						num2++;
					}
					if (f.eof != 0)
					{
						return 0U;
					}
					if (num2 == 4)
					{
						byte* ptr = stackalloc byte[(UIntPtr)27];
						uint num3;
						for (num3 = 0U; num3 < 4U; num3 += 1U)
						{
							ptr[num3] = StbVorbis.ogg_page_header[(int)num3];
						}
						while (num3 < 27U)
						{
							ptr[num3] = StbVorbis.get8(f);
							num3 += 1U;
						}
						if (f.eof != 0)
						{
							return 0U;
						}
						if (ptr[4] == 0)
						{
							uint num4 = (uint)((int)ptr[22] + ((int)ptr[23] << 8) + ((int)ptr[24] << 16) + ((int)ptr[25] << 24));
							for (num3 = 22U; num3 < 26U; num3 += 1U)
							{
								ptr[num3] = 0;
							}
							uint num5 = 0U;
							for (num3 = 0U; num3 < 27U; num3 += 1U)
							{
								num5 = StbVorbis.crc32_update(num5, ptr[num3]);
							}
							uint num6 = 0U;
							for (num3 = 0U; num3 < (uint)ptr[26]; num3 += 1U)
							{
								int num7 = (int)StbVorbis.get8(f);
								num5 = StbVorbis.crc32_update(num5, (byte)num7);
								num6 += (uint)num7;
							}
							if (num6 != 0U && f.eof != 0)
							{
								return 0U;
							}
							for (num3 = 0U; num3 < num6; num3 += 1U)
							{
								num5 = StbVorbis.crc32_update(num5, StbVorbis.get8(f));
							}
							if (num5 == num4)
							{
								if (end != null)
								{
									*end = StbVorbis.stb_vorbis_get_file_offset(f);
								}
								if (last != null)
								{
									if ((ptr[5] & 4) != 0)
									{
										*last = 1U;
									}
									else
									{
										*last = 0U;
									}
								}
								StbVorbis.set_file_offset(f, num - 1U);
								return 1U;
							}
						}
					}
					StbVorbis.set_file_offset(f, num);
				}
			}
			return 0U;
		}

		// Token: 0x06000202 RID: 514 RVA: 0x0001F288 File Offset: 0x0001D488
		public unsafe static int get_seek_page_info(StbVorbis.stb_vorbis f, StbVorbis.ProbedPage* z)
		{
			byte* ptr = stackalloc byte[(UIntPtr)27];
			byte* ptr2 = stackalloc byte[(UIntPtr)255];
			z->page_start = StbVorbis.stb_vorbis_get_file_offset(f);
			StbVorbis.getn(f, ptr, 27);
			if (*ptr != 79 || ptr[1] != 103 || ptr[2] != 103 || ptr[3] != 83)
			{
				return 0;
			}
			StbVorbis.getn(f, ptr2, (int)ptr[26]);
			int num = 0;
			for (int i = 0; i < (int)ptr[26]; i++)
			{
				num += (int)ptr2[i];
			}
			z->page_end = (uint)((ulong)(z->page_start + 27U + (uint)ptr[26]) + (ulong)((long)num));
			z->last_decoded_sample = (uint)((int)ptr[6] + ((int)ptr[7] << 8) + ((int)ptr[8] << 16) + ((int)ptr[9] << 24));
			StbVorbis.set_file_offset(f, z->page_start);
			return 1;
		}

		// Token: 0x06000203 RID: 515 RVA: 0x0001F34C File Offset: 0x0001D54C
		public unsafe static int go_to_page_before(StbVorbis.stb_vorbis f, uint limit_offset)
		{
			uint loc;
			if (limit_offset >= 65536U && limit_offset - 65536U >= f.first_audio_page_offset)
			{
				loc = limit_offset - 65536U;
			}
			else
			{
				loc = f.first_audio_page_offset;
			}
			StbVorbis.set_file_offset(f, loc);
			uint num;
			while (StbVorbis.vorbis_find_page(f, &num, null) != 0U)
			{
				if (num >= limit_offset && StbVorbis.stb_vorbis_get_file_offset(f) < limit_offset)
				{
					return 1;
				}
				StbVorbis.set_file_offset(f, num);
			}
			return 0;
		}

		// Token: 0x06000204 RID: 516 RVA: 0x0001F3B0 File Offset: 0x0001D5B0
		public unsafe static int seek_to_sample_coarse(StbVorbis.stb_vorbis f, uint sample_number)
		{
			StbVorbis.ProbedPage probedPage = default(StbVorbis.ProbedPage);
			StbVorbis.ProbedPage probedPage2 = default(StbVorbis.ProbedPage);
			StbVorbis.ProbedPage probedPage3 = default(StbVorbis.ProbedPage);
			double num = 0.0;
			double num2 = 0.0;
			int num3 = 0;
			uint num4 = StbVorbis.stb_vorbis_stream_length_in_samples(f);
			if (num4 == 0U)
			{
				return StbVorbis.error(f, 7);
			}
			if (sample_number > num4)
			{
				return StbVorbis.error(f, 11);
			}
			uint num5 = (uint)(f.blocksize_1 - f.blocksize_0 >> 2);
			if (sample_number < num5)
			{
				sample_number = 0U;
			}
			else
			{
				sample_number -= num5;
			}
			probedPage = f.p_first;
			while (probedPage.last_decoded_sample == 4294967295U)
			{
				StbVorbis.set_file_offset(f, probedPage.page_end);
				if (StbVorbis.get_seek_page_info(f, &probedPage) == 0)
				{
					IL_34F:
					StbVorbis.stb_vorbis_seek_start(f);
					return StbVorbis.error(f, 37);
				}
			}
			probedPage2 = f.p_last;
			if (sample_number <= probedPage.last_decoded_sample)
			{
				if (StbVorbis.stb_vorbis_seek_start(f) != 0)
				{
					return 1;
				}
				return 0;
			}
			else
			{
				IL_24E:
				while (probedPage.page_end != probedPage2.page_start)
				{
					uint num6 = probedPage2.page_start - probedPage.page_end;
					if (num6 <= 65536U)
					{
						StbVorbis.set_file_offset(f, probedPage.page_end);
					}
					else
					{
						if (num3 < 2)
						{
							if (num3 == 0)
							{
								num2 = (probedPage2.page_end - probedPage.page_start) / probedPage2.last_decoded_sample;
								num = probedPage.page_start + num2 * (sample_number - probedPage.last_decoded_sample);
							}
							else
							{
								double num7 = (sample_number - probedPage3.last_decoded_sample) * num2;
								if (num7 >= 0.0 && num7 < 8000.0)
								{
									num7 = 8000.0;
								}
								if (num7 < 0.0 && num7 > -8000.0)
								{
									num7 = -8000.0;
								}
								num += num7 * 2.0;
							}
							if (num < probedPage.page_end)
							{
								num = probedPage.page_end;
							}
							if (num > probedPage2.page_start - 65536U)
							{
								num = probedPage2.page_start - 65536U;
							}
							StbVorbis.set_file_offset(f, (uint)num);
						}
						else
						{
							StbVorbis.set_file_offset(f, probedPage.page_end + num6 / 2U - 32768U);
						}
						if (StbVorbis.vorbis_find_page(f, null, null) == 0U)
						{
							goto IL_34F;
						}
					}
					while (StbVorbis.get_seek_page_info(f, &probedPage3) != 0)
					{
						if (probedPage3.last_decoded_sample == 4294967295U)
						{
							StbVorbis.set_file_offset(f, probedPage3.page_end);
						}
						else
						{
							if (probedPage3.page_start != probedPage2.page_start)
							{
								if (sample_number < probedPage3.last_decoded_sample)
								{
									probedPage2 = probedPage3;
								}
								else
								{
									probedPage = probedPage3;
								}
								num3++;
								goto IL_24E;
							}
							goto IL_25F;
						}
					}
					goto IL_34F;
				}
				IL_25F:
				int num8 = (int)probedPage.page_start;
				StbVorbis.set_file_offset(f, (uint)num8);
				if (StbVorbis.start_page(f) == 0)
				{
					return StbVorbis.error(f, 37);
				}
				int num9 = f.end_seg_with_known_loc;
				int num10;
				for (;;)
				{
					int i = num9;
					while (i > 0 && f.segments[i - 1] == 255)
					{
						i--;
					}
					num10 = i;
					if (num10 > 0 || (f.page_flag & 1) == 0)
					{
						break;
					}
					if (StbVorbis.go_to_page_before(f, (uint)num8) == 0)
					{
						goto IL_34F;
					}
					num8 = (int)StbVorbis.stb_vorbis_get_file_offset(f);
					if (StbVorbis.start_page(f) == 0)
					{
						goto IL_34F;
					}
					num9 = f.segment_count - 1;
				}
				f.current_loc_valid = 0;
				f.last_seg = 0;
				f.valid_bits = 0;
				f.packet_bytes = 0;
				f.bytes_in_seg = 0;
				f.previous_length = 0;
				f.next_seg = num10;
				for (int i = 0; i < num10; i++)
				{
					StbVorbis.skip(f, (int)f.segments[i]);
				}
				if (StbVorbis.vorbis_pump_first_frame(f) == 0)
				{
					return 0;
				}
				if (f.current_loc > sample_number)
				{
					return StbVorbis.error(f, 37);
				}
				return 1;
			}
		}

		// Token: 0x06000205 RID: 517 RVA: 0x0001F71C File Offset: 0x0001D91C
		public unsafe static int peek_decode_initial(StbVorbis.stb_vorbis f, int* p_left_start, int* p_left_end, int* p_right_start, int* p_right_end, int* mode)
		{
			if (StbVorbis.vorbis_decode_initial(f, p_left_start, p_left_end, p_right_start, p_right_end, mode) == 0)
			{
				return 0;
			}
			int num = 1 + StbVorbis.ilog(f.mode_count - 1);
			if (f.mode_config[*mode].blockflag != 0)
			{
				num += 2;
			}
			int num2 = (num + 7) / 8;
			f.bytes_in_seg += (byte)num2;
			f.packet_bytes -= num2;
			StbVorbis.skip(f, -num2);
			if (f.next_seg == -1)
			{
				f.next_seg = f.segment_count - 1;
			}
			else
			{
				f.next_seg--;
			}
			f.valid_bits = 0;
			return 1;
		}

		// Token: 0x06000206 RID: 518 RVA: 0x0001F7C4 File Offset: 0x0001D9C4
		public unsafe static int stb_vorbis_seek_frame(StbVorbis.stb_vorbis f, uint sample_number)
		{
			if (f.push_mode != 0)
			{
				return StbVorbis.error(f, 2);
			}
			if (StbVorbis.seek_to_sample_coarse(f, sample_number) == 0)
			{
				return 0;
			}
			uint num = (uint)(f.blocksize_1 * 3 - f.blocksize_0 >> 2);
			while (f.current_loc < sample_number)
			{
				int num2;
				int num3;
				int num4;
				int num5;
				int num6;
				if (StbVorbis.peek_decode_initial(f, &num2, &num3, &num4, &num5, &num6) == 0)
				{
					return StbVorbis.error(f, 37);
				}
				int num7 = num4 - num2;
				if ((ulong)f.current_loc + (ulong)((long)num7) > (ulong)sample_number)
				{
					return 1;
				}
				if ((ulong)f.current_loc + (ulong)((long)num7) + (ulong)num > (ulong)sample_number)
				{
					StbVorbis.vorbis_pump_first_frame(f);
				}
				else
				{
					f.current_loc += (uint)num7;
					f.previous_length = 0;
					StbVorbis.maybe_start_packet(f);
					StbVorbis.flush_packet(f);
				}
			}
			return 1;
		}

		// Token: 0x06000207 RID: 519 RVA: 0x0001F880 File Offset: 0x0001DA80
		public unsafe static int stb_vorbis_seek(StbVorbis.stb_vorbis f, uint sample_number)
		{
			if (StbVorbis.stb_vorbis_seek_frame(f, sample_number) == 0)
			{
				return 0;
			}
			if (sample_number != f.current_loc)
			{
				uint current_loc = f.current_loc;
				float*[] array = null;
				int num;
				StbVorbis.stb_vorbis_get_frame_float(f, &num, ref array);
				f.channel_buffer_start += (int)(sample_number - current_loc);
			}
			return 1;
		}

		// Token: 0x06000208 RID: 520 RVA: 0x0001F8C7 File Offset: 0x0001DAC7
		public static int stb_vorbis_seek_start(StbVorbis.stb_vorbis f)
		{
			if (f.push_mode != 0)
			{
				return StbVorbis.error(f, 2);
			}
			StbVorbis.set_file_offset(f, f.first_audio_page_offset);
			f.previous_length = 0;
			f.first_decode = 1;
			f.next_seg = -1;
			return StbVorbis.vorbis_pump_first_frame(f);
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0001F904 File Offset: 0x0001DB04
		public unsafe static uint stb_vorbis_stream_length_in_samples(StbVorbis.stb_vorbis f)
		{
			if (f.push_mode != 0)
			{
				return (uint)StbVorbis.error(f, 2);
			}
			if (f.total_samples == 0U)
			{
				sbyte* data = stackalloc sbyte[(UIntPtr)6];
				uint loc = StbVorbis.stb_vorbis_get_file_offset(f);
				uint loc2;
				if (f.stream_len >= 65536U && f.stream_len - 65536U >= f.first_audio_page_offset)
				{
					loc2 = f.stream_len - 65536U;
				}
				else
				{
					loc2 = f.first_audio_page_offset;
				}
				StbVorbis.set_file_offset(f, loc2);
				uint num;
				uint num2;
				if (StbVorbis.vorbis_find_page(f, &num, &num2) == 0U)
				{
					f.error = 36;
					f.total_samples = uint.MaxValue;
				}
				else
				{
					uint num3 = StbVorbis.stb_vorbis_get_file_offset(f);
					while (num2 == 0U)
					{
						StbVorbis.set_file_offset(f, num);
						if (StbVorbis.vorbis_find_page(f, &num, &num2) == 0U)
						{
							break;
						}
						loc2 = num3 + 1U;
						num3 = StbVorbis.stb_vorbis_get_file_offset(f);
					}
					StbVorbis.set_file_offset(f, num3);
					StbVorbis.getn(f, (byte*)data, 6);
					uint num4 = StbVorbis.get32(f);
					uint num5 = StbVorbis.get32(f);
					if (num4 == 4294967295U && num5 == 4294967295U)
					{
						f.error = 36;
						f.total_samples = uint.MaxValue;
					}
					else
					{
						if (num5 != 0U)
						{
							num4 = 4294967294U;
						}
						f.total_samples = num4;
						f.p_last.page_start = num3;
						f.p_last.page_end = num;
						f.p_last.last_decoded_sample = num4;
					}
				}
				StbVorbis.set_file_offset(f, loc);
			}
			if (f.total_samples != 4294967295U)
			{
				return f.total_samples;
			}
			return 0U;
		}

		// Token: 0x0600020A RID: 522 RVA: 0x0001FA53 File Offset: 0x0001DC53
		public static float stb_vorbis_stream_length_in_seconds(StbVorbis.stb_vorbis f)
		{
			return StbVorbis.stb_vorbis_stream_length_in_samples(f) / f.sample_rate;
		}

		// Token: 0x0600020B RID: 523 RVA: 0x0001FA68 File Offset: 0x0001DC68
		public unsafe static int stb_vorbis_get_frame_float(StbVorbis.stb_vorbis f, int* channels, ref float*[] output)
		{
			if (f.push_mode != 0)
			{
				return StbVorbis.error(f, 2);
			}
			int num;
			int num2;
			int right;
			if (StbVorbis.vorbis_decode_packet(f, &num, &num2, &right) == 0)
			{
				f.channel_buffer_start = (f.channel_buffer_end = 0);
				return 0;
			}
			num = StbVorbis.vorbis_finish_frame(f, num, num2, right);
			for (int i = 0; i < f.channels; i++)
			{
				f.outputs[i] = f.channel_buffers[i] + num2;
			}
			f.channel_buffer_start = num2;
			f.channel_buffer_end = num2 + num;
			if (channels != null)
			{
				*channels = f.channels;
			}
			output = f.outputs;
			return num;
		}

		// Token: 0x0600020C RID: 524 RVA: 0x0001FB00 File Offset: 0x0001DD00
		public unsafe static StbVorbis.stb_vorbis stb_vorbis_open_memory(byte* data, int len, int* error, StbVorbis.stb_vorbis_alloc* alloc)
		{
			StbVorbis.stb_vorbis stb_vorbis = new StbVorbis.stb_vorbis();
			if (data == null)
			{
				return null;
			}
			StbVorbis.vorbis_init(stb_vorbis, alloc);
			stb_vorbis.stream = data;
			stb_vorbis.stream_end = data + len;
			stb_vorbis.stream_start = stb_vorbis.stream;
			stb_vorbis.stream_len = (uint)len;
			stb_vorbis.push_mode = 0;
			if (StbVorbis.start_decoder(stb_vorbis) != 0 && StbVorbis.vorbis_alloc(stb_vorbis) != null)
			{
				StbVorbis.stb_vorbis stb_vorbis2 = stb_vorbis;
				StbVorbis.vorbis_pump_first_frame(stb_vorbis2);
				if (error != null)
				{
					*error = 0;
				}
				return stb_vorbis2;
			}
			if (error != null)
			{
				*error = stb_vorbis.error;
			}
			StbVorbis.vorbis_deinit(stb_vorbis);
			return null;
		}

		// Token: 0x0600020D RID: 525 RVA: 0x0001FB80 File Offset: 0x0001DD80
		public unsafe static void copy_samples(short* dest, float* src, int len)
		{
			for (int i = 0; i < len; i++)
			{
				int num = (int)(src[i] * 32768f);
				if (num + 32768 > 65535)
				{
					num = ((num < 0) ? (-32768) : 32767);
				}
				dest[i] = (short)num;
			}
		}

		// Token: 0x0600020E RID: 526 RVA: 0x0001FBD0 File Offset: 0x0001DDD0
		public unsafe static void compute_samples(int mask, short* output, int num_c, float*[] data, int d_offset, int len)
		{
			float* ptr = stackalloc float[(UIntPtr)128];
			int num = 32;
			for (int i = 0; i < len; i += 32)
			{
				CRuntime.memset((void*)ptr, 0, 128UL);
				if (i + num > len)
				{
					num = len - i;
				}
				for (int j = 0; j < num_c; j++)
				{
					if (((int)StbVorbis.channel_position[num_c, j] & mask) != 0)
					{
						for (int k = 0; k < num; k++)
						{
							ptr[k] += data[j][d_offset + i + k];
						}
					}
				}
				for (int k = 0; k < num; k++)
				{
					int num2 = (int)(ptr[k] * 32768f);
					if (num2 + 32768 > 65535)
					{
						num2 = ((num2 < 0) ? (-32768) : 32767);
					}
					output[i + k] = (short)num2;
				}
			}
		}

		// Token: 0x0600020F RID: 527 RVA: 0x0001FCA8 File Offset: 0x0001DEA8
		public unsafe static void compute_stereo_samples(short* output, int num_c, float*[] data, int d_offset, int len)
		{
			float* ptr = stackalloc float[(UIntPtr)128];
			int num = 16;
			for (int i = 0; i < len; i += 16)
			{
				int num2 = i << 1;
				CRuntime.memset((void*)ptr, 0, 128UL);
				if (i + num > len)
				{
					num = len - i;
				}
				for (int j = 0; j < num_c; j++)
				{
					int num3 = (int)(StbVorbis.channel_position[num_c, j] & 6);
					if (num3 == 6)
					{
						for (int k = 0; k < num; k++)
						{
							ptr[k * 2] += data[j][d_offset + i + k];
							ptr[k * 2 + 1] += data[j][d_offset + i + k];
						}
					}
					else if (num3 == 2)
					{
						for (int k = 0; k < num; k++)
						{
							ptr[k * 2] += data[j][d_offset + i + k];
						}
					}
					else if (num3 == 4)
					{
						for (int k = 0; k < num; k++)
						{
							ptr[k * 2 + 1] += data[j][d_offset + i + k];
						}
					}
				}
				for (int k = 0; k < num << 1; k++)
				{
					int num4 = (int)(ptr[k] * 32768f);
					if (num4 + 32768 > 65535)
					{
						num4 = ((num4 < 0) ? (-32768) : 32767);
					}
					output[num2 + k] = (short)num4;
				}
			}
		}

		// Token: 0x06000210 RID: 528 RVA: 0x0001FE0C File Offset: 0x0001E00C
		public unsafe static void convert_samples_short(int buf_c, short** buffer, int b_offset, int data_c, float*[] data, int d_offset, int samples)
		{
			int i;
			if (buf_c != data_c && buf_c <= 2 && data_c <= 6)
			{
				for (i = 0; i < buf_c; i++)
				{
					StbVorbis.compute_samples(StbVorbis.channel_selector[buf_c, i], *(IntPtr*)(buffer + (IntPtr)i * (IntPtr)sizeof(short*) / (IntPtr)sizeof(short*)) / 2 + b_offset * 2, data_c, data, d_offset, samples);
				}
				return;
			}
			int num = ((buf_c < data_c) ? buf_c : data_c);
			for (i = 0; i < num; i++)
			{
				StbVorbis.copy_samples(*(IntPtr*)(buffer + (IntPtr)i * (IntPtr)sizeof(short*) / (IntPtr)sizeof(short*)) / 2 + b_offset * 2, data[i] + d_offset, samples);
			}
			while (i < buf_c)
			{
				CRuntime.memset((void*)(*(IntPtr*)(buffer + (IntPtr)i * (IntPtr)sizeof(short*) / (IntPtr)sizeof(short*)) + (byte*)((IntPtr)b_offset * 2)), 0, (ulong)((long)(2 * samples)));
				i++;
			}
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0001FEB8 File Offset: 0x0001E0B8
		public unsafe static int stb_vorbis_get_frame_short(StbVorbis.stb_vorbis f, int num_c, short** buffer, int num_samples)
		{
			float*[] data = null;
			int num = StbVorbis.stb_vorbis_get_frame_float(f, null, ref data);
			if (num > num_samples)
			{
				num = num_samples;
			}
			if (num != 0)
			{
				StbVorbis.convert_samples_short(num_c, buffer, 0, f.channels, data, 0, num);
			}
			return num;
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0001FEF0 File Offset: 0x0001E0F0
		public unsafe static void convert_channels_short_interleaved(int buf_c, short* buffer, int data_c, float*[] data, int d_offset, int len)
		{
			if (buf_c != data_c && buf_c <= 2 && data_c <= 6)
			{
				for (int i = 0; i < buf_c; i++)
				{
					StbVorbis.compute_stereo_samples(buffer, data_c, data, d_offset, len);
				}
				return;
			}
			int num = ((buf_c < data_c) ? buf_c : data_c);
			for (int j = 0; j < len; j++)
			{
				int i;
				for (i = 0; i < num; i++)
				{
					int num2 = (int)(data[i][d_offset + j] * 32768f);
					if (num2 + 32768 > 65535)
					{
						num2 = ((num2 < 0) ? (-32768) : 32767);
					}
					*(buffer++) = (short)num2;
				}
				while (i < buf_c)
				{
					*(buffer++) = 0;
					i++;
				}
			}
		}

		// Token: 0x06000213 RID: 531 RVA: 0x0001FF94 File Offset: 0x0001E194
		public unsafe static int stb_vorbis_get_frame_short_interleaved(StbVorbis.stb_vorbis f, int num_c, short* buffer, int num_shorts)
		{
			float*[] data = null;
			if (num_c == 1)
			{
				return StbVorbis.stb_vorbis_get_frame_short(f, num_c, &buffer, num_shorts);
			}
			int num = StbVorbis.stb_vorbis_get_frame_float(f, null, ref data);
			if (num != 0)
			{
				if (num * num_c > num_shorts)
				{
					num = num_shorts / num_c;
				}
				StbVorbis.convert_channels_short_interleaved(num_c, buffer, f.channels, data, 0, num);
			}
			return num;
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0001FFDC File Offset: 0x0001E1DC
		public unsafe static int stb_vorbis_get_samples_short_interleaved(StbVorbis.stb_vorbis f, int channels, short* buffer, int num_shorts)
		{
			float*[] array = null;
			int num = num_shorts / channels;
			int i = 0;
			if (f.channels > channels)
			{
			}
			while (i < num)
			{
				int num2 = f.channel_buffer_end - f.channel_buffer_start;
				if (i + num2 >= num)
				{
					num2 = num - i;
				}
				if (num2 != 0)
				{
					StbVorbis.convert_channels_short_interleaved(channels, buffer, f.channels, f.channel_buffers, f.channel_buffer_start, num2);
				}
				buffer += num2 * channels;
				i += num2;
				f.channel_buffer_start += num2;
				if (i == num || StbVorbis.stb_vorbis_get_frame_float(f, null, ref array) == 0)
				{
					break;
				}
			}
			return i;
		}

		// Token: 0x06000215 RID: 533 RVA: 0x00020064 File Offset: 0x0001E264
		public unsafe static int stb_vorbis_get_samples_short(StbVorbis.stb_vorbis f, int channels, short** buffer, int len)
		{
			float*[] array = null;
			int i = 0;
			if (f.channels > channels)
			{
			}
			while (i < len)
			{
				int num = f.channel_buffer_end - f.channel_buffer_start;
				if (i + num >= len)
				{
					num = len - i;
				}
				if (num != 0)
				{
					StbVorbis.convert_samples_short(channels, buffer, i, f.channels, f.channel_buffers, f.channel_buffer_start, num);
				}
				i += num;
				f.channel_buffer_start += num;
				if (i == len || StbVorbis.stb_vorbis_get_frame_float(f, null, ref array) == 0)
				{
					break;
				}
			}
			return i;
		}

		// Token: 0x06000216 RID: 534 RVA: 0x000200E0 File Offset: 0x0001E2E0
		public unsafe static int stb_vorbis_decode_memory(byte* mem, int len, int* channels, int* sample_rate, ref short* output)
		{
			int num;
			StbVorbis.stb_vorbis stb_vorbis = StbVorbis.stb_vorbis_open_memory(mem, len, &num, null);
			if (stb_vorbis == null)
			{
				return -1;
			}
			int num2 = stb_vorbis.channels * 4096;
			*channels = stb_vorbis.channels;
			if (sample_rate != null)
			{
				*sample_rate = (int)stb_vorbis.sample_rate;
			}
			int num4;
			int num3 = (num4 = 0);
			int num5 = num2;
			short* ptr = (short*)CRuntime.malloc((ulong)((long)(num5 * 2)));
			if (ptr == null)
			{
				StbVorbis.stb_vorbis_close(stb_vorbis);
				return -2;
			}
			for (;;)
			{
				int num6 = StbVorbis.stb_vorbis_get_frame_short_interleaved(stb_vorbis, stb_vorbis.channels, ptr + num4, num5 - num4);
				if (num6 == 0)
				{
					goto IL_BE;
				}
				num3 += num6;
				num4 += num6 * stb_vorbis.channels;
				if (num4 + num2 > num5)
				{
					num5 *= 2;
					short* ptr2 = (short*)CRuntime.realloc((void*)ptr, (ulong)((long)(num5 * 2)));
					if (ptr2 == null)
					{
						break;
					}
					ptr = ptr2;
				}
			}
			CRuntime.free((void*)ptr);
			StbVorbis.stb_vorbis_close(stb_vorbis);
			return -2;
			IL_BE:
			output = ptr;
			StbVorbis.stb_vorbis_close(stb_vorbis);
			return num3;
		}

		// Token: 0x06000217 RID: 535 RVA: 0x000201B8 File Offset: 0x0001E3B8
		public unsafe static int stb_vorbis_get_samples_float_interleaved(StbVorbis.stb_vorbis f, int channels, float* buffer, int num_floats)
		{
			float*[] array = null;
			int num = num_floats / channels;
			int i = 0;
			int num2 = f.channels;
			if (num2 > channels)
			{
				num2 = channels;
			}
			while (i < num)
			{
				int num3 = f.channel_buffer_end - f.channel_buffer_start;
				if (i + num3 >= num)
				{
					num3 = num - i;
				}
				for (int j = 0; j < num3; j++)
				{
					int k;
					for (k = 0; k < num2; k++)
					{
						*(buffer++) = f.channel_buffers[k][f.channel_buffer_start + j];
					}
					while (k < channels)
					{
						*(buffer++) = 0f;
						k++;
					}
				}
				i += num3;
				f.channel_buffer_start += num3;
				if (i == num || StbVorbis.stb_vorbis_get_frame_float(f, null, ref array) == 0)
				{
					break;
				}
			}
			return i;
		}

		// Token: 0x06000218 RID: 536 RVA: 0x00020284 File Offset: 0x0001E484
		public unsafe static int stb_vorbis_get_samples_float(StbVorbis.stb_vorbis f, int channels, float** buffer, int num_samples)
		{
			float*[] array = null;
			int i = 0;
			int num = f.channels;
			if (num > channels)
			{
				num = channels;
			}
			while (i < num_samples)
			{
				int num2 = f.channel_buffer_end - f.channel_buffer_start;
				if (i + num2 >= num_samples)
				{
					num2 = num_samples - i;
				}
				if (num2 != 0)
				{
					int j;
					for (j = 0; j < num; j++)
					{
						CRuntime.memcpy((void*)(*(IntPtr*)(buffer + (IntPtr)j * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) + (byte*)((IntPtr)i * 4)), (void*)(f.channel_buffers[j] + f.channel_buffer_start), (ulong)((long)(4 * num2)));
					}
					while (j < channels)
					{
						CRuntime.memset((void*)(*(IntPtr*)(buffer + (IntPtr)j * (IntPtr)sizeof(float*) / (IntPtr)sizeof(float*)) + (byte*)((IntPtr)i * 4)), 0, (ulong)((long)(4 * num2)));
						j++;
					}
				}
				i += num2;
				f.channel_buffer_start += num2;
				if (i == num_samples || StbVorbis.stb_vorbis_get_frame_float(f, null, ref array) == 0)
				{
					break;
				}
			}
			return i;
		}

		// Token: 0x040000A7 RID: 167
		public static sbyte[,] channel_position = new sbyte[,]
		{
			{ 0, 0, 0, 0, 0, 0 },
			{ 7, 0, 0, 0, 0, 0 },
			{ 3, 5, 0, 0, 0, 0 },
			{ 3, 7, 5, 0, 0, 0 },
			{ 3, 5, 3, 5, 0, 0 },
			{ 3, 7, 5, 3, 5, 0 },
			{ 3, 7, 5, 3, 5, 7 }
		};

		// Token: 0x040000A8 RID: 168
		public const int VORBIS__no_error = 0;

		// Token: 0x040000A9 RID: 169
		public const int VORBIS_need_more_data = 1;

		// Token: 0x040000AA RID: 170
		public const int VORBIS_invalid_api_mixing = 2;

		// Token: 0x040000AB RID: 171
		public const int VORBIS_outofmem = 3;

		// Token: 0x040000AC RID: 172
		public const int VORBIS_feature_not_supported = 4;

		// Token: 0x040000AD RID: 173
		public const int VORBIS_too_many_channels = 5;

		// Token: 0x040000AE RID: 174
		public const int VORBIS_file_open_failure = 6;

		// Token: 0x040000AF RID: 175
		public const int VORBIS_seek_without_length = 7;

		// Token: 0x040000B0 RID: 176
		public const int VORBIS_unexpected_eof = 10;

		// Token: 0x040000B1 RID: 177
		public const int VORBIS_seek_invalid = 11;

		// Token: 0x040000B2 RID: 178
		public const int VORBIS_invalid_setup = 20;

		// Token: 0x040000B3 RID: 179
		public const int VORBIS_invalid_stream = 21;

		// Token: 0x040000B4 RID: 180
		public const int VORBIS_missing_capture_pattern = 30;

		// Token: 0x040000B5 RID: 181
		public const int VORBIS_invalid_stream_structure_version = 31;

		// Token: 0x040000B6 RID: 182
		public const int VORBIS_continued_packet_flag_invalid = 32;

		// Token: 0x040000B7 RID: 183
		public const int VORBIS_incorrect_stream_serial_number = 33;

		// Token: 0x040000B8 RID: 184
		public const int VORBIS_invalid_first_page = 34;

		// Token: 0x040000B9 RID: 185
		public const int VORBIS_bad_packet_type = 35;

		// Token: 0x040000BA RID: 186
		public const int VORBIS_cant_find_last_page = 36;

		// Token: 0x040000BB RID: 187
		public const int VORBIS_seek_failed = 37;

		// Token: 0x040000BC RID: 188
		public const int VORBIS_packet_id = 1;

		// Token: 0x040000BD RID: 189
		public const int VORBIS_packet_comment = 3;

		// Token: 0x040000BE RID: 190
		public const int VORBIS_packet_setup = 5;

		// Token: 0x040000BF RID: 191
		public static uint[] _crc_table = new uint[256];

		// Token: 0x040000C0 RID: 192
		public static sbyte[] log2_4 = new sbyte[]
		{
			0, 1, 2, 2, 3, 3, 3, 3, 4, 4,
			4, 4, 4, 4, 4, 4
		};

		// Token: 0x040000C1 RID: 193
		public static byte[] ogg_page_header = new byte[] { 79, 103, 103, 83 };

		// Token: 0x040000C2 RID: 194
		public static float[] inverse_db_table = new float[]
		{
			1.0649863E-07f, 1.1341951E-07f, 1.2079015E-07f, 1.2863978E-07f, 1.369995E-07f, 1.459025E-07f, 1.5538409E-07f, 1.6548181E-07f, 1.7623574E-07f, 1.8768856E-07f,
			1.998856E-07f, 2.128753E-07f, 2.2670913E-07f, 2.4144197E-07f, 2.5713223E-07f, 2.7384212E-07f, 2.9163792E-07f, 3.1059022E-07f, 3.307741E-07f, 3.5226967E-07f,
			3.7516213E-07f, 3.995423E-07f, 4.255068E-07f, 4.5315863E-07f, 4.8260745E-07f, 5.1397E-07f, 5.4737063E-07f, 5.829419E-07f, 6.208247E-07f, 6.611694E-07f,
			7.041359E-07f, 7.4989464E-07f, 7.98627E-07f, 8.505263E-07f, 9.057983E-07f, 9.646621E-07f, 1.0273513E-06f, 1.0941144E-06f, 1.1652161E-06f, 1.2409384E-06f,
			1.3215816E-06f, 1.4074654E-06f, 1.4989305E-06f, 1.5963394E-06f, 1.7000785E-06f, 1.8105592E-06f, 1.9282195E-06f, 2.053526E-06f, 2.1869757E-06f, 2.3290977E-06f,
			2.4804558E-06f, 2.6416496E-06f, 2.813319E-06f, 2.9961443E-06f, 3.1908505E-06f, 3.39821E-06f, 3.619045E-06f, 3.8542307E-06f, 4.1047006E-06f, 4.371447E-06f,
			4.6555283E-06f, 4.958071E-06f, 5.280274E-06f, 5.623416E-06f, 5.988857E-06f, 6.3780467E-06f, 6.7925284E-06f, 7.2339453E-06f, 7.704048E-06f, 8.2047E-06f,
			8.737888E-06f, 9.305725E-06f, 9.910464E-06f, 1.0554501E-05f, 1.1240392E-05f, 1.1970856E-05f, 1.2748789E-05f, 1.3577278E-05f, 1.4459606E-05f, 1.5399271E-05f,
			1.6400005E-05f, 1.7465769E-05f, 1.8600793E-05f, 1.9809577E-05f, 2.1096914E-05f, 2.2467912E-05f, 2.3928002E-05f, 2.5482977E-05f, 2.7139005E-05f, 2.890265E-05f,
			3.078091E-05f, 3.2781227E-05f, 3.4911533E-05f, 3.718028E-05f, 3.9596467E-05f, 4.2169668E-05f, 4.491009E-05f, 4.7828602E-05f, 5.0936775E-05f, 5.424693E-05f,
			5.7772202E-05f, 6.152657E-05f, 6.552491E-05f, 6.9783084E-05f, 7.4317984E-05f, 7.914758E-05f, 8.429104E-05f, 8.976875E-05f, 9.560242E-05f, 0.00010181521f,
			0.00010843174f, 0.00011547824f, 0.00012298267f, 0.00013097477f, 0.00013948625f, 0.00014855085f, 0.00015820454f, 0.00016848555f, 0.00017943469f, 0.00019109536f,
			0.00020351382f, 0.0002167393f, 0.00023082423f, 0.00024582449f, 0.00026179955f, 0.00027881275f, 0.00029693157f, 0.00031622787f, 0.00033677815f, 0.00035866388f,
			0.00038197188f, 0.00040679457f, 0.00043323037f, 0.0004613841f, 0.0004913675f, 0.00052329927f, 0.0005573062f, 0.0005935231f, 0.0006320936f, 0.0006731706f,
			0.000716917f, 0.0007635063f, 0.00081312325f, 0.00086596457f, 0.00092223985f, 0.0009821722f, 0.0010459992f, 0.0011139743f, 0.0011863665f, 0.0012634633f,
			0.0013455702f, 0.0014330129f, 0.0015261382f, 0.0016253153f, 0.0017309374f, 0.0018434235f, 0.0019632196f, 0.0020908006f, 0.0022266726f, 0.0023713743f,
			0.0025254795f, 0.0026895993f, 0.0028643848f, 0.0030505287f, 0.003248769f, 0.0034598925f, 0.0036847359f, 0.0039241905f, 0.0041792067f, 0.004450795f,
			0.004740033f, 0.005048067f, 0.0053761187f, 0.005725489f, 0.0060975635f, 0.0064938175f, 0.0069158226f, 0.0073652514f, 0.007843887f, 0.008353627f,
			0.008896492f, 0.009474637f, 0.010090352f, 0.01074608f, 0.011444421f, 0.012188144f, 0.012980198f, 0.013823725f, 0.014722068f, 0.015678791f,
			0.016697686f, 0.017782796f, 0.018938422f, 0.020169148f, 0.021479854f, 0.022875736f, 0.02436233f, 0.025945531f, 0.027631618f, 0.029427277f,
			0.031339627f, 0.03337625f, 0.035545226f, 0.037855156f, 0.0403152f, 0.042935107f, 0.045725275f, 0.048696756f, 0.05186135f, 0.05523159f,
			0.05882085f, 0.062643364f, 0.06671428f, 0.07104975f, 0.075666964f, 0.08058423f, 0.08582105f, 0.09139818f, 0.097337745f, 0.1036633f,
			0.11039993f, 0.11757434f, 0.12521498f, 0.13335215f, 0.14201812f, 0.15124726f, 0.16107617f, 0.1715438f, 0.18269168f, 0.19456401f,
			0.20720787f, 0.22067343f, 0.23501402f, 0.25028655f, 0.26655158f, 0.28387362f, 0.3023213f, 0.32196787f, 0.34289113f, 0.36517414f,
			0.3889052f, 0.41417846f, 0.44109413f, 0.4697589f, 0.50028646f, 0.53279793f, 0.5674221f, 0.6042964f, 0.64356697f, 0.6853896f,
			0.72993004f, 0.777365f, 0.8278826f, 0.88168305f, 0.9389798f, 1f
		};

		// Token: 0x040000C3 RID: 195
		public static int[,] channel_selector = new int[,]
		{
			{ 0, 0 },
			{ 1, 0 },
			{ 2, 4 }
		};

		// Token: 0x0200003E RID: 62
		public class Residue
		{
			// Token: 0x04000237 RID: 567
			public uint begin;

			// Token: 0x04000238 RID: 568
			public uint end;

			// Token: 0x04000239 RID: 569
			public uint part_size;

			// Token: 0x0400023A RID: 570
			public byte classifications;

			// Token: 0x0400023B RID: 571
			public byte classbook;

			// Token: 0x0400023C RID: 572
			public unsafe byte** classdata;

			// Token: 0x0400023D RID: 573
			public short[,] residue_books;
		}

		// Token: 0x0200003F RID: 63
		public class stb_vorbis
		{
			// Token: 0x0600026C RID: 620 RVA: 0x00020990 File Offset: 0x0001EB90
			public unsafe stb_vorbis()
			{
			}

			// Token: 0x0400023E RID: 574
			public uint sample_rate;

			// Token: 0x0400023F RID: 575
			public int channels;

			// Token: 0x04000240 RID: 576
			public uint setup_memory_required;

			// Token: 0x04000241 RID: 577
			public uint temp_memory_required;

			// Token: 0x04000242 RID: 578
			public uint setup_temp_memory_required;

			// Token: 0x04000243 RID: 579
			public unsafe byte* stream;

			// Token: 0x04000244 RID: 580
			public unsafe byte* stream_start;

			// Token: 0x04000245 RID: 581
			public unsafe byte* stream_end;

			// Token: 0x04000246 RID: 582
			public uint stream_len;

			// Token: 0x04000247 RID: 583
			public byte push_mode;

			// Token: 0x04000248 RID: 584
			public uint first_audio_page_offset;

			// Token: 0x04000249 RID: 585
			public StbVorbis.ProbedPage p_first;

			// Token: 0x0400024A RID: 586
			public StbVorbis.ProbedPage p_last;

			// Token: 0x0400024B RID: 587
			public StbVorbis.stb_vorbis_alloc alloc;

			// Token: 0x0400024C RID: 588
			public int setup_offset;

			// Token: 0x0400024D RID: 589
			public int temp_offset;

			// Token: 0x0400024E RID: 590
			public int eof;

			// Token: 0x0400024F RID: 591
			public int error;

			// Token: 0x04000250 RID: 592
			public int[] blocksize = new int[2];

			// Token: 0x04000251 RID: 593
			public int blocksize_0;

			// Token: 0x04000252 RID: 594
			public int blocksize_1;

			// Token: 0x04000253 RID: 595
			public int codebook_count;

			// Token: 0x04000254 RID: 596
			public unsafe StbVorbis.Codebook* codebooks;

			// Token: 0x04000255 RID: 597
			public int floor_count;

			// Token: 0x04000256 RID: 598
			public ushort[] floor_types = new ushort[64];

			// Token: 0x04000257 RID: 599
			public unsafe StbVorbis.Floor* floor_config;

			// Token: 0x04000258 RID: 600
			public int residue_count;

			// Token: 0x04000259 RID: 601
			public ushort[] residue_types = new ushort[64];

			// Token: 0x0400025A RID: 602
			public StbVorbis.Residue[] residue_config;

			// Token: 0x0400025B RID: 603
			public int mapping_count;

			// Token: 0x0400025C RID: 604
			public unsafe StbVorbis.Mapping* mapping;

			// Token: 0x0400025D RID: 605
			public int mode_count;

			// Token: 0x0400025E RID: 606
			public unsafe StbVorbis.Mode* mode_config = (StbVorbis.Mode*)CRuntime.malloc((long)(64 * sizeof(StbVorbis.Mode)));

			// Token: 0x0400025F RID: 607
			public uint total_samples;

			// Token: 0x04000260 RID: 608
			public unsafe float*[] channel_buffers = new float*[16];

			// Token: 0x04000261 RID: 609
			public unsafe float*[] outputs = new float*[16];

			// Token: 0x04000262 RID: 610
			public unsafe float*[] previous_window = new float*[16];

			// Token: 0x04000263 RID: 611
			public int previous_length;

			// Token: 0x04000264 RID: 612
			public unsafe short*[] finalY = new short*[16];

			// Token: 0x04000265 RID: 613
			public uint current_loc;

			// Token: 0x04000266 RID: 614
			public int current_loc_valid;

			// Token: 0x04000267 RID: 615
			public unsafe float*[] A = new float*[2];

			// Token: 0x04000268 RID: 616
			public unsafe float*[] B = new float*[2];

			// Token: 0x04000269 RID: 617
			public unsafe float*[] C = new float*[2];

			// Token: 0x0400026A RID: 618
			public unsafe float*[] window = new float*[2];

			// Token: 0x0400026B RID: 619
			public unsafe ushort*[] bit_reverse = new ushort*[2];

			// Token: 0x0400026C RID: 620
			public uint serial;

			// Token: 0x0400026D RID: 621
			public int last_page;

			// Token: 0x0400026E RID: 622
			public int segment_count;

			// Token: 0x0400026F RID: 623
			public unsafe byte* segments = (byte*)CRuntime.malloc(255L);

			// Token: 0x04000270 RID: 624
			public byte page_flag;

			// Token: 0x04000271 RID: 625
			public byte bytes_in_seg;

			// Token: 0x04000272 RID: 626
			public byte first_decode;

			// Token: 0x04000273 RID: 627
			public int next_seg;

			// Token: 0x04000274 RID: 628
			public int last_seg;

			// Token: 0x04000275 RID: 629
			public int last_seg_which;

			// Token: 0x04000276 RID: 630
			public uint acc;

			// Token: 0x04000277 RID: 631
			public int valid_bits;

			// Token: 0x04000278 RID: 632
			public int packet_bytes;

			// Token: 0x04000279 RID: 633
			public int end_seg_with_known_loc;

			// Token: 0x0400027A RID: 634
			public uint known_loc_for_packet;

			// Token: 0x0400027B RID: 635
			public int discard_samples_deferred;

			// Token: 0x0400027C RID: 636
			public uint samples_output;

			// Token: 0x0400027D RID: 637
			public int page_crc_tests;

			// Token: 0x0400027E RID: 638
			public StbVorbis.CRCscan[] scan = new StbVorbis.CRCscan[4];

			// Token: 0x0400027F RID: 639
			public int channel_buffer_start;

			// Token: 0x04000280 RID: 640
			public int channel_buffer_end;
		}

		// Token: 0x02000040 RID: 64
		public struct stb_vorbis_alloc
		{
			// Token: 0x04000281 RID: 641
			public unsafe sbyte* alloc_buffer;

			// Token: 0x04000282 RID: 642
			public int alloc_buffer_length_in_bytes;
		}

		// Token: 0x02000041 RID: 65
		public struct stb_vorbis_info
		{
			// Token: 0x04000283 RID: 643
			public uint sample_rate;

			// Token: 0x04000284 RID: 644
			public int channels;

			// Token: 0x04000285 RID: 645
			public uint setup_memory_required;

			// Token: 0x04000286 RID: 646
			public uint setup_temp_memory_required;

			// Token: 0x04000287 RID: 647
			public uint temp_memory_required;

			// Token: 0x04000288 RID: 648
			public int max_frame_size;
		}

		// Token: 0x02000042 RID: 66
		public struct Codebook
		{
			// Token: 0x04000289 RID: 649
			public int dimensions;

			// Token: 0x0400028A RID: 650
			public int entries;

			// Token: 0x0400028B RID: 651
			public unsafe byte* codeword_lengths;

			// Token: 0x0400028C RID: 652
			public float minimum_value;

			// Token: 0x0400028D RID: 653
			public float delta_value;

			// Token: 0x0400028E RID: 654
			public byte value_bits;

			// Token: 0x0400028F RID: 655
			public byte lookup_type;

			// Token: 0x04000290 RID: 656
			public byte sequence_p;

			// Token: 0x04000291 RID: 657
			public byte sparse;

			// Token: 0x04000292 RID: 658
			public uint lookup_values;

			// Token: 0x04000293 RID: 659
			public unsafe float* multiplicands;

			// Token: 0x04000294 RID: 660
			public unsafe uint* codewords;

			// Token: 0x04000295 RID: 661
			[FixedBuffer(typeof(short), 1024)]
			public StbVorbis.Codebook.<fast_huffman>e__FixedBuffer fast_huffman;

			// Token: 0x04000296 RID: 662
			public unsafe uint* sorted_codewords;

			// Token: 0x04000297 RID: 663
			public unsafe int* sorted_values;

			// Token: 0x04000298 RID: 664
			public int sorted_entries;

			// Token: 0x0200006E RID: 110
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 2048)]
			public struct <fast_huffman>e__FixedBuffer
			{
				// Token: 0x040002CA RID: 714
				public short FixedElementField;
			}
		}

		// Token: 0x02000043 RID: 67
		public struct Floor0
		{
			// Token: 0x04000299 RID: 665
			public byte order;

			// Token: 0x0400029A RID: 666
			public ushort rate;

			// Token: 0x0400029B RID: 667
			public ushort bark_map_size;

			// Token: 0x0400029C RID: 668
			public byte amplitude_bits;

			// Token: 0x0400029D RID: 669
			public byte amplitude_offset;

			// Token: 0x0400029E RID: 670
			public byte number_of_books;

			// Token: 0x0400029F RID: 671
			[FixedBuffer(typeof(byte), 16)]
			public StbVorbis.Floor0.<book_list>e__FixedBuffer book_list;

			// Token: 0x0200006F RID: 111
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 16)]
			public struct <book_list>e__FixedBuffer
			{
				// Token: 0x040002CB RID: 715
				public byte FixedElementField;
			}
		}

		// Token: 0x02000044 RID: 68
		public struct Floor1
		{
			// Token: 0x040002A0 RID: 672
			public byte partitions;

			// Token: 0x040002A1 RID: 673
			[FixedBuffer(typeof(byte), 32)]
			public StbVorbis.Floor1.<partition_class_list>e__FixedBuffer partition_class_list;

			// Token: 0x040002A2 RID: 674
			[FixedBuffer(typeof(byte), 16)]
			public StbVorbis.Floor1.<class_dimensions>e__FixedBuffer class_dimensions;

			// Token: 0x040002A3 RID: 675
			[FixedBuffer(typeof(byte), 16)]
			public StbVorbis.Floor1.<class_subclasses>e__FixedBuffer class_subclasses;

			// Token: 0x040002A4 RID: 676
			[FixedBuffer(typeof(byte), 16)]
			public StbVorbis.Floor1.<class_masterbooks>e__FixedBuffer class_masterbooks;

			// Token: 0x040002A5 RID: 677
			[FixedBuffer(typeof(short), 128)]
			public StbVorbis.Floor1.<subclass_books>e__FixedBuffer subclass_books;

			// Token: 0x040002A6 RID: 678
			[FixedBuffer(typeof(ushort), 250)]
			public StbVorbis.Floor1.<Xlist>e__FixedBuffer Xlist;

			// Token: 0x040002A7 RID: 679
			[FixedBuffer(typeof(byte), 250)]
			public StbVorbis.Floor1.<sorted_order>e__FixedBuffer sorted_order;

			// Token: 0x040002A8 RID: 680
			[FixedBuffer(typeof(byte), 500)]
			public StbVorbis.Floor1.<neighbors>e__FixedBuffer neighbors;

			// Token: 0x040002A9 RID: 681
			public byte floor1_multiplier;

			// Token: 0x040002AA RID: 682
			public byte rangebits;

			// Token: 0x040002AB RID: 683
			public int values;

			// Token: 0x02000070 RID: 112
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 250)]
			public struct <sorted_order>e__FixedBuffer
			{
				// Token: 0x040002CC RID: 716
				public byte FixedElementField;
			}

			// Token: 0x02000071 RID: 113
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 500)]
			public struct <Xlist>e__FixedBuffer
			{
				// Token: 0x040002CD RID: 717
				public ushort FixedElementField;
			}

			// Token: 0x02000072 RID: 114
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 32)]
			public struct <partition_class_list>e__FixedBuffer
			{
				// Token: 0x040002CE RID: 718
				public byte FixedElementField;
			}

			// Token: 0x02000073 RID: 115
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 16)]
			public struct <class_dimensions>e__FixedBuffer
			{
				// Token: 0x040002CF RID: 719
				public byte FixedElementField;
			}

			// Token: 0x02000074 RID: 116
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 16)]
			public struct <class_subclasses>e__FixedBuffer
			{
				// Token: 0x040002D0 RID: 720
				public byte FixedElementField;
			}

			// Token: 0x02000075 RID: 117
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 16)]
			public struct <class_masterbooks>e__FixedBuffer
			{
				// Token: 0x040002D1 RID: 721
				public byte FixedElementField;
			}

			// Token: 0x02000076 RID: 118
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 256)]
			public struct <subclass_books>e__FixedBuffer
			{
				// Token: 0x040002D2 RID: 722
				public short FixedElementField;
			}

			// Token: 0x02000077 RID: 119
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 500)]
			public struct <neighbors>e__FixedBuffer
			{
				// Token: 0x040002D3 RID: 723
				public byte FixedElementField;
			}
		}

		// Token: 0x02000045 RID: 69
		public struct Floor
		{
			// Token: 0x040002AC RID: 684
			public StbVorbis.Floor0 floor0;

			// Token: 0x040002AD RID: 685
			public StbVorbis.Floor1 floor1;
		}

		// Token: 0x02000046 RID: 70
		public struct MappingChannel
		{
			// Token: 0x040002AE RID: 686
			public byte magnitude;

			// Token: 0x040002AF RID: 687
			public byte angle;

			// Token: 0x040002B0 RID: 688
			public byte mux;
		}

		// Token: 0x02000047 RID: 71
		public struct Mapping
		{
			// Token: 0x040002B1 RID: 689
			public ushort coupling_steps;

			// Token: 0x040002B2 RID: 690
			public unsafe StbVorbis.MappingChannel* chan;

			// Token: 0x040002B3 RID: 691
			public byte submaps;

			// Token: 0x040002B4 RID: 692
			[FixedBuffer(typeof(byte), 15)]
			public StbVorbis.Mapping.<submap_floor>e__FixedBuffer submap_floor;

			// Token: 0x040002B5 RID: 693
			[FixedBuffer(typeof(byte), 15)]
			public StbVorbis.Mapping.<submap_residue>e__FixedBuffer submap_residue;

			// Token: 0x02000078 RID: 120
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 15)]
			public struct <submap_floor>e__FixedBuffer
			{
				// Token: 0x040002D4 RID: 724
				public byte FixedElementField;
			}

			// Token: 0x02000079 RID: 121
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 15)]
			public struct <submap_residue>e__FixedBuffer
			{
				// Token: 0x040002D5 RID: 725
				public byte FixedElementField;
			}
		}

		// Token: 0x02000048 RID: 72
		public struct Mode
		{
			// Token: 0x040002B6 RID: 694
			public byte blockflag;

			// Token: 0x040002B7 RID: 695
			public byte mapping;

			// Token: 0x040002B8 RID: 696
			public ushort windowtype;

			// Token: 0x040002B9 RID: 697
			public ushort transformtype;
		}

		// Token: 0x02000049 RID: 73
		public struct CRCscan
		{
			// Token: 0x040002BA RID: 698
			public uint goal_crc;

			// Token: 0x040002BB RID: 699
			public int bytes_left;

			// Token: 0x040002BC RID: 700
			public uint crc_so_far;

			// Token: 0x040002BD RID: 701
			public int bytes_done;

			// Token: 0x040002BE RID: 702
			public uint sample_loc;
		}

		// Token: 0x0200004A RID: 74
		public struct ProbedPage
		{
			// Token: 0x040002BF RID: 703
			public uint page_start;

			// Token: 0x040002C0 RID: 704
			public uint page_end;

			// Token: 0x040002C1 RID: 705
			public uint last_decoded_sample;
		}

		// Token: 0x0200004B RID: 75
		public struct stbv__floor_ordering
		{
			// Token: 0x040002C2 RID: 706
			public ushort x;

			// Token: 0x040002C3 RID: 707
			public ushort id;
		}
	}
}
