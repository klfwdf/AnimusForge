using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.UI.Adapter.MCMv5.Base;

namespace MCM.UI.Adapter.MCMv5.Presets
{
	// Token: 0x0200000E RID: 14
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class MCMv5FluentSettingsPresetWrapper : SettingsPresetWrapper<MCMv5FluentSettingsWrapper>
	{
		// Token: 0x06000024 RID: 36 RVA: 0x0000297E File Offset: 0x00000B7E
		[NullableContext(2)]
		public MCMv5FluentSettingsPresetWrapper(object @object)
			: base(@object)
		{
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002989 File Offset: 0x00000B89
		protected override MCMv5FluentSettingsWrapper Create([Nullable(2)] object @object)
		{
			return new MCMv5FluentSettingsWrapper(@object);
		}
	}
}
