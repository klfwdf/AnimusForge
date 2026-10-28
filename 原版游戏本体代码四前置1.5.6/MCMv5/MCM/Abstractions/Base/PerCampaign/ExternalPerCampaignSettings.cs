using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.Utils;
using MCM.Abstractions.Xml;
using MCM.Common;

namespace MCM.Abstractions.Base.PerCampaign;

public sealed class ExternalPerCampaignSettings : FluentPerCampaignSettings
{
	public static ExternalPerCampaignSettings? CreateFromXmlStream(Stream xmlStream, Func<IPropertyDefinitionBase, IRef> assignRefDelegate, PropertyChangedEventHandler? propertyChanged = null)
	{
		SettingsXmlModel settingsXmlModel = SerializationUtils.DeserializeXml<SettingsXmlModel>(xmlStream);
		if (settingsXmlModel == null)
		{
			return null;
		}
		char subGroupDelimiter = settingsXmlModel.SubGroupDelimiter[0];
		IEnumerable<SettingsPropertyDefinition> settingsPropertyDefinitions = settingsXmlModel.Groups.SelectMany((PropertyGroupXmlModel g) => g.Properties.Select((PropertyBaseXmlModel p) => new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(p), g, assignRefDelegate(p), subGroupDelimiter))).Concat(settingsXmlModel.Properties.Select((PropertyBaseXmlModel p) => new SettingsPropertyDefinition(SettingsUtils.GetPropertyDefinitionWrappers(p), SettingsPropertyGroupDefinition.DefaultGroup, assignRefDelegate(p), subGroupDelimiter)));
		List<SettingsPropertyGroupDefinition> settingsPropertyGroups = SettingsUtils.GetSettingsPropertyGroups(subGroupDelimiter, settingsPropertyDefinitions);
		return new ExternalPerCampaignSettings(settingsXmlModel.Id, settingsXmlModel.DisplayName, settingsXmlModel.FolderName, settingsXmlModel.SubFolder, settingsXmlModel.UIVersion, subGroupDelimiter, propertyChanged, settingsPropertyGroups, new List<ISettingsPresetBuilder>());
	}

	private ExternalPerCampaignSettings(string id, string displayName, string folderName, string subFolder, int uiVersion, char subGroupDelimiter, PropertyChangedEventHandler? onPropertyChanged, IEnumerable<SettingsPropertyGroupDefinition> settingPropertyGroups, IEnumerable<ISettingsPresetBuilder> presets)
		: base(id, displayName, folderName, subFolder, uiVersion, subGroupDelimiter, onPropertyChanged, settingPropertyGroups, presets)
	{
	}
}
