using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000BD RID: 189
	public class CodeGenerationContext
	{
		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x06000708 RID: 1800 RVA: 0x00017AA6 File Offset: 0x00015CA6
		// (set) Token: 0x06000709 RID: 1801 RVA: 0x00017AAE File Offset: 0x00015CAE
		public List<NamespaceCode> Namespaces { get; private set; }

		// Token: 0x0600070A RID: 1802 RVA: 0x00017AB7 File Offset: 0x00015CB7
		public CodeGenerationContext()
		{
			this.Namespaces = new List<NamespaceCode>();
		}

		// Token: 0x0600070B RID: 1803 RVA: 0x00017ACC File Offset: 0x00015CCC
		public NamespaceCode FindOrCreateNamespace(string name)
		{
			foreach (NamespaceCode namespaceCode in this.Namespaces)
			{
				if (namespaceCode.Name == name)
				{
					return namespaceCode;
				}
			}
			NamespaceCode namespaceCode2 = new NamespaceCode();
			namespaceCode2.Name = name;
			this.Namespaces.Add(namespaceCode2);
			return namespaceCode2;
		}

		// Token: 0x0600070C RID: 1804 RVA: 0x00017B48 File Offset: 0x00015D48
		public void GenerateInto(CodeGenerationFile codeGenerationFile)
		{
			foreach (NamespaceCode namespaceCode in this.Namespaces)
			{
				namespaceCode.GenerateInto(codeGenerationFile);
				codeGenerationFile.AddLine("");
			}
		}
	}
}
