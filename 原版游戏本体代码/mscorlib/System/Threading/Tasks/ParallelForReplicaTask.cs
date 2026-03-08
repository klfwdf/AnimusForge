using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000562 RID: 1378
	internal class ParallelForReplicaTask : Task
	{
		// Token: 0x06004153 RID: 16723 RVA: 0x000F3EF0 File Offset: 0x000F20F0
		internal ParallelForReplicaTask(Action<object> taskReplicaDelegate, object stateObject, Task parentTask, TaskScheduler taskScheduler, TaskCreationOptions creationOptionsForReplica, InternalTaskOptions internalOptionsForReplica)
			: base(taskReplicaDelegate, stateObject, parentTask, default(CancellationToken), creationOptionsForReplica, internalOptionsForReplica, taskScheduler)
		{
		}

		// Token: 0x170009BE RID: 2494
		// (get) Token: 0x06004154 RID: 16724 RVA: 0x000F3F15 File Offset: 0x000F2115
		// (set) Token: 0x06004155 RID: 16725 RVA: 0x000F3F1D File Offset: 0x000F211D
		internal override object SavedStateForNextReplica
		{
			get
			{
				return this.m_stateForNextReplica;
			}
			set
			{
				this.m_stateForNextReplica = value;
			}
		}

		// Token: 0x170009BF RID: 2495
		// (get) Token: 0x06004156 RID: 16726 RVA: 0x000F3F26 File Offset: 0x000F2126
		// (set) Token: 0x06004157 RID: 16727 RVA: 0x000F3F2E File Offset: 0x000F212E
		internal override object SavedStateFromPreviousReplica
		{
			get
			{
				return this.m_stateFromPreviousReplica;
			}
			set
			{
				this.m_stateFromPreviousReplica = value;
			}
		}

		// Token: 0x170009C0 RID: 2496
		// (get) Token: 0x06004158 RID: 16728 RVA: 0x000F3F37 File Offset: 0x000F2137
		// (set) Token: 0x06004159 RID: 16729 RVA: 0x000F3F3F File Offset: 0x000F213F
		internal override Task HandedOverChildReplica
		{
			get
			{
				return this.m_handedOverChildReplica;
			}
			set
			{
				this.m_handedOverChildReplica = value;
			}
		}

		// Token: 0x04001B24 RID: 6948
		internal object m_stateForNextReplica;

		// Token: 0x04001B25 RID: 6949
		internal object m_stateFromPreviousReplica;

		// Token: 0x04001B26 RID: 6950
		internal Task m_handedOverChildReplica;
	}
}
