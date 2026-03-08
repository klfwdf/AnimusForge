using System;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x02000007 RID: 7
	public static class StbImageResize
	{
		// Token: 0x06000031 RID: 49 RVA: 0x000039D0 File Offset: 0x00001BD0
		public static byte stbir__linear_to_srgb_uchar(float _in_)
		{
			StbImageResize.stbir__FP32 stbir__FP = new StbImageResize.stbir__FP32
			{
				u = 1065353215U
			};
			StbImageResize.stbir__FP32 stbir__FP2 = new StbImageResize.stbir__FP32
			{
				u = 956301312U
			};
			StbImageResize.stbir__FP32 stbir__FP3 = default(StbImageResize.stbir__FP32);
			if (_in_ <= stbir__FP2.f)
			{
				_in_ = stbir__FP2.f;
			}
			if (_in_ > stbir__FP.f)
			{
				_in_ = stbir__FP.f;
			}
			stbir__FP3.f = _in_;
			uint num = StbImageResize.fp32_to_srgb8_tab4[(int)(stbir__FP3.u - stbir__FP2.u >> 20)];
			uint num2 = num >> 16 << 9;
			uint num3 = num & 65535U;
			uint num4 = (stbir__FP3.u >> 12) & 255U;
			return (byte)(num2 + num3 * num4 >> 16);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003A7F File Offset: 0x00001C7F
		public static int stbir__min(int a, int b)
		{
			if (a >= b)
			{
				return b;
			}
			return a;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003A88 File Offset: 0x00001C88
		public static int stbir__max(int a, int b)
		{
			if (a <= b)
			{
				return b;
			}
			return a;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003A91 File Offset: 0x00001C91
		public static float stbir__saturate(float x)
		{
			if (x < 0f)
			{
				return 0f;
			}
			if (x > 1f)
			{
				return 1f;
			}
			return x;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003AB1 File Offset: 0x00001CB1
		public static float stbir__srgb_to_linear(float f)
		{
			if (f <= 0.04045f)
			{
				return f / 12.92f;
			}
			return (float)CRuntime.pow((double)((f + 0.055f) / 1.055f), 2.4000000953674316);
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003AE1 File Offset: 0x00001CE1
		public static float stbir__linear_to_srgb(float f)
		{
			if (f <= 0.0031308f)
			{
				return f * 12.92f;
			}
			return 1.055f * (float)CRuntime.pow((double)f, 0.4166666567325592) - 0.055f;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003B14 File Offset: 0x00001D14
		public static float stbir__filter_trapezoid(float x, float scale)
		{
			float num = scale / 2f;
			float num2 = 0.5f + num;
			x = CRuntime.fabs((double)x);
			if (x >= num2)
			{
				return 0f;
			}
			float num3 = 0.5f - num;
			if (x <= num3)
			{
				return 1f;
			}
			return (num2 - x) / scale;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003B60 File Offset: 0x00001D60
		public static float stbir__support_trapezoid(float scale)
		{
			return 0.5f + scale / 2f;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003B70 File Offset: 0x00001D70
		public static float stbir__filter_triangle(float x, float s)
		{
			x = CRuntime.fabs((double)x);
			if (x <= 1f)
			{
				return 1f - x;
			}
			return 0f;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003B94 File Offset: 0x00001D94
		public static float stbir__filter_cubic(float x, float s)
		{
			x = CRuntime.fabs((double)x);
			if (x < 1f)
			{
				return (4f + x * x * (3f * x - 6f)) / 6f;
			}
			if (x < 2f)
			{
				return (8f + x * (-12f + x * (6f - x))) / 6f;
			}
			return 0f;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003C00 File Offset: 0x00001E00
		public static float stbir__filter_catmullrom(float x, float s)
		{
			x = CRuntime.fabs((double)x);
			if (x < 1f)
			{
				return 1f - x * x * (2.5f - 1.5f * x);
			}
			if (x < 2f)
			{
				return 2f - x * (4f + x * (0.5f * x - 2.5f));
			}
			return 0f;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003C64 File Offset: 0x00001E64
		public static float stbir__filter_mitchell(float x, float s)
		{
			x = CRuntime.fabs((double)x);
			if (x < 1f)
			{
				return (16f + x * x * (21f * x - 36f)) / 18f;
			}
			if (x < 2f)
			{
				return (32f + x * (-60f + x * (36f - 7f * x))) / 18f;
			}
			return 0f;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003CD4 File Offset: 0x00001ED4
		public static float stbir__support_zero(float s)
		{
			return 0f;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00003CDB File Offset: 0x00001EDB
		public static float stbir__support_one(float s)
		{
			return 1f;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003CE2 File Offset: 0x00001EE2
		public static float stbir__support_two(float s)
		{
			return 2f;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003CE9 File Offset: 0x00001EE9
		public static int stbir__use_upsampling(float ratio)
		{
			if (ratio <= 1f)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003CF6 File Offset: 0x00001EF6
		public static int stbir__use_width_upsampling(StbImageResize.stbir__info stbir_info)
		{
			return StbImageResize.stbir__use_upsampling(stbir_info.horizontal_scale);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003D04 File Offset: 0x00001F04
		public static int stbir__use_height_upsampling(StbImageResize.stbir__info stbir_info)
		{
			return StbImageResize.stbir__use_upsampling(stbir_info.vertical_scale);
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003D14 File Offset: 0x00001F14
		public static int stbir__get_filter_pixel_width(int filter, float scale)
		{
			if (StbImageResize.stbir__use_upsampling(scale) != 0)
			{
				return (int)CRuntime.ceil((double)(StbImageResize.stbir__filter_info_table[filter].support(1f / scale) * 2f));
			}
			return (int)CRuntime.ceil((double)(StbImageResize.stbir__filter_info_table[filter].support(scale) * 2f / scale));
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003D73 File Offset: 0x00001F73
		public static int stbir__get_filter_pixel_margin(int filter, float scale)
		{
			return StbImageResize.stbir__get_filter_pixel_width(filter, scale) / 2;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003D80 File Offset: 0x00001F80
		public static int stbir__get_coefficient_width(int filter, float scale)
		{
			if (StbImageResize.stbir__use_upsampling(scale) != 0)
			{
				return (int)CRuntime.ceil((double)(StbImageResize.stbir__filter_info_table[filter].support(1f / scale) * 2f));
			}
			return (int)CRuntime.ceil((double)(StbImageResize.stbir__filter_info_table[filter].support(scale) * 2f));
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003DDD File Offset: 0x00001FDD
		public static int stbir__get_contributors(float scale, int filter, int input_size, int output_size)
		{
			if (StbImageResize.stbir__use_upsampling(scale) != 0)
			{
				return output_size;
			}
			return input_size + StbImageResize.stbir__get_filter_pixel_margin(filter, scale) * 2;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003DF6 File Offset: 0x00001FF6
		public static int stbir__get_total_horizontal_coefficients(StbImageResize.stbir__info info)
		{
			return info.horizontal_num_contributors * StbImageResize.stbir__get_coefficient_width(info.horizontal_filter, info.horizontal_scale);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003E11 File Offset: 0x00002011
		public static int stbir__get_total_vertical_coefficients(StbImageResize.stbir__info info)
		{
			return info.vertical_num_contributors * StbImageResize.stbir__get_coefficient_width(info.vertical_filter, info.vertical_scale);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003E2C File Offset: 0x0000202C
		public unsafe static StbImageResize.stbir__contributors* stbir__get_contributor(StbImageResize.stbir__contributors* contributors, int n)
		{
			return contributors + n;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003E3C File Offset: 0x0000203C
		public unsafe static float* stbir__get_coefficient(float* coefficients, int filter, float scale, int n, int c)
		{
			int num = StbImageResize.stbir__get_coefficient_width(filter, scale);
			return coefficients + (num * n + c);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003E60 File Offset: 0x00002060
		public static int stbir__edge_wrap_slow(int edge, int n, int max)
		{
			switch (edge)
			{
			case 1:
				if (n < 0)
				{
					return 0;
				}
				if (n >= max)
				{
					return max - 1;
				}
				return n;
			case 2:
				if (n < 0)
				{
					if (n < max)
					{
						return -n;
					}
					return max - 1;
				}
				else
				{
					if (n < max)
					{
						return n;
					}
					int num = max * 2;
					if (n >= num)
					{
						return 0;
					}
					return num - n - 1;
				}
				break;
			case 3:
			{
				if (n >= 0)
				{
					return n % max;
				}
				int num2 = -n % max;
				if (num2 != 0)
				{
					num2 = max - num2;
				}
				return num2;
			}
			case 4:
				return 0;
			default:
				return 0;
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003ED5 File Offset: 0x000020D5
		public static int stbir__edge_wrap(int edge, int n, int max)
		{
			if (n >= 0 && n < max)
			{
				return n;
			}
			return StbImageResize.stbir__edge_wrap_slow(edge, n, max);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003EEC File Offset: 0x000020EC
		public unsafe static void stbir__calculate_sample_range_upsample(int n, float out_filter_radius, float scale_ratio, float out_shift, int* in_first_pixel, int* in_last_pixel, float* in_center_of_out)
		{
			float num = (float)n + 0.5f;
			float num2 = num - out_filter_radius;
			float num3 = num + out_filter_radius;
			float num4 = (num2 + out_shift) / scale_ratio;
			float num5 = (num3 + out_shift) / scale_ratio;
			*in_center_of_out = (num + out_shift) / scale_ratio;
			*in_first_pixel = (int)CRuntime.floor((double)num4 + 0.5);
			*in_last_pixel = (int)CRuntime.floor((double)num5 - 0.5);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003F50 File Offset: 0x00002150
		public unsafe static void stbir__calculate_sample_range_downsample(int n, float in_pixels_radius, float scale_ratio, float out_shift, int* out_first_pixel, int* out_last_pixel, float* out_center_of_in)
		{
			float num = (float)n + 0.5f;
			float num2 = num - in_pixels_radius;
			float num3 = num + in_pixels_radius;
			float num4 = num2 * scale_ratio - out_shift;
			float num5 = num3 * scale_ratio - out_shift;
			*out_center_of_in = num * scale_ratio - out_shift;
			*out_first_pixel = (int)CRuntime.floor((double)num4 + 0.5);
			*out_last_pixel = (int)CRuntime.floor((double)num5 - 0.5);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003FB4 File Offset: 0x000021B4
		public unsafe static void stbir__calculate_coefficients_upsample(StbImageResize.stbir__info stbir_info, int filter, float scale, int in_first_pixel, int in_last_pixel, float in_center_of_out, StbImageResize.stbir__contributors* contributor, float* coefficient_group)
		{
			float num = 0f;
			contributor->n0 = in_first_pixel;
			contributor->n1 = in_last_pixel;
			int i;
			for (i = 0; i <= in_last_pixel - in_first_pixel; i++)
			{
				float num2 = (float)(i + in_first_pixel) + 0.5f;
				coefficient_group[i] = StbImageResize.stbir__filter_info_table[filter].kernel(in_center_of_out - num2, 1f / scale);
				if (i == 0 && coefficient_group[i] == 0f)
				{
					in_first_pixel = (contributor->n0 = in_first_pixel + 1);
					i--;
				}
				else
				{
					num += coefficient_group[i];
				}
			}
			float num3 = 1f / num;
			for (i = 0; i <= in_last_pixel - in_first_pixel; i++)
			{
				coefficient_group[i] *= num3;
			}
			i = in_last_pixel - in_first_pixel;
			while (i >= 0 && coefficient_group[i] == 0f)
			{
				contributor->n1 = contributor->n0 + i - 1;
				i--;
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000040A0 File Offset: 0x000022A0
		public unsafe static void stbir__calculate_coefficients_downsample(StbImageResize.stbir__info stbir_info, int filter, float scale_ratio, int out_first_pixel, int out_last_pixel, float out_center_of_in, StbImageResize.stbir__contributors* contributor, float* coefficient_group)
		{
			contributor->n0 = out_first_pixel;
			contributor->n1 = out_last_pixel;
			int i;
			for (i = 0; i <= out_last_pixel - out_first_pixel; i++)
			{
				float num = (float)(i + out_first_pixel) + 0.5f - out_center_of_in;
				coefficient_group[i] = StbImageResize.stbir__filter_info_table[filter].kernel(num, scale_ratio) * scale_ratio;
			}
			i = out_last_pixel - out_first_pixel;
			while (i >= 0 && coefficient_group[i] == 0f)
			{
				contributor->n1 = contributor->n0 + i - 1;
				i--;
			}
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004130 File Offset: 0x00002330
		public unsafe static void stbir__normalize_downsample_coefficients(StbImageResize.stbir__info stbir_info, StbImageResize.stbir__contributors* contributors, float* coefficients, int filter, float scale_ratio, float shift, int input_size, int output_size)
		{
			int num = StbImageResize.stbir__get_contributors(scale_ratio, filter, input_size, output_size);
			int a = StbImageResize.stbir__get_coefficient_width(filter, scale_ratio);
			for (int i = 0; i < output_size; i++)
			{
				float num2 = 0f;
				for (int j = 0; j < num; j++)
				{
					if (i >= contributors[j].n0 && i <= contributors[j].n1)
					{
						float num3 = *StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, j, i - contributors[j].n0);
						num2 += num3;
					}
					else if (i < contributors[j].n0)
					{
						break;
					}
				}
				float num4 = 1f / num2;
				for (int j = 0; j < num; j++)
				{
					if (i >= contributors[j].n0 && i <= contributors[j].n1)
					{
						*StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, j, i - contributors[j].n0) *= num4;
					}
					else if (i < contributors[j].n0)
					{
						break;
					}
				}
			}
			for (int j = 0; j < num; j++)
			{
				int num5 = 0;
				while (*StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, j, num5) == 0f)
				{
					num5++;
				}
				contributors[j].n0 += num5;
				while (contributors[j].n0 < 0)
				{
					contributors[j].n0++;
					num5++;
				}
				int b = contributors[j].n1 - contributors[j].n0 + 1;
				int num6 = StbImageResize.stbir__min(a, b);
				int num7 = StbImageResize.stbir__get_coefficient_width(filter, scale_ratio);
				int i = 0;
				while (i < num6 && i + num5 < num7)
				{
					*StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, j, i) = *StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, j, i + num5);
					i++;
				}
			}
			for (int i = 0; i < num; i++)
			{
				contributors[i].n1 = StbImageResize.stbir__min(contributors[i].n1, output_size - 1);
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00004370 File Offset: 0x00002570
		public unsafe static void stbir__calculate_filters(StbImageResize.stbir__info stbir_info, StbImageResize.stbir__contributors* contributors, float* coefficients, int filter, float scale_ratio, float shift, int input_size, int output_size)
		{
			int num = StbImageResize.stbir__get_contributors(scale_ratio, filter, input_size, output_size);
			if (StbImageResize.stbir__use_upsampling(scale_ratio) != 0)
			{
				float num2 = StbImageResize.stbir__filter_info_table[filter].support(1f / scale_ratio) * scale_ratio;
				for (int i = 0; i < num; i++)
				{
					int in_first_pixel;
					int in_last_pixel;
					float num3;
					StbImageResize.stbir__calculate_sample_range_upsample(i, num2, scale_ratio, shift, &in_first_pixel, &in_last_pixel, &num3);
					StbImageResize.stbir__calculate_coefficients_upsample(stbir_info, filter, scale_ratio, in_first_pixel, in_last_pixel, num3, StbImageResize.stbir__get_contributor(contributors, i), StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, i, 0));
				}
				return;
			}
			float num4 = StbImageResize.stbir__filter_info_table[filter].support(scale_ratio) / scale_ratio;
			for (int i = 0; i < num; i++)
			{
				int out_first_pixel;
				int out_last_pixel;
				float num5;
				StbImageResize.stbir__calculate_sample_range_downsample(i - StbImageResize.stbir__get_filter_pixel_margin(filter, scale_ratio), num4, scale_ratio, shift, &out_first_pixel, &out_last_pixel, &num5);
				StbImageResize.stbir__calculate_coefficients_downsample(stbir_info, filter, scale_ratio, out_first_pixel, out_last_pixel, num5, StbImageResize.stbir__get_contributor(contributors, i), StbImageResize.stbir__get_coefficient(coefficients, filter, scale_ratio, i, 0));
			}
			StbImageResize.stbir__normalize_downsample_coefficients(stbir_info, contributors, coefficients, filter, scale_ratio, shift, input_size, output_size);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x0000447A File Offset: 0x0000267A
		public unsafe static float* stbir__get_decode_buffer(StbImageResize.stbir__info stbir_info)
		{
			return stbir_info.decode_buffer + stbir_info.horizontal_filter_pixel_margin * stbir_info.channels;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00004494 File Offset: 0x00002694
		public unsafe static void stbir__decode_scanline(StbImageResize.stbir__info stbir_info, int n)
		{
			int channels = stbir_info.channels;
			int alpha_channel = stbir_info.alpha_channel;
			int type = stbir_info.type;
			int colorspace = stbir_info.colorspace;
			int input_w = stbir_info.input_w;
			ulong num = (ulong)((long)stbir_info.input_stride_bytes);
			float* ptr = StbImageResize.stbir__get_decode_buffer(stbir_info);
			int edge_horizontal = stbir_info.edge_horizontal;
			int edge_vertical = stbir_info.edge_vertical;
			ulong num2 = (ulong)((long)(StbImageResize.stbir__edge_wrap(edge_vertical, n, stbir_info.input_h) * (int)num));
			void* ptr2 = (void*)((byte*)stbir_info.input_data + num2);
			int num3 = input_w + stbir_info.horizontal_filter_pixel_margin;
			int num4 = type * 2 + colorspace;
			int i = -stbir_info.horizontal_filter_pixel_margin;
			if (edge_vertical == 4)
			{
				if (n >= 0)
				{
					if (n < stbir_info.input_h)
					{
						goto IL_BD;
					}
				}
				while (i < num3)
				{
					for (int j = 0; j < channels; j++)
					{
						ptr[i * channels + j] = 0f;
					}
					i++;
				}
				return;
			}
			IL_BD:
			switch (num4)
			{
			case 0:
				while (i < num3)
				{
					int num5 = i * channels;
					int num6 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num5 + j] = (float)((byte*)ptr2)[num6 + j] / 255f;
					}
					i++;
				}
				break;
			case 1:
				while (i < num3)
				{
					int num7 = i * channels;
					int num8 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num7 + j] = StbImageResize.stbir__srgb_uchar_to_linear_float[(int)((byte*)ptr2)[num8 + j]];
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						ptr[num7 + alpha_channel] = (float)((byte*)ptr2)[num8 + alpha_channel] / 255f;
					}
					i++;
				}
				break;
			case 2:
				while (i < num3)
				{
					int num9 = i * channels;
					int num10 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num9 + j] = (float)(*(ushort*)((byte*)ptr2 + (IntPtr)(num10 + j) * 2)) / 65535f;
					}
					i++;
				}
				break;
			case 3:
				while (i < num3)
				{
					int num11 = i * channels;
					int num12 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num11 + j] = StbImageResize.stbir__srgb_to_linear((float)(*(ushort*)((byte*)ptr2 + (IntPtr)(num12 + j) * 2)) / 65535f);
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						ptr[num11 + alpha_channel] = (float)(*(ushort*)((byte*)ptr2 + (IntPtr)(num12 + alpha_channel) * 2)) / 65535f;
					}
					i++;
				}
				break;
			case 4:
				while (i < num3)
				{
					int num13 = i * channels;
					int num14 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num13 + j] = (float)(*(uint*)((byte*)ptr2 + (IntPtr)(num14 + j) * 4) / 4294967295.0);
					}
					i++;
				}
				break;
			case 5:
				while (i < num3)
				{
					int num15 = i * channels;
					int num16 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num15 + j] = StbImageResize.stbir__srgb_to_linear((float)(*(uint*)((byte*)ptr2 + (IntPtr)(num16 + j) * 4) / 4294967295.0));
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						ptr[num15 + alpha_channel] = (float)(*(uint*)((byte*)ptr2 + (IntPtr)(num16 + alpha_channel) * 4) / 4294967295.0);
					}
					i++;
				}
				break;
			case 6:
				while (i < num3)
				{
					int num17 = i * channels;
					int num18 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num17 + j] = *(float*)((byte*)ptr2 + (IntPtr)(num18 + j) * 4);
					}
					i++;
				}
				break;
			case 7:
				while (i < num3)
				{
					int num19 = i * channels;
					int num20 = StbImageResize.stbir__edge_wrap(edge_horizontal, i, input_w) * channels;
					for (int j = 0; j < channels; j++)
					{
						ptr[num19 + j] = StbImageResize.stbir__srgb_to_linear(*(float*)((byte*)ptr2 + (IntPtr)(num20 + j) * 4));
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						ptr[num19 + alpha_channel] = *(float*)((byte*)ptr2 + (IntPtr)(num20 + alpha_channel) * 4);
					}
					i++;
				}
				break;
			}
			if ((stbir_info.flags & 1U) == 0U)
			{
				for (i = -stbir_info.horizontal_filter_pixel_margin; i < num3; i++)
				{
					int num21 = i * channels;
					float num22 = ptr[num21 + alpha_channel];
					if (stbir_info.type != 3)
					{
						num22 += 8.271806E-25f;
						ptr[num21 + alpha_channel] = num22;
					}
					for (int j = 0; j < channels; j++)
					{
						if (j != alpha_channel)
						{
							ptr[num21 + j] *= num22;
						}
					}
				}
			}
			if (edge_horizontal == 4)
			{
				for (i = -stbir_info.horizontal_filter_pixel_margin; i < 0; i++)
				{
					for (int j = 0; j < channels; j++)
					{
						ptr[i * channels + j] = 0f;
					}
				}
				for (i = input_w; i < num3; i++)
				{
					for (int j = 0; j < channels; j++)
					{
						ptr[i * channels + j] = 0f;
					}
				}
			}
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00004987 File Offset: 0x00002B87
		public unsafe static float* stbir__get_ring_buffer_entry(float* ring_buffer, int index, int ring_buffer_length)
		{
			return ring_buffer + index * ring_buffer_length;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00004994 File Offset: 0x00002B94
		public unsafe static float* stbir__add_empty_ring_buffer_entry(StbImageResize.stbir__info stbir_info, int n)
		{
			stbir_info.ring_buffer_last_scanline = n;
			int index;
			if (stbir_info.ring_buffer_begin_index < 0)
			{
				index = (stbir_info.ring_buffer_begin_index = 0);
				stbir_info.ring_buffer_first_scanline = n;
			}
			else
			{
				index = (stbir_info.ring_buffer_begin_index + (stbir_info.ring_buffer_last_scanline - stbir_info.ring_buffer_first_scanline)) % stbir_info.ring_buffer_num_entries;
			}
			float* ptr = StbImageResize.stbir__get_ring_buffer_entry(stbir_info.ring_buffer, index, stbir_info.ring_buffer_length_bytes / 4);
			CRuntime.memset((void*)ptr, 0, (ulong)((long)stbir_info.ring_buffer_length_bytes));
			return ptr;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00004A04 File Offset: 0x00002C04
		public unsafe static void stbir__resample_horizontal_upsample(StbImageResize.stbir__info stbir_info, int n, float* output_buffer)
		{
			int output_w = stbir_info.output_w;
			int horizontal_filter_pixel_width = stbir_info.horizontal_filter_pixel_width;
			int channels = stbir_info.channels;
			float* ptr = StbImageResize.stbir__get_decode_buffer(stbir_info);
			StbImageResize.stbir__contributors* horizontal_contributors = stbir_info.horizontal_contributors;
			float* horizontal_coefficients = stbir_info.horizontal_coefficients;
			int horizontal_coefficient_width = stbir_info.horizontal_coefficient_width;
			for (int i = 0; i < output_w; i++)
			{
				int n2 = horizontal_contributors[i].n0;
				int n3 = horizontal_contributors[i].n1;
				int num = i * channels;
				int num2 = horizontal_coefficient_width * i;
				int num3 = 0;
				switch (channels)
				{
				case 1:
					for (int j = n2; j <= n3; j++)
					{
						int num4 = j;
						float num5 = horizontal_coefficients[(IntPtr)(num2 + num3++) * 4];
						output_buffer[num] += ptr[num4] * num5;
					}
					break;
				case 2:
					for (int j = n2; j <= n3; j++)
					{
						int num6 = j * 2;
						float num7 = horizontal_coefficients[(IntPtr)(num2 + num3++) * 4];
						output_buffer[num] += ptr[num6] * num7;
						output_buffer[num + 1] += ptr[num6 + 1] * num7;
					}
					break;
				case 3:
					for (int j = n2; j <= n3; j++)
					{
						int num8 = j * 3;
						float num9 = horizontal_coefficients[(IntPtr)(num2 + num3++) * 4];
						output_buffer[num] += ptr[num8] * num9;
						output_buffer[num + 1] += ptr[num8 + 1] * num9;
						output_buffer[num + 2] += ptr[num8 + 2] * num9;
					}
					break;
				case 4:
					for (int j = n2; j <= n3; j++)
					{
						int num10 = j * 4;
						float num11 = horizontal_coefficients[(IntPtr)(num2 + num3++) * 4];
						output_buffer[num] += ptr[num10] * num11;
						output_buffer[num + 1] += ptr[num10 + 1] * num11;
						output_buffer[num + 2] += ptr[num10 + 2] * num11;
						output_buffer[num + 3] += ptr[num10 + 3] * num11;
					}
					break;
				default:
					for (int j = n2; j <= n3; j++)
					{
						int num12 = j * channels;
						float num13 = horizontal_coefficients[(IntPtr)(num2 + num3++) * 4];
						for (int k = 0; k < channels; k++)
						{
							output_buffer[num + k] += ptr[num12 + k] * num13;
						}
					}
					break;
				}
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00004CB8 File Offset: 0x00002EB8
		public unsafe static void stbir__resample_horizontal_downsample(StbImageResize.stbir__info stbir_info, int n, float* output_buffer)
		{
			int input_w = stbir_info.input_w;
			int output_w = stbir_info.output_w;
			int horizontal_filter_pixel_width = stbir_info.horizontal_filter_pixel_width;
			int channels = stbir_info.channels;
			float* ptr = StbImageResize.stbir__get_decode_buffer(stbir_info);
			StbImageResize.stbir__contributors* horizontal_contributors = stbir_info.horizontal_contributors;
			float* horizontal_coefficients = stbir_info.horizontal_coefficients;
			int horizontal_coefficient_width = stbir_info.horizontal_coefficient_width;
			int horizontal_filter_pixel_margin = stbir_info.horizontal_filter_pixel_margin;
			int num = input_w + horizontal_filter_pixel_margin * 2;
			switch (channels)
			{
			case 1:
				for (int i = 0; i < num; i++)
				{
					int n2 = horizontal_contributors[i].n0;
					int n3 = horizontal_contributors[i].n1;
					int num2 = i - horizontal_filter_pixel_margin;
					int num3 = n3;
					int num4 = horizontal_coefficient_width * i;
					for (int j = n2; j <= num3; j++)
					{
						int num5 = j;
						float num6 = horizontal_coefficients[num4 + j - n2];
						output_buffer[num5] += ptr[num2] * num6;
					}
				}
				return;
			case 2:
				for (int i = 0; i < num; i++)
				{
					int n4 = horizontal_contributors[i].n0;
					int n5 = horizontal_contributors[i].n1;
					int num7 = (i - horizontal_filter_pixel_margin) * 2;
					int num8 = n5;
					int num9 = horizontal_coefficient_width * i;
					for (int j = n4; j <= num8; j++)
					{
						int num10 = j * 2;
						float num11 = horizontal_coefficients[num9 + j - n4];
						output_buffer[num10] += ptr[num7] * num11;
						output_buffer[num10 + 1] += ptr[num7 + 1] * num11;
					}
				}
				return;
			case 3:
				for (int i = 0; i < num; i++)
				{
					int n6 = horizontal_contributors[i].n0;
					int n7 = horizontal_contributors[i].n1;
					int num12 = (i - horizontal_filter_pixel_margin) * 3;
					int num13 = n7;
					int num14 = horizontal_coefficient_width * i;
					for (int j = n6; j <= num13; j++)
					{
						int num15 = j * 3;
						float num16 = horizontal_coefficients[num14 + j - n6];
						output_buffer[num15] += ptr[num12] * num16;
						output_buffer[num15 + 1] += ptr[num12 + 1] * num16;
						output_buffer[num15 + 2] += ptr[num12 + 2] * num16;
					}
				}
				return;
			case 4:
				for (int i = 0; i < num; i++)
				{
					int n8 = horizontal_contributors[i].n0;
					int n9 = horizontal_contributors[i].n1;
					int num17 = (i - horizontal_filter_pixel_margin) * 4;
					int num18 = n9;
					int num19 = horizontal_coefficient_width * i;
					for (int j = n8; j <= num18; j++)
					{
						int num20 = j * 4;
						float num21 = horizontal_coefficients[num19 + j - n8];
						output_buffer[num20] += ptr[num17] * num21;
						output_buffer[num20 + 1] += ptr[num17 + 1] * num21;
						output_buffer[num20 + 2] += ptr[num17 + 2] * num21;
						output_buffer[num20 + 3] += ptr[num17 + 3] * num21;
					}
				}
				return;
			default:
				for (int i = 0; i < num; i++)
				{
					int n10 = horizontal_contributors[i].n0;
					int n11 = horizontal_contributors[i].n1;
					int num22 = (i - horizontal_filter_pixel_margin) * channels;
					int num23 = n11;
					int num24 = horizontal_coefficient_width * i;
					for (int j = n10; j <= num23; j++)
					{
						int num25 = j * channels;
						float num26 = horizontal_coefficients[num24 + j - n10];
						for (int k = 0; k < channels; k++)
						{
							output_buffer[num25 + k] += ptr[num22 + k] * num26;
						}
					}
				}
				return;
			}
		}

		// Token: 0x06000059 RID: 89 RVA: 0x0000506D File Offset: 0x0000326D
		public static void stbir__decode_and_resample_upsample(StbImageResize.stbir__info stbir_info, int n)
		{
			StbImageResize.stbir__decode_scanline(stbir_info, n);
			if (StbImageResize.stbir__use_width_upsampling(stbir_info) != 0)
			{
				StbImageResize.stbir__resample_horizontal_upsample(stbir_info, n, StbImageResize.stbir__add_empty_ring_buffer_entry(stbir_info, n));
				return;
			}
			StbImageResize.stbir__resample_horizontal_downsample(stbir_info, n, StbImageResize.stbir__add_empty_ring_buffer_entry(stbir_info, n));
		}

		// Token: 0x0600005A RID: 90 RVA: 0x0000509C File Offset: 0x0000329C
		public unsafe static void stbir__decode_and_resample_downsample(StbImageResize.stbir__info stbir_info, int n)
		{
			StbImageResize.stbir__decode_scanline(stbir_info, n);
			CRuntime.memset((void*)stbir_info.horizontal_buffer, 0, (ulong)((long)(stbir_info.output_w * stbir_info.channels * 4)));
			if (StbImageResize.stbir__use_width_upsampling(stbir_info) != 0)
			{
				StbImageResize.stbir__resample_horizontal_upsample(stbir_info, n, stbir_info.horizontal_buffer);
				return;
			}
			StbImageResize.stbir__resample_horizontal_downsample(stbir_info, n, stbir_info.horizontal_buffer);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x000050F0 File Offset: 0x000032F0
		public unsafe static float* stbir__get_ring_buffer_scanline(int get_scanline, float* ring_buffer, int begin_index, int first_scanline, int ring_buffer_num_entries, int ring_buffer_length)
		{
			int index = (begin_index + (get_scanline - first_scanline)) % ring_buffer_num_entries;
			return StbImageResize.stbir__get_ring_buffer_entry(ring_buffer, index, ring_buffer_length);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00005110 File Offset: 0x00003310
		public unsafe static void stbir__encode_scanline(StbImageResize.stbir__info stbir_info, int num_pixels, void* output_buffer, float* encode_buffer, int channels, int alpha_channel, int decode)
		{
			ushort* ptr = stackalloc ushort[(UIntPtr)128];
			int i;
			if ((stbir_info.flags & 1U) == 0U)
			{
				for (i = 0; i < num_pixels; i++)
				{
					int num = i * channels;
					float num2 = encode_buffer[num + alpha_channel];
					float num3 = ((num2 != 0f) ? (1f / num2) : 0f);
					for (int j = 0; j < channels; j++)
					{
						if (j != alpha_channel)
						{
							encode_buffer[num + j] *= num3;
						}
					}
				}
			}
			i = 0;
			int num4 = 0;
			while (i < channels)
			{
				if (i != alpha_channel || (stbir_info.flags & 2U) != 0U)
				{
					ptr[(IntPtr)(num4++) * 2] = (ushort)i;
				}
				i++;
			}
			switch (decode)
			{
			case 0:
				for (i = 0; i < num_pixels; i++)
				{
					int num5 = i * channels;
					for (int j = 0; j < channels; j++)
					{
						int num6 = num5 + j;
						((byte*)output_buffer)[num6] = (byte)((int)((double)(StbImageResize.stbir__saturate(encode_buffer[num6]) * 255f) + 0.5));
					}
				}
				return;
			case 1:
				for (i = 0; i < num_pixels; i++)
				{
					int num7 = i * channels;
					for (int j = 0; j < num4; j++)
					{
						int num8 = num7 + (int)ptr[j];
						((byte*)output_buffer)[num8] = StbImageResize.stbir__linear_to_srgb_uchar(encode_buffer[num8]);
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						((byte*)output_buffer)[num7 + alpha_channel] = (byte)((int)((double)(StbImageResize.stbir__saturate(encode_buffer[num7 + alpha_channel]) * 255f) + 0.5));
					}
				}
				return;
			case 2:
				for (i = 0; i < num_pixels; i++)
				{
					int num9 = i * channels;
					for (int j = 0; j < channels; j++)
					{
						int num10 = num9 + j;
						*(short*)((byte*)output_buffer + (IntPtr)num10 * 2) = (short)((ushort)((int)((double)(StbImageResize.stbir__saturate(encode_buffer[num10]) * 65535f) + 0.5)));
					}
				}
				return;
			case 3:
				for (i = 0; i < num_pixels; i++)
				{
					int num11 = i * channels;
					for (int j = 0; j < num4; j++)
					{
						int num12 = num11 + (int)ptr[j];
						*(short*)((byte*)output_buffer + (IntPtr)num12 * 2) = (short)((ushort)((int)((double)(StbImageResize.stbir__linear_to_srgb(StbImageResize.stbir__saturate(encode_buffer[num12])) * 65535f) + 0.5)));
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						*(short*)((byte*)output_buffer + (IntPtr)(num11 + alpha_channel) * 2) = (short)((ushort)((int)((double)(StbImageResize.stbir__saturate(encode_buffer[num11 + alpha_channel]) * 65535f) + 0.5)));
					}
				}
				return;
			case 4:
				for (i = 0; i < num_pixels; i++)
				{
					int num13 = i * channels;
					for (int j = 0; j < channels; j++)
					{
						int num14 = num13 + j;
						*(int*)((byte*)output_buffer + (IntPtr)num14 * 4) = (int)((uint)((double)StbImageResize.stbir__saturate(encode_buffer[num14]) * 4294967295.0 + 0.5));
					}
				}
				return;
			case 5:
				for (i = 0; i < num_pixels; i++)
				{
					int num15 = i * channels;
					for (int j = 0; j < num4; j++)
					{
						int num16 = num15 + (int)ptr[j];
						*(int*)((byte*)output_buffer + (IntPtr)num16 * 4) = (int)((uint)((double)StbImageResize.stbir__linear_to_srgb(StbImageResize.stbir__saturate(encode_buffer[num16])) * 4294967295.0 + 0.5));
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						*(int*)((byte*)output_buffer + (IntPtr)(num15 + alpha_channel) * 4) = (int)((double)StbImageResize.stbir__saturate(encode_buffer[num15 + alpha_channel]) * 4294967295.0 + 0.5);
					}
				}
				return;
			case 6:
				for (i = 0; i < num_pixels; i++)
				{
					int num17 = i * channels;
					for (int j = 0; j < channels; j++)
					{
						int num18 = num17 + j;
						*(float*)((byte*)output_buffer + (IntPtr)num18 * 4) = encode_buffer[num18];
					}
				}
				return;
			case 7:
				for (i = 0; i < num_pixels; i++)
				{
					int num19 = i * channels;
					for (int j = 0; j < num4; j++)
					{
						int num20 = num19 + (int)ptr[j];
						*(float*)((byte*)output_buffer + (IntPtr)num20 * 4) = StbImageResize.stbir__linear_to_srgb(encode_buffer[num20]);
					}
					if ((stbir_info.flags & 2U) == 0U)
					{
						*(float*)((byte*)output_buffer + (IntPtr)(num19 + alpha_channel) * 4) = encode_buffer[num19 + alpha_channel];
					}
				}
				return;
			default:
				return;
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00005514 File Offset: 0x00003714
		public unsafe static void stbir__resample_vertical_upsample(StbImageResize.stbir__info stbir_info, int n, int in_first_scanline, int in_last_scanline, float in_center_of_out)
		{
			int output_w = stbir_info.output_w;
			StbImageResize.stbir__contributors* vertical_contributors = stbir_info.vertical_contributors;
			float* vertical_coefficients = stbir_info.vertical_coefficients;
			int channels = stbir_info.channels;
			int alpha_channel = stbir_info.alpha_channel;
			int type = stbir_info.type;
			int colorspace = stbir_info.colorspace;
			int ring_buffer_num_entries = stbir_info.ring_buffer_num_entries;
			void* output_data = stbir_info.output_data;
			float* encode_buffer = stbir_info.encode_buffer;
			int decode = type * 2 + colorspace;
			int vertical_coefficient_width = stbir_info.vertical_coefficient_width;
			float* ring_buffer = stbir_info.ring_buffer;
			int ring_buffer_begin_index = stbir_info.ring_buffer_begin_index;
			int ring_buffer_first_scanline = stbir_info.ring_buffer_first_scanline;
			int ring_buffer_last_scanline = stbir_info.ring_buffer_last_scanline;
			int ring_buffer_length = stbir_info.ring_buffer_length_bytes / 4;
			int num = vertical_coefficient_width * n;
			int n2 = vertical_contributors[n].n0;
			int n3 = vertical_contributors[n].n1;
			int num2 = n * stbir_info.output_stride_bytes;
			CRuntime.memset((void*)encode_buffer, 0, (ulong)((long)(output_w * 4 * channels)));
			int num3 = 0;
			switch (channels)
			{
			case 1:
				for (int i = n2; i <= n3; i++)
				{
					int num4 = num3++;
					float* ptr = StbImageResize.stbir__get_ring_buffer_scanline(i, ring_buffer, ring_buffer_begin_index, ring_buffer_first_scanline, ring_buffer_num_entries, ring_buffer_length);
					float num5 = vertical_coefficients[num + num4];
					for (int j = 0; j < output_w; j++)
					{
						int num6 = j;
						encode_buffer[num6] += ptr[num6] * num5;
					}
				}
				break;
			case 2:
				for (int i = n2; i <= n3; i++)
				{
					int num7 = num3++;
					float* ptr2 = StbImageResize.stbir__get_ring_buffer_scanline(i, ring_buffer, ring_buffer_begin_index, ring_buffer_first_scanline, ring_buffer_num_entries, ring_buffer_length);
					float num8 = vertical_coefficients[num + num7];
					for (int j = 0; j < output_w; j++)
					{
						int num9 = j * 2;
						encode_buffer[num9] += ptr2[num9] * num8;
						encode_buffer[num9 + 1] += ptr2[num9 + 1] * num8;
					}
				}
				break;
			case 3:
				for (int i = n2; i <= n3; i++)
				{
					int num10 = num3++;
					float* ptr3 = StbImageResize.stbir__get_ring_buffer_scanline(i, ring_buffer, ring_buffer_begin_index, ring_buffer_first_scanline, ring_buffer_num_entries, ring_buffer_length);
					float num11 = vertical_coefficients[num + num10];
					for (int j = 0; j < output_w; j++)
					{
						int num12 = j * 3;
						encode_buffer[num12] += ptr3[num12] * num11;
						encode_buffer[num12 + 1] += ptr3[num12 + 1] * num11;
						encode_buffer[num12 + 2] += ptr3[num12 + 2] * num11;
					}
				}
				break;
			case 4:
				for (int i = n2; i <= n3; i++)
				{
					int num13 = num3++;
					float* ptr4 = StbImageResize.stbir__get_ring_buffer_scanline(i, ring_buffer, ring_buffer_begin_index, ring_buffer_first_scanline, ring_buffer_num_entries, ring_buffer_length);
					float num14 = vertical_coefficients[num + num13];
					for (int j = 0; j < output_w; j++)
					{
						int num15 = j * 4;
						encode_buffer[num15] += ptr4[num15] * num14;
						encode_buffer[num15 + 1] += ptr4[num15 + 1] * num14;
						encode_buffer[num15 + 2] += ptr4[num15 + 2] * num14;
						encode_buffer[num15 + 3] += ptr4[num15 + 3] * num14;
					}
				}
				break;
			default:
				for (int i = n2; i <= n3; i++)
				{
					int num16 = num3++;
					float* ptr5 = StbImageResize.stbir__get_ring_buffer_scanline(i, ring_buffer, ring_buffer_begin_index, ring_buffer_first_scanline, ring_buffer_num_entries, ring_buffer_length);
					float num17 = vertical_coefficients[num + num16];
					for (int j = 0; j < output_w; j++)
					{
						int num18 = j * channels;
						for (int k = 0; k < channels; k++)
						{
							encode_buffer[num18 + k] += ptr5[num18 + k] * num17;
						}
					}
				}
				break;
			}
			StbImageResize.stbir__encode_scanline(stbir_info, output_w, (void*)((byte*)output_data + num2), encode_buffer, channels, alpha_channel, decode);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x000058E4 File Offset: 0x00003AE4
		public unsafe static void stbir__resample_vertical_downsample(StbImageResize.stbir__info stbir_info, int n, int in_first_scanline, int in_last_scanline, float in_center_of_out)
		{
			int output_w = stbir_info.output_w;
			int output_h = stbir_info.output_h;
			StbImageResize.stbir__contributors* vertical_contributors = stbir_info.vertical_contributors;
			float* vertical_coefficients = stbir_info.vertical_coefficients;
			int channels = stbir_info.channels;
			int ring_buffer_num_entries = stbir_info.ring_buffer_num_entries;
			void* output_data = stbir_info.output_data;
			float* horizontal_buffer = stbir_info.horizontal_buffer;
			int vertical_coefficient_width = stbir_info.vertical_coefficient_width;
			int num = n + stbir_info.vertical_filter_pixel_margin;
			float* ring_buffer = stbir_info.ring_buffer;
			int ring_buffer_begin_index = stbir_info.ring_buffer_begin_index;
			int ring_buffer_first_scanline = stbir_info.ring_buffer_first_scanline;
			int ring_buffer_last_scanline = stbir_info.ring_buffer_last_scanline;
			int ring_buffer_length = stbir_info.ring_buffer_length_bytes / 4;
			int n2 = vertical_contributors[num].n0;
			int n3 = vertical_contributors[num].n1;
			for (int i = n2; i <= n3; i++)
			{
				int num2 = i - n2;
				int num3 = vertical_coefficient_width * num;
				float num4 = vertical_coefficients[num3 + num2];
				float* ptr = StbImageResize.stbir__get_ring_buffer_scanline(i, ring_buffer, ring_buffer_begin_index, ring_buffer_first_scanline, ring_buffer_num_entries, ring_buffer_length);
				switch (channels)
				{
				case 1:
					for (int j = 0; j < output_w; j++)
					{
						int num5 = j;
						ptr[num5] += horizontal_buffer[num5] * num4;
					}
					break;
				case 2:
					for (int j = 0; j < output_w; j++)
					{
						int num6 = j * 2;
						ptr[num6] += horizontal_buffer[num6] * num4;
						ptr[num6 + 1] += horizontal_buffer[num6 + 1] * num4;
					}
					break;
				case 3:
					for (int j = 0; j < output_w; j++)
					{
						int num7 = j * 3;
						ptr[num7] += horizontal_buffer[num7] * num4;
						ptr[num7 + 1] += horizontal_buffer[num7 + 1] * num4;
						ptr[num7 + 2] += horizontal_buffer[num7 + 2] * num4;
					}
					break;
				case 4:
					for (int j = 0; j < output_w; j++)
					{
						int num8 = j * 4;
						ptr[num8] += horizontal_buffer[num8] * num4;
						ptr[num8 + 1] += horizontal_buffer[num8 + 1] * num4;
						ptr[num8 + 2] += horizontal_buffer[num8 + 2] * num4;
						ptr[num8 + 3] += horizontal_buffer[num8 + 3] * num4;
					}
					break;
				default:
					for (int j = 0; j < output_w; j++)
					{
						int num9 = j * channels;
						for (int k = 0; k < channels; k++)
						{
							ptr[num9 + k] += horizontal_buffer[num9 + k] * num4;
						}
					}
					break;
				}
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00005B94 File Offset: 0x00003D94
		public unsafe static void stbir__buffer_loop_upsample(StbImageResize.stbir__info stbir_info)
		{
			float num = stbir_info.vertical_scale;
			float num2 = StbImageResize.stbir__filter_info_table[stbir_info.vertical_filter].support(1f / num) * num;
			for (int i = 0; i < stbir_info.output_h; i++)
			{
				float num3 = 0f;
				int j = 0;
				int k = 0;
				StbImageResize.stbir__calculate_sample_range_upsample(i, num2, num, stbir_info.vertical_shift, &j, &k, &num3);
				if (stbir_info.ring_buffer_begin_index >= 0)
				{
					while (j > stbir_info.ring_buffer_first_scanline)
					{
						if (stbir_info.ring_buffer_first_scanline == stbir_info.ring_buffer_last_scanline)
						{
							stbir_info.ring_buffer_begin_index = -1;
							stbir_info.ring_buffer_first_scanline = 0;
							stbir_info.ring_buffer_last_scanline = 0;
							break;
						}
						stbir_info.ring_buffer_first_scanline++;
						stbir_info.ring_buffer_begin_index = (stbir_info.ring_buffer_begin_index + 1) % stbir_info.ring_buffer_num_entries;
					}
				}
				if (stbir_info.ring_buffer_begin_index < 0)
				{
					StbImageResize.stbir__decode_and_resample_upsample(stbir_info, j);
				}
				while (k > stbir_info.ring_buffer_last_scanline)
				{
					StbImageResize.stbir__decode_and_resample_upsample(stbir_info, stbir_info.ring_buffer_last_scanline + 1);
				}
				StbImageResize.stbir__resample_vertical_upsample(stbir_info, i, j, k, num3);
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00005CA0 File Offset: 0x00003EA0
		public unsafe static void stbir__empty_ring_buffer(StbImageResize.stbir__info stbir_info, int first_necessary_scanline)
		{
			int output_stride_bytes = stbir_info.output_stride_bytes;
			int channels = stbir_info.channels;
			int alpha_channel = stbir_info.alpha_channel;
			int type = stbir_info.type;
			int colorspace = stbir_info.colorspace;
			int output_w = stbir_info.output_w;
			void* output_data = stbir_info.output_data;
			int decode = type * 2 + colorspace;
			float* ring_buffer = stbir_info.ring_buffer;
			int ring_buffer_length = stbir_info.ring_buffer_length_bytes / 4;
			if (stbir_info.ring_buffer_begin_index >= 0)
			{
				while (first_necessary_scanline > stbir_info.ring_buffer_first_scanline)
				{
					if (stbir_info.ring_buffer_first_scanline >= 0 && stbir_info.ring_buffer_first_scanline < stbir_info.output_h)
					{
						int num = stbir_info.ring_buffer_first_scanline * output_stride_bytes;
						float* encode_buffer = StbImageResize.stbir__get_ring_buffer_entry(ring_buffer, stbir_info.ring_buffer_begin_index, ring_buffer_length);
						StbImageResize.stbir__encode_scanline(stbir_info, output_w, (void*)((byte*)output_data + num), encode_buffer, channels, alpha_channel, decode);
					}
					if (stbir_info.ring_buffer_first_scanline == stbir_info.ring_buffer_last_scanline)
					{
						stbir_info.ring_buffer_begin_index = -1;
						stbir_info.ring_buffer_first_scanline = 0;
						stbir_info.ring_buffer_last_scanline = 0;
						return;
					}
					stbir_info.ring_buffer_first_scanline++;
					stbir_info.ring_buffer_begin_index = (stbir_info.ring_buffer_begin_index + 1) % stbir_info.ring_buffer_num_entries;
				}
			}
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00005DA0 File Offset: 0x00003FA0
		public unsafe static void stbir__buffer_loop_downsample(StbImageResize.stbir__info stbir_info)
		{
			float num = stbir_info.vertical_scale;
			int output_h = stbir_info.output_h;
			float num2 = StbImageResize.stbir__filter_info_table[stbir_info.vertical_filter].support(num) / num;
			int vertical_filter_pixel_margin = stbir_info.vertical_filter_pixel_margin;
			int num3 = stbir_info.input_h + vertical_filter_pixel_margin;
			for (int i = -vertical_filter_pixel_margin; i < num3; i++)
			{
				int num4;
				int j;
				float num5;
				StbImageResize.stbir__calculate_sample_range_downsample(i, num2, num, stbir_info.vertical_shift, &num4, &j, &num5);
				if (j >= 0 && num4 < output_h)
				{
					StbImageResize.stbir__empty_ring_buffer(stbir_info, num4);
					StbImageResize.stbir__decode_and_resample_downsample(stbir_info, i);
					if (stbir_info.ring_buffer_begin_index < 0)
					{
						StbImageResize.stbir__add_empty_ring_buffer_entry(stbir_info, num4);
					}
					while (j > stbir_info.ring_buffer_last_scanline)
					{
						StbImageResize.stbir__add_empty_ring_buffer_entry(stbir_info, stbir_info.ring_buffer_last_scanline + 1);
					}
					StbImageResize.stbir__resample_vertical_downsample(stbir_info, i, num4, j, num5);
				}
			}
			StbImageResize.stbir__empty_ring_buffer(stbir_info, stbir_info.output_h);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00005E74 File Offset: 0x00004074
		public static void stbir__setup(StbImageResize.stbir__info info, int input_w, int input_h, int output_w, int output_h, int channels)
		{
			info.input_w = input_w;
			info.input_h = input_h;
			info.output_w = output_w;
			info.output_h = output_h;
			info.channels = channels;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00005E9C File Offset: 0x0000409C
		public unsafe static void stbir__calculate_transform(StbImageResize.stbir__info info, float s0, float t0, float s1, float t1, float* transform)
		{
			info.s0 = s0;
			info.t0 = t0;
			info.s1 = s1;
			info.t1 = t1;
			if (transform != null)
			{
				info.horizontal_scale = *transform;
				info.vertical_scale = transform[1];
				info.horizontal_shift = transform[2];
				info.vertical_shift = transform[3];
				return;
			}
			info.horizontal_scale = (float)info.output_w / (float)info.input_w / (s1 - s0);
			info.vertical_scale = (float)info.output_h / (float)info.input_h / (t1 - t0);
			info.horizontal_shift = s0 * (float)info.output_w / (s1 - s0);
			info.vertical_shift = t0 * (float)info.output_h / (t1 - t0);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00005F63 File Offset: 0x00004163
		public static void stbir__choose_filter(StbImageResize.stbir__info info, int h_filter, int v_filter)
		{
			if (h_filter == 0)
			{
				h_filter = ((StbImageResize.stbir__use_upsampling(info.horizontal_scale) != 0) ? 4 : 5);
			}
			if (v_filter == 0)
			{
				v_filter = ((StbImageResize.stbir__use_upsampling(info.vertical_scale) != 0) ? 4 : 5);
			}
			info.horizontal_filter = h_filter;
			info.vertical_filter = v_filter;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00005FA4 File Offset: 0x000041A4
		public static uint stbir__calculate_memory(StbImageResize.stbir__info info)
		{
			int num = StbImageResize.stbir__get_filter_pixel_margin(info.horizontal_filter, info.horizontal_scale);
			int num2 = StbImageResize.stbir__get_filter_pixel_width(info.vertical_filter, info.vertical_scale);
			info.horizontal_num_contributors = StbImageResize.stbir__get_contributors(info.horizontal_scale, info.horizontal_filter, info.input_w, info.output_w);
			info.vertical_num_contributors = StbImageResize.stbir__get_contributors(info.vertical_scale, info.vertical_filter, info.input_h, info.output_h);
			info.ring_buffer_num_entries = num2 + 1;
			info.horizontal_contributors_size = info.horizontal_num_contributors * sizeof(StbImageResize.stbir__contributors);
			info.horizontal_coefficients_size = StbImageResize.stbir__get_total_horizontal_coefficients(info) * 4;
			info.vertical_contributors_size = info.vertical_num_contributors * sizeof(StbImageResize.stbir__contributors);
			info.vertical_coefficients_size = StbImageResize.stbir__get_total_vertical_coefficients(info) * 4;
			info.decode_buffer_size = (info.input_w + num * 2) * info.channels * 4;
			info.horizontal_buffer_size = info.output_w * info.channels * 4;
			info.ring_buffer_size = info.output_w * info.channels * info.ring_buffer_num_entries * 4;
			info.encode_buffer_size = info.output_w * info.channels * 4;
			if (StbImageResize.stbir__use_height_upsampling(info) != 0)
			{
				info.horizontal_buffer_size = 0;
			}
			else
			{
				info.encode_buffer_size = 0;
			}
			return (uint)(info.horizontal_contributors_size + info.horizontal_coefficients_size + info.vertical_contributors_size + info.vertical_coefficients_size + info.decode_buffer_size + info.horizontal_buffer_size + info.ring_buffer_size + info.encode_buffer_size);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00006118 File Offset: 0x00004318
		public unsafe static int stbir__resize_allocated(StbImageResize.stbir__info info, void* input_data, int input_stride_in_bytes, void* output_data, int output_stride_in_bytes, int alpha_channel, uint flags, int type, int edge_horizontal, int edge_vertical, int colorspace, void* tempmem, ulong tempmem_size_in_bytes)
		{
			ulong num = (ulong)StbImageResize.stbir__calculate_memory(info);
			int input_stride_bytes = ((input_stride_in_bytes != 0) ? input_stride_in_bytes : (info.channels * info.input_w * (int)StbImageResize.stbir__type_size[type]));
			int output_stride_bytes = ((output_stride_in_bytes != 0) ? output_stride_in_bytes : (info.channels * info.output_w * (int)StbImageResize.stbir__type_size[type]));
			if (info.channels < 0 || info.channels > 64)
			{
				return 0;
			}
			if (info.horizontal_filter >= 6)
			{
				return 0;
			}
			if (info.vertical_filter >= 6)
			{
				return 0;
			}
			if (alpha_channel < 0)
			{
				flags |= 3U;
			}
			if ((flags & 2U) != 0U)
			{
				uint num2 = flags & 1U;
			}
			if (alpha_channel >= info.channels)
			{
				return 0;
			}
			if (tempmem == null)
			{
				return 0;
			}
			if (tempmem_size_in_bytes < num)
			{
				return 0;
			}
			CRuntime.memset(tempmem, 0, tempmem_size_in_bytes);
			info.input_data = input_data;
			info.input_stride_bytes = input_stride_bytes;
			info.output_data = output_data;
			info.output_stride_bytes = output_stride_bytes;
			info.alpha_channel = alpha_channel;
			info.flags = flags;
			info.type = type;
			info.edge_horizontal = edge_horizontal;
			info.edge_vertical = edge_vertical;
			info.colorspace = colorspace;
			info.horizontal_coefficient_width = StbImageResize.stbir__get_coefficient_width(info.horizontal_filter, info.horizontal_scale);
			info.vertical_coefficient_width = StbImageResize.stbir__get_coefficient_width(info.vertical_filter, info.vertical_scale);
			info.horizontal_filter_pixel_width = StbImageResize.stbir__get_filter_pixel_width(info.horizontal_filter, info.horizontal_scale);
			info.vertical_filter_pixel_width = StbImageResize.stbir__get_filter_pixel_width(info.vertical_filter, info.vertical_scale);
			info.horizontal_filter_pixel_margin = StbImageResize.stbir__get_filter_pixel_margin(info.horizontal_filter, info.horizontal_scale);
			info.vertical_filter_pixel_margin = StbImageResize.stbir__get_filter_pixel_margin(info.vertical_filter, info.vertical_scale);
			info.ring_buffer_length_bytes = info.output_w * info.channels * 4;
			info.decode_buffer_pixels = info.input_w + info.horizontal_filter_pixel_margin * 2;
			info.horizontal_contributors = (StbImageResize.stbir__contributors*)tempmem;
			info.horizontal_coefficients = (float*)(info.horizontal_contributors + info.horizontal_contributors_size / sizeof(StbImageResize.stbir__contributors));
			info.vertical_contributors = (StbImageResize.stbir__contributors*)(info.horizontal_coefficients + info.horizontal_coefficients_size / 4);
			info.vertical_coefficients = (float*)(info.vertical_contributors + info.vertical_contributors_size / sizeof(StbImageResize.stbir__contributors));
			info.decode_buffer = info.vertical_coefficients + info.vertical_coefficients_size / 4;
			if (StbImageResize.stbir__use_height_upsampling(info) != 0)
			{
				info.horizontal_buffer = null;
				info.ring_buffer = info.decode_buffer + info.decode_buffer_size / 4;
				info.encode_buffer = info.ring_buffer + info.ring_buffer_size / 4;
			}
			else
			{
				info.horizontal_buffer = info.decode_buffer + info.decode_buffer_size / 4;
				info.ring_buffer = info.horizontal_buffer + info.horizontal_buffer_size / 4;
				info.encode_buffer = null;
			}
			info.ring_buffer_begin_index = -1;
			StbImageResize.stbir__calculate_filters(info, info.horizontal_contributors, info.horizontal_coefficients, info.horizontal_filter, info.horizontal_scale, info.horizontal_shift, info.input_w, info.output_w);
			StbImageResize.stbir__calculate_filters(info, info.vertical_contributors, info.vertical_coefficients, info.vertical_filter, info.vertical_scale, info.vertical_shift, info.input_h, info.output_h);
			if (StbImageResize.stbir__use_height_upsampling(info) != 0)
			{
				StbImageResize.stbir__buffer_loop_upsample(info);
			}
			else
			{
				StbImageResize.stbir__buffer_loop_downsample(info);
			}
			return 1;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00006414 File Offset: 0x00004614
		public unsafe static int stbir__resize_arbitrary(void* alloc_context, void* input_data, int input_w, int input_h, int input_stride_in_bytes, void* output_data, int output_w, int output_h, int output_stride_in_bytes, float s0, float t0, float s1, float t1, float* transform, int channels, int alpha_channel, uint flags, int type, int h_filter, int v_filter, int edge_horizontal, int edge_vertical, int colorspace)
		{
			StbImageResize.stbir__info info = new StbImageResize.stbir__info();
			StbImageResize.stbir__setup(info, input_w, input_h, output_w, output_h, channels);
			StbImageResize.stbir__calculate_transform(info, s0, t0, s1, t1, transform);
			StbImageResize.stbir__choose_filter(info, h_filter, v_filter);
			ulong num = (ulong)StbImageResize.stbir__calculate_memory(info);
			void* ptr = CRuntime.malloc(num);
			if (ptr == null)
			{
				return 0;
			}
			int result = StbImageResize.stbir__resize_allocated(info, input_data, input_stride_in_bytes, output_data, output_stride_in_bytes, alpha_channel, flags, type, edge_horizontal, edge_vertical, colorspace, ptr, num);
			CRuntime.free(ptr);
			return result;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x0000648C File Offset: 0x0000468C
		public unsafe static int stbir_resize_uint8(byte* input_pixels, int input_w, int input_h, int input_stride_in_bytes, byte* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels)
		{
			return StbImageResize.stbir__resize_arbitrary(null, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, -1, 0U, 0, 0, 0, 1, 1, 0);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x000064CC File Offset: 0x000046CC
		public unsafe static int stbir_resize_float(float* input_pixels, int input_w, int input_h, int input_stride_in_bytes, float* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels)
		{
			return StbImageResize.stbir__resize_arbitrary(null, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, -1, 0U, 3, 0, 0, 1, 1, 0);
		}

		// Token: 0x0600006A RID: 106 RVA: 0x0000650C File Offset: 0x0000470C
		public unsafe static int stbir_resize_uint8_srgb(byte* input_pixels, int input_w, int input_h, int input_stride_in_bytes, byte* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels, int alpha_channel, int flags)
		{
			return StbImageResize.stbir__resize_arbitrary(null, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, alpha_channel, (uint)flags, 0, 0, 0, 1, 1, 1);
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00006550 File Offset: 0x00004750
		public unsafe static int stbir_resize_uint8_srgb_edgemode(byte* input_pixels, int input_w, int input_h, int input_stride_in_bytes, byte* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels, int alpha_channel, int flags, int edge_wrap_mode)
		{
			return StbImageResize.stbir__resize_arbitrary(null, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, alpha_channel, (uint)flags, 0, 0, 0, edge_wrap_mode, edge_wrap_mode, 1);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00006594 File Offset: 0x00004794
		public unsafe static int stbir_resize_uint8_generic(byte* input_pixels, int input_w, int input_h, int input_stride_in_bytes, byte* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels, int alpha_channel, int flags, int edge_wrap_mode, int filter, int space, void* alloc_context)
		{
			return StbImageResize.stbir__resize_arbitrary(alloc_context, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, alpha_channel, (uint)flags, 0, filter, filter, edge_wrap_mode, edge_wrap_mode, space);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x000065DC File Offset: 0x000047DC
		public unsafe static int stbir_resize_uint16_generic(ushort* input_pixels, int input_w, int input_h, int input_stride_in_bytes, ushort* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels, int alpha_channel, int flags, int edge_wrap_mode, int filter, int space, void* alloc_context)
		{
			return StbImageResize.stbir__resize_arbitrary(alloc_context, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, alpha_channel, (uint)flags, 1, filter, filter, edge_wrap_mode, edge_wrap_mode, space);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00006624 File Offset: 0x00004824
		public unsafe static int stbir_resize_float_generic(float* input_pixels, int input_w, int input_h, int input_stride_in_bytes, float* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels, int alpha_channel, int flags, int edge_wrap_mode, int filter, int space, void* alloc_context)
		{
			return StbImageResize.stbir__resize_arbitrary(alloc_context, (void*)input_pixels, input_w, input_h, input_stride_in_bytes, (void*)output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, alpha_channel, (uint)flags, 3, filter, filter, edge_wrap_mode, edge_wrap_mode, space);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x0000666C File Offset: 0x0000486C
		public unsafe static int stbir_resize(void* input_pixels, int input_w, int input_h, int input_stride_in_bytes, void* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int datatype, int num_channels, int alpha_channel, int flags, int edge_mode_horizontal, int edge_mode_vertical, int filter_horizontal, int filter_vertical, int space, void* alloc_context)
		{
			return StbImageResize.stbir__resize_arbitrary(alloc_context, input_pixels, input_w, input_h, input_stride_in_bytes, output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, null, num_channels, alpha_channel, (uint)flags, datatype, filter_horizontal, filter_vertical, edge_mode_horizontal, edge_mode_vertical, space);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000066B4 File Offset: 0x000048B4
		public unsafe static int stbir_resize_subpixel(void* input_pixels, int input_w, int input_h, int input_stride_in_bytes, void* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int datatype, int num_channels, int alpha_channel, int flags, int edge_mode_horizontal, int edge_mode_vertical, int filter_horizontal, int filter_vertical, int space, void* alloc_context, float x_scale, float y_scale, float x_offset, float y_offset)
		{
			float* ptr = stackalloc float[(UIntPtr)16];
			*ptr = x_scale;
			ptr[1] = y_scale;
			ptr[2] = x_offset;
			ptr[3] = y_offset;
			return StbImageResize.stbir__resize_arbitrary(alloc_context, input_pixels, input_w, input_h, input_stride_in_bytes, output_pixels, output_w, output_h, output_stride_in_bytes, 0f, 0f, 1f, 1f, ptr, num_channels, alpha_channel, (uint)flags, datatype, filter_horizontal, filter_vertical, edge_mode_horizontal, edge_mode_vertical, space);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00006724 File Offset: 0x00004924
		public unsafe static int stbir_resize_region(void* input_pixels, int input_w, int input_h, int input_stride_in_bytes, void* output_pixels, int output_w, int output_h, int output_stride_in_bytes, int datatype, int num_channels, int alpha_channel, int flags, int edge_mode_horizontal, int edge_mode_vertical, int filter_horizontal, int filter_vertical, int space, void* alloc_context, float s0, float t0, float s1, float t1)
		{
			return StbImageResize.stbir__resize_arbitrary(alloc_context, input_pixels, input_w, input_h, input_stride_in_bytes, output_pixels, output_w, output_h, output_stride_in_bytes, s0, t0, s1, t1, null, num_channels, alpha_channel, (uint)flags, datatype, filter_horizontal, filter_vertical, edge_mode_horizontal, edge_mode_vertical, space);
		}

		// Token: 0x0400001E RID: 30
		public static StbImageResize.stbir__filter_info[] stbir__filter_info_table = new StbImageResize.stbir__filter_info[]
		{
			new StbImageResize.stbir__filter_info(null, new StbImageResize.stbir__support_fn(StbImageResize.stbir__support_zero)),
			new StbImageResize.stbir__filter_info(new StbImageResize.stbir__kernel_fn(StbImageResize.stbir__filter_trapezoid), new StbImageResize.stbir__support_fn(StbImageResize.stbir__support_trapezoid)),
			new StbImageResize.stbir__filter_info(new StbImageResize.stbir__kernel_fn(StbImageResize.stbir__filter_triangle), new StbImageResize.stbir__support_fn(StbImageResize.stbir__support_one)),
			new StbImageResize.stbir__filter_info(new StbImageResize.stbir__kernel_fn(StbImageResize.stbir__filter_cubic), new StbImageResize.stbir__support_fn(StbImageResize.stbir__support_two)),
			new StbImageResize.stbir__filter_info(new StbImageResize.stbir__kernel_fn(StbImageResize.stbir__filter_catmullrom), new StbImageResize.stbir__support_fn(StbImageResize.stbir__support_two)),
			new StbImageResize.stbir__filter_info(new StbImageResize.stbir__kernel_fn(StbImageResize.stbir__filter_mitchell), new StbImageResize.stbir__support_fn(StbImageResize.stbir__support_two))
		};

		// Token: 0x0400001F RID: 31
		public const int STBIR_EDGE_CLAMP = 1;

		// Token: 0x04000020 RID: 32
		public const int STBIR_EDGE_REFLECT = 2;

		// Token: 0x04000021 RID: 33
		public const int STBIR_EDGE_WRAP = 3;

		// Token: 0x04000022 RID: 34
		public const int STBIR_EDGE_ZERO = 4;

		// Token: 0x04000023 RID: 35
		public const int STBIR_FILTER_DEFAULT = 0;

		// Token: 0x04000024 RID: 36
		public const int STBIR_FILTER_BOX = 1;

		// Token: 0x04000025 RID: 37
		public const int STBIR_FILTER_TRIANGLE = 2;

		// Token: 0x04000026 RID: 38
		public const int STBIR_FILTER_CUBICBSPLINE = 3;

		// Token: 0x04000027 RID: 39
		public const int STBIR_FILTER_CATMULLROM = 4;

		// Token: 0x04000028 RID: 40
		public const int STBIR_FILTER_MITCHELL = 5;

		// Token: 0x04000029 RID: 41
		public const int STBIR_COLORSPACE_LINEAR = 0;

		// Token: 0x0400002A RID: 42
		public const int STBIR_COLORSPACE_SRGB = 1;

		// Token: 0x0400002B RID: 43
		public const int STBIR_MAX_COLORSPACES = 2;

		// Token: 0x0400002C RID: 44
		public const int STBIR_TYPE_UINT8 = 0;

		// Token: 0x0400002D RID: 45
		public const int STBIR_TYPE_UINT16 = 1;

		// Token: 0x0400002E RID: 46
		public const int STBIR_TYPE_UINT32 = 2;

		// Token: 0x0400002F RID: 47
		public const int STBIR_TYPE_FLOAT = 3;

		// Token: 0x04000030 RID: 48
		public const int STBIR_MAX_TYPES = 4;

		// Token: 0x04000031 RID: 49
		public static byte[] stbir__type_size = new byte[] { 1, 2, 4, 4 };

		// Token: 0x04000032 RID: 50
		public static float[] stbir__srgb_uchar_to_linear_float = new float[]
		{
			0f, 0.000304f, 0.000607f, 0.000911f, 0.001214f, 0.001518f, 0.001821f, 0.002125f, 0.002428f, 0.002732f,
			0.003035f, 0.003347f, 0.003677f, 0.004025f, 0.004391f, 0.004777f, 0.005182f, 0.005605f, 0.006049f, 0.006512f,
			0.006995f, 0.007499f, 0.008023f, 0.008568f, 0.009134f, 0.009721f, 0.01033f, 0.01096f, 0.011612f, 0.012286f,
			0.012983f, 0.013702f, 0.014444f, 0.015209f, 0.015996f, 0.016807f, 0.017642f, 0.0185f, 0.019382f, 0.020289f,
			0.021219f, 0.022174f, 0.023153f, 0.024158f, 0.025187f, 0.026241f, 0.027321f, 0.028426f, 0.029557f, 0.030713f,
			0.031896f, 0.033105f, 0.03434f, 0.035601f, 0.036889f, 0.038204f, 0.039546f, 0.040915f, 0.042311f, 0.043735f,
			0.045186f, 0.046665f, 0.048172f, 0.049707f, 0.051269f, 0.052861f, 0.05448f, 0.056128f, 0.057805f, 0.059511f,
			0.061246f, 0.06301f, 0.064803f, 0.066626f, 0.068478f, 0.07036f, 0.072272f, 0.074214f, 0.076185f, 0.078187f,
			0.08022f, 0.082283f, 0.084376f, 0.0865f, 0.088656f, 0.090842f, 0.093059f, 0.095307f, 0.097587f, 0.099899f,
			0.102242f, 0.104616f, 0.107023f, 0.109462f, 0.111932f, 0.114435f, 0.116971f, 0.119538f, 0.122139f, 0.124772f,
			0.127438f, 0.130136f, 0.132868f, 0.135633f, 0.138432f, 0.141263f, 0.144128f, 0.147027f, 0.14996f, 0.152926f,
			0.155926f, 0.158961f, 0.162029f, 0.165132f, 0.168269f, 0.171441f, 0.174647f, 0.177888f, 0.181164f, 0.184475f,
			0.187821f, 0.191202f, 0.194618f, 0.198069f, 0.201556f, 0.205079f, 0.208637f, 0.212231f, 0.215861f, 0.219526f,
			0.223228f, 0.226966f, 0.23074f, 0.234551f, 0.238398f, 0.242281f, 0.246201f, 0.250158f, 0.254152f, 0.258183f,
			0.262251f, 0.266356f, 0.270498f, 0.274677f, 0.278894f, 0.283149f, 0.287441f, 0.291771f, 0.296138f, 0.300544f,
			0.304987f, 0.309469f, 0.313989f, 0.318547f, 0.323143f, 0.327778f, 0.332452f, 0.337164f, 0.341914f, 0.346704f,
			0.351533f, 0.3564f, 0.361307f, 0.366253f, 0.371238f, 0.376262f, 0.381326f, 0.38643f, 0.391573f, 0.396755f,
			0.401978f, 0.40724f, 0.412543f, 0.417885f, 0.423268f, 0.428691f, 0.434154f, 0.439657f, 0.445201f, 0.450786f,
			0.456411f, 0.462077f, 0.467784f, 0.473532f, 0.47932f, 0.48515f, 0.491021f, 0.496933f, 0.502887f, 0.508881f,
			0.514918f, 0.520996f, 0.527115f, 0.533276f, 0.53948f, 0.545725f, 0.552011f, 0.55834f, 0.564712f, 0.571125f,
			0.577581f, 0.584078f, 0.590619f, 0.597202f, 0.603827f, 0.610496f, 0.617207f, 0.62396f, 0.630757f, 0.637597f,
			0.64448f, 0.651406f, 0.658375f, 0.665387f, 0.672443f, 0.679543f, 0.686685f, 0.693872f, 0.701102f, 0.708376f,
			0.715694f, 0.723055f, 0.730461f, 0.737911f, 0.745404f, 0.752942f, 0.760525f, 0.768151f, 0.775822f, 0.783538f,
			0.791298f, 0.799103f, 0.806952f, 0.814847f, 0.822786f, 0.83077f, 0.838799f, 0.846873f, 0.854993f, 0.863157f,
			0.871367f, 0.879622f, 0.887923f, 0.896269f, 0.904661f, 0.913099f, 0.921582f, 0.930111f, 0.938686f, 0.947307f,
			0.955974f, 0.964686f, 0.973445f, 0.982251f, 0.991102f, 1f
		};

		// Token: 0x04000033 RID: 51
		public static uint[] fp32_to_srgb8_tab4 = new uint[]
		{
			7536653U, 7995405U, 8388621U, 8847373U, 9240589U, 9699341U, 10092557U, 10551309U, 10944538U, 11796506U,
			12648474U, 13500442U, 14286874U, 15138842U, 15990810U, 16842778U, 17694771U, 19398707U, 21037107U, 22741043U,
			24444979U, 26148915U, 27787315U, 29491251U, 31195239U, 34537575U, 37945447U, 41287783U, 44695655U, 48037991U,
			51445863U, 54788199U, 58196174U, 64946382U, 71696590U, 78446798U, 85197006U, 91947205U, 98369724U, 104530101U,
			110559576U, 121766210U, 132317488U, 142278944U, 151716114U, 160694534U, 169279740U, 177537266U, 185532875U, 200540590U,
			214630805U, 227869056U, 240517486U, 252510558U, 263979344U, 274923843U, 285672036U, 305660478U, 324469277U, 342229505U,
			359006697U, 374997459U, 390332864U, 405012911U, 419300145U, 446038782U, 471139026U, 494797485U, 517210765U, 538575472U,
			559022678U, 578617920U, 597623875U, 633340926U, 666829764U, 698418066U, 728367975U, 756876097U, 784204575U, 810353408U,
			835782064U, 883426645U, 928122119U, 970261701U, 1010238603U, 1048314968U, 1084752938U, 1119683585U, 1153566616U, 1217267486U,
			1276905142U, 1333134941U, 1386546704U, 1437337036U, 1485964687U, 1532560729U, 1577847331U, 1662781824U, 1742407926U, 1817512063U,
			1888749592U, 1956644797U, 2021459820U, 2083718947U
		};

		// Token: 0x02000010 RID: 16
		// (Invoke) Token: 0x0600023E RID: 574
		public delegate float stbir__kernel_fn(float x, float scale);

		// Token: 0x02000011 RID: 17
		// (Invoke) Token: 0x06000242 RID: 578
		public delegate float stbir__support_fn(float scale);

		// Token: 0x02000012 RID: 18
		public class stbir__filter_info
		{
			// Token: 0x06000245 RID: 581 RVA: 0x000207A9 File Offset: 0x0001E9A9
			public stbir__filter_info(StbImageResize.stbir__kernel_fn k, StbImageResize.stbir__support_fn s)
			{
				this.kernel = k;
				this.support = s;
			}

			// Token: 0x040000F6 RID: 246
			public StbImageResize.stbir__kernel_fn kernel;

			// Token: 0x040000F7 RID: 247
			public StbImageResize.stbir__support_fn support;
		}

		// Token: 0x02000013 RID: 19
		public class stbir__info
		{
			// Token: 0x040000F8 RID: 248
			public unsafe void* input_data;

			// Token: 0x040000F9 RID: 249
			public int input_w;

			// Token: 0x040000FA RID: 250
			public int input_h;

			// Token: 0x040000FB RID: 251
			public int input_stride_bytes;

			// Token: 0x040000FC RID: 252
			public unsafe void* output_data;

			// Token: 0x040000FD RID: 253
			public int output_w;

			// Token: 0x040000FE RID: 254
			public int output_h;

			// Token: 0x040000FF RID: 255
			public int output_stride_bytes;

			// Token: 0x04000100 RID: 256
			public float s0;

			// Token: 0x04000101 RID: 257
			public float t0;

			// Token: 0x04000102 RID: 258
			public float s1;

			// Token: 0x04000103 RID: 259
			public float t1;

			// Token: 0x04000104 RID: 260
			public float horizontal_shift;

			// Token: 0x04000105 RID: 261
			public float vertical_shift;

			// Token: 0x04000106 RID: 262
			public float horizontal_scale;

			// Token: 0x04000107 RID: 263
			public float vertical_scale;

			// Token: 0x04000108 RID: 264
			public int channels;

			// Token: 0x04000109 RID: 265
			public int alpha_channel;

			// Token: 0x0400010A RID: 266
			public uint flags;

			// Token: 0x0400010B RID: 267
			public int type;

			// Token: 0x0400010C RID: 268
			public int horizontal_filter;

			// Token: 0x0400010D RID: 269
			public int vertical_filter;

			// Token: 0x0400010E RID: 270
			public int edge_horizontal;

			// Token: 0x0400010F RID: 271
			public int edge_vertical;

			// Token: 0x04000110 RID: 272
			public int colorspace;

			// Token: 0x04000111 RID: 273
			public unsafe StbImageResize.stbir__contributors* horizontal_contributors;

			// Token: 0x04000112 RID: 274
			public unsafe float* horizontal_coefficients;

			// Token: 0x04000113 RID: 275
			public unsafe StbImageResize.stbir__contributors* vertical_contributors;

			// Token: 0x04000114 RID: 276
			public unsafe float* vertical_coefficients;

			// Token: 0x04000115 RID: 277
			public int decode_buffer_pixels;

			// Token: 0x04000116 RID: 278
			public unsafe float* decode_buffer;

			// Token: 0x04000117 RID: 279
			public unsafe float* horizontal_buffer;

			// Token: 0x04000118 RID: 280
			public int horizontal_coefficient_width;

			// Token: 0x04000119 RID: 281
			public int vertical_coefficient_width;

			// Token: 0x0400011A RID: 282
			public int horizontal_filter_pixel_width;

			// Token: 0x0400011B RID: 283
			public int vertical_filter_pixel_width;

			// Token: 0x0400011C RID: 284
			public int horizontal_filter_pixel_margin;

			// Token: 0x0400011D RID: 285
			public int vertical_filter_pixel_margin;

			// Token: 0x0400011E RID: 286
			public int horizontal_num_contributors;

			// Token: 0x0400011F RID: 287
			public int vertical_num_contributors;

			// Token: 0x04000120 RID: 288
			public int ring_buffer_length_bytes;

			// Token: 0x04000121 RID: 289
			public int ring_buffer_num_entries;

			// Token: 0x04000122 RID: 290
			public int ring_buffer_first_scanline;

			// Token: 0x04000123 RID: 291
			public int ring_buffer_last_scanline;

			// Token: 0x04000124 RID: 292
			public int ring_buffer_begin_index;

			// Token: 0x04000125 RID: 293
			public unsafe float* ring_buffer;

			// Token: 0x04000126 RID: 294
			public unsafe float* encode_buffer;

			// Token: 0x04000127 RID: 295
			public int horizontal_contributors_size;

			// Token: 0x04000128 RID: 296
			public int horizontal_coefficients_size;

			// Token: 0x04000129 RID: 297
			public int vertical_contributors_size;

			// Token: 0x0400012A RID: 298
			public int vertical_coefficients_size;

			// Token: 0x0400012B RID: 299
			public int decode_buffer_size;

			// Token: 0x0400012C RID: 300
			public int horizontal_buffer_size;

			// Token: 0x0400012D RID: 301
			public int ring_buffer_size;

			// Token: 0x0400012E RID: 302
			public int encode_buffer_size;
		}

		// Token: 0x02000014 RID: 20
		[StructLayout(LayoutKind.Explicit)]
		public struct stbir__FP32
		{
			// Token: 0x0400012F RID: 303
			[FieldOffset(0)]
			public uint u;

			// Token: 0x04000130 RID: 304
			[FieldOffset(0)]
			public float f;
		}

		// Token: 0x02000015 RID: 21
		public struct stbir__contributors
		{
			// Token: 0x04000131 RID: 305
			public int n0;

			// Token: 0x04000132 RID: 306
			public int n1;
		}
	}
}
