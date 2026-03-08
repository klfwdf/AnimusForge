using System;
using System.Runtime.Serialization;

namespace System.Threading.Tasks
{
	// Token: 0x02000573 RID: 1395
	[__DynamicallyInvokable]
	[Serializable]
	public class TaskCanceledException : OperationCanceledException
	{
		// Token: 0x0600418C RID: 16780 RVA: 0x000F4D44 File Offset: 0x000F2F44
		[__DynamicallyInvokable]
		public TaskCanceledException()
			: base(Environment.GetResourceString("TaskCanceledException_ctor_DefaultMessage"))
		{
		}

		// Token: 0x0600418D RID: 16781 RVA: 0x000F4D56 File Offset: 0x000F2F56
		[__DynamicallyInvokable]
		public TaskCanceledException(string message)
			: base(message)
		{
		}

		// Token: 0x0600418E RID: 16782 RVA: 0x000F4D5F File Offset: 0x000F2F5F
		[__DynamicallyInvokable]
		public TaskCanceledException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x0600418F RID: 16783 RVA: 0x000F4D6C File Offset: 0x000F2F6C
		[__DynamicallyInvokable]
		public TaskCanceledException(Task task)
			: base(Environment.GetResourceString("TaskCanceledException_ctor_DefaultMessage"), (task != null) ? task.CancellationToken : default(CancellationToken))
		{
			this.m_canceledTask = task;
		}

		// Token: 0x06004190 RID: 16784 RVA: 0x000F4DA4 File Offset: 0x000F2FA4
		protected TaskCanceledException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x170009C2 RID: 2498
		// (get) Token: 0x06004191 RID: 16785 RVA: 0x000F4DAE File Offset: 0x000F2FAE
		[__DynamicallyInvokable]
		public Task Task
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_canceledTask;
			}
		}

		// Token: 0x04001B62 RID: 7010
		[NonSerialized]
		private Task m_canceledTask;
	}
}
