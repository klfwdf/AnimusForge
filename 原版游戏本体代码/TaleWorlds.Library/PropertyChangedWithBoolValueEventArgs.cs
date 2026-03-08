using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200004A RID: 74
	public struct PropertyChangedWithBoolValueEventArgs
	{
		// Token: 0x06000264 RID: 612 RVA: 0x00007BBB File Offset: 0x00005DBB
		public PropertyChangedWithBoolValueEventArgs(string propertyName, bool value)
		{
			this.PropertyName = propertyName;
			this.Value = value;
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000265 RID: 613 RVA: 0x00007BCB File Offset: 0x00005DCB
		public string PropertyName { get; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000266 RID: 614 RVA: 0x00007BD3 File Offset: 0x00005DD3
		public bool Value { get; }
	}
}
