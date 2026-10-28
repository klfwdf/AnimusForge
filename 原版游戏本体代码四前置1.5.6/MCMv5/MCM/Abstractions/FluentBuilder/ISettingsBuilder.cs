using System;
using System.ComponentModel;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Base.PerCampaign;
using MCM.Abstractions.Base.PerSave;

namespace MCM.Abstractions.FluentBuilder;

public interface ISettingsBuilder
{
	ISettingsBuilder SetFolderName(string value);

	ISettingsBuilder SetSubFolder(string value);

	ISettingsBuilder SetFormat(string value);

	ISettingsBuilder SetUIVersion(int value);

	ISettingsBuilder SetSubGroupDelimiter(char value);

	ISettingsBuilder SetOnPropertyChanged(PropertyChangedEventHandler value);

	ISettingsBuilder CreateGroup(string name, Action<ISettingsPropertyGroupBuilder> builder);

	ISettingsBuilder CreatePreset(string id, string name, Action<ISettingsPresetBuilder> builder);

	ISettingsBuilder WithoutDefaultPreset();

	FluentGlobalSettings BuildAsGlobal();

	FluentPerSaveSettings BuildAsPerSave();

	FluentPerCampaignSettings BuildAsPerCampaign();
}
