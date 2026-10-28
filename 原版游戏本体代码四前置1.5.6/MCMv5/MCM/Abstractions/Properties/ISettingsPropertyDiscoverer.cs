using System.Collections.Generic;
using MCM.Abstractions.Base;

namespace MCM.Abstractions.Properties;

public interface ISettingsPropertyDiscoverer
{
	IEnumerable<string> DiscoveryTypes { get; }

	IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings);
}
