using System.Collections.Generic;

namespace MCM.Abstractions;

public interface ISettingsContainerHasSettingsDefinitions
{
	IEnumerable<SettingsDefinition> SettingsDefinitions { get; }
}
