using System;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000012 RID: 18
	public class WidgetCodeGenerationInfoChildSearchResult
	{
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x00007E62 File Offset: 0x00006062
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x00007E6A File Offset: 0x0000606A
		public WidgetCodeGenerationInfo FoundWidget { get; set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00007E73 File Offset: 0x00006073
		// (set) Token: 0x060000F4 RID: 244 RVA: 0x00007E7B File Offset: 0x0000607B
		public BindingPath RemainingPath { get; internal set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x00007E84 File Offset: 0x00006084
		// (set) Token: 0x060000F6 RID: 246 RVA: 0x00007E8C File Offset: 0x0000608C
		public WidgetCodeGenerationInfo ReachedWidget { get; internal set; }
	}
}
