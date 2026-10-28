using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.UI.Adapter.MCMv5.Presets;

namespace MCM.UI.Adapter.MCMv5.Base;

internal sealed class MCMv5FluentSettingsWrapper : SettingsWrapper
{
	public override string DiscoveryType => "mcm_v5_fluent";

	public MCMv5FluentSettingsWrapper(object? @object)
		: base(@object)
	{
	}

	protected override BaseSettings Create(object? @object)
	{
		return new MCMv5FluentSettingsWrapper(@object);
	}

	protected override ISettingsPreset CreatePreset(object? @object)
	{
		return new MCMv5FluentSettingsPresetWrapper(@object);
	}
}
