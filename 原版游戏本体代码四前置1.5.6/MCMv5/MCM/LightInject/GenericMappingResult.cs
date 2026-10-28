using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class GenericMappingResult
{
	private readonly string[] genericParameterNames;

	private readonly IDictionary<string, Type> genericArgumentMap;

	private readonly Type genericServiceType;

	private readonly Type openGenericImplementingType;

	public bool IsValid
	{
		get
		{
			if (!genericServiceType.GetTypeInfo().IsGenericType && openGenericImplementingType.GetTypeInfo().ContainsGenericParameters)
			{
				return false;
			}
			return genericParameterNames.All((string n) => genericArgumentMap.ContainsKey(n));
		}
	}

	internal GenericMappingResult(string[] genericParameterNames, IDictionary<string, Type> genericArgumentMap, Type genericServiceType, Type openGenericImplementingType)
	{
		this.genericParameterNames = genericParameterNames;
		this.genericArgumentMap = genericArgumentMap;
		this.genericServiceType = genericServiceType;
		this.openGenericImplementingType = openGenericImplementingType;
	}

	public Type[] GetMappedArguments()
	{
		string[] source = genericParameterNames.Where((string n) => !genericArgumentMap.ContainsKey(n)).ToArray();
		if (source.Any())
		{
			string text = source.Aggregate((string current, string next) => current + "," + next);
			string message = "The generic parameter(s) " + text + " found in type " + openGenericImplementingType.FullName + " cannot be mapped from " + genericServiceType.FullName;
			throw new InvalidOperationException(message);
		}
		return genericParameterNames.Select((string parameterName) => genericArgumentMap[parameterName]).ToArray();
	}
}
