using System;
using System.Text;

namespace StbSharp
{
	// Token: 0x02000008 RID: 8
	public static class StbImageWrite
	{
		// Token: 0x06000073 RID: 115 RVA: 0x00006878 File Offset: 0x00004A78
		public unsafe static void stbi__start_write_callbacks(StbImageWrite.stbi__write_context s, StbImageWrite.WriteCallback c, void* context)
		{
			s.func = c;
			s.context = context;
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00006888 File Offset: 0x00004A88
		public unsafe static void stbiw__writefv(StbImageWrite.stbi__write_context s, string fmt, params object[] v)
		{
			int num = 0;
			foreach (char c in fmt)
			{
				if (c != ' ')
				{
					switch (c)
					{
					case '1':
					{
						byte b = (byte)((int)v[num++] & 255);
						s.func(s.context, (void*)(&b), 1);
						break;
					}
					case '2':
					{
						int num2 = (int)v[num++];
						byte* ptr = stackalloc byte[(UIntPtr)2];
						*ptr = (byte)(num2 & 255);
						ptr[1] = (byte)((num2 >> 8) & 255);
						s.func(s.context, (void*)ptr, 2);
						break;
					}
					case '4':
					{
						int num3 = (int)v[num++];
						byte* ptr2 = stackalloc byte[(UIntPtr)4];
						*ptr2 = (byte)(num3 & 255);
						ptr2[1] = (byte)((num3 >> 8) & 255);
						ptr2[2] = (byte)((num3 >> 16) & 255);
						ptr2[3] = (byte)((num3 >> 24) & 255);
						s.func(s.context, (void*)ptr2, 4);
						break;
					}
					}
				}
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000069B9 File Offset: 0x00004BB9
		public static void stbiw__writef(StbImageWrite.stbi__write_context s, string fmt, params object[] v)
		{
			StbImageWrite.stbiw__writefv(s, fmt, v);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000069C4 File Offset: 0x00004BC4
		public unsafe static int stbiw__outfile(StbImageWrite.stbi__write_context s, int rgb_dir, int vdir, int x, int y, int comp, int expand_mono, void* data, int alpha, int pad, string fmt, params object[] v)
		{
			if (y < 0 || x < 0)
			{
				return 0;
			}
			StbImageWrite.stbiw__writefv(s, fmt, v);
			StbImageWrite.stbiw__write_pixels(s, rgb_dir, vdir, x, y, comp, data, alpha, pad, expand_mono);
			return 1;
		}

		// Token: 0x06000077 RID: 119 RVA: 0x000069FC File Offset: 0x00004BFC
		public unsafe static int stbi_write_bmp_to_func(StbImageWrite.WriteCallback func, void* context, int x, int y, int comp, void* data)
		{
			StbImageWrite.stbi__write_context s = new StbImageWrite.stbi__write_context();
			StbImageWrite.stbi__start_write_callbacks(s, func, context);
			return StbImageWrite.stbi_write_bmp_core(s, x, y, comp, data);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00006A16 File Offset: 0x00004C16
		public unsafe static int stbi_write_tga_to_func(StbImageWrite.WriteCallback func, void* context, int x, int y, int comp, void* data)
		{
			StbImageWrite.stbi__write_context s = new StbImageWrite.stbi__write_context();
			StbImageWrite.stbi__start_write_callbacks(s, func, context);
			return StbImageWrite.stbi_write_tga_core(s, x, y, comp, data);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00006A30 File Offset: 0x00004C30
		public unsafe static int stbi_write_hdr_to_func(StbImageWrite.WriteCallback func, void* context, int x, int y, int comp, float* data)
		{
			StbImageWrite.stbi__write_context s = new StbImageWrite.stbi__write_context();
			StbImageWrite.stbi__start_write_callbacks(s, func, context);
			return StbImageWrite.stbi_write_hdr_core(s, x, y, comp, data);
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00006A4C File Offset: 0x00004C4C
		public unsafe static int stbi_write_png_to_func(StbImageWrite.WriteCallback func, void* context, int x, int y, int comp, void* data, int stride_bytes)
		{
			int size;
			byte* ptr = StbImageWrite.stbi_write_png_to_mem((byte*)data, stride_bytes, x, y, comp, &size);
			if (ptr == null)
			{
				return 0;
			}
			func(context, (void*)ptr, size);
			CRuntime.free((void*)ptr);
			return 1;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00006A82 File Offset: 0x00004C82
		public unsafe static int stbi_write_jpg_to_func(StbImageWrite.WriteCallback func, void* context, int x, int y, int comp, void* data, int quality)
		{
			StbImageWrite.stbi__write_context s = new StbImageWrite.stbi__write_context();
			StbImageWrite.stbi__start_write_callbacks(s, func, context);
			return StbImageWrite.stbi_write_jpg_core(s, x, y, comp, data, quality);
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00006AA0 File Offset: 0x00004CA0
		public unsafe static int stbi_write_hdr_core(StbImageWrite.stbi__write_context s, int x, int y, int comp, float* data)
		{
			if (y <= 0 || x <= 0 || data == null)
			{
				return 0;
			}
			byte* ptr = (byte*)CRuntime.malloc((ulong)((long)(x * 4)));
			string s2 = "#?RADIANCE\n# Written by stb_image_write.h\nFORMAT=32-bit_rle_rgbe\n";
			byte[] bytes = Encoding.UTF8.GetBytes(s2);
			byte[] array;
			byte* data2;
			if ((array = bytes) == null || array.Length == 0)
			{
				data2 = null;
			}
			else
			{
				data2 = &array[0];
			}
			s.func(s.context, (void*)data2, bytes.Length);
			array = null;
			string s3 = string.Format("EXPOSURE=          1.0000000000000\n\n-Y {0} +X {1}\n", y, x);
			bytes = Encoding.UTF8.GetBytes(s3);
			byte* data3;
			if ((array = bytes) == null || array.Length == 0)
			{
				data3 = null;
			}
			else
			{
				data3 = &array[0];
			}
			s.func(s.context, (void*)data3, bytes.Length);
			array = null;
			for (int i = 0; i < y; i++)
			{
				StbImageWrite.stbiw__write_hdr_scanline(s, x, comp, ptr, data + comp * i * x);
			}
			CRuntime.free((void*)ptr);
			return 1;
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00006B93 File Offset: 0x00004D93
		public unsafe static void stbiw__putc(StbImageWrite.stbi__write_context s, byte c)
		{
			s.func(s.context, (void*)(&c), 1);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00006BAC File Offset: 0x00004DAC
		public unsafe static void stbiw__write3(StbImageWrite.stbi__write_context s, byte a, byte b, byte c)
		{
			byte* ptr = stackalloc byte[(UIntPtr)3];
			*ptr = a;
			ptr[1] = b;
			ptr[2] = c;
			s.func(s.context, (void*)ptr, 3);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00006BE0 File Offset: 0x00004DE0
		public unsafe static void stbiw__write_pixel(StbImageWrite.stbi__write_context s, int rgb_dir, int comp, int write_alpha, int expand_mono, byte* d)
		{
			byte* ptr = stackalloc byte[(UIntPtr)3];
			*ptr = byte.MaxValue;
			ptr[1] = 0;
			ptr[2] = byte.MaxValue;
			byte* ptr2 = stackalloc byte[(UIntPtr)3];
			if (write_alpha < 0)
			{
				s.func(s.context, (void*)(d + (comp - 1)), 1);
			}
			if (comp - 1 > 1)
			{
				if (comp - 3 <= 1)
				{
					if (comp == 4 && write_alpha == 0)
					{
						for (int i = 0; i < 3; i++)
						{
							ptr2[i] = ptr[i] + (d[i] - ptr[i]) * d[3] / byte.MaxValue;
						}
						StbImageWrite.stbiw__write3(s, ptr2[1 - rgb_dir], ptr2[1], ptr2[1 + rgb_dir]);
					}
					else
					{
						StbImageWrite.stbiw__write3(s, d[1 - rgb_dir], d[1], d[1 + rgb_dir]);
					}
				}
			}
			else if (expand_mono != 0)
			{
				StbImageWrite.stbiw__write3(s, *d, *d, *d);
			}
			else
			{
				s.func(s.context, (void*)d, 1);
			}
			if (write_alpha > 0)
			{
				s.func(s.context, (void*)(d + (comp - 1)), 1);
			}
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00006CEC File Offset: 0x00004EEC
		public unsafe static void stbiw__write_pixels(StbImageWrite.stbi__write_context s, int rgb_dir, int vdir, int x, int y, int comp, void* data, int write_alpha, int scanline_pad, int expand_mono)
		{
			uint num = 0U;
			if (y <= 0)
			{
				return;
			}
			int num2;
			int num3;
			if (vdir < 0)
			{
				num2 = -1;
				num3 = y - 1;
			}
			else
			{
				num2 = y;
				num3 = 0;
			}
			while (num3 != num2)
			{
				for (int i = 0; i < x; i++)
				{
					byte* d = (byte*)data + (num3 * x + i) * comp;
					StbImageWrite.stbiw__write_pixel(s, rgb_dir, comp, write_alpha, expand_mono, d);
				}
				s.func(s.context, (void*)(&num), scanline_pad);
				num3 += vdir;
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00006D5C File Offset: 0x00004F5C
		public unsafe static int stbi_write_bmp_core(StbImageWrite.stbi__write_context s, int x, int y, int comp, void* data)
		{
			int num = (-x * 3) & 3;
			return StbImageWrite.stbiw__outfile(s, -1, -1, x, y, comp, 1, data, 0, num, "11 4 22 44 44 22 444444", new object[]
			{
				66,
				77,
				54 + (x * 3 + num) * y,
				0,
				0,
				54,
				40,
				x,
				y,
				1,
				24,
				0,
				0,
				0,
				0,
				0,
				0
			});
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00006E3C File Offset: 0x0000503C
		public unsafe static int stbi_write_tga_core(StbImageWrite.stbi__write_context s, int x, int y, int comp, void* data)
		{
			int num = ((comp == 2 || comp == 4) ? 1 : 0);
			int num2 = ((num != 0) ? (comp - 1) : comp);
			int num3 = ((num2 < 2) ? 3 : 2);
			if (y < 0 || x < 0)
			{
				return 0;
			}
			if (StbImageWrite.stbi_write_tga_with_rle == 0)
			{
				return StbImageWrite.stbiw__outfile(s, -1, -1, x, y, comp, 0, data, num, 0, "111 221 2222 11", new object[]
				{
					0,
					0,
					num3,
					0,
					0,
					0,
					0,
					0,
					x,
					y,
					(num2 + num) * 8,
					num * 8
				});
			}
			StbImageWrite.stbiw__writef(s, "111 221 2222 11", new object[]
			{
				0,
				0,
				num3 + 8,
				0,
				0,
				0,
				0,
				0,
				x,
				y,
				(num2 + num) * 8,
				num * 8
			});
			for (int i = y - 1; i >= 0; i--)
			{
				byte* ptr = (byte*)data + i * x * comp;
				int num5;
				for (int j = 0; j < x; j += num5)
				{
					byte* ptr2 = ptr + j * comp;
					int num4 = 1;
					num5 = 1;
					if (j < x - 1)
					{
						num5++;
						num4 = CRuntime.memcmp((void*)ptr2, (void*)(ptr + (j + 1) * comp), (ulong)((long)comp));
						if (num4 != 0)
						{
							byte* ptr3 = ptr2;
							for (int k = j + 2; k < x; k++)
							{
								if (num5 >= 128)
								{
									break;
								}
								if (CRuntime.memcmp((void*)ptr3, (void*)(ptr + k * comp), (ulong)((long)comp)) == 0)
								{
									num5--;
									break;
								}
								ptr3 += comp;
								num5++;
							}
						}
						else
						{
							int k = j + 2;
							while (k < x && num5 < 128 && CRuntime.memcmp((void*)ptr2, (void*)(ptr + k * comp), (ulong)((long)comp)) == 0)
							{
								num5++;
								k++;
							}
						}
					}
					if (num4 != 0)
					{
						byte b = (byte)((num5 - 1) & 255);
						s.func(s.context, (void*)(&b), 1);
						for (int k = 0; k < num5; k++)
						{
							StbImageWrite.stbiw__write_pixel(s, -1, comp, num, 0, ptr2 + k * comp);
						}
					}
					else
					{
						byte b2 = (byte)((num5 - 129) & 255);
						s.func(s.context, (void*)(&b2), 1);
						StbImageWrite.stbiw__write_pixel(s, -1, comp, num, 0, ptr2);
					}
				}
			}
			return 1;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00007100 File Offset: 0x00005300
		public unsafe static void stbiw__linear_to_rgbe(byte* rgbe, float* linear)
		{
			float num = ((*linear > ((linear[1] > linear[2]) ? linear[1] : linear[2])) ? (*linear) : ((linear[1] > linear[2]) ? linear[1] : linear[2]));
			if (num < 1E-32f)
			{
				*rgbe = (rgbe[1] = (rgbe[2] = (rgbe[3] = 0)));
				return;
			}
			int num3;
			float num2 = (float)CRuntime.frexp((double)num, &num3) * 256f / num;
			*rgbe = (byte)(*linear * num2);
			rgbe[1] = (byte)(linear[1] * num2);
			rgbe[2] = (byte)(linear[2] * num2);
			rgbe[3] = (byte)(num3 + 128);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000071B0 File Offset: 0x000053B0
		public unsafe static void stbiw__write_run_data(StbImageWrite.stbi__write_context s, int length, byte databyte)
		{
			byte b = (byte)((length + 128) & 255);
			s.func(s.context, (void*)(&b), 1);
			s.func(s.context, (void*)(&databyte), 1);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000071F8 File Offset: 0x000053F8
		public unsafe static void stbiw__write_dump_data(StbImageWrite.stbi__write_context s, int length, byte* data)
		{
			byte b = (byte)(length & 255);
			s.func(s.context, (void*)(&b), 1);
			s.func(s.context, (void*)data, length);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00007238 File Offset: 0x00005438
		public unsafe static void stbiw__write_hdr_scanline(StbImageWrite.stbi__write_context s, int width, int ncomp, byte* scratch, float* scanline)
		{
			byte* ptr = stackalloc byte[(UIntPtr)4];
			*ptr = 2;
			ptr[1] = 2;
			ptr[2] = 0;
			ptr[3] = 0;
			byte* ptr2 = stackalloc byte[(UIntPtr)4];
			float* ptr3 = stackalloc float[(UIntPtr)12];
			ptr[2] = (byte)((width & 65280) >> 8);
			ptr[3] = (byte)(width & 255);
			if (width < 8 || width >= 32768)
			{
				for (int i = 0; i < width; i++)
				{
					if (ncomp - 3 <= 1)
					{
						ptr3[2] = scanline[i * ncomp + 2];
						ptr3[1] = scanline[i * ncomp + 1];
						*ptr3 = scanline[i * ncomp];
					}
					else
					{
						*ptr3 = (ptr3[1] = (ptr3[2] = scanline[i * ncomp]));
					}
					StbImageWrite.stbiw__linear_to_rgbe(ptr2, ptr3);
					s.func(s.context, (void*)ptr2, 4);
				}
				return;
			}
			for (int i = 0; i < width; i++)
			{
				if (ncomp - 3 <= 1)
				{
					ptr3[2] = scanline[i * ncomp + 2];
					ptr3[1] = scanline[i * ncomp + 1];
					*ptr3 = scanline[i * ncomp];
				}
				else
				{
					*ptr3 = (ptr3[1] = (ptr3[2] = scanline[i * ncomp]));
				}
				StbImageWrite.stbiw__linear_to_rgbe(ptr2, ptr3);
				scratch[i] = *ptr2;
				scratch[i + width] = ptr2[1];
				scratch[i + width * 2] = ptr2[2];
				scratch[i + width * 3] = ptr2[3];
			}
			s.func(s.context, (void*)ptr, 4);
			for (int j = 0; j < 4; j++)
			{
				byte* ptr4 = scratch + width * j;
				int i = 0;
				while (i < width)
				{
					int k = i;
					while (k + 2 < width && (ptr4[k] != ptr4[k + 1] || ptr4[k] != ptr4[k + 2]))
					{
						k++;
					}
					if (k + 2 >= width)
					{
						k = width;
					}
					while (i < k)
					{
						int num = k - i;
						if (num > 128)
						{
							num = 128;
						}
						StbImageWrite.stbiw__write_dump_data(s, num, ptr4 + i);
						i += num;
					}
					if (k + 2 < width)
					{
						while (k < width)
						{
							if (ptr4[k] != ptr4[i])
							{
								break;
							}
							k++;
						}
						while (i < k)
						{
							int num2 = k - i;
							if (num2 > 127)
							{
								num2 = 127;
							}
							StbImageWrite.stbiw__write_run_data(s, num2, ptr4[i]);
							i += num2;
						}
					}
				}
			}
		}

		// Token: 0x06000087 RID: 135 RVA: 0x000074B0 File Offset: 0x000056B0
		public unsafe static void* stbiw__sbgrowf(void** arr, int increment, int itemsize)
		{
			int num = ((*(IntPtr*)arr != (IntPtr)((UIntPtr)0)) ? (2 * *(*(IntPtr*)arr - (IntPtr)2 * 4) + increment) : (increment + 1));
			void* ptr = CRuntime.realloc((*(IntPtr*)arr != (IntPtr)((UIntPtr)0)) ? (*(IntPtr*)arr - (IntPtr)2 * 4) : null, (ulong)((long)(itemsize * num + 8)));
			if (ptr != null)
			{
				if (*(IntPtr*)arr == (IntPtr)((UIntPtr)0))
				{
					*(int*)((byte*)ptr + 4) = 0;
				}
				*(IntPtr*)arr = (byte*)ptr + (IntPtr)2 * 4;
				*(*(IntPtr*)arr - (IntPtr)2 * 4) = num;
			}
			return *(IntPtr*)arr;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00007518 File Offset: 0x00005718
		public unsafe static byte* stbiw__zlib_flushf(byte* data, uint* bitbuffer, int* bitcount)
		{
			while (*bitcount >= 8)
			{
				if (data == null || *(int*)(data - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(data - (IntPtr)2 * 4))
				{
					StbImageWrite.stbiw__sbgrowf((void**)(&data), 1, 1);
				}
				ref byte ptr = ref *data;
				byte* ptr2 = data - (IntPtr)2 * 4 + 4;
				int num = *(int*)ptr2;
				*(int*)ptr2 = num + 1;
				*((ref ptr) + num) = (byte)(*bitbuffer & 255U);
				*bitbuffer >>= 8;
				*bitcount -= 8;
			}
			return data;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x0000757C File Offset: 0x0000577C
		public static int stbiw__zlib_bitrev(int code, int codebits)
		{
			int num = 0;
			while (codebits-- != 0)
			{
				num = (num << 1) | (code & 1);
				code >>= 1;
			}
			return num;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x000075A4 File Offset: 0x000057A4
		public unsafe static uint stbiw__zlib_countm(byte* a, byte* b, int limit)
		{
			int num = 0;
			while (num < limit && num < 258 && a[num] == b[num])
			{
				num++;
			}
			return (uint)num;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x000075D0 File Offset: 0x000057D0
		public unsafe static uint stbiw__zhash(byte* data)
		{
			int num = (int)(*data) + ((int)data[1] << 8) + ((int)data[2] << 16);
			int num2 = num ^ (num << 3);
			int num3 = num2 + (int)((uint)num2 >> 5);
			int num4 = num3 ^ (num3 << 4);
			int num5 = num4 + (int)((uint)num4 >> 17);
			int num6 = num5 ^ (num5 << 25);
			return (uint)(num6 + (int)((uint)num6 >> 6));
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00007600 File Offset: 0x00005800
		public unsafe static byte* stbi_zlib_compress(byte* data, int data_len, int* out_len, int quality)
		{
			uint num = 0U;
			int num2 = 0;
			byte* ptr = null;
			byte*** ptr2 = (byte***)CRuntime.malloc((ulong)((long)(16384 * sizeof(byte**))));
			if (quality < 5)
			{
				quality = 5;
			}
			if (ptr == null || *(int*)(ptr - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(ptr - (IntPtr)2 * 4))
			{
				StbImageWrite.stbiw__sbgrowf((void**)(&ptr), 1, 1);
			}
			ref byte ptr3 = ref *ptr;
			byte* ptr4 = ptr - (IntPtr)2 * 4 + 4;
			int num3 = *(int*)ptr4;
			*(int*)ptr4 = num3 + 1;
			*((ref ptr3) + num3) = 120;
			if (ptr == null || *(int*)(ptr - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(ptr - (IntPtr)2 * 4))
			{
				StbImageWrite.stbiw__sbgrowf((void**)(&ptr), 1, 1);
			}
			ref byte ptr5 = ref *ptr;
			byte* ptr6 = ptr - (IntPtr)2 * 4 + 4;
			num3 = *(int*)ptr6;
			*(int*)ptr6 = num3 + 1;
			*((ref ptr5) + num3) = 94;
			num |= 1U << num2;
			num2++;
			ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
			num |= 1U << num2;
			num2 += 2;
			ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
			int i;
			for (i = 0; i < 16384; i++)
			{
				*(IntPtr*)(ptr2 + (IntPtr)i * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) = (IntPtr)((UIntPtr)0);
			}
			i = 0;
			int k;
			while (i < data_len - 3)
			{
				int num4 = (int)(StbImageWrite.stbiw__zhash(data + i) & 16383U);
				int j = 3;
				byte* ptr7 = null;
				byte** ptr8 = *(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**));
				int num5 = ((ptr8 != null) ? (*(int*)(ptr8 - (IntPtr)2 * 4 / (IntPtr)sizeof(byte*) + 4 / sizeof(byte*))) : 0);
				for (k = 0; k < num5; k++)
				{
					if ((*(IntPtr*)(ptr8 + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) - data) / 1 > i - 32768)
					{
						int num6 = (int)StbImageWrite.stbiw__zlib_countm(*(IntPtr*)(ptr8 + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)), data + i, data_len - i);
						if (num6 >= j)
						{
							j = num6;
							ptr7 = *(IntPtr*)(ptr8 + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*));
						}
					}
				}
				if (*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) != (IntPtr)((UIntPtr)0) && *(*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) - (IntPtr)2 * 4 + 4) == 2 * quality)
				{
					CRuntime.memmove(*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)), (void*)(*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) + (byte*)((IntPtr)quality * (IntPtr)sizeof(byte*))), (ulong)((long)(sizeof(byte*) * quality)));
					*(*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) - (IntPtr)2 * 4 + 4) = quality;
				}
				if (*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) == (IntPtr)((UIntPtr)0) || *(*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) - (IntPtr)2 * 4 + 4) + 1 >= *(*(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) - (IntPtr)2 * 4))
				{
					StbImageWrite.stbiw__sbgrowf((void**)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)), 1, sizeof(byte*));
				}
				ref IntPtr ptr9 = *(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**));
				IntPtr intPtr = *(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) - (IntPtr)2 * 4 + 4;
				num3 = *intPtr;
				*intPtr = num3 + 1;
				*((ref ptr9) + (IntPtr)num3 * (IntPtr)sizeof(byte*)) = data + i;
				if (ptr7 != null)
				{
					num4 = (int)(StbImageWrite.stbiw__zhash(data + i + 1) & 16383U);
					ptr8 = *(IntPtr*)(ptr2 + (IntPtr)num4 * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**));
					num5 = ((ptr8 != null) ? (*(int*)(ptr8 - (IntPtr)2 * 4 / (IntPtr)sizeof(byte*) + 4 / sizeof(byte*))) : 0);
					for (k = 0; k < num5; k++)
					{
						if ((*(IntPtr*)(ptr8 + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)) - data) / 1 > i - 32767 && StbImageWrite.stbiw__zlib_countm(*(IntPtr*)(ptr8 + (IntPtr)k * (IntPtr)sizeof(byte*) / (IntPtr)sizeof(byte*)), data + i + 1, data_len - i - 1) > (uint)j)
						{
							ptr7 = null;
							break;
						}
					}
				}
				if (ptr7 != null)
				{
					int l = (int)((long)(data + i - ptr7));
					k = 0;
					while (j > (int)(StbImageWrite.lengthc[k + 1] - 1))
					{
						k++;
					}
					if (k + 257 <= 143)
					{
						num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(48 + (k + 257), 8) << num2);
						num2 += 8;
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					else if (k + 257 <= 255)
					{
						num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(400 + (k + 257) - 144, 9) << num2);
						num2 += 9;
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					else if (k + 257 <= 279)
					{
						num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(k + 257 - 256, 7) << num2);
						num2 += 7;
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					else
					{
						num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(192 + (k + 257) - 280, 8) << num2);
						num2 += 8;
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					if (StbImageWrite.lengtheb[k] != 0)
					{
						num |= (uint)((uint)(j - (int)StbImageWrite.lengthc[k]) << num2);
						num2 += (int)StbImageWrite.lengtheb[k];
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					k = 0;
					while (l > (int)(StbImageWrite.distc[k + 1] - 1))
					{
						k++;
					}
					num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(k, 5) << num2);
					num2 += 5;
					ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					if (StbImageWrite.disteb[k] != 0)
					{
						num |= (uint)((uint)(l - (int)StbImageWrite.distc[k]) << num2);
						num2 += (int)StbImageWrite.disteb[k];
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					i += j;
				}
				else
				{
					if (data[i] <= 143)
					{
						num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev((int)(48 + data[i]), 8) << num2);
						num2 += 8;
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					else
					{
						num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(400 + (int)data[i] - 144, 9) << num2);
						num2 += 9;
						ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
					}
					i++;
				}
			}
			while (i < data_len)
			{
				if (data[i] <= 143)
				{
					num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev((int)(48 + data[i]), 8) << num2);
					num2 += 8;
					ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
				}
				else
				{
					num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(400 + (int)data[i] - 144, 9) << num2);
					num2 += 9;
					ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
				}
				i++;
			}
			num |= (uint)((uint)StbImageWrite.stbiw__zlib_bitrev(0, 7) << num2);
			num2 += 7;
			ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
			while (num2 != 0)
			{
				num |= 0U << num2;
				num2++;
				ptr = StbImageWrite.stbiw__zlib_flushf(ptr, &num, &num2);
			}
			for (i = 0; i < 16384; i++)
			{
				if (*(IntPtr*)(ptr2 + (IntPtr)i * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) != (IntPtr)((UIntPtr)0))
				{
					CRuntime.free(*(IntPtr*)(ptr2 + (IntPtr)i * (IntPtr)sizeof(byte**) / (IntPtr)sizeof(byte**)) - (IntPtr)2 * 4);
				}
			}
			CRuntime.free((void*)ptr2);
			uint num7 = 1U;
			uint num8 = 0U;
			int num9 = data_len % 5552;
			k = 0;
			while (k < data_len)
			{
				for (i = 0; i < num9; i++)
				{
					num7 += (uint)data[k + i];
					num8 += num7;
				}
				num7 %= 65521U;
				num8 %= 65521U;
				k += num9;
				num9 = 5552;
			}
			if (ptr == null || *(int*)(ptr - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(ptr - (IntPtr)2 * 4))
			{
				StbImageWrite.stbiw__sbgrowf((void**)(&ptr), 1, 1);
			}
			ref byte ptr10 = ref *ptr;
			byte* ptr11 = ptr - (IntPtr)2 * 4 + 4;
			num3 = *(int*)ptr11;
			*(int*)ptr11 = num3 + 1;
			*((ref ptr10) + num3) = (byte)((num8 >> 8) & 255U);
			if (ptr == null || *(int*)(ptr - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(ptr - (IntPtr)2 * 4))
			{
				StbImageWrite.stbiw__sbgrowf((void**)(&ptr), 1, 1);
			}
			ref byte ptr12 = ref *ptr;
			byte* ptr13 = ptr - (IntPtr)2 * 4 + 4;
			num3 = *(int*)ptr13;
			*(int*)ptr13 = num3 + 1;
			*((ref ptr12) + num3) = (byte)(num8 & 255U);
			if (ptr == null || *(int*)(ptr - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(ptr - (IntPtr)2 * 4))
			{
				StbImageWrite.stbiw__sbgrowf((void**)(&ptr), 1, 1);
			}
			ref byte ptr14 = ref *ptr;
			byte* ptr15 = ptr - (IntPtr)2 * 4 + 4;
			num3 = *(int*)ptr15;
			*(int*)ptr15 = num3 + 1;
			*((ref ptr14) + num3) = (byte)((num7 >> 8) & 255U);
			if (ptr == null || *(int*)(ptr - (IntPtr)2 * 4 + 4) + 1 >= *(int*)(ptr - (IntPtr)2 * 4))
			{
				StbImageWrite.stbiw__sbgrowf((void**)(&ptr), 1, 1);
			}
			ref byte ptr16 = ref *ptr;
			byte* ptr17 = ptr - (IntPtr)2 * 4 + 4;
			num3 = *(int*)ptr17;
			*(int*)ptr17 = num3 + 1;
			*((ref ptr16) + num3) = (byte)(num7 & 255U);
			*out_len = *(int*)(ptr - (IntPtr)2 * 4 + 4);
			CRuntime.memmove((void*)(ptr - (IntPtr)2 * 4), (void*)ptr, (ulong)((long)(*out_len)));
			return ptr - (IntPtr)2 * 4;
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00007E18 File Offset: 0x00006018
		public unsafe static uint stbiw__crc32(byte* buffer, int len)
		{
			uint num = uint.MaxValue;
			for (int i = 0; i < len; i++)
			{
				num = (num >> 8) ^ StbImageWrite.crc_table[(int)((uint)buffer[i] ^ (num & 255U))];
			}
			return ~num;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00007E4C File Offset: 0x0000604C
		public unsafe static void stbiw__wpcrc(byte** data, int len)
		{
			uint num = StbImageWrite.stbiw__crc32(*(IntPtr*)data - (IntPtr)len - 4, len + 4);
			*(*(IntPtr*)data) = (byte)((num >> 24) & 255U);
			*(*(IntPtr*)data + 1) = (byte)((num >> 16) & 255U);
			*(*(IntPtr*)data + 2) = (byte)((num >> 8) & 255U);
			*(*(IntPtr*)data + 3) = (byte)(num & 255U);
			*(IntPtr*)data = *(IntPtr*)data + 4;
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00007EA8 File Offset: 0x000060A8
		public static byte stbiw__paeth(int a, int b, int c)
		{
			int num = a + b - c;
			int num2 = CRuntime.abs(num - a);
			int num3 = CRuntime.abs(num - b);
			int num4 = CRuntime.abs(num - c);
			if (num2 <= num3 && num2 <= num4)
			{
				return (byte)(a & 255);
			}
			if (num3 <= num4)
			{
				return (byte)(b & 255);
			}
			return (byte)(c & 255);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00007EFC File Offset: 0x000060FC
		public unsafe static byte* stbi_write_png_to_mem(byte* pixels, int stride_bytes, int x, int y, int n, int* out_len)
		{
			int* ptr = stackalloc int[(UIntPtr)20];
			*ptr = -1;
			ptr[1] = 0;
			ptr[2] = 4;
			ptr[3] = 2;
			ptr[4] = 6;
			byte* ptr2 = stackalloc byte[(UIntPtr)8];
			*ptr2 = 137;
			ptr2[1] = 80;
			ptr2[2] = 78;
			ptr2[3] = 71;
			ptr2[4] = 13;
			ptr2[5] = 10;
			ptr2[6] = 26;
			ptr2[7] = 10;
			if (stride_bytes == 0)
			{
				stride_bytes = x * n;
			}
			byte* ptr3 = (byte*)CRuntime.malloc((ulong)((long)((x * n + 1) * y)));
			if (ptr3 == null)
			{
				return null;
			}
			sbyte* ptr4 = (sbyte*)CRuntime.malloc((ulong)((long)(x * n)));
			if (ptr4 == null)
			{
				CRuntime.free((void*)ptr3);
				return null;
			}
			for (int i = 0; i < y; i++)
			{
				int* ptr5 = stackalloc int[(UIntPtr)20];
				*ptr5 = 0;
				ptr5[1] = 1;
				ptr5[2] = 2;
				ptr5[3] = 3;
				ptr5[4] = 4;
				int* ptr6 = stackalloc int[(UIntPtr)20];
				*ptr6 = 0;
				ptr6[1] = 1;
				ptr6[2] = 0;
				ptr6[3] = 5;
				ptr6[4] = 6;
				int* ptr7 = ((i != 0) ? ptr5 : ptr6);
				int num = 0;
				int num2 = int.MaxValue;
				for (int j = 0; j < 2; j++)
				{
					for (int k = ((j != 0) ? num : 0); k < 5; k++)
					{
						int num3 = ptr7[k];
						int num4 = 0;
						byte* ptr8 = pixels + stride_bytes * i;
						for (int l = 0; l < n; l++)
						{
							switch (num3)
							{
							case 0:
								ptr4[l] = (sbyte)ptr8[l];
								break;
							case 1:
								ptr4[l] = (sbyte)ptr8[l];
								break;
							case 2:
								ptr4[l] = (sbyte)(ptr8[l] - ptr8[l - stride_bytes]);
								break;
							case 3:
								ptr4[l] = (sbyte)((int)ptr8[l] - (ptr8[l - stride_bytes] >> 1));
								break;
							case 4:
								ptr4[l] = (sbyte)(ptr8[l] - StbImageWrite.stbiw__paeth(0, (int)ptr8[l - stride_bytes], 0));
								break;
							case 5:
								ptr4[l] = (sbyte)ptr8[l];
								break;
							case 6:
								ptr4[l] = (sbyte)ptr8[l];
								break;
							}
						}
						for (int l = n; l < x * n; l++)
						{
							switch (num3)
							{
							case 0:
								ptr4[l] = (sbyte)ptr8[l];
								break;
							case 1:
								ptr4[l] = (sbyte)(ptr8[l] - ptr8[l - n]);
								break;
							case 2:
								ptr4[l] = (sbyte)(ptr8[l] - ptr8[l - stride_bytes]);
								break;
							case 3:
								ptr4[l] = (sbyte)((int)ptr8[l] - (ptr8[l - n] + ptr8[l - stride_bytes] >> 1));
								break;
							case 4:
								ptr4[l] = (sbyte)(ptr8[l] - StbImageWrite.stbiw__paeth((int)ptr8[l - n], (int)ptr8[l - stride_bytes], (int)ptr8[l - stride_bytes - n]));
								break;
							case 5:
								ptr4[l] = (sbyte)((int)ptr8[l] - (ptr8[l - n] >> 1));
								break;
							case 6:
								ptr4[l] = (sbyte)(ptr8[l] - StbImageWrite.stbiw__paeth((int)ptr8[l - n], 0, 0));
								break;
							}
						}
						if (j != 0)
						{
							break;
						}
						for (int l = 0; l < x * n; l++)
						{
							num4 += CRuntime.abs((int)ptr4[l]);
						}
						if (num4 < num2)
						{
							num2 = num4;
							num = k;
						}
					}
				}
				ptr3[i * (x * n + 1)] = (byte)num;
				CRuntime.memmove((void*)(ptr3 + i * (x * n + 1) + 1), (void*)ptr4, (ulong)((long)(x * n)));
			}
			CRuntime.free((void*)ptr4);
			int num5;
			byte* ptr9 = StbImageWrite.stbi_zlib_compress(ptr3, y * (x * n + 1), &num5, 8);
			CRuntime.free((void*)ptr3);
			if (ptr9 == null)
			{
				return null;
			}
			byte* ptr10 = (byte*)CRuntime.malloc((ulong)((long)(45 + num5 + 12)));
			if (ptr10 == null)
			{
				return null;
			}
			*out_len = 45 + num5 + 12;
			byte* ptr11 = ptr10;
			CRuntime.memmove((void*)ptr11, (void*)ptr2, 8UL);
			ptr11 += 8;
			*ptr11 = 0;
			ptr11[1] = 0;
			ptr11[2] = 0;
			ptr11[3] = 13;
			ptr11 += 4;
			*ptr11 = (byte)("IHDR"[0] & 'ÿ');
			ptr11[1] = (byte)("IHDR"[1] & 'ÿ');
			ptr11[2] = (byte)("IHDR"[2] & 'ÿ');
			ptr11[3] = (byte)("IHDR"[3] & 'ÿ');
			ptr11 += 4;
			*ptr11 = (byte)((x >> 24) & 255);
			ptr11[1] = (byte)((x >> 16) & 255);
			ptr11[2] = (byte)((x >> 8) & 255);
			ptr11[3] = (byte)(x & 255);
			ptr11 += 4;
			*ptr11 = (byte)((y >> 24) & 255);
			ptr11[1] = (byte)((y >> 16) & 255);
			ptr11[2] = (byte)((y >> 8) & 255);
			ptr11[3] = (byte)(y & 255);
			ptr11 += 4;
			*(ptr11++) = 8;
			*(ptr11++) = (byte)(ptr[n] & 255);
			*(ptr11++) = 0;
			*(ptr11++) = 0;
			*(ptr11++) = 0;
			StbImageWrite.stbiw__wpcrc(&ptr11, 13);
			*ptr11 = (byte)((num5 >> 24) & 255);
			ptr11[1] = (byte)((num5 >> 16) & 255);
			ptr11[2] = (byte)((num5 >> 8) & 255);
			ptr11[3] = (byte)(num5 & 255);
			ptr11 += 4;
			*ptr11 = (byte)("IDAT"[0] & 'ÿ');
			ptr11[1] = (byte)("IDAT"[1] & 'ÿ');
			ptr11[2] = (byte)("IDAT"[2] & 'ÿ');
			ptr11[3] = (byte)("IDAT"[3] & 'ÿ');
			ptr11 += 4;
			CRuntime.memmove((void*)ptr11, (void*)ptr9, (ulong)((long)num5));
			ptr11 += num5;
			CRuntime.free((void*)ptr9);
			StbImageWrite.stbiw__wpcrc(&ptr11, num5);
			*ptr11 = 0;
			ptr11[1] = 0;
			ptr11[2] = 0;
			ptr11[3] = 0;
			ptr11 += 4;
			*ptr11 = (byte)("IEND"[0] & 'ÿ');
			ptr11[1] = (byte)("IEND"[1] & 'ÿ');
			ptr11[2] = (byte)("IEND"[2] & 'ÿ');
			ptr11[3] = (byte)("IEND"[3] & 'ÿ');
			ptr11 += 4;
			StbImageWrite.stbiw__wpcrc(&ptr11, 0);
			return ptr10;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00008564 File Offset: 0x00006764
		public unsafe static void stbiw__jpg_writeBits(StbImageWrite.stbi__write_context s, int* bitBufP, int* bitCntP, ushort bs0, ushort bs1)
		{
			int num = *bitBufP;
			int i = *bitCntP;
			i += (int)bs1;
			num |= (int)bs0 << 24 - i;
			while (i >= 8)
			{
				byte b = (byte)((num >> 16) & 255);
				StbImageWrite.stbiw__putc(s, b);
				if (b == 255)
				{
					StbImageWrite.stbiw__putc(s, 0);
				}
				num <<= 8;
				i -= 8;
			}
			*bitBufP = num;
			*bitCntP = i;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000085C0 File Offset: 0x000067C0
		public unsafe static void stbiw__jpg_DCT(float* d0p, float* d1p, float* d2p, float* d3p, float* d4p, float* d5p, float* d6p, float* d7p)
		{
			float num = *d0p;
			float num2 = *d1p;
			float num3 = *d2p;
			float num4 = *d3p;
			float num5 = *d4p;
			float num6 = *d5p;
			float num7 = *d6p;
			float num8 = *d7p;
			float num9 = num + num8;
			float num10 = num - num8;
			float num11 = num2 + num7;
			float num12 = num2 - num7;
			float num13 = num3 + num6;
			float num14 = num3 - num6;
			float num15 = num4 + num5;
			float num16 = num4 - num5;
			float num17 = num9 + num15;
			float num18 = num9 - num15;
			float num19 = num11 + num13;
			float num20 = num11 - num13;
			num = num17 + num19;
			num5 = num17 - num19;
			float num21 = (num20 + num18) * 0.70710677f;
			num3 = num18 + num21;
			num7 = num18 - num21;
			num17 = num16 + num14;
			num19 = num14 + num12;
			num20 = num12 + num10;
			float num22 = (num17 - num20) * 0.38268343f;
			float num23 = num17 * 0.5411961f + num22;
			float num24 = num20 * 1.306563f + num22;
			float num25 = num19 * 0.70710677f;
			float num26 = num10 + num25;
			float num27 = num10 - num25;
			*d5p = num27 + num23;
			*d3p = num27 - num23;
			*d1p = num26 + num24;
			*d7p = num26 - num24;
			*d0p = num;
			*d2p = num3;
			*d4p = num5;
			*d6p = num7;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00008708 File Offset: 0x00006908
		public unsafe static void stbiw__jpg_calcBits(int val, ushort* bits)
		{
			int num = ((val < 0) ? (-val) : val);
			val = ((val < 0) ? (val - 1) : val);
			bits[1] = 1;
			while ((num >>= 1) != 0)
			{
				ushort* ptr = bits + 1;
				*ptr += 1;
			}
			*bits = (ushort)(val & ((1 << (int)bits[1]) - 1));
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00008754 File Offset: 0x00006954
		public unsafe static int stbiw__jpg_processDU(StbImageWrite.stbi__write_context s, int* bitBuf, int* bitCnt, float* CDU, float* fdtbl, int DC, ushort[,] HTDC, ushort[,] HTAC)
		{
			ushort* ptr = stackalloc ushort[(UIntPtr)4];
			*ptr = HTAC[0, 0];
			ptr[1] = HTAC[0, 1];
			ushort* ptr2 = stackalloc ushort[(UIntPtr)4];
			*ptr2 = HTAC[240, 0];
			ptr2[1] = HTAC[240, 1];
			int* ptr3 = stackalloc int[(UIntPtr)256];
			for (int i = 0; i < 64; i += 8)
			{
				StbImageWrite.stbiw__jpg_DCT(CDU + i, CDU + (i + 1), CDU + (i + 2), CDU + (i + 3), CDU + (i + 4), CDU + (i + 5), CDU + (i + 6), CDU + (i + 7));
			}
			for (int i = 0; i < 8; i++)
			{
				StbImageWrite.stbiw__jpg_DCT(CDU + i, CDU + (i + 8), CDU + (i + 16), CDU + (i + 24), CDU + (i + 32), CDU + (i + 40), CDU + (i + 48), CDU + (i + 56));
			}
			for (int j = 0; j < 64; j++)
			{
				float num = CDU[j] * fdtbl[j];
				ptr3[StbImageWrite.stbiw__jpg_ZigZag[j]] = (int)((num < 0f) ? (num - 0.5f) : (num + 0.5f));
			}
			int num2 = *ptr3 - DC;
			if (num2 == 0)
			{
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, HTDC[0, 0], HTDC[0, 1]);
			}
			else
			{
				ushort* ptr4 = stackalloc ushort[(UIntPtr)4];
				StbImageWrite.stbiw__jpg_calcBits(num2, ptr4);
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, HTDC[(int)ptr4[1], 0], HTDC[(int)ptr4[1], 1]);
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, *ptr4, ptr4[1]);
			}
			int num3 = 63;
			while (num3 > 0 && ptr3[num3] == 0)
			{
				num3--;
			}
			if (num3 == 0)
			{
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, *ptr, ptr[1]);
				return *ptr3;
			}
			for (int j = 1; j <= num3; j++)
			{
				int num4 = j;
				ushort* ptr5 = stackalloc ushort[(UIntPtr)4];
				while (ptr3[j] == 0 && j <= num3)
				{
					j++;
				}
				int num5 = j - num4;
				if (num5 >= 16)
				{
					int num6 = num5 >> 4;
					for (int k = 1; k <= num6; k++)
					{
						StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, *ptr2, ptr2[1]);
					}
					num5 &= 15;
				}
				StbImageWrite.stbiw__jpg_calcBits(ptr3[j], ptr5);
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, HTAC[(num5 << 4) + (int)ptr5[1], 0], HTAC[(num5 << 4) + (int)ptr5[1], 1]);
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, *ptr5, ptr5[1]);
			}
			if (num3 != 63)
			{
				StbImageWrite.stbiw__jpg_writeBits(s, bitBuf, bitCnt, *ptr, ptr[1]);
			}
			return *ptr3;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00008A14 File Offset: 0x00006C14
		public unsafe static int stbi_write_jpg_core(StbImageWrite.stbi__write_context s, int width, int height, int comp, void* data, int quality)
		{
			float* ptr = stackalloc float[(UIntPtr)256];
			float* ptr2 = stackalloc float[(UIntPtr)256];
			byte* ptr3 = stackalloc byte[(UIntPtr)64];
			byte* ptr4 = stackalloc byte[(UIntPtr)64];
			if (data == null || width == 0 || height == 0 || comp > 4 || comp < 1)
			{
				return 0;
			}
			quality = ((quality != 0) ? quality : 90);
			quality = ((quality < 1) ? 1 : ((quality > 100) ? 100 : quality));
			quality = ((quality < 50) ? (5000 / quality) : (200 - quality * 2));
			for (int i = 0; i < 64; i++)
			{
				int num = (StbImageWrite.YQT[i] * quality + 50) / 100;
				ptr3[StbImageWrite.stbiw__jpg_ZigZag[i]] = (byte)((num < 1) ? 1 : ((num > 255) ? 255 : num));
				int num2 = (StbImageWrite.UVQT[i] * quality + 50) / 100;
				ptr4[StbImageWrite.stbiw__jpg_ZigZag[i]] = (byte)((num2 < 1) ? 1 : ((num2 > 255) ? 255 : num2));
			}
			int j = 0;
			int num3 = 0;
			while (j < 8)
			{
				int k = 0;
				while (k < 8)
				{
					ptr[num3] = 1f / ((float)ptr3[StbImageWrite.stbiw__jpg_ZigZag[num3]] * StbImageWrite.aasf[j] * StbImageWrite.aasf[k]);
					ptr2[num3] = 1f / ((float)ptr4[StbImageWrite.stbiw__jpg_ZigZag[num3]] * StbImageWrite.aasf[j] * StbImageWrite.aasf[k]);
					k++;
					num3++;
				}
				j++;
			}
			byte* ptr5 = stackalloc byte[(UIntPtr)24];
			*ptr5 = byte.MaxValue;
			ptr5[1] = 192;
			ptr5[2] = 0;
			ptr5[3] = 17;
			ptr5[4] = 8;
			ptr5[5] = (byte)(height >> 8);
			ptr5[6] = (byte)(height & 255);
			ptr5[7] = (byte)(width >> 8);
			ptr5[8] = (byte)(width & 255);
			ptr5[9] = 3;
			ptr5[10] = 1;
			ptr5[11] = 17;
			ptr5[12] = 0;
			ptr5[13] = 2;
			ptr5[14] = 17;
			ptr5[15] = 1;
			ptr5[16] = 3;
			ptr5[17] = 17;
			ptr5[18] = 1;
			ptr5[19] = byte.MaxValue;
			ptr5[20] = 196;
			ptr5[21] = 1;
			ptr5[22] = 162;
			ptr5[23] = 0;
			byte[] array;
			byte* data2;
			if ((array = StbImageWrite.head0) == null || array.Length == 0)
			{
				data2 = null;
			}
			else
			{
				data2 = &array[0];
			}
			s.func(s.context, (void*)data2, StbImageWrite.head0.Length);
			array = null;
			s.func(s.context, (void*)ptr3, 64);
			StbImageWrite.stbiw__putc(s, 1);
			s.func(s.context, (void*)ptr4, 64);
			s.func(s.context, (void*)ptr5, 24);
			fixed (byte* ptr6 = &StbImageWrite.std_dc_luminance_nrcodes[1])
			{
				byte* data3 = ptr6;
				s.func(s.context, (void*)data3, StbImageWrite.std_dc_chrominance_nrcodes.Length - 1);
			}
			byte* data4;
			if ((array = StbImageWrite.std_dc_luminance_values) == null || array.Length == 0)
			{
				data4 = null;
			}
			else
			{
				data4 = &array[0];
			}
			s.func(s.context, (void*)data4, StbImageWrite.std_dc_chrominance_values.Length);
			array = null;
			StbImageWrite.stbiw__putc(s, 16);
			fixed (byte* ptr6 = &StbImageWrite.std_ac_luminance_nrcodes[1])
			{
				byte* data5 = ptr6;
				s.func(s.context, (void*)data5, StbImageWrite.std_ac_luminance_nrcodes.Length - 1);
			}
			byte* data6;
			if ((array = StbImageWrite.std_ac_luminance_values) == null || array.Length == 0)
			{
				data6 = null;
			}
			else
			{
				data6 = &array[0];
			}
			s.func(s.context, (void*)data6, StbImageWrite.std_ac_luminance_values.Length);
			array = null;
			StbImageWrite.stbiw__putc(s, 1);
			fixed (byte* ptr6 = &StbImageWrite.std_dc_chrominance_nrcodes[1])
			{
				byte* data7 = ptr6;
				s.func(s.context, (void*)data7, StbImageWrite.std_dc_chrominance_nrcodes.Length - 1);
			}
			byte* data8;
			if ((array = StbImageWrite.std_dc_chrominance_values) == null || array.Length == 0)
			{
				data8 = null;
			}
			else
			{
				data8 = &array[0];
			}
			s.func(s.context, (void*)data8, StbImageWrite.std_dc_chrominance_values.Length);
			array = null;
			StbImageWrite.stbiw__putc(s, 17);
			fixed (byte* ptr6 = &StbImageWrite.std_ac_chrominance_nrcodes[1])
			{
				byte* data9 = ptr6;
				s.func(s.context, (void*)data9, StbImageWrite.std_ac_chrominance_nrcodes.Length - 1);
			}
			byte* data10;
			if ((array = StbImageWrite.std_ac_chrominance_values) == null || array.Length == 0)
			{
				data10 = null;
			}
			else
			{
				data10 = &array[0];
			}
			s.func(s.context, (void*)data10, StbImageWrite.std_ac_chrominance_values.Length);
			array = null;
			byte* data11;
			if ((array = StbImageWrite.head2) == null || array.Length == 0)
			{
				data11 = null;
			}
			else
			{
				data11 = &array[0];
			}
			s.func(s.context, (void*)data11, StbImageWrite.head2.Length);
			array = null;
			ushort* ptr7 = stackalloc ushort[(UIntPtr)4];
			*ptr7 = 127;
			ptr7[1] = 7;
			int dc = 0;
			int dc2 = 0;
			int dc3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = ((comp > 2) ? 1 : 0);
			int num7 = ((comp > 2) ? 2 : 0);
			float* ptr8 = stackalloc float[(UIntPtr)256];
			float* ptr9 = stackalloc float[(UIntPtr)256];
			float* ptr10 = stackalloc float[(UIntPtr)256];
			for (int l = 0; l < height; l += 8)
			{
				for (int m = 0; m < width; m += 8)
				{
					j = l;
					int num8 = 0;
					while (j < l + 8)
					{
						int k = m;
						while (k < m + 8)
						{
							int num9 = j * width * comp + k * comp;
							if (j >= height)
							{
								num9 -= width * comp * (j + 1 - height);
							}
							if (k >= width)
							{
								num9 -= comp * (k + 1 - width);
							}
							float num10 = (float)((byte*)data)[num9];
							float num11 = (float)((byte*)data)[num9 + num6];
							float num12 = (float)((byte*)data)[num9 + num7];
							ptr8[num8] = 0.299f * num10 + 0.587f * num11 + 0.114f * num12 - 128f;
							ptr9[num8] = -0.16874f * num10 - 0.33126f * num11 + 0.5f * num12;
							ptr10[num8] = 0.5f * num10 - 0.41869f * num11 - 0.08131f * num12;
							k++;
							num8++;
						}
						j++;
					}
					dc = StbImageWrite.stbiw__jpg_processDU(s, &num4, &num5, ptr8, ptr, dc, StbImageWrite.YDC_HT, StbImageWrite.YAC_HT);
					dc2 = StbImageWrite.stbiw__jpg_processDU(s, &num4, &num5, ptr9, ptr2, dc2, StbImageWrite.UVDC_HT, StbImageWrite.UVAC_HT);
					dc3 = StbImageWrite.stbiw__jpg_processDU(s, &num4, &num5, ptr10, ptr2, dc3, StbImageWrite.UVDC_HT, StbImageWrite.UVAC_HT);
				}
			}
			StbImageWrite.stbiw__jpg_writeBits(s, &num4, &num5, *ptr7, ptr7[1]);
			StbImageWrite.stbiw__putc(s, byte.MaxValue);
			StbImageWrite.stbiw__putc(s, 217);
			return 1;
		}

		// Token: 0x04000034 RID: 52
		public static int stbi_write_tga_with_rle = 1;

		// Token: 0x04000035 RID: 53
		public static ushort[] lengthc = new ushort[]
		{
			3, 4, 5, 6, 7, 8, 9, 10, 11, 13,
			15, 17, 19, 23, 27, 31, 35, 43, 51, 59,
			67, 83, 99, 115, 131, 163, 195, 227, 258, 259
		};

		// Token: 0x04000036 RID: 54
		public static byte[] lengtheb = new byte[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 1, 1,
			1, 1, 2, 2, 2, 2, 3, 3, 3, 3,
			4, 4, 4, 4, 5, 5, 5, 5, 0
		};

		// Token: 0x04000037 RID: 55
		public static ushort[] distc = new ushort[]
		{
			1, 2, 3, 4, 5, 7, 9, 13, 17, 25,
			33, 49, 65, 97, 129, 193, 257, 385, 513, 769,
			1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577,
			32768
		};

		// Token: 0x04000038 RID: 56
		public static byte[] disteb = new byte[]
		{
			0, 0, 0, 0, 1, 1, 2, 2, 3, 3,
			4, 4, 5, 5, 6, 6, 7, 7, 8, 8,
			9, 9, 10, 10, 11, 11, 12, 12, 13, 13
		};

		// Token: 0x04000039 RID: 57
		public static uint[] crc_table = new uint[]
		{
			0U, 1996959894U, 3993919788U, 2567524794U, 124634137U, 1886057615U, 3915621685U, 2657392035U, 249268274U, 2044508324U,
			3772115230U, 2547177864U, 162941995U, 2125561021U, 3887607047U, 2428444049U, 498536548U, 1789927666U, 4089016648U, 2227061214U,
			450548861U, 1843258603U, 4107580753U, 2211677639U, 325883990U, 1684777152U, 4251122042U, 2321926636U, 335633487U, 1661365465U,
			4195302755U, 2366115317U, 997073096U, 1281953886U, 3579855332U, 2724688242U, 1006888145U, 1258607687U, 3524101629U, 2768942443U,
			901097722U, 1119000684U, 3686517206U, 2898065728U, 853044451U, 1172266101U, 3705015759U, 2882616665U, 651767980U, 1373503546U,
			3369554304U, 3218104598U, 565507253U, 1454621731U, 3485111705U, 3099436303U, 671266974U, 1594198024U, 3322730930U, 2970347812U,
			795835527U, 1483230225U, 3244367275U, 3060149565U, 1994146192U, 31158534U, 2563907772U, 4023717930U, 1907459465U, 112637215U,
			2680153253U, 3904427059U, 2013776290U, 251722036U, 2517215374U, 3775830040U, 2137656763U, 141376813U, 2439277719U, 3865271297U,
			1802195444U, 476864866U, 2238001368U, 4066508878U, 1812370925U, 453092731U, 2181625025U, 4111451223U, 1706088902U, 314042704U,
			2344532202U, 4240017532U, 1658658271U, 366619977U, 2362670323U, 4224994405U, 1303535960U, 984961486U, 2747007092U, 3569037538U,
			1256170817U, 1037604311U, 2765210733U, 3554079995U, 1131014506U, 879679996U, 2909243462U, 3663771856U, 1141124467U, 855842277U,
			2852801631U, 3708648649U, 1342533948U, 654459306U, 3188396048U, 3373015174U, 1466479909U, 544179635U, 3110523913U, 3462522015U,
			1591671054U, 702138776U, 2966460450U, 3352799412U, 1504918807U, 783551873U, 3082640443U, 3233442989U, 3988292384U, 2596254646U,
			62317068U, 1957810842U, 3939845945U, 2647816111U, 81470997U, 1943803523U, 3814918930U, 2489596804U, 225274430U, 2053790376U,
			3826175755U, 2466906013U, 167816743U, 2097651377U, 4027552580U, 2265490386U, 503444072U, 1762050814U, 4150417245U, 2154129355U,
			426522225U, 1852507879U, 4275313526U, 2312317920U, 282753626U, 1742555852U, 4189708143U, 2394877945U, 397917763U, 1622183637U,
			3604390888U, 2714866558U, 953729732U, 1340076626U, 3518719985U, 2797360999U, 1068828381U, 1219638859U, 3624741850U, 2936675148U,
			906185462U, 1090812512U, 3747672003U, 2825379669U, 829329135U, 1181335161U, 3412177804U, 3160834842U, 628085408U, 1382605366U,
			3423369109U, 3138078467U, 570562233U, 1426400815U, 3317316542U, 2998733608U, 733239954U, 1555261956U, 3268935591U, 3050360625U,
			752459403U, 1541320221U, 2607071920U, 3965973030U, 1969922972U, 40735498U, 2617837225U, 3943577151U, 1913087877U, 83908371U,
			2512341634U, 3803740692U, 2075208622U, 213261112U, 2463272603U, 3855990285U, 2094854071U, 198958881U, 2262029012U, 4057260610U,
			1759359992U, 534414190U, 2176718541U, 4139329115U, 1873836001U, 414664567U, 2282248934U, 4279200368U, 1711684554U, 285281116U,
			2405801727U, 4167216745U, 1634467795U, 376229701U, 2685067896U, 3608007406U, 1308918612U, 956543938U, 2808555105U, 3495958263U,
			1231636301U, 1047427035U, 2932959818U, 3654703836U, 1088359270U, 936918000U, 2847714899U, 3736837829U, 1202900863U, 817233897U,
			3183342108U, 3401237130U, 1404277552U, 615818150U, 3134207493U, 3453421203U, 1423857449U, 601450431U, 3009837614U, 3294710456U,
			1567103746U, 711928724U, 3020668471U, 3272380065U, 1510334235U, 755167117U
		};

		// Token: 0x0400003A RID: 58
		public static byte[] stbiw__jpg_ZigZag = new byte[]
		{
			0, 1, 5, 6, 14, 15, 27, 28, 2, 4,
			7, 13, 16, 26, 29, 42, 3, 8, 12, 17,
			25, 30, 41, 43, 9, 11, 18, 24, 31, 40,
			44, 53, 10, 19, 23, 32, 39, 45, 52, 54,
			20, 22, 33, 38, 46, 51, 55, 60, 21, 34,
			37, 47, 50, 56, 59, 61, 35, 36, 48, 49,
			57, 58, 62, 63
		};

		// Token: 0x0400003B RID: 59
		public static byte[] std_dc_luminance_nrcodes = new byte[]
		{
			0, 0, 1, 5, 1, 1, 1, 1, 1, 1,
			0, 0, 0, 0, 0, 0, 0
		};

		// Token: 0x0400003C RID: 60
		public static byte[] std_dc_luminance_values = new byte[]
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
			10, 11
		};

		// Token: 0x0400003D RID: 61
		public static byte[] std_ac_luminance_nrcodes = new byte[]
		{
			0, 0, 2, 1, 3, 3, 2, 4, 3, 5,
			5, 4, 4, 0, 0, 1, 125
		};

		// Token: 0x0400003E RID: 62
		public static byte[] std_ac_luminance_values = new byte[]
		{
			1, 2, 3, 0, 4, 17, 5, 18, 33, 49,
			65, 6, 19, 81, 97, 7, 34, 113, 20, 50,
			129, 145, 161, 8, 35, 66, 177, 193, 21, 82,
			209, 240, 36, 51, 98, 114, 130, 9, 10, 22,
			23, 24, 25, 26, 37, 38, 39, 40, 41, 42,
			52, 53, 54, 55, 56, 57, 58, 67, 68, 69,
			70, 71, 72, 73, 74, 83, 84, 85, 86, 87,
			88, 89, 90, 99, 100, 101, 102, 103, 104, 105,
			106, 115, 116, 117, 118, 119, 120, 121, 122, 131,
			132, 133, 134, 135, 136, 137, 138, 146, 147, 148,
			149, 150, 151, 152, 153, 154, 162, 163, 164, 165,
			166, 167, 168, 169, 170, 178, 179, 180, 181, 182,
			183, 184, 185, 186, 194, 195, 196, 197, 198, 199,
			200, 201, 202, 210, 211, 212, 213, 214, 215, 216,
			217, 218, 225, 226, 227, 228, 229, 230, 231, 232,
			233, 234, 241, 242, 243, 244, 245, 246, 247, 248,
			249, 250
		};

		// Token: 0x0400003F RID: 63
		public static byte[] std_dc_chrominance_nrcodes = new byte[]
		{
			0, 0, 3, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 0, 0, 0, 0, 0
		};

		// Token: 0x04000040 RID: 64
		public static byte[] std_dc_chrominance_values = new byte[]
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
			10, 11
		};

		// Token: 0x04000041 RID: 65
		public static byte[] std_ac_chrominance_nrcodes = new byte[]
		{
			0, 0, 2, 1, 2, 4, 4, 3, 4, 7,
			5, 4, 4, 0, 1, 2, 119
		};

		// Token: 0x04000042 RID: 66
		public static byte[] std_ac_chrominance_values = new byte[]
		{
			0, 1, 2, 3, 17, 4, 5, 33, 49, 6,
			18, 65, 81, 7, 97, 113, 19, 34, 50, 129,
			8, 20, 66, 145, 161, 177, 193, 9, 35, 51,
			82, 240, 21, 98, 114, 209, 10, 22, 36, 52,
			225, 37, 241, 23, 24, 25, 26, 38, 39, 40,
			41, 42, 53, 54, 55, 56, 57, 58, 67, 68,
			69, 70, 71, 72, 73, 74, 83, 84, 85, 86,
			87, 88, 89, 90, 99, 100, 101, 102, 103, 104,
			105, 106, 115, 116, 117, 118, 119, 120, 121, 122,
			130, 131, 132, 133, 134, 135, 136, 137, 138, 146,
			147, 148, 149, 150, 151, 152, 153, 154, 162, 163,
			164, 165, 166, 167, 168, 169, 170, 178, 179, 180,
			181, 182, 183, 184, 185, 186, 194, 195, 196, 197,
			198, 199, 200, 201, 202, 210, 211, 212, 213, 214,
			215, 216, 217, 218, 226, 227, 228, 229, 230, 231,
			232, 233, 234, 242, 243, 244, 245, 246, 247, 248,
			249, 250
		};

		// Token: 0x04000043 RID: 67
		public static ushort[,] YDC_HT = new ushort[,]
		{
			{ 0, 2 },
			{ 2, 3 },
			{ 3, 3 },
			{ 4, 3 },
			{ 5, 3 },
			{ 6, 3 },
			{ 14, 4 },
			{ 30, 5 },
			{ 62, 6 },
			{ 126, 7 },
			{ 254, 8 },
			{ 510, 9 }
		};

		// Token: 0x04000044 RID: 68
		public static ushort[,] UVDC_HT = new ushort[,]
		{
			{ 0, 2 },
			{ 1, 2 },
			{ 2, 2 },
			{ 6, 3 },
			{ 14, 4 },
			{ 30, 5 },
			{ 62, 6 },
			{ 126, 7 },
			{ 254, 8 },
			{ 510, 9 },
			{ 1022, 10 },
			{ 2046, 11 }
		};

		// Token: 0x04000045 RID: 69
		public static ushort[,] YAC_HT = new ushort[,]
		{
			{ 10, 4 },
			{ 0, 2 },
			{ 1, 2 },
			{ 4, 3 },
			{ 11, 4 },
			{ 26, 5 },
			{ 120, 7 },
			{ 248, 8 },
			{ 1014, 10 },
			{ 65410, 16 },
			{ 65411, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 12, 4 },
			{ 27, 5 },
			{ 121, 7 },
			{ 502, 9 },
			{ 2038, 11 },
			{ 65412, 16 },
			{ 65413, 16 },
			{ 65414, 16 },
			{ 65415, 16 },
			{ 65416, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 28, 5 },
			{ 249, 8 },
			{ 1015, 10 },
			{ 4084, 12 },
			{ 65417, 16 },
			{ 65418, 16 },
			{ 65419, 16 },
			{ 65420, 16 },
			{ 65421, 16 },
			{ 65422, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 58, 6 },
			{ 503, 9 },
			{ 4085, 12 },
			{ 65423, 16 },
			{ 65424, 16 },
			{ 65425, 16 },
			{ 65426, 16 },
			{ 65427, 16 },
			{ 65428, 16 },
			{ 65429, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 59, 6 },
			{ 1016, 10 },
			{ 65430, 16 },
			{ 65431, 16 },
			{ 65432, 16 },
			{ 65433, 16 },
			{ 65434, 16 },
			{ 65435, 16 },
			{ 65436, 16 },
			{ 65437, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 122, 7 },
			{ 2039, 11 },
			{ 65438, 16 },
			{ 65439, 16 },
			{ 65440, 16 },
			{ 65441, 16 },
			{ 65442, 16 },
			{ 65443, 16 },
			{ 65444, 16 },
			{ 65445, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 123, 7 },
			{ 4086, 12 },
			{ 65446, 16 },
			{ 65447, 16 },
			{ 65448, 16 },
			{ 65449, 16 },
			{ 65450, 16 },
			{ 65451, 16 },
			{ 65452, 16 },
			{ 65453, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 250, 8 },
			{ 4087, 12 },
			{ 65454, 16 },
			{ 65455, 16 },
			{ 65456, 16 },
			{ 65457, 16 },
			{ 65458, 16 },
			{ 65459, 16 },
			{ 65460, 16 },
			{ 65461, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 504, 9 },
			{ 32704, 15 },
			{ 65462, 16 },
			{ 65463, 16 },
			{ 65464, 16 },
			{ 65465, 16 },
			{ 65466, 16 },
			{ 65467, 16 },
			{ 65468, 16 },
			{ 65469, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 505, 9 },
			{ 65470, 16 },
			{ 65471, 16 },
			{ 65472, 16 },
			{ 65473, 16 },
			{ 65474, 16 },
			{ 65475, 16 },
			{ 65476, 16 },
			{ 65477, 16 },
			{ 65478, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 506, 9 },
			{ 65479, 16 },
			{ 65480, 16 },
			{ 65481, 16 },
			{ 65482, 16 },
			{ 65483, 16 },
			{ 65484, 16 },
			{ 65485, 16 },
			{ 65486, 16 },
			{ 65487, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 1017, 10 },
			{ 65488, 16 },
			{ 65489, 16 },
			{ 65490, 16 },
			{ 65491, 16 },
			{ 65492, 16 },
			{ 65493, 16 },
			{ 65494, 16 },
			{ 65495, 16 },
			{ 65496, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 1018, 10 },
			{ 65497, 16 },
			{ 65498, 16 },
			{ 65499, 16 },
			{ 65500, 16 },
			{ 65501, 16 },
			{ 65502, 16 },
			{ 65503, 16 },
			{ 65504, 16 },
			{ 65505, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 2040, 11 },
			{ 65506, 16 },
			{ 65507, 16 },
			{ 65508, 16 },
			{ 65509, 16 },
			{ 65510, 16 },
			{ 65511, 16 },
			{ 65512, 16 },
			{ 65513, 16 },
			{ 65514, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 65515, 16 },
			{ 65516, 16 },
			{ 65517, 16 },
			{ 65518, 16 },
			{ 65519, 16 },
			{ 65520, 16 },
			{ 65521, 16 },
			{ 65522, 16 },
			{ 65523, 16 },
			{ 65524, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 2041, 11 },
			{ 65525, 16 },
			{ 65526, 16 },
			{ 65527, 16 },
			{ 65528, 16 },
			{ 65529, 16 },
			{ 65530, 16 },
			{ 65531, 16 },
			{ 65532, 16 },
			{ 65533, 16 },
			{ 65534, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 }
		};

		// Token: 0x04000046 RID: 70
		public static ushort[,] UVAC_HT = new ushort[,]
		{
			{ 0, 2 },
			{ 1, 2 },
			{ 4, 3 },
			{ 10, 4 },
			{ 24, 5 },
			{ 25, 5 },
			{ 56, 6 },
			{ 120, 7 },
			{ 500, 9 },
			{ 1014, 10 },
			{ 4084, 12 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 11, 4 },
			{ 57, 6 },
			{ 246, 8 },
			{ 501, 9 },
			{ 2038, 11 },
			{ 4085, 12 },
			{ 65416, 16 },
			{ 65417, 16 },
			{ 65418, 16 },
			{ 65419, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 26, 5 },
			{ 247, 8 },
			{ 1015, 10 },
			{ 4086, 12 },
			{ 32706, 15 },
			{ 65420, 16 },
			{ 65421, 16 },
			{ 65422, 16 },
			{ 65423, 16 },
			{ 65424, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 27, 5 },
			{ 248, 8 },
			{ 1016, 10 },
			{ 4087, 12 },
			{ 65425, 16 },
			{ 65426, 16 },
			{ 65427, 16 },
			{ 65428, 16 },
			{ 65429, 16 },
			{ 65430, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 58, 6 },
			{ 502, 9 },
			{ 65431, 16 },
			{ 65432, 16 },
			{ 65433, 16 },
			{ 65434, 16 },
			{ 65435, 16 },
			{ 65436, 16 },
			{ 65437, 16 },
			{ 65438, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 59, 6 },
			{ 1017, 10 },
			{ 65439, 16 },
			{ 65440, 16 },
			{ 65441, 16 },
			{ 65442, 16 },
			{ 65443, 16 },
			{ 65444, 16 },
			{ 65445, 16 },
			{ 65446, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 121, 7 },
			{ 2039, 11 },
			{ 65447, 16 },
			{ 65448, 16 },
			{ 65449, 16 },
			{ 65450, 16 },
			{ 65451, 16 },
			{ 65452, 16 },
			{ 65453, 16 },
			{ 65454, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 122, 7 },
			{ 2040, 11 },
			{ 65455, 16 },
			{ 65456, 16 },
			{ 65457, 16 },
			{ 65458, 16 },
			{ 65459, 16 },
			{ 65460, 16 },
			{ 65461, 16 },
			{ 65462, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 249, 8 },
			{ 65463, 16 },
			{ 65464, 16 },
			{ 65465, 16 },
			{ 65466, 16 },
			{ 65467, 16 },
			{ 65468, 16 },
			{ 65469, 16 },
			{ 65470, 16 },
			{ 65471, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 503, 9 },
			{ 65472, 16 },
			{ 65473, 16 },
			{ 65474, 16 },
			{ 65475, 16 },
			{ 65476, 16 },
			{ 65477, 16 },
			{ 65478, 16 },
			{ 65479, 16 },
			{ 65480, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 504, 9 },
			{ 65481, 16 },
			{ 65482, 16 },
			{ 65483, 16 },
			{ 65484, 16 },
			{ 65485, 16 },
			{ 65486, 16 },
			{ 65487, 16 },
			{ 65488, 16 },
			{ 65489, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 505, 9 },
			{ 65490, 16 },
			{ 65491, 16 },
			{ 65492, 16 },
			{ 65493, 16 },
			{ 65494, 16 },
			{ 65495, 16 },
			{ 65496, 16 },
			{ 65497, 16 },
			{ 65498, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 506, 9 },
			{ 65499, 16 },
			{ 65500, 16 },
			{ 65501, 16 },
			{ 65502, 16 },
			{ 65503, 16 },
			{ 65504, 16 },
			{ 65505, 16 },
			{ 65506, 16 },
			{ 65507, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 2041, 11 },
			{ 65508, 16 },
			{ 65509, 16 },
			{ 65510, 16 },
			{ 65511, 16 },
			{ 65512, 16 },
			{ 65513, 16 },
			{ 65514, 16 },
			{ 65515, 16 },
			{ 65516, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 16352, 14 },
			{ 65517, 16 },
			{ 65518, 16 },
			{ 65519, 16 },
			{ 65520, 16 },
			{ 65521, 16 },
			{ 65522, 16 },
			{ 65523, 16 },
			{ 65524, 16 },
			{ 65525, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 1018, 10 },
			{ 32707, 15 },
			{ 65526, 16 },
			{ 65527, 16 },
			{ 65528, 16 },
			{ 65529, 16 },
			{ 65530, 16 },
			{ 65531, 16 },
			{ 65532, 16 },
			{ 65533, 16 },
			{ 65534, 16 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 },
			{ 0, 0 }
		};

		// Token: 0x04000047 RID: 71
		public static int[] YQT = new int[]
		{
			16, 11, 10, 16, 24, 40, 51, 61, 12, 12,
			14, 19, 26, 58, 60, 55, 14, 13, 16, 24,
			40, 57, 69, 56, 14, 17, 22, 29, 51, 87,
			80, 62, 18, 22, 37, 56, 68, 109, 103, 77,
			24, 35, 55, 64, 81, 104, 113, 92, 49, 64,
			78, 87, 103, 121, 120, 101, 72, 92, 95, 98,
			112, 100, 103, 99
		};

		// Token: 0x04000048 RID: 72
		public static int[] UVQT = new int[]
		{
			17, 18, 24, 47, 99, 99, 99, 99, 18, 21,
			26, 66, 99, 99, 99, 99, 24, 26, 56, 99,
			99, 99, 99, 99, 47, 66, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99
		};

		// Token: 0x04000049 RID: 73
		public static float[] aasf = new float[] { 2.828427f, 3.9231412f, 3.6955183f, 3.3258781f, 2.828427f, 2.222281f, 1.5307337f, 0.7803613f };

		// Token: 0x0400004A RID: 74
		public static byte[] head0 = new byte[]
		{
			byte.MaxValue, 216, byte.MaxValue, 224, 0, 16, 74, 70, 73, 70,
			0, 1, 1, 0, 0, 1, 0, 1, 0, 0,
			byte.MaxValue, 219, 0, 132, 0
		};

		// Token: 0x0400004B RID: 75
		public static byte[] head2 = new byte[]
		{
			byte.MaxValue, 218, 0, 12, 3, 1, 0, 2, 17, 3,
			17, 0, 63, 0
		};

		// Token: 0x02000016 RID: 22
		// (Invoke) Token: 0x06000248 RID: 584
		public unsafe delegate int WriteCallback(void* context, void* data, int size);

		// Token: 0x02000017 RID: 23
		public class stbi__write_context
		{
			// Token: 0x04000133 RID: 307
			public StbImageWrite.WriteCallback func;

			// Token: 0x04000134 RID: 308
			public unsafe void* context;
		}
	}
}
