using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009FF RID: 2559
	internal abstract class RuntimeClass : __ComObject
	{
		// Token: 0x060064F4 RID: 25844
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetRedirectedGetHashCodeMD();

		// Token: 0x060064F5 RID: 25845
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern int RedirectGetHashCode(IntPtr pMD);

		// Token: 0x060064F6 RID: 25846 RVA: 0x00157DD4 File Offset: 0x00155FD4
		[SecuritySafeCritical]
		public override int GetHashCode()
		{
			IntPtr redirectedGetHashCodeMD = this.GetRedirectedGetHashCodeMD();
			if (redirectedGetHashCodeMD == IntPtr.Zero)
			{
				return base.GetHashCode();
			}
			return this.RedirectGetHashCode(redirectedGetHashCodeMD);
		}

		// Token: 0x060064F7 RID: 25847
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetRedirectedToStringMD();

		// Token: 0x060064F8 RID: 25848
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern string RedirectToString(IntPtr pMD);

		// Token: 0x060064F9 RID: 25849 RVA: 0x00157E04 File Offset: 0x00156004
		[SecuritySafeCritical]
		public override string ToString()
		{
			IStringable stringable = this as IStringable;
			if (stringable != null)
			{
				return stringable.ToString();
			}
			IntPtr redirectedToStringMD = this.GetRedirectedToStringMD();
			if (redirectedToStringMD == IntPtr.Zero)
			{
				return base.ToString();
			}
			return this.RedirectToString(redirectedToStringMD);
		}

		// Token: 0x060064FA RID: 25850
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetRedirectedEqualsMD();

		// Token: 0x060064FB RID: 25851
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern bool RedirectEquals(object obj, IntPtr pMD);

		// Token: 0x060064FC RID: 25852 RVA: 0x00157E44 File Offset: 0x00156044
		[SecuritySafeCritical]
		public override bool Equals(object obj)
		{
			IntPtr redirectedEqualsMD = this.GetRedirectedEqualsMD();
			if (redirectedEqualsMD == IntPtr.Zero)
			{
				return base.Equals(obj);
			}
			return this.RedirectEquals(obj, redirectedEqualsMD);
		}
	}
}
