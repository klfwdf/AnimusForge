using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000016 RID: 22
	public struct FogInformation
	{
		// Token: 0x06000044 RID: 68 RVA: 0x00002B2D File Offset: 0x00000D2D
		public void DeserializeFrom(IReader reader)
		{
			this.Density = reader.ReadFloat();
			this.Color = reader.ReadVec3();
			this.Falloff = reader.ReadFloat();
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002B53 File Offset: 0x00000D53
		public void SerializeTo(IWriter writer)
		{
			writer.WriteFloat(this.Density);
			writer.WriteVec3(this.Color);
			writer.WriteFloat(this.Falloff);
		}

		// Token: 0x04000042 RID: 66
		public float Density;

		// Token: 0x04000043 RID: 67
		public Vec3 Color;

		// Token: 0x04000044 RID: 68
		public float Falloff;
	}
}
