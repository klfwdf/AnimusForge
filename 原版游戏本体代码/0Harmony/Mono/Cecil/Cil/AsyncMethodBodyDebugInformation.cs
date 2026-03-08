using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000318 RID: 792
	internal sealed class AsyncMethodBodyDebugInformation : CustomDebugInformation
	{
		// Token: 0x1700053F RID: 1343
		// (get) Token: 0x06001485 RID: 5253 RVA: 0x0004112B File Offset: 0x0003F32B
		// (set) Token: 0x06001486 RID: 5254 RVA: 0x00041133 File Offset: 0x0003F333
		public InstructionOffset CatchHandler
		{
			get
			{
				return this.catch_handler;
			}
			set
			{
				this.catch_handler = value;
			}
		}

		// Token: 0x17000540 RID: 1344
		// (get) Token: 0x06001487 RID: 5255 RVA: 0x0004113C File Offset: 0x0003F33C
		public Collection<InstructionOffset> Yields
		{
			get
			{
				if (this.yields == null)
				{
					Interlocked.CompareExchange<Collection<InstructionOffset>>(ref this.yields, new Collection<InstructionOffset>(), null);
				}
				return this.yields;
			}
		}

		// Token: 0x17000541 RID: 1345
		// (get) Token: 0x06001488 RID: 5256 RVA: 0x0004115E File Offset: 0x0003F35E
		public Collection<InstructionOffset> Resumes
		{
			get
			{
				if (this.resumes == null)
				{
					Interlocked.CompareExchange<Collection<InstructionOffset>>(ref this.resumes, new Collection<InstructionOffset>(), null);
				}
				return this.resumes;
			}
		}

		// Token: 0x17000542 RID: 1346
		// (get) Token: 0x06001489 RID: 5257 RVA: 0x00041180 File Offset: 0x0003F380
		public Collection<MethodDefinition> ResumeMethods
		{
			get
			{
				Collection<MethodDefinition> result;
				if ((result = this.resume_methods) == null)
				{
					result = (this.resume_methods = new Collection<MethodDefinition>());
				}
				return result;
			}
		}

		// Token: 0x17000543 RID: 1347
		// (get) Token: 0x0600148A RID: 5258 RVA: 0x000411A5 File Offset: 0x0003F3A5
		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.AsyncMethodBody;
			}
		}

		// Token: 0x0600148B RID: 5259 RVA: 0x000411A8 File Offset: 0x0003F3A8
		internal AsyncMethodBodyDebugInformation(int catchHandler)
			: base(AsyncMethodBodyDebugInformation.KindIdentifier)
		{
			this.catch_handler = new InstructionOffset(catchHandler);
		}

		// Token: 0x0600148C RID: 5260 RVA: 0x000411C1 File Offset: 0x0003F3C1
		public AsyncMethodBodyDebugInformation(Instruction catchHandler)
			: base(AsyncMethodBodyDebugInformation.KindIdentifier)
		{
			this.catch_handler = new InstructionOffset(catchHandler);
		}

		// Token: 0x0600148D RID: 5261 RVA: 0x000411DA File Offset: 0x0003F3DA
		public AsyncMethodBodyDebugInformation()
			: base(AsyncMethodBodyDebugInformation.KindIdentifier)
		{
			this.catch_handler = new InstructionOffset(-1);
		}

		// Token: 0x04000A48 RID: 2632
		internal InstructionOffset catch_handler;

		// Token: 0x04000A49 RID: 2633
		internal Collection<InstructionOffset> yields;

		// Token: 0x04000A4A RID: 2634
		internal Collection<InstructionOffset> resumes;

		// Token: 0x04000A4B RID: 2635
		internal Collection<MethodDefinition> resume_methods;

		// Token: 0x04000A4C RID: 2636
		public static Guid KindIdentifier = new Guid("{54FD2AC5-E925-401A-9C2A-F94F171072F8}");
	}
}
