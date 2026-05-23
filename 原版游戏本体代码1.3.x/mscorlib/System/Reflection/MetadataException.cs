using System;
using System.Globalization;

namespace System.Reflection
{
	// Token: 0x020005FF RID: 1535
	internal class MetadataException : Exception
	{
		// Token: 0x060046D1 RID: 18129 RVA: 0x00103006 File Offset: 0x00101206
		internal MetadataException(int hr)
		{
			this.m_hr = hr;
		}

		// Token: 0x060046D2 RID: 18130 RVA: 0x00103015 File Offset: 0x00101215
		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "MetadataException HResult = {0:x}.", this.m_hr);
		}

		// Token: 0x04001D57 RID: 7511
		private int m_hr;
	}
}
