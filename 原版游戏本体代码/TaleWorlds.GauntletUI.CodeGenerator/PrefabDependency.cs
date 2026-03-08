using System;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x0200000B RID: 11
	public class PrefabDependency
	{
		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000077 RID: 119 RVA: 0x0000394A File Offset: 0x00001B4A
		// (set) Token: 0x06000078 RID: 120 RVA: 0x00003952 File Offset: 0x00001B52
		public string Type { get; private set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000079 RID: 121 RVA: 0x0000395B File Offset: 0x00001B5B
		// (set) Token: 0x0600007A RID: 122 RVA: 0x00003963 File Offset: 0x00001B63
		public string VariantName { get; private set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x0600007B RID: 123 RVA: 0x0000396C File Offset: 0x00001B6C
		// (set) Token: 0x0600007C RID: 124 RVA: 0x00003974 File Offset: 0x00001B74
		public bool IsRoot { get; private set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600007D RID: 125 RVA: 0x0000397D File Offset: 0x00001B7D
		// (set) Token: 0x0600007E RID: 126 RVA: 0x00003985 File Offset: 0x00001B85
		public WidgetTemplateGenerateContext WidgetTemplateGenerateContext { get; private set; }

		// Token: 0x0600007F RID: 127 RVA: 0x0000398E File Offset: 0x00001B8E
		public PrefabDependency(string type, string variantName, bool isRoot, WidgetTemplateGenerateContext widgetTemplateGenerateContext)
		{
			this.Type = type;
			this.VariantName = variantName;
			this.IsRoot = isRoot;
			this.WidgetTemplateGenerateContext = widgetTemplateGenerateContext;
		}
	}
}
