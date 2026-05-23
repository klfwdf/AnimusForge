using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002EE RID: 750
	internal sealed class ExceptionHandler
	{
		// Token: 0x170004E6 RID: 1254
		// (get) Token: 0x0600134D RID: 4941 RVA: 0x0003D5D6 File Offset: 0x0003B7D6
		// (set) Token: 0x0600134E RID: 4942 RVA: 0x0003D5DE File Offset: 0x0003B7DE
		public Instruction TryStart
		{
			get
			{
				return this.try_start;
			}
			set
			{
				this.try_start = value;
			}
		}

		// Token: 0x170004E7 RID: 1255
		// (get) Token: 0x0600134F RID: 4943 RVA: 0x0003D5E7 File Offset: 0x0003B7E7
		// (set) Token: 0x06001350 RID: 4944 RVA: 0x0003D5EF File Offset: 0x0003B7EF
		public Instruction TryEnd
		{
			get
			{
				return this.try_end;
			}
			set
			{
				this.try_end = value;
			}
		}

		// Token: 0x170004E8 RID: 1256
		// (get) Token: 0x06001351 RID: 4945 RVA: 0x0003D5F8 File Offset: 0x0003B7F8
		// (set) Token: 0x06001352 RID: 4946 RVA: 0x0003D600 File Offset: 0x0003B800
		public Instruction FilterStart
		{
			get
			{
				return this.filter_start;
			}
			set
			{
				this.filter_start = value;
			}
		}

		// Token: 0x170004E9 RID: 1257
		// (get) Token: 0x06001353 RID: 4947 RVA: 0x0003D609 File Offset: 0x0003B809
		// (set) Token: 0x06001354 RID: 4948 RVA: 0x0003D611 File Offset: 0x0003B811
		public Instruction HandlerStart
		{
			get
			{
				return this.handler_start;
			}
			set
			{
				this.handler_start = value;
			}
		}

		// Token: 0x170004EA RID: 1258
		// (get) Token: 0x06001355 RID: 4949 RVA: 0x0003D61A File Offset: 0x0003B81A
		// (set) Token: 0x06001356 RID: 4950 RVA: 0x0003D622 File Offset: 0x0003B822
		public Instruction HandlerEnd
		{
			get
			{
				return this.handler_end;
			}
			set
			{
				this.handler_end = value;
			}
		}

		// Token: 0x170004EB RID: 1259
		// (get) Token: 0x06001357 RID: 4951 RVA: 0x0003D62B File Offset: 0x0003B82B
		// (set) Token: 0x06001358 RID: 4952 RVA: 0x0003D633 File Offset: 0x0003B833
		public TypeReference CatchType
		{
			get
			{
				return this.catch_type;
			}
			set
			{
				this.catch_type = value;
			}
		}

		// Token: 0x170004EC RID: 1260
		// (get) Token: 0x06001359 RID: 4953 RVA: 0x0003D63C File Offset: 0x0003B83C
		// (set) Token: 0x0600135A RID: 4954 RVA: 0x0003D644 File Offset: 0x0003B844
		public ExceptionHandlerType HandlerType
		{
			get
			{
				return this.handler_type;
			}
			set
			{
				this.handler_type = value;
			}
		}

		// Token: 0x0600135B RID: 4955 RVA: 0x0003D64D File Offset: 0x0003B84D
		public ExceptionHandler(ExceptionHandlerType handlerType)
		{
			this.handler_type = handlerType;
		}

		// Token: 0x040008A2 RID: 2210
		private Instruction try_start;

		// Token: 0x040008A3 RID: 2211
		private Instruction try_end;

		// Token: 0x040008A4 RID: 2212
		private Instruction filter_start;

		// Token: 0x040008A5 RID: 2213
		private Instruction handler_start;

		// Token: 0x040008A6 RID: 2214
		private Instruction handler_end;

		// Token: 0x040008A7 RID: 2215
		private TypeReference catch_type;

		// Token: 0x040008A8 RID: 2216
		private ExceptionHandlerType handler_type;
	}
}
