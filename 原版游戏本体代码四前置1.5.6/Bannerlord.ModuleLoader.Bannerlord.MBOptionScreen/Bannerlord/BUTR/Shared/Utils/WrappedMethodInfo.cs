using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Bannerlord.BUTR.Shared.Utils;

internal sealed class WrappedMethodInfo : MethodInfo
{
	private readonly object _instance;

	private readonly MethodInfo _methodInfoImplementation;

	public override MethodAttributes Attributes => _methodInfoImplementation.Attributes;

	public override CallingConventions CallingConvention => _methodInfoImplementation.CallingConvention;

	public override bool ContainsGenericParameters => _methodInfoImplementation.ContainsGenericParameters;

	public override IEnumerable<CustomAttributeData> CustomAttributes => _methodInfoImplementation.CustomAttributes;

	public override Type? DeclaringType => _methodInfoImplementation.DeclaringType;

	public override bool IsGenericMethod => _methodInfoImplementation.IsGenericMethod;

	public override bool IsGenericMethodDefinition => _methodInfoImplementation.IsGenericMethodDefinition;

	public override bool IsSecurityCritical => _methodInfoImplementation.IsSecurityCritical;

	public override bool IsSecuritySafeCritical => _methodInfoImplementation.IsSecuritySafeCritical;

	public override bool IsSecurityTransparent => _methodInfoImplementation.IsSecurityTransparent;

	public override MemberTypes MemberType => _methodInfoImplementation.MemberType;

	public override int MetadataToken => _methodInfoImplementation.MetadataToken;

	public override RuntimeMethodHandle MethodHandle => _methodInfoImplementation.MethodHandle;

	public override MethodImplAttributes MethodImplementationFlags => _methodInfoImplementation.MethodImplementationFlags;

	public override Module Module => _methodInfoImplementation.Module;

	public override string Name => _methodInfoImplementation.Name;

	public override Type? ReflectedType => _methodInfoImplementation.ReflectedType;

	public override ParameterInfo ReturnParameter => _methodInfoImplementation.ReturnParameter;

	public override Type ReturnType => _methodInfoImplementation.ReturnType;

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => _methodInfoImplementation.ReturnTypeCustomAttributes;

	public WrappedMethodInfo(MethodInfo actualMethodInfo, object instance)
	{
		_methodInfoImplementation = actualMethodInfo;
		_instance = instance;
	}

	public override Delegate CreateDelegate(Type delegateType)
	{
		return _methodInfoImplementation.CreateDelegate(delegateType);
	}

	public override Delegate CreateDelegate(Type delegateType, object? target)
	{
		return _methodInfoImplementation.CreateDelegate(delegateType, target);
	}

	public override MethodInfo GetBaseDefinition()
	{
		return _methodInfoImplementation.GetBaseDefinition();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _methodInfoImplementation.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _methodInfoImplementation.GetCustomAttributes(attributeType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return _methodInfoImplementation.GetCustomAttributesData();
	}

	public override Type[] GetGenericArguments()
	{
		return _methodInfoImplementation.GetGenericArguments();
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		return _methodInfoImplementation.GetGenericMethodDefinition();
	}

	public override MethodBody? GetMethodBody()
	{
		return _methodInfoImplementation.GetMethodBody();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return _methodInfoImplementation.GetMethodImplementationFlags();
	}

	public override ParameterInfo[] GetParameters()
	{
		return _methodInfoImplementation.GetParameters();
	}

	public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
	{
		return _methodInfoImplementation.Invoke(_instance, invokeAttr, binder, parameters, culture);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _methodInfoImplementation.IsDefined(attributeType, inherit);
	}

	public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
	{
		return _methodInfoImplementation.MakeGenericMethod(typeArguments);
	}

	public override string? ToString()
	{
		return _methodInfoImplementation.ToString();
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is WrappedMethodInfo wrappedMethodInfo))
		{
			if (obj is MethodInfo obj2)
			{
				return _methodInfoImplementation.Equals(obj2);
			}
			return _methodInfoImplementation.Equals(obj);
		}
		return _methodInfoImplementation.Equals(wrappedMethodInfo._methodInfoImplementation);
	}

	public override int GetHashCode()
	{
		return _methodInfoImplementation.GetHashCode();
	}
}
