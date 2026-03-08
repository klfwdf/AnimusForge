using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000018 RID: 24
	public struct AreaInformation
	{
		// Token: 0x06000048 RID: 72 RVA: 0x00002BF5 File Offset: 0x00000DF5
		public void DeserializeFrom(IReader reader)
		{
			this.Temperature = reader.ReadFloat();
			this.Humidity = reader.ReadFloat();
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002C0F File Offset: 0x00000E0F
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.Temperature);
			writer.WriteFloat(this.Humidity);
		}

		// Token: 0x0400004A RID: 74
		public float Temperature;

		// Token: 0x0400004B RID: 75
		public float Humidity;
	}
}
