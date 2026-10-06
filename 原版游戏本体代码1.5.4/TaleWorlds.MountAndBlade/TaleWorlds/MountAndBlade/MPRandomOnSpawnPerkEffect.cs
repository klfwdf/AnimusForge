using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks;

namespace TaleWorlds.MountAndBlade;

public abstract class MPRandomOnSpawnPerkEffect : MPOnSpawnPerkEffectBase
{
	protected static Dictionary<string, Type> Registered;

	static MPRandomOnSpawnPerkEffect()
	{
		Registered = new Dictionary<string, Type>();
		foreach (Type item in from t in PerkAssemblyCollection.GetPerkAssemblyTypes()
			where t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MPRandomOnSpawnPerkEffect))
			select t)
		{
			string key = (string)item.GetField("StringType", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
			Registered.Add(key, item);
		}
	}

	public static MPRandomOnSpawnPerkEffect CreateFrom(XmlNode node)
	{
		string key = node?.Attributes?["type"]?.Value;
		MPRandomOnSpawnPerkEffect obj = (MPRandomOnSpawnPerkEffect)Activator.CreateInstance(Registered[key], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, CultureInfo.InvariantCulture);
		obj.Deserialize(node);
		return obj;
	}
}
