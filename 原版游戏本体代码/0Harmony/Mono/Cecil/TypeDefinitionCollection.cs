using System;
using System.Collections.Generic;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002A6 RID: 678
	internal sealed class TypeDefinitionCollection : Collection<TypeDefinition>
	{
		// Token: 0x06001105 RID: 4357 RVA: 0x0003307F File Offset: 0x0003127F
		internal TypeDefinitionCollection(ModuleDefinition container)
		{
			this.container = container;
			this.name_cache = new Dictionary<Row<string, string>, TypeDefinition>(new RowEqualityComparer());
		}

		// Token: 0x06001106 RID: 4358 RVA: 0x0003309E File Offset: 0x0003129E
		internal TypeDefinitionCollection(ModuleDefinition container, int capacity)
			: base(capacity)
		{
			this.container = container;
			this.name_cache = new Dictionary<Row<string, string>, TypeDefinition>(capacity, new RowEqualityComparer());
		}

		// Token: 0x06001107 RID: 4359 RVA: 0x000330BF File Offset: 0x000312BF
		protected override void OnAdd(TypeDefinition item, int index)
		{
			this.Attach(item);
		}

		// Token: 0x06001108 RID: 4360 RVA: 0x000330BF File Offset: 0x000312BF
		protected override void OnSet(TypeDefinition item, int index)
		{
			this.Attach(item);
		}

		// Token: 0x06001109 RID: 4361 RVA: 0x000330BF File Offset: 0x000312BF
		protected override void OnInsert(TypeDefinition item, int index)
		{
			this.Attach(item);
		}

		// Token: 0x0600110A RID: 4362 RVA: 0x000330C8 File Offset: 0x000312C8
		protected override void OnRemove(TypeDefinition item, int index)
		{
			this.Detach(item);
		}

		// Token: 0x0600110B RID: 4363 RVA: 0x000330D4 File Offset: 0x000312D4
		protected override void OnClear()
		{
			foreach (TypeDefinition type in this)
			{
				this.Detach(type);
			}
		}

		// Token: 0x0600110C RID: 4364 RVA: 0x00033124 File Offset: 0x00031324
		private void Attach(TypeDefinition type)
		{
			if (type.Module != null && type.Module != this.container)
			{
				throw new ArgumentException("Type already attached");
			}
			type.module = this.container;
			type.scope = this.container;
			this.name_cache[new Row<string, string>(type.Namespace, type.Name)] = type;
		}

		// Token: 0x0600110D RID: 4365 RVA: 0x00033187 File Offset: 0x00031387
		private void Detach(TypeDefinition type)
		{
			type.module = null;
			type.scope = null;
			this.name_cache.Remove(new Row<string, string>(type.Namespace, type.Name));
		}

		// Token: 0x0600110E RID: 4366 RVA: 0x000331B4 File Offset: 0x000313B4
		public TypeDefinition GetType(string fullname)
		{
			string @namespace;
			string name;
			TypeParser.SplitFullName(fullname, out @namespace, out name);
			return this.GetType(@namespace, name);
		}

		// Token: 0x0600110F RID: 4367 RVA: 0x000331D4 File Offset: 0x000313D4
		public TypeDefinition GetType(string @namespace, string name)
		{
			TypeDefinition type;
			if (this.name_cache.TryGetValue(new Row<string, string>(@namespace, name), out type))
			{
				return type;
			}
			return null;
		}

		// Token: 0x040005FB RID: 1531
		private readonly ModuleDefinition container;

		// Token: 0x040005FC RID: 1532
		private readonly Dictionary<Row<string, string>, TypeDefinition> name_cache;
	}
}
