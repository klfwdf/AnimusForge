using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using MCM.Common;

namespace MCM.Abstractions.Base.Global;

public abstract class SettingsWrapper : BaseSettings, IWrapper
{
	private delegate string GetIdDelegate();

	private delegate string GetFolderNameDelegate();

	private delegate string GetDisplayNameDelegate();

	private delegate int GetUIVersionDelegate();

	private delegate string GetSubFolderDelegate();

	private delegate char GetSubGroupDelimiterDelegate();

	private delegate string GetFormatDelegate();

	private delegate void OnPropertyChangedDelegate(string? propertyName);

	private delegate IEnumerable? GetBuiltInPresetsDelegate();

	private delegate object CreateNewDelegate();

	private delegate object CopyAsNewDelegate();

	private readonly GetIdDelegate? _getIdDelegate;

	private readonly GetFolderNameDelegate? _getFolderNameDelegate;

	private readonly GetDisplayNameDelegate? _getDisplayNameDelegate;

	private readonly GetUIVersionDelegate? _getUIVersionDelegate;

	private readonly GetSubFolderDelegate? _getSubFolderDelegate;

	private readonly GetSubGroupDelimiterDelegate? _getSubGroupDelimiterDelegate;

	private readonly GetFormatDelegate? _getFormatDelegate;

	private readonly OnPropertyChangedDelegate? _methodOnPropertyChangedDelegate;

	private readonly GetBuiltInPresetsDelegate? _methodGetBuiltInPresetsDelegate;

	private readonly CreateNewDelegate? _methodCreateNewDelegate;

	private readonly CopyAsNewDelegate? _methodCopyAsNewDelegate;

	public override string Id => _getIdDelegate?.Invoke() ?? "ERROR";

	public override string FolderName => _getFolderNameDelegate?.Invoke() ?? string.Empty;

	public override string DisplayName => _getDisplayNameDelegate?.Invoke() ?? "ERROR";

	public override int UIVersion => _getUIVersionDelegate?.Invoke() ?? 1;

	public override string SubFolder => _getSubFolderDelegate?.Invoke() ?? string.Empty;

	public override char SubGroupDelimiter => _getSubGroupDelimiterDelegate?.Invoke() ?? '/';

	public override string FormatType => _getFormatDelegate?.Invoke() ?? "none";

	public object? Object { get; }

	public override event PropertyChangedEventHandler? PropertyChanged
	{
		add
		{
			if (Object is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged += value;
			}
		}
		remove
		{
			if (Object is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged -= value;
			}
		}
	}

	protected SettingsWrapper(object? @object)
	{
		if (@object != null)
		{
			Object = @object;
			Type type = @object.GetType();
			_getIdDelegate = AccessTools2.GetPropertyGetterDelegate<GetIdDelegate>(@object, type, "Id");
			_getFolderNameDelegate = AccessTools2.GetPropertyGetterDelegate<GetFolderNameDelegate>(@object, type, "FolderName");
			_getDisplayNameDelegate = AccessTools2.GetPropertyGetterDelegate<GetDisplayNameDelegate>(@object, type, "DisplayName");
			_getUIVersionDelegate = AccessTools2.GetPropertyGetterDelegate<GetUIVersionDelegate>(@object, type, "UIVersion");
			_getSubFolderDelegate = AccessTools2.GetPropertyGetterDelegate<GetSubFolderDelegate>(@object, type, "SubFolder");
			_getSubGroupDelimiterDelegate = AccessTools2.GetPropertyGetterDelegate<GetSubGroupDelimiterDelegate>(@object, type, "SubGroupDelimiter");
			_getFormatDelegate = AccessTools2.GetPropertyGetterDelegate<GetFormatDelegate>(@object, type, "FormatType");
			_methodOnPropertyChangedDelegate = AccessTools2.GetDelegate<OnPropertyChangedDelegate>(@object, type, "OnPropertyChanged");
			_methodGetBuiltInPresetsDelegate = AccessTools2.GetDelegate<GetBuiltInPresetsDelegate>(@object, type, "GetBuiltInPresets");
			_methodCreateNewDelegate = AccessTools2.GetDelegate<CreateNewDelegate>(@object, type, "CreateNew");
			_methodCopyAsNewDelegate = AccessTools2.GetDelegate<CopyAsNewDelegate>(@object, type, "CopyAsNew");
		}
	}

	protected abstract BaseSettings Create(object? @object);

	protected abstract ISettingsPreset CreatePreset(object? @object);

	public override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		_methodOnPropertyChangedDelegate?.Invoke(propertyName);
	}

	public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
	{
		return _methodGetBuiltInPresetsDelegate?.Invoke()?.Cast<object>().Select(CreatePreset).OfType<ISettingsPreset>() ?? Array.Empty<ISettingsPreset>();
	}

	public override BaseSettings CreateNew()
	{
		return Create(_methodCreateNewDelegate?.Invoke());
	}

	public override BaseSettings CopyAsNew()
	{
		return Create(_methodCopyAsNewDelegate?.Invoke());
	}
}
