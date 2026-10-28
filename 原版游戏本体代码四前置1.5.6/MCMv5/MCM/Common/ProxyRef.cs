using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MCM.Common;

public class ProxyRef<T> : IRef, INotifyPropertyChanged, IEquatable<ProxyRef<T>>
{
	private readonly Func<T> _getter;

	private readonly Action<T>? _setter;

	public Type Type => typeof(T);

	public object? Value
	{
		get
		{
			return _getter();
		}
		set
		{
			if (_setter != null && value is T obj)
			{
				_setter(obj);
				OnPropertyChanged("Value");
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public ProxyRef(Func<T> getter, Action<T>? setter)
	{
		_getter = getter ?? throw new ArgumentNullException("getter");
		_setter = setter;
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public bool Equals(ProxyRef<T>? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		return _getter.Equals(other._getter) && object.Equals(_setter, other._setter);
	}

	public override bool Equals(object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((ProxyRef<T>)obj);
	}

	public override int GetHashCode()
	{
		int num = 269;
		num = num * 47 + _getter.GetHashCode();
		if (_setter != null)
		{
			num = num * 47 + _setter.GetHashCode();
		}
		return num;
	}

	public static bool operator ==(ProxyRef<T>? left, ProxyRef<T>? right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(ProxyRef<T>? left, ProxyRef<T>? right)
	{
		return !object.Equals(left, right);
	}
}
