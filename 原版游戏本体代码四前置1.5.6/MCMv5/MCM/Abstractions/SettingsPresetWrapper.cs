using System;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions.Base;
using MCM.Common;

namespace MCM.Abstractions;

public abstract class SettingsPresetWrapper<TSetting> : ISettingsPreset, IWrapper where TSetting : BaseSettings, IWrapper
{
	private delegate string GetSettingsIdDelegate();

	private delegate string GetIdDelegate();

	private delegate string GetNameDelegate();

	private delegate object LoadPresetDelegate();

	private delegate bool SavePresetDelegate(object settings);

	private readonly GetSettingsIdDelegate? _methodGetSettingsIdDelegate;

	private readonly GetIdDelegate? _methodGetIdDelegate;

	private readonly GetNameDelegate? _methodGetNameDelegate;

	private readonly LoadPresetDelegate? _methodLoadPresetDelegate;

	private readonly SavePresetDelegate? _methodSavePresetDelegate;

	public string SettingsId => _methodGetSettingsIdDelegate?.Invoke() ?? "ERROR";

	public string Id => _methodGetIdDelegate?.Invoke() ?? "ERROR";

	public string Name => _methodGetNameDelegate?.Invoke() ?? "ERROR";

	public object? Object { get; }

	protected SettingsPresetWrapper(object? @object)
	{
		if (@object != null)
		{
			Object = @object;
			Type type = @object.GetType();
			_methodGetSettingsIdDelegate = AccessTools2.GetPropertyGetterDelegate<GetSettingsIdDelegate>(@object, type, "SettingsId");
			_methodGetIdDelegate = AccessTools2.GetPropertyGetterDelegate<GetIdDelegate>(@object, type, "Id");
			_methodGetNameDelegate = AccessTools2.GetPropertyGetterDelegate<GetNameDelegate>(@object, type, "Name");
			_methodLoadPresetDelegate = AccessTools2.GetDelegate<LoadPresetDelegate>(@object, type, "LoadPreset");
			_methodSavePresetDelegate = AccessTools2.GetDelegate<SavePresetDelegate>(@object, type, "SavePreset");
		}
	}

	protected abstract TSetting Create(object? @object);

	public BaseSettings LoadPreset()
	{
		return Create(_methodLoadPresetDelegate?.Invoke());
	}

	public bool SavePreset(BaseSettings settings)
	{
		int result;
		if (settings is TSetting val)
		{
			object obj = val.Object;
			if (obj != null)
			{
				result = ((_methodSavePresetDelegate?.Invoke(obj) ?? false) ? 1 : 0);
				goto IL_0039;
			}
		}
		result = 0;
		goto IL_0039;
		IL_0039:
		return (byte)result != 0;
	}
}
