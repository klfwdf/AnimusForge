using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200004D RID: 77
	public struct PropertyChangedWithUIntValueEventArgs
	{
		// Token: 0x0600026D RID: 621 RVA: 0x00007C1B File Offset: 0x00005E1B
		public PropertyChangedWithUIntValueEventArgs(string propertyName, uint value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600026E RID: 622 RVA: 0x00007C2B File Offset: 0x00005E2B
		public string PropertyName { get; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x0600026F RID: 623 RVA: 0x00007C33 File Offset: 0x00005E33
		public uint Value { get; }
	}
}
