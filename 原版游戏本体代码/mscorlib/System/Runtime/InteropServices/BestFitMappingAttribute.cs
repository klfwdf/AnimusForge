using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200093E RID: 2366
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class BestFitMappingAttribute : Attribute
	{
		// Token: 0x0600605D RID: 24669 RVA: 0x0014C041 File Offset: 0x0014A241
		[__DynamicallyInvokable]
		public BestFitMappingAttribute(bool BestFitMapping)
		{
			this._bestFitMapping = BestFitMapping;
		}

		// Token: 0x170010F2 RID: 4338
		// (get) Token: 0x0600605E RID: 24670 RVA: 0x0014C050 File Offset: 0x0014A250
		[__DynamicallyInvokable]
		public bool BestFitMapping
		{
			[__DynamicallyInvokable]
			get
			{
				return this._bestFitMapping;
			}
		}

		// Token: 0x04002B2F RID: 11055
		internal bool _bestFitMapping;

		// Token: 0x04002B30 RID: 11056
		[__DynamicallyInvokable]
		public bool ThrowOnUnmappableChar;
	}
}
