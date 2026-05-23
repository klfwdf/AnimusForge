using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200004F RID: 79
	public struct PropertyChangedWithDoubleValueEventArgs
	{
		// Token: 0x06000273 RID: 627 RVA: 0x00007C5B File Offset: 0x00005E5B
		public PropertyChangedWithDoubleValueEventArgs(string propertyName, double value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000274 RID: 628 RVA: 0x00007C6B File Offset: 0x00005E6B
		public string PropertyName { get; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000275 RID: 629 RVA: 0x00007C73 File Offset: 0x00005E73
		public double Value { get; }
	}
}
