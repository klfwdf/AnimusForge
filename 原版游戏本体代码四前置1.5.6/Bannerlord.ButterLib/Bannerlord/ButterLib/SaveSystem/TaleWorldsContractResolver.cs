using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TaleWorlds.SaveSystem;

namespace Bannerlord.ButterLib.SaveSystem;

public class TaleWorldsContractResolver : TaleWorldsContractResolverBase
{
	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
	{
		if ((TaleWorldsContractResolverBase._isContainerDelegate != null && TaleWorldsContractResolverBase._isContainerDelegate(type)) || (TaleWorldsContractResolverBase._isContainerDelegate == null && TaleWorldsContractResolverBase.IsContainerFallback(type)))
		{
			return base.CreateProperties(type, memberSerialization);
		}
		if (type.GetCustomAttribute<SerializableAttribute>() != null)
		{
			return base.CreateProperties(type, memberSerialization);
		}
		if (type.GetMembers().All((MemberInfo m) => m.GetCustomAttributes(inherit: true).Any((object att) => att.GetType() != typeof(SaveableFieldAttribute) || att.GetType() != typeof(SaveablePropertyAttribute))))
		{
			return new List<JsonProperty>();
		}
		IEnumerable<FieldInfo> source = from x in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where x.GetCustomAttribute<SaveableFieldAttribute>(inherit: true) != null
			select x;
		return Enumerable.Concat(second: from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where p.GetCustomAttribute<SaveablePropertyAttribute>(inherit: true) != null
			select new JsonProperty
			{
				PropertyName = p.Name,
				PropertyType = p.PropertyType,
				Readable = true,
				Writable = true,
				ValueProvider = base.CreateMemberValueProvider(p)
			}, first: source.Select((FieldInfo p) => new JsonProperty
		{
			PropertyName = p.Name,
			PropertyType = p.FieldType,
			Readable = true,
			Writable = true,
			ValueProvider = base.CreateMemberValueProvider(p)
		})).ToList();
	}
}
