using System;

namespace System.Runtime.ConstrainedExecution
{
	// Token: 0x0200072C RID: 1836
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Interface, Inherited = false)]
	public sealed class ReliabilityContractAttribute : Attribute
	{
		// Token: 0x06005174 RID: 20852 RVA: 0x0011F1E8 File Offset: 0x0011D3E8
		public ReliabilityContractAttribute(Consistency consistencyGuarantee, Cer cer)
		{
			this._consistency = consistencyGuarantee;
			this._cer = cer;
		}

		// Token: 0x17000D6F RID: 3439
		// (get) Token: 0x06005175 RID: 20853 RVA: 0x0011F1FE File Offset: 0x0011D3FE
		public Consistency ConsistencyGuarantee
		{
			get
			{
				return this._consistency;
			}
		}

		// Token: 0x17000D70 RID: 3440
		// (get) Token: 0x06005176 RID: 20854 RVA: 0x0011F206 File Offset: 0x0011D406
		public Cer Cer
		{
			get
			{
				return this._cer;
			}
		}

		// Token: 0x04002439 RID: 9273
		private Consistency _consistency;

		// Token: 0x0400243A RID: 9274
		private Cer _cer;
	}
}
