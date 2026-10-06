using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class HasTableauCache : Attribute
{
	public Type TableauCacheType { get; set; }

	public Type MaterialCacheIDGetType { get; set; }

	internal static Dictionary<Type, MaterialCacheIDGetMethodDelegate> TableauCacheTypes { get; private set; }

	public HasTableauCache(Type tableauCacheType, Type materialCacheIDGetType)
	{
		TableauCacheType = tableauCacheType;
		MaterialCacheIDGetType = materialCacheIDGetType;
	}

	public static void CollectTableauCacheTypes()
	{
		TableauCacheTypes = new Dictionary<Type, MaterialCacheIDGetMethodDelegate>();
		CollectTableauCacheTypesFrom(typeof(HasTableauCache).Assembly);
		Assembly[] referencingAssembliesSafe = typeof(HasTableauCache).Assembly.GetReferencingAssembliesSafe();
		for (int i = 0; i < referencingAssembliesSafe.Length; i++)
		{
			CollectTableauCacheTypesFrom(referencingAssembliesSafe[i]);
		}
	}

	private static void CollectTableauCacheTypesFrom(Assembly assembly)
	{
		object[] customAttributesSafe = assembly.GetCustomAttributesSafe(typeof(HasTableauCache), inherit: true);
		if (customAttributesSafe.Length != 0)
		{
			object[] array = customAttributesSafe;
			for (int i = 0; i < array.Length; i++)
			{
				HasTableauCache hasTableauCache = (HasTableauCache)array[i];
				MethodInfo method = hasTableauCache.MaterialCacheIDGetType.GetMethod("GetMaterialCacheID", BindingFlags.Static | BindingFlags.Public);
				MaterialCacheIDGetMethodDelegate value = (MaterialCacheIDGetMethodDelegate)Delegate.CreateDelegate(typeof(MaterialCacheIDGetMethodDelegate), method);
				TableauCacheTypes.Add(hasTableauCache.TableauCacheType, value);
			}
		}
	}
}
