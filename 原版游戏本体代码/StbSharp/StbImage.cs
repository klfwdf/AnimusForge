using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x0200000A RID: 10
	public static class StbImage
	{
		// Token: 0x0600009E RID: 158 RVA: 0x00009644 File Offset: 0x00007844
		private unsafe static void* stbi__malloc(int size)
		{
			return CRuntime.malloc((ulong)((long)size));
		}

		// Token: 0x0600009F RID: 159 RVA: 0x0000964D File Offset: 0x0000784D
		private unsafe static void* stbi__malloc(ulong size)
		{
			return StbImage.stbi__malloc((int)size);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00009656 File Offset: 0x00007856
		private static int stbi__err(string str)
		{
			StbImage.LastError = str;
			return 0;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00009660 File Offset: 0x00007860
		public unsafe static void stbi__gif_parse_colortable(StbImage.stbi__context s, byte* pal, int num_entries, int transp)
		{
			for (int i = 0; i < num_entries; i++)
			{
				pal[i * 4 + 2] = StbImage.stbi__get8(s);
				pal[i * 4 + 1] = StbImage.stbi__get8(s);
				pal[i * 4] = StbImage.stbi__get8(s);
				pal[i * 4 + 3] = ((transp == i) ? 0 : byte.MaxValue);
			}
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x000096B8 File Offset: 0x000078B8
		public unsafe static Image LoadFromMemory(byte[] bytes, int req_comp = 0)
		{
			byte* ptr = null;
			Image image;
			try
			{
				int num;
				int num2;
				int num3;
				try
				{
					fixed (byte[] array = bytes)
					{
						byte* buffer;
						if (bytes == null || array.Length == 0)
						{
							buffer = null;
						}
						else
						{
							buffer = &array[0];
						}
						ptr = StbImage.stbi_load_from_memory(buffer, bytes.Length, &num, &num2, &num3, req_comp);
					}
				}
				finally
				{
					byte[] array = null;
				}
				if (ptr == null)
				{
					throw new InvalidOperationException(StbImage.LastError);
				}
				image = new Image
				{
					Width = num,
					Height = num2,
					SourceComp = num3,
					Comp = ((req_comp == 0) ? num3 : req_comp)
				};
				image.Data = new byte[num * num2 * image.Comp];
				Marshal.Copy(new IntPtr((void*)ptr), image.Data, 0, image.Data.Length);
			}
			finally
			{
				if (ptr != null)
				{
					CRuntime.free((void*)ptr);
				}
			}
			return image;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00009794 File Offset: 0x00007994
		public unsafe static void stbi__start_mem(StbImage.stbi__context s, byte* buffer, int len)
		{
			s.io.read = null;
			s.read_from_callbacks = 0;
			s.img_buffer_original = buffer;
			s.img_buffer = buffer;
			s.img_buffer_end = (s.img_buffer_original_end = buffer + len);
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x000097D6 File Offset: 0x000079D6
		public unsafe static void stbi__start_callbacks(StbImage.stbi__context s, StbImage.stbi_io_callbacks c, void* user)
		{
			s.io = c;
			s.io_user_data = user;
			s.buflen = 128;
			s.read_from_callbacks = 1;
			s.img_buffer_original = s.buffer_start;
			StbImage.stbi__refill_buffer(s);
			s.img_buffer_original_end = s.img_buffer_end;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00009816 File Offset: 0x00007A16
		public static void stbi__rewind(StbImage.stbi__context s)
		{
			s.img_buffer = s.img_buffer_original;
			s.img_buffer_end = s.img_buffer_original_end;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00009830 File Offset: 0x00007A30
		public static int stbi__addsizes_valid(int a, int b)
		{
			if (b < 0)
			{
				return 0;
			}
			if (a > 2147483647 - b)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00009845 File Offset: 0x00007A45
		public static int stbi__mul2sizes_valid(int a, int b)
		{
			if (a < 0 || b < 0)
			{
				return 0;
			}
			if (b == 0)
			{
				return 1;
			}
			if (a > 2147483647 / b)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00009863 File Offset: 0x00007A63
		public static int stbi__mad2sizes_valid(int a, int b, int add)
		{
			if (StbImage.stbi__mul2sizes_valid(a, b) == 0 || StbImage.stbi__addsizes_valid(a * b, add) == 0)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x0000987C File Offset: 0x00007A7C
		public static int stbi__mad3sizes_valid(int a, int b, int c, int add)
		{
			if (StbImage.stbi__mul2sizes_valid(a, b) == 0 || StbImage.stbi__mul2sizes_valid(a * b, c) == 0 || StbImage.stbi__addsizes_valid(a * b * c, add) == 0)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x000098A2 File Offset: 0x00007AA2
		public static int stbi__mad4sizes_valid(int a, int b, int c, int d, int add)
		{
			if (StbImage.stbi__mul2sizes_valid(a, b) == 0 || StbImage.stbi__mul2sizes_valid(a * b, c) == 0 || StbImage.stbi__mul2sizes_valid(a * b * c, d) == 0 || StbImage.stbi__addsizes_valid(a * b * c * d, add) == 0)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x000098D8 File Offset: 0x00007AD8
		public unsafe static void* stbi__malloc_mad2(int a, int b, int add)
		{
			if (StbImage.stbi__mad2sizes_valid(a, b, add) == 0)
			{
				return null;
			}
			return StbImage.stbi__malloc((ulong)((long)(a * b + add)));
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000098F2 File Offset: 0x00007AF2
		public unsafe static void* stbi__malloc_mad3(int a, int b, int c, int add)
		{
			if (StbImage.stbi__mad3sizes_valid(a, b, c, add) == 0)
			{
				return null;
			}
			return StbImage.stbi__malloc((ulong)((long)(a * b * c + add)));
		}

		// Token: 0x060000AD RID: 173 RVA: 0x0000990F File Offset: 0x00007B0F
		public unsafe static void* stbi__malloc_mad4(int a, int b, int c, int d, int add)
		{
			if (StbImage.stbi__mad4sizes_valid(a, b, c, d, add) == 0)
			{
				return null;
			}
			return StbImage.stbi__malloc((ulong)((long)(a * b * c * d + add)));
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00009931 File Offset: 0x00007B31
		public static void stbi_set_flip_vertically_on_load(int flag_true_if_should_flip)
		{
			StbImage.stbi__vertically_flip_on_load = flag_true_if_should_flip;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x0000993C File Offset: 0x00007B3C
		public unsafe static void* stbi__load_main(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri, int bpc)
		{
			ri->bits_per_channel = 8;
			ri->channel_order = 0;
			ri->num_channels = 0;
			if (StbImage.stbi__jpeg_test(s) != 0)
			{
				return StbImage.stbi__jpeg_load(s, x, y, comp, req_comp, ri);
			}
			if (StbImage.stbi__png_test(s) != 0)
			{
				return StbImage.stbi__png_load(s, x, y, comp, req_comp, ri);
			}
			if (StbImage.stbi__bmp_test(s) != 0)
			{
				return StbImage.stbi__bmp_load(s, x, y, comp, req_comp, ri);
			}
			if (StbImage.stbi__gif_test(s) != 0)
			{
				return StbImage.stbi__gif_load(s, x, y, comp, req_comp, ri);
			}
			if (StbImage.stbi__psd_test(s) != 0)
			{
				return StbImage.stbi__psd_load(s, x, y, comp, req_comp, ri, bpc);
			}
			if (StbImage.stbi__tga_test(s) != 0)
			{
				return StbImage.stbi__tga_load(s, x, y, comp, req_comp, ri);
			}
			return (StbImage.stbi__err("unknown image type") != 0) ? null : null;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x000099FC File Offset: 0x00007BFC
		public unsafe static byte* stbi__convert_16_to_8(ushort* orig, int w, int h, int channels)
		{
			int num = w * h * channels;
			byte* ptr = (byte*)StbImage.stbi__malloc((ulong)((long)num));
			if (ptr == null)
			{
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			for (int i = 0; i < num; i++)
			{
				ptr[i] = (byte)((orig[i] >> 8) & 255);
			}
			CRuntime.free((void*)orig);
			return ptr;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00009A58 File Offset: 0x00007C58
		public unsafe static ushort* stbi__convert_8_to_16(byte* orig, int w, int h, int channels)
		{
			int num = w * h * channels;
			ushort* ptr = (ushort*)StbImage.stbi__malloc((ulong)((long)(num * 2)));
			if (ptr == null)
			{
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			for (int i = 0; i < num; i++)
			{
				ptr[i] = (ushort)(((int)orig[i] << 8) + (int)orig[i]);
			}
			CRuntime.free((void*)orig);
			return ptr;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00009AB8 File Offset: 0x00007CB8
		public unsafe static void stbi__vertical_flip(void* image, int w, int h, int bytes_per_pixel)
		{
			ulong num = (ulong)((long)(w * bytes_per_pixel));
			byte* ptr = stackalloc byte[(UIntPtr)2048];
			for (int i = 0; i < h >> 1; i++)
			{
				byte* ptr2 = (byte*)image + (long)i * (long)num;
				byte* ptr3 = (byte*)image + (long)(h - i - 1) * (long)num;
				ulong num3;
				for (ulong num2 = num; num2 != 0UL; num2 -= num3)
				{
					num3 = ((num2 < 2048UL) ? num2 : 2048UL);
					CRuntime.memcpy((void*)ptr, (void*)ptr2, num3);
					CRuntime.memcpy((void*)ptr2, (void*)ptr3, num3);
					CRuntime.memcpy((void*)ptr3, (void*)ptr, num3);
					ptr2 += num3;
					ptr3 += num3;
				}
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00009B50 File Offset: 0x00007D50
		public unsafe static byte* stbi__load_and_postprocess_8bit(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp)
		{
			StbImage.stbi__result_info stbi__result_info = default(StbImage.stbi__result_info);
			void* ptr = StbImage.stbi__load_main(s, x, y, comp, req_comp, &stbi__result_info, 8);
			if (ptr == null)
			{
				return null;
			}
			if (stbi__result_info.bits_per_channel != 8)
			{
				ptr = (void*)StbImage.stbi__convert_16_to_8((ushort*)ptr, *x, *y, (req_comp == 0) ? (*comp) : req_comp);
				stbi__result_info.bits_per_channel = 8;
			}
			if (StbImage.stbi__vertically_flip_on_load != 0)
			{
				int bytes_per_pixel = ((req_comp != 0) ? req_comp : (*comp));
				StbImage.stbi__vertical_flip(ptr, *x, *y, bytes_per_pixel);
			}
			return (byte*)ptr;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00009BC4 File Offset: 0x00007DC4
		public unsafe static ushort* stbi__load_and_postprocess_16bit(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp)
		{
			StbImage.stbi__result_info stbi__result_info = default(StbImage.stbi__result_info);
			void* ptr = StbImage.stbi__load_main(s, x, y, comp, req_comp, &stbi__result_info, 16);
			if (ptr == null)
			{
				return null;
			}
			if (stbi__result_info.bits_per_channel != 16)
			{
				ptr = (void*)StbImage.stbi__convert_8_to_16((byte*)ptr, *x, *y, (req_comp == 0) ? (*comp) : req_comp);
				stbi__result_info.bits_per_channel = 16;
			}
			if (StbImage.stbi__vertically_flip_on_load != 0)
			{
				int num = ((req_comp != 0) ? req_comp : (*comp));
				StbImage.stbi__vertical_flip(ptr, *x, *y, num * 2);
			}
			return (ushort*)ptr;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00009C3A File Offset: 0x00007E3A
		public unsafe static ushort* stbi_load_16_from_memory(byte* buffer, int len, int* x, int* y, int* channels_in_file, int desired_channels)
		{
			StbImage.stbi__context s = new StbImage.stbi__context();
			StbImage.stbi__start_mem(s, buffer, len);
			return StbImage.stbi__load_and_postprocess_16bit(s, x, y, channels_in_file, desired_channels);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00009C54 File Offset: 0x00007E54
		public unsafe static ushort* stbi_load_16_from_callbacks(StbImage.stbi_io_callbacks clbk, void* user, int* x, int* y, int* channels_in_file, int desired_channels)
		{
			StbImage.stbi__context s = new StbImage.stbi__context();
			StbImage.stbi__start_callbacks(s, clbk, user);
			return StbImage.stbi__load_and_postprocess_16bit(s, x, y, channels_in_file, desired_channels);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00009C6E File Offset: 0x00007E6E
		public unsafe static byte* stbi_load_from_memory(byte* buffer, int len, int* x, int* y, int* comp, int req_comp)
		{
			StbImage.stbi__context s = new StbImage.stbi__context();
			StbImage.stbi__start_mem(s, buffer, len);
			return StbImage.stbi__load_and_postprocess_8bit(s, x, y, comp, req_comp);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00009C88 File Offset: 0x00007E88
		public unsafe static byte* stbi_load_from_callbacks(StbImage.stbi_io_callbacks clbk, void* user, int* x, int* y, int* comp, int req_comp)
		{
			StbImage.stbi__context s = new StbImage.stbi__context();
			StbImage.stbi__start_callbacks(s, clbk, user);
			return StbImage.stbi__load_and_postprocess_8bit(s, x, y, comp, req_comp);
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00009CA2 File Offset: 0x00007EA2
		public static void stbi_hdr_to_ldr_gamma(float gamma)
		{
			StbImage.stbi__h2l_gamma_i = 1f / gamma;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00009CB1 File Offset: 0x00007EB1
		public static void stbi_hdr_to_ldr_scale(float scale)
		{
			StbImage.stbi__h2l_scale_i = 1f / scale;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00009CC0 File Offset: 0x00007EC0
		public unsafe static void stbi__refill_buffer(StbImage.stbi__context s)
		{
			int num = s.io.read(s.io_user_data, (sbyte*)s.buffer_start, s.buflen);
			if (num == 0)
			{
				s.read_from_callbacks = 0;
				s.img_buffer = s.buffer_start;
				s.img_buffer_end = s.buffer_start;
				s.img_buffer_end++;
				*s.img_buffer = 0;
				return;
			}
			s.img_buffer = s.buffer_start;
			s.img_buffer_end = s.buffer_start;
			s.img_buffer_end += num;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00009D50 File Offset: 0x00007F50
		public unsafe static byte stbi__get8(StbImage.stbi__context s)
		{
			if (s.img_buffer < s.img_buffer_end)
			{
				byte* img_buffer = s.img_buffer;
				s.img_buffer = img_buffer + 1;
				return *img_buffer;
			}
			if (s.read_from_callbacks != 0)
			{
				StbImage.stbi__refill_buffer(s);
				byte* img_buffer = s.img_buffer;
				s.img_buffer = img_buffer + 1;
				return *img_buffer;
			}
			return 0;
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00009DA0 File Offset: 0x00007FA0
		public static int stbi__at_eof(StbImage.stbi__context s)
		{
			if (s.io.read != null)
			{
				if (s.io.eof(s.io_user_data) == 0)
				{
					return 0;
				}
				if (s.read_from_callbacks == 0)
				{
					return 1;
				}
			}
			if (s.img_buffer < s.img_buffer_end)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00009DF0 File Offset: 0x00007FF0
		public static void stbi__skip(StbImage.stbi__context s, int n)
		{
			if (n < 0)
			{
				s.img_buffer = s.img_buffer_end;
				return;
			}
			if (s.io.read != null)
			{
				int num = (int)((long)(s.img_buffer_end - s.img_buffer));
				if (num < n)
				{
					s.img_buffer = s.img_buffer_end;
					s.io.skip(s.io_user_data, n - num);
					return;
				}
			}
			s.img_buffer += n;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00009E68 File Offset: 0x00008068
		public unsafe static int stbi__getn(StbImage.stbi__context s, byte* buffer, int n)
		{
			if (s.io.read != null)
			{
				int num = (int)((long)(s.img_buffer_end - s.img_buffer));
				if (num < n)
				{
					CRuntime.memcpy((void*)buffer, (void*)s.img_buffer, (ulong)((long)num));
					int result = ((s.io.read(s.io_user_data, (sbyte*)(buffer + num), n - num) == n - num) ? 1 : 0);
					s.img_buffer = s.img_buffer_end;
					return result;
				}
			}
			if (s.img_buffer + n == s.img_buffer_end)
			{
				CRuntime.memcpy((void*)buffer, (void*)s.img_buffer, (ulong)((long)n));
				s.img_buffer += n;
				return 1;
			}
			return 0;
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00009F07 File Offset: 0x00008107
		public static int stbi__get16be(StbImage.stbi__context s)
		{
			return ((int)StbImage.stbi__get8(s) << 8) + (int)StbImage.stbi__get8(s);
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00009F18 File Offset: 0x00008118
		public static uint stbi__get32be(StbImage.stbi__context s)
		{
			return (uint)((ulong)((ulong)StbImage.stbi__get16be(s) << 16) + (ulong)((long)StbImage.stbi__get16be(s)));
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00009F2D File Offset: 0x0000812D
		public static int stbi__get16le(StbImage.stbi__context s)
		{
			return (int)StbImage.stbi__get8(s) + ((int)StbImage.stbi__get8(s) << 8);
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00009F3E File Offset: 0x0000813E
		public static uint stbi__get32le(StbImage.stbi__context s)
		{
			return (uint)((ulong)StbImage.stbi__get16le(s) + (ulong)((long)((long)StbImage.stbi__get16le(s) << 16)));
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00009F53 File Offset: 0x00008153
		public static byte stbi__compute_y(int r, int g, int b)
		{
			return (byte)(r * 77 + g * 150 + 29 * b >> 8);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00009F6C File Offset: 0x0000816C
		public unsafe static byte* stbi__convert_format(byte* data, int img_n, int req_comp, uint x, uint y)
		{
			if (req_comp == img_n)
			{
				return data;
			}
			byte* ptr = (byte*)StbImage.stbi__malloc_mad3(req_comp, (int)x, (int)y, 0);
			if (ptr == null)
			{
				CRuntime.free((void*)data);
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			int i = 0;
			while (i < (int)y)
			{
				byte* ptr2 = data + (long)i * (long)((ulong)x) * (long)img_n;
				byte* ptr3 = ptr + (long)i * (long)((ulong)x) * (long)req_comp;
				int num = img_n * 8 + req_comp;
				switch (num)
				{
				case 10:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = *ptr2;
						ptr3[1] = byte.MaxValue;
						j--;
						ptr2++;
						ptr3 += 2;
					}
					break;
				}
				case 11:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						j--;
						ptr2++;
						ptr3 += 3;
					}
					break;
				}
				case 12:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						ptr3[3] = byte.MaxValue;
						j--;
						ptr2++;
						ptr3 += 4;
					}
					break;
				}
				case 13:
				case 14:
				case 15:
				case 16:
				case 18:
					goto IL_33A;
				case 17:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = *ptr2;
						j--;
						ptr2 += 2;
						ptr3++;
					}
					break;
				}
				case 19:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						j--;
						ptr2 += 2;
						ptr3 += 3;
					}
					break;
				}
				case 20:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						ptr3[3] = ptr2[1];
						j--;
						ptr2 += 2;
						ptr3 += 4;
					}
					break;
				}
				default:
					switch (num)
					{
					case 25:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							j--;
							ptr2 += 3;
							ptr3++;
						}
						break;
					}
					case 26:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							ptr3[1] = byte.MaxValue;
							j--;
							ptr2 += 3;
							ptr3 += 2;
						}
						break;
					}
					case 27:
					case 29:
					case 30:
					case 31:
					case 32:
						goto IL_33A;
					case 28:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = *ptr2;
							ptr3[1] = ptr2[1];
							ptr3[2] = ptr2[2];
							ptr3[3] = byte.MaxValue;
							j--;
							ptr2 += 3;
							ptr3 += 4;
						}
						break;
					}
					case 33:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							j--;
							ptr2 += 4;
							ptr3++;
						}
						break;
					}
					case 34:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							ptr3[1] = ptr2[3];
							j--;
							ptr2 += 4;
							ptr3 += 2;
						}
						break;
					}
					case 35:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = *ptr2;
							ptr3[1] = ptr2[1];
							ptr3[2] = ptr2[2];
							j--;
							ptr2 += 4;
							ptr3 += 3;
						}
						break;
					}
					default:
						goto IL_33A;
					}
					break;
				}
				i++;
				continue;
				IL_33A:
				return (StbImage.stbi__err("0") != 0) ? null : null;
			}
			CRuntime.free((void*)data);
			return ptr;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x0000A2DB File Offset: 0x000084DB
		public static ushort stbi__compute_y_16(int r, int g, int b)
		{
			return (ushort)(r * 77 + g * 150 + 29 * b >> 8);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x0000A2F4 File Offset: 0x000084F4
		public unsafe static ushort* stbi__convert_format16(ushort* data, int img_n, int req_comp, uint x, uint y)
		{
			if (req_comp == img_n)
			{
				return data;
			}
			ushort* ptr = (ushort*)StbImage.stbi__malloc((ulong)((long)req_comp * (long)((ulong)x) * (long)((ulong)y) * 2L));
			if (ptr == null)
			{
				CRuntime.free((void*)data);
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			int i = 0;
			while (i < (int)y)
			{
				ushort* ptr2 = data + (long)i * (long)((ulong)x) * (long)img_n * 2L / 2L;
				ushort* ptr3 = ptr + (long)i * (long)((ulong)x) * (long)req_comp * 2L / 2L;
				int num = img_n * 8 + req_comp;
				switch (num)
				{
				case 10:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = *ptr2;
						ptr3[1] = ushort.MaxValue;
						j--;
						ptr2++;
						ptr3 += 2;
					}
					break;
				}
				case 11:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						j--;
						ptr2++;
						ptr3 += 3;
					}
					break;
				}
				case 12:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						ptr3[3] = ushort.MaxValue;
						j--;
						ptr2++;
						ptr3 += 4;
					}
					break;
				}
				case 13:
				case 14:
				case 15:
				case 16:
				case 18:
					goto IL_3B0;
				case 17:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = *ptr2;
						j--;
						ptr2 += 2;
						ptr3++;
					}
					break;
				}
				case 19:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						j--;
						ptr2 += 2;
						ptr3 += 3;
					}
					break;
				}
				case 20:
				{
					int j = (int)(x - 1U);
					while (j >= 0)
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = *ptr2));
						ptr3[3] = ptr2[1];
						j--;
						ptr2 += 2;
						ptr3 += 4;
					}
					break;
				}
				default:
					switch (num)
					{
					case 25:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y_16((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							j--;
							ptr2 += 3;
							ptr3++;
						}
						break;
					}
					case 26:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y_16((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							ptr3[1] = ushort.MaxValue;
							j--;
							ptr2 += 3;
							ptr3 += 2;
						}
						break;
					}
					case 27:
					case 29:
					case 30:
					case 31:
					case 32:
						goto IL_3B0;
					case 28:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = *ptr2;
							ptr3[1] = ptr2[1];
							ptr3[2] = ptr2[2];
							ptr3[3] = ushort.MaxValue;
							j--;
							ptr2 += 3;
							ptr3 += 4;
						}
						break;
					}
					case 33:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y_16((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							j--;
							ptr2 += 4;
							ptr3++;
						}
						break;
					}
					case 34:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = StbImage.stbi__compute_y_16((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
							ptr3[1] = ptr2[3];
							j--;
							ptr2 += 4;
							ptr3 += 2;
						}
						break;
					}
					case 35:
					{
						int j = (int)(x - 1U);
						while (j >= 0)
						{
							*ptr3 = *ptr2;
							ptr3[1] = ptr2[1];
							ptr3[2] = ptr2[2];
							j--;
							ptr2 += 4;
							ptr3 += 3;
						}
						break;
					}
					default:
						goto IL_3B0;
					}
					break;
				}
				i++;
				continue;
				IL_3B0:
				return (StbImage.stbi__err("0") != 0) ? null : null;
			}
			CRuntime.free((void*)data);
			return ptr;
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000A6DC File Offset: 0x000088DC
		public unsafe static int stbi__build_huffman(StbImage.stbi__huffman h, int* count)
		{
			int num = 0;
			int j;
			for (int i = 0; i < 16; i++)
			{
				for (j = 0; j < count[i]; j++)
				{
					h.size[num++] = (byte)(i + 1);
				}
			}
			h.size[num] = 0;
			int num2 = 0;
			num = 0;
			for (j = 1; j <= 16; j++)
			{
				h.delta[j] = num - num2;
				if ((int)h.size[num] == j)
				{
					while ((int)h.size[num] == j)
					{
						h.code[num++] = (ushort)num2++;
					}
					if (num2 - 1 >= 1 << j)
					{
						return StbImage.stbi__err("bad code lengths");
					}
				}
				h.maxcode[j] = (uint)((uint)num2 << 16 - j);
				num2 <<= 1;
			}
			h.maxcode[j] = uint.MaxValue;
			for (int i = 0; i < h.fast.Length; i++)
			{
				h.fast[i] = byte.MaxValue;
			}
			for (int i = 0; i < num; i++)
			{
				int num3 = (int)h.size[i];
				if (num3 <= 9)
				{
					int num4 = (int)h.code[i] << 9 - num3;
					int num5 = 1 << 9 - num3;
					for (j = 0; j < num5; j++)
					{
						h.fast[num4 + j] = (byte)i;
					}
				}
			}
			return 1;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000A814 File Offset: 0x00008A14
		public static void stbi__build_fast_ac(short[] fast_ac, StbImage.stbi__huffman h)
		{
			for (int i = 0; i < 512; i++)
			{
				byte b = h.fast[i];
				fast_ac[i] = 0;
				if (b < 255)
				{
					byte b2 = h.values[(int)b];
					int num = (b2 >> 4) & 15;
					int num2 = (int)(b2 & 15);
					int num3 = (int)h.size[(int)b];
					if (num2 != 0 && num3 + num2 <= 9)
					{
						int num4 = ((i << num3) & 511) >> 9 - num2;
						int num5 = 1 << num2 - 1;
						if (num4 < num5)
						{
							num4 += (-1 << num2) + 1;
						}
						if (num4 >= -128 && num4 <= 127)
						{
							fast_ac[i] = (short)((num4 << 8) + (num << 4) + (num3 + num2));
						}
					}
				}
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x0000A8C8 File Offset: 0x00008AC8
		public static void stbi__grow_buffer_unsafe(StbImage.stbi__jpeg j)
		{
			int num2;
			for (;;)
			{
				int num = (int)((j.nomore != 0) ? 0 : StbImage.stbi__get8(j.s));
				if (num == 255)
				{
					for (num2 = (int)StbImage.stbi__get8(j.s); num2 == 255; num2 = (int)StbImage.stbi__get8(j.s))
					{
					}
					if (num2 != 0)
					{
						break;
					}
				}
				j.code_buffer |= (uint)((uint)num << 24 - j.code_bits);
				j.code_bits += 8;
				if (j.code_bits > 24)
				{
					return;
				}
			}
			j.marker = (byte)num2;
			j.nomore = 1;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000A960 File Offset: 0x00008B60
		public static int stbi__jpeg_huff_decode(StbImage.stbi__jpeg j, StbImage.stbi__huffman h)
		{
			if (j.code_bits < 16)
			{
				StbImage.stbi__grow_buffer_unsafe(j);
			}
			int num = (int)((j.code_buffer >> 23) & 511U);
			int num2 = (int)h.fast[num];
			if (num2 < 255)
			{
				int num3 = (int)h.size[num2];
				if (num3 > j.code_bits)
				{
					return -1;
				}
				j.code_buffer <<= num3;
				j.code_bits -= num3;
				return (int)h.values[num2];
			}
			else
			{
				uint num4 = j.code_buffer >> 16;
				num2 = 10;
				while (num4 >= h.maxcode[num2])
				{
					num2++;
				}
				if (num2 == 17)
				{
					j.code_bits -= 16;
					return -1;
				}
				if (num2 > j.code_bits)
				{
					return -1;
				}
				num = (int)((ulong)((j.code_buffer >> 32 - num2) & StbImage.stbi__bmask[num2]) + (ulong)((long)h.delta[num2]));
				j.code_bits -= num2;
				j.code_buffer <<= num2;
				return (int)h.values[num];
			}
		}

		// Token: 0x060000CC RID: 204 RVA: 0x0000AA64 File Offset: 0x00008C64
		public static int stbi__extend_receive(StbImage.stbi__jpeg j, int n)
		{
			if (j.code_bits < n)
			{
				StbImage.stbi__grow_buffer_unsafe(j);
			}
			int num = (int)j.code_buffer >> 31;
			uint num2 = CRuntime._lrotl(j.code_buffer, n);
			j.code_buffer = num2 & ~StbImage.stbi__bmask[n];
			num2 &= StbImage.stbi__bmask[n];
			j.code_bits -= n;
			return (int)((ulong)num2 + (ulong)((long)(StbImage.stbi__jbias[n] & ~num)));
		}

		// Token: 0x060000CD RID: 205 RVA: 0x0000AAD0 File Offset: 0x00008CD0
		public static int stbi__jpeg_get_bits(StbImage.stbi__jpeg j, int n)
		{
			if (j.code_bits < n)
			{
				StbImage.stbi__grow_buffer_unsafe(j);
			}
			uint num = CRuntime._lrotl(j.code_buffer, n);
			j.code_buffer = num & ~StbImage.stbi__bmask[n];
			num &= StbImage.stbi__bmask[n];
			j.code_bits -= n;
			return (int)num;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000AB22 File Offset: 0x00008D22
		public static int stbi__jpeg_get_bit(StbImage.stbi__jpeg j)
		{
			if (j.code_bits < 1)
			{
				StbImage.stbi__grow_buffer_unsafe(j);
			}
			int code_buffer = (int)j.code_buffer;
			j.code_buffer <<= 1;
			j.code_bits--;
			return code_buffer & int.MinValue;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000AB5C File Offset: 0x00008D5C
		public unsafe static int stbi__jpeg_decode_block(StbImage.stbi__jpeg j, short* data, StbImage.stbi__huffman hdc, StbImage.stbi__huffman hac, short[] fac, int b, ushort[] dequant)
		{
			if (j.code_bits < 16)
			{
				StbImage.stbi__grow_buffer_unsafe(j);
			}
			int num = StbImage.stbi__jpeg_huff_decode(j, hdc);
			if (num < 0)
			{
				return StbImage.stbi__err("bad huffman code");
			}
			CRuntime.memset((void*)data, 0, 128UL);
			int num2 = ((num != 0) ? StbImage.stbi__extend_receive(j, num) : 0);
			int num3 = j.img_comp[b].dc_pred + num2;
			j.img_comp[b].dc_pred = num3;
			*data = (short)(num3 * (int)dequant[0]);
			int num4 = 1;
			for (;;)
			{
				if (j.code_bits < 16)
				{
					StbImage.stbi__grow_buffer_unsafe(j);
				}
				int num5 = (int)((j.code_buffer >> 23) & 511U);
				int num6 = (int)fac[num5];
				if (num6 != 0)
				{
					num4 += (num6 >> 4) & 15;
					int num7 = num6 & 15;
					j.code_buffer <<= num7;
					j.code_bits -= num7;
					uint num8 = (uint)StbImage.stbi__jpeg_dezigzag[num4++];
					data[(ulong)num8 * 2UL / 2UL] = (short)((num6 >> 8) * (int)dequant[(int)num8]);
				}
				else
				{
					int num9 = StbImage.stbi__jpeg_huff_decode(j, hac);
					if (num9 < 0)
					{
						break;
					}
					int num7 = num9 & 15;
					num6 = num9 >> 4;
					if (num7 == 0)
					{
						if (num9 != 240)
						{
							return 1;
						}
						num4 += 16;
					}
					else
					{
						num4 += num6;
						uint num8 = (uint)StbImage.stbi__jpeg_dezigzag[num4++];
						data[(ulong)num8 * 2UL / 2UL] = (short)(StbImage.stbi__extend_receive(j, num7) * (int)dequant[(int)num8]);
					}
				}
				if (num4 >= 64)
				{
					return 1;
				}
			}
			return StbImage.stbi__err("bad huffman code");
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000ACD0 File Offset: 0x00008ED0
		public unsafe static int stbi__jpeg_decode_block_prog_dc(StbImage.stbi__jpeg j, short* data, StbImage.stbi__huffman hdc, int b)
		{
			if (j.spec_end != 0)
			{
				return StbImage.stbi__err("can't merge dc and ac");
			}
			if (j.code_bits < 16)
			{
				StbImage.stbi__grow_buffer_unsafe(j);
			}
			if (j.succ_high == 0)
			{
				CRuntime.memset((void*)data, 0, 128UL);
				int num = StbImage.stbi__jpeg_huff_decode(j, hdc);
				int num2 = ((num != 0) ? StbImage.stbi__extend_receive(j, num) : 0);
				int num3 = j.img_comp[b].dc_pred + num2;
				j.img_comp[b].dc_pred = num3;
				*data = (short)(num3 << j.succ_low);
			}
			else if (StbImage.stbi__jpeg_get_bit(j) != 0)
			{
				*data += (short)(1 << j.succ_low);
			}
			return 1;
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000AD7C File Offset: 0x00008F7C
		public unsafe static int stbi__jpeg_decode_block_prog_ac(StbImage.stbi__jpeg j, short* data, StbImage.stbi__huffman hac, short[] fac)
		{
			if (j.spec_start == 0)
			{
				return StbImage.stbi__err("can't merge dc and ac");
			}
			if (j.succ_high == 0)
			{
				int succ_low = j.succ_low;
				if (j.eob_run != 0)
				{
					j.eob_run--;
					return 1;
				}
				int i = j.spec_start;
				int num2;
				for (;;)
				{
					if (j.code_bits < 16)
					{
						StbImage.stbi__grow_buffer_unsafe(j);
					}
					int num = (int)((j.code_buffer >> 23) & 511U);
					num2 = (int)fac[num];
					if (num2 != 0)
					{
						i += (num2 >> 4) & 15;
						int num3 = num2 & 15;
						j.code_buffer <<= num3;
						j.code_bits -= num3;
						uint num4 = (uint)StbImage.stbi__jpeg_dezigzag[i++];
						data[(ulong)num4 * 2UL / 2UL] = (short)(num2 >> 8 << succ_low);
					}
					else
					{
						int num5 = StbImage.stbi__jpeg_huff_decode(j, hac);
						if (num5 < 0)
						{
							break;
						}
						int num3 = num5 & 15;
						num2 = num5 >> 4;
						if (num3 == 0)
						{
							if (num2 < 15)
							{
								goto Block_8;
							}
							i += 16;
						}
						else
						{
							i += num2;
							uint num4 = (uint)StbImage.stbi__jpeg_dezigzag[i++];
							data[(ulong)num4 * 2UL / 2UL] = (short)(StbImage.stbi__extend_receive(j, num3) << succ_low);
						}
					}
					if (i > j.spec_end)
					{
						goto Block_10;
					}
				}
				return StbImage.stbi__err("bad huffman code");
				Block_8:
				j.eob_run = 1 << num2;
				if (num2 != 0)
				{
					j.eob_run += StbImage.stbi__jpeg_get_bits(j, num2);
				}
				j.eob_run--;
				Block_10:;
			}
			else
			{
				short num6 = (short)(1 << j.succ_low);
				if (j.eob_run == 0)
				{
					int i = j.spec_start;
					for (;;)
					{
						int num7 = StbImage.stbi__jpeg_huff_decode(j, hac);
						if (num7 < 0)
						{
							break;
						}
						int num8 = num7 & 15;
						int num9 = num7 >> 4;
						if (num8 == 0)
						{
							if (num9 < 15)
							{
								j.eob_run = (1 << num9) - 1;
								if (num9 != 0)
								{
									j.eob_run += StbImage.stbi__jpeg_get_bits(j, num9);
								}
								num9 = 64;
							}
						}
						else
						{
							if (num8 != 1)
							{
								goto Block_21;
							}
							if (StbImage.stbi__jpeg_get_bit(j) != 0)
							{
								num8 = (int)num6;
							}
							else
							{
								num8 = (int)(-(int)num6);
							}
						}
						while (i <= j.spec_end)
						{
							short* ptr = data + (IntPtr)StbImage.stbi__jpeg_dezigzag[i++];
							if (*ptr != 0)
							{
								if (StbImage.stbi__jpeg_get_bit(j) != 0 && (*ptr & num6) == 0)
								{
									if (*ptr > 0)
									{
										short* ptr2 = ptr;
										*ptr2 += num6;
									}
									else
									{
										short* ptr3 = ptr;
										*ptr3 -= num6;
									}
								}
							}
							else
							{
								if (num9 == 0)
								{
									*ptr = (short)num8;
									break;
								}
								num9--;
							}
						}
						if (i > j.spec_end)
						{
							return 1;
						}
					}
					return StbImage.stbi__err("bad huffman code");
					Block_21:
					return StbImage.stbi__err("bad huffman code");
				}
				j.eob_run--;
				for (int i = j.spec_start; i <= j.spec_end; i++)
				{
					short* ptr4 = data + StbImage.stbi__jpeg_dezigzag[i];
					if (*ptr4 != 0 && StbImage.stbi__jpeg_get_bit(j) != 0 && (*ptr4 & num6) == 0)
					{
						if (*ptr4 > 0)
						{
							short* ptr5 = ptr4;
							*ptr5 += num6;
						}
						else
						{
							short* ptr6 = ptr4;
							*ptr6 -= num6;
						}
					}
				}
			}
			return 1;
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000B06B File Offset: 0x0000926B
		public static byte stbi__clamp(int x)
		{
			if (x > 255)
			{
				if (x < 0)
				{
					return 0;
				}
				if (x > 255)
				{
					return byte.MaxValue;
				}
			}
			return (byte)x;
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000B08C File Offset: 0x0000928C
		public unsafe static void stbi__idct_block(byte* _out_, int out_stride, short* data)
		{
			int* ptr = stackalloc int[(UIntPtr)256];
			int* ptr2 = ptr;
			short* ptr3 = data;
			int i = 0;
			while (i < 8)
			{
				if (ptr3[8] == 0 && ptr3[16] == 0 && ptr3[24] == 0 && ptr3[32] == 0 && ptr3[40] == 0 && ptr3[48] == 0 && ptr3[56] == 0)
				{
					int num = (int)(*ptr3) << 2;
					*ptr2 = (ptr2[8] = (ptr2[16] = (ptr2[24] = (ptr2[32] = (ptr2[40] = (ptr2[48] = (ptr2[56] = num)))))));
				}
				else
				{
					int num2 = (int)ptr3[16];
					int num3 = (int)ptr3[48];
					int num4 = (num2 + num3) * 2217;
					int num5 = num4 + num3 * -7567;
					int num6 = num4 + num2 * 3135;
					num2 = (int)(*ptr3);
					num3 = (int)ptr3[32];
					int num7 = num2 + num3 << 12;
					int num8 = num2 - num3 << 12;
					int num9 = num7 + num6;
					int num10 = num7 - num6;
					int num11 = num8 + num5;
					int num12 = num8 - num5;
					num7 = (int)ptr3[56];
					num8 = (int)ptr3[40];
					num5 = (int)ptr3[24];
					num6 = (int)ptr3[8];
					num3 = num7 + num5;
					int num13 = num8 + num6;
					num4 = num7 + num6;
					num2 = num8 + num5;
					int num14 = (num3 + num13) * 4816;
					num7 *= 1223;
					num8 *= 8410;
					num5 *= 12586;
					num6 *= 6149;
					num4 = num14 + num4 * -3685;
					num2 = num14 + num2 * -10497;
					num3 *= -8034;
					num13 *= -1597;
					num6 += num4 + num13;
					num5 += num2 + num3;
					num8 += num2 + num13;
					num7 += num4 + num3;
					num9 += 512;
					num11 += 512;
					num12 += 512;
					num10 += 512;
					*ptr2 = num9 + num6 >> 10;
					ptr2[56] = num9 - num6 >> 10;
					ptr2[8] = num11 + num5 >> 10;
					ptr2[48] = num11 - num5 >> 10;
					ptr2[16] = num12 + num8 >> 10;
					ptr2[40] = num12 - num8 >> 10;
					ptr2[24] = num10 + num7 >> 10;
					ptr2[32] = num10 - num7 >> 10;
				}
				i++;
				ptr3++;
				ptr2++;
			}
			i = 0;
			ptr2 = ptr;
			byte* ptr4 = _out_;
			while (i < 8)
			{
				int num15 = ptr2[2];
				int num16 = ptr2[6];
				int num17 = (num15 + num16) * 2217;
				int num18 = num17 + num16 * -7567;
				int num19 = num17 + num15 * 3135;
				num15 = *ptr2;
				num16 = ptr2[4];
				int num20 = num15 + num16 << 12;
				int num21 = num15 - num16 << 12;
				int num22 = num20 + num19;
				int num23 = num20 - num19;
				int num24 = num21 + num18;
				int num25 = num21 - num18;
				num20 = ptr2[7];
				num21 = ptr2[5];
				num18 = ptr2[3];
				num19 = ptr2[1];
				num16 = num20 + num18;
				int num26 = num21 + num19;
				num17 = num20 + num19;
				num15 = num21 + num18;
				int num27 = (num16 + num26) * 4816;
				num20 *= 1223;
				num21 *= 8410;
				num18 *= 12586;
				num19 *= 6149;
				num17 = num27 + num17 * -3685;
				num15 = num27 + num15 * -10497;
				num16 *= -8034;
				num26 *= -1597;
				num19 += num17 + num26;
				num18 += num15 + num16;
				num21 += num15 + num26;
				num20 += num17 + num16;
				num22 += 16842752;
				num24 += 16842752;
				num25 += 16842752;
				num23 += 16842752;
				*ptr4 = StbImage.stbi__clamp(num22 + num19 >> 17);
				ptr4[7] = StbImage.stbi__clamp(num22 - num19 >> 17);
				ptr4[1] = StbImage.stbi__clamp(num24 + num18 >> 17);
				ptr4[6] = StbImage.stbi__clamp(num24 - num18 >> 17);
				ptr4[2] = StbImage.stbi__clamp(num25 + num21 >> 17);
				ptr4[5] = StbImage.stbi__clamp(num25 - num21 >> 17);
				ptr4[3] = StbImage.stbi__clamp(num23 + num20 >> 17);
				ptr4[4] = StbImage.stbi__clamp(num23 - num20 >> 17);
				i++;
				ptr2 += 8;
				ptr4 += out_stride;
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000B57C File Offset: 0x0000977C
		public static byte stbi__get_marker(StbImage.stbi__jpeg j)
		{
			byte b;
			if (j.marker != 255)
			{
				b = j.marker;
				j.marker = byte.MaxValue;
				return b;
			}
			b = StbImage.stbi__get8(j.s);
			if (b != 255)
			{
				return byte.MaxValue;
			}
			while (b == 255)
			{
				b = StbImage.stbi__get8(j.s);
			}
			return b;
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x0000B5DC File Offset: 0x000097DC
		public static void stbi__jpeg_reset(StbImage.stbi__jpeg j)
		{
			j.code_bits = 0;
			j.code_buffer = 0U;
			j.nomore = 0;
			j.img_comp[0].dc_pred = (j.img_comp[1].dc_pred = (j.img_comp[2].dc_pred = (j.img_comp[3].dc_pred = 0)));
			j.marker = byte.MaxValue;
			j.todo = ((j.restart_interval != 0) ? j.restart_interval : int.MaxValue);
			j.eob_run = 0;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000B67C File Offset: 0x0000987C
		public unsafe static int stbi__parse_entropy_coded_data(StbImage.stbi__jpeg z)
		{
			StbImage.stbi__jpeg_reset(z);
			if (z.progressive == 0)
			{
				if (z.scan_n == 1)
				{
					short* data = stackalloc short[(UIntPtr)128];
					int num = z.order[0];
					int num2 = z.img_comp[num].x + 7 >> 3;
					int num3 = z.img_comp[num].y + 7 >> 3;
					for (int i = 0; i < num3; i++)
					{
						for (int j = 0; j < num2; j++)
						{
							int ha = z.img_comp[num].ha;
							if (StbImage.stbi__jpeg_decode_block(z, data, z.huff_dc[z.img_comp[num].hd], z.huff_ac[ha], z.fast_ac[ha], num, z.dequant[z.img_comp[num].tq]) == 0)
							{
								return 0;
							}
							z.idct_block_kernel(z.img_comp[num].data + z.img_comp[num].w2 * i * 8 + j * 8, z.img_comp[num].w2, data);
							int num4 = z.todo - 1;
							z.todo = num4;
							if (num4 <= 0)
							{
								if (z.code_bits < 24)
								{
									StbImage.stbi__grow_buffer_unsafe(z);
								}
								if (z.marker < 208 || z.marker > 215)
								{
									return 1;
								}
								StbImage.stbi__jpeg_reset(z);
							}
						}
					}
					return 1;
				}
				short* data2 = stackalloc short[(UIntPtr)128];
				for (int k = 0; k < z.img_mcu_y; k++)
				{
					for (int l = 0; l < z.img_mcu_x; l++)
					{
						for (int m = 0; m < z.scan_n; m++)
						{
							int num5 = z.order[m];
							for (int n = 0; n < z.img_comp[num5].v; n++)
							{
								for (int num6 = 0; num6 < z.img_comp[num5].h; num6++)
								{
									int num7 = (l * z.img_comp[num5].h + num6) * 8;
									int num8 = (k * z.img_comp[num5].v + n) * 8;
									int ha2 = z.img_comp[num5].ha;
									if (StbImage.stbi__jpeg_decode_block(z, data2, z.huff_dc[z.img_comp[num5].hd], z.huff_ac[ha2], z.fast_ac[ha2], num5, z.dequant[z.img_comp[num5].tq]) == 0)
									{
										return 0;
									}
									z.idct_block_kernel(z.img_comp[num5].data + z.img_comp[num5].w2 * num8 + num7, z.img_comp[num5].w2, data2);
								}
							}
						}
						int num4 = z.todo - 1;
						z.todo = num4;
						if (num4 <= 0)
						{
							if (z.code_bits < 24)
							{
								StbImage.stbi__grow_buffer_unsafe(z);
							}
							if (z.marker < 208 || z.marker > 215)
							{
								return 1;
							}
							StbImage.stbi__jpeg_reset(z);
						}
					}
				}
				return 1;
			}
			else
			{
				if (z.scan_n == 1)
				{
					int num9 = z.order[0];
					int num10 = z.img_comp[num9].x + 7 >> 3;
					int num11 = z.img_comp[num9].y + 7 >> 3;
					for (int num12 = 0; num12 < num11; num12++)
					{
						for (int num13 = 0; num13 < num10; num13++)
						{
							short* data3 = z.img_comp[num9].coeff + 64 * (num13 + num12 * z.img_comp[num9].coeff_w);
							if (z.spec_start == 0)
							{
								if (StbImage.stbi__jpeg_decode_block_prog_dc(z, data3, z.huff_dc[z.img_comp[num9].hd], num9) == 0)
								{
									return 0;
								}
							}
							else
							{
								int ha3 = z.img_comp[num9].ha;
								if (StbImage.stbi__jpeg_decode_block_prog_ac(z, data3, z.huff_ac[ha3], z.fast_ac[ha3]) == 0)
								{
									return 0;
								}
							}
							int num4 = z.todo - 1;
							z.todo = num4;
							if (num4 <= 0)
							{
								if (z.code_bits < 24)
								{
									StbImage.stbi__grow_buffer_unsafe(z);
								}
								if (z.marker < 208 || z.marker > 215)
								{
									return 1;
								}
								StbImage.stbi__jpeg_reset(z);
							}
						}
					}
					return 1;
				}
				for (int num14 = 0; num14 < z.img_mcu_y; num14++)
				{
					for (int num15 = 0; num15 < z.img_mcu_x; num15++)
					{
						for (int num16 = 0; num16 < z.scan_n; num16++)
						{
							int num17 = z.order[num16];
							for (int num18 = 0; num18 < z.img_comp[num17].v; num18++)
							{
								for (int num19 = 0; num19 < z.img_comp[num17].h; num19++)
								{
									int num20 = num15 * z.img_comp[num17].h + num19;
									int num21 = num14 * z.img_comp[num17].v + num18;
									short* data4 = z.img_comp[num17].coeff + 64 * (num20 + num21 * z.img_comp[num17].coeff_w);
									if (StbImage.stbi__jpeg_decode_block_prog_dc(z, data4, z.huff_dc[z.img_comp[num17].hd], num17) == 0)
									{
										return 0;
									}
								}
							}
						}
						int num4 = z.todo - 1;
						z.todo = num4;
						if (num4 <= 0)
						{
							if (z.code_bits < 24)
							{
								StbImage.stbi__grow_buffer_unsafe(z);
							}
							if (z.marker < 208 || z.marker > 215)
							{
								return 1;
							}
							StbImage.stbi__jpeg_reset(z);
						}
					}
				}
				return 1;
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000BCBC File Offset: 0x00009EBC
		public unsafe static void stbi__jpeg_dequantize(short* data, ushort[] dequant)
		{
			for (int i = 0; i < 64; i++)
			{
				short* ptr = data + i;
				*ptr *= (short)dequant[i];
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000BCE8 File Offset: 0x00009EE8
		public unsafe static void stbi__jpeg_finish(StbImage.stbi__jpeg z)
		{
			if (z.progressive != 0)
			{
				for (int i = 0; i < z.s.img_n; i++)
				{
					int num = z.img_comp[i].x + 7 >> 3;
					int num2 = z.img_comp[i].y + 7 >> 3;
					for (int j = 0; j < num2; j++)
					{
						for (int k = 0; k < num; k++)
						{
							short* data = z.img_comp[i].coeff + 64 * (k + j * z.img_comp[i].coeff_w);
							StbImage.stbi__jpeg_dequantize(data, z.dequant[z.img_comp[i].tq]);
							z.idct_block_kernel(z.img_comp[i].data + z.img_comp[i].w2 * j * 8 + k * 8, z.img_comp[i].w2, data);
						}
					}
				}
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000BE08 File Offset: 0x0000A008
		public unsafe static int stbi__process_marker(StbImage.stbi__jpeg z, int m)
		{
			int i;
			if (m <= 219)
			{
				if (m != 196)
				{
					if (m == 219)
					{
						int num2;
						for (i = StbImage.stbi__get16be(z.s) - 2; i > 0; i -= ((num2 != 0) ? 129 : 65))
						{
							byte b = StbImage.stbi__get8(z.s);
							int num = b >> 4;
							num2 = ((num != 0) ? 1 : 0);
							int num3 = (int)(b & 15);
							if (num != 0 && num != 1)
							{
								return StbImage.stbi__err("bad DQT type");
							}
							if (num3 > 3)
							{
								return StbImage.stbi__err("bad DQT table");
							}
							for (int j = 0; j < 64; j++)
							{
								z.dequant[num3][(int)StbImage.stbi__jpeg_dezigzag[j]] = (ushort)((num2 != 0) ? StbImage.stbi__get16be(z.s) : ((int)StbImage.stbi__get8(z.s)));
							}
						}
						if (i != 0)
						{
							return 0;
						}
						return 1;
					}
				}
				else
				{
					int num4;
					for (i = StbImage.stbi__get16be(z.s) - 2; i > 0; i -= num4)
					{
						int* ptr = stackalloc int[(UIntPtr)64];
						num4 = 0;
						byte b2 = StbImage.stbi__get8(z.s);
						int num5 = b2 >> 4;
						int num6 = (int)(b2 & 15);
						if (num5 > 1 || num6 > 3)
						{
							return StbImage.stbi__err("bad DHT header");
						}
						for (int k = 0; k < 16; k++)
						{
							ptr[k] = (int)StbImage.stbi__get8(z.s);
							num4 += ptr[k];
						}
						i -= 17;
						byte[] values;
						if (num5 == 0)
						{
							if (StbImage.stbi__build_huffman(z.huff_dc[num6], ptr) == 0)
							{
								return 0;
							}
							values = z.huff_dc[num6].values;
						}
						else
						{
							if (StbImage.stbi__build_huffman(z.huff_ac[num6], ptr) == 0)
							{
								return 0;
							}
							values = z.huff_ac[num6].values;
						}
						for (int k = 0; k < num4; k++)
						{
							values[k] = StbImage.stbi__get8(z.s);
						}
						if (num5 != 0)
						{
							StbImage.stbi__build_fast_ac(z.fast_ac[num6], z.huff_ac[num6]);
						}
					}
					if (i != 0)
					{
						return 0;
					}
					return 1;
				}
			}
			else if (m != 221)
			{
				if (m == 255)
				{
					return StbImage.stbi__err("expected marker");
				}
			}
			else
			{
				if (StbImage.stbi__get16be(z.s) != 4)
				{
					return StbImage.stbi__err("bad DRI len");
				}
				z.restart_interval = StbImage.stbi__get16be(z.s);
				return 1;
			}
			if ((m < 224 || m > 239) && m != 254)
			{
				return StbImage.stbi__err("unknown marker");
			}
			i = StbImage.stbi__get16be(z.s);
			if (i >= 2)
			{
				i -= 2;
				if (m == 224 && i >= 5)
				{
					byte* ptr2 = stackalloc byte[(UIntPtr)5];
					*ptr2 = 74;
					ptr2[1] = 70;
					ptr2[2] = 73;
					ptr2[3] = 70;
					ptr2[4] = 0;
					int num7 = 1;
					for (int l = 0; l < 5; l++)
					{
						if (StbImage.stbi__get8(z.s) != ptr2[l])
						{
							num7 = 0;
						}
					}
					i -= 5;
					if (num7 != 0)
					{
						z.jfif = 1;
					}
				}
				else if (m == 238 && i >= 12)
				{
					byte* ptr3 = stackalloc byte[(UIntPtr)6];
					*ptr3 = 65;
					ptr3[1] = 100;
					ptr3[2] = 111;
					ptr3[3] = 98;
					ptr3[4] = 101;
					ptr3[5] = 0;
					int num8 = 1;
					for (int n = 0; n < 6; n++)
					{
						if (StbImage.stbi__get8(z.s) != ptr3[n])
						{
							num8 = 0;
						}
					}
					i -= 6;
					if (num8 != 0)
					{
						StbImage.stbi__get8(z.s);
						StbImage.stbi__get16be(z.s);
						StbImage.stbi__get16be(z.s);
						z.app14_color_transform = (int)StbImage.stbi__get8(z.s);
						i -= 6;
					}
				}
				StbImage.stbi__skip(z.s, i);
				return 1;
			}
			if (m == 254)
			{
				return StbImage.stbi__err("bad COM len");
			}
			return StbImage.stbi__err("bad APP len");
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000C1C8 File Offset: 0x0000A3C8
		public static int stbi__process_scan_header(StbImage.stbi__jpeg z)
		{
			int num = StbImage.stbi__get16be(z.s);
			z.scan_n = (int)StbImage.stbi__get8(z.s);
			if (z.scan_n < 1 || z.scan_n > 4 || z.scan_n > z.s.img_n)
			{
				return StbImage.stbi__err("bad SOS component count");
			}
			if (num != 6 + 2 * z.scan_n)
			{
				return StbImage.stbi__err("bad SOS len");
			}
			for (int i = 0; i < z.scan_n; i++)
			{
				int num2 = (int)StbImage.stbi__get8(z.s);
				int num3 = (int)StbImage.stbi__get8(z.s);
				int num4 = 0;
				while (num4 < z.s.img_n && z.img_comp[num4].id != num2)
				{
					num4++;
				}
				if (num4 == z.s.img_n)
				{
					return 0;
				}
				z.img_comp[num4].hd = num3 >> 4;
				if (z.img_comp[num4].hd > 3)
				{
					return StbImage.stbi__err("bad DC huff");
				}
				z.img_comp[num4].ha = num3 & 15;
				if (z.img_comp[num4].ha > 3)
				{
					return StbImage.stbi__err("bad AC huff");
				}
				z.order[i] = num4;
			}
			z.spec_start = (int)StbImage.stbi__get8(z.s);
			z.spec_end = (int)StbImage.stbi__get8(z.s);
			int num5 = (int)StbImage.stbi__get8(z.s);
			z.succ_high = num5 >> 4;
			z.succ_low = num5 & 15;
			if (z.progressive != 0)
			{
				if (z.spec_start > 63 || z.spec_end > 63 || z.spec_start > z.spec_end || z.succ_high > 13 || z.succ_low > 13)
				{
					return StbImage.stbi__err("bad SOS");
				}
			}
			else
			{
				if (z.spec_start != 0)
				{
					return StbImage.stbi__err("bad SOS");
				}
				if (z.succ_high != 0 || z.succ_low != 0)
				{
					return StbImage.stbi__err("bad SOS");
				}
				z.spec_end = 63;
			}
			return 1;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000C3DC File Offset: 0x0000A5DC
		public unsafe static int stbi__free_jpeg_components(StbImage.stbi__jpeg z, int ncomp, int why)
		{
			for (int i = 0; i < ncomp; i++)
			{
				if (z.img_comp[i].raw_data != null)
				{
					CRuntime.free(z.img_comp[i].raw_data);
					z.img_comp[i].raw_data = null;
					z.img_comp[i].data = null;
				}
				if (z.img_comp[i].raw_coeff != null)
				{
					CRuntime.free(z.img_comp[i].raw_coeff);
					z.img_comp[i].raw_coeff = null;
					z.img_comp[i].coeff = null;
				}
				if (z.img_comp[i].linebuf != null)
				{
					CRuntime.free((void*)z.img_comp[i].linebuf);
					z.img_comp[i].linebuf = null;
				}
			}
			return why;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000C4DC File Offset: 0x0000A6DC
		public unsafe static int stbi__process_frame_header(StbImage.stbi__jpeg z, int scan)
		{
			StbImage.stbi__context s = z.s;
			int num = 1;
			int num2 = 1;
			int num3 = StbImage.stbi__get16be(s);
			if (num3 < 11)
			{
				return StbImage.stbi__err("bad SOF len");
			}
			if (StbImage.stbi__get8(s) != 8)
			{
				return StbImage.stbi__err("only 8-bit");
			}
			s.img_y = (uint)StbImage.stbi__get16be(s);
			if (s.img_y == 0U)
			{
				return StbImage.stbi__err("no header height");
			}
			s.img_x = (uint)StbImage.stbi__get16be(s);
			if (s.img_x == 0U)
			{
				return StbImage.stbi__err("0 width");
			}
			int num4 = (int)StbImage.stbi__get8(s);
			if (num4 != 3 && num4 != 1 && num4 != 4)
			{
				return StbImage.stbi__err("bad component count");
			}
			s.img_n = num4;
			for (int i = 0; i < num4; i++)
			{
				z.img_comp[i].data = null;
				z.img_comp[i].linebuf = null;
			}
			if (num3 != 8 + 3 * s.img_n)
			{
				return StbImage.stbi__err("bad SOF len");
			}
			z.rgb = 0;
			for (int i = 0; i < s.img_n; i++)
			{
				byte* ptr = stackalloc byte[(UIntPtr)3];
				*ptr = 82;
				ptr[1] = 71;
				ptr[2] = 66;
				z.img_comp[i].id = (int)StbImage.stbi__get8(s);
				if (s.img_n == 3 && z.img_comp[i].id == (int)ptr[i])
				{
					z.rgb++;
				}
				int num5 = (int)StbImage.stbi__get8(s);
				z.img_comp[i].h = num5 >> 4;
				if (z.img_comp[i].h == 0 || z.img_comp[i].h > 4)
				{
					return StbImage.stbi__err("bad H");
				}
				z.img_comp[i].v = num5 & 15;
				if (z.img_comp[i].v == 0 || z.img_comp[i].v > 4)
				{
					return StbImage.stbi__err("bad V");
				}
				z.img_comp[i].tq = (int)StbImage.stbi__get8(s);
				if (z.img_comp[i].tq > 3)
				{
					return StbImage.stbi__err("bad TQ");
				}
			}
			if (scan != 0)
			{
				return 1;
			}
			if (StbImage.stbi__mad3sizes_valid((int)s.img_x, (int)s.img_y, s.img_n, 0) == 0)
			{
				return StbImage.stbi__err("too large");
			}
			for (int i = 0; i < s.img_n; i++)
			{
				if (z.img_comp[i].h > num)
				{
					num = z.img_comp[i].h;
				}
				if (z.img_comp[i].v > num2)
				{
					num2 = z.img_comp[i].v;
				}
			}
			z.img_h_max = num;
			z.img_v_max = num2;
			z.img_mcu_w = num * 8;
			z.img_mcu_h = num2 * 8;
			z.img_mcu_x = (int)(((ulong)s.img_x + (ulong)((long)z.img_mcu_w) - 1UL) / (ulong)((long)z.img_mcu_w));
			z.img_mcu_y = (int)(((ulong)s.img_y + (ulong)((long)z.img_mcu_h) - 1UL) / (ulong)((long)z.img_mcu_h));
			for (int i = 0; i < s.img_n; i++)
			{
				z.img_comp[i].x = (int)(((ulong)s.img_x * (ulong)((long)z.img_comp[i].h) + (ulong)((long)num) - 1UL) / (ulong)((long)num));
				z.img_comp[i].y = (int)(((ulong)s.img_y * (ulong)((long)z.img_comp[i].v) + (ulong)((long)num2) - 1UL) / (ulong)((long)num2));
				z.img_comp[i].w2 = z.img_mcu_x * z.img_comp[i].h * 8;
				z.img_comp[i].h2 = z.img_mcu_y * z.img_comp[i].v * 8;
				z.img_comp[i].coeff = null;
				z.img_comp[i].raw_coeff = null;
				z.img_comp[i].linebuf = null;
				z.img_comp[i].raw_data = StbImage.stbi__malloc_mad2(z.img_comp[i].w2, z.img_comp[i].h2, 15);
				if (z.img_comp[i].raw_data == null)
				{
					return StbImage.stbi__free_jpeg_components(z, i + 1, StbImage.stbi__err("outofmem"));
				}
				z.img_comp[i].data = ((byte*)z.img_comp[i].raw_data + 15L) & -16L;
				if (z.progressive != 0)
				{
					z.img_comp[i].coeff_w = z.img_comp[i].w2 / 8;
					z.img_comp[i].coeff_h = z.img_comp[i].h2 / 8;
					z.img_comp[i].raw_coeff = StbImage.stbi__malloc_mad3(z.img_comp[i].w2, z.img_comp[i].h2, 2, 15);
					if (z.img_comp[i].raw_coeff == null)
					{
						return StbImage.stbi__free_jpeg_components(z, i + 1, StbImage.stbi__err("outofmem"));
					}
					z.img_comp[i].coeff = ((short*)z.img_comp[i].raw_coeff + 15L / 2L) & -16L;
				}
			}
			return 1;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000CA94 File Offset: 0x0000AC94
		public static int stbi__decode_jpeg_header(StbImage.stbi__jpeg z, int scan)
		{
			z.jfif = 0;
			z.app14_color_transform = -1;
			z.marker = byte.MaxValue;
			int num = (int)StbImage.stbi__get_marker(z);
			if (num != 216)
			{
				return StbImage.stbi__err("no SOI");
			}
			if (scan == 1)
			{
				return 1;
			}
			num = (int)StbImage.stbi__get_marker(z);
			while (num != 192 && num != 193 && num != 194)
			{
				if (StbImage.stbi__process_marker(z, num) == 0)
				{
					return 0;
				}
				for (num = (int)StbImage.stbi__get_marker(z); num == 255; num = (int)StbImage.stbi__get_marker(z))
				{
					if (StbImage.stbi__at_eof(z.s) != 0)
					{
						return StbImage.stbi__err("no SOF");
					}
				}
			}
			z.progressive = ((num == 194) ? 1 : 0);
			if (StbImage.stbi__process_frame_header(z, scan) == 0)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000CB54 File Offset: 0x0000AD54
		public static int stbi__decode_jpeg_image(StbImage.stbi__jpeg j)
		{
			for (int i = 0; i < 4; i++)
			{
				j.img_comp[i].raw_data = null;
				j.img_comp[i].raw_coeff = null;
			}
			j.restart_interval = 0;
			if (StbImage.stbi__decode_jpeg_header(j, 0) == 0)
			{
				return 0;
			}
			for (int i = (int)StbImage.stbi__get_marker(j); i != 217; i = (int)StbImage.stbi__get_marker(j))
			{
				if (i == 218)
				{
					if (StbImage.stbi__process_scan_header(j) == 0)
					{
						return 0;
					}
					if (StbImage.stbi__parse_entropy_coded_data(j) == 0)
					{
						return 0;
					}
					if (j.marker == 255)
					{
						while (StbImage.stbi__at_eof(j.s) == 0)
						{
							if (StbImage.stbi__get8(j.s) == 255)
							{
								j.marker = StbImage.stbi__get8(j.s);
								break;
							}
						}
					}
				}
				else if (i == 220)
				{
					int num = StbImage.stbi__get16be(j.s);
					uint num2 = (uint)StbImage.stbi__get16be(j.s);
					if (num != 4)
					{
						StbImage.stbi__err("bad DNL len");
					}
					if (num2 != j.s.img_y)
					{
						StbImage.stbi__err("bad DNL height");
					}
				}
				else if (StbImage.stbi__process_marker(j, i) == 0)
				{
					return 0;
				}
			}
			if (j.progressive != 0)
			{
				StbImage.stbi__jpeg_finish(j);
			}
			return 1;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000CC87 File Offset: 0x0000AE87
		public unsafe static byte* resample_row_1(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			return in_near;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000CC8C File Offset: 0x0000AE8C
		public unsafe static byte* stbi__resample_row_v_2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			for (int i = 0; i < w; i++)
			{
				_out_[i] = (byte)(3 * in_near[i] + in_far[i] + 2 >> 2);
			}
			return _out_;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000CCBC File Offset: 0x0000AEBC
		public unsafe static byte* stbi__resample_row_h_2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			if (w == 1)
			{
				*_out_ = (_out_[1] = *in_near);
				return _out_;
			}
			*_out_ = *in_near;
			_out_[1] = (byte)(*in_near * 3 + in_near[1] + 2 >> 2);
			int i;
			for (i = 1; i < w - 1; i++)
			{
				int num = (int)(3 * in_near[i] + 2);
				_out_[i * 2] = (byte)(num + (int)in_near[i - 1] >> 2);
				_out_[i * 2 + 1] = (byte)(num + (int)in_near[i + 1] >> 2);
			}
			_out_[i * 2] = (byte)(in_near[w - 2] * 3 + in_near[w - 1] + 2 >> 2);
			_out_[i * 2 + 1] = in_near[w - 1];
			return _out_;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000CD58 File Offset: 0x0000AF58
		public unsafe static byte* stbi__resample_row_hv_2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			if (w == 1)
			{
				*_out_ = (_out_[1] = (byte)(3 * *in_near + *in_far + 2 >> 2));
				return _out_;
			}
			int num = (int)(3 * *in_near + *in_far);
			*_out_ = (byte)(num + 2 >> 2);
			for (int i = 1; i < w; i++)
			{
				int num2 = num;
				num = (int)(3 * in_near[i] + in_far[i]);
				_out_[i * 2 - 1] = (byte)(3 * num2 + num + 8 >> 4);
				_out_[i * 2] = (byte)(3 * num + num2 + 8 >> 4);
			}
			_out_[w * 2 - 1] = (byte)(num + 2 >> 2);
			return _out_;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000CDDC File Offset: 0x0000AFDC
		public unsafe static byte* stbi__resample_row_generic(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			for (int i = 0; i < w; i++)
			{
				for (int j = 0; j < hs; j++)
				{
					_out_[i * hs + j] = in_near[i];
				}
			}
			return _out_;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000CE10 File Offset: 0x0000B010
		public unsafe static void stbi__YCbCr_to_RGB_row(byte* _out_, byte* y, byte* pcb, byte* pcr, int count, int step)
		{
			for (int i = 0; i < count; i++)
			{
				int num = ((int)y[i] << 20) + 524288;
				int num2 = (int)(pcr[i] - 128);
				int num3 = (int)(pcb[i] - 128);
				int num4 = num + num2 * 1470208;
				int num5 = (int)((long)(num + num2 * -748800) + ((long)(num3 * -360960) & (long)((ulong)(-65536))));
				int num6 = num + num3 * 1858048;
				num4 >>= 20;
				num5 >>= 20;
				num6 >>= 20;
				if (num4 > 255)
				{
					if (num4 < 0)
					{
						num4 = 0;
					}
					else
					{
						num4 = 255;
					}
				}
				if (num5 > 255)
				{
					if (num5 < 0)
					{
						num5 = 0;
					}
					else
					{
						num5 = 255;
					}
				}
				if (num6 > 255)
				{
					if (num6 < 0)
					{
						num6 = 0;
					}
					else
					{
						num6 = 255;
					}
				}
				*_out_ = (byte)num4;
				_out_[1] = (byte)num5;
				_out_[2] = (byte)num6;
				_out_[3] = byte.MaxValue;
				_out_ += step;
			}
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000CEF8 File Offset: 0x0000B0F8
		public static void stbi__setup_jpeg(StbImage.stbi__jpeg j)
		{
			j.idct_block_kernel = new StbImage.idct_block_kernel(StbImage.stbi__idct_block);
			j.YCbCr_to_RGB_kernel = new StbImage.YCbCr_to_RGB_kernel(StbImage.stbi__YCbCr_to_RGB_row);
			j.resample_row_hv_2_kernel = new StbImage.Resampler(StbImage.stbi__resample_row_hv_2);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000CF30 File Offset: 0x0000B130
		public static void stbi__cleanup_jpeg(StbImage.stbi__jpeg j)
		{
			StbImage.stbi__free_jpeg_components(j, j.s.img_n, 0);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000CF45 File Offset: 0x0000B145
		public static byte stbi__blinn_8x8(byte x, byte y)
		{
			byte b = x * y + 128;
			return (byte)((uint)b + ((uint)b >> 8) >> 8);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000CF58 File Offset: 0x0000B158
		public unsafe static byte* load_jpeg_image(StbImage.stbi__jpeg z, int* out_x, int* out_y, int* comp, int req_comp)
		{
			z.s.img_n = 0;
			if (req_comp < 0 || req_comp > 4)
			{
				return (StbImage.stbi__err("bad req_comp") != 0) ? null : null;
			}
			if (StbImage.stbi__decode_jpeg_image(z) == 0)
			{
				StbImage.stbi__cleanup_jpeg(z);
				return null;
			}
			int num = ((req_comp != 0) ? req_comp : ((z.s.img_n >= 3) ? 3 : 1));
			int num2 = ((z.s.img_n == 3 && (z.rgb == 3 || (z.app14_color_transform == 0 && z.jfif == 0))) ? 1 : 0);
			int num3;
			if (z.s.img_n == 3 && num < 3 && num2 == 0)
			{
				num3 = 1;
			}
			else
			{
				num3 = z.s.img_n;
			}
			byte** ptr = stackalloc byte*[checked(unchecked((UIntPtr)4) * (UIntPtr)sizeof(byte*))];
			StbImage.stbi__resample[] array = new StbImage.stbi__resample[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new StbImage.stbi__resample();
			}
			for (int j = 0; j < num3; j++)
			{
				StbImage.stbi__resample stbi__resample = array[j];
				z.img_comp[j].linebuf = (byte*)StbImage.stbi__malloc((ulong)(z.s.img_x + 3U));
				if (z.img_comp[j].linebuf == null)
				{
					StbImage.stbi__cleanup_jpeg(z);
					return (StbImage.stbi__err("outofmem") != 0) ? null : null;
				}
				stbi__resample.hs = z.img_h_max / z.img_comp[j].h;
				stbi__resample.vs = z.img_v_max / z.img_comp[j].v;
				stbi__resample.ystep = stbi__resample.vs >> 1;
				stbi__resample.w_lores = (int)(((ulong)z.s.img_x + (ulong)((long)stbi__resample.hs) - 1UL) / (ulong)((long)stbi__resample.hs));
				stbi__resample.ypos = 0;
				stbi__resample.line0 = (stbi__resample.line1 = z.img_comp[j].data);
				if (stbi__resample.hs == 1 && stbi__resample.vs == 1)
				{
					stbi__resample.resample = new StbImage.Resampler(StbImage.resample_row_1);
				}
				else if (stbi__resample.hs == 1 && stbi__resample.vs == 2)
				{
					stbi__resample.resample = new StbImage.Resampler(StbImage.stbi__resample_row_v_2);
				}
				else if (stbi__resample.hs == 2 && stbi__resample.vs == 1)
				{
					stbi__resample.resample = new StbImage.Resampler(StbImage.stbi__resample_row_h_2);
				}
				else if (stbi__resample.hs == 2 && stbi__resample.vs == 2)
				{
					stbi__resample.resample = z.resample_row_hv_2_kernel;
				}
				else
				{
					stbi__resample.resample = new StbImage.Resampler(StbImage.stbi__resample_row_generic);
				}
			}
			byte* ptr2 = (byte*)StbImage.stbi__malloc_mad3(num, (int)z.s.img_x, (int)z.s.img_y, 1);
			if (ptr2 == null)
			{
				StbImage.stbi__cleanup_jpeg(z);
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			for (uint num4 = 0U; num4 < z.s.img_y; num4 += 1U)
			{
				byte* ptr3 = ptr2 + (long)num * (long)((ulong)z.s.img_x) * (long)((ulong)num4);
				for (int j = 0; j < num3; j++)
				{
					StbImage.stbi__resample stbi__resample2 = array[j];
					int num5 = ((stbi__resample2.ystep >= stbi__resample2.vs >> 1) ? 1 : 0);
					*(IntPtr*)(ptr + (IntPtr)j * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) = stbi__resample2.resample(z.img_comp[j].linebuf, (num5 != 0) ? stbi__resample2.line1 : stbi__resample2.line0, (num5 != 0) ? stbi__resample2.line0 : stbi__resample2.line1, stbi__resample2.w_lores, stbi__resample2.hs);
					StbImage.stbi__resample stbi__resample3 = stbi__resample2;
					int num6 = stbi__resample3.ystep + 1;
					stbi__resample3.ystep = num6;
					if (num6 >= stbi__resample2.vs)
					{
						stbi__resample2.ystep = 0;
						stbi__resample2.line0 = stbi__resample2.line1;
						StbImage.stbi__resample stbi__resample4 = stbi__resample2;
						num6 = stbi__resample4.ypos + 1;
						stbi__resample4.ypos = num6;
						if (num6 < z.img_comp[j].y)
						{
							stbi__resample2.line1 += z.img_comp[j].w2;
						}
					}
				}
				if (num >= 3)
				{
					byte* ptr4 = *(IntPtr*)ptr;
					if (z.s.img_n == 3)
					{
						if (num2 != 0)
						{
							for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
							{
								*ptr3 = ptr4[num7];
								ptr3[1] = *(*(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)) + (IntPtr)((UIntPtr)num7));
								ptr3[2] = *(*(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7));
								ptr3[3] = byte.MaxValue;
								ptr3 += num;
							}
						}
						else
						{
							z.YCbCr_to_RGB_kernel(ptr3, ptr4, *(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)), *(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)), (int)z.s.img_x, num);
						}
					}
					else if (z.s.img_n == 4)
					{
						if (z.app14_color_transform == 0)
						{
							for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
							{
								byte y = *(*(IntPtr*)(ptr + (IntPtr)3 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7));
								*ptr3 = StbImage.stbi__blinn_8x8(*(*(IntPtr*)ptr + (IntPtr)((UIntPtr)num7)), y);
								ptr3[1] = StbImage.stbi__blinn_8x8(*(*(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)) + (IntPtr)((UIntPtr)num7)), y);
								ptr3[2] = StbImage.stbi__blinn_8x8(*(*(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7)), y);
								ptr3[3] = byte.MaxValue;
								ptr3 += num;
							}
						}
						else if (z.app14_color_transform == 2)
						{
							z.YCbCr_to_RGB_kernel(ptr3, ptr4, *(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)), *(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)), (int)z.s.img_x, num);
							for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
							{
								byte y2 = *(*(IntPtr*)(ptr + (IntPtr)3 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7));
								*ptr3 = StbImage.stbi__blinn_8x8(byte.MaxValue - *ptr3, y2);
								ptr3[1] = StbImage.stbi__blinn_8x8(byte.MaxValue - ptr3[1], y2);
								ptr3[2] = StbImage.stbi__blinn_8x8(byte.MaxValue - ptr3[2], y2);
								ptr3 += num;
							}
						}
						else
						{
							z.YCbCr_to_RGB_kernel(ptr3, ptr4, *(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)), *(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)), (int)z.s.img_x, num);
						}
					}
					else
					{
						for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
						{
							*ptr3 = (ptr3[1] = (ptr3[2] = ptr4[num7]));
							ptr3[3] = byte.MaxValue;
							ptr3 += num;
						}
					}
				}
				else if (num2 != 0)
				{
					if (num == 1)
					{
						for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
						{
							*(ptr3++) = StbImage.stbi__compute_y((int)(*(*(IntPtr*)ptr + (IntPtr)((UIntPtr)num7))), (int)(*(*(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)) + (IntPtr)((UIntPtr)num7))), (int)(*(*(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7))));
						}
					}
					else
					{
						uint num7 = 0U;
						while (num7 < z.s.img_x)
						{
							*ptr3 = StbImage.stbi__compute_y((int)(*(*(IntPtr*)ptr + (IntPtr)((UIntPtr)num7))), (int)(*(*(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)) + (IntPtr)((UIntPtr)num7))), (int)(*(*(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7))));
							ptr3[1] = byte.MaxValue;
							num7 += 1U;
							ptr3 += 2;
						}
					}
				}
				else if (z.s.img_n == 4 && z.app14_color_transform == 0)
				{
					for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
					{
						byte y3 = *(*(IntPtr*)(ptr + (IntPtr)3 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7));
						byte r = StbImage.stbi__blinn_8x8(*(*(IntPtr*)ptr + (IntPtr)((UIntPtr)num7)), y3);
						byte g = StbImage.stbi__blinn_8x8(*(*(IntPtr*)(ptr + sizeof(byte*) / sizeof(byte*)) + (IntPtr)((UIntPtr)num7)), y3);
						byte b = StbImage.stbi__blinn_8x8(*(*(IntPtr*)(ptr + (IntPtr)2 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7)), y3);
						*ptr3 = StbImage.stbi__compute_y((int)r, (int)g, (int)b);
						ptr3[1] = byte.MaxValue;
						ptr3 += num;
					}
				}
				else if (z.s.img_n == 4 && z.app14_color_transform == 2)
				{
					for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
					{
						*ptr3 = StbImage.stbi__blinn_8x8(byte.MaxValue - *(*(IntPtr*)ptr + (IntPtr)((UIntPtr)num7)), *(*(IntPtr*)(ptr + (IntPtr)3 * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) + (IntPtr)((UIntPtr)num7)));
						ptr3[1] = byte.MaxValue;
						ptr3 += num;
					}
				}
				else
				{
					byte* ptr5 = *(IntPtr*)ptr;
					if (num == 1)
					{
						for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
						{
							ptr3[num7] = ptr5[num7];
						}
					}
					else
					{
						for (uint num7 = 0U; num7 < z.s.img_x; num7 += 1U)
						{
							*(ptr3++) = ptr5[num7];
							*(ptr3++) = byte.MaxValue;
						}
					}
				}
			}
			StbImage.stbi__cleanup_jpeg(z);
			*out_x = (int)z.s.img_x;
			*out_y = (int)z.s.img_y;
			if (comp != null)
			{
				*comp = ((z.s.img_n >= 3) ? 3 : 1);
			}
			return ptr2;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000D8B0 File Offset: 0x0000BAB0
		public unsafe static void* stbi__jpeg_load(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri)
		{
			StbImage.stbi__jpeg stbi__jpeg = new StbImage.stbi__jpeg();
			stbi__jpeg.s = s;
			StbImage.stbi__setup_jpeg(stbi__jpeg);
			return (void*)StbImage.load_jpeg_image(stbi__jpeg, x, y, comp, req_comp);
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000D8CE File Offset: 0x0000BACE
		public static int stbi__jpeg_test(StbImage.stbi__context s)
		{
			StbImage.stbi__jpeg stbi__jpeg = new StbImage.stbi__jpeg();
			stbi__jpeg.s = s;
			StbImage.stbi__setup_jpeg(stbi__jpeg);
			int result = StbImage.stbi__decode_jpeg_header(stbi__jpeg, 1);
			StbImage.stbi__rewind(s);
			return result;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000D8F0 File Offset: 0x0000BAF0
		public unsafe static int stbi__jpeg_info_raw(StbImage.stbi__jpeg j, int* x, int* y, int* comp)
		{
			if (StbImage.stbi__decode_jpeg_header(j, 2) == 0)
			{
				StbImage.stbi__rewind(j.s);
				return 0;
			}
			if (x != null)
			{
				*x = (int)j.s.img_x;
			}
			if (y != null)
			{
				*y = (int)j.s.img_y;
			}
			if (comp != null)
			{
				*comp = ((j.s.img_n >= 3) ? 3 : 1);
			}
			return 1;
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000D951 File Offset: 0x0000BB51
		public unsafe static int stbi__jpeg_info(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			return StbImage.stbi__jpeg_info_raw(new StbImage.stbi__jpeg
			{
				s = s
			}, x, y, comp);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000D968 File Offset: 0x0000BB68
		public static int stbi__bitreverse16(int n)
		{
			n = ((n & 43690) >> 1) | ((n & 21845) << 1);
			n = ((n & 52428) >> 2) | ((n & 13107) << 2);
			n = ((n & 61680) >> 4) | ((n & 3855) << 4);
			n = ((n & 65280) >> 8) | ((n & 255) << 8);
			return n;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000D9CA File Offset: 0x0000BBCA
		public static int stbi__bit_reverse(int v, int bits)
		{
			return StbImage.stbi__bitreverse16(v) >> 16 - bits;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000D9DC File Offset: 0x0000BBDC
		public unsafe static int stbi__zbuild_huffman(StbImage.stbi__zhuffman* z, byte* sizelist, int num)
		{
			int num2 = 0;
			int* ptr = stackalloc int[(UIntPtr)64];
			int* ptr2 = stackalloc int[(UIntPtr)68];
			CRuntime.memset((void*)ptr2, 0, 4UL);
			CRuntime.memset((void*)(&z->fast.FixedElementField), 0, 1024UL);
			for (int i = 0; i < num; i++)
			{
				ptr2[sizelist[i]]++;
			}
			*ptr2 = 0;
			for (int i = 1; i < 16; i++)
			{
				if (ptr2[i] > 1 << i)
				{
					return StbImage.stbi__err("bad sizes");
				}
			}
			int num3 = 0;
			for (int i = 1; i < 16; i++)
			{
				ptr[i] = num3;
				*((ref z->firstcode.FixedElementField) + (IntPtr)i * 2) = (ushort)num3;
				*((ref z->firstsymbol.FixedElementField) + (IntPtr)i * 2) = (ushort)num2;
				num3 += ptr2[i];
				if (ptr2[i] != 0 && num3 - 1 >= 1 << i)
				{
					return StbImage.stbi__err("bad codelengths");
				}
				*((ref z->maxcode.FixedElementField) + (IntPtr)i * 4) = num3 << 16 - i;
				num3 <<= 1;
				num2 += ptr2[i];
			}
			*((ref z->maxcode.FixedElementField) + (IntPtr)16 * 4) = 65536;
			for (int i = 0; i < num; i++)
			{
				int num4 = (int)sizelist[i];
				if (num4 != 0)
				{
					int num5 = ptr[num4] - (int)(*((ref z->firstcode.FixedElementField) + (IntPtr)num4 * 2)) + (int)(*((ref z->firstsymbol.FixedElementField) + (IntPtr)num4 * 2));
					ushort num6 = (ushort)((num4 << 9) | i);
					*((ref z->size.FixedElementField) + num5) = (byte)num4;
					*((ref z->value.FixedElementField) + (IntPtr)num5 * 2) = (ushort)i;
					if (num4 <= 9)
					{
						for (int j = StbImage.stbi__bit_reverse(ptr[num4], num4); j < 512; j += 1 << num4)
						{
							*((ref z->fast.FixedElementField) + (IntPtr)j * 2) = num6;
						}
					}
					ptr[num4]++;
				}
			}
			return 1;
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000DBE0 File Offset: 0x0000BDE0
		public unsafe static byte stbi__zget8(StbImage.stbi__zbuf* z)
		{
			if (z->zbuffer >= z->zbuffer_end)
			{
				return 0;
			}
			byte* zbuffer = z->zbuffer;
			z->zbuffer = zbuffer + 1;
			return *zbuffer;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000DC0C File Offset: 0x0000BE0C
		public unsafe static void stbi__fill_bits(StbImage.stbi__zbuf* z)
		{
			do
			{
				z->code_buffer = z->code_buffer | (uint)((uint)StbImage.stbi__zget8(z) << z->num_bits);
				z->num_bits = z->num_bits + 8;
			}
			while (z->num_bits <= 24);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000DC3D File Offset: 0x0000BE3D
		public unsafe static uint stbi__zreceive(StbImage.stbi__zbuf* z, int n)
		{
			if (z->num_bits < n)
			{
				StbImage.stbi__fill_bits(z);
			}
			uint result = (uint)((ulong)z->code_buffer & (ulong)((long)((1 << n) - 1)));
			z->code_buffer = z->code_buffer >> n;
			z->num_bits = z->num_bits - n;
			return result;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000DC7C File Offset: 0x0000BE7C
		public unsafe static int stbi__zhuffman_decode_slowpath(StbImage.stbi__zbuf* a, StbImage.stbi__zhuffman* z)
		{
			int i = StbImage.stbi__bit_reverse((int)a->code_buffer, 16);
			int num = 10;
			while (i >= *((ref z->maxcode.FixedElementField) + (IntPtr)num * 4))
			{
				num++;
			}
			if (num == 16)
			{
				return -1;
			}
			int num2 = (i >> 16 - num) - (int)(*((ref z->firstcode.FixedElementField) + (IntPtr)num * 2)) + (int)(*((ref z->firstsymbol.FixedElementField) + (IntPtr)num * 2));
			a->code_buffer = a->code_buffer >> num;
			a->num_bits = a->num_bits - num;
			return (int)(*((ref z->value.FixedElementField) + (IntPtr)num2 * 2));
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000DD14 File Offset: 0x0000BF14
		public unsafe static int stbi__zhuffman_decode(StbImage.stbi__zbuf* a, StbImage.stbi__zhuffman* z)
		{
			if (a->num_bits < 16)
			{
				StbImage.stbi__fill_bits(a);
			}
			int num = (int)(*((ref z->fast.FixedElementField) + (IntPtr)((ulong)(a->code_buffer & 511U) * 2UL)));
			if (num != 0)
			{
				int num2 = num >> 9;
				a->code_buffer = a->code_buffer >> num2;
				a->num_bits = a->num_bits - num2;
				return num & 511;
			}
			return StbImage.stbi__zhuffman_decode_slowpath(a, z);
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000DD80 File Offset: 0x0000BF80
		public unsafe static int stbi__zexpand(StbImage.stbi__zbuf* z, sbyte* zout, int n)
		{
			z->zout = zout;
			if (z->z_expandable == 0)
			{
				return StbImage.stbi__err("output buffer limit");
			}
			int num = (int)((long)(z->zout - z->zout_start));
			int num2 = (int)((long)(z->zout_end - z->zout_start));
			while (num + n > num2)
			{
				num2 *= 2;
			}
			sbyte* ptr = (sbyte*)CRuntime.realloc((void*)z->zout_start, (ulong)((long)num2));
			if (ptr == null)
			{
				return StbImage.stbi__err("outofmem");
			}
			z->zout_start = ptr;
			z->zout = ptr + num;
			z->zout_end = ptr + num2;
			return 1;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000DE10 File Offset: 0x0000C010
		public unsafe static int stbi__parse_huffman_block(StbImage.stbi__zbuf* a)
		{
			sbyte* zout = a->zout;
			for (;;)
			{
				int num = StbImage.stbi__zhuffman_decode(a, &a->z_length);
				if (num < 256)
				{
					if (num < 0)
					{
						break;
					}
					if (zout >= a->zout_end)
					{
						if (StbImage.stbi__zexpand(a, zout, 1) == 0)
						{
							return 0;
						}
						zout = a->zout;
					}
					*(zout++) = (sbyte)num;
				}
				else
				{
					if (num == 256)
					{
						goto Block_5;
					}
					num -= 257;
					int num2 = StbImage.stbi__zlength_base[num];
					if (StbImage.stbi__zlength_extra[num] != 0)
					{
						num2 += (int)StbImage.stbi__zreceive(a, StbImage.stbi__zlength_extra[num]);
					}
					num = StbImage.stbi__zhuffman_decode(a, &a->z_distance);
					if (num < 0)
					{
						goto Block_7;
					}
					int num3 = StbImage.stbi__zdist_base[num];
					if (StbImage.stbi__zdist_extra[num] != 0)
					{
						num3 += (int)StbImage.stbi__zreceive(a, StbImage.stbi__zdist_extra[num]);
					}
					if ((long)(zout - a->zout_start) < (long)num3)
					{
						goto Block_9;
					}
					if (zout + num2 != a->zout_end)
					{
						if (StbImage.stbi__zexpand(a, zout, num2) == 0)
						{
							return 0;
						}
						zout = a->zout;
					}
					byte* ptr = (byte*)(zout - num3);
					if (num3 == 1)
					{
						byte b = *ptr;
						if (num2 != 0)
						{
							do
							{
								*(zout++) = (sbyte)b;
							}
							while (--num2 != 0);
						}
					}
					else if (num2 != 0)
					{
						do
						{
							*(zout++) = (sbyte)(*(ptr++));
						}
						while (--num2 != 0);
					}
				}
			}
			return StbImage.stbi__err("bad huffman code");
			Block_5:
			a->zout = zout;
			return 1;
			Block_7:
			return StbImage.stbi__err("bad huffman code");
			Block_9:
			return StbImage.stbi__err("bad dist");
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000DF6C File Offset: 0x0000C16C
		public unsafe static int stbi__compute_huffman_codes(StbImage.stbi__zbuf* a)
		{
			StbImage.stbi__zhuffman stbi__zhuffman = default(StbImage.stbi__zhuffman);
			byte* ptr = stackalloc byte[(UIntPtr)455];
			byte* ptr2 = stackalloc byte[(UIntPtr)19];
			int num = (int)(StbImage.stbi__zreceive(a, 5) + 257U);
			int num2 = (int)(StbImage.stbi__zreceive(a, 5) + 1U);
			int num3 = (int)(StbImage.stbi__zreceive(a, 4) + 4U);
			int num4 = num + num2;
			CRuntime.memset((void*)ptr2, 0, 19UL);
			for (int i = 0; i < num3; i++)
			{
				int num5 = (int)StbImage.stbi__zreceive(a, 3);
				ptr2[StbImage.length_dezigzag[i]] = (byte)num5;
			}
			if (StbImage.stbi__zbuild_huffman(&stbi__zhuffman, ptr2, 19) == 0)
			{
				return 0;
			}
			int j = 0;
			while (j < num4)
			{
				int num6 = StbImage.stbi__zhuffman_decode(a, &stbi__zhuffman);
				if (num6 < 0 || num6 >= 19)
				{
					return StbImage.stbi__err("bad codelengths");
				}
				if (num6 < 16)
				{
					ptr[j++] = (byte)num6;
				}
				else
				{
					byte value = 0;
					if (num6 == 16)
					{
						num6 = (int)(StbImage.stbi__zreceive(a, 2) + 3U);
						if (j == 0)
						{
							return StbImage.stbi__err("bad codelengths");
						}
						value = ptr[j - 1];
					}
					else if (num6 == 17)
					{
						num6 = (int)(StbImage.stbi__zreceive(a, 3) + 3U);
					}
					else
					{
						num6 = (int)(StbImage.stbi__zreceive(a, 7) + 11U);
					}
					if (num4 - j < num6)
					{
						return StbImage.stbi__err("bad codelengths");
					}
					CRuntime.memset((void*)(ptr + j), (int)value, (ulong)((long)num6));
					j += num6;
				}
			}
			if (j != num4)
			{
				return StbImage.stbi__err("bad codelengths");
			}
			if (StbImage.stbi__zbuild_huffman(&a->z_length, ptr, num) == 0)
			{
				return 0;
			}
			if (StbImage.stbi__zbuild_huffman(&a->z_distance, ptr + num, num2) == 0)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000E0F0 File Offset: 0x0000C2F0
		public unsafe static int stbi__parse_uncompressed_block(StbImage.stbi__zbuf* a)
		{
			byte* ptr = stackalloc byte[(UIntPtr)4];
			if ((a->num_bits & 7) != 0)
			{
				StbImage.stbi__zreceive(a, a->num_bits & 7);
			}
			int i = 0;
			while (a->num_bits > 0)
			{
				ptr[i++] = (byte)(a->code_buffer & 255U);
				a->code_buffer = a->code_buffer >> 8;
				a->num_bits = a->num_bits - 8;
			}
			while (i < 4)
			{
				ptr[i++] = StbImage.stbi__zget8(a);
			}
			int num = (int)ptr[1] * 256 + (int)(*ptr);
			if ((int)ptr[3] * 256 + (int)ptr[2] != (num ^ 65535))
			{
				return StbImage.stbi__err("zlib corrupt");
			}
			if (a->zbuffer + num != a->zbuffer_end)
			{
				return StbImage.stbi__err("read past buffer");
			}
			if (a->zout + num != a->zout_end && StbImage.stbi__zexpand(a, a->zout, num) == 0)
			{
				return 0;
			}
			CRuntime.memcpy((void*)a->zout, (void*)a->zbuffer, (ulong)((long)num));
			a->zbuffer = a->zbuffer + num;
			a->zout = a->zout + num;
			return 1;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000E200 File Offset: 0x0000C400
		public unsafe static int stbi__parse_zlib_header(StbImage.stbi__zbuf* a)
		{
			byte b = StbImage.stbi__zget8(a);
			int num = (int)(b & 15);
			int num2 = (int)StbImage.stbi__zget8(a);
			if (((int)b * 256 + num2) % 31 != 0)
			{
				return StbImage.stbi__err("bad zlib header");
			}
			if ((num2 & 32) != 0)
			{
				return StbImage.stbi__err("no preset dict");
			}
			if (num != 8)
			{
				return StbImage.stbi__err("bad compression");
			}
			return 1;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000E258 File Offset: 0x0000C458
		public unsafe static int stbi__parse_zlib(StbImage.stbi__zbuf* a, int parse_header)
		{
			if (parse_header != 0 && StbImage.stbi__parse_zlib_header(a) == 0)
			{
				return 0;
			}
			a->num_bits = 0;
			a->code_buffer = 0U;
			for (;;)
			{
				int num = (int)StbImage.stbi__zreceive(a, 1);
				int num2 = (int)StbImage.stbi__zreceive(a, 2);
				if (num2 == 0)
				{
					if (StbImage.stbi__parse_uncompressed_block(a) == 0)
					{
						break;
					}
				}
				else
				{
					if (num2 == 3)
					{
						return 0;
					}
					if (num2 == 1)
					{
						byte[] array;
						byte* sizelist;
						if ((array = StbImage.stbi__zdefault_length) == null || array.Length == 0)
						{
							sizelist = null;
						}
						else
						{
							sizelist = &array[0];
						}
						if (StbImage.stbi__zbuild_huffman(&a->z_length, sizelist, 288) == 0)
						{
							return 0;
						}
						array = null;
						byte* sizelist2;
						if ((array = StbImage.stbi__zdefault_distance) == null || array.Length == 0)
						{
							sizelist2 = null;
						}
						else
						{
							sizelist2 = &array[0];
						}
						if (StbImage.stbi__zbuild_huffman(&a->z_distance, sizelist2, 32) == 0)
						{
							return 0;
						}
						array = null;
					}
					else if (StbImage.stbi__compute_huffman_codes(a) == 0)
					{
						return 0;
					}
					if (StbImage.stbi__parse_huffman_block(a) == 0)
					{
						return 0;
					}
				}
				if (num != 0)
				{
					return 1;
				}
			}
			return 0;
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000E32F File Offset: 0x0000C52F
		public unsafe static int stbi__do_zlib(StbImage.stbi__zbuf* a, sbyte* obuf, int olen, int exp, int parse_header)
		{
			a->zout_start = obuf;
			a->zout = obuf;
			a->zout_end = obuf + olen;
			a->z_expandable = exp;
			return StbImage.stbi__parse_zlib(a, parse_header);
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000E358 File Offset: 0x0000C558
		public unsafe static sbyte* stbi_zlib_decode_malloc_guesssize(sbyte* buffer, int len, int initial_size, int* outlen)
		{
			StbImage.stbi__zbuf stbi__zbuf = default(StbImage.stbi__zbuf);
			sbyte* ptr = (sbyte*)StbImage.stbi__malloc((ulong)((long)initial_size));
			if (ptr == null)
			{
				return null;
			}
			stbi__zbuf.zbuffer = (byte*)buffer;
			stbi__zbuf.zbuffer_end = (byte*)(buffer + len);
			if (StbImage.stbi__do_zlib(&stbi__zbuf, ptr, initial_size, 1, 1) != 0)
			{
				if (outlen != null)
				{
					*outlen = (int)((long)(stbi__zbuf.zout - stbi__zbuf.zout_start));
				}
				return stbi__zbuf.zout_start;
			}
			CRuntime.free((void*)stbi__zbuf.zout_start);
			return null;
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000E3C9 File Offset: 0x0000C5C9
		public unsafe static sbyte* stbi_zlib_decode_malloc(sbyte* buffer, int len, int* outlen)
		{
			return StbImage.stbi_zlib_decode_malloc_guesssize(buffer, len, 16384, outlen);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x0000E3D8 File Offset: 0x0000C5D8
		public unsafe static sbyte* stbi_zlib_decode_malloc_guesssize_headerflag(sbyte* buffer, int len, int initial_size, int* outlen, int parse_header)
		{
			StbImage.stbi__zbuf stbi__zbuf = default(StbImage.stbi__zbuf);
			sbyte* ptr = (sbyte*)StbImage.stbi__malloc((ulong)((long)initial_size));
			if (ptr == null)
			{
				return null;
			}
			stbi__zbuf.zbuffer = (byte*)buffer;
			stbi__zbuf.zbuffer_end = (byte*)(buffer + len);
			if (StbImage.stbi__do_zlib(&stbi__zbuf, ptr, initial_size, 1, parse_header) != 0)
			{
				if (outlen != null)
				{
					*outlen = (int)((long)(stbi__zbuf.zout - stbi__zbuf.zout_start));
				}
				return stbi__zbuf.zout_start;
			}
			CRuntime.free((void*)stbi__zbuf.zout_start);
			return null;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000E44C File Offset: 0x0000C64C
		public unsafe static int stbi_zlib_decode_buffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			StbImage.stbi__zbuf stbi__zbuf = default(StbImage.stbi__zbuf);
			stbi__zbuf.zbuffer = (byte*)ibuffer;
			stbi__zbuf.zbuffer_end = (byte*)(ibuffer + ilen);
			if (StbImage.stbi__do_zlib(&stbi__zbuf, obuffer, olen, 0, 1) != 0)
			{
				return (int)((long)(stbi__zbuf.zout - stbi__zbuf.zout_start));
			}
			return -1;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000E494 File Offset: 0x0000C694
		public unsafe static sbyte* stbi_zlib_decode_noheader_malloc(sbyte* buffer, int len, int* outlen)
		{
			StbImage.stbi__zbuf stbi__zbuf = default(StbImage.stbi__zbuf);
			sbyte* ptr = (sbyte*)StbImage.stbi__malloc(16384UL);
			if (ptr == null)
			{
				return null;
			}
			stbi__zbuf.zbuffer = (byte*)buffer;
			stbi__zbuf.zbuffer_end = (byte*)(buffer + len);
			if (StbImage.stbi__do_zlib(&stbi__zbuf, ptr, 16384, 1, 0) != 0)
			{
				if (outlen != null)
				{
					*outlen = (int)((long)(stbi__zbuf.zout - stbi__zbuf.zout_start));
				}
				return stbi__zbuf.zout_start;
			}
			CRuntime.free((void*)stbi__zbuf.zout_start);
			return null;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000E510 File Offset: 0x0000C710
		public unsafe static int stbi_zlib_decode_noheader_buffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			StbImage.stbi__zbuf stbi__zbuf = default(StbImage.stbi__zbuf);
			stbi__zbuf.zbuffer = (byte*)ibuffer;
			stbi__zbuf.zbuffer_end = (byte*)(ibuffer + ilen);
			if (StbImage.stbi__do_zlib(&stbi__zbuf, obuffer, olen, 0, 0) != 0)
			{
				return (int)((long)(stbi__zbuf.zout - stbi__zbuf.zout_start));
			}
			return -1;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000E558 File Offset: 0x0000C758
		public static StbImage.stbi__pngchunk stbi__get_chunk_header(StbImage.stbi__context s)
		{
			return new StbImage.stbi__pngchunk
			{
				length = StbImage.stbi__get32be(s),
				type = StbImage.stbi__get32be(s)
			};
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000E588 File Offset: 0x0000C788
		public static int stbi__check_png_header(StbImage.stbi__context s)
		{
			for (int i = 0; i < 8; i++)
			{
				if (StbImage.stbi__get8(s) != StbImage.png_sig[i])
				{
					return StbImage.stbi__err("bad png sig");
				}
			}
			return 1;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000E5BC File Offset: 0x0000C7BC
		public static int stbi__paeth(int a, int b, int c)
		{
			int num = a + b - c;
			int num2 = CRuntime.abs(num - a);
			int num3 = CRuntime.abs(num - b);
			int num4 = CRuntime.abs(num - c);
			if (num2 <= num3 && num2 <= num4)
			{
				return a;
			}
			if (num3 <= num4)
			{
				return b;
			}
			return c;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000E5FC File Offset: 0x0000C7FC
		public unsafe static int stbi__create_png_image_raw(StbImage.stbi__png a, byte* raw, uint raw_len, int out_n, uint x, uint y, int depth, int color)
		{
			int num = ((depth == 16) ? 2 : 1);
			StbImage.stbi__context s = a.s;
			uint num2 = (uint)((ulong)x * (ulong)((long)out_n) * (ulong)((long)num));
			int img_n = s.img_n;
			int num3 = out_n * num;
			int num4 = img_n * num;
			int num5 = (int)x;
			a._out_ = (byte*)StbImage.stbi__malloc_mad3((int)x, (int)y, num3, 0);
			if (a._out_ == null)
			{
				return StbImage.stbi__err("outofmem");
			}
			uint num6 = (uint)((long)img_n * (long)((ulong)x) * (long)depth + 7L >> 3);
			uint num7 = (num6 + 1U) * y;
			if (raw_len < num7)
			{
				return StbImage.stbi__err("not enough pixels");
			}
			for (uint num8 = 0U; num8 < y; num8 += 1U)
			{
				byte* ptr = a._out_ + num2 * num8;
				int num9 = (int)(*(raw++));
				if (num9 > 4)
				{
					return StbImage.stbi__err("invalid filter");
				}
				if (depth < 8)
				{
					ptr += (ulong)x * (ulong)((long)out_n) - (ulong)num6;
					num4 = 1;
					num5 = (int)num6;
				}
				byte* ptr2 = ptr - num2;
				if (num8 == 0U)
				{
					num9 = (int)StbImage.first_row_filter[num9];
				}
				for (int i = 0; i < num4; i++)
				{
					switch (num9)
					{
					case 0:
						ptr[i] = raw[i];
						break;
					case 1:
						ptr[i] = raw[i];
						break;
					case 2:
						ptr[i] = (raw[i] + ptr2[i]) & byte.MaxValue;
						break;
					case 3:
						ptr[i] = (byte)(((int)raw[i] + (ptr2[i] >> 1)) & 255);
						break;
					case 4:
						ptr[i] = (byte)(((int)raw[i] + StbImage.stbi__paeth(0, (int)ptr2[i], 0)) & 255);
						break;
					case 5:
						ptr[i] = raw[i];
						break;
					case 6:
						ptr[i] = raw[i];
						break;
					}
				}
				if (depth == 8)
				{
					if (img_n != out_n)
					{
						ptr[img_n] = byte.MaxValue;
					}
					raw += img_n;
					ptr += out_n;
					ptr2 += out_n;
				}
				else if (depth == 16)
				{
					if (img_n != out_n)
					{
						ptr[num4] = byte.MaxValue;
						ptr[num4 + 1] = byte.MaxValue;
					}
					raw += num4;
					ptr += num3;
					ptr2 += num3;
				}
				else
				{
					raw++;
					ptr++;
					ptr2++;
				}
				if (depth < 8 || img_n == out_n)
				{
					int num10 = (num5 - 1) * num4;
					switch (num9)
					{
					case 0:
						CRuntime.memcpy((void*)ptr, (void*)raw, (ulong)((long)num10));
						break;
					case 1:
						for (int i = 0; i < num10; i++)
						{
							ptr[i] = (raw[i] + ptr[i - num4]) & byte.MaxValue;
						}
						break;
					case 2:
						for (int i = 0; i < num10; i++)
						{
							ptr[i] = (raw[i] + ptr2[i]) & byte.MaxValue;
						}
						break;
					case 3:
						for (int i = 0; i < num10; i++)
						{
							ptr[i] = (byte)(((int)raw[i] + (ptr2[i] + ptr[i - num4] >> 1)) & 255);
						}
						break;
					case 4:
						for (int i = 0; i < num10; i++)
						{
							ptr[i] = (byte)(((int)raw[i] + StbImage.stbi__paeth((int)ptr[i - num4], (int)ptr2[i], (int)ptr2[i - num4])) & 255);
						}
						break;
					case 5:
						for (int i = 0; i < num10; i++)
						{
							ptr[i] = (byte)(((int)raw[i] + (ptr[i - num4] >> 1)) & 255);
						}
						break;
					case 6:
						for (int i = 0; i < num10; i++)
						{
							ptr[i] = (byte)(((int)raw[i] + StbImage.stbi__paeth((int)ptr[i - num4], 0, 0)) & 255);
						}
						break;
					}
					raw += num10;
				}
				else
				{
					switch (num9)
					{
					case 0:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = raw[i];
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					case 1:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = (raw[i] + ptr[i - num3]) & byte.MaxValue;
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					case 2:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = (raw[i] + ptr2[i]) & byte.MaxValue;
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					case 3:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = (byte)(((int)raw[i] + (ptr2[i] + ptr[i - num3] >> 1)) & 255);
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					case 4:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = (byte)(((int)raw[i] + StbImage.stbi__paeth((int)ptr[i - num3], (int)ptr2[i], (int)ptr2[i - num3])) & 255);
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					case 5:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = (byte)(((int)raw[i] + (ptr[i - num3] >> 1)) & 255);
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					case 6:
					{
						uint num11 = x - 1U;
						while (num11 >= 1U)
						{
							for (int i = 0; i < num4; i++)
							{
								ptr[i] = (byte)(((int)raw[i] + StbImage.stbi__paeth((int)ptr[i - num3], 0, 0)) & 255);
							}
							num11 -= 1U;
							ptr[num4] = byte.MaxValue;
							raw += num4;
							ptr += num3;
							ptr2 += num3;
						}
						break;
					}
					}
					if (depth == 16)
					{
						ptr = a._out_ + num2 * num8;
						uint num11 = 0U;
						while (num11 < x)
						{
							ptr[num4 + 1] = byte.MaxValue;
							num11 += 1U;
							ptr += num3;
						}
					}
				}
			}
			if (depth < 8)
			{
				for (uint num8 = 0U; num8 < y; num8 += 1U)
				{
					byte* ptr3 = a._out_ + num2 * num8;
					byte* ptr4 = a._out_ + num2 * num8 + (ulong)x * (ulong)((long)out_n) - num6;
					byte b = ((color == 0) ? StbImage.stbi__depth_scale_table[depth] : 1);
					if (depth == 4)
					{
						int i = (int)((ulong)x * (ulong)((long)img_n));
						while (i >= 2)
						{
							*(ptr3++) = (byte)((int)b * (*ptr4 >> 4));
							*(ptr3++) = b * (*ptr4 & 15);
							i -= 2;
							ptr4++;
						}
						if (i > 0)
						{
							*(ptr3++) = (byte)((int)b * (*ptr4 >> 4));
						}
					}
					else if (depth == 2)
					{
						int i = (int)((ulong)x * (ulong)((long)img_n));
						while (i >= 4)
						{
							*(ptr3++) = (byte)((int)b * (*ptr4 >> 6));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 4) & 3));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 2) & 3));
							*(ptr3++) = b * (*ptr4 & 3);
							i -= 4;
							ptr4++;
						}
						if (i > 0)
						{
							*(ptr3++) = (byte)((int)b * (*ptr4 >> 6));
						}
						if (i > 1)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 4) & 3));
						}
						if (i > 2)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 2) & 3));
						}
					}
					else if (depth == 1)
					{
						int i = (int)((ulong)x * (ulong)((long)img_n));
						while (i >= 8)
						{
							*(ptr3++) = (byte)((int)b * (*ptr4 >> 7));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 6) & 1));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 5) & 1));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 4) & 1));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 3) & 1));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 2) & 1));
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 1) & 1));
							*(ptr3++) = b * (*ptr4 & 1);
							i -= 8;
							ptr4++;
						}
						if (i > 0)
						{
							*(ptr3++) = (byte)((int)b * (*ptr4 >> 7));
						}
						if (i > 1)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 6) & 1));
						}
						if (i > 2)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 5) & 1));
						}
						if (i > 3)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 4) & 1));
						}
						if (i > 4)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 3) & 1));
						}
						if (i > 5)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 2) & 1));
						}
						if (i > 6)
						{
							*(ptr3++) = (byte)((int)b * ((*ptr4 >> 1) & 1));
						}
					}
					if (img_n != out_n)
					{
						ptr3 = a._out_ + num2 * num8;
						if (img_n == 1)
						{
							for (int j = (int)(x - 1U); j >= 0; j--)
							{
								ptr3[j * 2 + 1] = byte.MaxValue;
								ptr3[j * 2] = ptr3[j];
							}
						}
						else
						{
							for (int j = (int)(x - 1U); j >= 0; j--)
							{
								ptr3[j * 4 + 3] = byte.MaxValue;
								ptr3[j * 4 + 2] = ptr3[j * 3 + 2];
								ptr3[j * 4 + 1] = ptr3[j * 3 + 1];
								ptr3[j * 4] = ptr3[j * 3];
							}
						}
					}
				}
			}
			else if (depth == 16)
			{
				byte* ptr5 = a._out_;
				ushort* ptr6 = (ushort*)ptr5;
				uint num11 = 0U;
				while ((ulong)num11 < (ulong)(x * y) * (ulong)((long)out_n))
				{
					*ptr6 = (ushort)(((int)(*ptr5) << 8) | (int)ptr5[1]);
					num11 += 1U;
					ptr6++;
					ptr5 += 2;
				}
			}
			return 1;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000F09C File Offset: 0x0000D29C
		public unsafe static int stbi__create_png_image(StbImage.stbi__png a, byte* image_data, uint image_data_len, int out_n, int depth, int color, int interlaced)
		{
			int num = ((depth == 16) ? 2 : 1);
			int num2 = out_n * num;
			if (interlaced == 0)
			{
				return StbImage.stbi__create_png_image_raw(a, image_data, image_data_len, out_n, a.s.img_x, a.s.img_y, depth, color);
			}
			byte* ptr = (byte*)StbImage.stbi__malloc_mad3((int)a.s.img_x, (int)a.s.img_y, num2, 0);
			for (int i = 0; i < 7; i++)
			{
				int* ptr2 = stackalloc int[(UIntPtr)28];
				*ptr2 = 0;
				ptr2[1] = 4;
				ptr2[2] = 0;
				ptr2[3] = 2;
				ptr2[4] = 0;
				ptr2[5] = 1;
				ptr2[6] = 0;
				int* ptr3 = stackalloc int[(UIntPtr)28];
				*ptr3 = 0;
				ptr3[1] = 0;
				ptr3[2] = 4;
				ptr3[3] = 0;
				ptr3[4] = 2;
				ptr3[5] = 0;
				ptr3[6] = 1;
				int* ptr4 = stackalloc int[(UIntPtr)28];
				*ptr4 = 8;
				ptr4[1] = 8;
				ptr4[2] = 4;
				ptr4[3] = 4;
				ptr4[4] = 2;
				ptr4[5] = 2;
				ptr4[6] = 1;
				int* ptr5 = stackalloc int[(UIntPtr)28];
				*ptr5 = 8;
				ptr5[1] = 8;
				ptr5[2] = 8;
				ptr5[3] = 4;
				ptr5[4] = 4;
				ptr5[5] = 2;
				ptr5[6] = 2;
				int num3 = (int)(((ulong)a.s.img_x - (ulong)((long)ptr2[i]) + (ulong)((long)ptr4[i]) - 1UL) / (ulong)((long)ptr4[i]));
				int num4 = (int)(((ulong)a.s.img_y - (ulong)((long)ptr3[i]) + (ulong)((long)ptr5[i]) - 1UL) / (ulong)((long)ptr5[i]));
				if (num3 != 0 && num4 != 0)
				{
					uint num5 = (uint)(((a.s.img_n * num3 * depth + 7 >> 3) + 1) * num4);
					if (StbImage.stbi__create_png_image_raw(a, image_data, image_data_len, out_n, (uint)num3, (uint)num4, depth, color) == 0)
					{
						CRuntime.free((void*)ptr);
						return 0;
					}
					for (int j = 0; j < num4; j++)
					{
						for (int k = 0; k < num3; k++)
						{
							int num6 = j * ptr5[i] + ptr3[i];
							int num7 = k * ptr4[i] + ptr2[i];
							CRuntime.memcpy((void*)(ptr + (long)num6 * (long)((ulong)a.s.img_x) * (long)num2 + num7 * num2), (void*)(a._out_ + (j * num3 + k) * num2), (ulong)((long)num2));
						}
					}
					CRuntime.free((void*)a._out_);
					image_data += num5;
					image_data_len -= num5;
				}
			}
			a._out_ = ptr;
			return 1;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000F350 File Offset: 0x0000D550
		public unsafe static int stbi__compute_transparency(StbImage.stbi__png z, byte* tc, int out_n)
		{
			StbImage.stbi__context s = z.s;
			uint num = s.img_x * s.img_y;
			byte* ptr = z._out_;
			if (out_n == 2)
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					ptr[1] = ((*ptr == *tc) ? 0 : byte.MaxValue);
					ptr += 2;
				}
			}
			else
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					if (*ptr == *tc && ptr[1] == tc[1] && ptr[2] == tc[2])
					{
						ptr[3] = 0;
					}
					ptr += 4;
				}
			}
			return 1;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000F3D4 File Offset: 0x0000D5D4
		public unsafe static int stbi__compute_transparency16(StbImage.stbi__png z, ushort* tc, int out_n)
		{
			StbImage.stbi__context s = z.s;
			uint num = s.img_x * s.img_y;
			ushort* ptr = (ushort*)z._out_;
			if (out_n == 2)
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					ptr[1] = ((*ptr == *tc) ? 0 : ushort.MaxValue);
					ptr += 2;
				}
			}
			else
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					if (*ptr == *tc && ptr[1] == tc[1] && ptr[2] == tc[2])
					{
						ptr[3] = 0;
					}
					ptr += 4;
				}
			}
			return 1;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000F468 File Offset: 0x0000D668
		public unsafe static int stbi__expand_png_palette(StbImage.stbi__png a, byte* palette, int len, int pal_img_n)
		{
			uint num = a.s.img_x * a.s.img_y;
			byte* out_ = a._out_;
			byte* ptr = (byte*)StbImage.stbi__malloc_mad2((int)num, pal_img_n, 0);
			if (ptr == null)
			{
				return StbImage.stbi__err("outofmem");
			}
			byte* out_2 = ptr;
			if (pal_img_n == 3)
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					int num3 = (int)(out_[num2] * 4);
					*ptr = palette[num3];
					ptr[1] = palette[num3 + 1];
					ptr[2] = palette[num3 + 2];
					ptr += 3;
				}
			}
			else
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					int num4 = (int)(out_[num2] * 4);
					*ptr = palette[num4];
					ptr[1] = palette[num4 + 1];
					ptr[2] = palette[num4 + 2];
					ptr[3] = palette[num4 + 3];
					ptr += 4;
				}
			}
			CRuntime.free((void*)a._out_);
			a._out_ = out_2;
			return 1;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000F542 File Offset: 0x0000D742
		public static void stbi_set_unpremultiply_on_load(int flag_true_if_should_unpremultiply)
		{
			StbImage.stbi__unpremultiply_on_load = flag_true_if_should_unpremultiply;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000F54A File Offset: 0x0000D74A
		public static void stbi_convert_iphone_png_to_rgb(int flag_true_if_should_convert)
		{
			StbImage.stbi__de_iphone_flag = flag_true_if_should_convert;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000F554 File Offset: 0x0000D754
		public unsafe static void stbi__de_iphone(StbImage.stbi__png z)
		{
			StbImage.stbi__context s = z.s;
			uint num = s.img_x * s.img_y;
			byte* ptr = z._out_;
			if (s.img_out_n == 3)
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					byte b = *ptr;
					*ptr = ptr[2];
					ptr[2] = b;
					ptr += 3;
				}
				return;
			}
			if (StbImage.stbi__unpremultiply_on_load != 0)
			{
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					byte b2 = ptr[3];
					byte b3 = *ptr;
					if (b2 != 0)
					{
						byte b4 = b2 / 2;
						*ptr = (ptr[2] * byte.MaxValue + b4) / b2;
						ptr[1] = (ptr[1] * byte.MaxValue + b4) / b2;
						ptr[2] = (b3 * byte.MaxValue + b4) / b2;
					}
					else
					{
						*ptr = ptr[2];
						ptr[2] = b3;
					}
					ptr += 4;
				}
				return;
			}
			for (uint num2 = 0U; num2 < num; num2 += 1U)
			{
				byte b5 = *ptr;
				*ptr = ptr[2];
				ptr[2] = b5;
				ptr += 4;
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000F640 File Offset: 0x0000D840
		public unsafe static int stbi__parse_png_file(StbImage.stbi__png z, int scan, int req_comp)
		{
			byte* ptr = stackalloc byte[(UIntPtr)1024];
			byte b = 0;
			byte b2 = 0;
			byte* ptr2 = stackalloc byte[(UIntPtr)3];
			ushort* ptr3 = stackalloc ushort[(UIntPtr)6];
			uint num = 0U;
			uint num2 = 0U;
			uint num3 = 0U;
			int num4 = 1;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			StbImage.stbi__context s = z.s;
			z.expanded = null;
			z.idata = null;
			z._out_ = null;
			if (StbImage.stbi__check_png_header(s) == 0)
			{
				return 0;
			}
			if (scan == 1)
			{
				return 1;
			}
			for (;;)
			{
				StbImage.stbi__pngchunk stbi__pngchunk = StbImage.stbi__get_chunk_header(s);
				uint type = stbi__pngchunk.type;
				if (type <= 1229278788U)
				{
					if (type != 1130840649U)
					{
						if (type != 1229209940U)
						{
							if (type != 1229278788U)
							{
								goto IL_715;
							}
							goto IL_578;
						}
						else
						{
							if (num4 != 0)
							{
								goto Block_50;
							}
							if (b != 0 && num3 == 0U)
							{
								goto Block_52;
							}
							if (scan == 2)
							{
								goto Block_53;
							}
							if (num + stbi__pngchunk.length < num)
							{
								return 0;
							}
							if (num + stbi__pngchunk.length > num2)
							{
								if (num2 == 0U)
								{
									num2 = ((stbi__pngchunk.length > 4096U) ? stbi__pngchunk.length : 4096U);
								}
								while (num + stbi__pngchunk.length > num2)
								{
									num2 *= 2U;
								}
								byte* ptr4 = (byte*)CRuntime.realloc((void*)z.idata, (ulong)num2);
								if (ptr4 == null)
								{
									goto Block_59;
								}
								z.idata = ptr4;
							}
							if (StbImage.stbi__getn(s, z.idata + num, (int)stbi__pngchunk.length) == 0)
							{
								goto Block_60;
							}
							num += stbi__pngchunk.length;
						}
					}
					else
					{
						num7 = 1;
						StbImage.stbi__skip(s, (int)stbi__pngchunk.length);
					}
				}
				else if (type != 1229472850U)
				{
					if (type != 1347179589U)
					{
						if (type != 1951551059U)
						{
							goto IL_715;
						}
						if (num4 != 0)
						{
							goto Block_38;
						}
						if (z.idata != null)
						{
							goto Block_39;
						}
						if (b != 0)
						{
							if (scan == 2)
							{
								goto Block_41;
							}
							if (num3 == 0U)
							{
								goto Block_42;
							}
							if (stbi__pngchunk.length > num3)
							{
								goto Block_43;
							}
							b = 4;
							for (uint num8 = 0U; num8 < stbi__pngchunk.length; num8 += 1U)
							{
								ptr[num8 * 4U + 3U] = StbImage.stbi__get8(s);
							}
						}
						else
						{
							if ((s.img_n & 1) == 0)
							{
								goto Block_45;
							}
							if (stbi__pngchunk.length != (uint)(s.img_n * 2))
							{
								goto Block_46;
							}
							b2 = 1;
							if (z.depth == 16)
							{
								for (int i = 0; i < s.img_n; i++)
								{
									ptr3[i] = (ushort)StbImage.stbi__get16be(s);
								}
							}
							else
							{
								for (int i = 0; i < s.img_n; i++)
								{
									ptr2[i] = (byte)(StbImage.stbi__get16be(s) & 255) * StbImage.stbi__depth_scale_table[z.depth];
								}
							}
						}
					}
					else
					{
						if (num4 != 0)
						{
							goto Block_34;
						}
						if (stbi__pngchunk.length > 768U)
						{
							goto Block_35;
						}
						num3 = stbi__pngchunk.length / 3U;
						if (num3 * 3U != stbi__pngchunk.length)
						{
							goto Block_36;
						}
						for (uint num8 = 0U; num8 < num3; num8 += 1U)
						{
							ptr[num8 * 4U] = StbImage.stbi__get8(s);
							ptr[num8 * 4U + 1U] = StbImage.stbi__get8(s);
							ptr[num8 * 4U + 2U] = StbImage.stbi__get8(s);
							ptr[num8 * 4U + 3U] = byte.MaxValue;
						}
					}
				}
				else
				{
					if (num4 == 0)
					{
						break;
					}
					num4 = 0;
					if (stbi__pngchunk.length != 13U)
					{
						goto Block_11;
					}
					s.img_x = StbImage.stbi__get32be(s);
					if (s.img_x > 16777216U)
					{
						goto Block_12;
					}
					s.img_y = StbImage.stbi__get32be(s);
					if (s.img_y > 16777216U)
					{
						goto Block_13;
					}
					z.depth = (int)StbImage.stbi__get8(s);
					if (z.depth != 1 && z.depth != 2 && z.depth != 4 && z.depth != 8 && z.depth != 16)
					{
						goto Block_18;
					}
					num6 = (int)StbImage.stbi__get8(s);
					if (num6 > 6)
					{
						goto Block_19;
					}
					if (num6 == 3 && z.depth == 16)
					{
						goto Block_21;
					}
					if (num6 == 3)
					{
						b = 3;
					}
					else if ((num6 & 1) != 0)
					{
						goto Block_23;
					}
					if (StbImage.stbi__get8(s) != 0)
					{
						goto Block_24;
					}
					if (StbImage.stbi__get8(s) != 0)
					{
						goto Block_25;
					}
					num5 = (int)StbImage.stbi__get8(s);
					if (num5 > 1)
					{
						goto Block_26;
					}
					if (s.img_x == 0U || s.img_y == 0U)
					{
						goto IL_237;
					}
					if (b == 0)
					{
						s.img_n = (((num6 & 2) != 0) ? 3 : 1) + (((num6 & 4) != 0) ? 1 : 0);
						if ((ulong)(1073741824U / s.img_x) / (ulong)((long)s.img_n) < (ulong)s.img_y)
						{
							goto Block_31;
						}
						if (scan == 2)
						{
							return 1;
						}
					}
					else
					{
						s.img_n = 1;
						if (1073741824U / s.img_x / 4U < s.img_y)
						{
							goto Block_33;
						}
					}
				}
				IL_74C:
				StbImage.stbi__get32be(s);
				continue;
				IL_715:
				if (num4 != 0)
				{
					goto Block_80;
				}
				if ((stbi__pngchunk.type & 536870912U) == 0U)
				{
					goto Block_81;
				}
				StbImage.stbi__skip(s, (int)stbi__pngchunk.length);
				goto IL_74C;
			}
			return StbImage.stbi__err("multiple IHDR");
			Block_11:
			return StbImage.stbi__err("bad IHDR len");
			Block_12:
			return StbImage.stbi__err("too large");
			Block_13:
			return StbImage.stbi__err("too large");
			Block_18:
			return StbImage.stbi__err("1/2/4/8/16-bit only");
			Block_19:
			return StbImage.stbi__err("bad ctype");
			Block_21:
			return StbImage.stbi__err("bad ctype");
			Block_23:
			return StbImage.stbi__err("bad ctype");
			Block_24:
			return StbImage.stbi__err("bad comp method");
			Block_25:
			return StbImage.stbi__err("bad filter method");
			Block_26:
			return StbImage.stbi__err("bad interlace method");
			IL_237:
			return StbImage.stbi__err("0-pixel image");
			Block_31:
			return StbImage.stbi__err("too large");
			Block_33:
			return StbImage.stbi__err("too large");
			Block_34:
			return StbImage.stbi__err("first not IHDR");
			Block_35:
			return StbImage.stbi__err("invalid PLTE");
			Block_36:
			return StbImage.stbi__err("invalid PLTE");
			Block_38:
			return StbImage.stbi__err("first not IHDR");
			Block_39:
			return StbImage.stbi__err("tRNS after IDAT");
			Block_41:
			s.img_n = 4;
			return 1;
			Block_42:
			return StbImage.stbi__err("tRNS before PLTE");
			Block_43:
			return StbImage.stbi__err("bad tRNS len");
			Block_45:
			return StbImage.stbi__err("tRNS with alpha");
			Block_46:
			return StbImage.stbi__err("bad tRNS len");
			Block_50:
			return StbImage.stbi__err("first not IHDR");
			Block_52:
			return StbImage.stbi__err("no PLTE");
			Block_53:
			s.img_n = (int)b;
			return 1;
			Block_59:
			return StbImage.stbi__err("outofmem");
			Block_60:
			return StbImage.stbi__err("outofdata");
			IL_578:
			if (num4 != 0)
			{
				return StbImage.stbi__err("first not IHDR");
			}
			if (scan != 0)
			{
				return 1;
			}
			if (z.idata == null)
			{
				return StbImage.stbi__err("no IDAT");
			}
			uint num9 = (uint)((ulong)((uint)(((ulong)s.img_x * (ulong)((long)z.depth) + 7UL) / 8UL) * s.img_y) * (ulong)((long)s.img_n) + (ulong)s.img_y);
			z.expanded = (byte*)StbImage.stbi_zlib_decode_malloc_guesssize_headerflag((sbyte*)z.idata, (int)num, (int)num9, (int*)(&num9), (num7 != 0) ? 0 : 1);
			if (z.expanded == null)
			{
				return 0;
			}
			CRuntime.free((void*)z.idata);
			z.idata = null;
			if ((req_comp == s.img_n + 1 && req_comp != 3 && b == 0) || b2 != 0)
			{
				s.img_out_n = s.img_n + 1;
			}
			else
			{
				s.img_out_n = s.img_n;
			}
			if (StbImage.stbi__create_png_image(z, z.expanded, num9, s.img_out_n, z.depth, num6, num5) == 0)
			{
				return 0;
			}
			if (b2 != 0)
			{
				if (z.depth == 16)
				{
					if (StbImage.stbi__compute_transparency16(z, ptr3, s.img_out_n) == 0)
					{
						return 0;
					}
				}
				else if (StbImage.stbi__compute_transparency(z, ptr2, s.img_out_n) == 0)
				{
					return 0;
				}
			}
			if (num7 != 0 && StbImage.stbi__de_iphone_flag != 0 && s.img_out_n > 2)
			{
				StbImage.stbi__de_iphone(z);
			}
			if (b != 0)
			{
				s.img_n = (int)b;
				s.img_out_n = (int)b;
				if (req_comp >= 3)
				{
					s.img_out_n = req_comp;
				}
				if (StbImage.stbi__expand_png_palette(z, ptr, (int)num3, s.img_out_n) == 0)
				{
					return 0;
				}
			}
			else if (b2 != 0)
			{
				s.img_n++;
			}
			CRuntime.free((void*)z.expanded);
			z.expanded = null;
			return 1;
			Block_80:
			return StbImage.stbi__err("first not IHDR");
			Block_81:
			return StbImage.stbi__err("XXXX PNG chunk not known");
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0000FDA8 File Offset: 0x0000DFA8
		public unsafe static void* stbi__do_png(StbImage.stbi__png p, int* x, int* y, int* n, int req_comp, StbImage.stbi__result_info* ri)
		{
			void* ptr = null;
			if (req_comp < 0 || req_comp > 4)
			{
				return (StbImage.stbi__err("bad req_comp") != 0) ? null : null;
			}
			if (StbImage.stbi__parse_png_file(p, 0, req_comp) != 0)
			{
				if (p.depth < 8)
				{
					ri->bits_per_channel = 8;
				}
				else
				{
					ri->bits_per_channel = p.depth;
				}
				ptr = (void*)p._out_;
				p._out_ = null;
				if (req_comp != 0 && req_comp != p.s.img_out_n)
				{
					if (ri->bits_per_channel == 8)
					{
						ptr = (void*)StbImage.stbi__convert_format((byte*)ptr, p.s.img_out_n, req_comp, p.s.img_x, p.s.img_y);
					}
					else
					{
						ptr = (void*)StbImage.stbi__convert_format16((ushort*)ptr, p.s.img_out_n, req_comp, p.s.img_x, p.s.img_y);
					}
					p.s.img_out_n = req_comp;
					if (ptr == null)
					{
						return ptr;
					}
				}
				*x = (int)p.s.img_x;
				*y = (int)p.s.img_y;
				if (n != null)
				{
					*n = p.s.img_n;
				}
			}
			CRuntime.free((void*)p._out_);
			p._out_ = null;
			CRuntime.free((void*)p.expanded);
			p.expanded = null;
			CRuntime.free((void*)p.idata);
			p.idata = null;
			return ptr;
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000FF04 File Offset: 0x0000E104
		public unsafe static void* stbi__png_load(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri)
		{
			return StbImage.stbi__do_png(new StbImage.stbi__png
			{
				s = s
			}, x, y, comp, req_comp, ri);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000FF1E File Offset: 0x0000E11E
		public static int stbi__png_test(StbImage.stbi__context s)
		{
			int result = StbImage.stbi__check_png_header(s);
			StbImage.stbi__rewind(s);
			return result;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000FF2C File Offset: 0x0000E12C
		public unsafe static int stbi__png_info_raw(StbImage.stbi__png p, int* x, int* y, int* comp)
		{
			if (StbImage.stbi__parse_png_file(p, 2, 0) == 0)
			{
				StbImage.stbi__rewind(p.s);
				return 0;
			}
			if (x != null)
			{
				*x = (int)p.s.img_x;
			}
			if (y != null)
			{
				*y = (int)p.s.img_y;
			}
			if (comp != null)
			{
				*comp = p.s.img_n;
			}
			return 1;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000FF87 File Offset: 0x0000E187
		public unsafe static int stbi__png_info(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			return StbImage.stbi__png_info_raw(new StbImage.stbi__png
			{
				s = s
			}, x, y, comp);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000FFA0 File Offset: 0x0000E1A0
		public static int stbi__bmp_test_raw(StbImage.stbi__context s)
		{
			if (StbImage.stbi__get8(s) != 66)
			{
				return 0;
			}
			if (StbImage.stbi__get8(s) != 77)
			{
				return 0;
			}
			StbImage.stbi__get32le(s);
			StbImage.stbi__get16le(s);
			StbImage.stbi__get16le(s);
			StbImage.stbi__get32le(s);
			int num = (int)StbImage.stbi__get32le(s);
			if (num != 12 && num != 40 && num != 56 && num != 108 && num != 124)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00010004 File Offset: 0x0000E204
		public static int stbi__bmp_test(StbImage.stbi__context s)
		{
			int result = StbImage.stbi__bmp_test_raw(s);
			StbImage.stbi__rewind(s);
			return result;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00010014 File Offset: 0x0000E214
		public static int stbi__high_bit(uint z)
		{
			int num = 0;
			if (z == 0U)
			{
				return -1;
			}
			if (z >= 65536U)
			{
				num += 16;
				z >>= 16;
			}
			if (z >= 256U)
			{
				num += 8;
				z >>= 8;
			}
			if (z >= 16U)
			{
				num += 4;
				z >>= 4;
			}
			if (z >= 4U)
			{
				num += 2;
				z >>= 2;
			}
			if (z >= 2U)
			{
				num++;
				z >>= 1;
			}
			return num;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00010078 File Offset: 0x0000E278
		public static int stbi__bitcount(uint a)
		{
			a = (a & 1431655765U) + ((a >> 1) & 1431655765U);
			a = (a & 858993459U) + ((a >> 2) & 858993459U);
			a = (a + (a >> 4)) & 252645135U;
			a += a >> 8;
			a += a >> 16;
			return (int)(a & 255U);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x000100D0 File Offset: 0x0000E2D0
		public static int stbi__shiftsigned(int v, int shift, int bits)
		{
			if (shift < 0)
			{
				v <<= -shift;
			}
			else
			{
				v >>= shift;
			}
			int num = v;
			for (int i = bits; i < 8; i += bits)
			{
				num += v >> i;
			}
			return num;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00010110 File Offset: 0x0000E310
		public unsafe static void* stbi__bmp_parse_header(StbImage.stbi__context s, StbImage.stbi__bmp_data* info)
		{
			if (StbImage.stbi__get8(s) != 66 || StbImage.stbi__get8(s) != 77)
			{
				return (StbImage.stbi__err("not BMP") != 0) ? null : null;
			}
			StbImage.stbi__get32le(s);
			StbImage.stbi__get16le(s);
			StbImage.stbi__get16le(s);
			info->offset = (int)StbImage.stbi__get32le(s);
			int num = (info->hsz = (int)StbImage.stbi__get32le(s));
			info->mr = (info->mg = (info->mb = (info->ma = 0U)));
			if (num != 12 && num != 40 && num != 56 && num != 108 && num != 124)
			{
				return (StbImage.stbi__err("unknown BMP") != 0) ? null : null;
			}
			if (num == 12)
			{
				s.img_x = (uint)StbImage.stbi__get16le(s);
				s.img_y = (uint)StbImage.stbi__get16le(s);
			}
			else
			{
				s.img_x = StbImage.stbi__get32le(s);
				s.img_y = StbImage.stbi__get32le(s);
			}
			if (StbImage.stbi__get16le(s) != 1)
			{
				return (StbImage.stbi__err("bad BMP") != 0) ? null : null;
			}
			info->bpp = StbImage.stbi__get16le(s);
			if (info->bpp == 1)
			{
				return (StbImage.stbi__err("monochrome") != 0) ? null : null;
			}
			if (num != 12)
			{
				int num2 = (int)StbImage.stbi__get32le(s);
				if (num2 == 1 || num2 == 2)
				{
					return (StbImage.stbi__err("BMP RLE") != 0) ? null : null;
				}
				StbImage.stbi__get32le(s);
				StbImage.stbi__get32le(s);
				StbImage.stbi__get32le(s);
				StbImage.stbi__get32le(s);
				StbImage.stbi__get32le(s);
				if (num == 40 || num == 56)
				{
					if (num == 56)
					{
						StbImage.stbi__get32le(s);
						StbImage.stbi__get32le(s);
						StbImage.stbi__get32le(s);
						StbImage.stbi__get32le(s);
					}
					if (info->bpp == 16 || info->bpp == 32)
					{
						if (num2 == 0)
						{
							if (info->bpp == 32)
							{
								info->mr = 16711680U;
								info->mg = 65280U;
								info->mb = 255U;
								info->ma = 4278190080U;
								info->all_a = 0U;
							}
							else
							{
								info->mr = 31744U;
								info->mg = 992U;
								info->mb = 31U;
							}
						}
						else
						{
							if (num2 != 3)
							{
								return (StbImage.stbi__err("bad BMP") != 0) ? null : null;
							}
							info->mr = StbImage.stbi__get32le(s);
							info->mg = StbImage.stbi__get32le(s);
							info->mb = StbImage.stbi__get32le(s);
							if (info->mr == info->mg && info->mg == info->mb)
							{
								return (StbImage.stbi__err("bad BMP") != 0) ? null : null;
							}
						}
					}
				}
				else
				{
					if (num != 108 && num != 124)
					{
						return (StbImage.stbi__err("bad BMP") != 0) ? null : null;
					}
					info->mr = StbImage.stbi__get32le(s);
					info->mg = StbImage.stbi__get32le(s);
					info->mb = StbImage.stbi__get32le(s);
					info->ma = StbImage.stbi__get32le(s);
					StbImage.stbi__get32le(s);
					for (int i = 0; i < 12; i++)
					{
						StbImage.stbi__get32le(s);
					}
					if (num == 124)
					{
						StbImage.stbi__get32le(s);
						StbImage.stbi__get32le(s);
						StbImage.stbi__get32le(s);
						StbImage.stbi__get32le(s);
					}
				}
			}
			return 1;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00010444 File Offset: 0x0000E644
		public unsafe static void* stbi__bmp_load(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri)
		{
			byte* ptr = stackalloc byte[(UIntPtr)1024];
			int num = 0;
			StbImage.stbi__bmp_data stbi__bmp_data = default(StbImage.stbi__bmp_data);
			stbi__bmp_data.all_a = 255U;
			if (StbImage.stbi__bmp_parse_header(s, &stbi__bmp_data) == null)
			{
				return null;
			}
			int num2 = ((s.img_y > 0U) ? 1 : 0);
			s.img_y = (uint)CRuntime.abs((int)s.img_y);
			uint mr = stbi__bmp_data.mr;
			uint mg = stbi__bmp_data.mg;
			uint mb = stbi__bmp_data.mb;
			uint ma = stbi__bmp_data.ma;
			uint num3 = stbi__bmp_data.all_a;
			if (stbi__bmp_data.hsz == 12)
			{
				if (stbi__bmp_data.bpp < 24)
				{
					num = (stbi__bmp_data.offset - 14 - 24) / 3;
				}
			}
			else if (stbi__bmp_data.bpp < 16)
			{
				num = stbi__bmp_data.offset - 14 - stbi__bmp_data.hsz >> 2;
			}
			s.img_n = ((ma != 0U) ? 4 : 3);
			int num4;
			if (req_comp != 0 && req_comp >= 3)
			{
				num4 = req_comp;
			}
			else
			{
				num4 = s.img_n;
			}
			if (StbImage.stbi__mad3sizes_valid(num4, (int)s.img_x, (int)s.img_y, 0) == 0)
			{
				return (StbImage.stbi__err("too large") != 0) ? null : null;
			}
			byte* ptr2 = (byte*)StbImage.stbi__malloc_mad3(num4, (int)s.img_x, (int)s.img_y, 0);
			if (ptr2 == null)
			{
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			if (stbi__bmp_data.bpp < 16)
			{
				int num5 = 0;
				if (num == 0 || num > 256)
				{
					CRuntime.free((void*)ptr2);
					return (StbImage.stbi__err("invalid") != 0) ? null : null;
				}
				for (int i = 0; i < num; i++)
				{
					ptr[i * 4 + 2] = StbImage.stbi__get8(s);
					ptr[i * 4 + 1] = StbImage.stbi__get8(s);
					ptr[i * 4] = StbImage.stbi__get8(s);
					if (stbi__bmp_data.hsz != 12)
					{
						StbImage.stbi__get8(s);
					}
					ptr[i * 4 + 3] = byte.MaxValue;
				}
				StbImage.stbi__skip(s, stbi__bmp_data.offset - 14 - stbi__bmp_data.hsz - num * ((stbi__bmp_data.hsz == 12) ? 3 : 4));
				int num6;
				if (stbi__bmp_data.bpp == 4)
				{
					num6 = (int)(s.img_x + 1U >> 1);
				}
				else
				{
					if (stbi__bmp_data.bpp != 8)
					{
						CRuntime.free((void*)ptr2);
						return (StbImage.stbi__err("bad bpp") != 0) ? null : null;
					}
					num6 = (int)s.img_x;
				}
				int n = -num6 & 3;
				for (int j = 0; j < (int)s.img_y; j++)
				{
					for (int i = 0; i < (int)s.img_x; i += 2)
					{
						int num7 = (int)StbImage.stbi__get8(s);
						int num8 = 0;
						if (stbi__bmp_data.bpp == 4)
						{
							num8 = num7 & 15;
							num7 >>= 4;
						}
						ptr2[num5++] = ptr[num7 * 4];
						ptr2[num5++] = ptr[num7 * 4 + 1];
						ptr2[num5++] = ptr[num7 * 4 + 2];
						if (num4 == 4)
						{
							ptr2[num5++] = byte.MaxValue;
						}
						if (i + 1 == (int)s.img_x)
						{
							break;
						}
						num7 = ((stbi__bmp_data.bpp == 8) ? ((int)StbImage.stbi__get8(s)) : num8);
						ptr2[num5++] = ptr[num7 * 4];
						ptr2[num5++] = ptr[num7 * 4 + 1];
						ptr2[num5++] = ptr[num7 * 4 + 2];
						if (num4 == 4)
						{
							ptr2[num5++] = byte.MaxValue;
						}
					}
					StbImage.stbi__skip(s, n);
				}
			}
			else
			{
				int shift = 0;
				int shift2 = 0;
				int shift3 = 0;
				int shift4 = 0;
				int bits = 0;
				int bits2 = 0;
				int bits3 = 0;
				int bits4 = 0;
				int num9 = 0;
				int num10 = 0;
				StbImage.stbi__skip(s, stbi__bmp_data.offset - 14 - stbi__bmp_data.hsz);
				int num6;
				if (stbi__bmp_data.bpp == 24)
				{
					num6 = (int)(3U * s.img_x);
				}
				else if (stbi__bmp_data.bpp == 16)
				{
					num6 = (int)(2U * s.img_x);
				}
				else
				{
					num6 = 0;
				}
				int n = -num6 & 3;
				if (stbi__bmp_data.bpp == 24)
				{
					num10 = 1;
				}
				else if (stbi__bmp_data.bpp == 32 && mb == 255U && mg == 65280U && mr == 16711680U && ma == 4278190080U)
				{
					num10 = 2;
				}
				if (num10 == 0)
				{
					if (mr == 0U || mg == 0U || mb == 0U)
					{
						CRuntime.free((void*)ptr2);
						return (StbImage.stbi__err("bad masks") != 0) ? null : null;
					}
					shift = StbImage.stbi__high_bit(mr) - 7;
					bits = StbImage.stbi__bitcount(mr);
					shift2 = StbImage.stbi__high_bit(mg) - 7;
					bits2 = StbImage.stbi__bitcount(mg);
					shift3 = StbImage.stbi__high_bit(mb) - 7;
					bits3 = StbImage.stbi__bitcount(mb);
					shift4 = StbImage.stbi__high_bit(ma) - 7;
					bits4 = StbImage.stbi__bitcount(ma);
				}
				for (int j = 0; j < (int)s.img_y; j++)
				{
					if (num10 != 0)
					{
						for (int i = 0; i < (int)s.img_x; i++)
						{
							ptr2[num9 + 2] = StbImage.stbi__get8(s);
							ptr2[num9 + 1] = StbImage.stbi__get8(s);
							ptr2[num9] = StbImage.stbi__get8(s);
							num9 += 3;
							byte b = ((num10 == 2) ? StbImage.stbi__get8(s) : byte.MaxValue);
							num3 |= (uint)b;
							if (num4 == 4)
							{
								ptr2[num9++] = b;
							}
						}
					}
					else
					{
						int bpp = stbi__bmp_data.bpp;
						for (int i = 0; i < (int)s.img_x; i++)
						{
							uint num11 = (uint)((bpp == 16) ? StbImage.stbi__get16le(s) : ((int)StbImage.stbi__get32le(s)));
							ptr2[num9++] = (byte)(StbImage.stbi__shiftsigned((int)(num11 & mr), shift, bits) & 255);
							ptr2[num9++] = (byte)(StbImage.stbi__shiftsigned((int)(num11 & mg), shift2, bits2) & 255);
							ptr2[num9++] = (byte)(StbImage.stbi__shiftsigned((int)(num11 & mb), shift3, bits3) & 255);
							int num12 = ((ma != 0U) ? StbImage.stbi__shiftsigned((int)(num11 & ma), shift4, bits4) : 255);
							num3 |= (uint)num12;
							if (num4 == 4)
							{
								ptr2[num9++] = (byte)(num12 & 255);
							}
						}
					}
					StbImage.stbi__skip(s, n);
				}
			}
			if (num4 == 4 && num3 == 0U)
			{
				for (int i = (int)(4U * s.img_x * s.img_y - 1U); i >= 0; i -= 4)
				{
					ptr2[i] = byte.MaxValue;
				}
			}
			if (num2 != 0)
			{
				for (int j = 0; j < (int)s.img_y >> 1; j++)
				{
					byte* ptr3 = ptr2 + (long)j * (long)((ulong)s.img_x) * (long)num4;
					byte* ptr4 = ptr2 + ((ulong)(s.img_y - 1U) - (ulong)((long)j)) * (ulong)s.img_x * (ulong)((long)num4);
					for (int i = 0; i < (int)(s.img_x * (uint)num4); i++)
					{
						byte b2 = ptr3[i];
						ptr3[i] = ptr4[i];
						ptr4[i] = b2;
					}
				}
			}
			if (req_comp != 0 && req_comp != num4)
			{
				ptr2 = StbImage.stbi__convert_format(ptr2, num4, req_comp, s.img_x, s.img_y);
				if (ptr2 == null)
				{
					return (void*)ptr2;
				}
			}
			*x = (int)s.img_x;
			*y = (int)s.img_y;
			if (comp != null)
			{
				*comp = s.img_n;
			}
			return (void*)ptr2;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00010B54 File Offset: 0x0000ED54
		public unsafe static int stbi__tga_get_comp(int bits_per_pixel, int is_grey, int* is_rgb16)
		{
			if (is_rgb16 != null)
			{
				*is_rgb16 = 0;
			}
			if (bits_per_pixel <= 16)
			{
				if (bits_per_pixel == 8)
				{
					return 1;
				}
				if (bits_per_pixel - 15 <= 1)
				{
					if (bits_per_pixel == 16 && is_grey != 0)
					{
						return 2;
					}
					if (is_rgb16 != null)
					{
						*is_rgb16 = 1;
					}
					return 3;
				}
			}
			else if (bits_per_pixel == 24 || bits_per_pixel == 32)
			{
				return bits_per_pixel / 8;
			}
			return 0;
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00010BA4 File Offset: 0x0000EDA4
		public unsafe static int stbi__tga_info(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			StbImage.stbi__get8(s);
			int num = (int)StbImage.stbi__get8(s);
			if (num > 1)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			int num2 = (int)StbImage.stbi__get8(s);
			int num4;
			if (num == 1)
			{
				if (num2 != 1 && num2 != 9)
				{
					StbImage.stbi__rewind(s);
					return 0;
				}
				StbImage.stbi__skip(s, 4);
				int num3 = (int)StbImage.stbi__get8(s);
				if (num3 != 8 && num3 != 15 && num3 != 16 && num3 != 24 && num3 != 32)
				{
					StbImage.stbi__rewind(s);
					return 0;
				}
				StbImage.stbi__skip(s, 4);
				num4 = num3;
			}
			else
			{
				if (num2 != 2 && num2 != 3 && num2 != 10 && num2 != 11)
				{
					StbImage.stbi__rewind(s);
					return 0;
				}
				StbImage.stbi__skip(s, 9);
				num4 = 0;
			}
			int num5 = StbImage.stbi__get16le(s);
			if (num5 < 1)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			int num6 = StbImage.stbi__get16le(s);
			if (num6 < 1)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			int num7 = (int)StbImage.stbi__get8(s);
			StbImage.stbi__get8(s);
			int num8;
			if (num4 != 0)
			{
				if (num7 != 8 && num7 != 16)
				{
					StbImage.stbi__rewind(s);
					return 0;
				}
				num8 = StbImage.stbi__tga_get_comp(num4, 0, null);
			}
			else
			{
				num8 = StbImage.stbi__tga_get_comp(num7, (num2 == 3 || num2 == 11) ? 1 : 0, null);
			}
			if (num8 == 0)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			if (x != null)
			{
				*x = num5;
			}
			if (y != null)
			{
				*y = num6;
			}
			if (comp != null)
			{
				*comp = num8;
			}
			return 1;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00010CE4 File Offset: 0x0000EEE4
		public static int stbi__tga_test(StbImage.stbi__context s)
		{
			int result = 0;
			StbImage.stbi__get8(s);
			int num = (int)StbImage.stbi__get8(s);
			if (num <= 1)
			{
				int num2 = (int)StbImage.stbi__get8(s);
				if (num == 1)
				{
					if (num2 != 1 && num2 != 9)
					{
						goto IL_B7;
					}
					StbImage.stbi__skip(s, 4);
					num2 = (int)StbImage.stbi__get8(s);
					if (num2 != 8 && num2 != 15 && num2 != 16 && num2 != 24 && num2 != 32)
					{
						goto IL_B7;
					}
					StbImage.stbi__skip(s, 4);
				}
				else
				{
					if (num2 != 2 && num2 != 3 && num2 != 10 && num2 != 11)
					{
						goto IL_B7;
					}
					StbImage.stbi__skip(s, 9);
				}
				if (StbImage.stbi__get16le(s) >= 1 && StbImage.stbi__get16le(s) >= 1)
				{
					num2 = (int)StbImage.stbi__get8(s);
					if ((num != 1 || num2 == 8 || num2 == 16) && (num2 == 8 || num2 == 15 || num2 == 16 || num2 == 24 || num2 == 32))
					{
						result = 1;
					}
				}
			}
			IL_B7:
			StbImage.stbi__rewind(s);
			return result;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00010DB0 File Offset: 0x0000EFB0
		public unsafe static void stbi__tga_read_rgb16(StbImage.stbi__context s, byte* _out_)
		{
			ushort num = (ushort)StbImage.stbi__get16le(s);
			ushort num2 = 31;
			int num3 = (num >> 10) & (int)num2;
			int num4 = (num >> 5) & (int)num2;
			int num5 = (int)(num & num2);
			*_out_ = (byte)(num3 * 255 / 31);
			_out_[1] = (byte)(num4 * 255 / 31);
			_out_[2] = (byte)(num5 * 255 / 31);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00010E04 File Offset: 0x0000F004
		public unsafe static void* stbi__tga_load(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri)
		{
			int n = (int)StbImage.stbi__get8(s);
			int num = (int)StbImage.stbi__get8(s);
			int num2 = (int)StbImage.stbi__get8(s);
			int num3 = 0;
			int n2 = StbImage.stbi__get16le(s);
			int num4 = StbImage.stbi__get16le(s);
			int bits_per_pixel = (int)StbImage.stbi__get8(s);
			StbImage.stbi__get16le(s);
			StbImage.stbi__get16le(s);
			int num5 = StbImage.stbi__get16le(s);
			int num6 = StbImage.stbi__get16le(s);
			int num7 = (int)StbImage.stbi__get8(s);
			int num8 = 0;
			int num9 = (int)StbImage.stbi__get8(s);
			byte* ptr = null;
			byte* ptr2 = stackalloc byte[(UIntPtr)4];
			*ptr2 = 0;
			int num10 = 0;
			int num11 = 0;
			int num12 = 1;
			if (num2 >= 8)
			{
				num2 -= 8;
				num3 = 1;
			}
			num9 = 1 - ((num9 >> 5) & 1);
			int num13;
			if (num != 0)
			{
				num13 = StbImage.stbi__tga_get_comp(bits_per_pixel, 0, &num8);
			}
			else
			{
				num13 = StbImage.stbi__tga_get_comp(num7, (num2 == 3) ? 1 : 0, &num8);
			}
			if (num13 == 0)
			{
				return (StbImage.stbi__err("bad format") != 0) ? null : null;
			}
			*x = num5;
			*y = num6;
			if (comp != null)
			{
				*comp = num13;
			}
			if (StbImage.stbi__mad3sizes_valid(num5, num6, num13, 0) == 0)
			{
				return (StbImage.stbi__err("too large") != 0) ? null : null;
			}
			byte* ptr3 = (byte*)StbImage.stbi__malloc_mad3(num5, num6, num13, 0);
			if (ptr3 == null)
			{
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			StbImage.stbi__skip(s, n);
			if (num == 0 && num3 == 0 && num8 == 0)
			{
				for (int i = 0; i < num6; i++)
				{
					int num14 = ((num9 != 0) ? (num6 - i - 1) : i);
					byte* buffer = ptr3 + num14 * num5 * num13;
					StbImage.stbi__getn(s, buffer, num5 * num13);
				}
			}
			else
			{
				if (num != 0)
				{
					StbImage.stbi__skip(s, n2);
					ptr = (byte*)StbImage.stbi__malloc_mad2(num4, num13, 0);
					if (ptr == null)
					{
						CRuntime.free((void*)ptr3);
						return (StbImage.stbi__err("outofmem") != 0) ? null : null;
					}
					if (num8 != 0)
					{
						byte* ptr4 = ptr;
						for (int i = 0; i < num4; i++)
						{
							StbImage.stbi__tga_read_rgb16(s, ptr4);
							ptr4 += num13;
						}
					}
					else if (StbImage.stbi__getn(s, ptr, num4 * num13) == 0)
					{
						CRuntime.free((void*)ptr3);
						CRuntime.free((void*)ptr);
						return (StbImage.stbi__err("bad palette") != 0) ? null : null;
					}
				}
				for (int i = 0; i < num5 * num6; i++)
				{
					if (num3 != 0)
					{
						if (num10 == 0)
						{
							int num15 = (int)StbImage.stbi__get8(s);
							num10 = 1 + (num15 & 127);
							num11 = num15 >> 7;
							num12 = 1;
						}
						else if (num11 == 0)
						{
							num12 = 1;
						}
					}
					else
					{
						num12 = 1;
					}
					if (num12 != 0)
					{
						if (num != 0)
						{
							int num16 = ((num7 == 8) ? ((int)StbImage.stbi__get8(s)) : StbImage.stbi__get16le(s));
							if (num16 >= num4)
							{
								num16 = 0;
							}
							num16 *= num13;
							for (int j = 0; j < num13; j++)
							{
								ptr2[j] = ptr[num16 + j];
							}
						}
						else if (num8 != 0)
						{
							StbImage.stbi__tga_read_rgb16(s, ptr2);
						}
						else
						{
							for (int j = 0; j < num13; j++)
							{
								ptr2[j] = StbImage.stbi__get8(s);
							}
						}
						num12 = 0;
					}
					for (int j = 0; j < num13; j++)
					{
						ptr3[i * num13 + j] = ptr2[j];
					}
					num10--;
				}
				if (num9 != 0)
				{
					int j = 0;
					while (j * 2 < num6)
					{
						int num17 = j * num5 * num13;
						int num18 = (num6 - 1 - j) * num5 * num13;
						for (int i = num5 * num13; i > 0; i--)
						{
							byte b = ptr3[num17];
							ptr3[num17] = ptr3[num18];
							ptr3[num18] = b;
							num17++;
							num18++;
						}
						j++;
					}
				}
				if (ptr != null)
				{
					CRuntime.free((void*)ptr);
				}
			}
			if (num13 >= 3 && num8 == 0)
			{
				byte* ptr5 = ptr3;
				for (int i = 0; i < num5 * num6; i++)
				{
					byte b2 = *ptr5;
					*ptr5 = ptr5[2];
					ptr5[2] = b2;
					ptr5 += num13;
				}
			}
			if (req_comp != 0 && req_comp != num13)
			{
				ptr3 = StbImage.stbi__convert_format(ptr3, num13, req_comp, (uint)num5, (uint)num6);
			}
			return (void*)ptr3;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x000111EC File Offset: 0x0000F3EC
		public static int stbi__psd_test(StbImage.stbi__context s)
		{
			int result = ((StbImage.stbi__get32be(s) == 943870035U) ? 1 : 0);
			StbImage.stbi__rewind(s);
			return result;
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00011208 File Offset: 0x0000F408
		public unsafe static int stbi__psd_decode_rle(StbImage.stbi__context s, byte* p, int pixelCount)
		{
			int num = 0;
			int num2;
			while ((num2 = pixelCount - num) > 0)
			{
				int num3 = (int)StbImage.stbi__get8(s);
				if (num3 != 128)
				{
					if (num3 < 128)
					{
						num3++;
						if (num3 > num2)
						{
							return 0;
						}
						num += num3;
						while (num3 != 0)
						{
							*p = StbImage.stbi__get8(s);
							p += 4;
							num3--;
						}
					}
					else if (num3 > 128)
					{
						num3 = 257 - num3;
						if (num3 > num2)
						{
							return 0;
						}
						byte b = StbImage.stbi__get8(s);
						num += num3;
						while (num3 != 0)
						{
							*p = b;
							p += 4;
							num3--;
						}
					}
				}
			}
			return 1;
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00011294 File Offset: 0x0000F494
		public unsafe static void* stbi__psd_load(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri, int bpc)
		{
			if (StbImage.stbi__get32be(s) != 943870035U)
			{
				return (StbImage.stbi__err("not PSD") != 0) ? null : null;
			}
			if (StbImage.stbi__get16be(s) != 1)
			{
				return (StbImage.stbi__err("wrong version") != 0) ? null : null;
			}
			StbImage.stbi__skip(s, 6);
			int num = StbImage.stbi__get16be(s);
			if (num < 0 || num > 16)
			{
				return (StbImage.stbi__err("wrong channel count") != 0) ? null : null;
			}
			int num2 = (int)StbImage.stbi__get32be(s);
			int num3 = (int)StbImage.stbi__get32be(s);
			int num4 = StbImage.stbi__get16be(s);
			if (num4 != 8 && num4 != 16)
			{
				return (StbImage.stbi__err("unsupported bit depth") != 0) ? null : null;
			}
			if (StbImage.stbi__get16be(s) != 3)
			{
				return (StbImage.stbi__err("wrong color format") != 0) ? null : null;
			}
			StbImage.stbi__skip(s, (int)StbImage.stbi__get32be(s));
			StbImage.stbi__skip(s, (int)StbImage.stbi__get32be(s));
			StbImage.stbi__skip(s, (int)StbImage.stbi__get32be(s));
			int num5 = StbImage.stbi__get16be(s);
			if (num5 > 1)
			{
				return (StbImage.stbi__err("bad compression") != 0) ? null : null;
			}
			if (StbImage.stbi__mad3sizes_valid(4, num3, num2, 0) == 0)
			{
				return (StbImage.stbi__err("too large") != 0) ? null : null;
			}
			byte* ptr;
			if (num5 == 0 && num4 == 16 && bpc == 16)
			{
				ptr = (byte*)StbImage.stbi__malloc_mad3(8, num3, num2, 0);
				ri->bits_per_channel = 16;
			}
			else
			{
				ptr = (byte*)StbImage.stbi__malloc((ulong)((long)(4 * num3 * num2)));
			}
			if (ptr == null)
			{
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			int num6 = num3 * num2;
			if (num5 != 0)
			{
				StbImage.stbi__skip(s, num2 * num * 2);
				for (int i = 0; i < 4; i++)
				{
					byte* ptr2 = ptr + i;
					if (i >= num)
					{
						int j = 0;
						while (j < num6)
						{
							*ptr2 = ((i == 3) ? byte.MaxValue : 0);
							j++;
							ptr2 += 4;
						}
					}
					else if (StbImage.stbi__psd_decode_rle(s, ptr2, num6) == 0)
					{
						CRuntime.free((void*)ptr);
						return (StbImage.stbi__err("corrupt") != 0) ? null : null;
					}
				}
			}
			else
			{
				for (int i = 0; i < 4; i++)
				{
					if (i >= num)
					{
						if (num4 == 16 && bpc == 16)
						{
							ushort* ptr3 = (ushort*)(ptr + (IntPtr)i * 2);
							ushort num7 = ((i == 3) ? ushort.MaxValue : 0);
							int j = 0;
							while (j < num6)
							{
								*ptr3 = num7;
								j++;
								ptr3 += 4;
							}
						}
						else
						{
							byte* ptr4 = ptr + i;
							byte b = ((i == 3) ? byte.MaxValue : 0);
							int j = 0;
							while (j < num6)
							{
								*ptr4 = b;
								j++;
								ptr4 += 4;
							}
						}
					}
					else if (ri->bits_per_channel == 16)
					{
						ushort* ptr5 = (ushort*)(ptr + (IntPtr)i * 2);
						int j = 0;
						while (j < num6)
						{
							*ptr5 = (ushort)StbImage.stbi__get16be(s);
							j++;
							ptr5 += 4;
						}
					}
					else
					{
						byte* ptr6 = ptr + i;
						if (num4 == 16)
						{
							int j = 0;
							while (j < num6)
							{
								*ptr6 = (byte)(StbImage.stbi__get16be(s) >> 8);
								j++;
								ptr6 += 4;
							}
						}
						else
						{
							int j = 0;
							while (j < num6)
							{
								*ptr6 = StbImage.stbi__get8(s);
								j++;
								ptr6 += 4;
							}
						}
					}
				}
			}
			if (num >= 4)
			{
				if (ri->bits_per_channel == 16)
				{
					for (int j = 0; j < num3 * num2; j++)
					{
						ushort* ptr7 = (ushort*)(ptr + (IntPtr)(4 * j) * 2);
						if (ptr7[3] != 0 && ptr7[3] != 65535)
						{
							float num8 = (float)ptr7[3] / 65535f;
							float num9 = 1f / num8;
							float num10 = 65535f * (1f - num9);
							*ptr7 = (ushort)((float)(*ptr7) * num9 + num10);
							ptr7[1] = (ushort)((float)ptr7[1] * num9 + num10);
							ptr7[2] = (ushort)((float)ptr7[2] * num9 + num10);
						}
					}
				}
				else
				{
					for (int j = 0; j < num3 * num2; j++)
					{
						byte* ptr8 = ptr + 4 * j;
						if (ptr8[3] != 0 && ptr8[3] != 255)
						{
							float num11 = (float)ptr8[3] / 255f;
							float num12 = 1f / num11;
							float num13 = 255f * (1f - num12);
							*ptr8 = (byte)((float)(*ptr8) * num12 + num13);
							ptr8[1] = (byte)((float)ptr8[1] * num12 + num13);
							ptr8[2] = (byte)((float)ptr8[2] * num12 + num13);
						}
					}
				}
			}
			if (req_comp != 0 && req_comp != 4)
			{
				if (ri->bits_per_channel == 16)
				{
					ptr = (byte*)StbImage.stbi__convert_format16((ushort*)ptr, 4, req_comp, (uint)num3, (uint)num2);
				}
				else
				{
					ptr = StbImage.stbi__convert_format(ptr, 4, req_comp, (uint)num3, (uint)num2);
				}
				if (ptr == null)
				{
					return (void*)ptr;
				}
			}
			if (comp != null)
			{
				*comp = 4;
			}
			*y = num2;
			*x = num3;
			return (void*)ptr;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00011754 File Offset: 0x0000F954
		public static int stbi__gif_test_raw(StbImage.stbi__context s)
		{
			if (StbImage.stbi__get8(s) != 71 || StbImage.stbi__get8(s) != 73 || StbImage.stbi__get8(s) != 70 || StbImage.stbi__get8(s) != 56)
			{
				return 0;
			}
			int num = (int)StbImage.stbi__get8(s);
			if (num != 57 && num != 55)
			{
				return 0;
			}
			if (StbImage.stbi__get8(s) != 97)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000123 RID: 291 RVA: 0x000117AB File Offset: 0x0000F9AB
		public static int stbi__gif_test(StbImage.stbi__context s)
		{
			int result = StbImage.stbi__gif_test_raw(s);
			StbImage.stbi__rewind(s);
			return result;
		}

		// Token: 0x06000124 RID: 292 RVA: 0x000117BC File Offset: 0x0000F9BC
		public unsafe static int stbi__gif_header(StbImage.stbi__context s, StbImage.stbi__gif g, int* comp, int is_info)
		{
			if (StbImage.stbi__get8(s) != 71 || StbImage.stbi__get8(s) != 73 || StbImage.stbi__get8(s) != 70 || StbImage.stbi__get8(s) != 56)
			{
				return StbImage.stbi__err("not GIF");
			}
			byte b = StbImage.stbi__get8(s);
			if (b != 55 && b != 57)
			{
				return StbImage.stbi__err("not GIF");
			}
			if (StbImage.stbi__get8(s) != 97)
			{
				return StbImage.stbi__err("not GIF");
			}
			StbImage.stbi__g_failure_reason = "";
			g.w = StbImage.stbi__get16le(s);
			g.h = StbImage.stbi__get16le(s);
			g.flags = (int)StbImage.stbi__get8(s);
			g.bgindex = (int)StbImage.stbi__get8(s);
			g.ratio = (int)StbImage.stbi__get8(s);
			g.transparent = -1;
			if (comp != null)
			{
				*comp = 4;
			}
			if (is_info != 0)
			{
				return 1;
			}
			if ((g.flags & 128) != 0)
			{
				StbImage.stbi__gif_parse_colortable(s, g.pal, 2 << (g.flags & 7), -1);
			}
			return 1;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x000118B0 File Offset: 0x0000FAB0
		public unsafe static int stbi__gif_info_raw(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			StbImage.stbi__gif stbi__gif = new StbImage.stbi__gif();
			if (StbImage.stbi__gif_header(s, stbi__gif, comp, 1) == 0)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			if (x != null)
			{
				*x = stbi__gif.w;
			}
			if (y != null)
			{
				*y = stbi__gif.h;
			}
			return 1;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x000118F4 File Offset: 0x0000FAF4
		public unsafe static void stbi__out_gif_code(StbImage.stbi__gif g, ushort code)
		{
			if (g.codes[code].prefix >= 0)
			{
				StbImage.stbi__out_gif_code(g, (ushort)g.codes[code].prefix);
			}
			if (g.cur_y >= g.max_y)
			{
				return;
			}
			byte* ptr = g._out_ + (g.cur_x + g.cur_y);
			byte* ptr2 = g.color_table + g.codes[code].suffix * 4;
			if (ptr2[3] >= 128)
			{
				*ptr = ptr2[2];
				ptr[1] = ptr2[1];
				ptr[2] = *ptr2;
				ptr[3] = ptr2[3];
			}
			g.cur_x += 4;
			if (g.cur_x >= g.max_x)
			{
				g.cur_x = g.start_x;
				g.cur_y += g.step;
				while (g.cur_y >= g.max_y && g.parse > 0)
				{
					g.step = (1 << g.parse) * g.line_size;
					g.cur_y = g.start_y + (g.step >> 1);
					g.parse--;
				}
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00011A30 File Offset: 0x0000FC30
		public unsafe static byte* stbi__process_gif_raster(StbImage.stbi__context s, StbImage.stbi__gif g)
		{
			byte b = StbImage.stbi__get8(s);
			if (b > 12)
			{
				return null;
			}
			int num = 1 << (int)b;
			uint num2 = 1U;
			int num3 = (int)(b + 1);
			int num4 = (1 << num3) - 1;
			int num5 = 0;
			int num6 = 0;
			for (int i = 0; i < num; i++)
			{
				g.codes[i].prefix = -1;
				g.codes[i].first = (byte)i;
				g.codes[i].suffix = (byte)i;
			}
			int num7 = num + 2;
			int num8 = -1;
			int num9 = 0;
			for (;;)
			{
				if (num6 < num3)
				{
					if (num9 == 0)
					{
						num9 = (int)StbImage.stbi__get8(s);
						if (num9 == 0)
						{
							break;
						}
					}
					num9--;
					num5 |= (int)StbImage.stbi__get8(s) << num6;
					num6 += 8;
				}
				else
				{
					int num10 = num5 & num4;
					num5 >>= num3;
					num6 -= num3;
					if (num10 == num)
					{
						num3 = (int)(b + 1);
						num4 = (1 << num3) - 1;
						num7 = num + 2;
						num8 = -1;
						num2 = 0U;
					}
					else
					{
						if (num10 == num + 1)
						{
							goto Block_7;
						}
						if (num10 > num7)
						{
							goto IL_229;
						}
						if (num2 != 0U)
						{
							goto Block_10;
						}
						if (num8 >= 0)
						{
							StbImage.stbi__gif_lzw* ptr = g.codes + num7++;
							if (num7 > 4096)
							{
								goto Block_13;
							}
							ptr->prefix = (short)num8;
							ptr->first = g.codes[num8].first;
							ptr->suffix = ((num10 == num7) ? ptr->first : g.codes[num10].first);
						}
						else if (num10 == num7)
						{
							goto Block_16;
						}
						StbImage.stbi__out_gif_code(g, (ushort)num10);
						if ((num7 & num4) == 0 && num7 <= 4095)
						{
							num3++;
							num4 = (1 << num3) - 1;
						}
						num8 = num10;
					}
				}
			}
			return g._out_;
			Block_7:
			StbImage.stbi__skip(s, num9);
			while ((num9 = (int)StbImage.stbi__get8(s)) > 0)
			{
				StbImage.stbi__skip(s, num9);
			}
			return g._out_;
			Block_10:
			return (StbImage.stbi__err("no clear code") != 0) ? null : null;
			Block_13:
			return (StbImage.stbi__err("too many codes") != 0) ? null : null;
			Block_16:
			return (StbImage.stbi__err("illegal code in raster") != 0) ? null : null;
			IL_229:
			return (StbImage.stbi__err("illegal code in raster") != 0) ? null : null;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00011C7C File Offset: 0x0000FE7C
		public unsafe static void stbi__fill_gif_background(StbImage.stbi__gif g, int x0, int y0, int x1, int y1)
		{
			byte* ptr = g.pal + g.bgindex;
			for (int i = y0; i < y1; i += 4 * g.w)
			{
				for (int j = x0; j < x1; j += 4)
				{
					byte* ptr2 = g._out_ + (i + j);
					*ptr2 = ptr[2];
					ptr2[1] = ptr[1];
					ptr2[2] = *ptr;
					ptr2[3] = 0;
				}
			}
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00011CDC File Offset: 0x0000FEDC
		public unsafe static byte* stbi__gif_load_next(StbImage.stbi__context s, StbImage.stbi__gif g, int* comp, int req_comp)
		{
			byte* ptr = null;
			if (g._out_ == null && StbImage.stbi__gif_header(s, g, comp, 0) == 0)
			{
				return null;
			}
			if (StbImage.stbi__mad3sizes_valid(g.w, g.h, 4, 0) == 0)
			{
				return (StbImage.stbi__err("too large") != 0) ? null : null;
			}
			ptr = g._out_;
			g._out_ = (byte*)StbImage.stbi__malloc_mad3(4, g.w, g.h, 0);
			if (g._out_ == null)
			{
				return (StbImage.stbi__err("outofmem") != 0) ? null : null;
			}
			switch ((g.eflags & 28) >> 2)
			{
			case 0:
				StbImage.stbi__fill_gif_background(g, 0, 0, 4 * g.w, 4 * g.w * g.h);
				break;
			case 1:
				if (ptr != null)
				{
					CRuntime.memcpy((void*)g._out_, (void*)ptr, (ulong)((long)(4 * g.w * g.h)));
				}
				g.old_out = ptr;
				break;
			case 2:
				if (ptr != null)
				{
					CRuntime.memcpy((void*)g._out_, (void*)ptr, (ulong)((long)(4 * g.w * g.h)));
				}
				StbImage.stbi__fill_gif_background(g, g.start_x, g.start_y, g.max_x, g.max_y);
				break;
			case 3:
				if (g.old_out != null)
				{
					for (int i = g.start_y; i < g.max_y; i += 4 * g.w)
					{
						CRuntime.memcpy((void*)(g._out_ + (i + g.start_x)), (void*)(g.old_out + (i + g.start_x)), (ulong)((long)(g.max_x - g.start_x)));
					}
				}
				break;
			}
			byte b;
			for (;;)
			{
				b = StbImage.stbi__get8(s);
				if (b != 33)
				{
					break;
				}
				int num;
				if (StbImage.stbi__get8(s) == 249)
				{
					num = (int)StbImage.stbi__get8(s);
					if (num != 4)
					{
						StbImage.stbi__skip(s, num);
						continue;
					}
					g.eflags = (int)StbImage.stbi__get8(s);
					g.delay = StbImage.stbi__get16le(s);
					g.transparent = (int)StbImage.stbi__get8(s);
				}
				while ((num = (int)StbImage.stbi__get8(s)) != 0)
				{
					StbImage.stbi__skip(s, num);
				}
			}
			if (b != 44)
			{
				if (b != 59)
				{
					return (StbImage.stbi__err("unknown code") != 0) ? null : null;
				}
				return null;
			}
			else
			{
				int num2 = -1;
				int num3 = StbImage.stbi__get16le(s);
				int num4 = StbImage.stbi__get16le(s);
				int num5 = StbImage.stbi__get16le(s);
				int num6 = StbImage.stbi__get16le(s);
				if (num3 + num5 > g.w || num4 + num6 > g.h)
				{
					return (StbImage.stbi__err("bad Image Descriptor") != 0) ? null : null;
				}
				g.line_size = g.w * 4;
				g.start_x = num3 * 4;
				g.start_y = num4 * g.line_size;
				g.max_x = g.start_x + num5 * 4;
				g.max_y = g.start_y + num6 * g.line_size;
				g.cur_x = g.start_x;
				g.cur_y = g.start_y;
				g.lflags = (int)StbImage.stbi__get8(s);
				if ((g.lflags & 64) != 0)
				{
					g.step = 8 * g.line_size;
					g.parse = 3;
				}
				else
				{
					g.step = g.line_size;
					g.parse = 0;
				}
				if ((g.lflags & 128) != 0)
				{
					StbImage.stbi__gif_parse_colortable(s, g.lpal, 2 << (g.lflags & 7), ((g.eflags & 1) != 0) ? g.transparent : (-1));
					g.color_table = g.lpal;
				}
				else
				{
					if ((g.flags & 128) == 0)
					{
						return (StbImage.stbi__err("missing color table") != 0) ? null : null;
					}
					if (g.transparent >= 0 && (g.eflags & 1) != 0)
					{
						num2 = (int)g.pal[g.transparent * 4 + 3];
						g.pal[g.transparent * 4 + 3] = 0;
					}
					g.color_table = g.pal;
				}
				byte* ptr2 = StbImage.stbi__process_gif_raster(s, g);
				if (ptr2 == null)
				{
					return null;
				}
				if (num2 != -1)
				{
					g.pal[g.transparent * 4 + 3] = (byte)num2;
				}
				return ptr2;
			}
		}

		// Token: 0x0600012A RID: 298 RVA: 0x000120F8 File Offset: 0x000102F8
		public unsafe static void* stbi__gif_load(StbImage.stbi__context s, int* x, int* y, int* comp, int req_comp, StbImage.stbi__result_info* ri)
		{
			byte* ptr = null;
			StbImage.stbi__gif stbi__gif = new StbImage.stbi__gif();
			ptr = StbImage.stbi__gif_load_next(s, stbi__gif, comp, req_comp);
			if (ptr != null)
			{
				*x = stbi__gif.w;
				*y = stbi__gif.h;
				if (req_comp != 0 && req_comp != 4)
				{
					ptr = StbImage.stbi__convert_format(ptr, 4, req_comp, (uint)stbi__gif.w, (uint)stbi__gif.h);
				}
			}
			else if (stbi__gif._out_ != null)
			{
				CRuntime.free((void*)stbi__gif._out_);
			}
			return (void*)ptr;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x00012165 File Offset: 0x00010365
		public unsafe static int stbi__gif_info(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			return StbImage.stbi__gif_info_raw(s, x, y, comp);
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00012170 File Offset: 0x00010370
		public unsafe static int stbi__bmp_info(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			StbImage.stbi__bmp_data stbi__bmp_data = default(StbImage.stbi__bmp_data);
			stbi__bmp_data.all_a = 255U;
			UIntPtr uintPtr = StbImage.stbi__bmp_parse_header(s, &stbi__bmp_data);
			StbImage.stbi__rewind(s);
			if (uintPtr == (UIntPtr)0)
			{
				return 0;
			}
			if (x != null)
			{
				*x = (int)s.img_x;
			}
			if (y != null)
			{
				*y = (int)s.img_y;
			}
			if (comp != null)
			{
				*comp = ((stbi__bmp_data.ma != 0U) ? 4 : 3);
			}
			return 1;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x000121D4 File Offset: 0x000103D4
		public unsafe static int stbi__psd_info(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			int num;
			if (x == null)
			{
				x = &num;
			}
			if (y == null)
			{
				y = &num;
			}
			if (comp == null)
			{
				comp = &num;
			}
			if (StbImage.stbi__get32be(s) != 943870035U)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			if (StbImage.stbi__get16be(s) != 1)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			StbImage.stbi__skip(s, 6);
			int num2 = StbImage.stbi__get16be(s);
			if (num2 < 0 || num2 > 16)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			*y = (int)StbImage.stbi__get32be(s);
			*x = (int)StbImage.stbi__get32be(s);
			if (StbImage.stbi__get16be(s) != 8)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			if (StbImage.stbi__get16be(s) != 3)
			{
				StbImage.stbi__rewind(s);
				return 0;
			}
			*comp = 4;
			return 1;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0001227C File Offset: 0x0001047C
		public unsafe static int stbi__info_main(StbImage.stbi__context s, int* x, int* y, int* comp)
		{
			if (StbImage.stbi__jpeg_info(s, x, y, comp) != 0)
			{
				return 1;
			}
			if (StbImage.stbi__png_info(s, x, y, comp) != 0)
			{
				return 1;
			}
			if (StbImage.stbi__gif_info(s, x, y, comp) != 0)
			{
				return 1;
			}
			if (StbImage.stbi__bmp_info(s, x, y, comp) != 0)
			{
				return 1;
			}
			if (StbImage.stbi__psd_info(s, x, y, comp) != 0)
			{
				return 1;
			}
			if (StbImage.stbi__tga_info(s, x, y, comp) != 0)
			{
				return 1;
			}
			return StbImage.stbi__err("unknown image type");
		}

		// Token: 0x0600012F RID: 303 RVA: 0x000122E1 File Offset: 0x000104E1
		public unsafe static int stbi_info_from_memory(byte* buffer, int len, int* x, int* y, int* comp)
		{
			StbImage.stbi__context s = new StbImage.stbi__context();
			StbImage.stbi__start_mem(s, buffer, len);
			return StbImage.stbi__info_main(s, x, y, comp);
		}

		// Token: 0x06000130 RID: 304 RVA: 0x000122F9 File Offset: 0x000104F9
		public unsafe static int stbi_info_from_callbacks(StbImage.stbi_io_callbacks c, void* user, int* x, int* y, int* comp)
		{
			StbImage.stbi__context s = new StbImage.stbi__context();
			StbImage.stbi__start_callbacks(s, c, user);
			return StbImage.stbi__info_main(s, x, y, comp);
		}

		// Token: 0x0400004E RID: 78
		public static string LastError;

		// Token: 0x0400004F RID: 79
		public const int STBI__ZFAST_BITS = 9;

		// Token: 0x04000050 RID: 80
		public static string stbi__g_failure_reason;

		// Token: 0x04000051 RID: 81
		public static int stbi__vertically_flip_on_load;

		// Token: 0x04000052 RID: 82
		public const int STBI_default = 0;

		// Token: 0x04000053 RID: 83
		public const int STBI_grey = 1;

		// Token: 0x04000054 RID: 84
		public const int STBI_grey_alpha = 2;

		// Token: 0x04000055 RID: 85
		public const int STBI_rgb = 3;

		// Token: 0x04000056 RID: 86
		public const int STBI_rgb_alpha = 4;

		// Token: 0x04000057 RID: 87
		public const int STBI_ORDER_RGB = 0;

		// Token: 0x04000058 RID: 88
		public const int STBI_ORDER_BGR = 1;

		// Token: 0x04000059 RID: 89
		public const int STBI__SCAN_load = 0;

		// Token: 0x0400005A RID: 90
		public const int STBI__SCAN_type = 1;

		// Token: 0x0400005B RID: 91
		public const int STBI__SCAN_header = 2;

		// Token: 0x0400005C RID: 92
		public const int STBI__F_none = 0;

		// Token: 0x0400005D RID: 93
		public const int STBI__F_sub = 1;

		// Token: 0x0400005E RID: 94
		public const int STBI__F_up = 2;

		// Token: 0x0400005F RID: 95
		public const int STBI__F_avg = 3;

		// Token: 0x04000060 RID: 96
		public const int STBI__F_paeth = 4;

		// Token: 0x04000061 RID: 97
		public const int STBI__F_avg_first = 5;

		// Token: 0x04000062 RID: 98
		public const int STBI__F_paeth_first = 6;

		// Token: 0x04000063 RID: 99
		public static float stbi__h2l_gamma_i = 0.45454544f;

		// Token: 0x04000064 RID: 100
		public static float stbi__h2l_scale_i = 1f;

		// Token: 0x04000065 RID: 101
		public static uint[] stbi__bmask = new uint[]
		{
			0U, 1U, 3U, 7U, 15U, 31U, 63U, 127U, 255U, 511U,
			1023U, 2047U, 4095U, 8191U, 16383U, 32767U, 65535U
		};

		// Token: 0x04000066 RID: 102
		public static int[] stbi__jbias = new int[]
		{
			0, -1, -3, -7, -15, -31, -63, -127, -255, -511,
			-1023, -2047, -4095, -8191, -16383, -32767
		};

		// Token: 0x04000067 RID: 103
		public static byte[] stbi__jpeg_dezigzag = new byte[]
		{
			0, 1, 8, 16, 9, 2, 3, 10, 17, 24,
			32, 25, 18, 11, 4, 5, 12, 19, 26, 33,
			40, 48, 41, 34, 27, 20, 13, 6, 7, 14,
			21, 28, 35, 42, 49, 56, 57, 50, 43, 36,
			29, 22, 15, 23, 30, 37, 44, 51, 58, 59,
			52, 45, 38, 31, 39, 46, 53, 60, 61, 54,
			47, 55, 62, 63, 63, 63, 63, 63, 63, 63,
			63, 63, 63, 63, 63, 63, 63, 63, 63
		};

		// Token: 0x04000068 RID: 104
		public static int[] stbi__zlength_base = new int[]
		{
			3, 4, 5, 6, 7, 8, 9, 10, 11, 13,
			15, 17, 19, 23, 27, 31, 35, 43, 51, 59,
			67, 83, 99, 115, 131, 163, 195, 227, 258, 0,
			0
		};

		// Token: 0x04000069 RID: 105
		public static int[] stbi__zlength_extra = new int[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 1, 1,
			1, 1, 2, 2, 2, 2, 3, 3, 3, 3,
			4, 4, 4, 4, 5, 5, 5, 5, 0, 0,
			0
		};

		// Token: 0x0400006A RID: 106
		public static int[] stbi__zdist_base = new int[]
		{
			1, 2, 3, 4, 5, 7, 9, 13, 17, 25,
			33, 49, 65, 97, 129, 193, 257, 385, 513, 769,
			1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577,
			0, 0
		};

		// Token: 0x0400006B RID: 107
		public static int[] stbi__zdist_extra = new int[]
		{
			0, 0, 0, 0, 1, 1, 2, 2, 3, 3,
			4, 4, 5, 5, 6, 6, 7, 7, 8, 8,
			9, 9, 10, 10, 11, 11, 12, 12, 13, 13
		};

		// Token: 0x0400006C RID: 108
		public static byte[] length_dezigzag = new byte[]
		{
			16, 17, 18, 0, 8, 7, 9, 6, 10, 5,
			11, 4, 12, 3, 13, 2, 14, 1, 15
		};

		// Token: 0x0400006D RID: 109
		public static byte[] stbi__zdefault_length = new byte[]
		{
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
			8, 8, 8, 8, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
			9, 9, 9, 9, 9, 9, 7, 7, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
			8, 8, 8, 8, 8, 8, 8, 8
		};

		// Token: 0x0400006E RID: 110
		public static byte[] stbi__zdefault_distance = new byte[]
		{
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5
		};

		// Token: 0x0400006F RID: 111
		public static byte[] png_sig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

		// Token: 0x04000070 RID: 112
		public static byte[] first_row_filter = new byte[] { 0, 1, 0, 5, 6 };

		// Token: 0x04000071 RID: 113
		public static byte[] stbi__depth_scale_table = new byte[] { 0, byte.MaxValue, 85, 0, 17, 0, 0, 0, 1 };

		// Token: 0x04000072 RID: 114
		public static int stbi__unpremultiply_on_load = 0;

		// Token: 0x04000073 RID: 115
		public static int stbi__de_iphone_flag = 0;

		// Token: 0x02000018 RID: 24
		// (Invoke) Token: 0x0600024D RID: 589
		public unsafe delegate int ReadCallback(void* user, sbyte* data, int size);

		// Token: 0x02000019 RID: 25
		// (Invoke) Token: 0x06000251 RID: 593
		public unsafe delegate int SkipCallback(void* user, int n);

		// Token: 0x0200001A RID: 26
		// (Invoke) Token: 0x06000255 RID: 597
		public unsafe delegate int EofCallback(void* user);

		// Token: 0x0200001B RID: 27
		// (Invoke) Token: 0x06000259 RID: 601
		public unsafe delegate void idct_block_kernel(byte* output, int out_stride, short* data);

		// Token: 0x0200001C RID: 28
		// (Invoke) Token: 0x0600025D RID: 605
		public unsafe delegate void YCbCr_to_RGB_kernel(byte* output, byte* y, byte* pcb, byte* pcr, int count, int step);

		// Token: 0x0200001D RID: 29
		// (Invoke) Token: 0x06000261 RID: 609
		public unsafe delegate byte* Resampler(byte* a, byte* b, byte* c, int d, int e);

		// Token: 0x0200001E RID: 30
		public class stbi_io_callbacks
		{
			// Token: 0x04000135 RID: 309
			public StbImage.ReadCallback read;

			// Token: 0x04000136 RID: 310
			public StbImage.SkipCallback skip;

			// Token: 0x04000137 RID: 311
			public StbImage.EofCallback eof;
		}

		// Token: 0x0200001F RID: 31
		public struct img_comp
		{
			// Token: 0x04000138 RID: 312
			public int id;

			// Token: 0x04000139 RID: 313
			public int h;

			// Token: 0x0400013A RID: 314
			public int v;

			// Token: 0x0400013B RID: 315
			public int tq;

			// Token: 0x0400013C RID: 316
			public int hd;

			// Token: 0x0400013D RID: 317
			public int ha;

			// Token: 0x0400013E RID: 318
			public int dc_pred;

			// Token: 0x0400013F RID: 319
			public int x;

			// Token: 0x04000140 RID: 320
			public int y;

			// Token: 0x04000141 RID: 321
			public int w2;

			// Token: 0x04000142 RID: 322
			public int h2;

			// Token: 0x04000143 RID: 323
			public unsafe byte* data;

			// Token: 0x04000144 RID: 324
			public unsafe void* raw_data;

			// Token: 0x04000145 RID: 325
			public unsafe void* raw_coeff;

			// Token: 0x04000146 RID: 326
			public unsafe byte* linebuf;

			// Token: 0x04000147 RID: 327
			public unsafe short* coeff;

			// Token: 0x04000148 RID: 328
			public int coeff_w;

			// Token: 0x04000149 RID: 329
			public int coeff_h;
		}

		// Token: 0x02000020 RID: 32
		public class stbi__jpeg
		{
			// Token: 0x06000265 RID: 613 RVA: 0x000207C0 File Offset: 0x0001E9C0
			public stbi__jpeg()
			{
				for (int i = 0; i < 4; i++)
				{
					this.huff_ac[i] = new StbImage.stbi__huffman();
					this.huff_dc[i] = new StbImage.stbi__huffman();
				}
				for (int j = 0; j < this.img_comp.Length; j++)
				{
					this.img_comp[j] = default(StbImage.img_comp);
				}
				this.fast_ac = new short[4][];
				for (int k = 0; k < this.fast_ac.Length; k++)
				{
					this.fast_ac[k] = new short[512];
				}
				this.dequant = new ushort[4][];
				for (int l = 0; l < this.dequant.Length; l++)
				{
					this.dequant[l] = new ushort[64];
				}
			}

			// Token: 0x0400014A RID: 330
			public StbImage.stbi__context s;

			// Token: 0x0400014B RID: 331
			public readonly StbImage.stbi__huffman[] huff_dc = new StbImage.stbi__huffman[4];

			// Token: 0x0400014C RID: 332
			public readonly StbImage.stbi__huffman[] huff_ac = new StbImage.stbi__huffman[4];

			// Token: 0x0400014D RID: 333
			public readonly ushort[][] dequant;

			// Token: 0x0400014E RID: 334
			public readonly short[][] fast_ac;

			// Token: 0x0400014F RID: 335
			public int img_h_max;

			// Token: 0x04000150 RID: 336
			public int img_v_max;

			// Token: 0x04000151 RID: 337
			public int img_mcu_x;

			// Token: 0x04000152 RID: 338
			public int img_mcu_y;

			// Token: 0x04000153 RID: 339
			public int img_mcu_w;

			// Token: 0x04000154 RID: 340
			public int img_mcu_h;

			// Token: 0x04000155 RID: 341
			public StbImage.img_comp[] img_comp = new StbImage.img_comp[4];

			// Token: 0x04000156 RID: 342
			public uint code_buffer;

			// Token: 0x04000157 RID: 343
			public int code_bits;

			// Token: 0x04000158 RID: 344
			public byte marker;

			// Token: 0x04000159 RID: 345
			public int nomore;

			// Token: 0x0400015A RID: 346
			public int progressive;

			// Token: 0x0400015B RID: 347
			public int spec_start;

			// Token: 0x0400015C RID: 348
			public int spec_end;

			// Token: 0x0400015D RID: 349
			public int succ_high;

			// Token: 0x0400015E RID: 350
			public int succ_low;

			// Token: 0x0400015F RID: 351
			public int eob_run;

			// Token: 0x04000160 RID: 352
			public int jfif;

			// Token: 0x04000161 RID: 353
			public int app14_color_transform;

			// Token: 0x04000162 RID: 354
			public int rgb;

			// Token: 0x04000163 RID: 355
			public int scan_n;

			// Token: 0x04000164 RID: 356
			public int[] order = new int[4];

			// Token: 0x04000165 RID: 357
			public int restart_interval;

			// Token: 0x04000166 RID: 358
			public int todo;

			// Token: 0x04000167 RID: 359
			public StbImage.idct_block_kernel idct_block_kernel;

			// Token: 0x04000168 RID: 360
			public StbImage.YCbCr_to_RGB_kernel YCbCr_to_RGB_kernel;

			// Token: 0x04000169 RID: 361
			public StbImage.Resampler resample_row_hv_2_kernel;
		}

		// Token: 0x02000021 RID: 33
		public class stbi__resample
		{
			// Token: 0x0400016A RID: 362
			public StbImage.Resampler resample;

			// Token: 0x0400016B RID: 363
			public unsafe byte* line0;

			// Token: 0x0400016C RID: 364
			public unsafe byte* line1;

			// Token: 0x0400016D RID: 365
			public int hs;

			// Token: 0x0400016E RID: 366
			public int vs;

			// Token: 0x0400016F RID: 367
			public int w_lores;

			// Token: 0x04000170 RID: 368
			public int ystep;

			// Token: 0x04000171 RID: 369
			public int ypos;
		}

		// Token: 0x02000022 RID: 34
		public struct stbi__gif_lzw
		{
			// Token: 0x04000172 RID: 370
			public short prefix;

			// Token: 0x04000173 RID: 371
			public byte first;

			// Token: 0x04000174 RID: 372
			public byte suffix;
		}

		// Token: 0x02000023 RID: 35
		public class stbi__gif
		{
			// Token: 0x06000267 RID: 615 RVA: 0x000208AD File Offset: 0x0001EAAD
			public unsafe stbi__gif()
			{
				this.codes = (StbImage.stbi__gif_lzw*)StbImage.stbi__malloc(4096 * sizeof(StbImage.stbi__gif_lzw));
				this.pal = (byte*)StbImage.stbi__malloc(1024);
				this.lpal = (byte*)StbImage.stbi__malloc(1024);
			}

			// Token: 0x04000175 RID: 373
			public int w;

			// Token: 0x04000176 RID: 374
			public int h;

			// Token: 0x04000177 RID: 375
			public unsafe byte* _out_;

			// Token: 0x04000178 RID: 376
			public unsafe byte* old_out;

			// Token: 0x04000179 RID: 377
			public int flags;

			// Token: 0x0400017A RID: 378
			public int bgindex;

			// Token: 0x0400017B RID: 379
			public int ratio;

			// Token: 0x0400017C RID: 380
			public int transparent;

			// Token: 0x0400017D RID: 381
			public int eflags;

			// Token: 0x0400017E RID: 382
			public int delay;

			// Token: 0x0400017F RID: 383
			public unsafe byte* pal;

			// Token: 0x04000180 RID: 384
			public unsafe byte* lpal;

			// Token: 0x04000181 RID: 385
			public unsafe StbImage.stbi__gif_lzw* codes;

			// Token: 0x04000182 RID: 386
			public unsafe byte* color_table;

			// Token: 0x04000183 RID: 387
			public int parse;

			// Token: 0x04000184 RID: 388
			public int step;

			// Token: 0x04000185 RID: 389
			public int lflags;

			// Token: 0x04000186 RID: 390
			public int start_x;

			// Token: 0x04000187 RID: 391
			public int start_y;

			// Token: 0x04000188 RID: 392
			public int max_x;

			// Token: 0x04000189 RID: 393
			public int max_y;

			// Token: 0x0400018A RID: 394
			public int cur_x;

			// Token: 0x0400018B RID: 395
			public int cur_y;

			// Token: 0x0400018C RID: 396
			public int line_size;
		}

		// Token: 0x02000024 RID: 36
		public class stbi__context
		{
			// Token: 0x06000268 RID: 616 RVA: 0x000208EC File Offset: 0x0001EAEC
			public unsafe stbi__context()
			{
			}

			// Token: 0x0400018D RID: 397
			public uint img_x;

			// Token: 0x0400018E RID: 398
			public uint img_y;

			// Token: 0x0400018F RID: 399
			public int img_n;

			// Token: 0x04000190 RID: 400
			public int img_out_n;

			// Token: 0x04000191 RID: 401
			public StbImage.stbi_io_callbacks io = new StbImage.stbi_io_callbacks();

			// Token: 0x04000192 RID: 402
			public unsafe void* io_user_data;

			// Token: 0x04000193 RID: 403
			public int read_from_callbacks;

			// Token: 0x04000194 RID: 404
			public int buflen;

			// Token: 0x04000195 RID: 405
			public unsafe byte* buffer_start = (byte*)StbImage.stbi__malloc(128);

			// Token: 0x04000196 RID: 406
			public unsafe byte* img_buffer;

			// Token: 0x04000197 RID: 407
			public unsafe byte* img_buffer_end;

			// Token: 0x04000198 RID: 408
			public unsafe byte* img_buffer_original;

			// Token: 0x04000199 RID: 409
			public unsafe byte* img_buffer_original_end;
		}

		// Token: 0x02000025 RID: 37
		public struct stbi__result_info
		{
			// Token: 0x0400019A RID: 410
			public int bits_per_channel;

			// Token: 0x0400019B RID: 411
			public int num_channels;

			// Token: 0x0400019C RID: 412
			public int channel_order;
		}

		// Token: 0x02000026 RID: 38
		public class stbi__huffman
		{
			// Token: 0x0400019D RID: 413
			public byte[] fast = new byte[512];

			// Token: 0x0400019E RID: 414
			public ushort[] code = new ushort[256];

			// Token: 0x0400019F RID: 415
			public byte[] values = new byte[256];

			// Token: 0x040001A0 RID: 416
			public byte[] size = new byte[257];

			// Token: 0x040001A1 RID: 417
			public uint[] maxcode = new uint[18];

			// Token: 0x040001A2 RID: 418
			public int[] delta = new int[17];
		}

		// Token: 0x02000027 RID: 39
		public struct stbi__zhuffman
		{
			// Token: 0x040001A3 RID: 419
			[FixedBuffer(typeof(ushort), 512)]
			public StbImage.stbi__zhuffman.<fast>e__FixedBuffer fast;

			// Token: 0x040001A4 RID: 420
			[FixedBuffer(typeof(ushort), 16)]
			public StbImage.stbi__zhuffman.<firstcode>e__FixedBuffer firstcode;

			// Token: 0x040001A5 RID: 421
			[FixedBuffer(typeof(int), 17)]
			public StbImage.stbi__zhuffman.<maxcode>e__FixedBuffer maxcode;

			// Token: 0x040001A6 RID: 422
			[FixedBuffer(typeof(ushort), 16)]
			public StbImage.stbi__zhuffman.<firstsymbol>e__FixedBuffer firstsymbol;

			// Token: 0x040001A7 RID: 423
			[FixedBuffer(typeof(byte), 288)]
			public StbImage.stbi__zhuffman.<size>e__FixedBuffer size;

			// Token: 0x040001A8 RID: 424
			[FixedBuffer(typeof(ushort), 288)]
			public StbImage.stbi__zhuffman.<value>e__FixedBuffer value;

			// Token: 0x02000068 RID: 104
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 1024)]
			public struct <fast>e__FixedBuffer
			{
				// Token: 0x040002C4 RID: 708
				public ushort FixedElementField;
			}

			// Token: 0x02000069 RID: 105
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 32)]
			public struct <firstcode>e__FixedBuffer
			{
				// Token: 0x040002C5 RID: 709
				public ushort FixedElementField;
			}

			// Token: 0x0200006A RID: 106
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 32)]
			public struct <firstsymbol>e__FixedBuffer
			{
				// Token: 0x040002C6 RID: 710
				public ushort FixedElementField;
			}

			// Token: 0x0200006B RID: 107
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 68)]
			public struct <maxcode>e__FixedBuffer
			{
				// Token: 0x040002C7 RID: 711
				public int FixedElementField;
			}

			// Token: 0x0200006C RID: 108
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 288)]
			public struct <size>e__FixedBuffer
			{
				// Token: 0x040002C8 RID: 712
				public byte FixedElementField;
			}

			// Token: 0x0200006D RID: 109
			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 576)]
			public struct <value>e__FixedBuffer
			{
				// Token: 0x040002C9 RID: 713
				public ushort FixedElementField;
			}
		}

		// Token: 0x02000028 RID: 40
		public struct stbi__zbuf
		{
			// Token: 0x040001A9 RID: 425
			public unsafe byte* zbuffer;

			// Token: 0x040001AA RID: 426
			public unsafe byte* zbuffer_end;

			// Token: 0x040001AB RID: 427
			public int num_bits;

			// Token: 0x040001AC RID: 428
			public uint code_buffer;

			// Token: 0x040001AD RID: 429
			public unsafe sbyte* zout;

			// Token: 0x040001AE RID: 430
			public unsafe sbyte* zout_start;

			// Token: 0x040001AF RID: 431
			public unsafe sbyte* zout_end;

			// Token: 0x040001B0 RID: 432
			public int z_expandable;

			// Token: 0x040001B1 RID: 433
			public StbImage.stbi__zhuffman z_length;

			// Token: 0x040001B2 RID: 434
			public StbImage.stbi__zhuffman z_distance;
		}

		// Token: 0x02000029 RID: 41
		public struct stbi__pngchunk
		{
			// Token: 0x040001B3 RID: 435
			public uint length;

			// Token: 0x040001B4 RID: 436
			public uint type;
		}

		// Token: 0x0200002A RID: 42
		public class stbi__png
		{
			// Token: 0x040001B5 RID: 437
			public StbImage.stbi__context s = new StbImage.stbi__context();

			// Token: 0x040001B6 RID: 438
			public unsafe byte* idata;

			// Token: 0x040001B7 RID: 439
			public unsafe byte* expanded;

			// Token: 0x040001B8 RID: 440
			public unsafe byte* _out_;

			// Token: 0x040001B9 RID: 441
			public int depth;
		}

		// Token: 0x0200002B RID: 43
		public struct stbi__bmp_data
		{
			// Token: 0x040001BA RID: 442
			public int bpp;

			// Token: 0x040001BB RID: 443
			public int offset;

			// Token: 0x040001BC RID: 444
			public int hsz;

			// Token: 0x040001BD RID: 445
			public uint mr;

			// Token: 0x040001BE RID: 446
			public uint mg;

			// Token: 0x040001BF RID: 447
			public uint mb;

			// Token: 0x040001C0 RID: 448
			public uint ma;

			// Token: 0x040001C1 RID: 449
			public uint all_a;
		}
	}
}
