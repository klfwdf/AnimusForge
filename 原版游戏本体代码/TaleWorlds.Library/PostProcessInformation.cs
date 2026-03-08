using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000019 RID: 25
	public struct PostProcessInformation
	{
		// Token: 0x0600004A RID: 74 RVA: 0x00002C29 File Offset: 0x00000E29
		public void DeserializeFrom(IReader reader)
		{
			this.MinExposure = reader.ReadFloat();
			this.MaxExposure = reader.ReadFloat();
			this.BrightpassThreshold = reader.ReadFloat();
			this.MiddleGray = reader.ReadFloat();
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00002C5B File Offset: 0x00000E5B
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.MinExposure);
			writer.WriteFloat(this.MaxExposure);
			writer.WriteFloat(this.BrightpassThreshold);
			writer.WriteFloat(this.MiddleGray);
		}

		// Token: 0x0400004C RID: 76
		public float MinExposure;

		// Token: 0x0400004D RID: 77
		public float MaxExposure;

		// Token: 0x0400004E RID: 78
		public float BrightpassThreshold;

		// Token: 0x0400004F RID: 79
		public float MiddleGray;
	}
}
