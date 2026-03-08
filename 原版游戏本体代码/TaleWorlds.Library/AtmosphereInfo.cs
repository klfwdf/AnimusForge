using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library
{
	// Token: 0x0200001A RID: 26
	public struct AtmosphereInfo
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00002C8D File Offset: 0x00000E8D
		public bool IsValid
		{
			get
			{
				return !string.IsNullOrEmpty(this.AtmosphereName);
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00002CA0 File Offset: 0x00000EA0
		public static AtmosphereInfo GetInvalidAtmosphereInfo()
		{
			return new AtmosphereInfo
			{
				AtmosphereName = ""
			};
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00002CC4 File Offset: 0x00000EC4
		public void DeserializeFrom(IReader reader)
		{
			this.SunInfo.DeserializeFrom(reader);
			this.RainInfo.DeserializeFrom(reader);
			this.SnowInfo.DeserializeFrom(reader);
			this.AmbientInfo.DeserializeFrom(reader);
			this.FogInfo.DeserializeFrom(reader);
			this.SkyInfo.DeserializeFrom(reader);
			this.NauticalInfo.DeserializeFrom(reader);
			this.TimeInfo.DeserializeFrom(reader);
			this.AreaInfo.DeserializeFrom(reader);
			this.PostProInfo.DeserializeFrom(reader);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00002D4C File Offset: 0x00000F4C
		public void SerializeTo(IWriter writer)
		{
			this.SunInfo.SerializeTo(writer);
			this.RainInfo.SerializeTo(writer);
			this.SnowInfo.SerializeTo(writer);
			this.AmbientInfo.SerializeTo(writer);
			this.FogInfo.SerializeTo(writer);
			this.SkyInfo.SerializeTo(writer);
			this.NauticalInfo.SerializeTo(writer);
			this.TimeInfo.SerializeTo(writer);
			this.AreaInfo.SerializeTo(writer);
			this.PostProInfo.SerializeTo(writer);
		}

		// Token: 0x04000050 RID: 80
		public uint Seed;

		// Token: 0x04000051 RID: 81
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string AtmosphereName;

		// Token: 0x04000052 RID: 82
		public SunInformation SunInfo;

		// Token: 0x04000053 RID: 83
		public RainInformation RainInfo;

		// Token: 0x04000054 RID: 84
		public SnowInformation SnowInfo;

		// Token: 0x04000055 RID: 85
		public AmbientInformation AmbientInfo;

		// Token: 0x04000056 RID: 86
		public FogInformation FogInfo;

		// Token: 0x04000057 RID: 87
		public SkyInformation SkyInfo;

		// Token: 0x04000058 RID: 88
		public NauticalInformation NauticalInfo;

		// Token: 0x04000059 RID: 89
		public TimeInformation TimeInfo;

		// Token: 0x0400005A RID: 90
		public AreaInformation AreaInfo;

		// Token: 0x0400005B RID: 91
		public PostProcessInformation PostProInfo;

		// Token: 0x0400005C RID: 92
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string InterpolatedAtmosphereName;
	}
}
