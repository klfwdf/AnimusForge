using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x020000A6 RID: 166
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class ArgumentException : SystemException, ISerializable
	{
		// Token: 0x0600099A RID: 2458 RVA: 0x0001F374 File Offset: 0x0001D574
		[__DynamicallyInvokable]
		public ArgumentException()
			: base(Environment.GetResourceString("Arg_ArgumentException"))
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600099B RID: 2459 RVA: 0x0001F391 File Offset: 0x0001D591
		[__DynamicallyInvokable]
		public ArgumentException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600099C RID: 2460 RVA: 0x0001F3A5 File Offset: 0x0001D5A5
		[__DynamicallyInvokable]
		public ArgumentException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600099D RID: 2461 RVA: 0x0001F3BA File Offset: 0x0001D5BA
		[__DynamicallyInvokable]
		public ArgumentException(string message, string paramName, Exception innerException)
			: base(message, innerException)
		{
			this.m_paramName = paramName;
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600099E RID: 2462 RVA: 0x0001F3D6 File Offset: 0x0001D5D6
		[__DynamicallyInvokable]
		public ArgumentException(string message, string paramName)
			: base(message)
		{
			this.m_paramName = paramName;
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600099F RID: 2463 RVA: 0x0001F3F1 File Offset: 0x0001D5F1
		protected ArgumentException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.m_paramName = info.GetString("ParamName");
		}

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x060009A0 RID: 2464 RVA: 0x0001F40C File Offset: 0x0001D60C
		[__DynamicallyInvokable]
		public override string Message
		{
			[__DynamicallyInvokable]
			get
			{
				string message = base.Message;
				if (!string.IsNullOrEmpty(this.m_paramName))
				{
					string resourceString = Environment.GetResourceString("Arg_ParamName_Name", new object[] { this.m_paramName });
					return message + Environment.NewLine + resourceString;
				}
				return message;
			}
		}

		// Token: 0x17000144 RID: 324
		// (get) Token: 0x060009A1 RID: 2465 RVA: 0x0001F455 File Offset: 0x0001D655
		[__DynamicallyInvokable]
		public virtual string ParamName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_paramName;
			}
		}

		// Token: 0x060009A2 RID: 2466 RVA: 0x0001F45D File Offset: 0x0001D65D
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("ParamName", this.m_paramName, typeof(string));
		}

		// Token: 0x040003CD RID: 973
		private string m_paramName;
	}
}
