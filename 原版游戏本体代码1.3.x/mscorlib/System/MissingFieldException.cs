using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x0200010F RID: 271
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MissingFieldException : MissingMemberException, ISerializable
	{
		// Token: 0x06001058 RID: 4184 RVA: 0x000310D5 File Offset: 0x0002F2D5
		[__DynamicallyInvokable]
		public MissingFieldException()
			: base(Environment.GetResourceString("Arg_MissingFieldException"))
		{
			base.SetErrorCode(-2146233071);
		}

		// Token: 0x06001059 RID: 4185 RVA: 0x000310F2 File Offset: 0x0002F2F2
		[__DynamicallyInvokable]
		public MissingFieldException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233071);
		}

		// Token: 0x0600105A RID: 4186 RVA: 0x00031106 File Offset: 0x0002F306
		[__DynamicallyInvokable]
		public MissingFieldException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233071);
		}

		// Token: 0x0600105B RID: 4187 RVA: 0x0003111B File Offset: 0x0002F31B
		protected MissingFieldException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x170001BA RID: 442
		// (get) Token: 0x0600105C RID: 4188 RVA: 0x00031128 File Offset: 0x0002F328
		[__DynamicallyInvokable]
		public override string Message
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				if (this.ClassName == null)
				{
					return base.Message;
				}
				return Environment.GetResourceString("MissingField_Name", new object[] { ((this.Signature != null) ? (MissingMemberException.FormatSignature(this.Signature) + " ") : "") + this.ClassName + "." + this.MemberName });
			}
		}

		// Token: 0x0600105D RID: 4189 RVA: 0x00031191 File Offset: 0x0002F391
		private MissingFieldException(string className, string fieldName, byte[] signature)
		{
			this.ClassName = className;
			this.MemberName = fieldName;
			this.Signature = signature;
		}

		// Token: 0x0600105E RID: 4190 RVA: 0x000311AE File Offset: 0x0002F3AE
		public MissingFieldException(string className, string fieldName)
		{
			this.ClassName = className;
			this.MemberName = fieldName;
		}
	}
}
