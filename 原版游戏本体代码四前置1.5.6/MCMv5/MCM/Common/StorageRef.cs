using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MCM.Common;

public sealed class StorageRef : IRef, INotifyPropertyChanged
{
	private object? _value;

	public Type Type { get; }

	public object? Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!object.Equals(_value, value))
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public StorageRef(object? value)
	{
		_value = value;
		Type = value?.GetType() ?? throw new Exception("Value can't be null!");
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
public sealed class StorageRef<T> : IRef, INotifyPropertyChanged
{
	private T? _value;

	public Type Type { get; }

	public object? Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value is T val && !object.Equals(_value, val))
			{
				_value = val;
				OnPropertyChanged("Value");
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public StorageRef(T? value)
	{
		_value = value;
		object obj = value?.GetType();
		if (obj == null)
		{
			obj = typeof(T);
		}
		Type = (Type)obj;
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
