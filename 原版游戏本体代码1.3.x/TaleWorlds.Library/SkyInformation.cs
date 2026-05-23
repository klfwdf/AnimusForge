using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000014 RID: 20
	public struct SkyInformation
	{
		// Token: 0x06000040 RID: 64 RVA: 0x00002AAD File Offset: 0x00000CAD
		public void DeserializeFrom(IReader reader)
		{
			this.Brightness = reader.ReadFloat();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002ABB File Offset: 0x00000CBB
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.Brightness);
		}

		// Token: 0x0400003B RID: 59
		public float Brightness;
	}
}
