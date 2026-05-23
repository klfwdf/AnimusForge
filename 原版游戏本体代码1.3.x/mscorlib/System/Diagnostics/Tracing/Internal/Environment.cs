using System;
using System.Resources;
using Microsoft.Reflection;

namespace System.Diagnostics.Tracing.Internal
{
	// Token: 0x02000489 RID: 1161
	internal static class Environment
	{
		// Token: 0x1700081E RID: 2078
		// (get) Token: 0x0600376A RID: 14186 RVA: 0x000D5520 File Offset: 0x000D3720
		public static int TickCount
		{
			get
			{
				return Environment.TickCount;
			}
		}

		// Token: 0x0600376B RID: 14187 RVA: 0x000D5528 File Offset: 0x000D3728
		public static string GetResourceString(string key, params object[] args)
		{
			string @string = Environment.rm.GetString(key);
			if (@string != null)
			{
				return string.Format(@string, args);
			}
			string text = string.Empty;
			foreach (object obj in args)
			{
				if (text != string.Empty)
				{
					text += ", ";
				}
				text += obj.ToString();
			}
			return key + " (" + text + ")";
		}

		// Token: 0x0600376C RID: 14188 RVA: 0x000D559F File Offset: 0x000D379F
		public static string GetRuntimeResourceString(string key, params object[] args)
		{
			return Environment.GetResourceString(key, args);
		}

		// Token: 0x040018B3 RID: 6323
		public static readonly string NewLine = Environment.NewLine;

		// Token: 0x040018B4 RID: 6324
		private static ResourceManager rm = new ResourceManager("Microsoft.Diagnostics.Tracing.Messages", typeof(Environment).Assembly());
	}
}
