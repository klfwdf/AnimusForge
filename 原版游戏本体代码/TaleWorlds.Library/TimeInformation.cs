using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000017 RID: 23
	public struct TimeInformation
	{
		// Token: 0x06000046 RID: 70 RVA: 0x00002B79 File Offset: 0x00000D79
		public void DeserializeFrom(IReader reader)
		{
			this.TimeOfDay = reader.ReadFloat();
			this.NightTimeFactor = reader.ReadFloat();
			this.DrynessFactor = reader.ReadFloat();
			this.WinterTimeFactor = reader.ReadFloat();
			this.Season = reader.ReadInt();
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00002BB7 File Offset: 0x00000DB7
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.TimeOfDay);
			writer.WriteFloat(this.NightTimeFactor);
			writer.WriteFloat(this.DrynessFactor);
			writer.WriteFloat(this.WinterTimeFactor);
			writer.WriteInt(this.Season);
		}

		// Token: 0x04000045 RID: 69
		public float TimeOfDay;

		// Token: 0x04000046 RID: 70
		public float NightTimeFactor;

		// Token: 0x04000047 RID: 71
		public float DrynessFactor;

		// Token: 0x04000048 RID: 72
		public float WinterTimeFactor;

		// Token: 0x04000049 RID: 73
		public int Season;
	}
}
