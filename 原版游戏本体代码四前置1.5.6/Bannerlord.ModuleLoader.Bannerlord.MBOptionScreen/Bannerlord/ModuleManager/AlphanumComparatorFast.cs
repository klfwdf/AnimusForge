using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Bannerlord.ModuleManager;

internal sealed class AlphanumComparatorFast : IComparer<string?>, IComparer
{
	private enum ChunkType
	{
		Alphanumeric,
		Numeric
	}

	public int Compare(object? x, object? y)
	{
		return Compare(x as string, y as string);
	}

	public int Compare(string? s1, string? s2)
	{
		if (s1 == null && s2 == null)
		{
			return 0;
		}
		if (s1 == null)
		{
			return -1;
		}
		if (s2 == null)
		{
			return 1;
		}
		int length = s1.Length;
		int length2 = s2.Length;
		if (length == 0 && length2 == 0)
		{
			return 0;
		}
		if (length == 0)
		{
			return -1;
		}
		if (length2 == 0)
		{
			return 1;
		}
		int num = 0;
		int num2 = 0;
		while (num < length || num2 < length2)
		{
			if (num >= length)
			{
				return -1;
			}
			if (num2 >= length2)
			{
				return 1;
			}
			char c = s1[num];
			char c2 = s2[num2];
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			while (num < length && (stringBuilder.Length == 0 || InChunk(c, stringBuilder[0])))
			{
				stringBuilder.Append(c);
				num++;
				if (num < length)
				{
					c = s1[num];
				}
			}
			while (num2 < length2 && (stringBuilder2.Length == 0 || InChunk(c2, stringBuilder2[0])))
			{
				stringBuilder2.Append(c2);
				num2++;
				if (num2 < length2)
				{
					c2 = s2[num2];
				}
			}
			if (char.IsDigit(stringBuilder[0]) && char.IsDigit(stringBuilder2[0]))
			{
				int num3 = Convert.ToInt32(stringBuilder.ToString());
				int num4 = Convert.ToInt32(stringBuilder2.ToString());
				if (num3 < num4)
				{
					return -1;
				}
				if (num3 > num4)
				{
					return 1;
				}
			}
			else
			{
				int num5 = string.CompareOrdinal(stringBuilder.ToString(), stringBuilder2.ToString());
				if (num5 >= 1)
				{
					return 1;
				}
				if (num5 <= -1)
				{
					return -1;
				}
			}
		}
		return 0;
	}

	private static bool InChunk(char ch, char otherCh)
	{
		ChunkType chunkType = ChunkType.Alphanumeric;
		if (char.IsDigit(otherCh))
		{
			chunkType = ChunkType.Numeric;
		}
		if (chunkType != ChunkType.Alphanumeric || !char.IsDigit(ch))
		{
			if (chunkType == ChunkType.Numeric)
			{
				return char.IsDigit(ch);
			}
			return true;
		}
		return false;
	}
}
