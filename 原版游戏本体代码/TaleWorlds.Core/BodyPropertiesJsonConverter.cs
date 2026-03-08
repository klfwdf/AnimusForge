using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Core
{
	// Token: 0x02000022 RID: 34
	public class BodyPropertiesJsonConverter : JsonConverter
	{
		// Token: 0x060001A7 RID: 423 RVA: 0x000070A3 File Offset: 0x000052A3
		public override bool CanConvert(Type objectType)
		{
			return typeof(BodyProperties).IsAssignableFrom(objectType);
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x000070B8 File Offset: 0x000052B8
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			BodyProperties bodyProperties;
			BodyProperties.FromString((string)JObject.Load(reader)["_data"], out bodyProperties);
			return bodyProperties;
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060001A9 RID: 425 RVA: 0x000070E8 File Offset: 0x000052E8
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060001AA RID: 426 RVA: 0x000070EC File Offset: 0x000052EC
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			JProperty content = new JProperty("_data", ((BodyProperties)value).ToString());
			new JObject { content }.WriteTo(writer, Array.Empty<JsonConverter>());
		}
	}
}
