using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Bannerlord.BUTR.Shared.Utils;

internal sealed class WrappedPropertyInfo : PropertyInfo, INotifyPropertyChanged
{
	private readonly object _instance;

	private readonly PropertyInfo _propertyInfoImplementation;

	public override PropertyAttributes Attributes => _propertyInfoImplementation.Attributes;

	public override bool CanRead => _propertyInfoImplementation.CanRead;

	public override bool CanWrite => _propertyInfoImplementation.CanWrite;

	public override IEnumerable<CustomAttributeData> CustomAttributes => _propertyInfoImplementation.CustomAttributes;

	public override Type? DeclaringType => _propertyInfoImplementation.DeclaringType;

	public override MethodInfo? GetMethod => _propertyInfoImplementation.GetMethod;

	public override MemberTypes MemberType => _propertyInfoImplementation.MemberType;

	public override int MetadataToken => _propertyInfoImplementation.MetadataToken;

	public override Module Module => _propertyInfoImplementation.Module;

	public override string Name => _propertyInfoImplementation.Name;

	public override Type PropertyType => _propertyInfoImplementation.PropertyType;

	public override Type? ReflectedType => _propertyInfoImplementation.ReflectedType;

	public override MethodInfo? SetMethod => _propertyInfoImplementation.SetMethod;

	public event PropertyChangedEventHandler? PropertyChanged;

	public WrappedPropertyInfo(PropertyInfo actualPropertyInfo, object instance)
	{
		_propertyInfoImplementation = actualPropertyInfo;
		_instance = instance;
	}

	public override MethodInfo[] GetAccessors(bool nonPublic)
	{
		return (from m in _propertyInfoImplementation.GetAccessors(nonPublic)
			select new Bannerlord.BUTR.Shared.Utils.WrappedMethodInfo(m, _instance)).Cast<MethodInfo>().ToArray();
	}

	public override object? GetConstantValue()
	{
		return _propertyInfoImplementation.GetConstantValue();
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _propertyInfoImplementation.GetCustomAttributes(attributeType, inherit);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _propertyInfoImplementation.GetCustomAttributes(inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return _propertyInfoImplementation.GetCustomAttributesData();
	}

	public override MethodInfo GetGetMethod(bool nonPublic)
	{
		MethodInfo getMethod = _propertyInfoImplementation.GetGetMethod(nonPublic);
		if ((object)getMethod != null)
		{
			return new Bannerlord.BUTR.Shared.Utils.WrappedMethodInfo(getMethod, _instance);
		}
		return null;
	}

	public override ParameterInfo[] GetIndexParameters()
	{
		return _propertyInfoImplementation.GetIndexParameters();
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return _propertyInfoImplementation.GetOptionalCustomModifiers();
	}

	public override object? GetRawConstantValue()
	{
		return _propertyInfoImplementation.GetRawConstantValue();
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return _propertyInfoImplementation.GetRequiredCustomModifiers();
	}

	public override MethodInfo GetSetMethod(bool nonPublic)
	{
		MethodInfo setMethod = _propertyInfoImplementation.GetSetMethod(nonPublic);
		if ((object)setMethod != null)
		{
			return new Bannerlord.BUTR.Shared.Utils.WrappedMethodInfo(setMethod, _instance);
		}
		return null;
	}

	public override object? GetValue(object? obj, object?[]? index)
	{
		return _propertyInfoImplementation.GetValue(_instance, index);
	}

	public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
	{
		return _propertyInfoImplementation.GetValue(_instance, invokeAttr, binder, index, culture);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _propertyInfoImplementation.IsDefined(attributeType, inherit);
	}

	public override void SetValue(object? obj, object? value, object?[]? index)
	{
		_propertyInfoImplementation.SetValue(_instance, value, index);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyInfoImplementation.Name));
	}

	public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
	{
		_propertyInfoImplementation.SetValue(_instance, value, invokeAttr, binder, index, culture);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyInfoImplementation.Name));
	}

	public override string? ToString()
	{
		return _propertyInfoImplementation.ToString();
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is Bannerlord.BUTR.Shared.Utils.WrappedPropertyInfo wrappedPropertyInfo))
		{
			if (obj is PropertyInfo obj2)
			{
				return _propertyInfoImplementation.Equals(obj2);
			}
			return _propertyInfoImplementation.Equals(obj);
		}
		return _propertyInfoImplementation.Equals(wrappedPropertyInfo._propertyInfoImplementation);
	}

	public override int GetHashCode()
	{
		return _propertyInfoImplementation.GetHashCode();
	}
}
