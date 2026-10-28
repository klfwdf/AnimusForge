using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.Utils;
using MCM.Abstractions.Xml;
using MCM.Common;

namespace MCM.Abstractions.Base.Global;

public sealed class ExternalGlobalSettings : FluentGlobalSettings
{
	public string FilePath { get; init; }

	private static SettingsPropertyDefinition FromXml(IPropertyGroupDefinition group, PropertyBaseXmlModel xmlModel, char subGroupDelimiter)
	{
		if (1 == 0)
		{
		}
		SettingsPropertyDefinition result = default(SettingsPropertyDefinition);
		if (!(xmlModel is PropertyBoolXmlModel propertyBoolXmlModel))
		{
			if (!(xmlModel is PropertyDropdownXmlModel propertyDropdownXmlModel))
			{
				if (!(xmlModel is PropertyFloatingIntegerXmlModel propertyFloatingIntegerXmlModel))
				{
					if (!(xmlModel is PropertyIntegerXmlModel propertyIntegerXmlModel))
					{
						if (xmlModel is PropertyTextXmlModel propertyTextXmlModel)
						{
							result = new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(propertyTextXmlModel), group, new StorageRef<string>(propertyTextXmlModel.Value), subGroupDelimiter);
						}
						else
						{
							if (1 == 0)
							{
							}
							global::_003CPrivateImplementationDetails_003E.ThrowInvalidOperationException();
						}
					}
					else
					{
						result = new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(propertyIntegerXmlModel), group, new StorageRef<int>((int)propertyIntegerXmlModel.Value), subGroupDelimiter);
					}
				}
				else
				{
					result = new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(propertyFloatingIntegerXmlModel), group, new StorageRef<float>((float)propertyFloatingIntegerXmlModel.Value), subGroupDelimiter);
				}
			}
			else
			{
				result = new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(propertyDropdownXmlModel), group, new StorageRef<Dropdown<string>>(new Dropdown<string>(propertyDropdownXmlModel.Values, propertyDropdownXmlModel.SelectedIndex)), subGroupDelimiter);
			}
		}
		else
		{
			result = new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(propertyBoolXmlModel), group, new StorageRef<bool>(propertyBoolXmlModel.Value), subGroupDelimiter);
		}
		if (1 == 0)
		{
		}
		return result;
	}

	public static ExternalGlobalSettings? CreateFromXmlFile(string filePath, PropertyChangedEventHandler? propertyChanged = null)
	{
		using FileStream xmlStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		SettingsXmlModel settingsXmlModel = SerializationUtils.DeserializeXml<SettingsXmlModel>(xmlStream);
		if (settingsXmlModel == null)
		{
			return null;
		}
		char subGroupDelimiter = settingsXmlModel.SubGroupDelimiter[0];
		IEnumerable<SettingsPropertyDefinition> settingsPropertyDefinitions = settingsXmlModel.Groups.SelectMany((PropertyGroupXmlModel g) => g.Properties.Select((PropertyBaseXmlModel p) => FromXml(g, p, subGroupDelimiter))).Concat(settingsXmlModel.Properties.Select((PropertyBaseXmlModel p) => FromXml(SettingsPropertyGroupDefinition.DefaultGroup, p, subGroupDelimiter)));
		List<SettingsPropertyGroupDefinition> settingsPropertyGroups = SettingsUtils.GetSettingsPropertyGroups(subGroupDelimiter, settingsPropertyDefinitions);
		return new ExternalGlobalSettings(settingsXmlModel.Id, settingsXmlModel.DisplayName, settingsXmlModel.FolderName, settingsXmlModel.SubFolder, settingsXmlModel.FormatType, settingsXmlModel.UIVersion, subGroupDelimiter, propertyChanged, settingsPropertyGroups, new List<ISettingsPresetBuilder>(), filePath);
	}

	private ExternalGlobalSettings(string id, string displayName, string folderName, string subFolder, string format, int uiVersion, char subGroupDelimiter, PropertyChangedEventHandler? onPropertyChanged, IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups, IEnumerable<ISettingsPresetBuilder> presets, string filePath)
		: base(id, displayName, folderName, subFolder, format, uiVersion, subGroupDelimiter, onPropertyChanged, settingPropertyGroups, presets)
	{
		FilePath = filePath;
	}
}
