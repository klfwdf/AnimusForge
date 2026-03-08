using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002D0 RID: 720
	internal struct Triple<T1, T2, T3>
	{
		// Token: 0x06002566 RID: 9574 RVA: 0x000888DC File Offset: 0x00086ADC
		internal Triple(T1 first, T2 second, T3 third)
		{
			this._first = first;
			this._second = second;
			this._third = third;
		}

		// Token: 0x1700049D RID: 1181
		// (get) Token: 0x06002567 RID: 9575 RVA: 0x000888F3 File Offset: 0x00086AF3
		public T1 Item1
		{
			get
			{
				return this._first;
			}
		}

		// Token: 0x1700049E RID: 1182
		// (get) Token: 0x06002568 RID: 9576 RVA: 0x000888FB File Offset: 0x00086AFB
		public T2 Item2
		{
			get
			{
				return this._second;
			}
		}

		// Token: 0x1700049F RID: 1183
		// (get) Token: 0x06002569 RID: 9577 RVA: 0x00088903 File Offset: 0x00086B03
		public T3 Item3
		{
			get
			{
				return this._third;
			}
		}

		// Token: 0x04000E1C RID: 3612
		private readonly T1 _first;

		// Token: 0x04000E1D RID: 3613
		private readonly T2 _second;

		// Token: 0x04000E1E RID: 3614
		private readonly T3 _third;
	}
}
