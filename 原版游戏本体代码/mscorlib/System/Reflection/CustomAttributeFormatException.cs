using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
	// Token: 0x020005CE RID: 1486
	[ComVisible(true)]
	[Serializable]
	public class CustomAttributeFormatException : FormatException
	{
		// Token: 0x060044DC RID: 17628 RVA: 0x000FD328 File Offset: 0x000FB528
		public CustomAttributeFormatException()
			: base(Environment.GetResourceString("Arg_CustomAttributeFormatException"))
		{
			base.SetErrorCode(-2146232827);
		}

		// Token: 0x060044DD RID: 17629 RVA: 0x000FD345 File Offset: 0x000FB545
		public CustomAttributeFormatException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232827);
		}

		// Token: 0x060044DE RID: 17630 RVA: 0x000FD359 File Offset: 0x000FB559
		public CustomAttributeFormatException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146232827);
		}

		// Token: 0x060044DF RID: 17631 RVA: 0x000FD36E File Offset: 0x000FB56E
		protected CustomAttributeFormatException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
