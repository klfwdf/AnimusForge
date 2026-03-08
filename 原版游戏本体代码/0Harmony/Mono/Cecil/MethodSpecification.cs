using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000273 RID: 627
	internal abstract class MethodSpecification : MethodReference
	{
		// Token: 0x17000398 RID: 920
		// (get) Token: 0x06000EDA RID: 3802 RVA: 0x0002FB81 File Offset: 0x0002DD81
		public MethodReference ElementMethod
		{
			get
			{
				return this.method;
			}
		}

		// Token: 0x17000399 RID: 921
		// (get) Token: 0x06000EDB RID: 3803 RVA: 0x0002FB89 File Offset: 0x0002DD89
		// (set) Token: 0x06000EDC RID: 3804 RVA: 0x0001C627 File Offset: 0x0001A827
		public override string Name
		{
			get
			{
				return this.method.Name;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700039A RID: 922
		// (get) Token: 0x06000EDD RID: 3805 RVA: 0x0002FB96 File Offset: 0x0002DD96
		// (set) Token: 0x06000EDE RID: 3806 RVA: 0x0001C627 File Offset: 0x0001A827
		public override MethodCallingConvention CallingConvention
		{
			get
			{
				return this.method.CallingConvention;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700039B RID: 923
		// (get) Token: 0x06000EDF RID: 3807 RVA: 0x0002FBA3 File Offset: 0x0002DDA3
		// (set) Token: 0x06000EE0 RID: 3808 RVA: 0x0001C627 File Offset: 0x0001A827
		public override bool HasThis
		{
			get
			{
				return this.method.HasThis;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700039C RID: 924
		// (get) Token: 0x06000EE1 RID: 3809 RVA: 0x0002FBB0 File Offset: 0x0002DDB0
		// (set) Token: 0x06000EE2 RID: 3810 RVA: 0x0001C627 File Offset: 0x0001A827
		public override bool ExplicitThis
		{
			get
			{
				return this.method.ExplicitThis;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700039D RID: 925
		// (get) Token: 0x06000EE3 RID: 3811 RVA: 0x0002FBBD File Offset: 0x0002DDBD
		// (set) Token: 0x06000EE4 RID: 3812 RVA: 0x0001C627 File Offset: 0x0001A827
		public override MethodReturnType MethodReturnType
		{
			get
			{
				return this.method.MethodReturnType;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700039E RID: 926
		// (get) Token: 0x06000EE5 RID: 3813 RVA: 0x0002FBCA File Offset: 0x0002DDCA
		// (set) Token: 0x06000EE6 RID: 3814 RVA: 0x0001C627 File Offset: 0x0001A827
		public override TypeReference DeclaringType
		{
			get
			{
				return this.method.DeclaringType;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700039F RID: 927
		// (get) Token: 0x06000EE7 RID: 3815 RVA: 0x0002FBD7 File Offset: 0x0002DDD7
		public override ModuleDefinition Module
		{
			get
			{
				return this.method.Module;
			}
		}

		// Token: 0x170003A0 RID: 928
		// (get) Token: 0x06000EE8 RID: 3816 RVA: 0x0002FBE4 File Offset: 0x0002DDE4
		public override bool HasParameters
		{
			get
			{
				return this.method.HasParameters;
			}
		}

		// Token: 0x170003A1 RID: 929
		// (get) Token: 0x06000EE9 RID: 3817 RVA: 0x0002FBF1 File Offset: 0x0002DDF1
		public override Collection<ParameterDefinition> Parameters
		{
			get
			{
				return this.method.Parameters;
			}
		}

		// Token: 0x170003A2 RID: 930
		// (get) Token: 0x06000EEA RID: 3818 RVA: 0x0002FBFE File Offset: 0x0002DDFE
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.method.ContainsGenericParameter;
			}
		}

		// Token: 0x06000EEB RID: 3819 RVA: 0x0002FC0B File Offset: 0x0002DE0B
		internal MethodSpecification(MethodReference method)
		{
			Mixin.CheckMethod(method);
			this.method = method;
			this.token = new MetadataToken(TokenType.MethodSpec);
		}

		// Token: 0x06000EEC RID: 3820 RVA: 0x0002FC30 File Offset: 0x0002DE30
		public sealed override MethodReference GetElementMethod()
		{
			return this.method.GetElementMethod();
		}

		// Token: 0x04000494 RID: 1172
		private readonly MethodReference method;
	}
}
