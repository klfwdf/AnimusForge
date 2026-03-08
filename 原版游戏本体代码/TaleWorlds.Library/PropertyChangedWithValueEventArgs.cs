using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000049 RID: 73
	public struct PropertyChangedWithValueEventArgs
	{
		// Token: 0x06000261 RID: 609 RVA: 0x00007B9B File Offset: 0x00005D9B
		public PropertyChangedWithValueEventArgs(string propertyName, object value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000262 RID: 610 RVA: 0x00007BAB File Offset: 0x00005DAB
		public string PropertyName { get; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000263 RID: 611 RVA: 0x00007BB3 File Offset: 0x00005DB3
		public object Value { get; }
	}
}
