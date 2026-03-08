using System;
using System.Runtime.Serialization;

namespace System.Threading.Tasks
{
	// Token: 0x02000574 RID: 1396
	[__DynamicallyInvokable]
	[Serializable]
	public class TaskSchedulerException : Exception
	{
		// Token: 0x06004192 RID: 16786 RVA: 0x000F4DB6 File Offset: 0x000F2FB6
		[__DynamicallyInvokable]
		public TaskSchedulerException()
			: base(Environment.GetResourceString("TaskSchedulerException_ctor_DefaultMessage"))
		{
		}

		// Token: 0x06004193 RID: 16787 RVA: 0x000F4DC8 File Offset: 0x000F2FC8
		[__DynamicallyInvokable]
		public TaskSchedulerException(string message)
			: base(message)
		{
		}

		// Token: 0x06004194 RID: 16788 RVA: 0x000F4DD1 File Offset: 0x000F2FD1
		[__DynamicallyInvokable]
		public TaskSchedulerException(Exception innerException)
			: base(Environment.GetResourceString("TaskSchedulerException_ctor_DefaultMessage"), innerException)
		{
		}

		// Token: 0x06004195 RID: 16789 RVA: 0x000F4DE4 File Offset: 0x000F2FE4
		[__DynamicallyInvokable]
		public TaskSchedulerException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06004196 RID: 16790 RVA: 0x000F4DEE File Offset: 0x000F2FEE
		protected TaskSchedulerException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
