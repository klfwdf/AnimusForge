using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000241 RID: 577
	internal sealed class GenericParameterCollection : Collection<GenericParameter>
	{
		// Token: 0x06000D13 RID: 3347 RVA: 0x0002BE14 File Offset: 0x0002A014
		internal GenericParameterCollection(IGenericParameterProvider owner)
		{
			this.owner = owner;
		}

		// Token: 0x06000D14 RID: 3348 RVA: 0x0002BE23 File Offset: 0x0002A023
		internal GenericParameterCollection(IGenericParameterProvider owner, int capacity)
			: base(capacity)
		{
			this.owner = owner;
		}

		// Token: 0x06000D15 RID: 3349 RVA: 0x0002BE33 File Offset: 0x0002A033
		protected override void OnAdd(GenericParameter item, int index)
		{
			this.UpdateGenericParameter(item, index);
		}

		// Token: 0x06000D16 RID: 3350 RVA: 0x0002BE40 File Offset: 0x0002A040
		protected override void OnInsert(GenericParameter item, int index)
		{
			this.UpdateGenericParameter(item, index);
			for (int i = index; i < this.size; i++)
			{
				this.items[i].position = i + 1;
			}
		}

		// Token: 0x06000D17 RID: 3351 RVA: 0x0002BE33 File Offset: 0x0002A033
		protected override void OnSet(GenericParameter item, int index)
		{
			this.UpdateGenericParameter(item, index);
		}

		// Token: 0x06000D18 RID: 3352 RVA: 0x0002BE76 File Offset: 0x0002A076
		private void UpdateGenericParameter(GenericParameter item, int index)
		{
			item.owner = this.owner;
			item.position = index;
			item.type = this.owner.GenericParameterType;
		}

		// Token: 0x06000D19 RID: 3353 RVA: 0x0002BE9C File Offset: 0x0002A09C
		protected override void OnRemove(GenericParameter item, int index)
		{
			item.owner = null;
			item.position = -1;
			item.type = GenericParameterType.Type;
			for (int i = index + 1; i < this.size; i++)
			{
				this.items[i].position = i - 1;
			}
		}

		// Token: 0x040003E5 RID: 997
		private readonly IGenericParameterProvider owner;
	}
}
