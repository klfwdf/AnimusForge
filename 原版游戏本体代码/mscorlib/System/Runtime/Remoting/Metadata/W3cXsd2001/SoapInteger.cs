using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E9 RID: 2025
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapInteger : ISoapXsd
	{
		// Token: 0x17000E77 RID: 3703
		// (get) Token: 0x06005783 RID: 22403 RVA: 0x00136110 File Offset: 0x00134310
		public static string XsdType
		{
			get
			{
				return "integer";
			}
		}

		// Token: 0x06005784 RID: 22404 RVA: 0x00136117 File Offset: 0x00134317
		public string GetXsdType()
		{
			return SoapInteger.XsdType;
		}

		// Token: 0x06005785 RID: 22405 RVA: 0x0013611E File Offset: 0x0013431E
		public SoapInteger()
		{
		}

		// Token: 0x06005786 RID: 22406 RVA: 0x00136126 File Offset: 0x00134326
		public SoapInteger(decimal value)
		{
			this._value = decimal.Truncate(value);
		}

		// Token: 0x17000E78 RID: 3704
		// (get) Token: 0x06005787 RID: 22407 RVA: 0x0013613A File Offset: 0x0013433A
		// (set) Token: 0x06005788 RID: 22408 RVA: 0x00136142 File Offset: 0x00134342
		public decimal Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = decimal.Truncate(value);
			}
		}

		// Token: 0x06005789 RID: 22409 RVA: 0x00136150 File Offset: 0x00134350
		public override string ToString()
		{
			return this._value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x0600578A RID: 22410 RVA: 0x00136162 File Offset: 0x00134362
		public static SoapInteger Parse(string value)
		{
			return new SoapInteger(decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		// Token: 0x0400281B RID: 10267
		private decimal _value;
	}
}
