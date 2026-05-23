using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200008E RID: 142
	[EngineStruct("Managed_sound_event_parameter", false, null)]
	public struct SoundEventParameter
	{
		// Token: 0x06000CAD RID: 3245 RVA: 0x0000E16F File Offset: 0x0000C36F
		public SoundEventParameter(string paramName, float value)
		{
			this.ParamName = paramName;
			this.Value = value;
		}

		// Token: 0x06000CAE RID: 3246 RVA: 0x0000E17F File Offset: 0x0000C37F
		public void Update(string paramName, float value)
		{
			this.ParamName = paramName;
			this.Value = value;
		}

		// Token: 0x040001C6 RID: 454
		[CustomEngineStructMemberData("str_id")]
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		internal string ParamName;

		// Token: 0x040001C7 RID: 455
		internal float Value;
	}
}
