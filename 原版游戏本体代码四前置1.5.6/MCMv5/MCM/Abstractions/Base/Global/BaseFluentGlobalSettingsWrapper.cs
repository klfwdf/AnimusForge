using System;
using System.Collections.Generic;
using System.ComponentModel;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;

namespace MCM.Abstractions.Base.Global;

[Obsolete("Will be removed from future API", true)]
public abstract class BaseFluentGlobalSettingsWrapper : FluentGlobalSettings, IWrapper
{
	public object Object { get; protected set; }

	protected BaseFluentGlobalSettingsWrapper(object @object, string id, string displayName, string folderName, string subFolder, string format, int uiVersion, char subGroupDelimiter, PropertyChangedEventHandler? onPropertyChanged, IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups, IEnumerable<ISettingsPresetBuilder> presets)
		: base(id, displayName, folderName, subFolder, format, uiVersion, subGroupDelimiter, onPropertyChanged, settingPropertyGroups, presets)
	{
		Object = @object;
	}
}
