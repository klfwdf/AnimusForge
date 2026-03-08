using System;
using System.Globalization;
using System.Numerics;

namespace TaleWorlds.Library
{
	// Token: 0x02000020 RID: 32
	public struct Color
	{
		// Token: 0x060000AC RID: 172 RVA: 0x00003EFB File Offset: 0x000020FB
		public Color(float red, float green, float blue, float alpha = 1f)
		{
			this.Red = red;
			this.Green = green;
			this.Blue = blue;
			this.Alpha = alpha;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00003F1A File Offset: 0x0000211A
		public Vector3 ToVector3()
		{
			return new Vector3(this.Red, this.Green, this.Blue);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00003F33 File Offset: 0x00002133
		public Vec3 ToVec3()
		{
			return new Vec3(this.Red, this.Green, this.Blue, this.Alpha);
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00003F54 File Offset: 0x00002154
		public static Color operator *(Color c, float f)
		{
			float red = c.Red * f;
			float green = c.Green * f;
			float blue = c.Blue * f;
			float alpha = c.Alpha * f;
			return new Color(red, green, blue, alpha);
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00003F8C File Offset: 0x0000218C
		public static Color operator *(Color c1, Color c2)
		{
			return new Color(c1.Red * c2.Red, c1.Green * c2.Green, c1.Blue * c2.Blue, c1.Alpha * c2.Alpha);
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00003FC7 File Offset: 0x000021C7
		public static Color operator +(Color c1, Color c2)
		{
			return new Color(c1.Red + c2.Red, c1.Green + c2.Green, c1.Blue + c2.Blue, c1.Alpha + c2.Alpha);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00004002 File Offset: 0x00002202
		public static Color operator -(Color c1, Color c2)
		{
			return new Color(c1.Red - c2.Red, c1.Green - c2.Green, c1.Blue - c2.Blue, c1.Alpha - c2.Alpha);
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060000B3 RID: 179 RVA: 0x0000403D File Offset: 0x0000223D
		public static Color Black
		{
			get
			{
				return new Color(0f, 0f, 0f, 1f);
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x060000B4 RID: 180 RVA: 0x00004058 File Offset: 0x00002258
		public static Color White
		{
			get
			{
				return new Color(1f, 1f, 1f, 1f);
			}
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00004073 File Offset: 0x00002273
		public static bool operator ==(Color a, Color b)
		{
			return a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue && a.Alpha == b.Alpha;
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x000040AF File Offset: 0x000022AF
		public static bool operator !=(Color a, Color b)
		{
			return !(a == b);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x000040BB File Offset: 0x000022BB
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x000040D0 File Offset: 0x000022D0
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (!(obj is Color))
			{
				return false;
			}
			Color b = (Color)obj;
			return this == b;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x000040FF File Offset: 0x000022FF
		public static Color FromVector3(Vector3 vector3)
		{
			return new Color(vector3.X, vector3.Y, vector3.Z, 1f);
		}

		// Token: 0x060000BA RID: 186 RVA: 0x0000411D File Offset: 0x0000231D
		public static Color FromVector3(Vec3 vector3)
		{
			return new Color(vector3.x, vector3.y, vector3.z, 1f);
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000413B File Offset: 0x0000233B
		public float Length()
		{
			return MathF.Sqrt(this.Red * this.Red + this.Green * this.Green + this.Blue * this.Blue + this.Alpha * this.Alpha);
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000417C File Offset: 0x0000237C
		public uint ToUnsignedInteger()
		{
			byte b = (byte)(this.Red * 255f);
			byte b2 = (byte)(this.Green * 255f);
			byte b3 = (byte)(this.Blue * 255f);
			return (uint)(((int)((byte)(this.Alpha * 255f)) << 24) + ((int)b << 16) + ((int)b2 << 8) + (int)b3);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x000041D0 File Offset: 0x000023D0
		public static Color FromUint(uint color)
		{
			float num = (float)((byte)(color >> 24));
			byte b = (byte)(color >> 16);
			byte b2 = (byte)(color >> 8);
			byte b3 = (byte)color;
			float alpha = num * 0.003921569f;
			float red = (float)b * 0.003921569f;
			float green = (float)b2 * 0.003921569f;
			float blue = (float)b3 * 0.003921569f;
			return new Color(red, green, blue, alpha);
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00004220 File Offset: 0x00002420
		public static Color FromHSV(float h, float s, float v)
		{
			float red = v;
			float green = v;
			float blue = v;
			if (s != 0f)
			{
				float num = h * 6f;
				int num2 = (int)Math.Floor((double)num);
				float num3 = v * (1f - s);
				float num4 = v * (1f - s * (num - (float)num2));
				float num5 = v * (1f - s * (1f - (num - (float)num2)));
				if (num2 == 0)
				{
					red = v;
					green = num5;
					blue = num3;
				}
				else if (num2 == 1)
				{
					red = num4;
					green = v;
					blue = num3;
				}
				else if (num2 == 2)
				{
					red = num3;
					green = v;
					blue = num5;
				}
				else if (num2 == 3)
				{
					red = num3;
					green = num4;
					blue = v;
				}
				else if (num2 == 4)
				{
					red = num5;
					green = num3;
					blue = v;
				}
				else
				{
					red = v;
					green = num3;
					blue = num4;
				}
			}
			return new Color(red, green, blue, 1f);
		}

		// Token: 0x060000BF RID: 191 RVA: 0x000042E4 File Offset: 0x000024E4
		public static Color ConvertStringToColor(string color)
		{
			string s = color.Substring(1, 2);
			string s2 = color.Substring(3, 2);
			string s3 = color.Substring(5, 2);
			string s4 = color.Substring(7, 2);
			int num = int.Parse(s, NumberStyles.HexNumber);
			int num2 = int.Parse(s2, NumberStyles.HexNumber);
			int num3 = int.Parse(s3, NumberStyles.HexNumber);
			int num4 = int.Parse(s4, NumberStyles.HexNumber);
			return new Color((float)num * 0.003921569f, (float)num2 * 0.003921569f, (float)num3 * 0.003921569f, (float)num4 * 0.003921569f);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00004370 File Offset: 0x00002570
		public static Color Lerp(Color start, Color end, float ratio)
		{
			float red = start.Red * (1f - ratio) + end.Red * ratio;
			float green = start.Green * (1f - ratio) + end.Green * ratio;
			float blue = start.Blue * (1f - ratio) + end.Blue * ratio;
			float alpha = start.Alpha * (1f - ratio) + end.Alpha * ratio;
			return new Color(red, green, blue, alpha);
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x000043E4 File Offset: 0x000025E4
		public override string ToString()
		{
			byte b = (byte)(this.Red * 255f);
			byte b2 = (byte)(this.Green * 255f);
			byte b3 = (byte)(this.Blue * 255f);
			byte b4 = (byte)(this.Alpha * 255f);
			return string.Concat(new string[]
			{
				"#",
				b.ToString("X2"),
				b2.ToString("X2"),
				b3.ToString("X2"),
				b4.ToString("X2")
			});
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00004478 File Offset: 0x00002678
		public static string UIntToColorString(uint color)
		{
			string text = (color >> 24).ToString("X");
			if (text.Length == 1)
			{
				text = text.Insert(0, "0");
			}
			string text2 = (color >> 16).ToString("X");
			if (text2.Length == 1)
			{
				text2 = text2.Insert(0, "0");
			}
			text2 = text2.Substring(MathF.Max(0, text2.Length - 2));
			string text3 = (color >> 8).ToString("X");
			if (text3.Length == 1)
			{
				text3 = text3.Insert(0, "0");
			}
			text3 = text3.Substring(MathF.Max(0, text3.Length - 2));
			uint num = color;
			string text4 = num.ToString("X");
			if (text4.Length == 1)
			{
				text4 = text4.Insert(0, "0");
			}
			text4 = text4.Substring(MathF.Max(0, text4.Length - 2));
			return text2 + text3 + text4 + text;
		}

		// Token: 0x04000065 RID: 101
		public float Red;

		// Token: 0x04000066 RID: 102
		public float Green;

		// Token: 0x04000067 RID: 103
		public float Blue;

		// Token: 0x04000068 RID: 104
		public float Alpha;
	}
}
