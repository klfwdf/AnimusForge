using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.UI.Adapter.MCMv5.Presets;

namespace MCM.UI.Adapter.MCMv5.Base
{
	// Token: 0x02000010 RID: 16
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MCMv5FluentSettingsWrapper : SettingsWrapper
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600002A RID: 42 RVA: 0x000029B3 File Offset: 0x00000BB3
		public override string DiscoveryType
		{
			get
			{
				return "mcm_v5_fluent";
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000029BA File Offset: 0x00000BBA
		[NullableContext(2)]
		public MCMv5FluentSettingsWrapper(object @object)
			: base(@object)
		{
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000029C5 File Offset: 0x00000BC5
		protected override BaseSettings Create([Nullable(2)] object @object)
		{
			return new MCMv5FluentSettingsWrapper(@object);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x000029CD File Offset: 0x00000BCD
		protected override ISettingsPreset CreatePreset([Nullable(2)] object @object)
		{
			return new MCMv5FluentSettingsPresetWrapper(@object);
		}
	}
}
