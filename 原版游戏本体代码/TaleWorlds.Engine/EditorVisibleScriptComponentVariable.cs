using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000087 RID: 135
	public class EditorVisibleScriptComponentVariable : Attribute
	{
		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000C14 RID: 3092 RVA: 0x0000D3F3 File Offset: 0x0000B5F3
		// (set) Token: 0x06000C15 RID: 3093 RVA: 0x0000D3FB File Offset: 0x0000B5FB
		public bool Visible { get; set; }

		// Token: 0x06000C16 RID: 3094 RVA: 0x0000D404 File Offset: 0x0000B604
		public EditorVisibleScriptComponentVariable(bool visible)
		{
			this.Visible = visible;
		}
	}
}
