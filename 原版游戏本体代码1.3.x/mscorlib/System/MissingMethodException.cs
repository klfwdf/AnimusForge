using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x02000111 RID: 273
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MissingMethodException : MissingMemberException, ISerializable
	{
		// Token: 0x06001068 RID: 4200 RVA: 0x00031374 File Offset: 0x0002F574
		[__DynamicallyInvokable]
		public MissingMethodException()
			: base(Environment.GetResourceString("Arg_MissingMethodException"))
		{
			base.SetErrorCode(-2146233069);
		}

		// Token: 0x06001069 RID: 4201 RVA: 0x00031391 File Offset: 0x0002F591
		[__DynamicallyInvokable]
		public MissingMethodException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233069);
		}

		// Token: 0x0600106A RID: 4202 RVA: 0x000313A5 File Offset: 0x0002F5A5
		[__DynamicallyInvokable]
		public MissingMethodException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233069);
		}

		// Token: 0x0600106B RID: 4203 RVA: 0x000313BA File Offset: 0x0002F5BA
		protected MissingMethodException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x170001BC RID: 444
		// (get) Token: 0x0600106C RID: 4204 RVA: 0x000313C4 File Offset: 0x0002F5C4
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
				return Environment.GetResourceString("MissingMethod_Name", new object[] { this.ClassName + "." + this.MemberName + ((this.Signature != null) ? (" " + MissingMemberException.FormatSignature(this.Signature)) : "") });
			}
		}

		// Token: 0x0600106D RID: 4205 RVA: 0x0003142D File Offset: 0x0002F62D
		private MissingMethodException(string className, string methodName, byte[] signature)
		{
			this.ClassName = className;
			this.MemberName = methodName;
			this.Signature = signature;
		}

		// Token: 0x0600106E RID: 4206 RVA: 0x0003144A File Offset: 0x0002F64A
		public MissingMethodException(string className, string methodName)
		{
			this.ClassName = className;
			this.MemberName = methodName;
		}
	}
}
