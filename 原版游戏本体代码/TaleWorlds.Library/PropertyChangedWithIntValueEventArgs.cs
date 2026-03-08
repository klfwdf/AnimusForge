using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200004B RID: 75
	public struct PropertyChangedWithIntValueEventArgs
	{
		// Token: 0x06000267 RID: 615 RVA: 0x00007BDB File Offset: 0x00005DDB
		public PropertyChangedWithIntValueEventArgs(string propertyName, int value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000268 RID: 616 RVA: 0x00007BEB File Offset: 0x00005DEB
		public string PropertyName { get; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000269 RID: 617 RVA: 0x00007BF3 File Offset: 0x00005DF3
		public int Value { get; }
	}
}
