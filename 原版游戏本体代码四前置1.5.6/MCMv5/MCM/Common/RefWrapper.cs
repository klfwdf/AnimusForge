using System;
using System.ComponentModel;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Common;

public class RefWrapper : IRef, INotifyPropertyChanged, IWrapper
{
	private delegate Type GetTypeDelegate();

	private delegate object GetValueDelegate();

	private delegate void SetValueDelegate(object value);

	private readonly GetTypeDelegate? _getTypeDelegate;

	private readonly GetValueDelegate? _getValueDelegate;

	private readonly SetValueDelegate? _setValueDelegate;

	public object Object { get; }

	public Type Type => _getTypeDelegate();

	public object? Value
	{
		get
		{
			return _getValueDelegate();
		}
		set
		{
			if (_setValueDelegate != null && value != null)
			{
				_setValueDelegate(value);
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged
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

	public RefWrapper(object @object)
	{
		Object = @object;
		Type type = @object.GetType();
		_getTypeDelegate = AccessTools2.GetPropertyGetterDelegate<GetTypeDelegate>(@object, type, "Type");
		_getValueDelegate = AccessTools2.GetPropertyGetterDelegate<GetValueDelegate>(@object, type, "Value");
		_setValueDelegate = AccessTools2.GetPropertySetterDelegate<SetValueDelegate>(@object, type, "Value");
	}
}
