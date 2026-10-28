using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.Extensions;

internal static class ViewModelExtensions
{
	private delegate Dictionary<string, PropertyInfo> GetPropertiesDelegate(object instance);

	private delegate Dictionary<string, MethodInfo> GetMethodsDelegate(object instance);

	public delegate object DataSourceTypeBindingPropertiesCollectionCtorDelegate(Dictionary<string, PropertyInfo> properties, Dictionary<string, MethodInfo> methods);

	private static readonly string NestedType = "TaleWorlds.Library.ViewModel+DataSourceTypeBindingPropertiesCollection";

	private static readonly AccessTools.FieldRef<ViewModel, object>? PropertiesAndMethods = AccessTools2.FieldRefAccess<ViewModel, object>("_propertiesAndMethods");

	private static readonly GetPropertiesDelegate? GetProperties = AccessTools2.GetDeclaredPropertyGetterDelegate<GetPropertiesDelegate>(NestedType + ":Properties");

	private static readonly GetMethodsDelegate? GetMethods = AccessTools2.GetDeclaredPropertyGetterDelegate<GetMethodsDelegate>(NestedType + ":Methods");

	private static readonly AccessTools.FieldRef<IDictionary>? CachedViewModelProperties = AccessTools2.StaticFieldRefAccess<IDictionary>(typeof(ViewModel), "_cachedViewModelProperties");

	public static readonly DataSourceTypeBindingPropertiesCollectionCtorDelegate? DataSourceTypeBindingPropertiesCollectionCtor = AccessTools2.GetDeclaredConstructorDelegate<DataSourceTypeBindingPropertiesCollectionCtorDelegate>(NestedType, new Type[2]
	{
		typeof(Dictionary<string, PropertyInfo>),
		typeof(Dictionary<string, MethodInfo>)
	});

	public static void AddProperty(this ViewModel viewModel, string name, PropertyInfo propertyInfo)
	{
		if (GetOrCreateIndividualStorage(viewModel, out Dictionary<string, PropertyInfo> propDict, out Dictionary<string, MethodInfo> _))
		{
			propDict[name] = propertyInfo;
		}
	}

	public static void AddMethod(this ViewModel viewModel, string name, MethodInfo methodInfo)
	{
		if (GetOrCreateIndividualStorage(viewModel, out Dictionary<string, PropertyInfo> _, out Dictionary<string, MethodInfo> methodDict))
		{
			methodDict[name] = methodInfo;
		}
	}

	public static IReadOnlyCollection<PropertyInfo> GetViewModelProperties(this ViewModel viewModel)
	{
		if (PropertiesAndMethods == null || CachedViewModelProperties == null || DataSourceTypeBindingPropertiesCollectionCtor == null || GetProperties == null || GetMethods == null)
		{
			return Array.Empty<PropertyInfo>();
		}
		object obj = PropertiesAndMethods(viewModel);
		if (obj == null)
		{
			return Array.Empty<PropertyInfo>();
		}
		Dictionary<string, PropertyInfo> dictionary = GetProperties(obj);
		return dictionary.Values;
	}

	public static IReadOnlyCollection<MethodInfo> GetViewModelMethods(this ViewModel viewModel)
	{
		if (PropertiesAndMethods == null || CachedViewModelProperties == null || DataSourceTypeBindingPropertiesCollectionCtor == null || GetProperties == null || GetMethods == null)
		{
			return Array.Empty<MethodInfo>();
		}
		object obj = PropertiesAndMethods(viewModel);
		if (obj == null)
		{
			return Array.Empty<MethodInfo>();
		}
		Dictionary<string, MethodInfo> dictionary = GetMethods(obj);
		return dictionary.Values;
	}

	private static bool GetOrCreateIndividualStorage(ViewModel viewModel, [NotNullWhen(true)] out Dictionary<string, PropertyInfo>? propDict, [NotNullWhen(true)] out Dictionary<string, MethodInfo>? methodDict)
	{
		propDict = null;
		methodDict = null;
		if (PropertiesAndMethods == null || CachedViewModelProperties == null || DataSourceTypeBindingPropertiesCollectionCtor == null || GetProperties == null || GetMethods == null)
		{
			return false;
		}
		object obj = PropertiesAndMethods(viewModel);
		if (obj != null)
		{
			IDictionary dictionary = CachedViewModelProperties();
			if (dictionary != null)
			{
				Type type = viewModel.GetType();
				if (dictionary.Contains(type))
				{
					object obj2 = dictionary[type];
					if (obj2 != null)
					{
						if ((propDict = GetProperties(obj)) == null || (methodDict = GetMethods(obj)) == null)
						{
							return false;
						}
						if (obj == obj2)
						{
							PropertiesAndMethods(viewModel) = DataSourceTypeBindingPropertiesCollectionCtor(propDict = new Dictionary<string, PropertyInfo>(propDict), methodDict = new Dictionary<string, MethodInfo>(methodDict));
						}
						return true;
					}
				}
				return false;
			}
		}
		return false;
	}
}
