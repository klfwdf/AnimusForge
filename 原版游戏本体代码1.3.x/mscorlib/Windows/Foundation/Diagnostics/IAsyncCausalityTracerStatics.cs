using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Windows.Foundation.Diagnostics
{
	// Token: 0x02000031 RID: 49
	[Guid("50850B26-267E-451B-A890-AB6A370245EE")]
	[ComImport]
	internal interface IAsyncCausalityTracerStatics
	{
		// Token: 0x060001E3 RID: 483
		void TraceOperationCreation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, string operationName, ulong relatedContext);

		// Token: 0x060001E4 RID: 484
		void TraceOperationCompletion(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, AsyncCausalityStatus status);

		// Token: 0x060001E5 RID: 485
		void TraceOperationRelation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalityRelation relation);

		// Token: 0x060001E6 RID: 486
		void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalitySynchronousWork work);

		// Token: 0x060001E7 RID: 487
		void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySource source, CausalitySynchronousWork work);

		// Token: 0x060001E8 RID: 488
		EventRegistrationToken add_TracingStatusChanged(EventHandler<TracingStatusChangedEventArgs> eventHandler);

		// Token: 0x060001E9 RID: 489
		void remove_TracingStatusChanged(EventRegistrationToken token);
	}
}
