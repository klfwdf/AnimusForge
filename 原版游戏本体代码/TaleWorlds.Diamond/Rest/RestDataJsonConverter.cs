using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000035 RID: 53
	public class RestDataJsonConverter : JsonConverter<RestData>
	{
		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000123 RID: 291 RVA: 0x00003EB6 File Offset: 0x000020B6
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00003EB9 File Offset: 0x000020B9
		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00003EBC File Offset: 0x000020BC
		private RestData Create(Type objectType, JObject jObject)
		{
			if (jObject == null)
			{
				throw new ArgumentNullException("jObject");
			}
			string text = null;
			if (jObject["TypeName"] != null)
			{
				text = jObject["TypeName"].Value<string>();
			}
			else if (jObject["typeName"] != null)
			{
				text = jObject["typeName"].Value<string>();
			}
			if (text != null)
			{
				return Activator.CreateInstance(Type.GetType(text)) as RestData;
			}
			return null;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00003F2C File Offset: 0x0000212C
		public T ReadJson<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00003F34 File Offset: 0x00002134
		public override RestData ReadJson(JsonReader reader, Type objectType, RestData existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			if (serializer == null)
			{
				throw new ArgumentNullException("serializer");
			}
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}
			JObject jobject = JObject.Load(reader);
			RestData restData = this.Create(objectType, jobject);
			serializer.Populate(jobject.CreateReader(), restData);
			return restData;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00003F89 File Offset: 0x00002189
		public override void WriteJson(JsonWriter writer, RestData value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}
