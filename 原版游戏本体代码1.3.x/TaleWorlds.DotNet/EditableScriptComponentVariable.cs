using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200000D RID: 13
	public class EditableScriptComponentVariable : Attribute
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000041 RID: 65 RVA: 0x00002E0C File Offset: 0x0000100C
		// (set) Token: 0x06000042 RID: 66 RVA: 0x00002E14 File Offset: 0x00001014
		public bool Visible { get; set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000043 RID: 67 RVA: 0x00002E1D File Offset: 0x0000101D
		// (set) Token: 0x06000044 RID: 68 RVA: 0x00002E25 File Offset: 0x00001025
		public string OverrideFieldName { get; set; }

		// Token: 0x06000045 RID: 69 RVA: 0x00002E2E File Offset: 0x0000102E
		public EditableScriptComponentVariable(bool visible, string overrideFieldName = "")
		{
			this.Visible = visible;
			this.OverrideFieldName = overrideFieldName;
		}
	}
}
