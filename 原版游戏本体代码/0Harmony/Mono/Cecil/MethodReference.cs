using System;
using System.Text;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200026F RID: 623
	internal class MethodReference : MemberReference, IMethodSignature, IMetadataTokenProvider, IGenericParameterProvider, IGenericContext
	{
		// Token: 0x1700037B RID: 891
		// (get) Token: 0x06000EA2 RID: 3746 RVA: 0x0002F44F File Offset: 0x0002D64F
		// (set) Token: 0x06000EA3 RID: 3747 RVA: 0x0002F457 File Offset: 0x0002D657
		public virtual bool HasThis
		{
			get
			{
				return this.has_this;
			}
			set
			{
				this.has_this = value;
			}
		}

		// Token: 0x1700037C RID: 892
		// (get) Token: 0x06000EA4 RID: 3748 RVA: 0x0002F460 File Offset: 0x0002D660
		// (set) Token: 0x06000EA5 RID: 3749 RVA: 0x0002F468 File Offset: 0x0002D668
		public virtual bool ExplicitThis
		{
			get
			{
				return this.explicit_this;
			}
			set
			{
				this.explicit_this = value;
			}
		}

		// Token: 0x1700037D RID: 893
		// (get) Token: 0x06000EA6 RID: 3750 RVA: 0x0002F471 File Offset: 0x0002D671
		// (set) Token: 0x06000EA7 RID: 3751 RVA: 0x0002F479 File Offset: 0x0002D679
		public virtual MethodCallingConvention CallingConvention
		{
			get
			{
				return this.calling_convention;
			}
			set
			{
				this.calling_convention = value;
			}
		}

		// Token: 0x1700037E RID: 894
		// (get) Token: 0x06000EA8 RID: 3752 RVA: 0x0002F482 File Offset: 0x0002D682
		public virtual bool HasParameters
		{
			get
			{
				return !this.parameters.IsNullOrEmpty<ParameterDefinition>();
			}
		}

		// Token: 0x1700037F RID: 895
		// (get) Token: 0x06000EA9 RID: 3753 RVA: 0x0002F492 File Offset: 0x0002D692
		public virtual Collection<ParameterDefinition> Parameters
		{
			get
			{
				if (this.parameters == null)
				{
					Interlocked.CompareExchange<ParameterDefinitionCollection>(ref this.parameters, new ParameterDefinitionCollection(this), null);
				}
				return this.parameters;
			}
		}

		// Token: 0x17000380 RID: 896
		// (get) Token: 0x06000EAA RID: 3754 RVA: 0x0002F4B8 File Offset: 0x0002D6B8
		IGenericParameterProvider IGenericContext.Type
		{
			get
			{
				TypeReference declaring_type = this.DeclaringType;
				GenericInstanceType instance = declaring_type as GenericInstanceType;
				if (instance != null)
				{
					return instance.ElementType;
				}
				return declaring_type;
			}
		}

		// Token: 0x17000381 RID: 897
		// (get) Token: 0x06000EAB RID: 3755 RVA: 0x0001B6A2 File Offset: 0x000198A2
		IGenericParameterProvider IGenericContext.Method
		{
			get
			{
				return this;
			}
		}

		// Token: 0x17000382 RID: 898
		// (get) Token: 0x06000EAC RID: 3756 RVA: 0x0001B6C7 File Offset: 0x000198C7
		GenericParameterType IGenericParameterProvider.GenericParameterType
		{
			get
			{
				return GenericParameterType.Method;
			}
		}

		// Token: 0x17000383 RID: 899
		// (get) Token: 0x06000EAD RID: 3757 RVA: 0x0002F4DE File Offset: 0x0002D6DE
		public virtual bool HasGenericParameters
		{
			get
			{
				return !this.generic_parameters.IsNullOrEmpty<GenericParameter>();
			}
		}

		// Token: 0x17000384 RID: 900
		// (get) Token: 0x06000EAE RID: 3758 RVA: 0x0002F4EE File Offset: 0x0002D6EE
		public virtual Collection<GenericParameter> GenericParameters
		{
			get
			{
				if (this.generic_parameters == null)
				{
					Interlocked.CompareExchange<Collection<GenericParameter>>(ref this.generic_parameters, new GenericParameterCollection(this), null);
				}
				return this.generic_parameters;
			}
		}

		// Token: 0x17000385 RID: 901
		// (get) Token: 0x06000EAF RID: 3759 RVA: 0x0002F514 File Offset: 0x0002D714
		// (set) Token: 0x06000EB0 RID: 3760 RVA: 0x0002F534 File Offset: 0x0002D734
		public TypeReference ReturnType
		{
			get
			{
				MethodReturnType return_type = this.MethodReturnType;
				if (return_type == null)
				{
					return null;
				}
				return return_type.ReturnType;
			}
			set
			{
				MethodReturnType return_type = this.MethodReturnType;
				if (return_type != null)
				{
					return_type.ReturnType = value;
				}
			}
		}

		// Token: 0x17000386 RID: 902
		// (get) Token: 0x06000EB1 RID: 3761 RVA: 0x0002F552 File Offset: 0x0002D752
		// (set) Token: 0x06000EB2 RID: 3762 RVA: 0x0002F55A File Offset: 0x0002D75A
		public virtual MethodReturnType MethodReturnType
		{
			get
			{
				return this.return_type;
			}
			set
			{
				this.return_type = value;
			}
		}

		// Token: 0x17000387 RID: 903
		// (get) Token: 0x06000EB3 RID: 3763 RVA: 0x0002F564 File Offset: 0x0002D764
		public override string FullName
		{
			get
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(this.ReturnType.FullName).Append(" ").Append(base.MemberFullName());
				this.MethodSignatureFullName(builder);
				return builder.ToString();
			}
		}

		// Token: 0x17000388 RID: 904
		// (get) Token: 0x06000EB4 RID: 3764 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsGenericInstance
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000389 RID: 905
		// (get) Token: 0x06000EB5 RID: 3765 RVA: 0x0002F5AC File Offset: 0x0002D7AC
		public override bool ContainsGenericParameter
		{
			get
			{
				if (this.ReturnType.ContainsGenericParameter || base.ContainsGenericParameter)
				{
					return true;
				}
				if (!this.HasParameters)
				{
					return false;
				}
				Collection<ParameterDefinition> parameters = this.Parameters;
				for (int i = 0; i < parameters.Count; i++)
				{
					if (parameters[i].ParameterType.ContainsGenericParameter)
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x06000EB6 RID: 3766 RVA: 0x0002F608 File Offset: 0x0002D808
		internal MethodReference()
		{
			this.return_type = new MethodReturnType(this);
			this.token = new MetadataToken(TokenType.MemberRef);
		}

		// Token: 0x06000EB7 RID: 3767 RVA: 0x0002F62C File Offset: 0x0002D82C
		public MethodReference(string name, TypeReference returnType)
			: base(name)
		{
			Mixin.CheckType(returnType, Mixin.Argument.returnType);
			this.return_type = new MethodReturnType(this);
			this.return_type.ReturnType = returnType;
			this.token = new MetadataToken(TokenType.MemberRef);
		}

		// Token: 0x06000EB8 RID: 3768 RVA: 0x0002F665 File Offset: 0x0002D865
		public MethodReference(string name, TypeReference returnType, TypeReference declaringType)
			: this(name, returnType)
		{
			Mixin.CheckType(declaringType, Mixin.Argument.declaringType);
			this.DeclaringType = declaringType;
		}

		// Token: 0x06000EB9 RID: 3769 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public virtual MethodReference GetElementMethod()
		{
			return this;
		}

		// Token: 0x06000EBA RID: 3770 RVA: 0x0002F67E File Offset: 0x0002D87E
		protected override IMemberDefinition ResolveDefinition()
		{
			return this.Resolve();
		}

		// Token: 0x06000EBB RID: 3771 RVA: 0x0002F686 File Offset: 0x0002D886
		public new virtual MethodDefinition Resolve()
		{
			ModuleDefinition module = this.Module;
			if (module == null)
			{
				throw new NotSupportedException();
			}
			return module.Resolve(this);
		}

		// Token: 0x04000481 RID: 1153
		internal ParameterDefinitionCollection parameters;

		// Token: 0x04000482 RID: 1154
		private MethodReturnType return_type;

		// Token: 0x04000483 RID: 1155
		private bool has_this;

		// Token: 0x04000484 RID: 1156
		private bool explicit_this;

		// Token: 0x04000485 RID: 1157
		private MethodCallingConvention calling_convention;

		// Token: 0x04000486 RID: 1158
		internal Collection<GenericParameter> generic_parameters;
	}
}
