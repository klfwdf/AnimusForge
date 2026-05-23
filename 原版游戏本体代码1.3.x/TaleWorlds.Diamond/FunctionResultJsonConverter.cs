using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000009 RID: 9
	public class FunctionResultJsonConverter : JsonConverter
	{
		// Token: 0x0600002B RID: 43 RVA: 0x00002748 File Offset: 0x00000948
		public override bool CanConvert(Type objectType)
		{
			return typeof(FunctionResult).IsAssignableFrom(objectType);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000275C File Offset: 0x0000095C
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jobject = JObject.Load(reader);
			string key = (string)jobject["_type"];
			Type type;
			if (FunctionResultJsonConverter._knownTypes.TryGetValue(key, out type))
			{
				FunctionResult functionResult = (FunctionResult)Activator.CreateInstance(type);
				serializer.Populate(jobject.CreateReader(), functionResult);
				return functionResult;
			}
			return null;
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600002D RID: 45 RVA: 0x000027AD File Offset: 0x000009AD
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000027B0 File Offset: 0x000009B0
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			JProperty content = new JProperty("_type", value.GetType().FullName);
			JObject jobject = new JObject();
			jobject.Add(content);
			foreach (PropertyInfo propertyInfo in value.GetType().GetProperties())
			{
				if (propertyInfo.CanRead)
				{
					object value2 = propertyInfo.GetValue(value);
					if (value2 != null)
					{
						jobject.Add(propertyInfo.Name, JToken.FromObject(value2, serializer));
					}
				}
			}
			jobject.WriteTo(writer, Array.Empty<JsonConverter>());
		}

		// Token: 0x0400000C RID: 12
		private static readonly Dictionary<string, Type> _knownTypes = (from t in (from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.GlobalAssemblyCache
				select a).SelectMany((Assembly a) => a.GetTypes())
			where t.IsSubclassOf(typeof(FunctionResult))
			select t).ToDictionary((Type item) => item.FullName, (Type item) => item);
	}
}
