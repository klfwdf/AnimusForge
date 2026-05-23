using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000C0 RID: 192
	public class ConstructorCode
	{
		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x06000713 RID: 1811 RVA: 0x00017D7C File Offset: 0x00015F7C
		// (set) Token: 0x06000714 RID: 1812 RVA: 0x00017D84 File Offset: 0x00015F84
		public string Name { get; set; }

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000715 RID: 1813 RVA: 0x00017D8D File Offset: 0x00015F8D
		// (set) Token: 0x06000716 RID: 1814 RVA: 0x00017D95 File Offset: 0x00015F95
		public string MethodSignature { get; set; }

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x06000717 RID: 1815 RVA: 0x00017D9E File Offset: 0x00015F9E
		// (set) Token: 0x06000718 RID: 1816 RVA: 0x00017DA6 File Offset: 0x00015FA6
		public string BaseCall { get; set; }

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x06000719 RID: 1817 RVA: 0x00017DAF File Offset: 0x00015FAF
		// (set) Token: 0x0600071A RID: 1818 RVA: 0x00017DB7 File Offset: 0x00015FB7
		public bool IsStatic { get; set; }

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x0600071B RID: 1819 RVA: 0x00017DC0 File Offset: 0x00015FC0
		// (set) Token: 0x0600071C RID: 1820 RVA: 0x00017DC8 File Offset: 0x00015FC8
		public MethodCodeAccessModifier AccessModifier { get; set; }

		// Token: 0x0600071D RID: 1821 RVA: 0x00017DD1 File Offset: 0x00015FD1
		public ConstructorCode()
		{
			this.Name = "UnassignedConstructorName";
			this.MethodSignature = "()";
			this.BaseCall = "";
			this._lines = new List<string>();
		}

		// Token: 0x0600071E RID: 1822 RVA: 0x00017E08 File Offset: 0x00016008
		public void GenerateInto(CodeGenerationFile codeGenerationFile)
		{
			string text = "";
			if (this.AccessModifier == MethodCodeAccessModifier.Public)
			{
				text += "public ";
			}
			else if (this.AccessModifier == MethodCodeAccessModifier.Protected)
			{
				text += "protected ";
			}
			else if (this.AccessModifier == MethodCodeAccessModifier.Private)
			{
				text += "private ";
			}
			else if (this.AccessModifier == MethodCodeAccessModifier.Internal)
			{
				text += "internal ";
			}
			if (this.IsStatic)
			{
				text += "static ";
			}
			text = text + this.Name + this.MethodSignature;
			if (!string.IsNullOrEmpty(this.BaseCall))
			{
				text = text + " : base" + this.BaseCall;
			}
			codeGenerationFile.AddLine(text);
			codeGenerationFile.AddLine("{");
			foreach (string line in this._lines)
			{
				codeGenerationFile.AddLine(line);
			}
			codeGenerationFile.AddLine("}");
		}

		// Token: 0x0600071F RID: 1823 RVA: 0x00017F20 File Offset: 0x00016120
		public void AddLine(string line)
		{
			this._lines.Add(line);
		}

		// Token: 0x04000234 RID: 564
		private List<string> _lines;
	}
}
