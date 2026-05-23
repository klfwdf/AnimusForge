using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x020000B2 RID: 178
	public class MBPerlin
	{
		// Token: 0x0600094A RID: 2378 RVA: 0x0001E4CC File Offset: 0x0001C6CC
		static MBPerlin()
		{
			for (int i = 0; i < 512; i++)
			{
				MBPerlin._doubledPermutation[i] = MBPerlin._permutation[i % 256];
			}
		}

		// Token: 0x0600094B RID: 2379 RVA: 0x0001E528 File Offset: 0x0001C728
		public static float Noise(float x, float y, float z)
		{
			int num = MBPerlin.FastFloor(x) & 255;
			int num2 = MBPerlin.FastFloor(y) & 255;
			int num3 = MBPerlin.FastFloor(z) & 255;
			x -= (float)MBPerlin.FastFloor(x);
			y -= (float)MBPerlin.FastFloor(y);
			z -= (float)MBPerlin.FastFloor(z);
			float amount = MBPerlin.Fade(x);
			float amount2 = MBPerlin.Fade(y);
			float amount3 = MBPerlin.Fade(z);
			int num4 = MBPerlin._doubledPermutation[num] + num2;
			int num5 = MBPerlin._doubledPermutation[num4] + num3;
			int num6 = MBPerlin._doubledPermutation[num4 + 1] + num3;
			int num7 = MBPerlin._doubledPermutation[num + 1] + num2;
			int num8 = MBPerlin._doubledPermutation[num7] + num3;
			int num9 = MBPerlin._doubledPermutation[num7 + 1] + num3;
			return MBMath.Lerp(MBMath.Lerp(MBMath.Lerp(MBPerlin.Grad(MBPerlin._doubledPermutation[num5], x, y, z), MBPerlin.Grad(MBPerlin._doubledPermutation[num8], x - 1f, y, z), amount, 1E-05f), MBMath.Lerp(MBPerlin.Grad(MBPerlin._doubledPermutation[num6], x, y - 1f, z), MBPerlin.Grad(MBPerlin._doubledPermutation[num9], x - 1f, y - 1f, z), amount, 1E-05f), amount2, 1E-05f), MBMath.Lerp(MBMath.Lerp(MBPerlin.Grad(MBPerlin._doubledPermutation[num5 + 1], x, y, z - 1f), MBPerlin.Grad(MBPerlin._doubledPermutation[num8 + 1], x - 1f, y, z - 1f), amount, 1E-05f), MBMath.Lerp(MBPerlin.Grad(MBPerlin._doubledPermutation[num6 + 1], x, y - 1f, z - 1f), MBPerlin.Grad(MBPerlin._doubledPermutation[num9 + 1], x - 1f, y - 1f, z - 1f), amount, 1E-05f), amount2, 1E-05f), amount3, 1E-05f);
		}

		// Token: 0x0600094C RID: 2380 RVA: 0x0001E700 File Offset: 0x0001C900
		public static Vec3 NoiseVec3(float t)
		{
			float x = MBPerlin.Noise(t + 31.42f, t - 12.98f, t + 84.73f);
			float y = MBPerlin.Noise(t, t, t);
			float z = MBPerlin.Noise(t - 47.11f, t + 5.29f, t + 19.53f);
			return new Vec3(x, y, z, -1f);
		}

		// Token: 0x0600094D RID: 2381 RVA: 0x0001E758 File Offset: 0x0001C958
		public static Vec3 NoiseVec3(float x, float y, float z)
		{
			float x2 = MBPerlin.Noise(x, y, z);
			float y2 = MBPerlin.Noise(x + 31.42f, y - 12.98f, z + 84.73f);
			float z2 = MBPerlin.Noise(x - 47.11f, y + 5.29f, z + 19.53f);
			return new Vec3(x2, y2, z2, -1f);
		}

		// Token: 0x0600094E RID: 2382 RVA: 0x0001E7AF File Offset: 0x0001C9AF
		private static int FastFloor(float f)
		{
			if (f < 0f)
			{
				return (int)f - 1;
			}
			return (int)f;
		}

		// Token: 0x0600094F RID: 2383 RVA: 0x0001E7C0 File Offset: 0x0001C9C0
		private static float Fade(float t)
		{
			return MathF.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
		}

		// Token: 0x06000950 RID: 2384 RVA: 0x0001E7EC File Offset: 0x0001C9EC
		private static float Grad(int hash, float x, float y, float z)
		{
			int num = hash & 15;
			float num2 = ((num < 8) ? x : y);
			float num3 = ((num < 4) ? y : ((num == 12 || num == 14) ? x : z));
			return (((num & 1) == 0) ? num2 : (-num2)) + (((num & 2) == 0) ? num3 : (-num3));
		}

		// Token: 0x0400052C RID: 1324
		private static readonly int[] _permutation = new int[]
		{
			151, 160, 137, 91, 90, 15, 131, 13, 201, 95,
			96, 53, 194, 233, 7, 225, 140, 36, 103, 30,
			69, 142, 8, 99, 37, 240, 21, 10, 23, 190,
			6, 148, 247, 120, 234, 75, 0, 26, 197, 62,
			94, 252, 219, 203, 117, 35, 11, 32, 57, 177,
			33, 88, 237, 149, 56, 87, 174, 20, 125, 136,
			171, 168, 68, 175, 74, 165, 71, 134, 139, 48,
			27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
			60, 211, 133, 230, 220, 105, 92, 41, 55, 46,
			245, 40, 244, 102, 143, 54, 65, 25, 63, 161,
			1, 216, 80, 73, 209, 76, 132, 187, 208, 89,
			18, 169, 200, 196, 135, 130, 116, 188, 159, 86,
			164, 100, 109, 198, 173, 186, 3, 64, 52, 217,
			226, 250, 124, 123, 5, 202, 38, 147, 118, 126,
			255, 82, 85, 212, 207, 206, 59, 227, 47, 16,
			58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
			119, 248, 152, 2, 44, 154, 163, 70, 221, 153,
			101, 155, 167, 43, 172, 9, 129, 22, 39, 253,
			19, 98, 108, 110, 79, 113, 224, 232, 178, 185,
			112, 104, 218, 246, 97, 228, 251, 34, 242, 193,
			238, 210, 144, 12, 191, 179, 162, 241, 81, 51,
			145, 235, 249, 14, 239, 107, 49, 192, 214, 31,
			181, 199, 106, 157, 184, 84, 204, 176, 115, 121,
			50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
			222, 114, 67, 29, 24, 72, 243, 141, 128, 195,
			78, 66, 215, 61, 156, 180
		};

		// Token: 0x0400052D RID: 1325
		private static readonly int[] _doubledPermutation = new int[512];
	}
}
