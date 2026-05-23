using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
	// Token: 0x02000621 RID: 1569
	[ComVisible(true)]
	[Serializable]
	public class TargetException : ApplicationException
	{
		// Token: 0x060048B3 RID: 18611 RVA: 0x0010797F File Offset: 0x00105B7F
		public TargetException()
		{
			base.SetErrorCode(-2146232829);
		}

		// Token: 0x060048B4 RID: 18612 RVA: 0x00107992 File Offset: 0x00105B92
		public TargetException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232829);
		}

		// Token: 0x060048B5 RID: 18613 RVA: 0x001079A6 File Offset: 0x00105BA6
		public TargetException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146232829);
		}

		// Token: 0x060048B6 RID: 18614 RVA: 0x001079BB File Offset: 0x00105BBB
		protected TargetException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
