using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000427 RID: 1063
	[AttributeUsage(AttributeTargets.Method)]
	[__DynamicallyInvokable]
	public sealed class EventAttribute : Attribute
	{
		// Token: 0x06003530 RID: 13616 RVA: 0x000CE764 File Offset: 0x000CC964
		[__DynamicallyInvokable]
		public EventAttribute(int eventId)
		{
			this.EventId = eventId;
			this.Level = EventLevel.Informational;
			this.m_opcodeSet = false;
		}

		// Token: 0x170007E5 RID: 2021
		// (get) Token: 0x06003531 RID: 13617 RVA: 0x000CE781 File Offset: 0x000CC981
		// (set) Token: 0x06003532 RID: 13618 RVA: 0x000CE789 File Offset: 0x000CC989
		[__DynamicallyInvokable]
		public int EventId
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x170007E6 RID: 2022
		// (get) Token: 0x06003533 RID: 13619 RVA: 0x000CE792 File Offset: 0x000CC992
		// (set) Token: 0x06003534 RID: 13620 RVA: 0x000CE79A File Offset: 0x000CC99A
		[__DynamicallyInvokable]
		public EventLevel Level
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007E7 RID: 2023
		// (get) Token: 0x06003535 RID: 13621 RVA: 0x000CE7A3 File Offset: 0x000CC9A3
		// (set) Token: 0x06003536 RID: 13622 RVA: 0x000CE7AB File Offset: 0x000CC9AB
		[__DynamicallyInvokable]
		public EventKeywords Keywords
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007E8 RID: 2024
		// (get) Token: 0x06003537 RID: 13623 RVA: 0x000CE7B4 File Offset: 0x000CC9B4
		// (set) Token: 0x06003538 RID: 13624 RVA: 0x000CE7BC File Offset: 0x000CC9BC
		[__DynamicallyInvokable]
		public EventOpcode Opcode
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_opcode;
			}
			[__DynamicallyInvokable]
			set
			{
				this.m_opcode = value;
				this.m_opcodeSet = true;
			}
		}

		// Token: 0x170007E9 RID: 2025
		// (get) Token: 0x06003539 RID: 13625 RVA: 0x000CE7CC File Offset: 0x000CC9CC
		internal bool IsOpcodeSet
		{
			get
			{
				return this.m_opcodeSet;
			}
		}

		// Token: 0x170007EA RID: 2026
		// (get) Token: 0x0600353A RID: 13626 RVA: 0x000CE7D4 File Offset: 0x000CC9D4
		// (set) Token: 0x0600353B RID: 13627 RVA: 0x000CE7DC File Offset: 0x000CC9DC
		[__DynamicallyInvokable]
		public EventTask Task
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007EB RID: 2027
		// (get) Token: 0x0600353C RID: 13628 RVA: 0x000CE7E5 File Offset: 0x000CC9E5
		// (set) Token: 0x0600353D RID: 13629 RVA: 0x000CE7ED File Offset: 0x000CC9ED
		[__DynamicallyInvokable]
		public EventChannel Channel
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007EC RID: 2028
		// (get) Token: 0x0600353E RID: 13630 RVA: 0x000CE7F6 File Offset: 0x000CC9F6
		// (set) Token: 0x0600353F RID: 13631 RVA: 0x000CE7FE File Offset: 0x000CC9FE
		[__DynamicallyInvokable]
		public byte Version
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007ED RID: 2029
		// (get) Token: 0x06003540 RID: 13632 RVA: 0x000CE807 File Offset: 0x000CCA07
		// (set) Token: 0x06003541 RID: 13633 RVA: 0x000CE80F File Offset: 0x000CCA0F
		[__DynamicallyInvokable]
		public string Message
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007EE RID: 2030
		// (get) Token: 0x06003542 RID: 13634 RVA: 0x000CE818 File Offset: 0x000CCA18
		// (set) Token: 0x06003543 RID: 13635 RVA: 0x000CE820 File Offset: 0x000CCA20
		[__DynamicallyInvokable]
		public EventTags Tags
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007EF RID: 2031
		// (get) Token: 0x06003544 RID: 13636 RVA: 0x000CE829 File Offset: 0x000CCA29
		// (set) Token: 0x06003545 RID: 13637 RVA: 0x000CE831 File Offset: 0x000CCA31
		[__DynamicallyInvokable]
		public EventActivityOptions ActivityOptions
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x040017A3 RID: 6051
		private EventOpcode m_opcode;

		// Token: 0x040017A4 RID: 6052
		private bool m_opcodeSet;
	}
}
