using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000448 RID: 1096
	[__DynamicallyInvokable]
	public struct EventSourceOptions
	{
		// Token: 0x17000806 RID: 2054
		// (get) Token: 0x06003635 RID: 13877 RVA: 0x000D2A1A File Offset: 0x000D0C1A
		// (set) Token: 0x06003636 RID: 13878 RVA: 0x000D2A22 File Offset: 0x000D0C22
		[__DynamicallyInvokable]
		public EventLevel Level
		{
			[__DynamicallyInvokable]
			get
			{
				return (EventLevel)this.level;
			}
			[__DynamicallyInvokable]
			set
			{
				this.level = checked((byte)value);
				this.valuesSet |= 4;
			}
		}

		// Token: 0x17000807 RID: 2055
		// (get) Token: 0x06003637 RID: 13879 RVA: 0x000D2A3B File Offset: 0x000D0C3B
		// (set) Token: 0x06003638 RID: 13880 RVA: 0x000D2A43 File Offset: 0x000D0C43
		[__DynamicallyInvokable]
		public EventOpcode Opcode
		{
			[__DynamicallyInvokable]
			get
			{
				return (EventOpcode)this.opcode;
			}
			[__DynamicallyInvokable]
			set
			{
				this.opcode = checked((byte)value);
				this.valuesSet |= 8;
			}
		}

		// Token: 0x17000808 RID: 2056
		// (get) Token: 0x06003639 RID: 13881 RVA: 0x000D2A5C File Offset: 0x000D0C5C
		internal bool IsOpcodeSet
		{
			get
			{
				return (this.valuesSet & 8) > 0;
			}
		}

		// Token: 0x17000809 RID: 2057
		// (get) Token: 0x0600363A RID: 13882 RVA: 0x000D2A69 File Offset: 0x000D0C69
		// (set) Token: 0x0600363B RID: 13883 RVA: 0x000D2A71 File Offset: 0x000D0C71
		[__DynamicallyInvokable]
		public EventKeywords Keywords
		{
			[__DynamicallyInvokable]
			get
			{
				return this.keywords;
			}
			[__DynamicallyInvokable]
			set
			{
				this.keywords = value;
				this.valuesSet |= 1;
			}
		}

		// Token: 0x1700080A RID: 2058
		// (get) Token: 0x0600363C RID: 13884 RVA: 0x000D2A89 File Offset: 0x000D0C89
		// (set) Token: 0x0600363D RID: 13885 RVA: 0x000D2A91 File Offset: 0x000D0C91
		[__DynamicallyInvokable]
		public EventTags Tags
		{
			[__DynamicallyInvokable]
			get
			{
				return this.tags;
			}
			[__DynamicallyInvokable]
			set
			{
				this.tags = value;
				this.valuesSet |= 2;
			}
		}

		// Token: 0x1700080B RID: 2059
		// (get) Token: 0x0600363E RID: 13886 RVA: 0x000D2AA9 File Offset: 0x000D0CA9
		// (set) Token: 0x0600363F RID: 13887 RVA: 0x000D2AB1 File Offset: 0x000D0CB1
		[__DynamicallyInvokable]
		public EventActivityOptions ActivityOptions
		{
			[__DynamicallyInvokable]
			get
			{
				return this.activityOptions;
			}
			[__DynamicallyInvokable]
			set
			{
				this.activityOptions = value;
				this.valuesSet |= 16;
			}
		}

		// Token: 0x04001838 RID: 6200
		internal EventKeywords keywords;

		// Token: 0x04001839 RID: 6201
		internal EventTags tags;

		// Token: 0x0400183A RID: 6202
		internal EventActivityOptions activityOptions;

		// Token: 0x0400183B RID: 6203
		internal byte level;

		// Token: 0x0400183C RID: 6204
		internal byte opcode;

		// Token: 0x0400183D RID: 6205
		internal byte valuesSet;

		// Token: 0x0400183E RID: 6206
		internal const byte keywordsSet = 1;

		// Token: 0x0400183F RID: 6207
		internal const byte tagsSet = 2;

		// Token: 0x04001840 RID: 6208
		internal const byte levelSet = 4;

		// Token: 0x04001841 RID: 6209
		internal const byte opcodeSet = 8;

		// Token: 0x04001842 RID: 6210
		internal const byte activityOptionsSet = 16;
	}
}
