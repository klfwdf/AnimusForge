using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.ModuleManager;

public static class Extensions
{
	public static Assembly[] GetActiveReferencingGameAssembliesSafe(this Assembly assembly)
	{
		MBList<Assembly> activeGameAssemblies = ModuleHelper.GetActiveGameAssemblies();
		return assembly.GetReferencingAssembliesSafe((Assembly x) => activeGameAssemblies.Contains(x));
	}
}
