using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x02000110 RID: 272
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MissingMemberException : MemberAccessException, ISerializable
	{
		// Token: 0x0600105F RID: 4191 RVA: 0x000311C4 File Offset: 0x0002F3C4
		[__DynamicallyInvokable]
		public MissingMemberException()
			: base(Environment.GetResourceString("Arg_MissingMemberException"))
		{
			base.SetErrorCode(-2146233070);
		}

		// Token: 0x06001060 RID: 4192 RVA: 0x000311E1 File Offset: 0x0002F3E1
		[__DynamicallyInvokable]
		public MissingMemberException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233070);
		}

		// Token: 0x06001061 RID: 4193 RVA: 0x000311F5 File Offset: 0x0002F3F5
		[__DynamicallyInvokable]
		public MissingMemberException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233070);
		}

		// Token: 0x06001062 RID: 4194 RVA: 0x0003120C File Offset: 0x0002F40C
		protected MissingMemberException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.ClassName = info.GetString("MMClassName");
			this.MemberName = info.GetString("MMMemberName");
			this.Signature = (byte[])info.GetValue("MMSignature", typeof(byte[]));
		}

		// Token: 0x170001BB RID: 443
		// (get) Token: 0x06001063 RID: 4195 RVA: 0x00031264 File Offset: 0x0002F464
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
				return Environment.GetResourceString("MissingMember_Name", new object[] { this.ClassName + "." + this.MemberName + ((this.Signature != null) ? (" " + MissingMemberException.FormatSignature(this.Signature)) : "") });
			}
		}

		// Token: 0x06001064 RID: 4196
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string FormatSignature(byte[] signature);

		// Token: 0x06001065 RID: 4197 RVA: 0x000312CD File Offset: 0x0002F4CD
		private MissingMemberException(string className, string memberName, byte[] signature)
		{
			this.ClassName = className;
			this.MemberName = memberName;
			this.Signature = signature;
		}

		// Token: 0x06001066 RID: 4198 RVA: 0x000312EA File Offset: 0x0002F4EA
		public MissingMemberException(string className, string memberName)
		{
			this.ClassName = className;
			this.MemberName = memberName;
		}

		// Token: 0x06001067 RID: 4199 RVA: 0x00031300 File Offset: 0x0002F500
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("MMClassName", this.ClassName, typeof(string));
			info.AddValue("MMMemberName", this.MemberName, typeof(string));
			info.AddValue("MMSignature", this.Signature, typeof(byte[]));
		}

		// Token: 0x040005C3 RID: 1475
		protected string ClassName;

		// Token: 0x040005C4 RID: 1476
		protected string MemberName;

		// Token: 0x040005C5 RID: 1477
		protected byte[] Signature;
	}
}
