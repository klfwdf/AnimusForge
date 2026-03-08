using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
	// Token: 0x020000CF RID: 207
	[Serializable]
	internal struct Currency
	{
		// Token: 0x06000CE8 RID: 3304 RVA: 0x000275DA File Offset: 0x000257DA
		public Currency(decimal value)
		{
			this.m_value = decimal.ToCurrency(value).m_value;
		}

		// Token: 0x06000CE9 RID: 3305 RVA: 0x000275ED File Offset: 0x000257ED
		internal Currency(long value, int ignored)
		{
			this.m_value = value;
		}

		// Token: 0x06000CEA RID: 3306 RVA: 0x000275F6 File Offset: 0x000257F6
		public static Currency FromOACurrency(long cy)
		{
			return new Currency(cy, 0);
		}

		// Token: 0x06000CEB RID: 3307 RVA: 0x000275FF File Offset: 0x000257FF
		public long ToOACurrency()
		{
			return this.m_value;
		}

		// Token: 0x06000CEC RID: 3308 RVA: 0x00027608 File Offset: 0x00025808
		[SecuritySafeCritical]
		public static decimal ToDecimal(Currency c)
		{
			decimal result = 0m;
			Currency.FCallToDecimal(ref result, c);
			return result;
		}

		// Token: 0x06000CED RID: 3309
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallToDecimal(ref decimal result, Currency c);

		// Token: 0x04000539 RID: 1337
		internal long m_value;
	}
}
