using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x020000A8 RID: 168
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class ArgumentOutOfRangeException : ArgumentException, ISerializable
	{
		// Token: 0x17000145 RID: 325
		// (get) Token: 0x060009A8 RID: 2472 RVA: 0x0001F4FF File Offset: 0x0001D6FF
		private static string RangeMessage
		{
			get
			{
				if (ArgumentOutOfRangeException._rangeMessage == null)
				{
					ArgumentOutOfRangeException._rangeMessage = Environment.GetResourceString("Arg_ArgumentOutOfRangeException");
				}
				return ArgumentOutOfRangeException._rangeMessage;
			}
		}

		// Token: 0x060009A9 RID: 2473 RVA: 0x0001F522 File Offset: 0x0001D722
		[__DynamicallyInvokable]
		public ArgumentOutOfRangeException()
			: base(ArgumentOutOfRangeException.RangeMessage)
		{
			base.SetErrorCode(-2146233086);
		}

		// Token: 0x060009AA RID: 2474 RVA: 0x0001F53A File Offset: 0x0001D73A
		[__DynamicallyInvokable]
		public ArgumentOutOfRangeException(string paramName)
			: base(ArgumentOutOfRangeException.RangeMessage, paramName)
		{
			base.SetErrorCode(-2146233086);
		}

		// Token: 0x060009AB RID: 2475 RVA: 0x0001F553 File Offset: 0x0001D753
		[__DynamicallyInvokable]
		public ArgumentOutOfRangeException(string paramName, string message)
			: base(message, paramName)
		{
			base.SetErrorCode(-2146233086);
		}

		// Token: 0x060009AC RID: 2476 RVA: 0x0001F568 File Offset: 0x0001D768
		[__DynamicallyInvokable]
		public ArgumentOutOfRangeException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233086);
		}

		// Token: 0x060009AD RID: 2477 RVA: 0x0001F57D File Offset: 0x0001D77D
		[__DynamicallyInvokable]
		public ArgumentOutOfRangeException(string paramName, object actualValue, string message)
			: base(message, paramName)
		{
			this.m_actualValue = actualValue;
			base.SetErrorCode(-2146233086);
		}

		// Token: 0x17000146 RID: 326
		// (get) Token: 0x060009AE RID: 2478 RVA: 0x0001F59C File Offset: 0x0001D79C
		[__DynamicallyInvokable]
		public override string Message
		{
			[__DynamicallyInvokable]
			get
			{
				string message = base.Message;
				if (this.m_actualValue == null)
				{
					return message;
				}
				string resourceString = Environment.GetResourceString("ArgumentOutOfRange_ActualValue", new object[] { this.m_actualValue.ToString() });
				if (message == null)
				{
					return resourceString;
				}
				return message + Environment.NewLine + resourceString;
			}
		}

		// Token: 0x17000147 RID: 327
		// (get) Token: 0x060009AF RID: 2479 RVA: 0x0001F5EA File Offset: 0x0001D7EA
		[__DynamicallyInvokable]
		public virtual object ActualValue
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_actualValue;
			}
		}

		// Token: 0x060009B0 RID: 2480 RVA: 0x0001F5F2 File Offset: 0x0001D7F2
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("ActualValue", this.m_actualValue, typeof(object));
		}

		// Token: 0x060009B1 RID: 2481 RVA: 0x0001F625 File Offset: 0x0001D825
		protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.m_actualValue = info.GetValue("ActualValue", typeof(object));
		}

		// Token: 0x040003CE RID: 974
		private static volatile string _rangeMessage;

		// Token: 0x040003CF RID: 975
		private object m_actualValue;
	}
}
