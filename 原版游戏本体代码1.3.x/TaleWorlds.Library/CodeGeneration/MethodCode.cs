using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000C1 RID: 193
	public class MethodCode
	{
		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x06000720 RID: 1824 RVA: 0x00017F2E File Offset: 0x0001612E
		// (set) Token: 0x06000721 RID: 1825 RVA: 0x00017F36 File Offset: 0x00016136
		public string Comment { get; set; }

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x06000722 RID: 1826 RVA: 0x00017F3F File Offset: 0x0001613F
		// (set) Token: 0x06000723 RID: 1827 RVA: 0x00017F47 File Offset: 0x00016147
		public string Name { get; set; }

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x06000724 RID: 1828 RVA: 0x00017F50 File Offset: 0x00016150
		// (set) Token: 0x06000725 RID: 1829 RVA: 0x00017F58 File Offset: 0x00016158
		public string MethodSignature { get; set; }

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x06000726 RID: 1830 RVA: 0x00017F61 File Offset: 0x00016161
		// (set) Token: 0x06000727 RID: 1831 RVA: 0x00017F69 File Offset: 0x00016169
		public string ReturnParameter { get; set; }

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000728 RID: 1832 RVA: 0x00017F72 File Offset: 0x00016172
		// (set) Token: 0x06000729 RID: 1833 RVA: 0x00017F7A File Offset: 0x0001617A
		public bool IsStatic { get; set; }

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x0600072A RID: 1834 RVA: 0x00017F83 File Offset: 0x00016183
		// (set) Token: 0x0600072B RID: 1835 RVA: 0x00017F8B File Offset: 0x0001618B
		public MethodCodeAccessModifier AccessModifier { get; set; }

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x0600072C RID: 1836 RVA: 0x00017F94 File Offset: 0x00016194
		// (set) Token: 0x0600072D RID: 1837 RVA: 0x00017F9C File Offset: 0x0001619C
		public MethodCodePolymorphismInfo PolymorphismInfo { get; set; }

		// Token: 0x0600072E RID: 1838 RVA: 0x00017FA5 File Offset: 0x000161A5
		public MethodCode()
		{
			this.Name = "UnnamedMethod";
			this.MethodSignature = "()";
			this.PolymorphismInfo = MethodCodePolymorphismInfo.None;
			this.ReturnParameter = "void";
			this._lines = new List<string>();
		}

		// Token: 0x0600072F RID: 1839 RVA: 0x00017FE0 File Offset: 0x000161E0
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
			if (this.PolymorphismInfo == MethodCodePolymorphismInfo.Virtual)
			{
				text += "virtual ";
			}
			else if (this.PolymorphismInfo == MethodCodePolymorphismInfo.Override)
			{
				text += "override ";
			}
			text = string.Concat(new string[] { text, this.ReturnParameter, " ", this.Name, this.MethodSignature });
			if (!string.IsNullOrEmpty(this.Comment))
			{
				codeGenerationFile.AddLine(this.Comment);
			}
			codeGenerationFile.AddLine(text);
			codeGenerationFile.AddLine("{");
			foreach (string line in this._lines)
			{
				codeGenerationFile.AddLine(line);
			}
			codeGenerationFile.AddLine("}");
		}

		// Token: 0x06000730 RID: 1840 RVA: 0x0001813C File Offset: 0x0001633C
		public void AddLine(string line)
		{
			this._lines.Add(line);
		}

		// Token: 0x06000731 RID: 1841 RVA: 0x0001814C File Offset: 0x0001634C
		public void AddLines(IEnumerable<string> lines)
		{
			foreach (string item in lines)
			{
				this._lines.Add(item);
			}
		}

		// Token: 0x06000732 RID: 1842 RVA: 0x0001819C File Offset: 0x0001639C
		public void AddCodeBlock(CodeBlock codeBlock)
		{
			this.AddLines(codeBlock.Lines);
		}

		// Token: 0x0400023C RID: 572
		private List<string> _lines;
	}
}
