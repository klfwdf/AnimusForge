using System;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.ObjectSystem.Extensions;

public static class MBObjectBaseExtensions
{
	private static IMBObjectExtensionDataStore? _instance;

	private static IMBObjectExtensionDataStore? Instance => _instance ?? (_instance = ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<IMBObjectExtensionDataStore>());

	internal static void OnGameEnd()
	{
		_instance = null;
	}

	internal static bool HasVariable(this MBObjectBase @object, string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Variable name cannot be empty.", "name");
		}
		return Instance?.HasVariable(@object, name) ?? false;
	}

	internal static bool TryGetVariable<T>(this MBObjectBase @object, string name, out T value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Variable name cannot be empty.", "name");
		}
		if (typeof(T) == typeof(char))
		{
			throw new NotSupportedException("TaleWorlds.SaveSystem cannot store variable-width basic type `char`. Consider using a string.");
		}
		IMBObjectExtensionDataStore instance = Instance;
		if (instance != null && instance.TryGetVariable<T>(@object, name, out T value2))
		{
			value = value2;
			return true;
		}
		value = default(T);
		return false;
	}

	internal static void SetVariable<T>(this MBObjectBase @object, string name, T data)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Variable name cannot be empty.", "name");
		}
		if (typeof(T) == typeof(char))
		{
			throw new NotSupportedException("TaleWorlds.SaveSystem cannot store type 'char'. Consider using a string.");
		}
		Instance?.SetVariable(@object, name, data);
	}

	internal static void RemoveVariable(this MBObjectBase @object, string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Variable name cannot be empty.", "name");
		}
		Instance?.RemoveVariable(@object, name);
	}

	public static bool HasFlag(this MBObjectBase @object, string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Flag name cannot be empty.", "name");
		}
		return Instance?.HasFlag(@object, name) ?? false;
	}

	public static void SetFlag(this MBObjectBase @object, string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Flag name cannot be empty.", "name");
		}
		Instance?.SetFlag(@object, name);
	}

	public static void RemoveFlag(this MBObjectBase @object, string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException("Flag name cannot be empty.", "name");
		}
		Instance?.RemoveFlag(@object, name);
	}
}
