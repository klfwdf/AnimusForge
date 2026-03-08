using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000BF RID: 191
	public class CommentSection
	{
		// Token: 0x06000710 RID: 1808 RVA: 0x00017CF8 File Offset: 0x00015EF8
		public CommentSection()
		{
			this._lines = new List<string>();
		}

		// Token: 0x06000711 RID: 1809 RVA: 0x00017D0B File Offset: 0x00015F0B
		public void AddCommentLine(string line)
		{
			this._lines.Add(line);
		}

		// Token: 0x06000712 RID: 1810 RVA: 0x00017D1C File Offset: 0x00015F1C
		public void GenerateInto(CodeGenerationFile codeGenerationFile)
		{
			foreach (string str in this._lines)
			{
				codeGenerationFile.AddLine("//" + str);
			}
		}

		// Token: 0x0400022E RID: 558
		private List<string> _lines;
	}
}
