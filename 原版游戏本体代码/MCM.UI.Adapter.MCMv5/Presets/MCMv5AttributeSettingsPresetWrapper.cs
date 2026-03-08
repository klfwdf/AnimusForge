using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.UI.Adapter.MCMv5.Base;

namespace MCM.UI.Adapter.MCMv5.Presets
{
	// Token: 0x0200000D RID: 13
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class MCMv5AttributeSettingsPresetWrapper : SettingsPresetWrapper<MCMv5AttributeSettingsWrapper>
	{
		// Token: 0x06000022 RID: 34 RVA: 0x0000296B File Offset: 0x00000B6B
		[NullableContext(2)]
		public MCMv5AttributeSettingsPresetWrapper(object @object)
			: base(@object)
		{
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002976 File Offset: 0x00000B76
		protected override MCMv5AttributeSettingsWrapper Create([Nullable(2)] object @object)
		{
			return new MCMv5AttributeSettingsWrapper(@object);
		}
	}
}
