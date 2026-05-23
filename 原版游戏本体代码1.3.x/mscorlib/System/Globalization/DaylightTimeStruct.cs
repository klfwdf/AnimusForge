using System;

namespace System.Globalization
{
	// Token: 0x020003B5 RID: 949
	internal struct DaylightTimeStruct
	{
		// Token: 0x06002F46 RID: 12102 RVA: 0x000B5A21 File Offset: 0x000B3C21
		public DaylightTimeStruct(DateTime start, DateTime end, TimeSpan delta)
		{
			this.Start = start;
			this.End = end;
			this.Delta = delta;
		}

		// Token: 0x17000664 RID: 1636
		// (get) Token: 0x06002F47 RID: 12103 RVA: 0x000B5A38 File Offset: 0x000B3C38
		public DateTime Start { get; }

		// Token: 0x17000665 RID: 1637
		// (get) Token: 0x06002F48 RID: 12104 RVA: 0x000B5A40 File Offset: 0x000B3C40
		public DateTime End { get; }

		// Token: 0x17000666 RID: 1638
		// (get) Token: 0x06002F49 RID: 12105 RVA: 0x000B5A48 File Offset: 0x000B3C48
		public TimeSpan Delta { get; }
	}
}
