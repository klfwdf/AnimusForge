using System.Collections.Generic;

namespace TaleWorlds.DotNet;

internal class LibraryApplicationInterface
{
	internal static IManaged IManaged;

	internal static ITelemetry ITelemetry;

	internal static ILibrarySizeChecker ILibrarySizeChecker;

	internal static INativeArray INativeArray;

	internal static INativeObjectArray INativeObjectArray;

	internal static INativeStringHelper INativeStringHelper;

	internal static INativeString INativeString;

	private static Dictionary<string, object> _objects;

	private static T GetObject<T>() where T : class
	{
		if (_objects.TryGetValue(typeof(T).FullName, out var value))
		{
			return value as T;
		}
		return null;
	}

	internal static void SetObjects(Dictionary<string, object> objects)
	{
		_objects = objects;
		IManaged = GetObject<IManaged>();
		ITelemetry = GetObject<ITelemetry>();
		ILibrarySizeChecker = GetObject<ILibrarySizeChecker>();
		INativeArray = GetObject<INativeArray>();
		INativeObjectArray = GetObject<INativeObjectArray>();
		INativeStringHelper = GetObject<INativeStringHelper>();
		INativeString = GetObject<INativeString>();
	}
}
