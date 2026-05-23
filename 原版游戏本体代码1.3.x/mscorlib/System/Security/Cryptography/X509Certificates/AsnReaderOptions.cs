using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002B6 RID: 694
	internal struct AsnReaderOptions
	{
		// Token: 0x17000499 RID: 1177
		// (get) Token: 0x060024D1 RID: 9425 RVA: 0x00085547 File Offset: 0x00083747
		// (set) Token: 0x060024D2 RID: 9426 RVA: 0x0008555D File Offset: 0x0008375D
		public int UtcTimeTwoDigitYearMax
		{
			get
			{
				if (this._twoDigitYearMax == 0)
				{
					return 2049;
				}
				return (int)this._twoDigitYearMax;
			}
			set
			{
				if (value < 1 || value > 9999)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._twoDigitYearMax = (ushort)value;
			}
		}

		// Token: 0x1700049A RID: 1178
		// (get) Token: 0x060024D3 RID: 9427 RVA: 0x0008557E File Offset: 0x0008377E
		// (set) Token: 0x060024D4 RID: 9428 RVA: 0x00085586 File Offset: 0x00083786
		public bool SkipSetSortOrderVerification
		{
			get
			{
				return this._skipSetSortOrderVerification;
			}
			set
			{
				this._skipSetSortOrderVerification = value;
			}
		}

		// Token: 0x04000DC6 RID: 3526
		private const int DefaultTwoDigitMax = 2049;

		// Token: 0x04000DC7 RID: 3527
		private ushort _twoDigitYearMax;

		// Token: 0x04000DC8 RID: 3528
		private bool _skipSetSortOrderVerification;
	}
}
