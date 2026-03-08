using System;
using System.Text;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200023D RID: 573
	internal sealed class GenericInstanceMethod : MethodSpecification, IGenericInstance, IMetadataTokenProvider, IGenericContext
	{
		// Token: 0x170002D5 RID: 725
		// (get) Token: 0x06000CD3 RID: 3283 RVA: 0x0002B874 File Offset: 0x00029A74
		public bool HasGenericArguments
		{
			get
			{
				return !this.arguments.IsNullOrEmpty<TypeReference>();
			}
		}

		// Token: 0x170002D6 RID: 726
		// (get) Token: 0x06000CD4 RID: 3284 RVA: 0x0002B884 File Offset: 0x00029A84
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

		// Token: 0x170002D7 RID: 727
		// (get) Token: 0x06000CD5 RID: 3285 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsGenericInstance
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170002D8 RID: 728
		// (get) Token: 0x06000CD6 RID: 3286 RVA: 0x0002B8A6 File Offset: 0x00029AA6
		IGenericParameterProvider IGenericContext.Method
		{
			get
			{
				return base.ElementMethod;
			}
		}

		// Token: 0x170002D9 RID: 729
		// (get) Token: 0x06000CD7 RID: 3287 RVA: 0x0002B8AE File Offset: 0x00029AAE
		IGenericParameterProvider IGenericContext.Type
		{
			get
			{
				return base.ElementMethod.DeclaringType;
			}
		}

		// Token: 0x170002DA RID: 730
		// (get) Token: 0x06000CD8 RID: 3288 RVA: 0x0002B8BB File Offset: 0x00029ABB
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.ContainsGenericParameter() || base.ContainsGenericParameter;
			}
		}

		// Token: 0x170002DB RID: 731
		// (get) Token: 0x06000CD9 RID: 3289 RVA: 0x0002B8D0 File Offset: 0x00029AD0
		public override string FullName
		{
			get
			{
				StringBuilder signature = new StringBuilder();
				MethodReference method = base.ElementMethod;
				signature.Append(method.ReturnType.FullName).Append(" ").Append(method.DeclaringType.FullName)
					.Append("::")
					.Append(method.Name);
				this.GenericInstanceFullName(signature);
				this.MethodSignatureFullName(signature);
				return signature.ToString();
			}
		}

		// Token: 0x06000CDA RID: 3290 RVA: 0x0002B93F File Offset: 0x00029B3F
		public GenericInstanceMethod(MethodReference method)
			: base(method)
		{
		}

		// Token: 0x06000CDB RID: 3291 RVA: 0x0002B948 File Offset: 0x00029B48
		internal GenericInstanceMethod(MethodReference method, int arity)
			: this(method)
		{
			this.arguments = new Collection<TypeReference>(arity);
		}

		// Token: 0x040003DA RID: 986
		private Collection<TypeReference> arguments;
	}
}
