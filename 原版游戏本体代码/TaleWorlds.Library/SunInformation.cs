using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000010 RID: 16
	public struct SunInformation
	{
		// Token: 0x06000038 RID: 56 RVA: 0x0000294C File Offset: 0x00000B4C
		public void DeserializeFrom(IReader reader)
		{
			this.Altitude = reader.ReadFloat();
			this.Angle = reader.ReadFloat();
			this.Color = reader.ReadVec3();
			this.Brightness = reader.ReadFloat();
			this.MaxBrightness = reader.ReadFloat();
			this.Size = reader.ReadFloat();
			this.RayStrength = reader.ReadFloat();
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000029B0 File Offset: 0x00000BB0
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.Altitude);
			writer.WriteFloat(this.Angle);
			writer.WriteVec3(this.Color);
			writer.WriteFloat(this.Brightness);
			writer.WriteFloat(this.MaxBrightness);
			writer.WriteFloat(this.Size);
			writer.WriteFloat(this.RayStrength);
		}

		// Token: 0x0400002E RID: 46
		public float Altitude;

		// Token: 0x0400002F RID: 47
		public float Angle;

		// Token: 0x04000030 RID: 48
		public Vec3 Color;

		// Token: 0x04000031 RID: 49
		public float Brightness;

		// Token: 0x04000032 RID: 50
		public float MaxBrightness;

		// Token: 0x04000033 RID: 51
		public float Size;

		// Token: 0x04000034 RID: 52
		public float RayStrength;
	}
}
