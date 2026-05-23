using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.IO
{
	// Token: 0x0200019C RID: 412
	internal static class PathInternal
	{
		// Token: 0x06001941 RID: 6465 RVA: 0x00053F54 File Offset: 0x00052154
		internal static bool HasInvalidVolumeSeparator(string path)
		{
			int num = ((!AppContextSwitches.UseLegacyPathHandling && PathInternal.IsExtended(path)) ? "\\\\?\\".Length : PathInternal.PathStartSkip(path));
			return (path.Length > num && path[num] == Path.VolumeSeparatorChar) || (path.Length >= num + 2 && path[num + 1] == Path.VolumeSeparatorChar && !PathInternal.IsValidDriveChar(path[num])) || (path.Length > num + 2 && path.IndexOf(Path.VolumeSeparatorChar, num + 2) != -1);
		}

		// Token: 0x06001942 RID: 6466 RVA: 0x00053FE8 File Offset: 0x000521E8
		internal static bool StartsWithOrdinal(StringBuilder builder, string value, bool ignoreCase = false)
		{
			if (value == null || builder.Length < value.Length)
			{
				return false;
			}
			if (ignoreCase)
			{
				for (int i = 0; i < value.Length; i++)
				{
					if (char.ToUpperInvariant(builder[i]) != char.ToUpperInvariant(value[i]))
					{
						return false;
					}
				}
			}
			else
			{
				for (int j = 0; j < value.Length; j++)
				{
					if (builder[j] != value[j])
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06001943 RID: 6467 RVA: 0x0005405E File Offset: 0x0005225E
		internal static bool IsValidDriveChar(char value)
		{
			return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
		}

		// Token: 0x06001944 RID: 6468 RVA: 0x0005407B File Offset: 0x0005227B
		internal static bool IsPathTooLong(string fullPath)
		{
			if (AppContextSwitches.BlockLongPaths && (AppContextSwitches.UseLegacyPathHandling || !PathInternal.IsExtended(fullPath)))
			{
				return fullPath.Length >= 260;
			}
			return fullPath.Length >= 32767;
		}

		// Token: 0x06001945 RID: 6469 RVA: 0x000540B4 File Offset: 0x000522B4
		internal static bool AreSegmentsTooLong(string fullPath)
		{
			int length = fullPath.Length;
			int num = 0;
			for (int i = 0; i < length; i++)
			{
				if (PathInternal.IsDirectorySeparator(fullPath[i]))
				{
					if (i - num > PathInternal.MaxComponentLength)
					{
						return true;
					}
					num = i;
				}
			}
			return length - 1 - num > PathInternal.MaxComponentLength;
		}

		// Token: 0x06001946 RID: 6470 RVA: 0x00054101 File Offset: 0x00052301
		internal static bool IsDirectoryTooLong(string fullPath)
		{
			if (AppContextSwitches.BlockLongPaths && (AppContextSwitches.UseLegacyPathHandling || !PathInternal.IsExtended(fullPath)))
			{
				return fullPath.Length >= 248;
			}
			return PathInternal.IsPathTooLong(fullPath);
		}

		// Token: 0x06001947 RID: 6471 RVA: 0x00054130 File Offset: 0x00052330
		internal static string EnsureExtendedPrefix(string path)
		{
			if (PathInternal.IsPartiallyQualified(path) || PathInternal.IsDevice(path))
			{
				return path;
			}
			if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
			{
				return path.Insert(2, "?\\UNC\\");
			}
			return "\\\\?\\" + path;
		}

		// Token: 0x06001948 RID: 6472 RVA: 0x0005416A File Offset: 0x0005236A
		internal static string RemoveExtendedPrefix(string path)
		{
			if (!PathInternal.IsExtended(path))
			{
				return path;
			}
			if (PathInternal.IsExtendedUnc(path))
			{
				return path.Remove(2, 6);
			}
			return path.Substring(4);
		}

		// Token: 0x06001949 RID: 6473 RVA: 0x0005418E File Offset: 0x0005238E
		internal static StringBuilder RemoveExtendedPrefix(StringBuilder path)
		{
			if (!PathInternal.IsExtended(path))
			{
				return path;
			}
			if (PathInternal.IsExtendedUnc(path))
			{
				return path.Remove(2, 6);
			}
			return path.Remove(0, 4);
		}

		// Token: 0x0600194A RID: 6474 RVA: 0x000541B4 File Offset: 0x000523B4
		internal static bool IsDevice(string path)
		{
			return PathInternal.IsExtended(path) || (path.Length >= 4 && PathInternal.IsDirectorySeparator(path[0]) && PathInternal.IsDirectorySeparator(path[1]) && (path[2] == '.' || path[2] == '?') && PathInternal.IsDirectorySeparator(path[3]));
		}

		// Token: 0x0600194B RID: 6475 RVA: 0x00054214 File Offset: 0x00052414
		internal static bool IsDevice(StringBuffer path)
		{
			return PathInternal.IsExtended(path) || (path.Length >= 4U && PathInternal.IsDirectorySeparator(path[0U]) && PathInternal.IsDirectorySeparator(path[1U]) && (path[2U] == '.' || path[2U] == '?') && PathInternal.IsDirectorySeparator(path[3U]));
		}

		// Token: 0x0600194C RID: 6476 RVA: 0x00054274 File Offset: 0x00052474
		internal static bool IsExtended(string path)
		{
			return path.Length >= 4 && path[0] == '\\' && (path[1] == '\\' || path[1] == '?') && path[2] == '?' && path[3] == '\\';
		}

		// Token: 0x0600194D RID: 6477 RVA: 0x000542C4 File Offset: 0x000524C4
		internal static bool IsExtended(StringBuilder path)
		{
			return path.Length >= 4 && path[0] == '\\' && (path[1] == '\\' || path[1] == '?') && path[2] == '?' && path[3] == '\\';
		}

		// Token: 0x0600194E RID: 6478 RVA: 0x00054314 File Offset: 0x00052514
		internal static bool IsExtended(StringBuffer path)
		{
			return path.Length >= 4U && path[0U] == '\\' && (path[1U] == '\\' || path[1U] == '?') && path[2U] == '?' && path[3U] == '\\';
		}

		// Token: 0x0600194F RID: 6479 RVA: 0x00054364 File Offset: 0x00052564
		internal static bool IsExtendedUnc(string path)
		{
			return path.Length >= "\\\\?\\UNC\\".Length && PathInternal.IsExtended(path) && char.ToUpper(path[4]) == 'U' && char.ToUpper(path[5]) == 'N' && char.ToUpper(path[6]) == 'C' && path[7] == '\\';
		}

		// Token: 0x06001950 RID: 6480 RVA: 0x000543C8 File Offset: 0x000525C8
		internal static bool IsExtendedUnc(StringBuilder path)
		{
			return path.Length >= "\\\\?\\UNC\\".Length && PathInternal.IsExtended(path) && char.ToUpper(path[4]) == 'U' && char.ToUpper(path[5]) == 'N' && char.ToUpper(path[6]) == 'C' && path[7] == '\\';
		}

		// Token: 0x06001951 RID: 6481 RVA: 0x0005442C File Offset: 0x0005262C
		internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
		{
			return (AppContextSwitches.UseLegacyPathHandling || !PathInternal.IsDevice(path)) && PathInternal.AnyPathHasIllegalCharacters(path, checkAdditional);
		}

		// Token: 0x06001952 RID: 6482 RVA: 0x00054446 File Offset: 0x00052646
		internal static bool AnyPathHasIllegalCharacters(string path, bool checkAdditional = false)
		{
			return path.IndexOfAny(PathInternal.InvalidPathChars) >= 0 || (checkAdditional && PathInternal.AnyPathHasWildCardCharacters(path, 0));
		}

		// Token: 0x06001953 RID: 6483 RVA: 0x00054464 File Offset: 0x00052664
		internal static bool HasWildCardCharacters(string path)
		{
			int startIndex = (AppContextSwitches.UseLegacyPathHandling ? 0 : (PathInternal.IsDevice(path) ? "\\\\?\\".Length : 0));
			return PathInternal.AnyPathHasWildCardCharacters(path, startIndex);
		}

		// Token: 0x06001954 RID: 6484 RVA: 0x00054498 File Offset: 0x00052698
		internal static bool AnyPathHasWildCardCharacters(string path, int startIndex = 0)
		{
			for (int i = startIndex; i < path.Length; i++)
			{
				char c = path[i];
				if (c == '*' || c == '?')
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001955 RID: 6485 RVA: 0x000544CC File Offset: 0x000526CC
		[SecuritySafeCritical]
		internal unsafe static int GetRootLength(string path)
		{
			char* ptr = path;
			if (ptr != null)
			{
				ptr += RuntimeHelpers.OffsetToStringData / 2;
			}
			return (int)PathInternal.GetRootLength(ptr, (ulong)((long)path.Length));
		}

		// Token: 0x06001956 RID: 6486 RVA: 0x000544F6 File Offset: 0x000526F6
		[SecuritySafeCritical]
		internal static uint GetRootLength(StringBuffer path)
		{
			if (path.Length == 0U)
			{
				return 0U;
			}
			return PathInternal.GetRootLength(path.CharPointer, (ulong)path.Length);
		}

		// Token: 0x06001957 RID: 6487 RVA: 0x00054514 File Offset: 0x00052714
		[SecurityCritical]
		private unsafe static uint GetRootLength(char* path, ulong pathLength)
		{
			uint num = 0U;
			uint num2 = 2U;
			uint num3 = 2U;
			bool flag = PathInternal.StartsWithOrdinal(path, pathLength, "\\\\?\\");
			bool flag2 = PathInternal.StartsWithOrdinal(path, pathLength, "\\\\?\\UNC\\");
			if (flag)
			{
				if (flag2)
				{
					num3 = (uint)"\\\\?\\UNC\\".Length;
				}
				else
				{
					num2 += (uint)"\\\\?\\".Length;
				}
			}
			if ((!flag || flag2) && pathLength > 0UL && PathInternal.IsDirectorySeparator(*path))
			{
				num = 1U;
				if (flag2 || (pathLength > 1UL && PathInternal.IsDirectorySeparator(path[1])))
				{
					num = num3;
					int num4 = 2;
					while ((ulong)num < pathLength)
					{
						if (PathInternal.IsDirectorySeparator(path[(ulong)num * 2UL / 2UL]) && --num4 <= 0)
						{
							break;
						}
						num += 1U;
					}
				}
			}
			else if (pathLength >= (ulong)num2 && path[(ulong)(num2 - 1U) * 2UL / 2UL] == Path.VolumeSeparatorChar)
			{
				num = num2;
				if (pathLength >= (ulong)(num2 + 1U) && PathInternal.IsDirectorySeparator(path[(ulong)num2 * 2UL / 2UL]))
				{
					num += 1U;
				}
			}
			return num;
		}

		// Token: 0x06001958 RID: 6488 RVA: 0x000545F4 File Offset: 0x000527F4
		[SecurityCritical]
		private unsafe static bool StartsWithOrdinal(char* source, ulong sourceLength, string value)
		{
			if (sourceLength < (ulong)((long)value.Length))
			{
				return false;
			}
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i] != source[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001959 RID: 6489 RVA: 0x00054634 File Offset: 0x00052834
		internal static bool IsPartiallyQualified(string path)
		{
			if (path.Length < 2)
			{
				return true;
			}
			if (PathInternal.IsDirectorySeparator(path[0]))
			{
				return path[1] != '?' && !PathInternal.IsDirectorySeparator(path[1]);
			}
			return path.Length < 3 || path[1] != Path.VolumeSeparatorChar || !PathInternal.IsDirectorySeparator(path[2]) || !PathInternal.IsValidDriveChar(path[0]);
		}

		// Token: 0x0600195A RID: 6490 RVA: 0x000546B0 File Offset: 0x000528B0
		internal static bool IsPartiallyQualified(StringBuffer path)
		{
			if (path.Length < 2U)
			{
				return true;
			}
			if (PathInternal.IsDirectorySeparator(path[0U]))
			{
				return path[1U] != '?' && !PathInternal.IsDirectorySeparator(path[1U]);
			}
			return path.Length < 3U || path[1U] != Path.VolumeSeparatorChar || !PathInternal.IsDirectorySeparator(path[2U]) || !PathInternal.IsValidDriveChar(path[0U]);
		}

		// Token: 0x0600195B RID: 6491 RVA: 0x0005472C File Offset: 0x0005292C
		internal static int PathStartSkip(string path)
		{
			int num = 0;
			while (num < path.Length && path[num] == ' ')
			{
				num++;
			}
			if ((num > 0 && num < path.Length && PathInternal.IsDirectorySeparator(path[num])) || (num + 1 < path.Length && path[num + 1] == Path.VolumeSeparatorChar && PathInternal.IsValidDriveChar(path[num])))
			{
				return num;
			}
			return 0;
		}

		// Token: 0x0600195C RID: 6492 RVA: 0x0005479C File Offset: 0x0005299C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsDirectorySeparator(char c)
		{
			return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
		}

		// Token: 0x0600195D RID: 6493 RVA: 0x000547B0 File Offset: 0x000529B0
		internal static string NormalizeDirectorySeparators(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return path;
			}
			int num = PathInternal.PathStartSkip(path);
			if (num == 0)
			{
				bool flag = true;
				for (int i = 0; i < path.Length; i++)
				{
					char c = path[i];
					if (PathInternal.IsDirectorySeparator(c) && (c != Path.DirectorySeparatorChar || (i > 0 && i + 1 < path.Length && PathInternal.IsDirectorySeparator(path[i + 1]))))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return path;
				}
			}
			StringBuilder stringBuilder = StringBuilderCache.Acquire(path.Length);
			if (PathInternal.IsDirectorySeparator(path[num]))
			{
				num++;
				stringBuilder.Append(Path.DirectorySeparatorChar);
			}
			int j = num;
			while (j < path.Length)
			{
				char c = path[j];
				if (!PathInternal.IsDirectorySeparator(c))
				{
					goto IL_D2;
				}
				if (j + 1 >= path.Length || !PathInternal.IsDirectorySeparator(path[j + 1]))
				{
					c = Path.DirectorySeparatorChar;
					goto IL_D2;
				}
				IL_DA:
				j++;
				continue;
				IL_D2:
				stringBuilder.Append(c);
				goto IL_DA;
			}
			return StringBuilderCache.GetStringAndRelease(stringBuilder);
		}

		// Token: 0x040008E0 RID: 2272
		internal const string ExtendedPathPrefix = "\\\\?\\";

		// Token: 0x040008E1 RID: 2273
		internal const string UncPathPrefix = "\\\\";

		// Token: 0x040008E2 RID: 2274
		internal const string UncExtendedPrefixToInsert = "?\\UNC\\";

		// Token: 0x040008E3 RID: 2275
		internal const string UncExtendedPathPrefix = "\\\\?\\UNC\\";

		// Token: 0x040008E4 RID: 2276
		internal const string DevicePathPrefix = "\\\\.\\";

		// Token: 0x040008E5 RID: 2277
		internal const int DevicePrefixLength = 4;

		// Token: 0x040008E6 RID: 2278
		internal const int MaxShortPath = 260;

		// Token: 0x040008E7 RID: 2279
		internal const int MaxShortDirectoryPath = 248;

		// Token: 0x040008E8 RID: 2280
		internal const int MaxLongPath = 32767;

		// Token: 0x040008E9 RID: 2281
		internal static readonly int MaxComponentLength = 255;

		// Token: 0x040008EA RID: 2282
		internal static readonly char[] InvalidPathChars = new char[]
		{
			'"', '<', '>', '|', '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005',
			'\u0006', '\a', '\b', '\t', '\n', '\v', '\f', '\r', '\u000e', '\u000f',
			'\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019',
			'\u001a', '\u001b', '\u001c', '\u001d', '\u001e', '\u001f'
		};
	}
}
