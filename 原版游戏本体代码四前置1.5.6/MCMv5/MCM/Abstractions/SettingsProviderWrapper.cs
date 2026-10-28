using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Abstractions;

public abstract class SettingsProviderWrapper : BaseSettingsProvider, IWrapper
{
	private delegate IEnumerable GetSettingsDefinitionsDelegate();

	private delegate object? GetSettingsDelegate(string id);

	private delegate void SaveSettingsDelegate(object settings);

	private delegate void OverrideSettingsDelegate(object settings);

	private delegate void ResetSettingsDelegate(object settings);

	private readonly GetSettingsDefinitionsDelegate? _methodGetSettingsDefinitions;

	private readonly GetSettingsDelegate? _methodGetSettingsDelegate;

	private readonly SaveSettingsDelegate? _methodSaveSettingsDelegate;

	private readonly OverrideSettingsDelegate? _methodOverrideSettingsDelegate;

	private readonly ResetSettingsDelegate? _methodResetSettingsDelegate;

	public override IEnumerable<SettingsDefinition> SettingsDefinitions => _methodGetSettingsDefinitions?.Invoke().Cast<object>().Select((object x) => new SettingsDefinitionWrapper(x)) ?? Array.Empty<SettingsDefinitionWrapper>();

	public object Object { get; }

	protected SettingsProviderWrapper(object @object)
	{
		Object = @object;
		Type type = @object.GetType();
		_methodGetSettingsDefinitions = AccessTools2.GetPropertyGetterDelegate<GetSettingsDefinitionsDelegate>(@object, type, "SettingsDefinitions");
		_methodGetSettingsDelegate = AccessTools2.GetDelegate<GetSettingsDelegate>(@object, type, "GetSettings");
		_methodSaveSettingsDelegate = AccessTools2.GetDelegate<SaveSettingsDelegate>(@object, type, "SaveSettings");
		_methodOverrideSettingsDelegate = AccessTools2.GetDelegate<OverrideSettingsDelegate>(@object, type, "OverrideSettings");
		_methodResetSettingsDelegate = AccessTools2.GetDelegate<ResetSettingsDelegate>(@object, type, "ResetSettings");
	}

	protected abstract BaseSettings? Create(object obj);

	protected abstract bool IsSettings(BaseSettings settings, [NotNullWhen(true)] out object? wrapped);

	public override BaseSettings? GetSettings(string id)
	{
		object obj = _methodGetSettingsDelegate?.Invoke(id);
		return (obj != null) ? Create(obj) : null;
	}

	public override void SaveSettings(BaseSettings settings)
	{
		if (IsSettings(settings, out object wrapped))
		{
			_methodSaveSettingsDelegate?.Invoke(wrapped);
		}
	}

	public override void OverrideSettings(BaseSettings settings)
	{
		if (IsSettings(settings, out object wrapped))
		{
			_methodOverrideSettingsDelegate?.Invoke(wrapped);
		}
	}

	public override void ResetSettings(BaseSettings settings)
	{
		if (IsSettings(settings, out object wrapped))
		{
			_methodResetSettingsDelegate?.Invoke(wrapped);
		}
	}

	public abstract override IEnumerable<ISettingsPreset> GetPresets(string id);
}
