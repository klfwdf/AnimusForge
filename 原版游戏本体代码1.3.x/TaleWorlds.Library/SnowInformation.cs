using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000012 RID: 18
	public struct SnowInformation
	{
		// Token: 0x0600003C RID: 60 RVA: 0x00002A2D File Offset: 0x00000C2D
		public void DeserializeFrom(IReader reader)
		{
			this.Density = reader.ReadFloat();
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002A3B File Offset: 0x00000C3B
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.Density);
		}

		// Token: 0x04000036 RID: 54
		public float Density;
	}
}
