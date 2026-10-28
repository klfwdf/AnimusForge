using System;
using System.Collections.Generic;
using MCM.Abstractions.Base;

namespace MCM.Abstractions.Properties;

public sealed class NoneSettingsPropertyDiscoverer : ISettingsPropertyDiscoverer
{
	public IEnumerable<string> DiscoveryTypes { get; } = new string[1] { "none" };

	public IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings)
	{
		return Array.Empty<ISettingsPropertyDefinition>();
	}
}
