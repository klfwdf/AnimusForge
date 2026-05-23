using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000019 RID: 25
	public class LoginResultObjectJsonConverter : JsonConverter
	{
		// Token: 0x0600007E RID: 126 RVA: 0x00002B3C File Offset: 0x00000D3C
		public override bool CanConvert(Type objectType)
		{
			return typeof(LoginResultObject).IsAssignableFrom(objectType);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00002B50 File Offset: 0x00000D50
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jobject = JObject.Load(reader);
			string key = (string)jobject["_type"];
			Type type;
			if (LoginResultObjectJsonConverter._knownTypes.TryGetValue(key, out type))
			{
				LoginResultObject loginResultObject = (LoginResultObject)Activator.CreateInstance(type);
				serializer.Populate(jobject.CreateReader(), loginResultObject);
				return loginResultObject;
			}
			return null;
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000080 RID: 128 RVA: 0x00002BA1 File Offset: 0x00000DA1
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00002BA4 File Offset: 0x00000DA4
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

		// Token: 0x0400002E RID: 46
		private static readonly Dictionary<string, Type> _knownTypes = (from t in (from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.GlobalAssemblyCache
				select a).SelectMany((Assembly a) => a.GetTypes())
			where t.IsSubclassOf(typeof(LoginResultObject))
			select t).ToDictionary((Type item) => item.FullName, (Type item) => item);
	}
}
