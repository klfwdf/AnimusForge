using System;
using System.Text;
using System.Threading;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200023E RID: 574
	internal sealed class GenericInstanceType : TypeSpecification, IGenericInstance, IMetadataTokenProvider, IGenericContext
	{
		// Token: 0x170002DC RID: 732
		// (get) Token: 0x06000CDC RID: 3292 RVA: 0x0002B95D File Offset: 0x00029B5D
		public bool HasGenericArguments
		{
			get
			{
				return !this.arguments.IsNullOrEmpty<TypeReference>();
			}
		}

		// Token: 0x170002DD RID: 733
		// (get) Token: 0x06000CDD RID: 3293 RVA: 0x0002B96D File Offset: 0x00029B6D
		public Collection<TypeReference> GenericArguments
		{
			get
			{
				if (this.arguments == null)
				{
					Interlocked.CompareExchange<Collection<TypeReference>>(ref this.arguments, new Collection<TypeReference>(), null);
				}
				return this.arguments;
			}
		}

		// Token: 0x170002DE RID: 734
		// (get) Token: 0x06000CDE RID: 3294 RVA: 0x0002B98F File Offset: 0x00029B8F
		// (set) Token: 0x06000CDF RID: 3295 RVA: 0x00003BBE File Offset: 0x00001DBE
		public override TypeReference DeclaringType
		{
			get
			{
				return base.ElementType.DeclaringType;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x170002DF RID: 735
		// (get) Token: 0x06000CE0 RID: 3296 RVA: 0x0002B99C File Offset: 0x00029B9C
		public override string FullName
		{
			get
			{
				StringBuilder name = new StringBuilder();
				name.Append(base.FullName);
				this.GenericInstanceFullName(name);
				return name.ToString();
			}
		}

		// Token: 0x170002E0 RID: 736
		// (get) Token: 0x06000CE1 RID: 3297 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsGenericInstance
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170002E1 RID: 737
		// (get) Token: 0x06000CE2 RID: 3298 RVA: 0x0002B9C9 File Offset: 0x00029BC9
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.ContainsGenericParameter() || base.ContainsGenericParameter;
			}
		}

		// Token: 0x170002E2 RID: 738
		// (get) Token: 0x06000CE3 RID: 3299 RVA: 0x0002B9DB File Offset: 0x00029BDB
		IGenericParameterProvider IGenericContext.Type
		{
			get
			{
				return base.ElementType;
			}
		}

		// Token: 0x06000CE4 RID: 3300 RVA: 0x0002B9E3 File Offset: 0x00029BE3
		public GenericInstanceType(TypeReference type)
			: base(type)
		{
			base.IsValueType = type.IsValueType;
			this.etype = Mono.Cecil.Metadata.ElementType.GenericInst;
		}

		// Token: 0x06000CE5 RID: 3301 RVA: 0x0002BA00 File Offset: 0x00029C00
		internal GenericInstanceType(TypeReference type, int arity)
			: this(type)
		{
			this.arguments = new Collection<TypeReference>(arity);
		}

		// Token: 0x040003DB RID: 987
		private Collection<TypeReference> arguments;
	}
}
