using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks;

namespace TaleWorlds.MountAndBlade;

public abstract class MPPerkEffect : MPPerkEffectBase
{
	protected static Dictionary<string, Type> Registered;

	static MPPerkEffect()
	{
		Registered = new Dictionary<string, Type>();
		foreach (Type item in from t in PerkAssemblyCollection.GetPerkAssemblyTypes()
			where t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MPPerkEffect))
			select t)
		{
			string key = (string)item.GetField("StringType", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
			Registered.Add(key, item);
		}
	}

	public static MPPerkEffect CreateFrom(XmlNode node)
	{
		string key = node?.Attributes?["type"]?.Value;
		MPPerkEffect obj = (MPPerkEffect)Activator.CreateInstance(Registered[key], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, CultureInfo.InvariantCulture);
		obj.Deserialize(node);
		return obj;
	}
}
