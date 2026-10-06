using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public static class TextureProviderFactory
{
	private static Dictionary<string, Type> _textureProvidertypes;

	static TextureProviderFactory()
	{
		_textureProvidertypes = new Dictionary<string, Type>();
	}

	public static TextureProvider CreateInstance(string textureProviderName)
	{
		if (_textureProvidertypes.TryGetValue(textureProviderName, out var value))
		{
			try
			{
				if (Activator.CreateInstance(value) is TextureProvider result)
				{
					return result;
				}
			}
			catch
			{
			}
		}
		Debug.FailedAssert("Unable to create instance for texture provider with name: " + textureProviderName, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\TextureProviderFactory.cs", "CreateInstance", 36);
		return null;
	}

	public static void RefreshProviderTypes()
	{
		_textureProvidertypes.Clear();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			List<Type> typesSafe = assemblies[i].GetTypesSafe();
			for (int j = 0; j < typesSafe.Count; j++)
			{
				Type type = typesSafe[j];
				if (type == null)
				{
					Debug.FailedAssert("(RefreshProviderTypes): Null type while iterating assemblies", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\TextureProviderFactory.cs", "RefreshProviderTypes", 53);
				}
				else if (typeof(TextureProvider).IsAssignableFrom(type) && !type.IsAbstract)
				{
					_textureProvidertypes.Add(type.Name, type);
				}
			}
		}
	}
}
