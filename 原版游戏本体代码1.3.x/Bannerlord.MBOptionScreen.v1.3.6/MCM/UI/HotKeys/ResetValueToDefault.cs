using System;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.HotKeys;
using TaleWorlds.InputSystem;

namespace MCM.UI.HotKeys
{
	// Token: 0x0200001D RID: 29
	[NullableContext(1)]
	[Nullable(0)]
	public class ResetValueToDefault : HotKeyBase
	{
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000AF RID: 175 RVA: 0x00003E4B File Offset: 0x0000204B
		protected override string DisplayName { get; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00003E53 File Offset: 0x00002053
		protected override string Description { get; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000B1 RID: 177 RVA: 0x00003E5B File Offset: 0x0000205B
		protected override InputKey DefaultKey { get; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000B2 RID: 178 RVA: 0x00003E63 File Offset: 0x00002063
		protected override string Category { get; }

		// Token: 0x060000B3 RID: 179 RVA: 0x00003E6B File Offset: 0x0000206B
		public ResetValueToDefault()
			: base("ResetValueToDefault")
		{
			this.DisplayName = "{=HOV8WIcBrb}Reset Mod Options Value to Default";
			this.Description = "{=2d99VmOZZH}Resets a value in Mod Options menu to its default value when hovered.";
			this.DefaultKey = 19;
			this.Category = HotKeyManager.Categories[2];
		}
	}
}
