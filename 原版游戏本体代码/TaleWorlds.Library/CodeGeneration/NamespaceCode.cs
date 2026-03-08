using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000C5 RID: 197
	public class NamespaceCode
	{
		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x06000737 RID: 1847 RVA: 0x00018224 File Offset: 0x00016424
		// (set) Token: 0x06000738 RID: 1848 RVA: 0x0001822C File Offset: 0x0001642C
		public string Name { get; set; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x06000739 RID: 1849 RVA: 0x00018235 File Offset: 0x00016435
		// (set) Token: 0x0600073A RID: 1850 RVA: 0x0001823D File Offset: 0x0001643D
		public List<ClassCode> Classes { get; private set; }

		// Token: 0x0600073B RID: 1851 RVA: 0x00018246 File Offset: 0x00016446
		public NamespaceCode()
		{
			this.Classes = new List<ClassCode>();
		}

		// Token: 0x0600073C RID: 1852 RVA: 0x0001825C File Offset: 0x0001645C
		public void GenerateInto(CodeGenerationFile codeGenerationFile)
		{
			codeGenerationFile.AddLine("namespace " + this.Name);
			codeGenerationFile.AddLine("{");
			foreach (ClassCode classCode in this.Classes)
			{
				classCode.GenerateInto(codeGenerationFile);
				codeGenerationFile.AddLine("");
			}
			codeGenerationFile.AddLine("}");
		}

		// Token: 0x0600073D RID: 1853 RVA: 0x000182E4 File Offset: 0x000164E4
		public void AddClass(ClassCode clasCode)
		{
			this.Classes.Add(clasCode);
		}
	}
}
