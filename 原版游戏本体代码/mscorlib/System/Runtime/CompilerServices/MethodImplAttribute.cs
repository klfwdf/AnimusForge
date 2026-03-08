using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008BF RID: 2239
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class MethodImplAttribute : Attribute
	{
		// Token: 0x06005DB4 RID: 23988 RVA: 0x001498D0 File Offset: 0x00147AD0
		internal MethodImplAttribute(MethodImplAttributes methodImplAttributes)
		{
			MethodImplOptions methodImplOptions = MethodImplOptions.Unmanaged | MethodImplOptions.ForwardRef | MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall | MethodImplOptions.Synchronized | MethodImplOptions.NoInlining | MethodImplOptions.AggressiveInlining | MethodImplOptions.NoOptimization | MethodImplOptions.SecurityMitigations;
			this._val = (MethodImplOptions)(methodImplAttributes & (MethodImplAttributes)methodImplOptions);
		}

		// Token: 0x06005DB5 RID: 23989 RVA: 0x001498F2 File Offset: 0x00147AF2
		[__DynamicallyInvokable]
		public MethodImplAttribute(MethodImplOptions methodImplOptions)
		{
			this._val = methodImplOptions;
		}

		// Token: 0x06005DB6 RID: 23990 RVA: 0x00149901 File Offset: 0x00147B01
		public MethodImplAttribute(short value)
		{
			this._val = (MethodImplOptions)value;
		}

		// Token: 0x06005DB7 RID: 23991 RVA: 0x00149910 File Offset: 0x00147B10
		public MethodImplAttribute()
		{
		}

		// Token: 0x17001018 RID: 4120
		// (get) Token: 0x06005DB8 RID: 23992 RVA: 0x00149918 File Offset: 0x00147B18
		[__DynamicallyInvokable]
		public MethodImplOptions Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A2A RID: 10794
		internal MethodImplOptions _val;

		// Token: 0x04002A2B RID: 10795
		public MethodCodeType MethodCodeType;
	}
}
