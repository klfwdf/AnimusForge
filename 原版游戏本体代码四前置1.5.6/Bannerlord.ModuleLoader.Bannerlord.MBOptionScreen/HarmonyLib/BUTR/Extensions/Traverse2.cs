using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions;

internal class Traverse2<T>
{
	private readonly Traverse2 _traverse;

	public T? Value
	{
		get
		{
			return _traverse.GetValue<T>();
		}
		set
		{
			_traverse.SetValue(value);
		}
	}

	private Traverse2()
	{
		_traverse = new Traverse2(null);
	}

	public Traverse2(Traverse2 traverse)
	{
		_traverse = traverse;
	}
}
internal class Traverse2
{
	private static readonly AccessCacheHandle? Cache;

	private readonly Type? _type;

	private readonly object? _root;

	private readonly MemberInfo? _info;

	private readonly MethodBase? _method;

	private readonly object[]? _params;

	public static Action<Traverse2, Traverse2> CopyFields;

	[MethodImpl(MethodImplOptions.Synchronized)]
	static Traverse2()
	{
		CopyFields = delegate(Traverse2 from, Traverse2 to)
		{
			if (from != null)
			{
				to?.SetValue(from.GetValue());
			}
		};
		if (!Cache.HasValue)
		{
			Cache = AccessCacheHandle.Create();
		}
	}

	public static Traverse2 Create(Type? type)
	{
		return new Traverse2(type);
	}

	public static Traverse2 Create<T>()
	{
		return Create(typeof(T));
	}

	public static Traverse2 Create(object? root)
	{
		return new Traverse2(root);
	}

	public static Traverse2 CreateWithType(string name)
	{
		return new Traverse2(AccessTools2.TypeByName(name));
	}

	private Traverse2()
	{
	}

	public Traverse2(Type? type)
	{
		_type = type;
	}

	public Traverse2(object? root)
	{
		_root = root;
		_type = root?.GetType();
	}

	private Traverse2(object? root, MemberInfo info, object[]? index)
	{
		_root = root;
		_type = root?.GetType() ?? info.GetUnderlyingType();
		_info = info;
		_params = index;
	}

	private Traverse2(object? root, MethodInfo method, object[]? parameter)
	{
		_root = root;
		_type = method.ReturnType;
		_method = method;
		_params = parameter;
	}

	public object? GetValue()
	{
		if (_info is FieldInfo fieldInfo)
		{
			return fieldInfo.GetValue(_root);
		}
		if (_info is PropertyInfo propertyInfo)
		{
			return propertyInfo.GetValue(_root, AccessTools.all, null, _params, CultureInfo.CurrentCulture);
		}
		MethodBase method = _method;
		if ((object)method != null)
		{
			return method.Invoke(_root, _params);
		}
		if (_root == null && (object)_type != null)
		{
			return _type;
		}
		return _root;
	}

	public T? GetValue<T>()
	{
		object value = GetValue();
		if (value is T)
		{
			return (T)value;
		}
		return default(T);
	}

	public object? GetValue(params object[] arguments)
	{
		return _method?.Invoke(_root, arguments);
	}

	public T? GetValue<T>(params object[] arguments)
	{
		object obj = _method?.Invoke(_root, arguments);
		if (obj is T)
		{
			return (T)obj;
		}
		return default(T);
	}

	public Traverse2 SetValue(object value)
	{
		if (_info is FieldInfo fieldInfo && ((_root == null && fieldInfo.IsStatic) || _root != null))
		{
			fieldInfo.SetValue(_root, value, AccessTools.all, null, CultureInfo.CurrentCulture);
		}
		if (_info is PropertyInfo { SetMethod: not null } propertyInfo && ((_root == null && propertyInfo.SetMethod.IsStatic) || _root != null))
		{
			propertyInfo.SetValue(_root, value, AccessTools.all, null, _params, CultureInfo.CurrentCulture);
		}
		return this;
	}

	public Type? GetValueType()
	{
		if (_info is FieldInfo fieldInfo)
		{
			return fieldInfo.FieldType;
		}
		if (_info is PropertyInfo propertyInfo)
		{
			return propertyInfo.PropertyType;
		}
		return null;
	}

	private Traverse2 Resolve()
	{
		if (_root == null)
		{
			if (_info is FieldInfo { IsStatic: not false })
			{
				return new Traverse2(GetValue());
			}
			if (_info is PropertyInfo propertyInfo && propertyInfo.GetGetMethod().IsStatic)
			{
				return new Traverse2(GetValue());
			}
			MethodBase method = _method;
			if ((object)method != null && method.IsStatic)
			{
				return new Traverse2(GetValue());
			}
			if ((object)_type != null)
			{
				return this;
			}
		}
		return new Traverse2(GetValue());
	}

	public Traverse2 Type(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Traverse2();
		}
		if ((object)_type == null)
		{
			return new Traverse2();
		}
		Type type = AccessTools.Inner(_type, name);
		if ((object)type == null)
		{
			return new Traverse2();
		}
		return new Traverse2(type);
	}

	public Traverse2 Field(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Traverse2();
		}
		Traverse2 traverse = Resolve();
		if ((object)traverse._type == null)
		{
			return new Traverse2();
		}
		FieldInfo fieldInfo = Cache?.GetFieldInfo(traverse._type, name);
		if ((object)fieldInfo == null)
		{
			return new Traverse2();
		}
		if (!fieldInfo.IsStatic && traverse._root == null)
		{
			return new Traverse2();
		}
		return new Traverse2(traverse._root, fieldInfo, null);
	}

	public Traverse2<T> Field<T>(string name)
	{
		return new Traverse2<T>(Field(name));
	}

	public List<string> Fields()
	{
		return AccessTools.GetFieldNames(Resolve()._type);
	}

	public Traverse2 Property(string name, object[]? index = null)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Traverse2();
		}
		Traverse2 traverse = Resolve();
		if ((object)traverse._type == null)
		{
			return new Traverse2();
		}
		PropertyInfo propertyInfo = Cache?.GetPropertyInfo(traverse._type, name);
		if ((object)propertyInfo == null)
		{
			return new Traverse2();
		}
		return new Traverse2(traverse._root, propertyInfo, index);
	}

	public Traverse2<T> Property<T>(string name, object[]? index = null)
	{
		return new Traverse2<T>(Property(name, index));
	}

	public List<string> Properties()
	{
		return AccessTools.GetPropertyNames(Resolve()._type);
	}

	public Traverse2 Method(string name, params object[] arguments)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Traverse2();
		}
		Traverse2 traverse = Resolve();
		if ((object)traverse._type == null)
		{
			return new Traverse2();
		}
		Type[] types = AccessTools.GetTypes(arguments);
		if (!(Cache?.GetMethodInfo(traverse._type, name, types) is MethodInfo method))
		{
			return new Traverse2();
		}
		return new Traverse2(traverse._root, method, arguments);
	}

	public Traverse2 Method(string name, Type[] paramTypes, object[]? arguments = null)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Traverse2();
		}
		Traverse2 traverse = Resolve();
		if ((object)traverse._type == null)
		{
			return new Traverse2();
		}
		if (!(Cache?.GetMethodInfo(traverse._type, name, paramTypes) is MethodInfo method))
		{
			return new Traverse2();
		}
		return new Traverse2(traverse._root, method, arguments);
	}

	public List<string> Methods()
	{
		return AccessTools.GetMethodNames(Resolve()._type);
	}

	public bool FieldExists()
	{
		return _info is FieldInfo;
	}

	public bool PropertyExists()
	{
		return _info is PropertyInfo;
	}

	public bool MethodExists()
	{
		return (object)_method != null;
	}

	public bool TypeExists()
	{
		return (object)_type != null;
	}

	public static void IterateFields(object source, Action<Traverse2> action)
	{
		if (action != null)
		{
			Traverse2 sourceTrv = Create(source);
			AccessTools.GetFieldNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Field(f));
			});
		}
	}

	public static void IterateFields(object source, object target, Action<Traverse2, Traverse2> action)
	{
		if (action != null)
		{
			Traverse2 sourceTrv = Create(source);
			Traverse2 targetTrv = Create(target);
			AccessTools.GetFieldNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Field(f), targetTrv.Field(f));
			});
		}
	}

	public static void IterateFields(object source, object target, Action<string, Traverse2, Traverse2> action)
	{
		if (action != null)
		{
			Traverse2 sourceTrv = Create(source);
			Traverse2 targetTrv = Create(target);
			AccessTools.GetFieldNames(source).ForEach(delegate(string f)
			{
				action(f, sourceTrv.Field(f), targetTrv.Field(f));
			});
		}
	}

	public static void IterateProperties(object source, Action<Traverse2> action)
	{
		if (action != null)
		{
			Traverse2 sourceTrv = Create(source);
			AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Property(f));
			});
		}
	}

	public static void IterateProperties(object source, object target, Action<Traverse2, Traverse2> action)
	{
		if (action != null)
		{
			Traverse2 sourceTrv = Create(source);
			Traverse2 targetTrv = Create(target);
			AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Property(f), targetTrv.Property(f));
			});
		}
	}

	public static void IterateProperties(object source, object target, Action<string, Traverse2, Traverse2> action)
	{
		if (action != null)
		{
			Traverse2 sourceTrv = Create(source);
			Traverse2 targetTrv = Create(target);
			AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
			{
				action(f, sourceTrv.Property(f), targetTrv.Property(f));
			});
		}
	}

	public override string? ToString()
	{
		return (_method ?? GetValue())?.ToString();
	}
}
