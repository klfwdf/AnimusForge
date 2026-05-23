using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200004C RID: 76
	public struct PropertyChangedWithFloatValueEventArgs
	{
		// Token: 0x0600026A RID: 618 RVA: 0x00007BFB File Offset: 0x00005DFB
		public PropertyChangedWithFloatValueEventArgs(string propertyName, float value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600026B RID: 619 RVA: 0x00007C0B File Offset: 0x00005E0B
		public string PropertyName { get; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600026C RID: 620 RVA: 0x00007C13 File Offset: 0x00005E13
		public float Value { get; }
	}
}
