using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.Diagnostics.Contracts.Internal
{
	// Token: 0x02000419 RID: 1049
	[Obsolete("Use the ContractHelper class in the System.Runtime.CompilerServices namespace instead.")]
	[__DynamicallyInvokable]
	public static class ContractHelper
	{
		// Token: 0x0600342D RID: 13357 RVA: 0x000C6D9D File Offset: 0x000C4F9D
		[DebuggerNonUserCode]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static string RaiseContractFailedEvent(ContractFailureKind failureKind, string userMessage, string conditionText, Exception innerException)
		{
			return ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
		}

		// Token: 0x0600342E RID: 13358 RVA: 0x000C6DA8 File Offset: 0x000C4FA8
		[DebuggerNonUserCode]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void TriggerFailure(ContractFailureKind kind, string displayMessage, string userMessage, string conditionText, Exception innerException)
		{
			ContractHelper.TriggerFailure(kind, displayMessage, userMessage, conditionText, innerException);
		}
	}
}
