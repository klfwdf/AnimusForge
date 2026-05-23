using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000439 RID: 1081
	[Flags]
	[__DynamicallyInvokable]
	public enum EventKeywords : long
	{
		// Token: 0x04001804 RID: 6148
		[__DynamicallyInvokable]
		None = 0L,
		// Token: 0x04001805 RID: 6149
		[__DynamicallyInvokable]
		All = -1L,
		// Token: 0x04001806 RID: 6150
		MicrosoftTelemetry = 562949953421312L,
		// Token: 0x04001807 RID: 6151
		[__DynamicallyInvokable]
		WdiContext = 562949953421312L,
		// Token: 0x04001808 RID: 6152
		[__DynamicallyInvokable]
		WdiDiagnostic = 1125899906842624L,
		// Token: 0x04001809 RID: 6153
		[__DynamicallyInvokable]
		Sqm = 2251799813685248L,
		// Token: 0x0400180A RID: 6154
		[__DynamicallyInvokable]
		AuditFailure = 4503599627370496L,
		// Token: 0x0400180B RID: 6155
		[__DynamicallyInvokable]
		AuditSuccess = 9007199254740992L,
		// Token: 0x0400180C RID: 6156
		[__DynamicallyInvokable]
		CorrelationHint = 4503599627370496L,
		// Token: 0x0400180D RID: 6157
		[__DynamicallyInvokable]
		EventLogClassic = 36028797018963968L
	}
}
