using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000287 RID: 647
	internal sealed class ParameterDefinitionCollection : Collection<ParameterDefinition>
	{
		// Token: 0x06000FF5 RID: 4085 RVA: 0x00031620 File Offset: 0x0002F820
		internal ParameterDefinitionCollection(IMethodSignature method)
		{
			this.method = method;
		}

		// Token: 0x06000FF6 RID: 4086 RVA: 0x0003162F File Offset: 0x0002F82F
		internal ParameterDefinitionCollection(IMethodSignature method, int capacity)
			: base(capacity)
		{
			this.method = method;
		}

		// Token: 0x06000FF7 RID: 4087 RVA: 0x0003163F File Offset: 0x0002F83F
		protected override void OnAdd(ParameterDefinition item, int index)
		{
			item.method = this.method;
			item.index = index;
		}

		// Token: 0x06000FF8 RID: 4088 RVA: 0x00031654 File Offset: 0x0002F854
		protected override void OnInsert(ParameterDefinition item, int index)
		{
			item.method = this.method;
			item.index = index;
			for (int i = index; i < this.size; i++)
			{
				this.items[i].index = i + 1;
			}
		}

		// Token: 0x06000FF9 RID: 4089 RVA: 0x0003163F File Offset: 0x0002F83F
		protected override void OnSet(ParameterDefinition item, int index)
		{
			item.method = this.method;
			item.index = index;
		}

		// Token: 0x06000FFA RID: 4090 RVA: 0x00031698 File Offset: 0x0002F898
		protected override void OnRemove(ParameterDefinition item, int index)
		{
			item.method = null;
			item.index = -1;
			for (int i = index + 1; i < this.size; i++)
			{
				this.items[i].index = i - 1;
			}
		}

		// Token: 0x04000544 RID: 1348
		private readonly IMethodSignature method;
	}
}
