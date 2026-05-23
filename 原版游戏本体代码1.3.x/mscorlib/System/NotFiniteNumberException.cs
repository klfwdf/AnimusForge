using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x02000114 RID: 276
	[ComVisible(true)]
	[Serializable]
	public class NotFiniteNumberException : ArithmeticException
	{
		// Token: 0x06001076 RID: 4214 RVA: 0x000314E0 File Offset: 0x0002F6E0
		public NotFiniteNumberException()
			: base(Environment.GetResourceString("Arg_NotFiniteNumberException"))
		{
			this._offendingNumber = 0.0;
			base.SetErrorCode(-2146233048);
		}

		// Token: 0x06001077 RID: 4215 RVA: 0x0003150C File Offset: 0x0002F70C
		public NotFiniteNumberException(double offendingNumber)
		{
			this._offendingNumber = offendingNumber;
			base.SetErrorCode(-2146233048);
		}

		// Token: 0x06001078 RID: 4216 RVA: 0x00031526 File Offset: 0x0002F726
		public NotFiniteNumberException(string message)
			: base(message)
		{
			this._offendingNumber = 0.0;
			base.SetErrorCode(-2146233048);
		}

		// Token: 0x06001079 RID: 4217 RVA: 0x00031549 File Offset: 0x0002F749
		public NotFiniteNumberException(string message, double offendingNumber)
			: base(message)
		{
			this._offendingNumber = offendingNumber;
			base.SetErrorCode(-2146233048);
		}

		// Token: 0x0600107A RID: 4218 RVA: 0x00031564 File Offset: 0x0002F764
		public NotFiniteNumberException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233048);
		}

		// Token: 0x0600107B RID: 4219 RVA: 0x00031579 File Offset: 0x0002F779
		public NotFiniteNumberException(string message, double offendingNumber, Exception innerException)
			: base(message, innerException)
		{
			this._offendingNumber = offendingNumber;
			base.SetErrorCode(-2146233048);
		}

		// Token: 0x0600107C RID: 4220 RVA: 0x00031595 File Offset: 0x0002F795
		protected NotFiniteNumberException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this._offendingNumber = (double)info.GetInt32("OffendingNumber");
		}

		// Token: 0x170001BD RID: 445
		// (get) Token: 0x0600107D RID: 4221 RVA: 0x000315B1 File Offset: 0x0002F7B1
		public double OffendingNumber
		{
			get
			{
				return this._offendingNumber;
			}
		}

		// Token: 0x0600107E RID: 4222 RVA: 0x000315B9 File Offset: 0x0002F7B9
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("OffendingNumber", this._offendingNumber, typeof(int));
		}

		// Token: 0x040005C6 RID: 1478
		private double _offendingNumber;
	}
}
