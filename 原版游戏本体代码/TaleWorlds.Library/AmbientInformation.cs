using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000013 RID: 19
	public struct AmbientInformation
	{
		// Token: 0x0600003E RID: 62 RVA: 0x00002A49 File Offset: 0x00000C49
		public void DeserializeFrom(IReader reader)
		{
			this.EnvironmentMultiplier = reader.ReadFloat();
			this.AmbientColor = reader.ReadVec3();
			this.MieScatterStrength = reader.ReadFloat();
			this.RayleighConstant = reader.ReadFloat();
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002A7B File Offset: 0x00000C7B
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.EnvironmentMultiplier);
			writer.WriteVec3(this.AmbientColor);
			writer.WriteFloat(this.MieScatterStrength);
			writer.WriteFloat(this.RayleighConstant);
		}

		// Token: 0x04000037 RID: 55
		public float EnvironmentMultiplier;

		// Token: 0x04000038 RID: 56
		public Vec3 AmbientColor;

		// Token: 0x04000039 RID: 57
		public float MieScatterStrength;

		// Token: 0x0400003A RID: 58
		public float RayleighConstant;
	}
}
