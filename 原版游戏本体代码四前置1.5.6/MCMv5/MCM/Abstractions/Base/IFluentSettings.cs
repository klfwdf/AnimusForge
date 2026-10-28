using System;
using System.Collections.Generic;

namespace MCM.Abstractions.Base;

[Obsolete("Will be internal in the future")]
public interface IFluentSettings
{
	List<SettingsPropertyGroupDefinition> SettingPropertyGroups { get; }
}
