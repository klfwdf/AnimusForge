using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000646 RID: 1606
	internal static class IcedFeatures
	{
		// Token: 0x1700077A RID: 1914
		// (get) Token: 0x0600217D RID: 8573 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasGasFormatter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700077B RID: 1915
		// (get) Token: 0x0600217E RID: 8574 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasIntelFormatter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700077C RID: 1916
		// (get) Token: 0x0600217F RID: 8575 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasMasmFormatter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700077D RID: 1917
		// (get) Token: 0x06002180 RID: 8576 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasNasmFormatter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700077E RID: 1918
		// (get) Token: 0x06002181 RID: 8577 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasFastFormatter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700077F RID: 1919
		// (get) Token: 0x06002182 RID: 8578 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public static bool HasDecoder
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000780 RID: 1920
		// (get) Token: 0x06002183 RID: 8579 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public static bool HasEncoder
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000781 RID: 1921
		// (get) Token: 0x06002184 RID: 8580 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public static bool HasBlockEncoder
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000782 RID: 1922
		// (get) Token: 0x06002185 RID: 8581 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasOpCodeInfo
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000783 RID: 1923
		// (get) Token: 0x06002186 RID: 8582 RVA: 0x0001B69F File Offset: 0x0001989F
		public static bool HasInstructionInfo
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06002187 RID: 8583 RVA: 0x0006D232 File Offset: 0x0006B432
		public static void Initialize()
		{
			RuntimeHelpers.RunClassConstructor(typeof(Decoder).TypeHandle);
		}
	}
}
