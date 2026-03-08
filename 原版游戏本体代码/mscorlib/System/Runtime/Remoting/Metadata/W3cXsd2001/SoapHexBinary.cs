using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E7 RID: 2023
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapHexBinary : ISoapXsd
	{
		// Token: 0x17000E73 RID: 3699
		// (get) Token: 0x06005771 RID: 22385 RVA: 0x00135E94 File Offset: 0x00134094
		public static string XsdType
		{
			get
			{
				return "hexBinary";
			}
		}

		// Token: 0x06005772 RID: 22386 RVA: 0x00135E9B File Offset: 0x0013409B
		public string GetXsdType()
		{
			return SoapHexBinary.XsdType;
		}

		// Token: 0x06005773 RID: 22387 RVA: 0x00135EA2 File Offset: 0x001340A2
		public SoapHexBinary()
		{
		}

		// Token: 0x06005774 RID: 22388 RVA: 0x00135EB7 File Offset: 0x001340B7
		public SoapHexBinary(byte[] value)
		{
			this._value = value;
		}

		// Token: 0x17000E74 RID: 3700
		// (get) Token: 0x06005775 RID: 22389 RVA: 0x00135ED3 File Offset: 0x001340D3
		// (set) Token: 0x06005776 RID: 22390 RVA: 0x00135EDB File Offset: 0x001340DB
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

		// Token: 0x06005777 RID: 22391 RVA: 0x00135EE4 File Offset: 0x001340E4
		public override string ToString()
		{
			this.sb.Length = 0;
			for (int i = 0; i < this._value.Length; i++)
			{
				string text = this._value[i].ToString("X", CultureInfo.InvariantCulture);
				if (text.Length == 1)
				{
					this.sb.Append('0');
				}
				this.sb.Append(text);
			}
			return this.sb.ToString();
		}

		// Token: 0x06005778 RID: 22392 RVA: 0x00135F5B File Offset: 0x0013415B
		public static SoapHexBinary Parse(string value)
		{
			return new SoapHexBinary(SoapHexBinary.ToByteArray(SoapType.FilterBin64(value)));
		}

		// Token: 0x06005779 RID: 22393 RVA: 0x00135F70 File Offset: 0x00134170
		private static byte[] ToByteArray(string value)
		{
			char[] array = value.ToCharArray();
			if (array.Length % 2 != 0)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:hexBinary", value));
			}
			byte[] array2 = new byte[array.Length / 2];
			for (int i = 0; i < array.Length / 2; i++)
			{
				array2[i] = SoapHexBinary.ToByte(array[i * 2], value) * 16 + SoapHexBinary.ToByte(array[i * 2 + 1], value);
			}
			return array2;
		}

		// Token: 0x0600577A RID: 22394 RVA: 0x00135FE8 File Offset: 0x001341E8
		private static byte ToByte(char c, string value)
		{
			byte result = 0;
			string s = c.ToString();
			try
			{
				s = c.ToString();
				result = byte.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid", new object[] { "xsd:hexBinary", value }));
			}
			return result;
		}

		// Token: 0x04002818 RID: 10264
		private byte[] _value;

		// Token: 0x04002819 RID: 10265
		private StringBuilder sb = new StringBuilder(100);
	}
}
