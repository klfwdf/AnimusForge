using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ServiceNameProvider : IServiceNameProvider
{
	public string GetServiceName(Type serviceType, Type implementingType)
	{
		string text = implementingType.FullName;
		string text2 = serviceType.FullName;
		if (implementingType.GetTypeInfo().IsGenericTypeDefinition)
		{
			Regex regex = new Regex("((?:[a-z][a-z.]+))", RegexOptions.IgnoreCase);
			text = regex.Match(text).Groups[1].Value;
			text2 = regex.Match(text2).Groups[1].Value;
		}
		if (text2.Split(new char[1] { '.' }).Last().Substring(1) == text.Split(new char[1] { '.' }).Last())
		{
			text = string.Empty;
		}
		return text;
	}
}
