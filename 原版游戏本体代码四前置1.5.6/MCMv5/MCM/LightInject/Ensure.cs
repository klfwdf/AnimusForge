using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class Ensure
{
	public static void IsNotNull<T>(T value, string paramName)
	{
		if (value == null)
		{
			throw new ArgumentNullException(paramName);
		}
	}
}
