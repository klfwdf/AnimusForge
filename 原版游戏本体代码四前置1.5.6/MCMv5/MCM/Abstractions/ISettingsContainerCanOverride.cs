using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public interface ISettingsContainerCanOverride
{
	bool OverrideSettings(BaseSettings settings);
}
