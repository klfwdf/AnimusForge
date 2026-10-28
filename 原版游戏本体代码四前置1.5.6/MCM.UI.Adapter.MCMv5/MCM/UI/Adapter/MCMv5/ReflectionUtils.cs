using System;
using System.Linq;

namespace MCM.UI.Adapter.MCMv5;

internal static class ReflectionUtils
{
	public static bool ImplementsOrImplementsEquivalent(Type type, string? fullBaseTypeName, bool includeBase = true)
	{
		if (fullBaseTypeName == null)
		{
			return false;
		}
		Type type2 = (includeBase ? type : type.BaseType);
		while ((object)type2 != null)
		{
			string? fullName = type2.FullName;
			if (fullName != null && fullName.EndsWith(fullBaseTypeName, StringComparison.Ordinal))
			{
				return true;
			}
			type2 = type2.BaseType;
		}
		return type.GetInterfaces().Any((Type x) => (includeBase || type != x) && string.Equals(x.FullName, fullBaseTypeName, StringComparison.Ordinal));
	}

	public static bool ImplementsEquivalentInterface(Type type, string? fullBaseTypeName)
	{
		if (fullBaseTypeName == null)
		{
			return false;
		}
		Type type2 = type;
		while ((object)type2 != null)
		{
			if (type.GetInterfaces().Any((Type x) => x.FullName?.EndsWith(fullBaseTypeName, StringComparison.Ordinal) ?? false))
			{
				return true;
			}
			type2 = type2.BaseType;
		}
		return false;
	}
}
