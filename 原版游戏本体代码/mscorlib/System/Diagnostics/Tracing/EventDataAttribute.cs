using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000441 RID: 1089
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	[__DynamicallyInvokable]
	public class EventDataAttribute : Attribute
	{
		// Token: 0x170007F6 RID: 2038
		// (get) Token: 0x060035FB RID: 13819 RVA: 0x000D2447 File Offset: 0x000D0647
		// (set) Token: 0x060035FC RID: 13820 RVA: 0x000D244F File Offset: 0x000D064F
		[__DynamicallyInvokable]
		public string Name
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007F7 RID: 2039
		// (get) Token: 0x060035FD RID: 13821 RVA: 0x000D2458 File Offset: 0x000D0658
		// (set) Token: 0x060035FE RID: 13822 RVA: 0x000D2460 File Offset: 0x000D0660
		internal EventLevel Level
		{
			get
			{
				return this.level;
			}
			set
			{
				this.level = value;
			}
		}

		// Token: 0x170007F8 RID: 2040
		// (get) Token: 0x060035FF RID: 13823 RVA: 0x000D2469 File Offset: 0x000D0669
		// (set) Token: 0x06003600 RID: 13824 RVA: 0x000D2471 File Offset: 0x000D0671
		internal EventOpcode Opcode
		{
			get
			{
				return this.opcode;
			}
			set
			{
				this.opcode = value;
			}
		}

		// Token: 0x170007F9 RID: 2041
		// (get) Token: 0x06003601 RID: 13825 RVA: 0x000D247A File Offset: 0x000D067A
		// (set) Token: 0x06003602 RID: 13826 RVA: 0x000D2482 File Offset: 0x000D0682
		internal EventKeywords Keywords { get; set; }

		// Token: 0x170007FA RID: 2042
		// (get) Token: 0x06003603 RID: 13827 RVA: 0x000D248B File Offset: 0x000D068B
		// (set) Token: 0x06003604 RID: 13828 RVA: 0x000D2493 File Offset: 0x000D0693
		internal EventTags Tags { get; set; }

		// Token: 0x06003605 RID: 13829 RVA: 0x000D249C File Offset: 0x000D069C
		[__DynamicallyInvokable]
		public EventDataAttribute()
		{
		}

		// Token: 0x0400181E RID: 6174
		private EventLevel level = (EventLevel)(-1);

		// Token: 0x0400181F RID: 6175
		private EventOpcode opcode = (EventOpcode)(-1);
	}
}
