using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TaleWorlds.Library;

public abstract class ViewModel : IViewModel, INotifyPropertyChanged
{
	public interface IViewModelGetterInterface
	{
		bool IsValueSynced(string name);

		Type GetPropertyType(string name);

		object GetPropertyValue(string name);

		void OnFinalize();
	}

	public interface IViewModelSetterInterface
	{
		void SetPropertyValue(string name, object value);

		void OnFinalize();
	}

	private class DataSourceTypeBindingPropertiesCollection
	{
		public Dictionary<string, PropertyInfo> Properties { get; set; }

		public Dictionary<string, MethodInfo> Methods { get; set; }

		public DataSourceTypeBindingPropertiesCollection(Dictionary<string, PropertyInfo> properties, Dictionary<string, MethodInfo> methods)
		{
			Properties = properties;
			Methods = methods;
		}
	}

	public static bool UIDebugMode;

	private List<PropertyChangedEventHandler> _eventHandlers;

	private List<PropertyChangedWithValueEventHandler> _eventHandlersWithValue;

	private List<PropertyChangedWithBoolValueEventHandler> _eventHandlersWithBoolValue;

	private List<PropertyChangedWithIntValueEventHandler> _eventHandlersWithIntValue;

	private List<PropertyChangedWithFloatValueEventHandler> _eventHandlersWithFloatValue;

	private List<PropertyChangedWithUIntValueEventHandler> _eventHandlersWithUIntValue;

	private List<PropertyChangedWithColorValueEventHandler> _eventHandlersWithColorValue;

	private List<PropertyChangedWithDoubleValueEventHandler> _eventHandlersWithDoubleValue;

	private List<PropertyChangedWithVec2ValueEventHandler> _eventHandlersWithVec2Value;

	private Type _type;

	private DataSourceTypeBindingPropertiesCollection _propertiesAndMethods;

	private static Dictionary<Type, DataSourceTypeBindingPropertiesCollection> _cachedViewModelProperties = new Dictionary<Type, DataSourceTypeBindingPropertiesCollection>();

	public event PropertyChangedEventHandler PropertyChanged
	{
		add
		{
			if (_eventHandlers == null)
			{
				_eventHandlers = new List<PropertyChangedEventHandler>();
			}
			_eventHandlers.Add(value);
		}
		remove
		{
			if (_eventHandlers != null)
			{
				_eventHandlers.Remove(value);
			}
		}
	}

	public event PropertyChangedWithValueEventHandler PropertyChangedWithValue
	{
		add
		{
			if (_eventHandlersWithValue == null)
			{
				_eventHandlersWithValue = new List<PropertyChangedWithValueEventHandler>();
			}
			_eventHandlersWithValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithValue != null)
			{
				_eventHandlersWithValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithBoolValueEventHandler PropertyChangedWithBoolValue
	{
		add
		{
			if (_eventHandlersWithBoolValue == null)
			{
				_eventHandlersWithBoolValue = new List<PropertyChangedWithBoolValueEventHandler>();
			}
			_eventHandlersWithBoolValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithBoolValue != null)
			{
				_eventHandlersWithBoolValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithIntValueEventHandler PropertyChangedWithIntValue
	{
		add
		{
			if (_eventHandlersWithIntValue == null)
			{
				_eventHandlersWithIntValue = new List<PropertyChangedWithIntValueEventHandler>();
			}
			_eventHandlersWithIntValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithIntValue != null)
			{
				_eventHandlersWithIntValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithFloatValueEventHandler PropertyChangedWithFloatValue
	{
		add
		{
			if (_eventHandlersWithFloatValue == null)
			{
				_eventHandlersWithFloatValue = new List<PropertyChangedWithFloatValueEventHandler>();
			}
			_eventHandlersWithFloatValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithFloatValue != null)
			{
				_eventHandlersWithFloatValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithUIntValueEventHandler PropertyChangedWithUIntValue
	{
		add
		{
			if (_eventHandlersWithUIntValue == null)
			{
				_eventHandlersWithUIntValue = new List<PropertyChangedWithUIntValueEventHandler>();
			}
			_eventHandlersWithUIntValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithUIntValue != null)
			{
				_eventHandlersWithUIntValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithColorValueEventHandler PropertyChangedWithColorValue
	{
		add
		{
			if (_eventHandlersWithColorValue == null)
			{
				_eventHandlersWithColorValue = new List<PropertyChangedWithColorValueEventHandler>();
			}
			_eventHandlersWithColorValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithColorValue != null)
			{
				_eventHandlersWithColorValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithDoubleValueEventHandler PropertyChangedWithDoubleValue
	{
		add
		{
			if (_eventHandlersWithDoubleValue == null)
			{
				_eventHandlersWithDoubleValue = new List<PropertyChangedWithDoubleValueEventHandler>();
			}
			_eventHandlersWithDoubleValue.Add(value);
		}
		remove
		{
			if (_eventHandlersWithDoubleValue != null)
			{
				_eventHandlersWithDoubleValue.Remove(value);
			}
		}
	}

	public event PropertyChangedWithVec2ValueEventHandler PropertyChangedWithVec2Value
	{
		add
		{
			if (_eventHandlersWithVec2Value == null)
			{
				_eventHandlersWithVec2Value = new List<PropertyChangedWithVec2ValueEventHandler>();
			}
			_eventHandlersWithVec2Value.Add(value);
		}
		remove
		{
			if (_eventHandlersWithVec2Value != null)
			{
				_eventHandlersWithVec2Value.Remove(value);
			}
		}
	}

	protected ViewModel()
	{
		_type = GetType();
		_cachedViewModelProperties.TryGetValue(_type, out var value);
		if (value == null)
		{
			_propertiesAndMethods = GetPropertiesOfType(_type);
			_cachedViewModelProperties.Add(_type, _propertiesAndMethods);
		}
		else
		{
			_propertiesAndMethods = value;
		}
	}

	private PropertyInfo GetProperty(string name)
	{
		if (_propertiesAndMethods != null && _propertiesAndMethods.Properties.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	protected bool SetField<T>(ref T field, T value, string propertyName)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		if (_eventHandlers != null)
		{
			for (int i = 0; i < _eventHandlers.Count; i++)
			{
				PropertyChangedEventHandler propertyChangedEventHandler = _eventHandlers[i];
				PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
				propertyChangedEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue<T>(T value, [CallerMemberName] string propertyName = null) where T : class
	{
		if (_eventHandlersWithValue != null)
		{
			for (int i = 0; i < _eventHandlersWithValue.Count; i++)
			{
				PropertyChangedWithValueEventHandler propertyChangedWithValueEventHandler = _eventHandlersWithValue[i];
				PropertyChangedWithValueEventArgs e = new PropertyChangedWithValueEventArgs(propertyName, value);
				propertyChangedWithValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(bool value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithBoolValue != null)
		{
			for (int i = 0; i < _eventHandlersWithBoolValue.Count; i++)
			{
				PropertyChangedWithBoolValueEventHandler propertyChangedWithBoolValueEventHandler = _eventHandlersWithBoolValue[i];
				PropertyChangedWithBoolValueEventArgs e = new PropertyChangedWithBoolValueEventArgs(propertyName, value);
				propertyChangedWithBoolValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(int value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithIntValue != null)
		{
			for (int i = 0; i < _eventHandlersWithIntValue.Count; i++)
			{
				PropertyChangedWithIntValueEventHandler propertyChangedWithIntValueEventHandler = _eventHandlersWithIntValue[i];
				PropertyChangedWithIntValueEventArgs e = new PropertyChangedWithIntValueEventArgs(propertyName, value);
				propertyChangedWithIntValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(float value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithFloatValue != null)
		{
			for (int i = 0; i < _eventHandlersWithFloatValue.Count; i++)
			{
				PropertyChangedWithFloatValueEventHandler propertyChangedWithFloatValueEventHandler = _eventHandlersWithFloatValue[i];
				PropertyChangedWithFloatValueEventArgs e = new PropertyChangedWithFloatValueEventArgs(propertyName, value);
				propertyChangedWithFloatValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(uint value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithUIntValue != null)
		{
			for (int i = 0; i < _eventHandlersWithUIntValue.Count; i++)
			{
				PropertyChangedWithUIntValueEventHandler propertyChangedWithUIntValueEventHandler = _eventHandlersWithUIntValue[i];
				PropertyChangedWithUIntValueEventArgs e = new PropertyChangedWithUIntValueEventArgs(propertyName, value);
				propertyChangedWithUIntValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(Color value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithColorValue != null)
		{
			for (int i = 0; i < _eventHandlersWithColorValue.Count; i++)
			{
				PropertyChangedWithColorValueEventHandler propertyChangedWithColorValueEventHandler = _eventHandlersWithColorValue[i];
				PropertyChangedWithColorValueEventArgs e = new PropertyChangedWithColorValueEventArgs(propertyName, value);
				propertyChangedWithColorValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(double value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithDoubleValue != null)
		{
			for (int i = 0; i < _eventHandlersWithDoubleValue.Count; i++)
			{
				PropertyChangedWithDoubleValueEventHandler propertyChangedWithDoubleValueEventHandler = _eventHandlersWithDoubleValue[i];
				PropertyChangedWithDoubleValueEventArgs e = new PropertyChangedWithDoubleValueEventArgs(propertyName, value);
				propertyChangedWithDoubleValueEventHandler(this, e);
			}
		}
	}

	public void OnPropertyChangedWithValue(Vec2 value, [CallerMemberName] string propertyName = null)
	{
		if (_eventHandlersWithVec2Value != null)
		{
			for (int i = 0; i < _eventHandlersWithVec2Value.Count; i++)
			{
				PropertyChangedWithVec2ValueEventHandler propertyChangedWithVec2ValueEventHandler = _eventHandlersWithVec2Value[i];
				PropertyChangedWithVec2ValueEventArgs e = new PropertyChangedWithVec2ValueEventArgs(propertyName, value);
				propertyChangedWithVec2ValueEventHandler(this, e);
			}
		}
	}

	public object GetViewModelAtPath(BindingPath path, bool isList)
	{
		return GetViewModelAtPath(path);
	}

	public object GetViewModelAtPath(BindingPath path)
	{
		BindingPath subPath = path.SubPath;
		if (subPath != null)
		{
			PropertyInfo property = GetProperty(subPath.FirstNode);
			if (property != null)
			{
				object obj = property.GetGetMethod().InvokeWithLog(this, null);
				if (obj is ViewModel viewModel)
				{
					return viewModel.GetViewModelAtPath(subPath);
				}
				if (obj is IMBBindingList)
				{
					return GetChildAtPath(obj as IMBBindingList, subPath);
				}
			}
			return null;
		}
		return this;
	}

	private static object GetChildAtPath(IMBBindingList bindingList, BindingPath path)
	{
		BindingPath subPath = path.SubPath;
		if (subPath == null)
		{
			return bindingList;
		}
		if (bindingList.Count > 0)
		{
			int num = -1;
			num = Convert.ToInt32(subPath.FirstNode);
			if (num >= 0 && num < bindingList.Count)
			{
				object obj = bindingList[num];
				if (obj is ViewModel)
				{
					return (obj as ViewModel).GetViewModelAtPath(subPath);
				}
				if (obj is IMBBindingList)
				{
					return GetChildAtPath(obj as IMBBindingList, subPath);
				}
			}
		}
		return null;
	}

	public object GetPropertyValue(string name, PropertyTypeFeeder propertyTypeFeeder)
	{
		return GetPropertyValue(name);
	}

	public object GetPropertyValue(string name)
	{
		PropertyInfo property = GetProperty(name);
		object result = null;
		if (property != null)
		{
			result = property.GetGetMethod().InvokeWithLog(this, null);
		}
		return result;
	}

	public Type GetPropertyType(string name)
	{
		PropertyInfo property = GetProperty(name);
		if (property != null)
		{
			return property.PropertyType;
		}
		return null;
	}

	public void SetPropertyValue(string name, object value)
	{
		PropertyInfo property = GetProperty(name);
		if (property != null)
		{
			property.GetSetMethod()?.InvokeWithLog(this, value);
		}
	}

	public virtual void OnFinalize()
	{
	}

	public void ExecuteCommand(string commandName, object[] parameters)
	{
		MethodInfo methodInfo = null;
		if (_propertiesAndMethods != null && _propertiesAndMethods.Methods.TryGetValue(commandName, out var value))
		{
			methodInfo = value;
		}
		else
		{
			Type type = _type;
			while (type != null && methodInfo == null)
			{
				methodInfo = type.GetMethod(commandName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				type = type.BaseType;
			}
		}
		if (!(methodInfo != null))
		{
			return;
		}
		ParameterInfo[] parameters2 = methodInfo.GetParameters();
		if (parameters2.Length == parameters.Length)
		{
			object[] array = new object[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				object obj = parameters[i];
				Type parameterType = parameters2[i].ParameterType;
				array[i] = obj;
				if (obj is string && parameterType != typeof(string))
				{
					object obj2 = ConvertValueTo((string)obj, parameterType);
					array[i] = obj2;
				}
			}
			if (AreParametersCompatibleWithMethod(array, parameters2))
			{
				methodInfo.InvokeWithLog(this, array);
			}
		}
		else if (parameters2.Length == 0)
		{
			methodInfo.InvokeWithLog(this, null);
		}
	}

	private bool AreParametersCompatibleWithMethod(object[] parameters, ParameterInfo[] methodParameters)
	{
		if (parameters.Length != methodParameters.Length)
		{
			return false;
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			object obj = parameters[i];
			ParameterInfo parameterInfo = methodParameters[i];
			if (obj != null && !parameterInfo.ParameterType.IsAssignableFrom(obj.GetType()))
			{
				return false;
			}
		}
		return true;
	}

	private static object ConvertValueTo(string value, Type parameterType)
	{
		object result = null;
		if (parameterType == typeof(string))
		{
			result = value;
		}
		else if (parameterType == typeof(int))
		{
			result = Convert.ToInt32(value);
		}
		else if (parameterType == typeof(float))
		{
			result = Convert.ToSingle(value);
		}
		return result;
	}

	public virtual void RefreshValues()
	{
	}

	public static void RefreshPropertyAndMethodInfos()
	{
		_cachedViewModelProperties.Clear();
		Assembly[] viewModelAssemblies = GetViewModelAssemblies();
		for (int i = 0; i < viewModelAssemblies.Length; i++)
		{
			List<Type> typesSafe = viewModelAssemblies[i].GetTypesSafe();
			for (int j = 0; j < typesSafe.Count; j++)
			{
				Type type = typesSafe[j];
				if (typeof(IViewModel).IsAssignableFrom(type) && typeof(IViewModel) != type)
				{
					DataSourceTypeBindingPropertiesCollection propertiesOfType = GetPropertiesOfType(type);
					_cachedViewModelProperties[type] = propertiesOfType;
				}
			}
		}
	}

	private static Assembly[] GetViewModelAssemblies()
	{
		List<Assembly> list = new List<Assembly>();
		Assembly assembly = typeof(ViewModel).Assembly;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		list.Add(assembly);
		Assembly[] array = assemblies;
		foreach (Assembly assembly2 in array)
		{
			if (!(assembly2 != assembly))
			{
				continue;
			}
			AssemblyName[] referencedAssemblies = assembly2.GetReferencedAssemblies();
			for (int j = 0; j < referencedAssemblies.Length; j++)
			{
				if (referencedAssemblies[j].ToString() == assembly.GetName().ToString())
				{
					list.Add(assembly2);
					break;
				}
			}
		}
		return list.ToArray();
	}

	private static DataSourceTypeBindingPropertiesCollection GetPropertiesOfType(Type t)
	{
		_ = t.Name;
		Dictionary<string, PropertyInfo> dictionary = new Dictionary<string, PropertyInfo>();
		Dictionary<string, MethodInfo> dictionary2 = new Dictionary<string, MethodInfo>();
		PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			dictionary.Add(propertyInfo.Name, propertyInfo);
		}
		MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (!dictionary2.ContainsKey(methodInfo.Name))
			{
				dictionary2.Add(methodInfo.Name, methodInfo);
			}
		}
		return new DataSourceTypeBindingPropertiesCollection(dictionary, dictionary2);
	}
}
