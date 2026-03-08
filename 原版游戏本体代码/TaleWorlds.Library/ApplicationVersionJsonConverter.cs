using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Library
{
	// Token: 0x0200000C RID: 12
	public class ApplicationVersionJsonConverter : JsonConverter
	{
		// Token: 0x06000028 RID: 40 RVA: 0x00002658 File Offset: 0x00000858
		public override bool CanConvert(Type objectType)
		{
			return typeof(ApplicationVersion).IsAssignableFrom(objectType);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x0000266A File Offset: 0x0000086A
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return ApplicationVersion.FromString((string)JObject.Load(reader)["_version"], 0);
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600002A RID: 42 RVA: 0x0000268C File Offset: 0x0000088C
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002690 File Offset: 0x00000890
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			JProperty content = new JProperty("_version", ((ApplicationVersion)value).ToString());
			new JObject { content }.WriteTo(writer, Array.Empty<JsonConverter>());
		}
	}
}
