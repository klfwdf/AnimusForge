using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000C2 RID: 194
	public class CodeBlock
	{
		// Token: 0x170000EF RID: 239
		// (get) Token: 0x06000733 RID: 1843 RVA: 0x000181AA File Offset: 0x000163AA
		public List<string> Lines
		{
			get
			{
				return this._lines;
			}
		}

		// Token: 0x06000734 RID: 1844 RVA: 0x000181B2 File Offset: 0x000163B2
		public CodeBlock()
		{
			this._lines = new List<string>();
		}

		// Token: 0x06000735 RID: 1845 RVA: 0x000181C5 File Offset: 0x000163C5
		public void AddLine(string line)
		{
			this._lines.Add(line);
		}

		// Token: 0x06000736 RID: 1846 RVA: 0x000181D4 File Offset: 0x000163D4
		public void AddLines(IEnumerable<string> lines)
		{
			foreach (string item in lines)
			{
				this._lines.Add(item);
			}
		}

		// Token: 0x0400023D RID: 573
		private List<string> _lines;
	}
}
