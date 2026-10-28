using System.Collections.Generic;
using System.Linq;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.FluentBuilder;

namespace MCM.Implementation.FluentBuilder;

internal sealed class DefaultSettingsPresetBuilder : ISettingsPresetBuilder
{
	private string Id { get; }

	private string Name { get; }

	private IDictionary<string, object?> PropertyValues { get; } = new Dictionary<string, object>();

	public DefaultSettingsPresetBuilder(string id, string name)
	{
		Id = id;
		Name = name;
	}

	public ISettingsPresetBuilder SetPropertyValue(string property, object? value)
	{
		if (!PropertyValues.ContainsKey(property))
		{
			PropertyValues[property] = value;
		}
		return this;
	}

	public ISettingsPreset Build(BaseSettings settings)
	{
		return new MemorySettingsPreset(settings.Id, Id, Name, delegate
		{
			List<ISettingsPropertyDefinition> list = settings.GetAllSettingPropertyDefinitions().ToList();
			foreach (KeyValuePair<string, object> propertyValue in PropertyValues)
			{
				string overridePropertyId = propertyValue.Key;
				object value = propertyValue.Value;
				ISettingsPropertyDefinition settingsPropertyDefinition = list.Find((ISettingsPropertyDefinition x) => x.Id == overridePropertyId);
				if (settingsPropertyDefinition != null)
				{
					settingsPropertyDefinition.PropertyReference.Value = value;
				}
			}
			return settings;
		});
	}
}
