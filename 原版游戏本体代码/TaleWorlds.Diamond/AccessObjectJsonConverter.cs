using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000003 RID: 3
	public class AccessObjectJsonConverter : JsonConverter
	{
		// Token: 0x06000004 RID: 4 RVA: 0x00002061 File Offset: 0x00000261
		public override bool CanConvert(Type objectType)
		{
			return typeof(AccessObject).IsAssignableFrom(objectType);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002074 File Offset: 0x00000274
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jobject = JObject.Load(reader);
			string a = (string)jobject["Type"];
			AccessObject accessObject;
			if (a == "Steam")
			{
				accessObject = new SteamAccessObject();
			}
			else if (a == "Epic")
			{
				accessObject = new EpicAccessObject();
			}
			else if (a == "GOG")
			{
				accessObject = new GOGAccessObject();
			}
			else if (a == "GDK")
			{
				accessObject = new GDKAccessObject();
			}
			else if (a == "PS")
			{
				accessObject = new PSAccessObject();
			}
			else
			{
				if (!(a == "Test"))
				{
					return null;
				}
				accessObject = new TestAccessObject();
			}
			serializer.Populate(jobject.CreateReader(), accessObject);
			return accessObject;
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000006 RID: 6 RVA: 0x00002128 File Offset: 0x00000328
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000212B File Offset: 0x0000032B
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
		}
	}
}
