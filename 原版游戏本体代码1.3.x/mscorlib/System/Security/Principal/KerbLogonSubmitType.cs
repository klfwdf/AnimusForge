using System;

namespace System.Security.Principal
{
	// Token: 0x0200032D RID: 813
	[Serializable]
	internal enum KerbLogonSubmitType
	{
		// Token: 0x04001046 RID: 4166
		KerbInteractiveLogon = 2,
		// Token: 0x04001047 RID: 4167
		KerbSmartCardLogon = 6,
		// Token: 0x04001048 RID: 4168
		KerbWorkstationUnlockLogon,
		// Token: 0x04001049 RID: 4169
		KerbSmartCardUnlockLogon,
		// Token: 0x0400104A RID: 4170
		KerbProxyLogon,
		// Token: 0x0400104B RID: 4171
		KerbTicketLogon,
		// Token: 0x0400104C RID: 4172
		KerbTicketUnlockLogon,
		// Token: 0x0400104D RID: 4173
		KerbS4ULogon
	}
}
