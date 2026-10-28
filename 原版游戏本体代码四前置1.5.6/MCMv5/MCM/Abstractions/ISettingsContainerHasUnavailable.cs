using System.Collections.Generic;

namespace MCM.Abstractions;

public interface ISettingsContainerHasUnavailable
{
	IEnumerable<UnavailableSetting> GetUnavailableSettings();
}
