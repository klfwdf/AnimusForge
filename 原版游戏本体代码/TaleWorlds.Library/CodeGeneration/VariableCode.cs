using System;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000C6 RID: 198
	public class VariableCode
	{
		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x0600073E RID: 1854 RVA: 0x000182F2 File Offset: 0x000164F2
		// (set) Token: 0x0600073F RID: 1855 RVA: 0x000182FA File Offset: 0x000164FA
		public string Name { get; set; }

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000740 RID: 1856 RVA: 0x00018303 File Offset: 0x00016503
		// (set) Token: 0x06000741 RID: 1857 RVA: 0x0001830B File Offset: 0x0001650B
		public string Type { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000742 RID: 1858 RVA: 0x00018314 File Offset: 0x00016514
		// (set) Token: 0x06000743 RID: 1859 RVA: 0x0001831C File Offset: 0x0001651C
		public bool IsStatic { get; set; }

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x06000744 RID: 1860 RVA: 0x00018325 File Offset: 0x00016525
		// (set) Token: 0x06000745 RID: 1861 RVA: 0x0001832D File Offset: 0x0001652D
		public VariableCodeAccessModifier AccessModifier { get; set; }

		// Token: 0x06000746 RID: 1862 RVA: 0x00018336 File Offset: 0x00016536
		public VariableCode()
		{
			this.Type = "System.Object";
			this.Name = "Unnamed variable";
			this.IsStatic = false;
			this.AccessModifier = VariableCodeAccessModifier.Private;
		}

		// Token: 0x06000747 RID: 1863 RVA: 0x00018364 File Offset: 0x00016564
		public string GenerateLine()
		{
			string text = "";
			if (this.AccessModifier == VariableCodeAccessModifier.Public)
			{
				text += "public ";
			}
			else if (this.AccessModifier == VariableCodeAccessModifier.Protected)
			{
				text += "protected ";
			}
			else if (this.AccessModifier == VariableCodeAccessModifier.Private)
			{
				text += "private ";
			}
			else if (this.AccessModifier == VariableCodeAccessModifier.Internal)
			{
				text += "internal ";
			}
			if (this.IsStatic)
			{
				text += "static ";
			}
			return string.Concat(new string[] { text, this.Type, " ", this.Name, ";" });
		}
	}
}
