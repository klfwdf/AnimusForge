using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000050 RID: 80
	public struct PropertyChangedWithVec2ValueEventArgs
	{
		// Token: 0x06000276 RID: 630 RVA: 0x00007C7B File Offset: 0x00005E7B
		public PropertyChangedWithVec2ValueEventArgs(string propertyName, Vec2 value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000277 RID: 631 RVA: 0x00007C8B File Offset: 0x00005E8B
		public string PropertyName { get; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000278 RID: 632 RVA: 0x00007C93 File Offset: 0x00005E93
		public Vec2 Value { get; }
	}
}
