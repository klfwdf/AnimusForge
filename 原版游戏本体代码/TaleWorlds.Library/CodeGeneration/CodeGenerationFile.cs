using System;
using System.Collections.Generic;
using System.Text;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000BE RID: 190
	public class CodeGenerationFile
	{
		// Token: 0x0600070D RID: 1805 RVA: 0x00017BA4 File Offset: 0x00015DA4
		public CodeGenerationFile(List<string> usingDefinitions = null)
		{
			this._lines = new List<string>();
			if (usingDefinitions != null && usingDefinitions.Count > 0)
			{
				foreach (string str in usingDefinitions)
				{
					this.AddLine("using " + str + ";");
				}
			}
		}

		// Token: 0x0600070E RID: 1806 RVA: 0x00017C20 File Offset: 0x00015E20
		public void AddLine(string line)
		{
			this._lines.Add(line);
		}

		// Token: 0x0600070F RID: 1807 RVA: 0x00017C30 File Offset: 0x00015E30
		public string GenerateText()
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			foreach (string text in this._lines)
			{
				if (text == "}" || text == "};")
				{
					num--;
				}
				string text2 = "";
				for (int i = 0; i < num; i++)
				{
					text2 += "\t";
				}
				text2 = text2 + text + "\n";
				if (text == "{")
				{
					num++;
				}
				stringBuilder.Append(text2);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x0400022D RID: 557
		private List<string> _lines;
	}
}
