using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200090F RID: 2319
	[AttributeUsage(AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class UnmanagedFunctionPointerAttribute : Attribute
	{
		// Token: 0x06005FEE RID: 24558 RVA: 0x0014B6AE File Offset: 0x001498AE
		[__DynamicallyInvokable]
		public UnmanagedFunctionPointerAttribute(CallingConvention callingConvention)
		{
			this.m_callingConvention = callingConvention;
		}

		// Token: 0x170010CE RID: 4302
		// (get) Token: 0x06005FEF RID: 24559 RVA: 0x0014B6BD File Offset: 0x001498BD
		[__DynamicallyInvokable]
		public CallingConvention CallingConvention
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_callingConvention;
			}
		}

		// Token: 0x04002A5E RID: 10846
		private CallingConvention m_callingConvention;

		// Token: 0x04002A5F RID: 10847
		[__DynamicallyInvokable]
		public CharSet CharSet;

		// Token: 0x04002A60 RID: 10848
		[__DynamicallyInvokable]
		public bool BestFitMapping;

		// Token: 0x04002A61 RID: 10849
		[__DynamicallyInvokable]
		public bool ThrowOnUnmappableChar;

		// Token: 0x04002A62 RID: 10850
		[__DynamicallyInvokable]
		public bool SetLastError;
	}
}
