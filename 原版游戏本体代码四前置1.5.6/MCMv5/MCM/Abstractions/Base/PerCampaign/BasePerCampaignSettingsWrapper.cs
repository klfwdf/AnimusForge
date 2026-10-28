using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using MCM.Common;

namespace MCM.Abstractions.Base.PerCampaign;

[Obsolete("Will be removed from future API", true)]
public abstract class BasePerCampaignSettingsWrapper : PerCampaignSettings, IWrapper
{
	private delegate string GetIdDelegate();

	private delegate string GetFolderNameDelegate();

	private delegate string GetDisplayNameDelegate();

	private delegate int GetUIVersionDelegate();

	private delegate string GetSubFolderDelegate();

	private delegate char GetSubGroupDelimiterDelegate();

	private delegate void OnPropertyChangedDelegate(string? propertyName);

	private readonly GetIdDelegate? _getIdDelegate;

	private readonly GetFolderNameDelegate? _getFolderNameDelegate;

	private readonly GetDisplayNameDelegate? _getDisplayNameDelegate;

	private readonly GetUIVersionDelegate? _getUIVersionDelegate;

	private readonly GetSubFolderDelegate? _getSubFolderDelegate;

	private readonly GetSubGroupDelimiterDelegate? _getSubGroupDelimiterDelegate;

	private readonly OnPropertyChangedDelegate? _methodOnPropertyChangedDelegate;

	public object Object { get; protected set; }

	public override string Id => _getIdDelegate?.Invoke() ?? "ERROR";

	public override string FolderName => _getFolderNameDelegate?.Invoke() ?? string.Empty;

	public override string DisplayName => _getDisplayNameDelegate?.Invoke() ?? "ERROR";

	public override int UIVersion => _getUIVersionDelegate?.Invoke() ?? 1;

	public override string SubFolder => _getSubFolderDelegate?.Invoke() ?? string.Empty;

	public override char SubGroupDelimiter => _getSubGroupDelimiterDelegate?.Invoke() ?? '/';

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

	protected BasePerCampaignSettingsWrapper(object @object)
	{
		Object = @object;
		Type type = @object.GetType();
		_getIdDelegate = AccessTools2.GetPropertyGetterDelegate<GetIdDelegate>(@object, type, "Id");
		_getFolderNameDelegate = AccessTools2.GetPropertyGetterDelegate<GetFolderNameDelegate>(@object, type, "FolderName");
		_getDisplayNameDelegate = AccessTools2.GetPropertyGetterDelegate<GetDisplayNameDelegate>(@object, type, "DisplayName");
		_getUIVersionDelegate = AccessTools2.GetPropertyGetterDelegate<GetUIVersionDelegate>(@object, type, "UIVersion");
		_getSubFolderDelegate = AccessTools2.GetPropertyGetterDelegate<GetSubFolderDelegate>(@object, type, "SubFolder");
		_getSubGroupDelimiterDelegate = AccessTools2.GetPropertyGetterDelegate<GetSubGroupDelimiterDelegate>(@object, type, "SubGroupDelimiter");
		_methodOnPropertyChangedDelegate = AccessTools2.GetDelegate<OnPropertyChangedDelegate>(@object, type, "OnPropertyChanged");
	}

	public override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		_methodOnPropertyChangedDelegate?.Invoke(propertyName);
	}
}
