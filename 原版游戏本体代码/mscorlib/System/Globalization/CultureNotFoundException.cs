using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003AA RID: 938
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class CultureNotFoundException : ArgumentException, ISerializable
	{
		// Token: 0x06002EA6 RID: 11942 RVA: 0x000B2480 File Offset: 0x000B0680
		[__DynamicallyInvokable]
		public CultureNotFoundException()
			: base(CultureNotFoundException.DefaultMessage)
		{
		}

		// Token: 0x06002EA7 RID: 11943 RVA: 0x000B248D File Offset: 0x000B068D
		[__DynamicallyInvokable]
		public CultureNotFoundException(string message)
			: base(message)
		{
		}

		// Token: 0x06002EA8 RID: 11944 RVA: 0x000B2496 File Offset: 0x000B0696
		[__DynamicallyInvokable]
		public CultureNotFoundException(string paramName, string message)
			: base(message, paramName)
		{
		}

		// Token: 0x06002EA9 RID: 11945 RVA: 0x000B24A0 File Offset: 0x000B06A0
		[__DynamicallyInvokable]
		public CultureNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06002EAA RID: 11946 RVA: 0x000B24AA File Offset: 0x000B06AA
		public CultureNotFoundException(string paramName, int invalidCultureId, string message)
			: base(message, paramName)
		{
			this.m_invalidCultureId = new int?(invalidCultureId);
		}

		// Token: 0x06002EAB RID: 11947 RVA: 0x000B24C0 File Offset: 0x000B06C0
		public CultureNotFoundException(string message, int invalidCultureId, Exception innerException)
			: base(message, innerException)
		{
			this.m_invalidCultureId = new int?(invalidCultureId);
		}

		// Token: 0x06002EAC RID: 11948 RVA: 0x000B24D6 File Offset: 0x000B06D6
		[__DynamicallyInvokable]
		public CultureNotFoundException(string paramName, string invalidCultureName, string message)
			: base(message, paramName)
		{
			this.m_invalidCultureName = invalidCultureName;
		}

		// Token: 0x06002EAD RID: 11949 RVA: 0x000B24E7 File Offset: 0x000B06E7
		[__DynamicallyInvokable]
		public CultureNotFoundException(string message, string invalidCultureName, Exception innerException)
			: base(message, innerException)
		{
			this.m_invalidCultureName = invalidCultureName;
		}

		// Token: 0x06002EAE RID: 11950 RVA: 0x000B24F8 File Offset: 0x000B06F8
		protected CultureNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.m_invalidCultureId = (int?)info.GetValue("InvalidCultureId", typeof(int?));
			this.m_invalidCultureName = (string)info.GetValue("InvalidCultureName", typeof(string));
		}

		// Token: 0x06002EAF RID: 11951 RVA: 0x000B2550 File Offset: 0x000B0750
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			int? num = null;
			num = this.m_invalidCultureId;
			info.AddValue("InvalidCultureId", num, typeof(int?));
			info.AddValue("InvalidCultureName", this.m_invalidCultureName, typeof(string));
		}

		// Token: 0x17000623 RID: 1571
		// (get) Token: 0x06002EB0 RID: 11952 RVA: 0x000B25B8 File Offset: 0x000B07B8
		public virtual int? InvalidCultureId
		{
			get
			{
				return this.m_invalidCultureId;
			}
		}

		// Token: 0x17000624 RID: 1572
		// (get) Token: 0x06002EB1 RID: 11953 RVA: 0x000B25C0 File Offset: 0x000B07C0
		[__DynamicallyInvokable]
		public virtual string InvalidCultureName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_invalidCultureName;
			}
		}

		// Token: 0x17000625 RID: 1573
		// (get) Token: 0x06002EB2 RID: 11954 RVA: 0x000B25C8 File Offset: 0x000B07C8
		private static string DefaultMessage
		{
			get
			{
				return Environment.GetResourceString("Argument_CultureNotSupported");
			}
		}

		// Token: 0x17000626 RID: 1574
		// (get) Token: 0x06002EB3 RID: 11955 RVA: 0x000B25D4 File Offset: 0x000B07D4
		private string FormatedInvalidCultureId
		{
			get
			{
				if (this.InvalidCultureId != null)
				{
					return string.Format(CultureInfo.InvariantCulture, "{0} (0x{0:x4})", this.InvalidCultureId.Value);
				}
				return this.InvalidCultureName;
			}
		}

		// Token: 0x17000627 RID: 1575
		// (get) Token: 0x06002EB4 RID: 11956 RVA: 0x000B261C File Offset: 0x000B081C
		[__DynamicallyInvokable]
		public override string Message
		{
			[__DynamicallyInvokable]
			get
			{
				string message = base.Message;
				if (this.m_invalidCultureId == null && this.m_invalidCultureName == null)
				{
					return message;
				}
				string resourceString = Environment.GetResourceString("Argument_CultureInvalidIdentifier", new object[] { this.FormatedInvalidCultureId });
				if (message == null)
				{
					return resourceString;
				}
				return message + Environment.NewLine + resourceString;
			}
		}

		// Token: 0x04001357 RID: 4951
		private string m_invalidCultureName;

		// Token: 0x04001358 RID: 4952
		private int? m_invalidCultureId;
	}
}
