using System;

namespace StbSharp
{
	// Token: 0x02000006 RID: 6
	public static class StbDxt
	{
		// Token: 0x0600001D RID: 29 RVA: 0x000026B4 File Offset: 0x000008B4
		public unsafe static void stb__DitherBlock(byte* dest, byte* block)
		{
			int* ptr = stackalloc int[(UIntPtr)32];
			int* ptr2 = ptr;
			int* ptr3 = ptr + 4;
			for (int i = 0; i < 3; i++)
			{
				byte* ptr4 = block + i;
				byte* ptr5 = dest + i;
				byte[] array;
				byte* ptr6;
				if ((array = ((i == 1) ? StbDxt.stb__QuantGTab : StbDxt.stb__QuantRBTab)) == null || array.Length == 0)
				{
					ptr6 = null;
				}
				else
				{
					ptr6 = &array[0];
				}
				CRuntime.memset((void*)ptr, 0, 32UL);
				for (int j = 0; j < 4; j++)
				{
					*ptr5 = ptr6[(int)(*ptr4) + (3 * ptr3[1] + 5 * *ptr3 >> 4)];
					*ptr2 = (int)(*ptr4 - *ptr5);
					ptr5[4] = ptr6[(int)ptr4[4] + (7 * *ptr2 + 3 * ptr3[2] + 5 * ptr3[1] + *ptr3 >> 4)];
					ptr2[1] = (int)(ptr4[4] - ptr5[4]);
					ptr5[8] = ptr6[(int)ptr4[8] + (7 * ptr2[1] + 3 * ptr3[3] + 5 * ptr3[2] + ptr3[1] >> 4)];
					ptr2[2] = (int)(ptr4[8] - ptr5[8]);
					ptr5[12] = ptr6[(int)ptr4[12] + (7 * ptr2[2] + 5 * ptr3[3] + ptr3[2] >> 4)];
					ptr2[3] = (int)(ptr4[12] - ptr5[12]);
					ptr4 += 16;
					ptr5 += 16;
					int* ptr7 = ptr2;
					ptr2 = ptr3;
					ptr3 = ptr7;
				}
				array = null;
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000282C File Offset: 0x00000A2C
		public unsafe static byte[] stb_compress_dxt(Image image, bool hasAlpha = true, int mode = 10)
		{
			if (image.Comp != 4)
			{
				throw new Exception("This method supports only rgba images");
			}
			int num = (hasAlpha ? 16 : 8);
			byte[] array = new byte[(image.Width + 3) * (image.Height + 3) / 16 * num];
			byte[] array2;
			byte* ptr;
			if ((array2 = image.Data) == null || array2.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array2[0];
			}
			byte[] array3;
			byte* ptr2;
			if ((array3 = array) == null || array3.Length == 0)
			{
				ptr2 = null;
			}
			else
			{
				ptr2 = &array3[0];
			}
			byte* ptr3 = ptr2;
			byte* ptr4 = stackalloc byte[(UIntPtr)64];
			for (int i = 0; i < image.Width; i += 4)
			{
				int num2 = 4;
				for (int j = 0; j < image.Height; j += 4)
				{
					if (i + 3 >= image.Width)
					{
						num2 = image.Width - i;
					}
					int k = 0;
					while (k < 4 && i + k < image.Height)
					{
						CRuntime.memcpy((void*)(ptr4 + k * 16), (void*)(ptr + image.Width * 4 * (i + k) + j * 4), (long)(num2 * 4));
						k++;
					}
					int l;
					if (num2 < 4)
					{
						switch (num2)
						{
						case 0:
							throw new Exception("Unknown error");
						case 1:
							for (l = 0; l < k; l++)
							{
								CRuntime.memcpy((void*)(ptr4 + l * 16 + 4), (void*)(ptr4 + l * 16), 4L);
								CRuntime.memcpy((void*)(ptr4 + l * 16 + 8), (void*)(ptr4 + l * 16), 8L);
							}
							break;
						case 2:
							for (l = 0; l < k; l++)
							{
								CRuntime.memcpy((void*)(ptr4 + l * 16 + 8), (void*)(ptr4 + l * 16), 8L);
							}
							break;
						case 3:
							for (l = 0; l < k; l++)
							{
								CRuntime.memcpy((void*)(ptr4 + l * 16 + 12), (void*)(ptr4 + l * 16 + 4), 4L);
							}
							break;
						}
					}
					l = 0;
					while (k < 4)
					{
						CRuntime.memcpy((void*)(ptr4 + k * 16), (void*)(ptr4 + l * 16), 16L);
						k++;
						l++;
					}
					StbDxt.stb_compress_dxt_block(ptr3, ptr4, hasAlpha ? 1 : 0, mode);
					ptr3 += (hasAlpha ? 16 : 8);
				}
			}
			array3 = null;
			array2 = null;
			return array;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002A6A File Offset: 0x00000C6A
		public static int stb__Mul8Bit(int a, int b)
		{
			int num = a * b + 128;
			return num + (num >> 8) >> 8;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002A7C File Offset: 0x00000C7C
		public unsafe static void stb__From16Bit(byte* _out_, ushort v)
		{
			int num = (v & 63488) >> 11;
			int num2 = (v & 2016) >> 5;
			int num3 = (int)(v & 31);
			*_out_ = StbDxt.stb__Expand5[num];
			_out_[1] = StbDxt.stb__Expand6[num2];
			_out_[2] = StbDxt.stb__Expand5[num3];
			_out_[3] = 0;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002AC7 File Offset: 0x00000CC7
		public static ushort stb__As16Bit(int r, int g, int b)
		{
			return (ushort)((StbDxt.stb__Mul8Bit(r, 31) << 11) + (StbDxt.stb__Mul8Bit(g, 63) << 5) + StbDxt.stb__Mul8Bit(b, 31));
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002AE9 File Offset: 0x00000CE9
		public static int stb__Lerp13(int a, int b)
		{
			return (2 * a + b) / 3;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002AF2 File Offset: 0x00000CF2
		public unsafe static void stb__Lerp13RGB(byte* _out_, byte* p1, byte* p2)
		{
			*_out_ = (byte)StbDxt.stb__Lerp13((int)(*p1), (int)(*p2));
			_out_[1] = (byte)StbDxt.stb__Lerp13((int)p1[1], (int)p2[1]);
			_out_[2] = (byte)StbDxt.stb__Lerp13((int)p1[2], (int)p2[2]);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002B24 File Offset: 0x00000D24
		public static void stb__PrepareOptTable(byte[] Table, byte[] expand, int size)
		{
			for (int i = 0; i < 256; i++)
			{
				int num = 256;
				for (int j = 0; j < size; j++)
				{
					for (int k = 0; k < size; k++)
					{
						int num2 = (int)expand[j];
						int num3 = (int)expand[k];
						int num4 = CRuntime.abs(StbDxt.stb__Lerp13(num3, num2) - i);
						num4 += CRuntime.abs(num3 - num2) * 3 / 100;
						if (num4 < num)
						{
							Table[i * 2] = (byte)k;
							Table[i * 2 + 1] = (byte)j;
							num = num4;
						}
					}
				}
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002BA7 File Offset: 0x00000DA7
		public unsafe static void stb__EvalColors(byte* color, ushort c0, ushort c1)
		{
			StbDxt.stb__From16Bit(color, c0);
			StbDxt.stb__From16Bit(color + 4, c1);
			StbDxt.stb__Lerp13RGB(color + 8, color, color + 4);
			StbDxt.stb__Lerp13RGB(color + 12, color + 4, color);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002BD4 File Offset: 0x00000DD4
		public unsafe static uint stb__MatchColorsBlock(byte* block, byte* color, int dither)
		{
			uint num = 0U;
			int num2 = (int)(*color - color[4]);
			int num3 = (int)(color[1] - color[5]);
			int num4 = (int)(color[2] - color[6]);
			int* ptr = stackalloc int[(UIntPtr)64];
			int* ptr2 = stackalloc int[(UIntPtr)16];
			for (int i = 0; i < 16; i++)
			{
				ptr[i] = (int)block[i * 4] * num2 + (int)block[i * 4 + 1] * num3 + (int)block[i * 4 + 2] * num4;
			}
			for (int i = 0; i < 4; i++)
			{
				ptr2[i] = (int)color[i * 4] * num2 + (int)color[i * 4 + 1] * num3 + (int)color[i * 4 + 2] * num4;
			}
			int num5 = ptr2[1] + ptr2[3] >> 1;
			int num6 = ptr2[3] + ptr2[2] >> 1;
			int num7 = ptr2[2] + *ptr2 >> 1;
			if (dither == 0)
			{
				for (int i = 15; i >= 0; i--)
				{
					int num8 = ptr[i];
					num <<= 2;
					if (num8 < num6)
					{
						num |= ((num8 < num5) ? 1U : 3U);
					}
					else
					{
						num |= ((num8 < num7) ? 2U : 0U);
					}
				}
			}
			else
			{
				int* ptr3 = stackalloc int[(UIntPtr)32];
				int* ptr4 = ptr3;
				int* ptr5 = ptr3 + 4;
				int* ptr6 = ptr;
				num5 <<= 4;
				num6 <<= 4;
				num7 <<= 4;
				for (int i = 0; i < 8; i++)
				{
					ptr3[i] = 0;
				}
				for (int j = 0; j < 4; j++)
				{
					int num9 = (*ptr6 << 4) + (3 * ptr5[1] + 5 * *ptr5);
					int num10;
					if (num9 < num6)
					{
						num10 = ((num9 < num5) ? 1 : 3);
					}
					else
					{
						num10 = ((num9 < num7) ? 2 : 0);
					}
					*ptr4 = *ptr6 - ptr2[num10];
					int num11 = num10;
					num9 = (ptr6[1] << 4) + (7 * *ptr4 + 3 * ptr5[2] + 5 * ptr5[1] + *ptr5);
					if (num9 < num6)
					{
						num10 = ((num9 < num5) ? 1 : 3);
					}
					else
					{
						num10 = ((num9 < num7) ? 2 : 0);
					}
					ptr4[1] = ptr6[1] - ptr2[num10];
					num11 |= num10 << 2;
					num9 = (ptr6[2] << 4) + (7 * ptr4[1] + 3 * ptr5[3] + 5 * ptr5[2] + ptr5[1]);
					if (num9 < num6)
					{
						num10 = ((num9 < num5) ? 1 : 3);
					}
					else
					{
						num10 = ((num9 < num7) ? 2 : 0);
					}
					ptr4[2] = ptr6[2] - ptr2[num10];
					num11 |= num10 << 4;
					num9 = (ptr6[3] << 4) + (7 * ptr4[2] + 5 * ptr5[3] + ptr5[2]);
					if (num9 < num6)
					{
						num10 = ((num9 < num5) ? 1 : 3);
					}
					else
					{
						num10 = ((num9 < num7) ? 2 : 0);
					}
					ptr4[3] = ptr6[3] - ptr2[num10];
					num11 |= num10 << 6;
					ptr6 += 4;
					num |= (uint)((uint)num11 << j * 8);
					int* ptr7 = ptr4;
					ptr4 = ptr5;
					ptr5 = ptr7;
				}
			}
			return num;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002F0C File Offset: 0x0000110C
		public unsafe static void stb__OptimizeColorsBlock(byte* block, ushort* pmax16, ushort* pmin16)
		{
			int num = int.MaxValue;
			int num2 = -2147483647;
			byte* ptr = null;
			byte* ptr2 = null;
			int num3 = 4;
			float* ptr3 = stackalloc float[(UIntPtr)24];
			int* ptr4 = stackalloc int[(UIntPtr)24];
			int* ptr5 = stackalloc int[(UIntPtr)12];
			int* ptr6 = stackalloc int[(UIntPtr)12];
			int* ptr7 = stackalloc int[(UIntPtr)12];
			for (int i = 0; i < 3; i++)
			{
				byte* ptr8 = block + i;
				int num6;
				int num5;
				int num4 = (num5 = (num6 = (int)(*ptr8)));
				for (int j = 4; j < 64; j += 4)
				{
					num5 += (int)ptr8[j];
					if ((int)ptr8[j] < num4)
					{
						num4 = (int)ptr8[j];
					}
					else if ((int)ptr8[j] > num6)
					{
						num6 = (int)ptr8[j];
					}
				}
				ptr5[i] = num5 + 8 >> 4;
				ptr6[i] = num4;
				ptr7[i] = num6;
			}
			for (int j = 0; j < 6; j++)
			{
				ptr4[j] = 0;
			}
			for (int j = 0; j < 16; j++)
			{
				int num7 = (int)block[j * 4] - *ptr5;
				int num8 = (int)block[j * 4 + 1] - ptr5[1];
				int num9 = (int)block[j * 4 + 2] - ptr5[2];
				*ptr4 += num7 * num7;
				ptr4[1] += num7 * num8;
				ptr4[2] += num7 * num9;
				ptr4[3] += num8 * num8;
				ptr4[4] += num8 * num9;
				ptr4[5] += num9 * num9;
			}
			for (int j = 0; j < 6; j++)
			{
				ptr3[j] = (float)ptr4[j] / 255f;
			}
			float num10 = (float)(*ptr7 - *ptr6);
			float num11 = (float)(ptr7[1] - ptr6[1]);
			float num12 = (float)(ptr7[2] - ptr6[2]);
			for (int k = 0; k < num3; k++)
			{
				float num13 = num10 * *ptr3 + num11 * ptr3[1] + num12 * ptr3[2];
				float num14 = num10 * ptr3[1] + num11 * ptr3[3] + num12 * ptr3[4];
				float num15 = num10 * ptr3[2] + num11 * ptr3[4] + num12 * ptr3[5];
				num10 = num13;
				num11 = num14;
				num12 = num15;
			}
			double num16 = (double)CRuntime.fabs((double)num10);
			if ((double)CRuntime.fabs((double)num11) > num16)
			{
				num16 = (double)CRuntime.fabs((double)num11);
			}
			if ((double)CRuntime.fabs((double)num12) > num16)
			{
				num16 = (double)CRuntime.fabs((double)num12);
			}
			int num17;
			int num18;
			int num19;
			if (num16 < 4.0)
			{
				num17 = 299;
				num18 = 587;
				num19 = 114;
			}
			else
			{
				num16 = 512.0 / num16;
				num17 = (int)((double)num10 * num16);
				num18 = (int)((double)num11 * num16);
				num19 = (int)((double)num12 * num16);
			}
			for (int j = 0; j < 16; j++)
			{
				int num20 = (int)block[j * 4] * num17 + (int)block[j * 4 + 1] * num18 + (int)block[j * 4 + 2] * num19;
				if (num20 < num)
				{
					num = num20;
					ptr = block + j * 4;
				}
				if (num20 > num2)
				{
					num2 = num20;
					ptr2 = block + j * 4;
				}
			}
			*pmax16 = StbDxt.stb__As16Bit((int)(*ptr2), (int)ptr2[1], (int)ptr2[2]);
			*pmin16 = StbDxt.stb__As16Bit((int)(*ptr), (int)ptr[1], (int)ptr[2]);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003270 File Offset: 0x00001470
		public static int stb__sclamp(float y, int p0, int p1)
		{
			int num = (int)y;
			if (num < p0)
			{
				return p0;
			}
			if (num > p1)
			{
				return p1;
			}
			return num;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00003290 File Offset: 0x00001490
		public unsafe static int stb__RefineBlock(byte* block, ushort* pmax16, ushort* pmin16, uint mask)
		{
			int* ptr = stackalloc int[(UIntPtr)16];
			*ptr = 3;
			ptr[1] = 0;
			ptr[2] = 2;
			ptr[3] = 1;
			int* ptr2 = stackalloc int[(UIntPtr)16];
			*ptr2 = 589824;
			ptr2[1] = 2304;
			ptr2[2] = 262402;
			ptr2[3] = 66562;
			int num = 0;
			uint num2 = mask;
			ushort num3 = *pmin16;
			ushort num4 = *pmax16;
			ushort num8;
			ushort num9;
			if ((mask ^ (mask << 2)) < 4U)
			{
				int num5 = 8;
				int num6 = 8;
				int num7 = 8;
				for (int i = 0; i < 16; i++)
				{
					num5 += (int)block[i * 4];
					num6 += (int)block[i * 4 + 1];
					num7 += (int)block[i * 4 + 2];
				}
				num5 >>= 4;
				num6 >>= 4;
				num7 >>= 4;
				num8 = (ushort)(((int)StbDxt.stb__OMatch5[num5] << 11) | ((int)StbDxt.stb__OMatch6[num6] << 5) | (int)StbDxt.stb__OMatch5[num7]);
				num9 = (ushort)(((int)StbDxt.stb__OMatch5[num5 + 256] << 11) | ((int)StbDxt.stb__OMatch6[num6 + 256] << 5) | (int)StbDxt.stb__OMatch5[num7 + 256]);
			}
			else
			{
				int num12;
				int num11;
				int num10 = (num11 = (num12 = 0));
				int num15;
				int num14;
				int num13 = (num14 = (num15 = 0));
				int i = 0;
				while (i < 16)
				{
					int num16 = (int)(num2 & 3U);
					int num17 = ptr[num16];
					int num18 = (int)block[i * 4];
					int num19 = (int)block[i * 4 + 1];
					int num20 = (int)block[i * 4 + 2];
					num += ptr2[num16];
					num11 += num17 * num18;
					num10 += num17 * num19;
					num12 += num17 * num20;
					num14 += num18;
					num13 += num19;
					num15 += num20;
					i++;
					num2 >>= 2;
				}
				num14 = 3 * num14 - num11;
				num13 = 3 * num13 - num10;
				num15 = 3 * num15 - num12;
				int num21 = num >> 16;
				int num22 = (num >> 8) & 255;
				int num23 = num & 255;
				float num24 = 0.3647059f / (float)(num21 * num22 - num23 * num23);
				float num25 = num24 * 63f / 31f;
				num8 = (ushort)(StbDxt.stb__sclamp((float)(num11 * num22 - num14 * num23) * num24 + 0.5f, 0, 31) << 11);
				num8 |= (ushort)(StbDxt.stb__sclamp((float)(num10 * num22 - num13 * num23) * num25 + 0.5f, 0, 63) << 5);
				num8 |= (ushort)StbDxt.stb__sclamp((float)(num12 * num22 - num15 * num23) * num24 + 0.5f, 0, 31);
				num9 = (ushort)(StbDxt.stb__sclamp((float)(num14 * num21 - num11 * num23) * num24 + 0.5f, 0, 31) << 11);
				num9 |= (ushort)(StbDxt.stb__sclamp((float)(num13 * num21 - num10 * num23) * num25 + 0.5f, 0, 63) << 5);
				num9 |= (ushort)StbDxt.stb__sclamp((float)(num15 * num21 - num12 * num23) * num24 + 0.5f, 0, 31);
			}
			*pmin16 = num9;
			*pmax16 = num8;
			if (num3 == num9 && num4 == num8)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003598 File Offset: 0x00001798
		public unsafe static void stb__CompressColorBlock(byte* dest, byte* block, int mode)
		{
			byte* ptr = stackalloc byte[(UIntPtr)64];
			byte* color = stackalloc byte[(UIntPtr)16];
			int num = mode & 1;
			int num2 = (((mode & 2) != 0) ? 2 : 1);
			int i = 1;
			while (i < 16 && *(uint*)(block + (IntPtr)i * 4) == *(uint*)block)
			{
				i++;
			}
			uint num6;
			ushort num7;
			ushort num8;
			if (i == 16)
			{
				int num3 = (int)(*block);
				int num4 = (int)block[1];
				int num5 = (int)block[2];
				num6 = 2863311530U;
				num7 = (ushort)(((int)StbDxt.stb__OMatch5[num3] << 11) | ((int)StbDxt.stb__OMatch6[num4] << 5) | (int)StbDxt.stb__OMatch5[num5]);
				num8 = (ushort)(((int)StbDxt.stb__OMatch5[num3 + 256] << 11) | ((int)StbDxt.stb__OMatch6[num4 + 256] << 5) | (int)StbDxt.stb__OMatch5[num5 + 256]);
			}
			else
			{
				if (num != 0)
				{
					StbDxt.stb__DitherBlock(ptr, block);
				}
				StbDxt.stb__OptimizeColorsBlock((num != 0) ? ptr : block, &num7, &num8);
				if (num7 != num8)
				{
					StbDxt.stb__EvalColors(color, num7, num8);
					num6 = StbDxt.stb__MatchColorsBlock(block, color, num);
				}
				else
				{
					num6 = 0U;
				}
				for (i = 0; i < num2; i++)
				{
					uint num9 = num6;
					if (StbDxt.stb__RefineBlock((num != 0) ? ptr : block, &num7, &num8, num6) != 0)
					{
						if (num7 == num8)
						{
							num6 = 0U;
							break;
						}
						StbDxt.stb__EvalColors(color, num7, num8);
						num6 = StbDxt.stb__MatchColorsBlock(block, color, num);
					}
					if (num6 == num9)
					{
						break;
					}
				}
			}
			if (num7 < num8)
			{
				ushort num10 = num8;
				num8 = num7;
				num7 = num10;
				num6 ^= 1431655765U;
			}
			*dest = (byte)num7;
			dest[1] = (byte)(num7 >> 8);
			dest[2] = (byte)num8;
			dest[3] = (byte)(num8 >> 8);
			dest[4] = (byte)num6;
			dest[5] = (byte)(num6 >> 8);
			dest[6] = (byte)(num6 >> 16);
			dest[7] = (byte)(num6 >> 24);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x0000372C File Offset: 0x0000192C
		public unsafe static void stb__CompressAlphaBlock(byte* dest, byte* src, int stride)
		{
			int num2;
			int num = (num2 = (int)(*src));
			for (int i = 1; i < 16; i++)
			{
				if ((int)src[i * stride] < num2)
				{
					num2 = (int)src[i * stride];
				}
				else if ((int)src[i * stride] > num)
				{
					num = (int)src[i * stride];
				}
			}
			*dest = (byte)num;
			dest[1] = (byte)num2;
			dest += 2;
			int num3 = num - num2;
			int num4 = num3 * 4;
			int num5 = num3 * 2;
			int num6 = ((num3 < 8) ? (num3 - 1) : (num3 / 2 + 2));
			num6 -= num2 * 7;
			int num7 = 0;
			int num8 = 0;
			for (int i = 0; i < 16; i++)
			{
				int num9 = (int)(src[i * stride] * 7) + num6;
				int num10 = ((num9 >= num4) ? (-1) : 0);
				int num11 = num10 & 4;
				num9 -= num4 & num10;
				num10 = ((num9 >= num5) ? (-1) : 0);
				num11 += num10 & 2;
				num9 -= num5 & num10;
				num11 += ((num9 >= num3) ? 1 : 0);
				num11 = -num11 & 7;
				num11 ^= ((2 > num11) ? 1 : 0);
				num8 |= num11 << num7;
				if ((num7 += 3) >= 8)
				{
					*(dest++) = (byte)num8;
					num8 >>= 8;
					num7 -= 8;
				}
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003858 File Offset: 0x00001A58
		public static void stb__InitDXT()
		{
			for (int i = 0; i < 32; i++)
			{
				StbDxt.stb__Expand5[i] = (byte)((i << 3) | (i >> 2));
			}
			for (int i = 0; i < 64; i++)
			{
				StbDxt.stb__Expand6[i] = (byte)((i << 2) | (i >> 4));
			}
			for (int i = 0; i < 272; i++)
			{
				int a = ((i - 8 < 0) ? 0 : ((i - 8 > 255) ? 255 : (i - 8)));
				StbDxt.stb__QuantRBTab[i] = StbDxt.stb__Expand5[StbDxt.stb__Mul8Bit(a, 31)];
				StbDxt.stb__QuantGTab[i] = StbDxt.stb__Expand6[StbDxt.stb__Mul8Bit(a, 63)];
			}
			StbDxt.stb__PrepareOptTable(StbDxt.stb__OMatch5, StbDxt.stb__Expand5, 32);
			StbDxt.stb__PrepareOptTable(StbDxt.stb__OMatch6, StbDxt.stb__Expand6, 64);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003917 File Offset: 0x00001B17
		public unsafe static void stb_compress_dxt_block(byte* dest, byte* src, int alpha, int mode)
		{
			if (StbDxt.init != 0)
			{
				StbDxt.stb__InitDXT();
				StbDxt.init = 0;
			}
			if (alpha != 0)
			{
				StbDxt.stb__CompressAlphaBlock(dest, src + 3, 4);
				dest += 8;
			}
			StbDxt.stb__CompressColorBlock(dest, src, mode);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00003945 File Offset: 0x00001B45
		public unsafe static void stb_compress_bc4_block(byte* dest, byte* src)
		{
			StbDxt.stb__CompressAlphaBlock(dest, src, 1);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x0000394F File Offset: 0x00001B4F
		public unsafe static void stb_compress_bc5_block(byte* dest, byte* src)
		{
			StbDxt.stb__CompressAlphaBlock(dest, src, 2);
			StbDxt.stb__CompressAlphaBlock(dest + 8, src + 1, 2);
		}

		// Token: 0x04000017 RID: 23
		public static byte[] stb__Expand5 = new byte[32];

		// Token: 0x04000018 RID: 24
		public static byte[] stb__Expand6 = new byte[64];

		// Token: 0x04000019 RID: 25
		public static byte[] stb__OMatch5 = new byte[512];

		// Token: 0x0400001A RID: 26
		public static byte[] stb__OMatch6 = new byte[512];

		// Token: 0x0400001B RID: 27
		public static byte[] stb__QuantRBTab = new byte[272];

		// Token: 0x0400001C RID: 28
		public static byte[] stb__QuantGTab = new byte[272];

		// Token: 0x0400001D RID: 29
		public static int init = 1;
	}
}
