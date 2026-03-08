using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002A5 RID: 677
	internal class InterfaceImplementationCollection : Collection<InterfaceImplementation>
	{
		// Token: 0x060010FF RID: 4351 RVA: 0x00033049 File Offset: 0x00031249
		internal InterfaceImplementationCollection(TypeDefinition type)
		{
			this.type = type;
		}

		// Token: 0x06001100 RID: 4352 RVA: 0x00033058 File Offset: 0x00031258
		internal InterfaceImplementationCollection(TypeDefinition type, int length)
			: base(length)
		{
			this.type = type;
		}

		// Token: 0x06001101 RID: 4353 RVA: 0x00033068 File Offset: 0x00031268
		protected override void OnAdd(InterfaceImplementation item, int index)
		{
			item.type = this.type;
		}

		// Token: 0x06001102 RID: 4354 RVA: 0x00033068 File Offset: 0x00031268
		protected override void OnInsert(InterfaceImplementation item, int index)
		{
			item.type = this.type;
		}

		// Token: 0x06001103 RID: 4355 RVA: 0x00033068 File Offset: 0x00031268
		protected override void OnSet(InterfaceImplementation item, int index)
		{
			item.type = this.type;
		}

		// Token: 0x06001104 RID: 4356 RVA: 0x00033076 File Offset: 0x00031276
		protected override void OnRemove(InterfaceImplementation item, int index)
		{
			item.type = null;
		}

		// Token: 0x040005FA RID: 1530
		private readonly TypeDefinition type;
	}
}
