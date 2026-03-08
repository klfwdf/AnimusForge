using System;

namespace System.Runtime.Versioning
{
	// Token: 0x0200071E RID: 1822
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	public sealed class ComponentGuaranteesAttribute : Attribute
	{
		// Token: 0x06005155 RID: 20821 RVA: 0x0011ED90 File Offset: 0x0011CF90
		public ComponentGuaranteesAttribute(ComponentGuaranteesOptions guarantees)
		{
			this._guarantees = guarantees;
		}

		// Token: 0x17000D69 RID: 3433
		// (get) Token: 0x06005156 RID: 20822 RVA: 0x0011ED9F File Offset: 0x0011CF9F
		public ComponentGuaranteesOptions Guarantees
		{
			get
			{
				return this._guarantees;
			}
		}

		// Token: 0x0400240F RID: 9231
		private ComponentGuaranteesOptions _guarantees;
	}
}
