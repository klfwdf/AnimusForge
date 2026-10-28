using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class GenericArgumentMapper : IGenericArgumentMapper
{
	public GenericMappingResult Map(Type genericServiceType, Type openGenericImplementingType)
	{
		string[] genericParameterNames = openGenericImplementingType.GetTypeInfo().GenericTypeParameters.Select((Type t) => t.Name).ToArray();
		Dictionary<string, Type> genericArgumentMap = CreateMap(genericServiceType, openGenericImplementingType, genericParameterNames);
		return new GenericMappingResult(genericParameterNames, genericArgumentMap, genericServiceType, openGenericImplementingType);
	}

	public Type TryMakeGenericType(Type genericServiceType, Type openGenericImplementingType)
	{
		GenericMappingResult genericMappingResult = Map(genericServiceType, openGenericImplementingType);
		if (!genericMappingResult.IsValid)
		{
			return null;
		}
		try
		{
			return openGenericImplementingType.MakeGenericType(genericMappingResult.GetMappedArguments());
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static Dictionary<string, Type> CreateMap(Type genericServiceType, Type openGenericImplementingType, string[] genericParameterNames)
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>(genericParameterNames.Length);
		Type[] genericArgumentsOrParameters = GetGenericArgumentsOrParameters(genericServiceType);
		if (genericArgumentsOrParameters.Length != 0)
		{
			genericServiceType = genericServiceType.GetTypeInfo().GetGenericTypeDefinition();
			Type baseTypeImplementingGenericTypeDefinition = GetBaseTypeImplementingGenericTypeDefinition(openGenericImplementingType, genericServiceType);
			Type[] genericArgumentsOrParameters2 = GetGenericArgumentsOrParameters(baseTypeImplementingGenericTypeDefinition);
			MapGenericArguments(genericArgumentsOrParameters, genericArgumentsOrParameters2, dictionary);
			return dictionary;
		}
		return dictionary;
	}

	private static Type[] GetGenericArgumentsOrParameters(Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		if (typeInfo.IsGenericTypeDefinition)
		{
			return typeInfo.GenericTypeParameters;
		}
		return typeInfo.GenericTypeArguments;
	}

	private static void MapGenericArguments(Type[] serviceTypeGenericArguments, Type[] baseTypeGenericArguments, IDictionary<string, Type> map)
	{
		for (int i = 0; i < baseTypeGenericArguments.Length; i++)
		{
			Type type = baseTypeGenericArguments[i];
			Type type2 = serviceTypeGenericArguments[i];
			if (type.GetTypeInfo().IsGenericParameter)
			{
				map[type.Name] = type2;
			}
			else if (type.GetTypeInfo().IsGenericType)
			{
				if (type2.GetTypeInfo().IsGenericType)
				{
					MapGenericArguments(type2.GetTypeInfo().GenericTypeArguments, type.GetTypeInfo().GenericTypeArguments, map);
				}
				else
				{
					MapGenericArguments(serviceTypeGenericArguments, type.GetTypeInfo().GenericTypeArguments, map);
				}
			}
		}
	}

	private static Type GetBaseTypeImplementingGenericTypeDefinition(Type implementingType, Type genericTypeDefinition)
	{
		Type type = null;
		if (genericTypeDefinition.GetTypeInfo().IsInterface)
		{
			type = implementingType.GetTypeInfo().ImplementedInterfaces.FirstOrDefault((Type i) => i.GetTypeInfo().IsGenericType && i.GetTypeInfo().GetGenericTypeDefinition() == genericTypeDefinition);
		}
		else
		{
			Type type2 = implementingType;
			while (!ImplementsOpenGenericTypeDefinition(genericTypeDefinition, type2) && type2 != typeof(object))
			{
				type2 = type2.GetTypeInfo().BaseType;
			}
			if (type2 != typeof(object))
			{
				type = type2;
			}
		}
		if (type == null)
		{
			throw new InvalidOperationException("The generic type definition " + genericTypeDefinition.FullName + " not implemented by implementing type " + implementingType.FullName);
		}
		return type;
	}

	private static bool ImplementsOpenGenericTypeDefinition(Type genericTypeDefinition, Type baseType)
	{
		return baseType.GetTypeInfo().IsGenericType && baseType.GetTypeInfo().GetGenericTypeDefinition() == genericTypeDefinition;
	}
}
