using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public interface ISettingsContainerCanReset
{
	bool ResetSettings(BaseSettings settings);
}
