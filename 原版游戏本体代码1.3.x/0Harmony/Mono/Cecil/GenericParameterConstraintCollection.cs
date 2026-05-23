using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000243 RID: 579
	internal class GenericParameterConstraintCollection : Collection<GenericParameterConstraint>
	{
		// Token: 0x06000D22 RID: 3362 RVA: 0x0002BFCD File Offset: 0x0002A1CD
		internal GenericParameterConstraintCollection(GenericParameter genericParameter)
		{
			this.generic_parameter = genericParameter;
		}

		// Token: 0x06000D23 RID: 3363 RVA: 0x0002BFDC File Offset: 0x0002A1DC
		internal GenericParameterConstraintCollection(GenericParameter genericParameter, int length)
			: base(length)
		{
			this.generic_parameter = genericParameter;
		}

		// Token: 0x06000D24 RID: 3364 RVA: 0x0002BFEC File Offset: 0x0002A1EC
		protected override void OnAdd(GenericParameterConstraint item, int index)
		{
			item.generic_parameter = this.generic_parameter;
		}

		// Token: 0x06000D25 RID: 3365 RVA: 0x0002BFEC File Offset: 0x0002A1EC
		protected override void OnInsert(GenericParameterConstraint item, int index)
		{
			item.generic_parameter = this.generic_parameter;
		}

		// Token: 0x06000D26 RID: 3366 RVA: 0x0002BFEC File Offset: 0x0002A1EC
		protected override void OnSet(GenericParameterConstraint item, int index)
		{
			item.generic_parameter = this.generic_parameter;
		}

		// Token: 0x06000D27 RID: 3367 RVA: 0x0002BFFA File Offset: 0x0002A1FA
		protected override void OnRemove(GenericParameterConstraint item, int index)
		{
			item.generic_parameter = null;
		}

		// Token: 0x040003EA RID: 1002
		private readonly GenericParameter generic_parameter;
	}
}
