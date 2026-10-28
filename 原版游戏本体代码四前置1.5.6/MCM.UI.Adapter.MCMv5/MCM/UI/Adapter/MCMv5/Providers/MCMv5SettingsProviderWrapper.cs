using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Common;
using MCM.UI.Adapter.MCMv5.Base;
using MCM.UI.Adapter.MCMv5.Presets;

namespace MCM.UI.Adapter.MCMv5.Providers;

internal sealed class MCMv5SettingsProviderWrapper : SettingsProviderWrapper
{
	private delegate IEnumerable GetPresetsDelegate(string settingsId);

	private readonly GetPresetsDelegate? _methodGetPresetsDelegate;

	public MCMv5SettingsProviderWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		_methodGetPresetsDelegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetPresetsDelegate>(@object, type, "GetPresets");
	}

	protected override BaseSettings? Create(object obj)
	{
		Type type = obj.GetType();
		if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.FluentGlobalSettings"))
		{
			return new MCMv5FluentSettingsWrapper(obj);
		}
		if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.GlobalSettings"))
		{
			return new MCMv5AttributeSettingsWrapper(obj);
		}
		if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.FluentPerCampaignSettings"))
		{
			return new MCMv5FluentSettingsWrapper(obj);
		}
		if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.PerCampaignSettings"))
		{
			return new MCMv5AttributeSettingsWrapper(obj);
		}
		if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.FluentPerSaveSettings"))
		{
			return new MCMv5FluentSettingsWrapper(obj);
		}
		if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.PerSaveSettings"))
		{
			return new MCMv5AttributeSettingsWrapper(obj);
		}
		return null;
	}

	protected override bool IsSettings(BaseSettings settings, [NotNullWhen(true)] out object? wrapped)
	{
		object obj = default(object);
		bool flag;
		if (settings is MCMv5AttributeSettingsWrapper || settings is MCMv5FluentSettingsWrapper)
		{
			IWrapper wrapper = (IWrapper)settings;
			obj = wrapper.Object;
			if (obj != null)
			{
				flag = true;
				goto IL_002c;
			}
		}
		flag = false;
		goto IL_002c;
		IL_002c:
		if (flag)
		{
			wrapped = obj;
			return true;
		}
		wrapped = null;
		return false;
	}

	public override IEnumerable<ISettingsPreset> GetPresets(string settingsId)
	{
		BaseSettings settings = GetSettings(settingsId);
		if (settings is MCMv5AttributeSettingsWrapper && settings is IWrapper wrapper)
		{
			object obj = wrapper.Object;
			if (obj != null && _methodGetPresetsDelegate != null)
			{
				Type type = obj.GetType();
				IEnumerable<object> source = _methodGetPresetsDelegate(settingsId).OfType<object>();
				if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.FluentGlobalSettings"))
				{
					return source.Select((object x) => new MCMv5FluentSettingsPresetWrapper(x));
				}
				if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.Global.GlobalSettings"))
				{
					return source.Select((object x) => new MCMv5AttributeSettingsPresetWrapper(x));
				}
				if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.FluentPerCampaignSettings"))
				{
					return source.Select((object x) => new MCMv5FluentSettingsPresetWrapper(x));
				}
				if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerCampaign.PerCampaignSettings"))
				{
					return source.Select((object x) => new MCMv5AttributeSettingsPresetWrapper(x));
				}
				if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.FluentPerSaveSettings"))
				{
					return source.Select((object x) => new MCMv5FluentSettingsPresetWrapper(x));
				}
				if (ReflectionUtils.ImplementsOrImplementsEquivalent(type, "MCM.Abstractions.Base.PerSave.PerSaveSettings"))
				{
					return source.Select((object x) => new MCMv5AttributeSettingsPresetWrapper(x));
				}
				return Array.Empty<ISettingsPreset>();
			}
		}
		return Array.Empty<ISettingsPreset>();
	}

	public override IEnumerable<UnavailableSetting> GetUnavailableSettings()
	{
		return Array.Empty<UnavailableSetting>();
	}

	public override IEnumerable<SettingSnapshot> SaveAvailableSnapshots()
	{
		return Array.Empty<SettingSnapshot>();
	}

	public override IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots)
	{
		return Array.Empty<BaseSettings>();
	}
}
