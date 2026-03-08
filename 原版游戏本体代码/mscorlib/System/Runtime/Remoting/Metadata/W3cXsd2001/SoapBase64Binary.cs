using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E8 RID: 2024
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapBase64Binary : ISoapXsd
	{
		// Token: 0x17000E75 RID: 3701
		// (get) Token: 0x0600577B RID: 22395 RVA: 0x00136050 File Offset: 0x00134250
		public static string XsdType
		{
			get
			{
				return "base64Binary";
			}
		}

		// Token: 0x0600577C RID: 22396 RVA: 0x00136057 File Offset: 0x00134257
		public string GetXsdType()
		{
			return SoapBase64Binary.XsdType;
		}

		// Token: 0x0600577D RID: 22397 RVA: 0x0013605E File Offset: 0x0013425E
		public SoapBase64Binary()
		{
		}

		// Token: 0x0600577E RID: 22398 RVA: 0x00136066 File Offset: 0x00134266
		public SoapBase64Binary(byte[] value)
		{
			this._value = value;
		}

		// Token: 0x17000E76 RID: 3702
		// (get) Token: 0x0600577F RID: 22399 RVA: 0x00136075 File Offset: 0x00134275
		// (set) Token: 0x06005780 RID: 22400 RVA: 0x0013607D File Offset: 0x0013427D
		public byte[] Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = value;
			}
		}

		// Token: 0x06005781 RID: 22401 RVA: 0x00136086 File Offset: 0x00134286
		public override string ToString()
		{
			if (this._value == null)
			{
				return null;
			}
			return SoapType.LineFeedsBin64(Convert.ToBase64String(this._value));
		}

		// Token: 0x06005782 RID: 22402 RVA: 0x001360A4 File Offset: 0x001342A4
		public static SoapBase64Binary Parse(string value)
		{
			if (value == null || value.Length == 0)
			{
				return new SoapBase64Binary(new byte[0]);
			}
			byte[] value2;
			try
			{
				value2 = Convert.FromBase64String(SoapType.FilterBin64(value));
			}
			catch (Exception)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "base64Binary", value));
			}
			return new SoapBase64Binary(value2);
		}

		// Token: 0x0400281A RID: 10266
		private byte[] _value;
	}
}
