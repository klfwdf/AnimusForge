using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.UI.Adapter.MCMv5.Presets;

namespace MCM.UI.Adapter.MCMv5.Base
{
	// Token: 0x0200000F RID: 15
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MCMv5AttributeSettingsWrapper : SettingsWrapper
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00002991 File Offset: 0x00000B91
		public override string DiscoveryType
		{
			get
			{
				return "mcm_v5_attributes";
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002998 File Offset: 0x00000B98
		[NullableContext(2)]
		public MCMv5AttributeSettingsWrapper(object @object)
			: base(@object)
		{
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000029A3 File Offset: 0x00000BA3
		protected override BaseSettings Create([Nullable(2)] object @object)
		{
			return new MCMv5AttributeSettingsWrapper(@object);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000029AB File Offset: 0x00000BAB
		protected override ISettingsPreset CreatePreset([Nullable(2)] object @object)
		{
			return new MCMv5AttributeSettingsPresetWrapper(@object);
		}
	}
}
