using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
	// Token: 0x020000A9 RID: 169
	public struct ArgIterator
	{
		// Token: 0x060009B2 RID: 2482
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern ArgIterator(IntPtr arglist);

		// Token: 0x060009B3 RID: 2483 RVA: 0x0001F64A File Offset: 0x0001D84A
		[SecuritySafeCritical]
		public ArgIterator(RuntimeArgumentHandle arglist)
		{
			this = new ArgIterator(arglist.Value);
		}

		// Token: 0x060009B4 RID: 2484
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe extern ArgIterator(IntPtr arglist, void* ptr);

		// Token: 0x060009B5 RID: 2485 RVA: 0x0001F659 File Offset: 0x0001D859
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe ArgIterator(RuntimeArgumentHandle arglist, void* ptr)
		{
			this = new ArgIterator(arglist.Value, ptr);
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x0001F66C File Offset: 0x0001D86C
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe TypedReference GetNextArg()
		{
			TypedReference result = default(TypedReference);
			this.FCallGetNextArg((void*)(&result));
			return result;
		}

		// Token: 0x060009B7 RID: 2487
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe extern void FCallGetNextArg(void* result);

		// Token: 0x060009B8 RID: 2488 RVA: 0x0001F68C File Offset: 0x0001D88C
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe TypedReference GetNextArg(RuntimeTypeHandle rth)
		{
			if (this.sigPtr != IntPtr.Zero)
			{
				return this.GetNextArg();
			}
			if (this.ArgPtr == IntPtr.Zero)
			{
				throw new ArgumentNullException();
			}
			TypedReference result = default(TypedReference);
			this.InternalGetNextArg((void*)(&result), rth.GetRuntimeType());
			return result;
		}

		// Token: 0x060009B9 RID: 2489
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe extern void InternalGetNextArg(void* result, RuntimeType rt);

		// Token: 0x060009BA RID: 2490 RVA: 0x0001F6E3 File Offset: 0x0001D8E3
		public void End()
		{
		}

		// Token: 0x060009BB RID: 2491
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern int GetRemainingCount();

		// Token: 0x060009BC RID: 2492
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe extern void* _GetNextArgType();

		// Token: 0x060009BD RID: 2493 RVA: 0x0001F6E5 File Offset: 0x0001D8E5
		[SecuritySafeCritical]
		public RuntimeTypeHandle GetNextArgType()
		{
			return new RuntimeTypeHandle(Type.GetTypeFromHandleUnsafe((IntPtr)this._GetNextArgType()));
		}

		// Token: 0x060009BE RID: 2494 RVA: 0x0001F6FC File Offset: 0x0001D8FC
		public override int GetHashCode()
		{
			return ValueType.GetHashCodeOfPtr(this.ArgCookie);
		}

		// Token: 0x060009BF RID: 2495 RVA: 0x0001F709 File Offset: 0x0001D909
		public override bool Equals(object o)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NYI"));
		}

		// Token: 0x040003D0 RID: 976
		private IntPtr ArgCookie;

		// Token: 0x040003D1 RID: 977
		private IntPtr sigPtr;

		// Token: 0x040003D2 RID: 978
		private IntPtr sigPtrLen;

		// Token: 0x040003D3 RID: 979
		private IntPtr ArgPtr;

		// Token: 0x040003D4 RID: 980
		private int RemainingArgs;
	}
}
