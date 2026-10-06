using System;
using System.Globalization;
using System.Numerics;

namespace TaleWorlds.Library;

public struct Color
{
	public float Red;

	public float Green;

	public float Blue;

	public float Alpha;

	public static Color Black => new Color(0f, 0f, 0f);

	public static Color White => new Color(1f, 1f, 1f);

	public Color(float red, float green, float blue, float alpha = 1f)
	{
		Red = red;
		Green = green;
		Blue = blue;
		Alpha = alpha;
	}

	public Vector3 ToVector3()
	{
		return new Vector3(Red, Green, Blue);
	}

	public Vec3 ToVec3()
	{
		return new Vec3(Red, Green, Blue, Alpha);
	}

	public static Color operator *(Color c, float f)
	{
		float red = c.Red * f;
		float green = c.Green * f;
		float blue = c.Blue * f;
		float alpha = c.Alpha * f;
		return new Color(red, green, blue, alpha);
	}

	public static Color operator *(Color c1, Color c2)
	{
		return new Color(c1.Red * c2.Red, c1.Green * c2.Green, c1.Blue * c2.Blue, c1.Alpha * c2.Alpha);
	}

	public static Color operator +(Color c1, Color c2)
	{
		return new Color(c1.Red + c2.Red, c1.Green + c2.Green, c1.Blue + c2.Blue, c1.Alpha + c2.Alpha);
	}

	public static Color operator -(Color c1, Color c2)
	{
		return new Color(c1.Red - c2.Red, c1.Green - c2.Green, c1.Blue - c2.Blue, c1.Alpha - c2.Alpha);
	}

	public static bool operator ==(Color a, Color b)
	{
		if (a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue)
		{
			return a.Alpha == b.Alpha;
		}
		return false;
	}

	public static bool operator !=(Color a, Color b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (!(obj is Color color))
		{
			return false;
		}
		return this == color;
	}

	public static Color FromVector3(Vector3 vector3)
	{
		return new Color(vector3.X, vector3.Y, vector3.Z);
	}

	public static Color FromVector3(Vec3 vector3)
	{
		return new Color(vector3.x, vector3.y, vector3.z);
	}

	public float Length()
	{
		return MathF.Sqrt(Red * Red + Green * Green + Blue * Blue + Alpha * Alpha);
	}

	public uint ToUnsignedInteger()
	{
		byte b = (byte)(Red * 255f);
		byte b2 = (byte)(Green * 255f);
		byte b3 = (byte)(Blue * 255f);
		return (uint)(((byte)(Alpha * 255f) << 24) + (b << 16) + (b2 << 8) + b3);
	}

	public static Color FromUint(uint color)
	{
		byte num = (byte)(color >> 24);
		byte b = (byte)(color >> 16);
		byte b2 = (byte)(color >> 8);
		byte b3 = (byte)color;
		float alpha = (float)(int)num * 0.003921569f;
		float red = (float)(int)b * 0.003921569f;
		float green = (float)(int)b2 * 0.003921569f;
		float blue = (float)(int)b3 * 0.003921569f;
		return new Color(red, green, blue, alpha);
	}

	public static Color FromHSV(float h, float s, float v)
	{
		float red = v;
		float green = v;
		float blue = v;
		if (s != 0f)
		{
			float num = h * 6f;
			int num2 = (int)Math.Floor(num);
			float num3 = v * (1f - s);
			float num4 = v * (1f - s * (num - (float)num2));
			float num5 = v * (1f - s * (1f - (num - (float)num2)));
			switch (num2)
			{
			case 0:
				red = v;
				green = num5;
				blue = num3;
				break;
			case 1:
				red = num4;
				green = v;
				blue = num3;
				break;
			case 2:
				red = num3;
				green = v;
				blue = num5;
				break;
			case 3:
				red = num3;
				green = num4;
				blue = v;
				break;
			case 4:
				red = num5;
				green = num3;
				blue = v;
				break;
			default:
				red = v;
				green = num3;
				blue = num4;
				break;
			}
		}
		return new Color(red, green, blue);
	}

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

	public static Color Lerp(Color start, Color end, float ratio)
	{
		float red = start.Red * (1f - ratio) + end.Red * ratio;
		float green = start.Green * (1f - ratio) + end.Green * ratio;
		float blue = start.Blue * (1f - ratio) + end.Blue * ratio;
		float alpha = start.Alpha * (1f - ratio) + end.Alpha * ratio;
		return new Color(red, green, blue, alpha);
	}

	public override string ToString()
	{
		byte b = (byte)(Red * 255f);
		byte b2 = (byte)(Green * 255f);
		byte b3 = (byte)(Blue * 255f);
		byte b4 = (byte)(Alpha * 255f);
		return "#" + b.ToString("X2") + b2.ToString("X2") + b3.ToString("X2") + b4.ToString("X2");
	}

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
}
