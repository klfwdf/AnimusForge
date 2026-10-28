using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace TaleWorlds.Network;

public class JsonSocketMessage
{
	public MessageInfo MessageInfo { get; private set; }

	[JsonProperty]
	public string SocketMessageTypeId => GetType().FullName;

	[Obsolete]
	public JsonSocketMessage()
	{
		MessageInfo = new MessageInfo();
		Attribute[] customAttributes = Attribute.GetCustomAttributes(GetType(), inherit: true);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			_ = customAttributes[i] is PostBoxId;
		}
	}

	public static string GetTypeId(Type messageType)
	{
		return messageType.FullName;
	}

	public static Dictionary<string, Type> GetMessageDictionary()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			foreach (KeyValuePair<string, Type> item in RetrieveJSONSocketMessages(assemblies[i]))
			{
				dictionary.Add(item.Key, item.Value);
			}
		}
		return dictionary;
	}

	private static Dictionary<string, Type> RetrieveJSONSocketMessages(Assembly assembly)
	{
		Type[] exportedTypes = assembly.GetExportedTypes();
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		foreach (Type item in exportedTypes.Where((Type q) => q.IsSubclassOf(typeof(JsonSocketMessage))))
		{
			dictionary.Add(item.FullName, item);
		}
		return dictionary;
	}
}
