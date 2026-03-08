using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200001E RID: 30
	public class PeerIdJsonConverter : JsonConverter
	{
		// Token: 0x0600009F RID: 159 RVA: 0x000031CD File Offset: 0x000013CD
		public override bool CanConvert(Type objectType)
		{
			return typeof(PeerId).IsAssignableFrom(objectType);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x000031DF File Offset: 0x000013DF
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return PeerId.FromString((string)JObject.Load(reader)["_peerId"]);
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000A1 RID: 161 RVA: 0x00003200 File Offset: 0x00001400
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00003204 File Offset: 0x00001404
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			JProperty content = new JProperty("_peerId", ((PeerId)value).ToString());
			new JObject { content }.WriteTo(writer, Array.Empty<JsonConverter>());
		}
	}
}
