using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.IO
{
	// Token: 0x020001AB RID: 427
	[ComVisible(false)]
	internal static class LongPath
	{
		// Token: 0x06001ADC RID: 6876 RVA: 0x0005A5BE File Offset: 0x000587BE
		[SecurityCritical]
		internal static string NormalizePath(string path)
		{
			return LongPath.NormalizePath(path, true);
		}

		// Token: 0x06001ADD RID: 6877 RVA: 0x0005A5C7 File Offset: 0x000587C7
		[SecurityCritical]
		internal static string NormalizePath(string path, bool fullCheck)
		{
			return Path.NormalizePath(path, fullCheck, 32767);
		}

		// Token: 0x06001ADE RID: 6878 RVA: 0x0005A5D8 File Offset: 0x000587D8
		internal static string InternalCombine(string path1, string path2)
		{
			bool flag;
			string path3 = LongPath.TryRemoveLongPathPrefix(path1, out flag);
			string text = Path.InternalCombine(path3, path2);
			if (flag)
			{
				text = Path.AddLongPathPrefix(text);
			}
			return text;
		}

		// Token: 0x06001ADF RID: 6879 RVA: 0x0005A604 File Offset: 0x00058804
		internal static int GetRootLength(string path)
		{
			bool flag;
			string path2 = LongPath.TryRemoveLongPathPrefix(path, out flag);
			int num = Path.GetRootLength(path2);
			if (flag)
			{
				num += 4;
			}
			return num;
		}

		// Token: 0x06001AE0 RID: 6880 RVA: 0x0005A62C File Offset: 0x0005882C
		internal static bool IsPathRooted(string path)
		{
			string path2 = Path.RemoveLongPathPrefix(path);
			return Path.IsPathRooted(path2);
		}

		// Token: 0x06001AE1 RID: 6881 RVA: 0x0005A648 File Offset: 0x00058848
		[SecurityCritical]
		internal static string GetPathRoot(string path)
		{
			if (path == null)
			{
				return null;
			}
			bool flag;
			string path2 = LongPath.TryRemoveLongPathPrefix(path, out flag);
			path2 = LongPath.NormalizePath(path2, false);
			string text = path.Substring(0, LongPath.GetRootLength(path2));
			if (flag)
			{
				text = Path.AddLongPathPrefix(text);
			}
			return text;
		}

		// Token: 0x06001AE2 RID: 6882 RVA: 0x0005A684 File Offset: 0x00058884
		[SecurityCritical]
		internal static string GetDirectoryName(string path)
		{
			if (path != null)
			{
				bool flag;
				string text = LongPath.TryRemoveLongPathPrefix(path, out flag);
				Path.CheckInvalidPathChars(text, false);
				path = LongPath.NormalizePath(text, false);
				int rootLength = LongPath.GetRootLength(text);
				int num = text.Length;
				if (num > rootLength)
				{
					num = text.Length;
					if (num == rootLength)
					{
						return null;
					}
					while (num > rootLength && text[--num] != Path.DirectorySeparatorChar && text[num] != Path.AltDirectorySeparatorChar)
					{
					}
					string text2 = text.Substring(0, num);
					if (flag)
					{
						text2 = Path.AddLongPathPrefix(text2);
					}
					return text2;
				}
			}
			return null;
		}

		// Token: 0x06001AE3 RID: 6883 RVA: 0x0005A70A File Offset: 0x0005890A
		internal static string TryRemoveLongPathPrefix(string path, out bool removed)
		{
			removed = Path.HasLongPathPrefix(path);
			if (!removed)
			{
				return path;
			}
			return Path.RemoveLongPathPrefix(path);
		}
	}
}
