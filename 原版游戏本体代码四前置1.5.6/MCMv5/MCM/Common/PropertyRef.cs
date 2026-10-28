using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MCM.Common;

public class PropertyRef : IRef, INotifyPropertyChanged, IEquatable<PropertyRef>
{
	public PropertyInfo PropertyInfo { get; }

	public object Instance { get; }

	public Type Type => PropertyInfo.PropertyType;

	public object? Value
	{
		get
		{
			return PropertyInfo.GetValue(Instance);
		}
		set
		{
			if (PropertyInfo.CanWrite)
			{
				PropertyInfo.SetValue(Instance, value);
				OnPropertyChanged("Value");
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public PropertyRef(PropertyInfo propInfo, object instance)
	{
		PropertyInfo = propInfo;
		Instance = instance;
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public bool Equals(PropertyRef? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		return PropertyInfo.Equals(other.PropertyInfo) && Instance.Equals(other.Instance);
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
		return Equals((PropertyRef)obj);
	}

	public override int GetHashCode()
	{
		int num = 269;
		num = num * 47 + PropertyInfo.GetHashCode();
		return num * 47 + Instance.GetHashCode();
	}

	public static bool operator ==(PropertyRef? left, PropertyRef? right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(PropertyRef? left, PropertyRef? right)
	{
		return !object.Equals(left, right);
	}
}
