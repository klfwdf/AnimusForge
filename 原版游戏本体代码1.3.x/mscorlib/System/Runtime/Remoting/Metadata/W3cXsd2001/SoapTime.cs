using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E0 RID: 2016
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapTime : ISoapXsd
	{
		// Token: 0x17000E62 RID: 3682
		// (get) Token: 0x06005729 RID: 22313 RVA: 0x0013576C File Offset: 0x0013396C
		public static string XsdType
		{
			get
			{
				return "time";
			}
		}

		// Token: 0x0600572A RID: 22314 RVA: 0x00135773 File Offset: 0x00133973
		public string GetXsdType()
		{
			return SoapTime.XsdType;
		}

		// Token: 0x0600572B RID: 22315 RVA: 0x0013577A File Offset: 0x0013397A
		public SoapTime()
		{
		}

		// Token: 0x0600572C RID: 22316 RVA: 0x0013578D File Offset: 0x0013398D
		public SoapTime(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x17000E63 RID: 3683
		// (get) Token: 0x0600572D RID: 22317 RVA: 0x001357A7 File Offset: 0x001339A7
		// (set) Token: 0x0600572E RID: 22318 RVA: 0x001357AF File Offset: 0x001339AF
		public DateTime Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);
			}
		}

		// Token: 0x0600572F RID: 22319 RVA: 0x001357DB File Offset: 0x001339DB
		public override string ToString()
		{
			return this._value.ToString("HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture);
		}

		// Token: 0x06005730 RID: 22320 RVA: 0x001357F4 File Offset: 0x001339F4
		public static SoapTime Parse(string value)
		{
			string s = value;
			if (value.EndsWith("Z", StringComparison.Ordinal))
			{
				s = value.Substring(0, value.Length - 1) + "-00:00";
			}
			return new SoapTime(DateTime.ParseExact(s, SoapTime.formats, CultureInfo.InvariantCulture, DateTimeStyles.None));
		}

		// Token: 0x04002807 RID: 10247
		private DateTime _value = DateTime.MinValue;

		// Token: 0x04002808 RID: 10248
		private static string[] formats = new string[]
		{
			"HH:mm:ss.fffffffzzz", "HH:mm:ss.ffff", "HH:mm:ss.ffffzzz", "HH:mm:ss.fff", "HH:mm:ss.fffzzz", "HH:mm:ss.ff", "HH:mm:ss.ffzzz", "HH:mm:ss.f", "HH:mm:ss.fzzz", "HH:mm:ss",
			"HH:mm:sszzz", "HH:mm:ss.fffff", "HH:mm:ss.fffffzzz", "HH:mm:ss.ffffff", "HH:mm:ss.ffffffzzz", "HH:mm:ss.fffffff", "HH:mm:ss.ffffffff", "HH:mm:ss.ffffffffzzz", "HH:mm:ss.fffffffff", "HH:mm:ss.fffffffffzzz",
			"HH:mm:ss.fffffffff", "HH:mm:ss.fffffffffzzz"
		};
	}
}
