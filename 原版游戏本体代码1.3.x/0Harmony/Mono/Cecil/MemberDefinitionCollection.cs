using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000262 RID: 610
	internal sealed class MemberDefinitionCollection<T> : Collection<T> where T : IMemberDefinition
	{
		// Token: 0x06000DC4 RID: 3524 RVA: 0x0002D7B0 File Offset: 0x0002B9B0
		internal MemberDefinitionCollection(TypeDefinition container)
		{
			this.container = container;
		}

		// Token: 0x06000DC5 RID: 3525 RVA: 0x0002D7BF File Offset: 0x0002B9BF
		internal MemberDefinitionCollection(TypeDefinition container, int capacity)
			: base(capacity)
		{
			this.container = container;
		}

		// Token: 0x06000DC6 RID: 3526 RVA: 0x0002D7CF File Offset: 0x0002B9CF
		protected override void OnAdd(T item, int index)
		{
			this.Attach(item);
		}

		// Token: 0x06000DC7 RID: 3527 RVA: 0x0002D7CF File Offset: 0x0002B9CF
		protected sealed override void OnSet(T item, int index)
		{
			this.Attach(item);
		}

		// Token: 0x06000DC8 RID: 3528 RVA: 0x0002D7CF File Offset: 0x0002B9CF
		protected sealed override void OnInsert(T item, int index)
		{
			this.Attach(item);
		}

		// Token: 0x06000DC9 RID: 3529 RVA: 0x0002D7D8 File Offset: 0x0002B9D8
		protected sealed override void OnRemove(T item, int index)
		{
			MemberDefinitionCollection<T>.Detach(item);
		}

		// Token: 0x06000DCA RID: 3530 RVA: 0x0002D7E0 File Offset: 0x0002B9E0
		protected sealed override void OnClear()
		{
			foreach (T element in this)
			{
				MemberDefinitionCollection<T>.Detach(element);
			}
		}

		// Token: 0x06000DCB RID: 3531 RVA: 0x0002D82C File Offset: 0x0002BA2C
		private void Attach(T element)
		{
			if (element.DeclaringType == this.container)
			{
				return;
			}
			if (element.DeclaringType != null)
			{
				throw new ArgumentException("Member already attached");
			}
			element.DeclaringType = this.container;
		}

		// Token: 0x06000DCC RID: 3532 RVA: 0x0002D87C File Offset: 0x0002BA7C
		private static void Detach(T element)
		{
			element.DeclaringType = null;
		}

		// Token: 0x04000416 RID: 1046
		private TypeDefinition container;
	}
}
