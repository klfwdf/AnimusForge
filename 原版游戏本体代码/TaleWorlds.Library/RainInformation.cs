using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000011 RID: 17
	public struct RainInformation
	{
		// Token: 0x0600003A RID: 58 RVA: 0x00002A11 File Offset: 0x00000C11
		public void DeserializeFrom(IReader reader)
		{
			this.Density = reader.ReadFloat();
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002A1F File Offset: 0x00000C1F
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.Density);
		}

		// Token: 0x04000035 RID: 53
		public float Density;
	}
}
