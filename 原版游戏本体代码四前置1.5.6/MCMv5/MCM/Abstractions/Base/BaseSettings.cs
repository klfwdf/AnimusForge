using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Base;

public abstract class BaseSettings : INotifyPropertyChanged
{
	private delegate BaseSettings SettingsCtor();

	private static readonly ConcurrentDictionary<Type, SettingsCtor> _cachedConstructors = new ConcurrentDictionary<Type, SettingsCtor>();

	public const string SaveTriggered = "SAVE_TRIGGERED";

	public const string LoadingComplete = "LOADING_COMPLETE";

	public const string DefaultPresetId = "default";

	public const string DefaultPresetName = "{=BaseSettings_Default}Default";

	public abstract string Id { get; }

	public abstract string DisplayName { get; }

	public virtual string FolderName { get; } = string.Empty;

	public virtual string SubFolder => string.Empty;

	public virtual string FormatType => "none";

	public virtual string DiscoveryType => "none";

	public virtual int UIVersion => 1;

	public virtual char SubGroupDelimiter => '/';

	public virtual event PropertyChangedEventHandler? PropertyChanged;

	public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public virtual BaseSettings CreateNew()
	{
		Type type = GetType();
		SettingsCtor orAdd = _cachedConstructors.GetOrAdd(type, (Type t) => AccessTools2.GetConstructorDelegate<SettingsCtor>(t) ?? ((SettingsCtor)(() => (BaseSettings)FormatterServices.GetUninitializedObject(t))));
		return orAdd();
	}

	public virtual BaseSettings CopyAsNew()
	{
		BaseSettings baseSettings = CreateNew();
		SettingsUtils.OverrideSettings(baseSettings, this);
		return baseSettings;
	}

	public virtual IEnumerable<ISettingsPreset> GetBuiltInPresets()
	{
		yield return new MemorySettingsPreset(Id, "default", "{=BaseSettings_Default}Default", CreateNew);
	}
}
