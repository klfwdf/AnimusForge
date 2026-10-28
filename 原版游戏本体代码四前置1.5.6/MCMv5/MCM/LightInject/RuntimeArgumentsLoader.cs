using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class RuntimeArgumentsLoader
{
	public static object[] Load(object[] constants)
	{
		if (!(constants[^1] is object[] result))
		{
			return new object[0];
		}
		return result;
	}
}
