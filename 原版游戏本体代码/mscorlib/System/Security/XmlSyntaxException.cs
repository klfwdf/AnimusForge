using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Security
{
	// Token: 0x020001C0 RID: 448
	[ComVisible(true)]
	[Serializable]
	public sealed class XmlSyntaxException : SystemException
	{
		// Token: 0x06001C13 RID: 7187 RVA: 0x00060DB6 File Offset: 0x0005EFB6
		public XmlSyntaxException()
			: base(Environment.GetResourceString("XMLSyntax_InvalidSyntax"))
		{
			base.SetErrorCode(-2146233320);
		}

		// Token: 0x06001C14 RID: 7188 RVA: 0x00060DD3 File Offset: 0x0005EFD3
		public XmlSyntaxException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233320);
		}

		// Token: 0x06001C15 RID: 7189 RVA: 0x00060DE7 File Offset: 0x0005EFE7
		public XmlSyntaxException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233320);
		}

		// Token: 0x06001C16 RID: 7190 RVA: 0x00060DFC File Offset: 0x0005EFFC
		public XmlSyntaxException(int lineNumber)
			: base(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("XMLSyntax_SyntaxError"), lineNumber))
		{
			base.SetErrorCode(-2146233320);
		}

		// Token: 0x06001C17 RID: 7191 RVA: 0x00060E29 File Offset: 0x0005F029
		public XmlSyntaxException(int lineNumber, string message)
			: base(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("XMLSyntax_SyntaxErrorEx"), lineNumber, message))
		{
			base.SetErrorCode(-2146233320);
		}

		// Token: 0x06001C18 RID: 7192 RVA: 0x00060E57 File Offset: 0x0005F057
		internal XmlSyntaxException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
