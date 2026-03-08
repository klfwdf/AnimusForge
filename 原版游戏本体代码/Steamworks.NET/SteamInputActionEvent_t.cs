using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000188 RID: 392
	[Serializable]
	public struct SteamInputActionEvent_t
	{
		// Token: 0x04000A1B RID: 2587
		public InputHandle_t controllerHandle;

		// Token: 0x04000A1C RID: 2588
		public ESteamInputActionEventType eEventType;

		// Token: 0x04000A1D RID: 2589
		public SteamInputActionEvent_t.OptionValue m_val;

		// Token: 0x020001C7 RID: 455
		[Serializable]
		public struct AnalogAction_t
		{
			// Token: 0x04000AC8 RID: 2760
			public InputAnalogActionHandle_t actionHandle;

			// Token: 0x04000AC9 RID: 2761
			public InputAnalogActionData_t analogActionData;
		}

		// Token: 0x020001C8 RID: 456
		[Serializable]
		public struct DigitalAction_t
		{
			// Token: 0x04000ACA RID: 2762
			public InputDigitalActionHandle_t actionHandle;

			// Token: 0x04000ACB RID: 2763
			public InputDigitalActionData_t digitalActionData;
		}

		// Token: 0x020001C9 RID: 457
		[Serializable]
		[StructLayout(LayoutKind.Explicit)]
		public struct OptionValue
		{
			// Token: 0x04000ACC RID: 2764
			[FieldOffset(0)]
			public SteamInputActionEvent_t.AnalogAction_t analogAction;

			// Token: 0x04000ACD RID: 2765
			[FieldOffset(0)]
			public SteamInputActionEvent_t.DigitalAction_t digitalAction;
		}
	}
}
