using System;

namespace Mono.Cecil
{
	// Token: 0x02000235 RID: 565
	internal abstract class EventReference : MemberReference
	{
		// Token: 0x17000283 RID: 643
		// (get) Token: 0x06000C29 RID: 3113 RVA: 0x0002AAC1 File Offset: 0x00028CC1
		// (set) Token: 0x06000C2A RID: 3114 RVA: 0x0002AAC9 File Offset: 0x00028CC9
		public TypeReference EventType
		{
			get
			{
				return this.event_type;
			}
			set
			{
				this.event_type = value;
			}
		}

		// Token: 0x17000284 RID: 644
		// (get) Token: 0x06000C2B RID: 3115 RVA: 0x0002AAD2 File Offset: 0x00028CD2
		public override string FullName
		{
			get
			{
				return this.event_type.FullName + " " + base.MemberFullName();
			}
		}

		// Token: 0x06000C2C RID: 3116 RVA: 0x0002AAEF File Offset: 0x00028CEF
		protected EventReference(string name, TypeReference eventType)
			: base(name)
		{
			Mixin.CheckType(eventType, Mixin.Argument.eventType);
			this.event_type = eventType;
		}

		// Token: 0x06000C2D RID: 3117 RVA: 0x0002AB07 File Offset: 0x00028D07
		protected override IMemberDefinition ResolveDefinition()
		{
			return this.Resolve();
		}

		// Token: 0x06000C2E RID: 3118
		public new abstract EventDefinition Resolve();

		// Token: 0x040003AF RID: 943
		private TypeReference event_type;
	}
}
